using UnityEngine;
using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Exile.Inventory.Network {
    /// <summary>
    /// Networked wrapper for IInventoryItem that syncs across clients
    /// </summary>
    [Serializable]
    public struct NetworkedItemData : INetSerializable {
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
        public bool iTemPickedup;
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
            iTemPickedup = false;
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

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ItemName);
            writer.Put(ItemId);
            writer.Put(RuntimeID);
            writer.Put(Width);
            writer.Put(Height);
            writer.Put(Position.x);
            writer.Put(Position.y);
            writer.Put(Rotated);
            writer.Put(Quantity);
            writer.Put(Durability);
            writer.Put(MaxDurability);
            writer.Put(Stackable);
            writer.Put(MaxQuantity);
            writer.Put((int)ItemTier);
            writer.Put((int)ItemType);
            writer.Put(CanDrop);
            writer.Put(UseDurability);
            writer.Put(IsContainer);
            writer.Put(iTemPickedup);
        }

        public void Deserialize(NetDataReader reader)
        {
            ItemName = reader.GetString();
            ItemId = reader.GetInt();
            RuntimeID = reader.GetInt();
            Width = reader.GetInt();
            Height = reader.GetInt();
            Position = new Vector2Int(reader.GetInt(), reader.GetInt());
            Rotated = reader.GetBool();
            Quantity = reader.GetInt();
            Durability = reader.GetFloat();
            MaxDurability = reader.GetFloat();
            Stackable = reader.GetBool();
            MaxQuantity = reader.GetInt();
            ItemTier = (ItemTier)reader.GetInt();
            ItemType = (ItemType)reader.GetInt();
            CanDrop = reader.GetBool();
            UseDurability = reader.GetBool();
            IsContainer = reader.GetBool();
            iTemPickedup = reader.GetBool();
        }
    }

    [Serializable]
    public struct NetWorkedInventoryData : INetSerializable {
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

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(InventoryID);
            writer.Put(InventoryHeight);
            writer.Put(InventoryWidth);
            
            writer.Put(allitems.Count);
            foreach (var item in allitems)
            {
                item.Serialize(writer);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            InventoryID = reader.GetInt();
            InventoryHeight = reader.GetInt();
            InventoryWidth = reader.GetInt();
            
            int count = reader.GetInt();
            allitems = new List<NetworkedItemData>(count);
            for (int i = 0; i < count; i++)
            {
                var item = new NetworkedItemData();
                item.Deserialize(reader);
                allitems.Add(item);
            }
        }
    }
}