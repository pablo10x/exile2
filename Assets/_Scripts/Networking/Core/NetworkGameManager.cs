using UnityEngine;
using ExileSurvival.Networking.Entities;
using ExileSurvival.Networking.Data;
using LiteNetLib;

namespace ExileSurvival.Networking.Core
{
    public class NetworkGameManager : Singleton<NetworkGameManager>
    {
        [Header("Prefabs")]
        public NetworkEntity PlayerPrefab;
        // Enemy and Item prefabs should be handled by a server-side spawner system, not this manager.

        private int _nextEntityId = 1;

        private void Start()
        {
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.OnClientReady += OnClientReady;
            }
            
            if (ClientManager.Instance != null)
            {
                ClientManager.Instance.PacketProcessor.SubscribeNetSerializable<SpawnPacket, NetPeer>(OnSpawnPacketReceived);
                ClientManager.Instance.PacketProcessor.SubscribeNetSerializable<EntityDestroyPacket, NetPeer>(OnEntityDestroyReceived);
            }
        }

        private void OnClientReady(int clientId)
        {
            // Spawn Player for the new client
            SpawnPlayer(clientId, new Vector3(0, 5, 0));
        }

        public void SpawnPlayer(int clientId, Vector3 position)
        {
            if (PlayerPrefab == null) return;

            // 1. Instantiate on Server
            var player = Instantiate(PlayerPrefab, position, Quaternion.identity);
            
            // 2. Assign IDs
            player.OwnerId = clientId;
            player.EntityId = GenerateEntityId();
            player.TypeId = 0; // 0 for Player
            
            // 3. Register
            if (NetworkEntityManager.Instance != null)
                NetworkEntityManager.Instance.RegisterEntity(player);
            
            // The InterestManager will handle sending the spawn packet to other clients.
        }

        private void OnSpawnPacketReceived(SpawnPacket packet, NetPeer peer)
        {
            // Client side spawning
            NetworkEntity prefab = null;
            if (packet.TypeId == 0) // Player
            {
                prefab = PlayerPrefab;
            }
            // TODO: Add a way to get prefabs for other types (e.g. from a dictionary or addressables)
            
            if (prefab != null)
            {
                var entity = Instantiate(prefab, packet.Position, packet.Rotation);
                entity.EntityId = packet.EntityId;
                entity.OwnerId = packet.OwnerId;
                entity.TypeId = packet.TypeId;
                
                if (NetworkEntityManager.Instance != null)
                    NetworkEntityManager.Instance.RegisterEntity(entity);
            }
        }

        private void OnEntityDestroyReceived(EntityDestroyPacket packet, NetPeer peer)
        {
            // Find and destroy the entity
            // This is a simplified approach. A more robust system would use a dictionary for faster lookups.
            foreach (var entity in FindObjectsOfType<NetworkEntity>())
            {
                if (entity.EntityId == packet.EntityId)
                {
                    Destroy(entity.gameObject);
                    break;
                }
            }
        }
        
        private int GenerateEntityId() => _nextEntityId++;
    }
}