using System;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ExileSurvival.Networking.Data;
using Sirenix.OdinInspector;

namespace ExileSurvival.Networking.Core {
    public class ServerManager : Singleton<ServerManager> {
        [Header("Settings")] 
        public string ServerName = "Exile Survival Server";
        public string MapName = "MainMap";
        public string ConnectionKey = "ExileSurvival_Dev";
        public int Port = 9050;
        public int MaxPlayers = 32;
        public int TickRate = 60; // Server ticks per second

        // LiteNetLib components
        public NetManager Server { get; private set; }
        public NetPacketProcessor PacketProcessor { get; private set; }

        private EventBasedNetListener _serverListener;

        // Tick System
        public uint CurrentTick { get; private set; }
        private float _tickTimer;
        private float _tickInterval;

        public bool IsServer => Server != null && Server.IsRunning;

        // Events
        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;
        public event Action<PlayerInputPacket, int> OnServerReceivedInput; // Input, ClientID
        public event Action<uint> OnServerTick; // Event for server tick logic
        public event Action<int> OnClientReady; // Client has loaded the map

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);

            _tickInterval = 1f / TickRate;
            PacketProcessor = new NetPacketProcessor();

            // Register Packets
            NetworkCommon.RegisterPackets(PacketProcessor);
            SubscribePackets();
        }

        private void SubscribePackets() {
            PacketProcessor.SubscribeNetSerializable<PlayerInputPacket, NetPeer>(OnInputReceived);
            PacketProcessor.SubscribeNetSerializable<ClientReadyPacket, NetPeer>(OnClientReadyReceived);
        }

        private void Update() {
            Server?.PollEvents();

            if (IsServer) {
                ServerTickLogic();
            }
        }

        // --- Server Logic ---

        public void StartServer() {
            if (Server != null) return;

            _serverListener = new EventBasedNetListener();
            _serverListener.ConnectionRequestEvent += request => {
                if (Server.ConnectedPeersCount < MaxPlayers)
                    request.AcceptIfKey(ConnectionKey);
                else
                    request.Reject();
            };

            _serverListener.PeerConnectedEvent += peer => {
                Debug.Log($"Client connected: {peer.Id}");
                OnClientConnected?.Invoke(peer.Id);

                var acceptPacket = new JoinAcceptPacket { 
                    ClientId = peer.Id, 
                    ServerTick = CurrentTick,
                    MapName = this.MapName
                };
                SendToClient(peer.Id, acceptPacket, DeliveryMethod.ReliableOrdered);
            };

            _serverListener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
                Debug.Log($"Client disconnected: {peer.Id} Reason: {disconnectInfo.Reason}");
                OnClientDisconnected?.Invoke(peer.Id);
            };

            _serverListener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) => { 
                PacketProcessor.ReadAllPackets(dataReader, fromPeer); 
            };
            
            _serverListener.NetworkReceiveUnconnectedEvent += (remoteEndPoint, reader, messageType) =>
            {
                // Handle server browser pings
                if (messageType == UnconnectedMessageType.Broadcast)
                {
                    Debug.Log("Received discovery request");
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put(ServerName);
                    writer.Put(Server.ConnectedPeersCount);
                    writer.Put(MaxPlayers);
                    // TODO: Add other info like if user is banned, etc.
                    Server.SendUnconnectedMessage(writer, remoteEndPoint);
                }
            };

            Server = new NetManager(_serverListener);
            Server.Start(Port);
            Server.UnconnectedMessagesEnabled = true;
            Debug.Log($"Server started on port {Port}");
        }

        public void StopServer() {
            Server?.Stop();
            Server = null;
        }

        private void ServerTickLogic() {
            _tickTimer += Time.deltaTime;
            while (_tickTimer >= _tickInterval) {
                _tickTimer -= _tickInterval;
                CurrentTick++;
                OnServerTick?.Invoke(CurrentTick);
            }
        }

        private void OnInputReceived(PlayerInputPacket packet, NetPeer peer) {
            OnServerReceivedInput?.Invoke(packet, peer.Id);
        }

        private void OnClientReadyReceived(ClientReadyPacket packet, NetPeer peer) {
            Debug.Log($"Client {peer.Id} is ready.");
            OnClientReady?.Invoke(peer.Id);
        }

        // --- Helper Methods ---

        public void BroadcastToAll<T>(T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new() {
            if (Server == null) return;

            NetDataWriter writer = new NetDataWriter();
            PacketProcessor.WriteNetSerializable(writer, ref packet);
            Server.SendToAll(writer, deliveryMethod);
        }

        public void SendToClient<T>(int clientId, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new() {
            if (Server == null) return;
            var peer = Server.GetPeerById(clientId);
            if (peer != null) {
                NetDataWriter writer = new NetDataWriter();
                PacketProcessor.WriteNetSerializable(writer, ref packet);
                peer.Send(writer, deliveryMethod);
            }
        }

        [Button("Start server")]
        private void StartS() {
            StartServer();
        }

        [Button("Stop server")]
        private void StopS() {
            StopServer();
        }
    }
}