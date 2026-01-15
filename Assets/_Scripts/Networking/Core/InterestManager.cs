#if UNITY_SERVER || UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using ExileSurvival.Networking.Entities;
using ExileSurvival.Networking.Data;
using LiteNetLib;

namespace ExileSurvival.Networking.Core
{
    public class InterestManager : Singleton<InterestManager>
    {
        #region Inspector Fields
        [Header("Performance Settings")]
        [Tooltip("How far players can 'see' entities. Entities outside this radius will be despawned on the client.")]
        public float InterestRadius = 100f;
        [Tooltip("The size of each grid cell for spatial partitioning. For best performance, this should be similar to the Interest Radius.")]
        public float CellSize = 100f;
        [Tooltip("How often, in seconds, to update entity positions in the grid and check visibility.")]
        public float UpdateInterval = 0.1f;
        #endregion

        #region Private Fields
        private float _updateTimer;
        private SpatialGrid _grid;
        private readonly Dictionary<int, HashSet<int>> _clientVisibleEntities = new Dictionary<int, HashSet<int>>();
        private bool _isQuitting = false;
        private const string TAG = "InterestManager";
        #endregion

        #region Unity Lifecycle
        private void OnApplicationQuit() => _isQuitting = true;

        private void Start()
        {
            if (ServerManager.Instance == null)
            {
                Debug.LogWarning($"[{TAG}] No ServerManager found. Disabling InterestManager.");
                gameObject.SetActive(false);
                return;
            }

            _grid = new SpatialGrid(CellSize);
            ServerManager.Instance.OnClientConnected += OnClientConnected;
            ServerManager.Instance.OnClientDisconnected += OnClientDisconnected;
        }

        private void Update()
        {
            if (_isQuitting) return;

            var serverManager = ServerManager.Instance;
            if (serverManager == null || !serverManager.IsServerRunning)
            {
                return;
            }

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= UpdateInterval)
            {
                _updateTimer = 0f;
                UpdateAllClientsVisibility();
            }
        }
        #endregion

        #region Public API
        public void AddEntity(NetworkEntity entity) => _grid.Add(entity);
        public void RemoveEntity(NetworkEntity entity) => _grid.Remove(entity);
        #endregion

        #region Event Handlers
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
        #endregion

        #region Core Logic
        private void UpdateAllClientsVisibility()
        {
            if (NetworkEntityManager.Instance == null || ServerManager.Instance.Server == null) return;

            foreach (var entity in NetworkEntityManager.Instance.GetAllEntities())
            {
                _grid.UpdateEntityPosition(entity);
            }

            foreach (var peer in ServerManager.Instance.Server)
            {
                var playerEntity = NetworkEntityManager.Instance.GetPlayerEntity(peer.Id);
                if (playerEntity != null)
                {
                    UpdateClientVisibility(peer.Id, playerEntity);
                }
            }
        }

        private void UpdateClientVisibility(int clientId, NetworkEntity playerEntity)
        {
            if (!_clientVisibleEntities.ContainsKey(clientId)) return;

            var visibleEntities = _clientVisibleEntities[clientId];
            var entitiesInRange = new HashSet<int>();
            var playerPosition = playerEntity.transform.position;

            foreach (var entity in _grid.GetEntitiesInRadius(playerPosition, InterestRadius))
            {
                if (entity == null || entity.EntityId == 0) continue;

                if (Vector3.Distance(playerPosition, entity.transform.position) <= InterestRadius)
                {
                    entitiesInRange.Add(entity.EntityId);

                    if (!visibleEntities.Contains(entity.EntityId))
                    {
                        SendSpawnPacket(clientId, entity);
                        visibleEntities.Add(entity.EntityId);
                    }
                }
            }

            var entitiesToDestroy = new HashSet<int>(visibleEntities);
            entitiesToDestroy.ExceptWith(entitiesInRange);

            foreach (var entityId in entitiesToDestroy)
            {
                SendDestroyPacket(clientId, entityId);
                visibleEntities.Remove(entityId);
            }
        }
        #endregion

        #region Packet Sending
        private void SendSpawnPacket(int clientId, NetworkEntity entity)
        {
            var packet = new SpawnPacket
            {
                EntityId = entity.EntityId,
                OwnerId = entity.OwnerId,
                PrefabGuid = entity.PrefabGuid,
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
        #endregion
    }
}
#endif