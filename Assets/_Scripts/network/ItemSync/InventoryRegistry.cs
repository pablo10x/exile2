using UnityEngine;
using System.Collections.Generic;

public class InventoryRegistry : MonoBehaviour {
    public static InventoryRegistry Instance { get; private set; }

    private readonly Dictionary<int, NetworkInventoryBehaviour> _inventories =
        new Dictionary<int, NetworkInventoryBehaviour>();

    private void Awake() => Instance = this;

    public void Register(NetworkInventoryBehaviour inv) =>
        _inventories[inv.Inventory.NetworkInventoryId] = inv;

    public void Unregister(NetworkInventoryBehaviour inv) =>
        _inventories.Remove(inv.Inventory.NetworkInventoryId);

    public NetworkInventoryBehaviour Get(int id) =>
        _inventories.TryGetValue(id, out var inv) ? inv : null;
}