
using System;
using System.Collections.Generic;
using Exile.Inventory.Network;

namespace _Scripts.Items {

    [Serializable]
    public struct NetItem {
        public int  ItemID;  // maps to ItemBase.ID
        public int  Quantity;  // stack count
        public int  X;       // grid X (if used)
        public int  Y;       // grid Y
        public bool Rotated; // true if rotated 90deg
    }

    [Serializable]
    public struct NetItemContainer {
        public InventoryManager inventoryManager;
        public string Name;
        public List<NetworkedItemData> Items;
        public int Width;
        public int Height;

        public NetItemContainer(ref InventoryManager _inventorymanager, string name, List<NetworkedItemData> items, int width, int height) {
            Name   = name;
            Items  = items;
            Width  = width;
            Height = height;
            inventoryManager = _inventorymanager;
        }

       
    }
}