using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ExileSurvival.Networking.Core
{
    public class ServerBrowser : MonoBehaviour, INetEventListener
    {
        public Action<IPEndPoint, string> OnServerFound;

        private NetManager _client;
        private NetDataWriter _writer;

        private void Awake()
        {
            _writer = new NetDataWriter();
            _client = new NetManager(this)
            {
                UnconnectedMessagesEnabled = true
            };
        }

        public void DiscoverServers()
        {
            Debug.Log("Discovering local servers...");
            _client.Start();
            _client.SendBroadcast(new byte[] {1}, 9050); // Port should match server
        }

        private void Update()
        {
            _client?.PollEvents();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.Broadcast)
            {
                var serverName = reader.GetString();
                OnServerFound?.Invoke(remoteEndPoint, serverName);
                // In a real browser, you would add to a list and not stop immediately.
                _client.Stop();
            }
        }

        public void OnPeerConnected(NetPeer peer) { }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) { }

        private void OnDestroy()
        {
            _client?.Stop();
        }
    }
}