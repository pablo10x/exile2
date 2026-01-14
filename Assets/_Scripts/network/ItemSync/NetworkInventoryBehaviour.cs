// NetworkInventoryBehaviour.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Exile.Inventory;
using Exile.Inventory.Network;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Network wrapper that synchronizes <see cref="InventoryManager"/> state via a SyncList of <see cref="NetworkedItemData"/>.
/// This class does NOT modify your InventoryManager logic; it only mirrors state for networking.
/// </summary>
public class NetworkInventoryBehaviour : MonoBehaviour {
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
    private readonly List<NetworkedItemData> _items = new();

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
    private NetworkInventoryProvider _runtimeProvider;

    private int AssignedInventoryID;

    [Header("Spawning")] [SerializeField] private GameObject _droppedItemPrefab;

    #endregion

    #region Unity Lifecycle

    private void OnDestroy() {
        //_items.OnChange                           -= OnSyncListChanged;
         //AssignedInventoryID.OnChange -= OnInventoryIDChanged;
    }

    #endregion 

    #region FishNet Lifecycle

    /// <summary>
    /// Invoked when the object is initialized on the server.
    /// </summary>
    public void OnStartServer() {
        // Call the base method to initialize the object on the server.
        //base.OnStartServer();

        // Check if the Item Manager runtime is null.
        if (ItemManagerRuntime.Instance is null) {
            // If it is null, throw an exception.
            throw new InvalidOperationException("Item Manager runtime is null");
        }

        // If it is not null, register the inventory with the Item Manager runtime and assign the returned ID to AssignedInventoryID.
        AssignedInventoryID = ItemManagerRuntime.Instance.RegisterInventory(this);

        // Append the inventory ID to the name of the root object for debugging purposes.
        gameObject.transform.root.name += $"  {AssignedInventoryID}";

        // Create a new instance of NetworkInventoryProvider using the _items, _itemDatabase, InventoryRenderMode.Grid, and the product of _initialWidth and _initialHeight as parameters.
        _runtimeProvider = new NetworkInventoryProvider(_items, _itemDatabase, InventoryRenderMode.Grid, _initialWidth * _initialHeight);

        // Create a new instance of InventoryManager using AssignedInventoryID, _runtimeProvider, _initialWidth, _initialHeight, _allowAddItems, and _inventoryName as parameters.
        Inventory = new InventoryManager(AssignedInventoryID, _runtimeProvider, _initialWidth, _initialHeight, _allowAddItems, _inventoryName, this);
    }

    public void OnStartClient() {
        //base.OnStartClient();

        // Subscribe to ID changes to ensure we initialize with the correct ID
        //AssignedInventoryID.OnChange += OnInventoryIDChanged;

        // If we already have a valid ID (Snapshot received before OnStartClient), initialize immediately
        if (AssignedInventoryID != -1) {
            InitializeClientInventory(AssignedInventoryID);
        }
        else {
            Debug.Log($"[Client] OnStartClient: Waiting for Inventory ID sync...");
        }
    }

    /// <summary>
    /// Subscribes to ID changes and initializes client InventoryManager if a valid ID is received.
    /// </summary>
    /// <param name="prev">Previous ID value.</param>
    /// <param name="next">New ID value.</param>
    /// <param name="asServer">If the change is from server or not.</param>
    private void OnInventoryIDChanged(int prev, int next, bool asServer) {
        if (asServer) return; // Server doesn't need to re-init on its own change

        if (next != 0) {
            //Debug.Log($"[Client] Inventory ID Received: {next}. Initializing...");
            InitializeClientInventory(next);
        }
    }

    private void InitializeClientInventory(int inventoryId) {
        // Prevent double initialization if we already have this ID set up
        if (Inventory != null && Inventory.NetworkInventoryId == inventoryId)
            return;

        Debug.Log($"[Client] Initializing InventoryManager with ID: {inventoryId}");

        // Use NetworkInventoryProvider on client too
        var netProvider = new NetworkInventoryProvider(_items, _itemDatabase, InventoryRenderMode.Grid, _initialWidth * _initialHeight);
        _runtimeProvider = netProvider;

        Inventory = new InventoryManager(inventoryId, _runtimeProvider, _initialWidth, _initialHeight, _allowAddItems, _inventoryName, this);

        // When SyncList changes, we just need to tell Inventory to Rebuild (refresh view)
        // Ensure we don't double subscribe
        //_items.OnChange -= OnSyncListChanged;
        //_items.OnChange += OnSyncListChanged;

        OnInventoryInitialized?.Invoke();
    }

