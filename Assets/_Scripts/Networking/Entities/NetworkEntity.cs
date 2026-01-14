using UnityEngine;
using ExileSurvival.Networking.Core;

namespace ExileSurvival.Networking.Entities
{
    public class NetworkEntity : MonoBehaviour
    {
        public int OwnerId { get; set; } = -1;
        public int EntityId { get; set; } = -1; // Unique ID for network replication
        public byte TypeId { get; set; } // 0 = Player, 1 = Enemy, 2 = Container, etc.
        
        public bool IsOwnedByServer => OwnerId == -1;
        
        // Helper to check if this entity belongs to the local client
        public bool IsLocalPlayer 
        {
            get 
            {
                if (ClientManager.Instance != null)
                {
                    return ClientManager.Instance.LocalClientId == OwnerId;
                }
                return false;
            }
        }

        protected virtual void Start()
        {
            if (NetworkEntityManager.Instance != null)
            {
                NetworkEntityManager.Instance.RegisterEntity(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (NetworkEntityManager.Instance != null)
            {
                NetworkEntityManager.Instance.UnregisterEntity(this);
            }
        }
    }
}