using UnityEngine;
using Exile.Inventory;
using ExileSurvival.Networking.Core;
using ExileSurvival.Networking.Entities;

namespace ExileSurvival.Networking.Components
{
    public class NetworkedInventory : NetworkEntity, IInventoryProvider
    {
        [Header("Inventory Settings")]
        public int Width = 5;
        public int Height = 5;
        public string InventoryName = "Container";
        
        public InventoryManager Inventory { get; private set; }
        
        // IInventoryProvider Implementation
        public int inventoryItemCount => 0; // Initial count, usually 0 or loaded from save
        public InventoryRenderMode inventoryRenderMode => InventoryRenderMode.Grid;
        public bool isInventoryFull => false;

        protected  void Start()
        {
          
            
            // Initialize InventoryManager
            // Use EntityId as the NetworkInventoryId
            Inventory = new InventoryManager(EntityId, this, Width, Height, true, InventoryName);
            
            // Register with Network Manager
            if (InventoryNetManager.Instance != null)
            {
                InventoryNetManager.Instance.RegisterInventory(Inventory);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (InventoryNetManager.Instance != null && Inventory != null)
            {
                InventoryNetManager.Instance.UnregisterInventory(Inventory.NetworkInventoryId);
            }
        }

        // IInventoryProvider Methods
        public IInventoryItem GetInventoryItem(int index) => null;
        public bool AddInventoryItem(IInventoryItem item) => true; // Logic handled by InventoryManager
        public bool RemoveInventoryItem(IInventoryItem item) => true;
        public bool DropInventoryItem(IInventoryItem item) => true;
        public bool CanAddInventoryItem(IInventoryItem item) => true;
        public bool CanRemoveInventoryItem(IInventoryItem item) => true;
        public bool CanDropInventoryItem(IInventoryItem item) => true;
        public void OnInventoryRebuilt() { }
    }
}