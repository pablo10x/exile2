using System;
using Exile.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Exile.Inventory
{
    public interface IInventoryController
    {
        Action<IInventoryItem> onItemHovered { get; set; }
        Action<IInventoryItem> onItemPickedUp { get; set; }
        Action<IInventoryItem> onItemAdded { get; set; }
        Action<IInventoryItem> onItemSwapped { get; set; }
        Action<IInventoryItem> onItemReturned { get; set; }
        Action<IInventoryItem> onItemDropped { get; set; }
    }

    /// <summary>
    /// Enables human interaction with an inventory renderer using Unity's event systems
    /// </summary>
    [RequireComponent(typeof(InventoryRenderer))]
    public class InventoryController : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IPointerExitHandler, IPointerEnterHandler,
        IInventoryController
        {
            // The dragged item is static and shared by all controllers
            // This way items can be moved between controllers easily
            private static InventoryDraggedItem _draggedItem;

            /// <inheritdoc />
            public Action<IInventoryItem> onItemHovered { get; set; }

            /// <inheritdoc />
            public Action<IInventoryItem> onItemPickedUp { get; set; }

            /// <inheritdoc />
            public Action<IInventoryItem> onItemAdded { get; set; }

            /// <inheritdoc />
            public Action<IInventoryItem> onItemSwapped { get; set; }

            /// <inheritdoc />
            public Action<IInventoryItem> onItemReturned { get; set; }

            /// <inheritdoc />
            public Action<IInventoryItem> onItemDropped { get; set; }

            private Canvas _canvas;
            internal InventoryRenderer inventoryRenderer;
            internal InventoryManager inventory => (InventoryManager) inventoryRenderer.inventory;

            private IInventoryItem _itemToDrag;
            private PointerEventData _currentEventData;
            private IInventoryItem _lastHoveredItem;

            /*
             * Setup
             */
            void Awake()
            {
                inventoryRenderer = GetComponent<InventoryRenderer>();
                if (inventoryRenderer == null) { throw new NullReferenceException("Could not find a renderer. This is not allowed!"); }

                // Find the canvas
                var canvases = GetComponentsInParent<Canvas>();
                if (canvases.Length == 0) { throw new NullReferenceException("Could not find a canvas."); }
                _canvas = canvases[canvases.Length - 1];
            }

            /*
             * Grid was clicked (IPointerDownHandler)
             */
            public void OnPointerDown(PointerEventData eventData)
            {
                var grid = ScreenToGrid(eventData.position);
                var itemAtPoint = inventory.GetAtPoint(grid);

                if (_draggedItem != null) return;

                if (itemAtPoint != null) {
                    // Check for split action (e.g., holding 'X' key)
                    if (Input.GetKey(KeyCode.X)) {
                        HandleSplitStack(itemAtPoint);
                    } else {
                        _itemToDrag = itemAtPoint;
                    }
                }
            }

            /*
             * Dragging started (IBeginDragHandler)
             */
            public void OnBeginDrag(PointerEventData eventData) {
                // Clear any existing selection in the inventory renderer
                inventoryRenderer.ClearSelection();

                // If there's no item to drag or an item is already being dragged, do nothing
                if (_itemToDrag == null || _draggedItem != null) return;

                // Calculate the local position within the renderer and the offset for dragging
                var localPosition = ScreenToLocalPositionInRenderer(eventData.position);
                var itemOffest    = inventoryRenderer.GetItemOffset(_itemToDrag);
                var dragOffset = itemOffest - localPosition;

                // Create a new InventoryDraggedItem instance
                _draggedItem = new InventoryDraggedItem(_canvas, this, _itemToDrag.position, _itemToDrag, dragOffset);

                // Ensure the dragged item's image maintains its aspect ratio
                _draggedItem._image.preserveAspect = true;

                // Calculate the dimensions for the dragged item based on its size and cell size
                float itemWidth = _itemToDrag.width * inventoryRenderer.cellSize.x / 1.8f;
               
                float itemHeight = _itemToDrag.height * inventoryRenderer.cellSize.y;

                // Apply rotation and adjust size based on the item's rotated state
                if (_draggedItem.item.Rotated) {
                    _draggedItem._image.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
                    // When rotated, swap the dimensions
                    _draggedItem._image.rectTransform.sizeDelta = new Vector2(itemHeight, itemWidth);
                }
                else {
                    _draggedItem._image.rectTransform.localRotation = Quaternion.Euler(0, 0, 0f);
                    _draggedItem._image.rectTransform.sizeDelta = new Vector2(itemWidth, itemHeight);
                }

                // Attempt to remove the item from the inventory
                inventory.TryRemove(_itemToDrag);

                onItemPickedUp?.Invoke(_itemToDrag);
            }

            /*
             * Dragging is continuing (IDragHandler)
             */
            public void OnDrag(PointerEventData eventData)
            {
                _currentEventData = eventData;
                if (_draggedItem != null)
                {
                    // Update the items position
                   // _draggedItem.position = eventData.position;
                }
            }

            /*
             * Dragging stopped (IEndDragHandler)
             */
            public void OnEndDrag(PointerEventData eventData)
            {
                if (_draggedItem == null) {
                    return;
                }

                bool wasHandled = HandleSwapOnDrop(eventData);

                // If the swap logic handled the drop, we are done.
                if (wasHandled) {
                    _draggedItem = null;
                    _itemToDrag = null;
                    return;
                }

                var mode = _draggedItem.Drop(eventData.position);

                switch (mode)
                {
                    case InventoryDraggedItem.DropMode.Added:
                        onItemAdded?.Invoke(_itemToDrag);
                        break;
                    case InventoryDraggedItem.DropMode.Swapped:
                        onItemSwapped?.Invoke(_itemToDrag);
                        break;
                    case InventoryDraggedItem.DropMode.Returned:
                        onItemReturned?.Invoke(_itemToDrag);
                        break;
                    case InventoryDraggedItem.DropMode.Dropped:
                        onItemDropped?.Invoke(_itemToDrag);
                        ClearHoveredItem();
                        break;
                }

                _draggedItem = null;
                _itemToDrag = null;
                _currentEventData = null;
            }

            /// <summary>
            /// Checks if an item is being dropped onto another item and attempts to swap them.
            /// </summary>
            /// <returns>True if the drop was handled (either by swapping or stacking), false otherwise.</returns>
            private bool HandleSwapOnDrop(PointerEventData eventData) {
                var draggedItem = _draggedItem.item;

                // Find the inventory controller under the cursor
                var raycastResults = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, raycastResults);
                var targetController = raycastResults.Count > 0 ? raycastResults[0].gameObject.GetComponent<InventoryController>() : null;

                if (targetController == null) return false;

                // Find the item at the drop position
                var gridPoint  = targetController.ScreenToGrid(eventData.position);
                var targetItem = targetController.inventory.GetAtPoint(gridPoint);

                if (targetItem == null) return false; // Not dropping on an item

                // --- Stacking Logic ---
                // If items are the same and stackable, try to stack them first.
                if (targetItem != draggedItem && targetItem.Stackable && targetItem.ItemName == draggedItem.ItemName) {
                    int spaceInStack     = targetItem.maxQuantity - targetItem.Quantity;
                    int amountToTransfer = Mathf.Min(spaceInStack, draggedItem.Quantity);

                    if (amountToTransfer > 0) {
                        targetItem.Quantity += amountToTransfer;
                        draggedItem.Quantity -= amountToTransfer;
                        targetController.inventoryRenderer.RefreshItem(targetItem);

                        if (draggedItem.Quantity <= 0) {
                            Destroy(_draggedItem._image.gameObject);
                            return true; // Fully stacked, operation is complete.
                        }
                        // If there's a remainder, fall through to the normal drop logic.
                        return false;
                    }
                }

                // --- Swapping Logic ---
                // If not stacking, attempt to swap.
                var originalController = _draggedItem.originalController.inventory;
                var originalPosition = _draggedItem.originPoint;

                // Check if the target item can fit in the dragged item's original spot
                if (targetController.inventory.TryRemove(targetItem) && originalController.CanAddAt(targetItem, originalPosition)) {
                    // Perform the swap
                    originalController.TryAddAt(targetItem, originalPosition);
                    targetController.inventory.TryAddAt(draggedItem, targetItem.position);
                    Destroy(_draggedItem._image.gameObject);
                    return true; // Swap was successful.
                }

                return false; // Could not stack or swap.
            }

            /// <summary>
            /// Handles splitting a stack of items when the split key is held.
            /// </summary>
            private void HandleSplitStack(IInventoryItem itemToSplit) {
                // Can only split stackable items with more than one in the stack
                if (!itemToSplit.Stackable || itemToSplit.Quantity <= 1) return;

                int originalQuantity = itemToSplit.Quantity;
                int newStackQuantity = Mathf.FloorToInt(originalQuantity / 2f);
                int remainingQuantity = originalQuantity - newStackQuantity;

                if (newStackQuantity > 0) {
                    // Create a new item instance for the split stack
                    var newItem = itemToSplit.CreateInstance();
                    newItem.Quantity = newStackQuantity;

                    // Find the next empty point that can fit the new item, bypassing the auto-stacking logic.
                    Vector2Int emptyPoint;
                    if (inventory.GetFirstPointThatFitsItem(newItem, out emptyPoint))
                    {
                        // Add the new item directly to the found empty slot.
                        inventory.TryAddAt(newItem, emptyPoint);

                        // If successful, update the original item's quantity.
                        itemToSplit.Quantity = remainingQuantity;

                        // Manually trigger a re-render of the original item's UI to update its quantity text.
                        // This is necessary because just changing the quantity doesn't fire an inventory event.
                        inventoryRenderer.RefreshItem(itemToSplit);
                    }
                    else {
                        // If adding failed (e.g., inventory is full), destroy the temporary item instance.
                        Destroy(newItem as UnityEngine.Object);
                        Debug.Log("Could not split stack: Not enough space in inventory.");
                    }
                }
            }

            /*
             * Pointer left the inventory (IPointerExitHandler)
             */
            public void OnPointerExit(PointerEventData eventData)
            {
                if (_draggedItem != null)
                {
                    // Clear the item as it leaves its current controller
                    _draggedItem.currentController = null;
                    inventoryRenderer.ClearSelection();
                }
                else { ClearHoveredItem(); }
                _currentEventData = null;
            }

            /*
             * Pointer entered the inventory (IPointerEnterHandler)
             */
            public void OnPointerEnter(PointerEventData eventData)
            {
                if (_draggedItem != null)
                {
                    // Change which controller is in control of the dragged item
                    _draggedItem.currentController = this;
                }
                _currentEventData = eventData;
            }

            /*
             * Update loop
             */
            private Vector2Int _lastHoveredCell = new Vector2Int(-1, -1);
            void Update() {
                if (_currentEventData == null) return;

                if (_draggedItem == null) {
                    // Detect hover on items
                    var grid = ScreenToGrid(_currentEventData.position);
                    var item = inventory.GetAtPoint(grid);

                    // Highlight the hovered cell for debugging
                    // if (grid != _lastHoveredCell) {
                    //     // Clear previous highlight
                    //     inventoryRenderer.ClearSelection();
                    //
                    //     // Create a temporary 1x1 item at the hovered position to visualize it
                    //     if (grid.x >= 0 && grid.x < inventory.width && grid.y >= 0 && grid.y < inventory.height) {
                    //         // Use a dummy item to show selection
                    //         var dummyItem = new HoverCell(grid);
                    //         inventoryRenderer.SelectItem(dummyItem, false, new Color(0f, 1f, 0f, 0.3f)); // Green transparent
                    //     }
                    //
                    //     _lastHoveredCell = grid;
                    // }

                    if (item == _lastHoveredItem) return;

                    onItemHovered?.Invoke(item);
                    _lastHoveredItem = item;
                }
                else {
                    
                     //check if user clicked item rotate
                     if (Input.GetKeyDown(KeyCode.Space)) {
                     
                         
                         if (!_draggedItem.item.Rotated) {
                             var h = _draggedItem.item.height;
                             var w = _draggedItem.item.width;
                             _draggedItem.item.height = w;
                             _draggedItem.item.width  = h;
                             _draggedItem._image.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
                             _draggedItem.item.Rotated = true;
                         }
                         else {
                             var h = _draggedItem.item.width;
                             var w = _draggedItem.item.height;
                             _draggedItem.item.height = h;
                             _draggedItem.item.width  = w;
                             _draggedItem._image.rectTransform.localRotation = Quaternion.Euler(0, 0, 0f);
                             _draggedItem.item.Rotated = false;
                         }
                         // if(_draggedItem.item.Rotated)
                         //  _draggedItem._image.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
                         // else _draggedItem._image.rectTransform.localRotation = Quaternion.Euler(0, 0, 0f);
                     }
                    
                    // Update position while dragging
                    _draggedItem.position = _currentEventData.position;
                }
            }

            /* 
             * 
             */
            private void ClearHoveredItem()
            {
                if (_lastHoveredItem != null)
                {
                    onItemHovered?.Invoke(null);
                }
                _lastHoveredItem = null;
            }

            /*
             * Get a point on the grid from a given screen point
             */
            internal Vector2Int ScreenToGrid(Vector2 screenPoint) {
                var pos       = ScreenToLocalPositionInRenderer(screenPoint);
                var sizeDelta = inventoryRenderer.rectTransform.sizeDelta;
                pos.x += sizeDelta.x / 2;
                pos.y += sizeDelta.y / 2;
                return new Vector2Int(Mathf.FloorToInt(pos.x / inventoryRenderer.cellSize.x), Mathf.FloorToInt(pos.y / inventoryRenderer.cellSize.y));
            }


            

            private Vector2 ScreenToLocalPositionInRenderer(Vector2 screenPosition)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    inventoryRenderer.rectTransform,
                    screenPosition,
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                    out var localPosition
                );
                return localPosition;
            }

            // Add this helper class inside InventoryController or in a separate file:
            
        }
    
    
}
