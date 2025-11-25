using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System;
using System.Collections.Generic;
using FishNet.Serializing;

namespace Exile.Inventory.Network {
    /// <summary>
    /// Networked wrapper for IInventoryItem that syncs across clients
    /// </summary>
    [Serializable]
    public struct NetworkedItemData {
        public string ItemName;
        public int ItemId; // The ScriptableObject instance ID or unique identifier
        public int RuntimeID;
        public int Width;
        public int Height;
        public Vector2Int Position;
        public bool Rotated;
        public int Quantity;
        public float Durability;
        public float MaxDurability;
        public bool Stackable;
        public int MaxQuantity;
        public ItemTier ItemTier;
        public ItemType ItemType;
        public bool CanDrop;
        public bool UseDurability;
        public bool IsContainer;
        
        public NetworkedItemData(IInventoryItem item) {
            var itemBase = item as ItemBase;
            
            ItemName = item.ItemName;
            ItemId = itemBase != null ? itemBase.Id : -1;
            RuntimeID = item.RuntimeID;
            Width = item.width;
            Height = item.height;
            Position = item.position;
            Rotated = item.Rotated;
            Quantity = item.Quantity;
            Durability = item.Durability;
            MaxDurability = item.MaxDurability;
            Stackable = item.Stackable;
            MaxQuantity = item.maxQuantity;
            ItemTier = item.ItemTier;
            ItemType = itemBase != null ? itemBase.Type : ItemType.Any;
            CanDrop = item.canDrop;
            UseDurability = item.useDurability;
            IsContainer = itemBase != null && itemBase.isContainer;
        }
        
        public void ApplyToItem(IInventoryItem item) {
            item.position = Position;
            item.Rotated = Rotated;
            item.Quantity = Quantity;
            item.Durability = Durability;
            item.width = Width;
            item.height = Height;
            
            // Apply ItemBase specific properties
            if (item is ItemBase itemBase) {
                itemBase.canDrop = CanDrop;
            }
        }

       

      
    }

    [Serializable]
    public struct NetWorkedInventoryData {
        public int InventoryID;
        public int InventoryHeight;
        public int InventoryWidth;
        public List<NetworkedItemData> allitems;

        public NetWorkedInventoryData(int id, int height, int width, List<NetworkedItemData> items) {
            InventoryID = id;
            InventoryHeight = height;
            InventoryWidth = width;
            allitems = items;
        }
    }
}