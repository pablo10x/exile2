using UnityEngine;
using ExileSurvival.Networking.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ExileSurvival.Networking.Entities
{
    public class NetworkEntity : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Networking Properties")]
        [Tooltip("The unique asset GUID of this prefab. This is set automatically in the editor and should not be changed.")]
        [SerializeField] private string _prefabGuid;
        
        [Tooltip("Is this entity a player character? This tells the Interest Manager that this entity is an 'observer'.")]
        public bool IsPlayer = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// The unique asset GUID of this prefab, used by the server to know what to spawn on clients.
        /// </summary>
        public string PrefabGuid => _prefabGuid;

        /// <summary>
        /// The ClientId of the player who owns this entity. -1 indicates it is owned by the server.
        /// </summary>
        public int OwnerId { get; private set; } = -1;

        /// <summary>
        /// The unique ID for this specific instance of the entity in the world.
        /// </summary>
        public int EntityId { get; private set; } = -1;
        
        /// <summary>
        /// Returns true if this entity is owned and controlled by the server (e.g., AI, world objects).
        /// </summary>
        public bool IsOwnedByServer => OwnerId == -1;
        
        /// <summary>
        /// Returns true if this entity is the one controlled by the local machine's player.
        /// This is the most common check for enabling input and cameras.
        /// </summary>
        public bool IsLocalPlayer 
        {
            get 
            {
                // If the ClientManager doesn't exist, we can't be a local player.
                if (ClientManager.Instance == null) return false;
                
                // We are the local player if our client ID matches this entity's owner ID.
                return ClientManager.Instance.LocalClientId == OwnerId;
            }
        }
        #endregion

        #region Initialization and Destruction
        /// <summary>
        /// Initializes the core network properties of this entity.
        /// This is called by the server immediately after instantiation and then replicated to clients.
        /// </summary>
        /// <param name="entityId">The unique instance ID for this entity.</param>
        /// <param name="ownerId">The client ID of the owning player, or -1 if server-owned.</param>
        public void Initialize(int entityId, int ownerId = -1)
        {
            this.EntityId = entityId;
            this.OwnerId = ownerId;

            if (NetworkEntityManager.Instance != null)
            {
                NetworkEntityManager.Instance.RegisterEntity(this);
                // Debug.Log($"Initialized and Registered Entity {EntityId} (Prefab: {PrefabGuid}, Owner: {OwnerId})");
            }
            else
            {
                Debug.LogError($"[{nameof(NetworkEntity)}] NetworkEntityManager not found! Could not register Entity {EntityId}.");
            }
            
            OnNetworkInitialize();
        }

        /// <summary>
        /// Called on both server and client after the entity has been initialized with its network IDs.
        /// Override this to perform game-specific setup like enabling cameras or attaching to UI.
        /// </summary>
        protected virtual void OnNetworkInitialize() { }

        protected virtual void OnDestroy()
        {
            if (NetworkEntityManager.Instance != null)
            {
                NetworkEntityManager.Instance.UnregisterEntity(this);
            }
            
            if (InterestManager.Instance != null && IsOwnedByServer)
            {
                InterestManager.Instance.RemoveEntity(this);
            }
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        /// <summary>
        /// Automatically sets the prefab GUID in the editor when the prefab is saved.
        /// </summary>
        private void OnValidate()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(this))
            {
                var path = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(path)) return; // Avoids errors on newly created, unsaved prefabs
                
                var newGuid = AssetDatabase.AssetPathToGUID(path);
                if (_prefabGuid != newGuid)
                {
                    _prefabGuid = newGuid;
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
        #endregion
    }
}