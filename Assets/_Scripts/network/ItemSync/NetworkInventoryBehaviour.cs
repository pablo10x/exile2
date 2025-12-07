// NetworkInventoryBehaviour.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Exile.Inventory;
using Exile.Inventory.Examples;
using Exile.Inventory.Network;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Network wrapper that synchronizes <see cref="InventoryManager"/> state via a SyncList of <see cref="NetworkedItemData"/>.
/// This class does NOT modify your InventoryManager logic; it only mirrors state for networking.
/// </summary>
public class NetworkInventoryBehaviour : NetworkBehaviour {
    #region Testing

    [BoxGroup("Testing")] [SerializeField] private List<ItemBase> items = new List<ItemBase>();

    [BoxGroup("Testing")]
    [Button("Add random Item")]
    public void AddItem() {
        int randomIndex = Random.Range(0, items.Count);
        var it          = _itemDatabase.GetItem(items[randomIndex].Id);
        if (it != null) {
            RequestAddServerRpc(items[randomIndex].Id);
        }
    }

    #endregion

    #region Fields & Properties

    /// <summary>
    /// Networked list of items (authoritative on server).
    /// </summary>
    private readonly SyncList<NetworkedItemData> _items = new();

    /// <summary>
    /// Local non-networked runtime inventory (actual gameplay logic).
    /// </summary>
    public InventoryManager Inventory { get; private set; }

    [SerializeField, Required] private ItemDatabase _itemDatabase;

    public Action OnInventoryInitialized;

    [Header("Inventory Settings")] [SerializeField] private int    _initialWidth  = 6;
    [SerializeField]                                private int    _initialHeight = 6;
    [SerializeField]                                private bool   _allowAddItems = true;
    [SerializeField]                                private string _inventoryName = "PlayerInventory";

    /// <summary>
    /// Internal provider for the InventoryManager.
    /// </summary>
    private InventoryProvider _runtimeProvider;

    private int AssignedInventoryID;

    #endregion

    #region Unity Lifecycle

    private void OnDestroy() {
        _items.OnChange -= OnItemsChanged;
    }

    #endregion

    #region FishNet Lifecycle

    public override void OnStartServer() {
        base.OnStartServer();


        if (ItemManagerRuntime.Instance is null) {
            UnityEngine.Debug.Log($"Item Manager runtile is null");
            AssignedInventoryID = Random.Range(0, 9999);
        }
        else {
            AssignedInventoryID = ItemManagerRuntime.Instance.GetNextContainerId();
        }

        // for debuging purpose add id to the name of root object 
        UnityEngine.Debug.Log($"netInv on {gameObject.transform.root.name}");
        gameObject.transform.root.name += $"  {AssignedInventoryID}";


        _runtimeProvider = new InventoryProvider();
        Inventory        = new InventoryManager(AssignedInventoryID, _runtimeProvider, _initialWidth, _initialHeight, _allowAddItems, _inventoryName, this);


        // Subscribe to incremental network changes
        _items.OnChange += OnItemsChanged;

        // Push initial state into SyncList
        PopulateSyncListFromManager();
    }

    public override void OnStartClient() {
        base.OnStartClient();
        
        _runtimeProvider = new InventoryProvider();
        Inventory        = new InventoryManager(AssignedInventoryID, _runtimeProvider, _initialWidth, _initialHeight, _allowAddItems, _inventoryName, this);

        _items.OnChange += OnItemsChanged;


        OnInventoryInitialized?.Invoke();
    }

    #endregion

    #region SyncList Handling

