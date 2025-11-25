using System.Collections.Generic;
using Exile.Inventory;

public static class RuntimeIdManager
{
    // Monotonically increasing ID counter (per server session).
    private static int _nextRuntimeId = 0;

    // Optional: tracking of items by id if you ever need global lookup.
    // If you don't need this, you can remove the dictionary and related methods.
    private static readonly Dictionary<int, IInventoryItem> _items
        = new Dictionary<int, IInventoryItem>();

    /// <summary>
    /// Get a new unique runtime id.
    /// Server-only.
    /// </summary>
    public static int GetNextId()
    {
        // NOTE: If you expect extremely long sessions and worry about overflow,
        // add wrap-around handling here.
        return _nextRuntimeId++;
    }

    /// <summary>
    /// Registers an item under a new unique id and returns that id.
    /// </summary>
    public static int RegisterItem(IInventoryItem item)
    {
        int id = GetNextId();
        item.RuntimeID = id;
        _items[id]     = item;
        return id;
    }

    /// <summary>
    /// Registers an item with a specific id (e.g. from deserialization).
    /// </summary>
    public static void RegisterItemWithId(IInventoryItem item, int runtimeId)
    {
        item.RuntimeID = runtimeId;
        _items[runtimeId] = item;
    }

    /// <summary>
    /// Unregister item when destroyed / removed permanently.
    /// </summary>
    public static void UnregisterItem(int runtimeId)
    {
        _items.Remove(runtimeId);
    }

    /// <summary>
    /// Optional: global lookup by runtime id.
    /// </summary>
    public static bool TryGetItem(int runtimeId, out IInventoryItem item)
    {
        return _items.TryGetValue(runtimeId, out item);
    }

    /// <summary>
    /// Clears all state (e.g. on server shutdown / scene change).
    /// </summary>
    public static void Reset()
    {
        _nextRuntimeId = 0;
        _items.Clear();
    }
}