    private void OnSyncListChanged(int index, NetworkedItemData oldItem, NetworkedItemData newItem, bool asServer) {
        // If we are server, the change likely came from our own logic (via Provider), so we might not need to force rebuild if logic did it.
        // But for safety and client sync:
        if (!asServer) {
            Inventory?.Rebuild();
        }
    }

    #endregion

    #region SyncList Handling

    // Removed manual SyncList listeners as Provider handles it directly.

    #endregion

    #region Server Inventory Event Handlers

    
    private void PopulateSyncListFromManager() {
        // This acts as a 'Reset' now if needed
        _items.Clear();

        foreach (var item in Inventory.allItems) {
            _items.Add(new NetworkedItemData(item));
        }
    }

    #endregion

    #region Server Authoritative API

    
    public bool Server_TryAdd(IInventoryItem item) {
        // Just call logic. Logic calls Provider. Provider calls SyncList.
        return Inventory.TryAdd(item);
    }

    
    private bool Server_TryAddAt(IInventoryItem item, Vector2Int pos) {
        return Inventory.TryAddAt(item, pos);
    }

    
    private bool Server_TryRemove(IInventoryItem item) {
        // Just call logic.
        return Inventory.TryRemove(item);
    }

    
    public bool Server_TrySwap(IInventoryItem a, IInventoryItem b) {
        bool success = Inventory.SwapItems(a, b);

        // SwapItems updates positions. We need to ensure SyncList is updated with new positions.
        // InventoryManager calls TryAddAt internally during Swap.
        // Since we modified TryAddAt to set position then Add, this works for the 'Add' part.
        // But Swap logic is: Remove A, Remove B, Add A(at B), Add B(at A).
        // Since it uses TryRemove and TryAddAt, the Provider (and thus SyncList) 
        // will automatically be updated with Remove -> Remove -> Add -> Add operations.
        // So no manual SyncList manipulation is needed here anymore!

        return success;
    }

    #endregion

    #region Editor / Dev RPCs

    
    public void RequestAddServerRpc(int itemId) {
        var itemDef = _itemDatabase.GetItem(itemId);
        if (itemDef == null)
            return;

        var itemInstance = itemDef.CreateInstance(ItemManagerRuntime.Instance.GetNextItemId());
        Server_TryAdd(itemInstance);
    }

    
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
    
