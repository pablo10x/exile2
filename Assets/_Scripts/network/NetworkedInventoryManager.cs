using System;
using Exile.Inventory;
using FishNet.Object;
using UnityEngine;

namespace core.network {
    public class NetworkedInventoryManager : NetworkBehaviour {
        
        public InventoryManager inventoryManager;

        public InventoryView inventoryView;
        private void Awake() {
           inventoryView.OnInvetoryViewCreated += () => {
                           inventoryManager = inventoryView.InventoryManager;
                           inventoryManager.onItemAdded += OnItemAdded;
                           inventoryManager.onItemChanged += OnItemChanged;
                       };
        }

        private void Start() {
            
            
        }

        private void OnItemChanged(IInventoryItem item) {
        }

        private void OnItemAdded(IInventoryItem item) {
            CMD_ItemAdded(item.ID,item.position);
        }


        [ServerRpc(RequireOwnership = false)]
        private void CMD_ItemAdded(int itemID,Vector2Int position) {

            Debug.Log($" SERVER:: item added: {itemID}");
        }
    }
}