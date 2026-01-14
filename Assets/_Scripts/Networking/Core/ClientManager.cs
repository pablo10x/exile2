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
        [Header("Settings")]
        public string ConnectionKey = "ExileSurvival_Dev";
        public int TickRate = 60;
        
        public NetManager Client { get; private set; }
        public NetPacketProcessor PacketProcessor { get; private set; }
        public int LocalClientId { get; private set; } = -1;
        
        // Tick System
        public uint CurrentTick { get; private set; }
        private float _tickTimer;
        private float _tickInterval;
        
        private EventBasedNetListener _listener;
        private NetDataWriter _writer;

        public bool IsConnected => Client != null && Client.FirstPeer != null && Client.FirstPeer.ConnectionState == ConnectionState.Connected;

        // Events
        public event Action<PlayerStatePacket> OnClientReceivedState;
        public event Action<JoinAcceptPacket> OnJoinAccept;

        private void Awake()
        {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            
            _tickInterval = 1f / TickRate;
            _writer = new NetDataWriter();
            PacketProcessor = new NetPacketProcessor();
            
            NetworkCommon.RegisterPackets(PacketProcessor);
            SubscribePackets();
        }

        private void SubscribePackets()
        {
            PacketProcessor.SubscribeNetSerializable<PlayerStatePacket, NetPeer>(OnStateReceived);
            PacketProcessor.SubscribeNetSerializable<JoinAcceptPacket, NetPeer>(OnJoinAcceptReceived);
        }

        public void StartClient(string address, int port)
        {
            if (Client == null)
            {
                _listener = new EventBasedNetListener();
                _listener.PeerConnectedEvent += peer => 
                {
                    Debug.Log("Connected to server!");
                };
                
                _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
                {
                    PacketProcessor.ReadAllPackets(dataReader, fromPeer);
                };
                
                _listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
                {
                    Debug.Log($"Disconnected from server: {disconnectInfo.Reason}");
                    LocalClientId = -1;
                };

                Client = new NetManager(_listener);
                Client.Start();
            }

            Connect(address, port, ConnectionKey);
        }

        public void Connect(string address, int port, string key)
        {
            if (Client == null)
            {
                StartClient(address, port);
                return;
            }

            if (Client.FirstPeer == null || Client.FirstPeer.ConnectionState == ConnectionState.Disconnected)
            {
                Client.Connect(address, port, key);
            }
        }

        public void StopClient()
        {
            Client?.Stop();
            Client = null;
            LocalClientId = -1;
        }

        private void Update()
        {
            Client?.PollEvents();
            
            if (IsConnected)
            {
                ClientTickLogic();
            }
        }
        
        private void ClientTickLogic() {
            _tickTimer += Time.deltaTime;
            while (_tickTimer >= _tickInterval) {
                _tickTimer -= _tickInterval;
                CurrentTick++;
            }
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new()
        {
            if (Client == null || Client.FirstPeer == null) return;
            
            _writer.Reset();
            PacketProcessor.WriteNetSerializable(_writer, ref packet);
            Client.FirstPeer.Send(_writer, deliveryMethod);
        }

        private void OnStateReceived(PlayerStatePacket packet, NetPeer peer) {
            OnClientReceivedState?.Invoke(packet);
        }

        private void OnJoinAcceptReceived(JoinAcceptPacket packet, NetPeer peer) {
            Debug.Log($"Join Accepted! My ID: {packet.ClientId}, Server Tick: {packet.ServerTick}, Map: {packet.MapName}");
            LocalClientId = packet.ClientId;
            CurrentTick = packet.ServerTick; // Sync tick
            OnJoinAccept?.Invoke(packet);
            
            // Load the map scene
            SceneManager.LoadScene(packet.MapName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Now that the scene is loaded, tell the server we are ready
            Debug.Log("Scene loaded, sending ClientReadyPacket.");
            SendPacket(new ClientReadyPacket(), DeliveryMethod.ReliableOrdered);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        [Button("Start Client")]
        private void StartC() {
            StartClient("localhost", 9050);
        }

        [Button("Stop Client")]
        private void StopC() {
            StopClient();
        }
    }
}