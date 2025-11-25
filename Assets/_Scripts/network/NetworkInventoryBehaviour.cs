// NetworkInventoryBehaviour.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Exile.Inventory;
using Exile.Inventory.Network;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Network wrapper that synchronizes InventoryManager state via SyncDictionary.
/// This class does NOT modify your InventoryManager logic.
/// </summary>
public class NetworkInventoryBehaviour : NetworkBehaviour {
    /// <summary>
    /// Inventory grid stored as: key = x + y * width, value = NetworkedItemData (serialized item).
    /// FishNet v4 SyncDictionary sends only delta operations (add/update/remove).
    /// </summary>
    private readonly SyncList<NetworkedItemData> Items = new();

    /// <summary>
    /// Local non-networked runtime inventory (your actual gameplay logic).
    /// </summary>
    public InventoryManager Inventory { get; private set; }

    [SerializeField] private ItemBase[] testItem;

    public ItemDatabase _ItemDatabase;

    [Header("Inventory Settings")] [SerializeField] private int    initialWidth  = 6;
    [SerializeField]                                private int    initialHeight = 6;
    [SerializeField]                                private bool   allowAddItems = true;
    [SerializeField]                                private string inventoryName = "PlayerInventory";

    /// <summary>
    /// Internal provider for your InventoryManager logic.
    /// </summary>
    private RuntimeInventoryProvider _runtimeProvider;

    /*-----------------------------------------------------------*/
    //test methods
    [SerializeField] private int itemsNumbersToAdd = 5;

    /*───────────────────────────────────────────────────────────────────────────────
     * LIFECYCLE
     *───────────────────────────────────────────────────────────────────────────────*/
    private void Awake() { }

    private void OnItemsChanged(SyncListOperation op, int index, NetworkedItemData oldItem, NetworkedItemData newItem, bool asServer) {
        // Server already has authoritative state; only clients rebuild
        if (asServer)
            return;

        RefreshInventoryFromSyncList();
        //run on client
        switch (op) {
            case SyncListOperation.Add:
                break;
            case SyncListOperation.Insert:
                break;
            case SyncListOperation.Set:

                break;
            case SyncListOperation.RemoveAt:
                break;
            case SyncListOperation.Clear:
                break;
            case SyncListOperation.Complete:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(op), op, null);
        }
    }

    public override void OnStartServer() {
        base.OnStartServer();

        _runtimeProvider = new RuntimeInventoryProvider();
        Inventory        = new InventoryManager(_runtimeProvider, initialWidth, initialHeight, allowAddItems, inventoryName, this);

        // Subscribe to inventory events
        Inventory.onItemAdded   += HandleServerItemAdded;
        Inventory.onItemRemoved += HandleServerItemRemoved;
        Inventory.onItemChanged += HandleServerItemChanged;


        for (int i = 0; i < itemsNumbersToAdd; i++) {
            addItemAsServer();
        }


        // Subscribe to incremental changes
        Items.OnChange += OnItemsChanged;
        //Push initial state → SyncDictionary
        PopulateSyncListFromManager();
    }

    public override void OnStartClient() {
        base.OnStartClient();

        _runtimeProvider = new RuntimeInventoryProvider();
        Inventory        = new InventoryManager(_runtimeProvider, initialWidth, initialHeight, allowAddItems, inventoryName, this);
        // Inventory.onItemAdded   += HandleClientItemAdded;
        // Inventory.onItemRemoved += HandleClientItemRemoved;
        // Inventory.onItemChanged += HandleClientItemChanged;

        Items.OnChange += OnItemsChanged;
        // We rebuild from current dictionary snapshot
        //ApplyDictionaryToManager();
    }

    [Server]
    private void HandleServerItemAdded(IInventoryItem item) {
        Debug.Log($"SERVER : ITEM ADDED");
    }

    [Server]
    private void HandleServerItemRemoved(IInventoryItem item) {
        Debug.Log($"SERVER : ITEM REMOVED");
    }

    [Server]
    private void HandleServerItemChanged(IInventoryItem item) {
        Debug.Log($"SERVER : ITEM CHANGED");
    }

    [Server]
    private void PopulateSyncListFromManager() {
        Items.Clear();
        foreach (var item in Inventory.allItems) {
            Items.Add(new NetworkedItemData(item));
        }
    }

    private void OnDestroy() {
        Items.OnChange -= OnItemsChanged;
    }

    /*───────────────────────────────────────────────────────────────────────────────
     * SERVER AUTHORITATIVE API
     *───────────────────────────────────────────────────────────────────────────────*/

    [ServerRpc(RequireOwnership = false)]
    public void RequestObjectObserver(NetworkConnection con) {
        Debug.Log($"ADdding observer added ID: {con.ClientId}");
        AddInventoryObserver(con);
    }

    [Server]
    private void AddInventoryObserver(NetworkConnection conn) {
        if (!IsServerInitialized)
            return;

        if (Observers.Contains(conn)) {
            Debug.Log($"con: {conn} already observing this object");
        }
        else {
            bool added = Observers.Add(conn);
            Debug.Log($"con: {conn} is now added to observers list: status: {added}");
        }
    }

    [Server]
    public void RemoveInventoryObserver(NetworkConnection conn) {
        if (!IsServerInitialized)
            return;

        if (Observers.Contains(conn))
            Observers.Remove(conn);
    }

    //test method

    [Button("Add test item")]
    public void addItemAsServer() {
        var rd = Random.Range(0, testItem.Length);
        RequestAddServerRpc(testItem[rd].ID);
    }

    [Server]
    public bool Server_TryAdd(IInventoryItem item) {
        if (Inventory.TryAdd(item)) {
            Debug.Log($"Item as {item.ItemName} added successfully. id: {item.RuntimeID}");
            PopulateSyncListFromManager();
        }
        else
            return false;

        //RebuildDictionaryFromManager();
        return true;
    }

    [Server]
    public bool Server_TryAddAt(IInventoryItem item, Vector2Int pos) {
        if (!Inventory.TryAddAt(item, pos))
            return false;

        //RebuildDictionaryFromManager();
        return true;
    }

    [Server]
    public bool Server_TryRemove(IInventoryItem item) {
        if (!Inventory.TryRemove(item))
            return false;

        //  RebuildDictionaryFromManager();
        return true;
    }

    [Server]
    public bool Server_TrySwap(IInventoryItem a, IInventoryItem b) {
        if (!Inventory.SwapItems(a, b))
            return false;

        // RebuildDictionaryFromManager();
        return true;
    }

    /*───────────────────────────────────────────────────────────────────────────────
     * CLIENT RPC REQUESTS
     *───────────────────────────────────────────────────────────────────────────────*/

