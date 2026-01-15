using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExileSurvival.Networking.Core;
using UnityEngine;

public class SpawnerPortSetter : MonoBehaviour {
    [Tooltip("Reference to your NetworkManager.")] [SerializeField] private ServerManager serverManager;

    [SerializeField] private ushort _defaultPort           = 7770;
    [SerializeField] private string _defaultSpawnerAddress = "ws://127.0.0.1:8080";


    [SerializeField] private string versionChecker = "Warning VERSION SUNDAY the new litenet with ws";
    
    private ClientWebSocket         _webSocket;
    private CancellationTokenSource _cancellationTokenSource;

    private async void Start() {
        UnityEngine.Debug.ClearDeveloperConsole();
        UnityEngine.Debug.Log($"[SpawnerPortSetter] Starting");


        if (serverManager == null) {
            Debug.LogError("[SpawnerPortSetter] NetworkManager is not referenced.");
            serverManager = FindAnyObjectByType<ServerManager>();
            if (serverManager == null) return;
        }

        // Parse command line arguments
        int    port           = GetPortFromCommandLine(_defaultPort);
        string spawnerAddress = GetSpawnerAddressFromCommandLine(_defaultSpawnerAddress);

        UnityEngine.Debug.Log($"[SpawnerPortSetter] Configuration - Port: {port}, Spawner Address: {spawnerAddress}");

        // Start Server
        StartServer(port);

        // Connect to spawner via WebSocket
        await ConnectToSpawner(spawnerAddress);
    }

    /// <summary>
    /// Starts the server on the specified port.
    /// </summary>
    private void StartServer(int port) {
        try {
            UnityEngine.Debug.Log($"[SpawnerPortSetter] Starting Server on Port: {port}");

            UnityEngine.Debug.Log(versionChecker);
            serverManager.Port = port;
            serverManager.StartServer();

           

        }
        catch (Exception ex) {
            UnityEngine.Debug.LogError($"[SpawnerPortSetter] Failed to start server: {ex}");
        }
    }

    /// <summary>
    /// Connects to the spawner using WebSocket.
    /// </summary>
    private async Task ConnectToSpawner(string spawnerAddress) {
        try {
            _webSocket               = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            UnityEngine.Debug.Log($"[SpawnerPortSetter] Connecting to Spawner WebSocket at: {spawnerAddress}");

            Uri serverUri = new Uri(spawnerAddress);

            await _webSocket.ConnectAsync(serverUri, _cancellationTokenSource.Token);

            UnityEngine.Debug.Log($"[SpawnerPortSetter] Successfully connected to Spawner WebSocket");

            // Start receiving messages from spawner
            _ = ReceiveMessages();

            // Send initial registration message to spawner
            await SendMessage($"{{ \"type\": \"register\", \"port\": {GetPortFromCommandLine(_defaultPort)} }}");
        }
        catch (Exception ex) {
            UnityEngine.Debug.LogError($"[SpawnerPortSetter] Failed to connect to Spawner WebSocket: {ex}");
        }
    }

    /// <summary>
    /// Receives messages from the spawner WebSocket.
    /// </summary>
    private async Task ReceiveMessages() {
        byte[] buffer = new byte[4096];

        try {
            while (_webSocket.State == WebSocketState.Open) {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Text) {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    UnityEngine.Debug.Log($"[SpawnerPortSetter] Received from Spawner: {message}");

                    // Handle spawner messages on Unity main thread
                    // Check if UnityMainThreadDispatcher exists or is needed, keeping it as is from user code
                    // UnityMainThreadDispatcher.Instance().Enqueue(() => HandleSpawnerMessage(message));
                }
                else if (result.MessageType == WebSocketMessageType.Close) {
                    UnityEngine.Debug.Log("[SpawnerPortSetter] Spawner WebSocket closed");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
        catch (Exception ex) {
            UnityEngine.Debug.LogError($"[SpawnerPortSetter] Error receiving WebSocket message: {ex}");
        }
    }

    /// <summary>
    /// Sends a message to the spawner via WebSocket.
    /// </summary>
    private async Task SendMessage(string message) {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open) {
            UnityEngine.Debug.LogWarning("[SpawnerPortSetter] Cannot send message, WebSocket is not open");
            return;
        }

        try {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);

            UnityEngine.Debug.Log($"[SpawnerPortSetter] Sent to Spawner: {message}");
        }
        catch (Exception ex) {
            UnityEngine.Debug.LogError($"[SpawnerPortSetter] Failed to send WebSocket message: {ex}");
        }
    }

    /// <summary>
    /// Handles messages received from the spawner.
    /// </summary>
    private void HandleSpawnerMessage(string message) {
        // Parse and handle spawner commands here
        UnityEngine.Debug.Log($"[SpawnerPortSetter] Handling spawner message: {message}");

        // Example: Parse JSON and handle different message types
        // You can add your specific logic here based on spawner protocol
    }

    /// <summary>
    /// Parses command line arguments to find the -port value.
    /// </summary>
    private int GetPortFromCommandLine(ushort defaultPort) {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) {
            if (args[i]
                   .ToLower() ==
                "-port" &&
                i + 1 < args.Length) {
                if (int.TryParse(args[i + 1], out int parsedPort)) {
                    Debug.Log($"[SpawnerPortSetter] Command line argument -port found: {parsedPort}");
                    return parsedPort;
                }
            }
        }

        Debug.Log($"[SpawnerPortSetter] Command line argument -port not found. Using default: {defaultPort}");
        return defaultPort;
    }

    /// <summary>
    /// Parses command line arguments to find the -spawneraddress value.
    /// </summary>
    private string GetSpawnerAddressFromCommandLine(string defaultAddress) {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) {
            if (args[i]
                   .ToLower() ==
                "-ws" &&
                i + 1 < args.Length) {
                string address = args[i + 1];
                Debug.Log($"[SpawnerPortSetter] Command line argument -ws found: {address}");
                return address;
            }
        }

        Debug.Log($"[SpawnerPortSetter] Command line argument -ws not found. Using default: {defaultAddress}");
        return defaultAddress;
    }

    private async void OnDestroy() {
        // Clean up WebSocket connection
        if (_webSocket != null && _webSocket.State == WebSocketState.Open) {
            _cancellationTokenSource?.Cancel();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application closing", CancellationToken.None);
            _webSocket?.Dispose();
        }
    }
}
