#if UNITY_SERVER || UNITY_EDITOR
using System;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExileSurvival.Networking.Data;
using Sirenix.OdinInspector;

namespace ExileSurvival.Networking.Core 
{
    public class ServerManager : Singleton<ServerManager> 
    {
        #region Inspector Fields
        [BoxGroup("Server Configuration")]
        [Tooltip("The name of the server that will appear in the server browser.")]
        public string ServerName = "Exile Survival Server";
        
        [BoxGroup("Server Configuration")]
        [Tooltip("A secret key that clients must have to connect.")]
        public string ConnectionKey = "ExileSurvival_Dev";
        
        [BoxGroup("Server Configuration")]
        [Tooltip("The network port the server will listen on.")]
        public int Port = 9050;
        
        [BoxGroup("Server Configuration")]
        [Tooltip("The maximum number of players that can connect.")]
        public int MaxPlayers = 32;
        
        [BoxGroup("Server Configuration")]
        [Tooltip("How many times per second the server will tick.")]
        public int TickRate = 60;
        #endregion

        #region Public Properties
        public NetManager Server { get; private set; }
        public NetPacketProcessor PacketProcessor { get; private set; }
        public uint CurrentTick { get; private set; }
        public bool IsServerRunning => Server != null && Server.IsRunning;
        public string MapName => SceneManager.GetActiveScene().name;
        #endregion

        #region Events
        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;
        public event Action<PlayerInputPacket, int> OnServerReceivedInput;
        public event Action<uint> OnServerTick;
        public event Action<int> OnClientReady;
        #endregion

        #region Private Fields
        private EventBasedNetListener _serverListener;
        private float _tickTimer;
        private float _tickInterval;
        private bool _isQuitting = false;
        private const string TAG = "ServerManager";
        #endregion
        
        #region Odin Inspector
        [BoxGroup("Server Control")]
        [Button("Start Server"), GUIColor(0, 1, 0), PropertyOrder(-1)]
        [HideIf("IsServerRunning")]
        private void StartServerButton() => StartServer();

        [BoxGroup("Server Control")]
        [Button("Stop Server"), GUIColor(1, 0, 0), PropertyOrder(-1)]
        [EnableIf("IsServerRunning")]
        private void StopServerButton() => StopServer();

        [BoxGroup("Live Server Info")]
        [ShowInInspector, ReadOnly, ShowIf("@IsServerRunning")]
        private int ConnectedPlayers => Server?.ConnectedPeersCount ?? 0;
        
        [BoxGroup("Live Server Info")]
        [ShowInInspector, ReadOnly, ShowIf("@IsServerRunning")]
        private uint ServerTick => CurrentTick;
        #endregion

        #region Unity Lifecycle
        private void OnApplicationQuit() => _isQuitting = true;

        private void Awake() {
           

            _tickInterval = 1f / TickRate;
            PacketProcessor = new NetPacketProcessor();
            NetworkCommon.RegisterPackets(PacketProcessor);
            SubscribeToPacketProcessor();
        }

        private void Update() {
            if (_isQuitting || !IsServerRunning) return;
            
            Server.PollEvents();
            ServerTickLogic();
        }
        #endregion

        #region Public API
        public void StartServer() {
            if (IsServerRunning) return;

            _serverListener = new EventBasedNetListener();
            SetupListeners();

            Server = new NetManager(_serverListener) {
                UnconnectedMessagesEnabled = true
            };
            Server.Start(Port);
            Debug.Log($"[{TAG}] Server started on port {Port}");
        }

        public void StopServer() {
            Server?.Stop();
            Server = null;
            Debug.Log($"[{TAG}] Server stopped.");
        }

        public void BroadcastToAll<T>(T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new() {
            if (!IsServerRunning) return;
            NetDataWriter writer = new NetDataWriter();
            PacketProcessor.WriteNetSerializable(writer, ref packet);
            Server.SendToAll(writer, deliveryMethod);
        }

        public void SendToClient<T>(int clientId, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new() {
            if (!IsServerRunning) return;
            var peer = Server.GetPeerById(clientId);
            if (peer != null) {
                NetDataWriter writer = new NetDataWriter();
                PacketProcessor.WriteNetSerializable(writer, ref packet);
                peer.Send(writer, deliveryMethod);
            }
        }
        #endregion

        #region Listeners
        private void SetupListeners()
        {
            _serverListener.ConnectionRequestEvent += request => {
                if (Server.ConnectedPeersCount < MaxPlayers)
                    request.AcceptIfKey(ConnectionKey);
                else
                    request.Reject();
            };

            _serverListener.PeerConnectedEvent += peer => {
                Debug.Log($"[{TAG}] Client connected with ID: {peer.Id}");
                OnClientConnected?.Invoke(peer.Id);
                var acceptPacket = new JoinAcceptPacket { 
                    ClientId = peer.Id, 
                    ServerTick = CurrentTick,
                    MapName = this.MapName
                };
                SendToClient(peer.Id, acceptPacket, DeliveryMethod.ReliableOrdered);
            };

            _serverListener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
                Debug.Log($"[{TAG}] Client disconnected: {peer.Id}. Reason: {disconnectInfo.Reason}");
                OnClientDisconnected?.Invoke(peer.Id);
            };

            _serverListener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) => { 
                PacketProcessor.ReadAllPackets(dataReader, fromPeer); 
            };
            
            _serverListener.NetworkReceiveUnconnectedEvent += (remoteEndPoint, reader, messageType) =>
            {
                if (messageType == UnconnectedMessageType.Broadcast)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put(ServerName);
                    writer.Put(Server.ConnectedPeersCount);
                    writer.Put(MaxPlayers);
                    Server.SendUnconnectedMessage(writer, remoteEndPoint);
                }
            };
        }

        private void SubscribeToPacketProcessor() {
            PacketProcessor.SubscribeNetSerializable<PlayerInputPacket, NetPeer>(OnInputReceived);
            PacketProcessor.SubscribeNetSerializable<ClientReadyPacket, NetPeer>(OnClientReadyReceived);
        }
        #endregion

        #region Internal Logic
        private void ServerTickLogic() {
            _tickTimer += Time.deltaTime;
            while (_tickTimer >= _tickInterval) {
                _tickTimer -= _tickInterval;
                CurrentTick++;
                OnServerTick?.Invoke(CurrentTick);
            }
        }

        private void OnInputReceived(PlayerInputPacket packet, NetPeer peer) => OnServerReceivedInput?.Invoke(packet, peer.Id);
        private void OnClientReadyReceived(ClientReadyPacket packet, NetPeer peer)
        {
            Debug.Log($"[{TAG}] Client {peer.Id} is ready.");
            OnClientReady?.Invoke(peer.Id);
        }
        #endregion
    }
}
#endif