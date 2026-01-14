using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ExileSurvival.Networking.Core
{
    public class ServerBrowser : MonoBehaviour, INetEventListener
    {
        private NetManager _client;
        private NetDataWriter _writer;

        private void Start()
        {
            _writer = new NetDataWriter();
            _client = new NetManager(this);
            _client.Start();
            _client.UnconnectedMessagesEnabled = true;
        }

        public void DiscoverServers()
        {
            Debug.Log("Discovering servers...");
            _client.SendBroadcast(new byte[] {1}, 9050); // Port should match server
        }

        private void Update()
        {
            _client?.PollEvents();
        }

        public void OnPeerConnected(NetPeer peer) { }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) { }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // This is where you get the server response
            // You can deserialize your server info packet here
            var serverName = reader.GetString();
            var playerCount = reader.GetInt();
            var maxPlayers = reader.GetInt();
            
            Debug.Log($"Found Server: {serverName} ({playerCount}/{maxPlayers}) at {remoteEndPoint.Address}");
            
            // Here you would add the server to your UI list
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) { }

        private void OnDestroy()
        {
            _client?.Stop();
        }
    }
}