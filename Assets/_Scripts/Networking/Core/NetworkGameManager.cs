using UnityEngine;
using ExileSurvival.Networking.Entities;
using ExileSurvival.Networking.Data;
using LiteNetLib;

namespace ExileSurvival.Networking.Core
{
    public class NetworkGameManager : Singleton<NetworkGameManager>
    {
        [Header("Player Settings")]
        [Tooltip("The prefab for the player character. Must have a NetworkEntity component.")]
        public NetworkEntity PlayerPrefab;
        
        public NetworkEntity LocalPlayer { get; private set; }

        private int _nextEntityId = 1;

        private void Start()
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.OnClientReady += OnClientReady;
            }
#endif
            
#if !UNITY_SERVER || UNITY_EDITOR
            if (ClientManager.Instance != null)
            {
                ClientManager.Instance.PacketProcessor.SubscribeNetSerializable<SpawnPacket, NetPeer>(OnSpawnPacketReceived);
                ClientManager.Instance.PacketProcessor.SubscribeNetSerializable<EntityDestroyPacket, NetPeer>(OnEntityDestroyReceived);
            }
#endif
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void OnClientReady(int clientId)
        {
            SpawnPlayer(clientId, new Vector3(0, 5, 0));
        }

        public void SpawnPlayer(int clientId, Vector3 position)
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab is not assigned in NetworkGameManager.");
                return;
            }

            var player = Instantiate(PlayerPrefab, position, Quaternion.identity);
            int newEntityId = GenerateEntityId();
            player.Initialize(newEntityId, clientId);
        }
#endif

#if !UNITY_SERVER || UNITY_EDITOR
        private void OnSpawnPacketReceived(SpawnPacket packet, NetPeer peer)
        {
            var prefab = PrefabManager.Instance.GetPrefab(packet.PrefabGuid);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found for GUID: {packet.PrefabGuid}");
                return;
            }

            var entity = Instantiate(prefab, packet.Position, packet.Rotation);
            entity.Initialize(packet.EntityId, packet.OwnerId);

            if (ClientManager.Instance != null && packet.OwnerId == ClientManager.Instance.LocalClientId)
            {
                LocalPlayer = entity;
            }
        }

        private void OnEntityDestroyReceived(EntityDestroyPacket packet, NetPeer peer)
        {
            var entity = NetworkEntityManager.Instance.GetEntity(packet.EntityId);
            if (entity != null)
            {
                Destroy(entity.gameObject);
            }
        }
#endif
        
        private int GenerateEntityId() => _nextEntityId++;
    }
}