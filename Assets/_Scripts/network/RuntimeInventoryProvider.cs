// RuntimeInventoryProvider.cs

using System.Collections.Generic;
using Exile.Inventory;
using UnityEngine;

public class RuntimeInventoryProvider : IInventoryProvider {
    // backing store of items used by InventoryManager
    private readonly List<IInventoryItem> _items = new();

    public int                 inventoryItemCount  => _items.Count;
    public bool                isInventoryFull     => false; // InventoryManager will check grid fullness itself
    public InventoryRenderMode inventoryRenderMode => InventoryRenderMode.Grid;

    public IInventoryItem GetInventoryItem(int index) {
        if (index < 0 || index >= _items.Count) return null;
        return _items[index];
    }

    public bool CanDropInventoryItem(IInventoryItem item) {
        throw new System.NotImplementedException();
    }

    public bool AddInventoryItem(IInventoryItem item) {
        if (item == null) return false;
        _items.Add(item);
        return true;
    }

    public bool RemoveInventoryItem(IInventoryItem item) {
        return _items.Remove(item);
    }

    public bool DropInventoryItem(IInventoryItem item) {
        // For runtime provider we just remove it; spawn world drop logic elsewhere on server
        return RemoveInventoryItem(item);
    }

    public bool CanAddInventoryItem(IInventoryItem    item) => true;
    public bool CanRemoveInventoryItem(IInventoryItem item) => true;
}