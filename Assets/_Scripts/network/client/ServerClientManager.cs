using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using UnityEngine;

namespace core.network.client
{
    public class ServerClientManager : MonoBehaviour, INetEventListener
    {
        private NetManager _netClient;

        void Start()
        {
            // Initialize NetManager
            _netClient = new NetManager(this)
            {
                AutoRecycle = true,
                PingInterval = 2000,        // 2 seconds
                ReconnectDelay = 2000,
                MaxConnectAttempts = 15,
                DisconnectTimeout = 10 * 1000, // 10 seconds
                NatPunchEnabled = true
            };

            // Start the client
            _netClient.Start();
            
            // Connect to the server
            _netClient.Connect("localhost" /* host ip or name */, 8080 /* port */, "key" /* text key or NetDataWriter */);
        }

        // Update is called once per frame
        void Update()
        {
            // Poll events in the update loop
            _netClient.PollEvents();
        }

        // Called when a peer is connected
        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log($"Connected to {peer.Address}");
        }

        // Called when a peer is disconnected
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"[CLIENT] We disconnected because {disconnectInfo.Reason}");
        }

        // Called on network error
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log($"[CLIENT] Received error {socketError}");
        }

        // Called when a network packet is received
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            // Handle received data
        }

        // Called when an unconnected network packet is received
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // Handle unconnected network receive
        }

        // Called on network latency update
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            Debug.Log($"Latency update: {latency}");
        }

        // Called when a connection request is received
        public void OnConnectionRequest(ConnectionRequest request)
        {
            // Handle connection request
        }

        // Called when the script is destroyed
        void OnDestroy()
        {
            // Stop the client
            _netClient?.Stop();
        }
    }
}
