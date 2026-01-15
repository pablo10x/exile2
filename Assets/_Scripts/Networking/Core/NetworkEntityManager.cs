using System.Collections.Generic;
using UnityEngine;
using ExileSurvival.Networking.Data;
using ExileSurvival.Networking.Entities;

namespace ExileSurvival.Networking.Core
{
    public class NetworkEntityManager : Singleton<NetworkEntityManager>
    {
        private readonly Dictionary<int, NetworkEntity> _entities = new Dictionary<int, NetworkEntity>();
        private readonly Dictionary<int, NetworkEntity> _playerEntities = new Dictionary<int, NetworkEntity>(); // ClientId -> PlayerEntity

        public IReadOnlyCollection<NetworkEntity> GetAllEntities() => _entities.Values;

        public NetworkEntity GetEntity(int entityId)
        {
            _entities.TryGetValue(entityId, out var entity);
            return entity;
        }

        public NetworkEntity GetPlayerEntity(int clientId)
        {
            _playerEntities.TryGetValue(clientId, out var entity);
            return entity;
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.OnServerReceivedInput += OnServerInput;
                ServerManager.Instance.OnClientDisconnected += OnClientDisconnected;
            }

            if (ClientManager.Instance != null)
            {
                ClientManager.Instance.OnClientReceivedState += OnClientState;
            }
        }

        public void RegisterEntity(NetworkEntity entity)
        {
            if (entity.EntityId != -1 && !_entities.ContainsKey(entity.EntityId))
            {
                _entities.Add(entity.EntityId, entity);
            }
            
            if (entity.OwnerId != -1)
            {
                _playerEntities[entity.OwnerId] = entity;
            }
        }

        public void UnregisterEntity(NetworkEntity entity)
        {
            if (_entities.ContainsKey(entity.EntityId))
            {
                _entities.Remove(entity.EntityId);
            }
            
            if (entity.OwnerId != -1 && _playerEntities.ContainsKey(entity.OwnerId))
            {
                _playerEntities.Remove(entity.OwnerId);
            }
        }

        private void OnServerInput(PlayerInputPacket packet, int clientId)
        {
            if (_playerEntities.TryGetValue(clientId, out var entity))
            {
                if (entity is PredictedEntity predictedEntity)
                {
                    predictedEntity.OnServerInput(packet);
                }
            }
        }

        private void OnClientState(PlayerStatePacket packet)
        {
            if (_playerEntities.TryGetValue(packet.PlayerId, out var entity))
            {
                if (entity is PredictedEntity predictedEntity)
                {
                    predictedEntity.OnClientState(packet);
                }
            }
        }

        private void OnClientDisconnected(int clientId)
        {
            if (_playerEntities.TryGetValue(clientId, out var entity))
            {
                _playerEntities.Remove(clientId);
            }
        }
    }
}