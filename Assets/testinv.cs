using System.Collections.Generic;
using Exile.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;

public class testinv : MonoBehaviour {
    public InventoryManager inventoryManager;

    public int          itemcount = 0;
    public int          inventorySize;
    public InventoryView view;

    public Dictionary<ItemBase, Vector2Int> itemPositions = new Dictionary<ItemBase, Vector2Int>();
    void                                    Start() { }

    [Button("Get Inventory Data")]
    public void GetInventoryData() {
        inventoryManager = view.InventoryManager;
        itemcount        = inventoryManager.allItems.Length;
        inventorySize    = inventoryManager.width * inventoryManager.height;

        itemPositions.Clear();
        foreach (var item in inventoryManager.allItems) {
            itemPositions.Add((ItemBase)item, new Vector2Int(item.position.x, item.position.y));
            Debug.Log(item.ItemName + " at position " + item.position);
        }
    }

    [Button("Clear Inventory")]
    public void ClearInventory() {
        inventoryManager.DropAll();
        inventoryManager.Clear();
    }

    [Button("Repopulate Inventory")]
    public void RePopulateInventory() {
        ClearInventory();
        foreach (var item in itemPositions) {
            var itemx = (ItemBase)item.Key;
            var it    = item.Value;
            inventoryManager.TryAddAt(itemx, new Vector2Int((int)it.x, (int)it.y));
        }
    }

    // Update is called once per frame
    void Update() { }
}