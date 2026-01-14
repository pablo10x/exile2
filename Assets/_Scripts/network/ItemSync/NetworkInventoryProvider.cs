using System.Collections.Generic;
using Exile.Inventory;
using UnityEngine;

namespace Exile.Inventory.Network {
    public class NetworkInventoryProvider : IInventoryProvider {
        // Changed to IList to hide it from FishNet weaver (SyncList implements IList)
        private readonly List<NetworkedItemData> _syncList;
        private readonly ItemDatabase _itemDatabase;
        private readonly Dictionary<int, IInventoryItem> _itemCache = new Dictionary<int, IInventoryItem>();
        
        private readonly InventoryRenderMode _renderMode;
        private readonly int _maxItems;
        private readonly ItemType _allowedType;

        public NetworkInventoryProvider(List<NetworkedItemData> syncList, ItemDatabase database, InventoryRenderMode renderMode, int maxItems = -1, ItemType allowedType = ItemType.Any) {
            _syncList = syncList;
            _itemDatabase = database;
            _renderMode = renderMode;
            _maxItems = maxItems;
            _allowedType = allowedType;
        }

        public InventoryRenderMode inventoryRenderMode => _renderMode;

        public int inventoryItemCount => _syncList.Count;

        public bool isInventoryFull {
            get {
                if (_maxItems < 0) return false;
                return inventoryItemCount >= _maxItems;
            }
        }

        public IInventoryItem GetInventoryItem(int index) {
            if (index < 0 || index >= _syncList.Count) return null;

            var data = _syncList[index];

            // Try to find in cache first
            if (_itemCache.TryGetValue(data.RuntimeID, out var cachedItem)) {
                // Update properties in case they changed on network
                data.ApplyToItem(cachedItem);
                return cachedItem;
            }

            // Create new instance
            var template = _itemDatabase.GetItem(data.ItemId);
            if (template == null) {
                Debug.LogError($"NetworkInventoryProvider: Could not find item template for ID {data.ItemId}");
                return null;
            }

            var newItem = template.CreateInstance(data.RuntimeID);
            data.ApplyToItem(newItem);
            
            // Cache it
            _itemCache[data.RuntimeID] = newItem;
            
            return newItem;
        }

        public bool CanAddInventoryItem(IInventoryItem item) {
            if (_allowedType != ItemType.Any && item is ItemBase ib && ib.Type != _allowedType) return false;
            return !isInventoryFull;
        }

        public bool CanRemoveInventoryItem(IInventoryItem item) => true;

        public bool CanDropInventoryItem(IInventoryItem item) => true;

        public bool AddInventoryItem(IInventoryItem item) {
            // Check if already in list (shouldn't happen with correct logic but safety check)
            if (Contains(item)) return false;

            // Convert to Network Data
            var netData = new NetworkedItemData(item);
            
            // Add to SyncList
            _syncList.Add(netData);
            
            // Add to local cache
            _itemCache[item.RuntimeID] = item;
            
            return true;
        }

        public bool RemoveInventoryItem(IInventoryItem item) {
            int index = FindIndex(x => x.RuntimeID == item.RuntimeID);
            if (index != -1) {
                _syncList.RemoveAt(index);
                _itemCache.Remove(item.RuntimeID);
                return true;
            }
            return false;
        }

        public bool DropInventoryItem(IInventoryItem item) {
             return RemoveInventoryItem(item);
        }

        // Helper to check existence
        private bool Contains(IInventoryItem item) {
            return FindIndex(x => x.RuntimeID == item.RuntimeID) != -1;
        }
        
        /// <summary>
        /// Updates an existing item in the SyncList to reflect changes (e.g. position, quantity).
        /// This should be called when the logic modifies an item in place.
        /// </summary>
        public void UpdateInventoryItem(IInventoryItem item) {
            int index = FindIndex(x => x.RuntimeID == item.RuntimeID);
            if (index != -1) {
                // Replace the struct with updated data
                _syncList[index] = new NetworkedItemData(item);
                
            }
        }
        
        /// <summary>
        /// Clears the cache. Useful when forcing a full rebuild or reset.
        /// </summary>
        public void ClearCache() {
            _itemCache.Clear();
           
        }

        // Helper method since IList doesn't have FindIndex
        private int FindIndex(System.Predicate<NetworkedItemData> match) {
            for (int i = 0; i < _syncList.Count; i++) {
                if (match(_syncList[i])) {
                    return i;
                }
            }
            return -1;
        }
    }
}
