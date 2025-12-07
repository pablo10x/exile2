using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Exile.Inventory {
    /// <summary>
    /// Class for keeping track of dragged items
    /// </summary>
    public class InventoryDraggedItem {
        public enum DropMode {
            Added,
            Swapped,
            Returned,
            QuantityChanged,
            Dropped,
        }

        /// <summary>
        /// Returns the InventoryController this item originated from
        /// </summary>
        public InventoryController originalController { get; private set; }

        /// <summary>
        /// Returns the point inside the inventory from which this item originated from
        /// </summary>
        public Vector2Int originPoint { get; private set; }

        /// <summary>
        /// Returns the item-instance that is being dragged
        /// </summary>
        public IInventoryItem item { get; private set; }

        /// <summary>
        /// Gets or sets the InventoryController currently in control of this item
        /// </summary>
        public InventoryController currentController;

        private readonly Canvas        _canvas;
        private readonly RectTransform _canvasRect;
        internal         Image         _image;
        private          Vector2       _offset;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="canvas">The canvas</param>
        /// <param name="originalController">The InventoryController this item originated from</param>
        /// <param name="originPoint">The point inside the inventory from which this item originated from</param>
        /// <param name="item">The item-instance that is being dragged</param>
        /// <param name="offset">The starting offset of this item</param>
        [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
        public InventoryDraggedItem(Canvas canvas, InventoryController originalController, Vector2Int originPoint, IInventoryItem item, Vector2 offset) {
            this.originalController = originalController;
            currentController       = this.originalController;
            this.originPoint        = originPoint;
            this.item               = item;

            _canvas     = canvas;
            _canvasRect = canvas.transform as RectTransform;

            _offset = offset;

            // Create an image representing the dragged item
            _image               = new GameObject("DraggedItem").AddComponent<Image>();
            _image.raycastTarget = false;
            _image.transform.SetParent(_canvas.transform);
            _image.transform.SetAsLastSibling();
            _image.transform.localScale = Vector3.one;
            _image.sprite               = item.sprite;
            _image.SetNativeSize();
        }

        /// <summary>
        /// Gets or sets the position of this dragged item
        /// </summary>
        public Vector2 position {
            set {
                // Move the image
                var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                                 ? null
                                 : _canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, value + _offset, camera, out var newValue);

                if (_image is null) Debug.Log("image is null");
                if (_image.rectTransform is null) Debug.Log("rect transform is null");
                if (_image.rectTransform != null) {
                    _image.rectTransform.localPosition = newValue;
                    
                }
                else {
                    Debug.Log($"null rect: image: {_image.transform.name}");
                }


                // Make selections
                if (currentController != null) {
                    item.position = currentController.ScreenToGrid(value + _offset + GetDraggedItemOffset(currentController.inventoryRenderer, item));
                    bool canAdd = currentController.inventory.CanAddAt(item, item.position) || CanSwap();

                    currentController.inventoryRenderer.SelectItem(item, !canAdd, CanStack(), Color.white, ref _image);
                }

                // Slowly animate the item towards the center of the mouse pointer
                _offset = Vector2.Lerp(_offset, Vector2.zero, Time.deltaTime * 10f);
            }
        }

        private bool CanStack() {
            var overlaped = currentController.inventoryRenderer.inventory.allItems.FirstOrDefault(x => item.Overlaps(x));
            return (overlaped != null && overlaped.ItemName == item.ItemName && overlaped.Quantity < overlaped.maxQuantity);
        }

        /// <summary>
        /// Drop this item at the given position
        /// </summary>
        public DropMode Drop(Vector2 pos) {
            DropMode mode;
            if (currentController != null) {
                var grid = currentController.ScreenToGrid(pos + _offset + GetDraggedItemOffset(currentController.inventoryRenderer, item));

                // Try to add a new item
                if (currentController.inventory.CanAddAt(item, grid)) {
                    currentController.inventory.TryAddAt(item, grid); // Place the item in a new location
                    mode = DropMode.Added;
                }
                // Adding did not work, try to swap
                else if (CanSwap()) {
                    var otherItem = currentController.inventory.allItems[0];
                    currentController.inventory.TryRemove(otherItem);
                    originalController.inventory.TryAdd(otherItem);
                    currentController.inventory.TryAdd(item);
                    mode = DropMode.Swapped;
                }
                // Could not add or swap, return the item
                else {
                    //probable stack here

                    mode = DropMode.Returned;

                    if (item.Stackable) {
                        var overlaped = currentController.inventoryRenderer.inventory.allItems.FirstOrDefault(x => item.Overlaps(x));

                        if (overlaped != null) {
                            
                            // Ensure both are the same item type
                            if (overlaped.ItemName == item.ItemName) {
                                // Only proceed if the overlapped stack isn't full
                                if (overlaped.Quantity < overlaped.maxQuantity) {
                                    // Calculate how much we can add
                                    int spaceLeft = overlaped.maxQuantity - overlaped.Quantity;
                                    // Skip if no space left
                                    if (spaceLeft <= 0) {
                                        Debug.Log("no space left");
                                        originalController.inventory.TryAddAt(item, originPoint);

                                        return DropMode.Returned;
                                    }

                                    // Determine how much to transfer from the current item
                                    int transferAmount = Mathf.Min(spaceLeft, item.Quantity);

                                    // Apply the transfer
                                    overlaped.Quantity += transferAmount;
                                    item.Quantity      -= transferAmount;

                                   
                                    mode = DropMode.QuantityChanged;

                                    // If the source item is now empty, remove it. Otherwise, return it to its original slot.
                                    if (item.Quantity <= 0) {
                                        originalController.inventoryRenderer.inventory.TryDrop(item);
                                    }
                                    else {
                                        // The dragged item still has quantity, so we need to put it back where it came from.
                                        // This was the missing piece causing the item to disappear.
                                        originalController.inventory.TryAddAt(item, originPoint);
                                    }
                                    
                                    // Optionally, refresh the UI
                                    currentController.inventoryRenderer.RefreshItem(item);
                                    currentController.inventoryRenderer.RefreshItem(overlaped);
                                }
                                else {
                                    
                                   
                                     originalController.inventory.TryAddAt(item, originPoint); // Return the item to its previous location
                                     mode = DropMode.Returned;
                                    
                                }
                            }
                        }
                    }
                    else {
                        originalController.inventory.TryAddAt(item, originPoint); // Return the item to its previous location
                        mode = DropMode.Returned;
                    }
                }

                currentController.inventoryRenderer.ClearSelection();
            }
            else {
                // originalController.inventory.TryAddAt(item, originPoint); // Return the item to its previous location
                // mode = DropMode.Returned;

                mode = DropMode.Dropped;
                if (!originalController.inventory.TryForceDrop(item)) // Drop the item on the ground
                {
                    originalController.inventory.TryAddAt(item, originPoint);
                }
            }

            // Destroy the image representing the item
            Object.Destroy(_image.gameObject);

            return mode;
        }

        /*
         * Returns the offset between dragged item and the grid
         */
        private Vector2 GetDraggedItemOffset(InventoryRenderer renderer, IInventoryItem item) {
            var scale = new Vector2(Screen.width / _canvasRect.sizeDelta.x, Screen.height / _canvasRect.sizeDelta.y);
            var gx    = -(item.width * renderer.cellSize.x / 2f) + (renderer.cellSize.x / 2);
            var gy    = -(item.height * renderer.cellSize.y / 2f) + (renderer.cellSize.y / 2);
            return new Vector2(gx, gy) * scale;
        }

        /*
         * Returns true if it is possible to swap
         */
        private bool CanSwap() {
            if (!currentController.inventory.CanSwap(item)) return false;
            var otherItem = currentController.inventory.allItems[0];
            return originalController.inventory.CanAdd(otherItem) && currentController.inventory.CanRemove(otherItem);
        }
    }
}