using System;
using System.Linq;
using Exile.Inventory;

using UnityEngine;


public class InventoryManager : IInventoryManager {
    private Vector2Int _size = Vector2Int.one;
    private IInventoryProvider _provider;

    [SerializeField] private bool _AllowAddingitems = true;
    private Rect _fullRect;

    public InventoryManager(IInventoryProvider provider, int width, int height, bool allowadditems) {
        _provider = provider;
        _AllowAddingitems = allowadditems;

        Rebuild();
        Resize(width, height);
    }


    /// <inheritdoc />
    //public InventoryContainer _InventoryContainer = ;

    /// <inheritdoc />
    /// <inheritdoc />     
    public int width => _size.x;

    /// <inheritdoc />
    public int height => _size.y;

    /// <inheritdoc />
    public void Resize(int newWidth, int newHeight) {
        _size.x = newWidth;
        _size.y = newHeight;
        RebuildRect();
    }

    private void RebuildRect() {
        _fullRect = new Rect(0, 0, _size.x, _size.y);
        HandleSizeChanged();
        onResized?.Invoke();
    }

    private void HandleSizeChanged() {
        // Drop all items that no longer fit the inventory
        for (int i = 0; i < allItems.Length;) {
            var item = allItems[i];
            var shouldBeDropped = false;
            var padding = Vector2.one * 0.01f;

            if (!_fullRect.Contains(item.GetMinPoint() + padding) || !_fullRect.Contains(item.GetMaxPoint() - padding)) {
                shouldBeDropped = true;
            }

            if (shouldBeDropped) {
                TryDrop(item);
            }
            else {
                i++;
            }
        }
    }

    /// <inheritdoc />
    public void Rebuild() {
        Rebuild(false);
    }

    private void Rebuild(bool silent) {
        allItems = new IInventoryItem[_provider.inventoryItemCount];
        for (var i = 0; i < _provider.inventoryItemCount; i++) {
            allItems[i] = _provider.GetInventoryItem(i);
        }

        if (!silent) onRebuilt?.Invoke();
    }

    public void Dispose() {
        _provider = null;
        allItems = null;
    }