    /// <summary>
    /// Called whenever the SyncList of items changes.
    /// </summary>
    private void OnItemsChanged(SyncListOperation op, int index, NetworkedItemData oldItem, NetworkedItemData newItem, bool asServer) {
        // Server already has authoritative state; only clients rebuild.
        if (IsServerInitialized)
            return;

        RefreshInventoryFromSyncList();

        // Operation-specific handling hook (currently unused but kept for future logic).
        switch (op) {
            case SyncListOperation.Add:
                break;
            case SyncListOperation.Insert:
                break;
            case SyncListOperation.Set:

                break;
            case SyncListOperation.RemoveAt:
            case SyncListOperation.Clear:
            case SyncListOperation.Complete:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(op), op, null);
        }
    }

    /// <summary>
    /// Rebuild local Inventory from the authoritative SyncList snapshot (client-side).
    /// </summary>
    private void RefreshInventoryFromSyncList() {
        if (Inventory == null || _itemDatabase == null)
            return;

        Inventory.Clear();

        for (int i = 0; i < _items.Count; i++) {
            var itemData = _items[i];

            var itemTemplate = _itemDatabase.GetItem(itemData.ItemId);
            if (itemTemplate == null) {
                Debug.LogWarning($"[Client] RefreshInventoryFromSyncList: No item template for ID {itemData.ItemId} (Name: {itemData.ItemName})");
                continue;
            }

            var newItem = itemTemplate.CreateInstance(itemData.RuntimeID);
            itemData.ApplyToItem(newItem);

            if (!Inventory.TryAddAt(newItem, newItem.position)) {
                Debug.LogWarning($"[Client] RefreshInventoryFromSyncList: Failed to place item {itemData.ItemName} at {itemData.Position}");
            }
        }
    }

    #endregion

    #region Server Inventory Event Handlers

    [Server]
    private void PopulateSyncListFromManager() {
        _items.Clear();

        foreach (var item in Inventory.allItems) {
            //  Debug.Log($"SyncList: adding {item.RuntimeID} {item.ItemName}");
            _items.Add(new NetworkedItemData(item));
        }
    }

    #endregion

    #region Server Authoritative API

    [Server]
    public bool Server_TryAdd(IInventoryItem item) {
        if (!Inventory.TryAdd(item))
            return false;

        PopulateSyncListFromManager();
        // Incremental update instead of full rebuild.

        return true;
    }

    [Server]
    private bool Server_TryAddAt(IInventoryItem item, Vector2Int pos) {
        if (!Inventory.TryAddAt(item, pos))
            return false;

        // Incremental update instead of full rebuild.
        _items.Add(new NetworkedItemData(item));

        return true;
    }

    [Server]
    private void Server_TryRemove(IInventoryItem item) {
        Inventory.TryRemove(item);


        // Incremental update instead of full rebuild.
        int index = _items.FindIndex(x => x.RuntimeID == item.RuntimeID);
        if (index >= 0)
            _items.RemoveAt(index);
        else
            Debug.LogWarning($"[Server] Server_TryRemove: No SyncList entry for RID {item.RuntimeID}");
    }

    [Server]
    public bool Server_TrySwap(IInventoryItem a, IInventoryItem b) {
        if (!Inventory.SwapItems(a, b))
            return false;

        // Both items changed position, so update both entries.
        int indexA = _items.FindIndex(x => x.RuntimeID == a.RuntimeID);
        if (indexA >= 0)
            _items[indexA] = new NetworkedItemData(a);

        int indexB = _items.FindIndex(x => x.RuntimeID == b.RuntimeID);
        if (indexB >= 0)
            _items[indexB] = new NetworkedItemData(b);

        return true;
    }

    #endregion

    #region Editor / Dev RPCs

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddServerRpc(int itemId) {
        var itemDef = _itemDatabase.GetItem(itemId);
        if (itemDef == null)
            return;

        var itemInstance = itemDef.CreateInstance(ItemManagerRuntime.Instance.GetNextItemId());
        Server_TryAdd(itemInstance);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddAtServerRpc(int itemId, int x, int y) {
        var itemDef = _itemDatabase.GetItem(itemId);
        if (itemDef == null)
            return;

        var itemInstance = itemDef.CreateInstance(ItemManagerRuntime.Instance.GetNextItemId());
        Server_TryAddAt(itemInstance, new Vector2Int(x, y));
    }

    #endregion

    #region Full Sync (Snapshot)

    /// <summary>
    /// Sends the entire inventory state to a specific client.
    /// Useful for late-joining players or refreshing an inventory view.
    /// </summary>
    [Server]
    public void SendFullInventory(NetworkConnection conn, bool openInventory = false) {
        if (Inventory == null)
            return;

        List<NetworkedItemData> networkedItems = _items.ToList();
        var                     inv            = new NetWorkedInventoryData(Inventory.NetworkInventoryId, Inventory.height, Inventory.width, networkedItems);

        TargetReceiveFullInventory(conn, openInventory, inv);
    }

    [TargetRpc]
    private void TargetReceiveFullInventory(NetworkConnection conn, bool openInventory, NetWorkedInventoryData networkedInventory) {
        Inventory.Clear();

        foreach (var itemData in networkedInventory.allitems) {
            var itemTemplate = _itemDatabase.GetItem(itemData.ItemId);
            if (itemTemplate == null)
                continue;

            var newItem = itemTemplate.CreateInstance(itemData.RuntimeID);
            itemData.ApplyToItem(newItem);
            Inventory.TryAddAt(newItem, newItem.position);
        }

        if (openInventory) {
            InventoryUIManager.Instance.AddContainerToLoot(Inventory);
        }
    }

    #endregion

    #region Item Commands (RPC)

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemMove(NetworkedItemData item) {
        var existingItem = Inventory.GetItemByRuntimeID(item.RuntimeID);
        if (existingItem == null)
            return;

        bool removedFromOldPosition = Inventory.TryRemove(existingItem);
        if (!removedFromOldPosition) {
            Debug.Log("Couldn't remove item from old position.");
            return;
        }

        bool addedToNewPosition = Inventory.TryAddAt(existingItem, item.Position);
        if (!addedToNewPosition)
            return;

        int syncListIndex = _items.FindIndex(x => x.RuntimeID == item.RuntimeID);
        if (syncListIndex < 0) {
            Debug.LogWarning($"No entry found in SyncList for RID {item.RuntimeID}");
            Debug.LogWarning($"inventory {AssignedInventoryID}");
            return;
        }

        var networkedItemData = _items[syncListIndex];

        // Handle item rotation
        if (item.Rotated) {
            networkedItemData.Height  = item.Height;
            networkedItemData.Width   = item.Width;
            networkedItemData.Rotated = true;
        }
        else {
            networkedItemData.Height  = item.Width;
            networkedItemData.Width   = item.Height;
            networkedItemData.Rotated = false;
        }

        networkedItemData.Position = item.Position;
        _items[syncListIndex]      = networkedItemData; // triggers SyncList sync
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemAdd(NetworkedItemData item) {

        UnityEngine.Debug.Log($"itemAdd being called on inv  {AssignedInventoryID}");
        var itemDef = _itemDatabase.GetItem(item.ItemId);
        if (itemDef == null)
            return;


        var instance = itemDef.CreateInstance(item.RuntimeID);
        item.ApplyToItem(instance);


        // add to synclist
        int index = _items.FindIndex(x => x.RuntimeID == instance.RuntimeID);
        if (index < 0) {
            _items.Add(new NetworkedItemData(instance));
            Debug.Log($"item added to synclist {item.ItemName} RID {item.RuntimeID}");
        }

        Server_TryAddAt(instance, item.Position);


        // if (Inventory.TryAddAt(instance, item.Position)) {
        //     Debug.Log($"Item {item.ItemName} has been added. RID {item.RuntimeID}");
        // }
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemDrop(NetworkedItemData item) {
        // If the server  initialized, remove the item directly from the SyncList.
        if (IsServerInitialized) {
            var index = _items.FindIndex(x => x.RuntimeID == item.RuntimeID);
            if (index >= 0)
                _items.RemoveAt(index);
            return;
        }

        var itemToRemove = Inventory.allItems.FirstOrDefault(x => x.RuntimeID == item.RuntimeID);
        if (itemToRemove != null)
            Server_TryRemove(itemToRemove);

        // The commented-out code below represents an alternative or previous approach
        // to handling item drops, potentially involving a 'TryDrop' method on the Inventory.
        // var existing = Inventory.GetItemByRuntimeID(item.RuntimeID);
        // if (existing != null) {
        // bool canRemove = Inventory.TryDrop(existing);
        // if (canRemove) {
        // Debug.Log($"Item dropped {item.ItemName}");
        // }
        // }
        // Debugging logs for inventory contents after a drop attempt.
        // Debug.Log($"list of items in inventory now {Inventory.allItems.Length}");
        // if (Inventory.allItems.Length > 0) {
        // foreach (var itemx in Inventory.allItems) {
        // Debug.Log($"ItemName: {itemx.ItemName} | RID: {itemx.RuntimeID}");
        // }
        // }
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemPickedUp(NetworkedItemData item) {
        // Hook for pickup logic if needed.
    }

    #endregion

    #region Utility

    /// <summary>
    /// Converts all items in the current inventory into a new list of <see cref="NetworkedItemData"/>.
    /// </summary>
    public List<NetworkedItemData> ConvertItemsToNetworkItems() {
        return Inventory.allItems.Select(item => new NetworkedItemData(item))
                        .ToList();
    }

    #endregion
}