#if UNITY_EDITOR || DEV_BUILD
    [ServerRpc(RequireOwnership = false)]
    public void RequestAddServerRpc(int itemId, NetworkConnection sender = null) {
        // Optional: Check if sender.IsHost or IsAdmin

        var itemDef = _ItemDatabase.GetItem(itemId);
        if (itemDef == null) return;

        // Server creates the instance. Client input is just an integer ID (definition), not the item itself.
        var itemInstance = itemDef.CreateInstance(RuntimeIdManager.GetNextId());

        // Server assigns the runtime ID.
        Server_TryAdd(itemInstance);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddAtServerRpc(int itemId, int x, int y, NetworkConnection sender = null) {
        var item = _ItemDatabase.GetItem(itemId)
                                .CreateInstance(RuntimeIdManager.GetNextId());
        Server_TryAddAt(item, new Vector2Int(x, y));
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRemoveServerRpc(int runtimeId, NetworkConnection sender = null) {
        var itemToRemove = Inventory.allItems.FirstOrDefault(item => item.RuntimeID == runtimeId);
        if (itemToRemove != null) {
            Server_TryRemove(itemToRemove);
        }
    }
#endif
    /*───────────────────────────────────────────────────────────────────────────────
     * FULL SYNC
     *───────────────────────────────────────────────────────────────────────────────*/

    /// <summary>
    /// Sends the entire inventory state to a specific client.
    /// This is useful for late-joining players or for refreshing an inventory view.
    /// </summary>
    /// <param name="conn">The connection of the client to send the inventory to.</param>
    /// <param name="OpenInventory">will trigger inventory </param>
    // SERVER: build and send snapshot
    [Server]
    public void SendFullInventory(NetworkConnection conn, bool OpenInventory = false) {
        if (Inventory == null)
            return;

        List<NetworkedItemData> networkedItems = Items.ToList(); // Items SyncList already contains the networked data

        var inv = new NetWorkedInventoryData(Inventory.networkInventoryId, Inventory.height, Inventory.width, networkedItems);
        TargetReceiveFullInventory(conn, OpenInventory, inv);
    }

    // CLIENT: receive and apply
    [TargetRpc]
    private void TargetReceiveFullInventory(NetworkConnection conn, bool openinventory, NetWorkedInventoryData networkedInventory) {
        Inventory.Clear();

        foreach (var itemData in networkedInventory.allitems) {
            var itemTemplate = _ItemDatabase.GetItem(itemData.ItemId);
            var newItem      = itemTemplate.CreateInstance(itemData.RuntimeID);
            itemData.ApplyToItem(newItem);
            Inventory.TryAddAt(newItem, newItem.position);
        }


        if (openinventory) {
            // Debug.Log($"[Client] Received full inventory: ID={networkedInventory.InventoryID}, Width={networkedInventory.InventoryWidth}, Height={networkedInventory.InventoryHeight}, ItemCount={networkedInventory.allitems.Count}");
            InventoryUIManager.Instance.AddContainerToLoot(Inventory);
        }

        // foreach (var it in Inventory.allItems) {
        //     Debug.Log($"[Client] Item in inventory: {it.ItemName} at {it.position}, ID: {it.ID}, RuntimeID: {it.RuntimeID}");
        // }

        // Open / refresh UI here if needed
    }

    [TargetRpc]
    public void GetInventoryData(NetworkConnection con) {
        if (Inventory == null)
            return;

        List<NetworkedItemData> items = Inventory.allItems.Select(item => new NetworkedItemData(item))
                                                 .ToList();

        foreach (var item in items) {
            Debug.Log($"[Client] Received inventory data: {item.ItemName} at {item.Position}");
        }
        // Do something with `items` on the client, e.g. pass to your UI
        // InventoryUIManager.Instance.SetInventory(items);
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemMove(NetworkedItemData item) {
        // Retrieve the item from the inventory using its runtime ID
        var existingItem = Inventory.GetItemByRuntimeID(item.RuntimeID);
        if (existingItem != null) {
            // Attempt to remove the item from its old position
            bool removedFromOldPosition = Inventory.TryRemove(existingItem);
            if (removedFromOldPosition) {
                // Attempt to add the item to the new requested position
                bool addedToNewPosition = Inventory.TryAddAt(existingItem, item.Position);
                if (addedToNewPosition) {
                    //Debug.Log($"Item {item.ItemName} ID: {item.RuntimeID} has been moved server side to {item.Position}");
                    // Find the item in the SyncList and update its position
                    int syncListIndex = Items.FindIndex(x => x.RuntimeID == item.RuntimeID);
                    if (syncListIndex >= 0) {
                        var networkedItemData = Items[syncListIndex];   // Get a copy of the struct
                        networkedItemData.Position = item.Position;     // Modify the position in the copy
                        Items[syncListIndex]       = networkedItemData; // Assign the modified copy back to the SyncList (triggers sync)
                    }
                    else {
                        Debug.LogWarning($"No entry found in SyncList for RID {item.RuntimeID}");
                    }
                }
            }
            else {
                Debug.Log($"couldn't drop the item");
            }


            // just add the item ( but tell other clients about it )
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemAdd(NetworkedItemData item) {
        Debug.Log($"Item add req: {item.RuntimeID}");

        var it = _ItemDatabase.GetItem(item.ItemId)
                              .CreateInstance(item.RuntimeID);
        item.ApplyToItem(it);
        var added = Inventory.TryAddAt(it, item.Position);
        if (added) Debug.Log($"item {item.ItemName} has been added");
        PopulateSyncListFromManager();
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemDrop(NetworkedItemData item) {
        Debug.Log($"Item drop req: {item.RuntimeID}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemPickedUp(NetworkedItemData item) {
        
        Debug.Log($"Item Pickedup req: {item.RuntimeID}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void cmd_ItemRotated(NetworkedItemData item) {
        int syncListIndex = Items.FindIndex(x => x.RuntimeID == item.RuntimeID);
        if (syncListIndex >= 0) {
            var networkedItemData = Items[syncListIndex]; // Get a copy of the struct
            networkedItemData.Rotated = item.Rotated;
            networkedItemData.Height  = item.Height;       // Modify the position in the copy
            networkedItemData.Width   = item.Width;        // Modify the position in the copy
            Items[syncListIndex]      = networkedItemData; // Assign the modified copy back to the SyncList (triggers sync)
        }
        else {
            Debug.LogWarning($"No entry found in SyncList for RID {item.RuntimeID}");
        }
    }

    // Rebuild local Inventory from the Items SyncList (client-side)
    private void RefreshInventoryFromSyncList() {
        if (Inventory == null || _ItemDatabase == null)
            return;

        // Clear local runtime inventory
        Inventory.Clear();

        // Rebuild from the authoritative SyncList snapshot
        for (int i = 0; i < Items.Count; i++) {
            var itemData = Items[i];

            // Find template in database
            var itemTemplate = _ItemDatabase.GetItem(itemData.ItemId);
            if (itemTemplate == null) {
                Debug.LogWarning($"[Client] RefreshInventoryFromSyncList: No item template for ID {itemData.ItemId} (Name: {itemData.ItemName})");
                continue;
            }

            // Create runtime instance and apply network data
            var newItem = itemTemplate.CreateInstance(itemData.RuntimeID);
            itemData.ApplyToItem(newItem);

            // Add to local inventory at the stored position
            if (!Inventory.TryAddAt(newItem, newItem.position)) {
                Debug.LogWarning($"[Client] RefreshInventoryFromSyncList: Failed to place item {itemData.ItemName} at {itemData.Position}");
            }
        }
    }
}