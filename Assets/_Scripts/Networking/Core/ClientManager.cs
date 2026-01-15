#if !UNITY_SERVER || UNITY_EDITOR
using System;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExileSurvival.Networking.Data;
using Sirenix.OdinInspector;

namespace ExileSurvival.Networking.Core
{
    public class ClientManager : Singleton<ClientManager>
    {
        #region Inspector Fields
        [BoxGroup("Client Configuration")]
        [Tooltip("The IP address of the server to connect to.")]
        public string ServerAddress = "localhost";
        
        [BoxGroup("Client Configuration")]
        [Tooltip("The port of the server to connect to.")]
        public int Port = 9050;

        [BoxGroup("Client Configuration")]
        [Tooltip("A secret key that must match the server's key to connect.")]
        public string ConnectionKey = "ExileSurvival_Dev";
        
        [BoxGroup("Client Configuration")]
        [Tooltip("How many times per second the client will tick.")]
        public int TickRate = 60;
        #endregion

        #region Public Properties
        public NetManager Client { get; private set; }
        public NetPacketProcessor PacketProcessor { get; private set; }
        public int LocalClientId { get; private set; } = -1;
        public uint CurrentTick { get; private set; }
        public bool IsConnected => Client != null && Client.FirstPeer != null && Client.FirstPeer.ConnectionState == ConnectionState.Connected;
        #endregion

        #region Events
        public event Action<PlayerStatePacket> OnClientReceivedState;
        public event Action<JoinAcceptPacket> OnJoinAccept;
        #endregion

        #region Private Fields
        private float _tickTimer;
        private float _tickInterval;
        private bool _isQuitting = false;
        private EventBasedNetListener _listener;
        private NetDataWriter _writer;
        private const string TAG = "ClientManager";
        #endregion

        #region Odin Inspector
        [BoxGroup("Client Control")]
        [Button("Connect"), GUIColor(0, 1, 0), PropertyOrder(-1)]
        [HideIf("IsConnected")]
        private void ConnectButton() => Connect(ServerAddress, Port, ConnectionKey);

        [BoxGroup("Client Control")]
        [Button("Disconnect"), GUIColor(1, 0, 0), PropertyOrder(-1)]
        [EnableIf("IsConnected")]
        private void DisconnectButton() => StopClient();

        [BoxGroup("Live Client Info")]
        [ShowInInspector, ReadOnly, ShowIf("@IsConnected")]
        private int ClientId => LocalClientId;
        
        [BoxGroup("Live Client Info")]
        [ShowInInspector, ReadOnly, ShowIf("@IsConnected")]
        private uint SyncedTick => CurrentTick;
        #endregion

        #region Unity Lifecycle
        private void OnApplicationQuit() => _isQuitting = true;

        private void Awake()
        {
           
            
            _tickInterval = 1f / TickRate;
            _writer = new NetDataWriter();
            PacketProcessor = new NetPacketProcessor();
            
            NetworkCommon.RegisterPackets(PacketProcessor);
            SubscribeToPacketProcessor();
            InitializeClient();
        }

        private void Update()
        {
            if (_isQuitting || Client == null) return;

            Client.PollEvents();
            if (IsConnected)
            {
                ClientTickLogic();
            }
        }
        #endregion

        #region Public API
        public void InitializeClient()
        {
            if (Client != null) return;

            _listener = new EventBasedNetListener();
            SetupListeners();

            Client = new NetManager(_listener);
            Client.Start();
        }

        public void Connect(string address, int port, string key)
        {
            if (Client == null) InitializeClient();

            if (Client.FirstPeer == null || Client.FirstPeer.ConnectionState == ConnectionState.Disconnected)
            {
                Debug.Log($"[{TAG}] Attempting to connect to {address}:{port}...");
                Client.Connect(address, port, key);
            }
        }

        public void StopClient()
        {
            Client?.Stop();
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new()
        {
            if (!IsConnected) return;
            
            _writer.Reset();
            PacketProcessor.WriteNetSerializable(_writer, ref packet);
            Client.FirstPeer.Send(_writer, deliveryMethod);
        }
        #endregion

        #region Listeners
        private void SetupListeners()
        {
            _listener.PeerConnectedEvent += peer => Debug.Log($"[{TAG}] Connection successful!");
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) => PacketProcessor.ReadAllPackets(dataReader, fromPeer);
            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
                Debug.Log($"[{TAG}] Disconnected from server: {disconnectInfo.Reason}");
                LocalClientId = -1;
            };
        }
        
        private void SubscribeToPacketProcessor()
        {
            PacketProcessor.SubscribeNetSerializable<PlayerStatePacket, NetPeer>(OnStateReceived);
            PacketProcessor.SubscribeNetSerializable<JoinAcceptPacket, NetPeer>(OnJoinAcceptReceived);
        }
        #endregion

        #region Internal Logic
        private void ClientTickLogic() {
            _tickTimer += Time.deltaTime;
            while (_tickTimer >= _tickInterval) {
                _tickTimer -= _tickInterval;
                CurrentTick++;
            }
        }

        private void OnStateReceived(PlayerStatePacket packet, NetPeer peer) => OnClientReceivedState?.Invoke(packet);

        private void OnJoinAcceptReceived(JoinAcceptPacket packet, NetPeer peer) {
            Debug.Log($"[{TAG}] Join Accepted! My ID: {packet.ClientId}, Server Tick: {packet.ServerTick}, Map: {packet.MapName}");
            LocalClientId = packet.ClientId;
            CurrentTick = packet.ServerTick;
            OnJoinAccept?.Invoke(packet);
            
            SceneManager.LoadScene(packet.MapName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[{TAG}] Scene loaded, sending ClientReadyPacket.");
            SendPacket(new ClientReadyPacket(), DeliveryMethod.ReliableOrdered);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion
    }
}
#endif