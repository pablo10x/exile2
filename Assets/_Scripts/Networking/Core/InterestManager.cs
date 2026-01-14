using System.Collections.Generic;
using UnityEngine;
using ExileSurvival.Networking.Entities;
using ExileSurvival.Networking.Data;
using LiteNetLib;

namespace ExileSurvival.Networking.Core
{
    public class InterestManager : Singleton<InterestManager>
    {
        [Header("Settings")]
        public float InterestRadius = 100f; // How far players can see entities
        public float UpdateInterval = 0.1f; // How often to check for new entities in range

        private float _updateTimer;

        // Track which entities are visible to which client
        private readonly Dictionary<int, HashSet<int>> _clientVisibleEntities = new Dictionary<int, HashSet<int>>(); // ClientId -> EntityIds

        private void Start()
        {
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.OnClientConnected += OnClientConnected;
                ServerManager.Instance.OnClientDisconnected += OnClientDisconnected;
            }
        }

        private void OnClientConnected(int clientId)
        {
            if (!_clientVisibleEntities.ContainsKey(clientId))
            {
                _clientVisibleEntities.Add(clientId, new HashSet<int>());
            }
        }

        private void OnClientDisconnected(int clientId)
        {
            if (_clientVisibleEntities.ContainsKey(clientId))
            {
                _clientVisibleEntities.Remove(clientId);
            }
        }

        private void Update()
        {
            if (ServerManager.Instance == null || !ServerManager.Instance.IsServer) return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= UpdateInterval)
            {
                _updateTimer = 0f;
                UpdateEntityVisibility();
            }
        }

        private void UpdateEntityVisibility()
        {
            var players = new Dictionary<int, Vector3>(); // ClientId -> Position
            foreach (var entity in NetworkEntityManager.Instance.GetAllEntities())
            {
                if (entity.TypeId == 0) // Is a player
                {
                    players[entity.OwnerId] = entity.transform.position;
                }
            }

            foreach (var player in players)
            {
                var clientId = player.Key;
                var playerPosition = player.Value;
                var visibleEntities = _clientVisibleEntities[clientId];

                var entitiesInRange = new HashSet<int>();

                foreach (var entity in NetworkEntityManager.Instance.GetAllEntities())
                {
                    if (entity.EntityId == 0) continue; // Skip invalid entities

                    float distance = Vector3.Distance(playerPosition, entity.transform.position);
                    if (distance <= InterestRadius)
                    {
                        entitiesInRange.Add(entity.EntityId);

                        if (!visibleEntities.Contains(entity.EntityId))
                        {
                            // New entity in range, send spawn packet
                            SendSpawnPacket(clientId, entity);
                            visibleEntities.Add(entity.EntityId);
                        }
                    }
                }

                // Check for entities that are no longer in range
                var entitiesToDestroy = new HashSet<int>(visibleEntities);
                entitiesToDestroy.ExceptWith(entitiesInRange);

                foreach (var entityId in entitiesToDestroy)
                {
                    SendDestroyPacket(clientId, entityId);
                    visibleEntities.Remove(entityId);
                }
            }
        }

        private void SendSpawnPacket(int clientId, NetworkEntity entity)
        {
            var packet = new SpawnPacket
            {
                EntityId = entity.EntityId,
                OwnerId = entity.OwnerId,
                TypeId = entity.TypeId,
                Position = entity.transform.position,
                Rotation = entity.transform.rotation
            };
            ServerManager.Instance.SendToClient(clientId, packet, DeliveryMethod.ReliableOrdered);
        }

        private void SendDestroyPacket(int clientId, int entityId)
        {
            var packet = new EntityDestroyPacket { EntityId = entityId };
            ServerManager.Instance.SendToClient(clientId, packet, DeliveryMethod.ReliableOrdered);
        }
    }
}