    /// <inheritdoc />
    public bool isFull
    {
        get
        {
            if (_provider.isInventoryFull) return true;

            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    if (GetAtPoint(new Vector2Int(x, y)) == null) {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <inheritdoc />
    public IInventoryItem[] allItems { get; private set; }

    /// <inheritdoc />
    public Action onRebuilt { get; set; }

    /// <inheritdoc />
    public Action<IInventoryItem> onItemDropped { get; set; }

    /// <inheritdoc />
    public Action<IInventoryItem> onItemDroppedFailed { get; set; }

    /// <inheritdoc />
    public Action<IInventoryItem> onItemAdded { get; set; }

    /// <inheritdoc />
    public Action<IInventoryItem> onItemAddedFailed { get; set; }

    /// <inheritdoc />
    public Action<IInventoryItem> onItemRemoved { get; set; }

    public Action<IInventoryItem> OnInventoryBlocked { get; set; }

    /// <inheritdoc />
    public Action onResized { get; set; }

    /// <inheritdoc />
    public IInventoryItem GetAtPoint(Vector2Int point) {
        // Single item override
        if (_provider.inventoryRenderMode == InventoryRenderMode.Single && _provider.isInventoryFull && allItems.Length > 0) {
            return allItems[0];
        }

        foreach (var item in allItems) {
            if (item.Contains(point)) {
                return item;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IInventoryItem[] GetAtPoint(Vector2Int point, Vector2Int size) {
        var posibleItems = new IInventoryItem[size.x * size.y];
        var c = 0;
        for (var x = 0; x < size.x; x++) {
            for (var y = 0; y < size.y; y++) {
                posibleItems[c] = GetAtPoint(point + new Vector2Int(x, y));
                c++;
            }
        }

        return posibleItems.Distinct().Where(x => x != null).ToArray();
    }

    /// <inheritdoc />
    public bool TryRemove(IInventoryItem item) {
        if (!CanRemove(item)) return false;
        if (!_provider.RemoveInventoryItem(item)) return false;
        Rebuild(true);
        onItemRemoved?.Invoke(item);
        return true;
    }

    /// <inheritdoc />
    public bool TryDrop(IInventoryItem item) {
        if (!CanDrop(item) || !_provider.DropInventoryItem(item)) {
            onItemDroppedFailed?.Invoke(item);
            return false;
        }

        Rebuild(true);
        onItemDropped?.Invoke(item);
        return true;
    }

    internal bool TryForceDrop(IInventoryItem item) {
        if (!item.canDrop) {
            onItemDroppedFailed?.Invoke(item);
            return false;
        }

        onItemDropped?.Invoke(item);
        return true;
    }

    /// <inheritdoc />
    public bool CanAddAt(IInventoryItem item, Vector2Int point) {

        if (!_provider.CanAddInventoryItem(item) || _provider.isInventoryFull || _AllowAddingitems == false) {
            Debug.LogWarning("couldn't add item because inventory is full or items can't be added");
            return false;
        }

        if (_provider.inventoryRenderMode == InventoryRenderMode.Single) {
            return true;
        }

        var previousPoint = item.position;
        item.position = point;
        var padding = Vector2.one * 0.01f;

        // Check if item is outside of inventory
        if (!_fullRect.Contains(item.GetMinPoint() + padding) || !_fullRect.Contains(item.GetMaxPoint() - padding)) {
            item.position = previousPoint;
            
            return false;
        }

        // FIXED: Check if item DOES NOT overlap any other item
        // If ANY item overlaps, we cannot add
        if (allItems.Any(otherItem => item.Overlaps(otherItem))) {
            item.position = previousPoint;
            
            return false;
        }

        // No overlaps found - item can be added
        return true;
    }

    /// <inheritdoc />
    public bool TryAddAt(IInventoryItem item, Vector2Int point) {
        if (!CanAddAt(item, point)) {
            Debug.LogWarning("Couldn't add because function [canadd-at] returned false");
            onItemAddedFailed?.Invoke(item);
            return false;
        }

        if (!_provider.AddInventoryItem(item)) {
            onItemAddedFailed?.Invoke(item);
            return false;
        }

        switch (_provider.inventoryRenderMode) {
            case InventoryRenderMode.Single:
                item.position = GetCenterPosition(item);
                break;
            case InventoryRenderMode.Grid:
                item.position = point;
                break;
            case InventoryRenderMode.Layered:
                item.position = point;
                break;
            default:
                throw new NotImplementedException($"InventoryRenderMode.{_provider.inventoryRenderMode.ToString()} have not yet been implemented");
        }

        Rebuild(true);
        onItemAdded?.Invoke(item);
        return true;
    }

    /// <inheritdoc />
    public bool CanAdd(IInventoryItem item) {
        if (!_AllowAddingitems) {
            Debug.LogWarning("items can't be added because inventory is blocked");
            OnInventoryBlocked?.Invoke(item);
            return false;
        }

        if (_provider.isInventoryFull) {
            Debug.LogWarning("items can't be added because inventory is full");
            return false;
        }

        try {
            Vector2Int point;
            // Check if item is not already in inventory and we can find a spot for it
            if (!Contains(item) && GetFirstPointThatFitsItem(item, out point)) {
            
                return CanAddAt(item, point); // FIXED: Return the result of CanAddAt
            }

            
            return false;
        }
        catch (Exception e) {
            Debug.LogWarning(e);
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryAdd(IInventoryItem item) {
        if (!CanAdd(item)) {
          //  Debug.LogWarning("Couldn't add because function [canadd] returned false");
            return false;
        }

        Vector2Int point;
        return GetFirstPointThatFitsItem(item, out point) && TryAddAt(item, point);
    }
    /// <summary>
/// Attempts to add an item to the inventory, trying both normal and rotated orientations.
/// If the item doesn't fit normally but fits when rotated, it will be marked as rotated.
/// </summary>
/// <param name="item">The item to add</param>
/// <returns>True if the item was successfully added (in either orientation)</returns>
public bool TryAddWithRotation(IInventoryItem item) {
    if (!_AllowAddingitems) {
        Debug.LogWarning("items can't be added because inventory is blocked");
        OnInventoryBlocked?.Invoke(item);
        return false;
    }

    if (_provider.isInventoryFull) {
        Debug.LogWarning("items can't be added because inventory is full");
        return false;
    }

    // Store original dimensions
    int originalWidth = item.width;
    int originalHeight = item.height;
    bool originalRotated = item.Rotated;

    try {
        // First, try adding in normal orientation
        item.Rotated = false;
        item.width = originalWidth;
        item.height = originalHeight;

        Vector2Int point;
        if (!Contains(item) && GetFirstPointThatFitsItem(item, out point)) {
            if (TryAddAt(item, point)) {
                return true;
            }
        }

        // If normal orientation failed, try rotated orientation

        
        item.width = originalHeight;  // Swap dimensions
        item.height = originalWidth;
        item.Rotated = true;
        if (!Contains(item) && GetFirstPointThatFitsItem(item, out point)) {
            if (TryAddAt(item, point)) {
               
                return true;
            }
        }

        // Both orientations failed, restore original state
        item.width = originalWidth;
        item.height = originalHeight;
        item.Rotated = originalRotated;
        return false;
    }
    catch (Exception e) {
        // Restore original state on error
        item.width = originalWidth;
        item.height = originalHeight;
        item.Rotated = originalRotated;
        
        Debug.LogWarning($"Error adding item with rotation: {e}");
        return false;
    }
}

    /// <inheritdoc />
    public bool CanSwap(IInventoryItem item) {
        return _provider.inventoryRenderMode == InventoryRenderMode.Single && DoesItemFit(item) && _provider.CanAddInventoryItem(item);
    }

    /// <inheritdoc />(
    public void DropAll() {
        var itemsToDrop = allItems.ToArray();
        foreach (var item in itemsToDrop) {
            TryDrop(item);
        }
    }

    /// <inheritdoc />
    public void Clear() {
        foreach (var item in allItems) {
            TryRemove(item);
        }
    }

    /// <inheritdoc />
    public bool Contains(IInventoryItem item) => allItems.Contains(item);


    /// <inheritdoc />
    public bool CanRemove(IInventoryItem item) => Contains(item) && _provider.CanRemoveInventoryItem(item);

    /// <inheritdoc />
    public bool CanDrop(IInventoryItem item) => Contains(item) && _provider.CanDropInventoryItem(item) && item.canDrop;

    /*
  * Get first free point that will fit the given item
  */
    public bool GetFirstPointThatFitsItem(IInventoryItem item, out Vector2Int point) {
        if (DoesItemFit(item)) {
           
            for (var x = 0; x < width - (item.width - 1); x++) {
                for (var y = 0; y < height - (item.height - 1); y++) {
                    point = new Vector2Int(x, y);
             
                    if (CanAddAt(item, point)) return true;
                }
            }
        }else Debug.LogWarning($"Couldn't find a point that fits item {item.ItemName}");

        

        point = Vector2Int.zero;
        return false;
    }

    /* 
  * Returns true if given items physically fits within this inventory
  */
    private bool DoesItemFit(IInventoryItem item) => item.width <= width && item.height <= height;

    /*
  * Returns the center post position for a given item within this inventory
  */
    private Vector2Int GetCenterPosition(IInventoryItem item) {
        return new Vector2Int((_size.x - item.width) / 2, (_size.y - item.height) / 2);
    }
}