    public void SendFullInventory(object conn, bool openInventory = false) {
        if (Inventory == null)
            return;

        List<NetworkedItemData> networkedItems = _items.ToList();
        var                     inv            = new NetWorkedInventoryData(Inventory.NetworkInventoryId, Inventory.height, Inventory.width, networkedItems);

        TargetReceiveFullInventory(conn, openInventory, inv);
    }

    
    private void TargetReceiveFullInventory(object conn, bool openInventory, NetWorkedInventoryData networkedInventory) {
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

    
    public void cmd_ItemMove(NetworkedItemData item) {
        // 1. Find the item in the inventory logic
        var existingItem = Inventory.GetItemByRuntimeID(item.RuntimeID);
        if (existingItem == null) {
            Debug.LogWarning($"[Server] cmd_ItemMove: Item {item.RuntimeID} not found in inventory.");
            return;
        }

        // Capture state for rollback
        var oldPosition = existingItem.position;
        var oldRotation = existingItem.Rotated;
        var oldWidth    = existingItem.width;
        var oldHeight   = existingItem.height;

        // 2. Remove from old position (This updates SyncList via Provider)
        bool removed = Inventory.TryRemove(existingItem);
        if (!removed) {
            Debug.LogWarning("Couldn't remove item from old position.");
            return;
        }

        // 3. Update properties (Rotation) on the instance BEFORE adding back
        if (item.Rotated) {
            existingItem.Rotated = true;
            existingItem.width   = item.Width;
            existingItem.height  = item.Height;
        }
        else {
            existingItem.Rotated = false;
            existingItem.width   = item.Width;
            existingItem.height  = item.Height;
        }

        // 4. Add to new position (This updates SyncList via Provider)
        bool added = Inventory.TryAddAt(existingItem, item.Position);

        if (!added) {
            Debug.LogWarning($"[Server] Failed to move item to {item.Position}. Rolling back to {oldPosition}.");

            // Revert properties
            existingItem.Rotated = oldRotation;
            existingItem.width   = oldWidth;
            existingItem.height  = oldHeight;

            // Revert position
            bool rolledBack = Inventory.TryAddAt(existingItem, oldPosition);
            if (!rolledBack) {
                Debug.LogError($"[Server] CRITICAL: Item {item.RuntimeID} lost during move! Could not revert to {oldPosition}.");
            }
        }
    }

    
    public void cmd_ItemAdd(NetworkedItemData item, int inventoryID) {
        var itemDef = _itemDatabase.GetItem(item.ItemId);
        if (itemDef == null) {
            Debug.LogError($"[Server] cmd_ItemAdd: Item definition not found for ID {item.ItemId}");
            return;
        }

        var instance = itemDef.CreateInstance(item.RuntimeID);
        item.ApplyToItem(instance);

        if (AssignedInventoryID != inventoryID) {
            // Transfer logic
          //  Debug.Log($"[Server] Transferring item {item.RuntimeID} from Inv {AssignedInventoryID.Value} to Inv {inventoryID}");

            // 1. Remove from source
            var  existingItem   = Inventory.GetItemByRuntimeID(item.RuntimeID);
            bool removalSuccess = false;

            if (existingItem != null) {
                removalSuccess = Server_TryRemove(existingItem);
            }
            else {
                //if (!IsServerInitialized)
                    Debug.LogWarning($"[Server] Item {item.RuntimeID} not found in source inventory {AssignedInventoryID}. Cannot remove.");
                // Fail-safe: if it's not there, we can't transfer it reliably (dupe risk or ghost item)
            }

            if (!removalSuccess) {
                //if (!IsServerInitialized)
                    Debug.LogWarning($"[Server] Failed to remove item from source inventory {AssignedInventoryID}. Aborting transfer.");
                return;
            }

            // 2. Add to destination
            var  targetInventory = ItemManagerRuntime.Instance.GetInventoryByID(inventoryID);
            bool success         = false;

            if (targetInventory != null) {
                success = targetInventory.Server_TryAddAt(instance, item.Position);
            }
            else {
                Debug.LogError($"[Server] Target inventory ID {inventoryID} not found!");
            }

            // 3. Rollback if failed
            if (!success) {
                Debug.LogWarning($"[Server] Transfer failed. Returning item to source inventory {AssignedInventoryID}.");
                bool rolledBack = Server_TryAddAt(instance, item.Position); // Try adding back to original spot
                if (!rolledBack) {
                    rolledBack = Server_TryAdd(instance);
                }

                if (!rolledBack) {
                    Debug.LogError($"[Server] CRITICAL: Item {item.RuntimeID} lost during transfer! Could not return to source.");
                }
            }
        }
        else {
            Server_TryAddAt(instance, item.Position);
        }
    }

    
    public void cmd_ItemDrop(NetworkedItemData item) {
        var existingItem = Inventory.GetItemByRuntimeID(item.RuntimeID);

        // 1. Remove from inventory
        bool removed = false;
        if (existingItem != null) {
            removed = Server_TryRemove(existingItem);
        }
        else {
            //if (IsServerInitialized) {
            //    removed = true;
            //}
            //else {
            // Fallback desync check?
            Debug.LogWarning($"[Server] cmd_ItemDrop: Item {item.RuntimeID} not found in logic. Cannot drop.");
            return;
                
            //}
        }

        if (removed && existingItem != null && _droppedItemPrefab != null) {
            // 2. Spawn world object
            var spawnPos      = transform.position + Vector3.up + Random.insideUnitSphere * 0.5f;
            var droppedObject = Instantiate(_droppedItemPrefab, spawnPos, Quaternion.identity);

            var pickup = droppedObject.GetComponent<ItemPickup>();
            if (pickup != null) {
                // Initialize the pickup with item data
                if (existingItem is ItemBase itemBase) {
                    pickup.SetItem(itemBase);
                }
            }

            //ServerManager.Spawn(droppedObject);
            Debug.Log($"[Server] Item {item.RuntimeID} dropped and spawned.");
        }
    }


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