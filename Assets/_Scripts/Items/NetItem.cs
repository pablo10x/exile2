
using System;
using System.Collections.Generic;
using FishNet.Object.Synchronizing;
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
        public string Name;
        public List<NetItem> Items;
        public int Width;
        public int Height;

        public NetItemContainer(string name, List<NetItem> items, int width, int height) {
            Name   = name;
            Items  = items;
            Width  = width;
            Height = height;
        }

    }
}