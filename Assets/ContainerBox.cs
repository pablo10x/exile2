using System.Collections.Generic;
using Exile.Inventory;
using Exile.Inventory.Examples;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class ContainerBox : NetworkBehaviour {
    [SerializeField] private List<ItemBase> _itemsToAdd;

    [SerializeField] private InventoryShape inventoryShape;

    private InventoryManager _inventoryManager;

    public override void OnSpawnServer(NetworkConnection connection) {
        base.OnSpawnServer(connection);


        _inventoryManager = new InventoryManager(new InventoryProvider(InventoryRenderMode.Grid, -1, ItemType.Any), inventoryShape.width, inventoryShape.height, true);

        foreach (var item in _itemsToAdd) {
            if (_inventoryManager.TryAddWithRotation(item)) {
                Debug.Log($"item {item} has been added");
            }
        }

        Debug.Log("server spawn");
    }
}