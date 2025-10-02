using System;
using System.Collections.Generic;
using Exile.Inventory;
using Exile.Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;



namespace Exile.Inventory {
    /// <summary>
    /// Renders a given inventory
    /// /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InventoryRenderer : MonoBehaviour {
        [FoldoutGroup("Grid Sprites")] [SerializeField, Tooltip("The sprite to use for empty cells")] private Sprite _cellSpriteEmpty = null;

        [FoldoutGroup("Grid Sprites")] [SerializeField, Tooltip("The sprite to use for selected cells")] private Sprite _cellSpriteSelected = null;

        [FoldoutGroup("Grid Sprites")] [SerializeField, Tooltip("The sprite to use for blocked cells")] private Sprite _cellSpriteBlocked = null;

        [FoldoutGroup("Grid Sprites")] [SerializeField, Tooltip("The sprite to use for used cells")]   private Sprite _cellSpriteUsed   = null;
        [FoldoutGroup("Grid Sprites")] [SerializeField, Tooltip("The sprite to use for shadow cells")] private Sprite _cellSpriteShadow = null;


        
        public GlobalDataSO GlobalDataSo;
        
       
        internal IInventoryManager                inventory;
        InventoryRenderMode                       _renderMode;
        private bool                              _haveListeners;
        private Pool<Image>                       _imagePool;
        private Image[]                           _grids;
        private Dictionary<IInventoryItem, Image> _items = new Dictionary<IInventoryItem, Image>();

        [FoldoutGroup("DBUG")] public Vector3 topLeft;
        [FoldoutGroup("DBUG")] public float   yoffset = 1f;

        Dictionary<IInventoryItem, Image> CellShadows = new Dictionary<IInventoryItem, Image>();

        /*
         * Setup
         */
        void Awake() {
            rectTransform = GetComponent<RectTransform>();


            var imageContainer = new GameObject("Image Pool").AddComponent<RectTransform>();
            imageContainer.transform.SetParent(transform);
            imageContainer.transform.localPosition = Vector3.zero;
            imageContainer.transform.localScale    = Vector3.one;

            // Create pool of images
            _imagePool = new Pool<Image>(delegate {
                var image = new GameObject("Image").AddComponent<Image>();
                image.transform.SetParent(imageContainer);
                image.transform.localScale = Vector3.one;
                image.color                = new Color(1f, 1f, 1f, 1f);
                return image;
            });
        }

        private void Start() { }

        /// <summary>
        /// Set what inventory to use when rendering
        /// </summary>
        public void SetInventory(IInventoryManager inventoryManager, InventoryRenderMode renderMode) {
            OnDisable();
            inventory   = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _renderMode = renderMode;
            OnEnable();
        }

        public IInventoryManager GetInventoryManager() => inventory;

        /// <summary>
        /// Returns the RectTransform for this renderer
        /// </summary>
        public RectTransform rectTransform;

        /// <summary>
        /// Returns the size of this inventory's cells
        /// </summary>
        [HideInInspector] public Vector2 cellSize;

        /*
        Invoked when the inventory inventoryRenderer is enabled
        */
        void OnEnable() {
            if (inventory != null && !_haveListeners) {
                if (_cellSpriteEmpty == null) {
                    throw new NullReferenceException("Sprite for empty cell is null");
                }

                if (_cellSpriteSelected == null) {
                    throw new NullReferenceException("Sprite for selected cells is null.");
                }

                if (_cellSpriteBlocked == null) {
                    throw new NullReferenceException("Sprite for blocked cells is null.");
                }

                inventory.onRebuilt     += ReRenderAllItems;
                inventory.onItemAdded   += HandleItemAdded;
                inventory.onItemRemoved += HandleItemRemoved;
                inventory.onItemDropped += HandleItemRemoved;
                inventory.onResized     += HandleResized;
                _haveListeners          =  true;

                // Render inventory
                ReRenderGrid();
                ReRenderAllItems();
            }
        }

        /*
        Invoked when the inventory inventoryRenderer is disabled
        */
        void OnDisable() {
            if (inventory != null && _haveListeners) {
                inventory.onRebuilt     -= ReRenderAllItems;
                inventory.onItemAdded   -= HandleItemAdded;
                inventory.onItemRemoved -= HandleItemRemoved;
                inventory.onItemDropped -= HandleItemRemoved;
                inventory.onResized     -= HandleResized;
                _haveListeners          =  false;
            }
        }

        /*
        Clears and renders the grid. This must be done whenever the size of the inventory changes
        */
        private void ReRenderGrid() {
            // Clear the grid
            if (_grids != null) {
                for (var i = 0; i < _grids.Length; i++) {
                    _grids[i]
                        .gameObject.SetActive(false);
                    RecycleImage(_grids[i]);
                    _grids[i]
                        .transform.SetSiblingIndex(i);
                }

                foreach (var cell_shadows in CellShadows) {
                    RecycleImage(cell_shadows.Value);
                }

                CellShadows.Clear();
            }

            _grids = null;

            // Render new grid
            var   containerSize = new Vector2(cellSize.x * inventory.width, cellSize.y * inventory.height);
            Image grid;
            switch (_renderMode) {
                case InventoryRenderMode.Single:
                    grid = CreateImage(_cellSpriteEmpty, true);
                    grid.rectTransform.SetAsFirstSibling();
                    grid.type                        = Image.Type.Sliced;
                    grid.rectTransform.localPosition = Vector3.zero;
                    grid.rectTransform.sizeDelta     = containerSize;
                    _grids                           = new[] { grid };

                    // Check if single cell is used
                    grid.sprite = inventory.allItems.Length > 0
                                      ? _cellSpriteUsed
                                      : _cellSpriteEmpty;
                    break;
                case InventoryRenderMode.Layered:

                    topLeft = new Vector3(-containerSize.x / 2, -containerSize.y / 2 + yoffset, 0); // Calculate topleft corner


                    var halfCellSize = new Vector3(cellSize.x / 2, cellSize.y / 2, 0); // Calulcate cells half-size
                    _grids = new Image[inventory.width * inventory.height];
                    var c = 0;
                    for (int y = 0; y < inventory.height; y++) {
                        for (int x = 0; x < inventory.width; x++) {
                            grid                 = CreateImage(_cellSpriteEmpty, true);
                            grid.gameObject.name = "Grid " + c;
                            grid.rectTransform.SetAsFirstSibling();
                            grid.type = Image.Type.Simple;
                            //grid.rectTransform.localPosition =  topLeft + new Vector3(cellSize.x * ((inventory.width - 1) - x), cellSize.y * y, 0) +  halfCellSize;
                            // old   grid.rectTransform.localPosition = topLeft + new Vector3(cellSize.x * (inventory.width - 1 - x), cellSize.y * y, 0) + halfCellSize;
                            grid.rectTransform.localPosition = topLeft + new Vector3(cellSize.x * x, -cellSize.y * y, 0) + halfCellSize;
                            grid.rectTransform.sizeDelta     = cellSize;
                            _grids[c]                        = grid;
                            c++;
                        }
                    }

                    break;


                default:
                    topLeft      = new Vector3(-containerSize.x / 2, -containerSize.y / 2 + yoffset, 0);
                    halfCellSize = new Vector3(cellSize.x / 2, cellSize.y / 2, 0);
                    _grids       = new Image[inventory.width * inventory.height];
                    c            = 0;
                    for (int y = 0; y < inventory.height; y++) {
                        for (int x = 0; x < inventory.width; x++) {
                            grid                 = CreateImage(_cellSpriteEmpty, true);
                            grid.gameObject.name = "grid " + c;
                            grid.rectTransform.SetAsFirstSibling();
                            grid.type = Image.Type.Simple;

                            // FIXED: No coordinate flip
                            grid.rectTransform.localPosition = topLeft + new Vector3(cellSize.x * x, cellSize.y * y, 0) + halfCellSize;

                            grid.rectTransform.sizeDelta = cellSize;
                            _grids[c]                    = grid;
                            c++;
                        }
                    }

                    break;
            }

            // Set the size of the main RectTransform
            rectTransform.sizeDelta = containerSize;
        }

        /*
        Clears and renders all items
        */
        private void ReRenderAllItems() {
            // Clear all items
            foreach (var image in _items.Values) {
                image.gameObject.SetActive(false);
                RecycleImage(image);
            }

            _items.Clear();

            // Add all items
            foreach (var item in inventory.allItems) {
                HandleItemAdded(item);
            }
        }

        /*
        Handler for when inventory.OnItemAdded is invoked
        */

        private void HandleItemAdded(IInventoryItem item) {
            // add shadows
            // they need to be added first so they are under the item
            if (CellShadows.ContainsKey(item)) {
                var shad = CellShadows[item];
                shad.rectTransform.localPosition = GetItemOffset(item);
                shad.rectTransform.sizeDelta     = new Vector2(item.width * cellSize.x, item.height * cellSize.y);
                shad.rectTransform.SetAsLastSibling();
            }
            else {
                // --- Create drop shadow for the whole item ---
                var shadow = CreateShadowImage(_cellSpriteShadow, false);
                shadow.name = "Shadow_" + item.ItemName;
                // Position shadow at item offset
                shadow.rectTransform.localPosition = GetItemOffset(item);
                // Scale to cover all occupied cells

                shadow.rectTransform.sizeDelta = new Vector2(item.width * cellSize.x, item.height * cellSize.y);


                //Style it like a drop shadow
                switch (item.ItemTier) {
                    case ItemTier.Common:
                        shadow.color = GlobalDataSo.itemVariationColor_common;
                        break;
                    case ItemTier.Uncommon:
                        shadow.color = GlobalDataSo.itemVariationColor_uncommon;
                        break;
                    case ItemTier.Rare:
                        shadow.color = GlobalDataSo.itemVariationColor_rare;
                        break;
                    case ItemTier.Epic:
                        shadow.color = GlobalDataSo.itemVariationColor_epic;
                        break;
                    case ItemTier.Legendary:
                        shadow.color = GlobalDataSo.itemVariationColor_legendary;
                        break;
                    case ItemTier.Mythic:
                        shadow.color = GlobalDataSo.itemVariationColor_mythic;
                        break;
                    case ItemTier.Exotic:
                        shadow.color = GlobalDataSo.itemVariationColor_exotic;
                        break;
                    default:
                        shadow.color = GlobalDataSo.itemVariationColor_common;
                        break;
                }


                shadow.rectTransform.SetAsLastSibling(); // send shadow behind item
                CellShadows.Add(item, shadow);
            }


            var img = CreateImage(item.sprite, false);

            if (_renderMode == InventoryRenderMode.Single) {
                img.rectTransform.localPosition = rectTransform.rect.center;
                UpdateCellSprite(Vector2Int.zero, true);
            }
            else {
                img.rectTransform.localPosition = GetItemOffset(item);


                // Update all cells this item occupies
                for (int x = 0; x < item.width; x++) {
                    for (int y = 0; y < item.height; y++) {
                        if (item.IsPartOfShape(new Vector2Int(x, y))) {
                            var pos = item.position + new Vector2Int(x, y);
                            UpdateCellSprite(pos, true);
                        }
                    }
                }
            }

            _items.Add(item, img);
        }

        /*
        Handler for when inventory.OnItemRemoved is invoked
        */
        private void HandleItemRemoved(IInventoryItem item) {
            if (_items.ContainsKey(item)) {
                var image = _items[item];
                image.gameObject.SetActive(false);
                RecycleImage(image);
                _items.Remove(item);

                // Reset cells to empty
                if (_renderMode == InventoryRenderMode.Single) {
                    UpdateCellSprite(Vector2Int.zero, false);
                }
                else {
                    for (int x = 0; x < item.width; x++) {
                        for (int y = 0; y < item.height; y++) {
                            if (item.IsPartOfShape(new Vector2Int(x, y))) {
                                var pos = item.position + new Vector2Int(x, y);
                                UpdateCellSprite(pos, false);
                            }
                        }
                    }
                }

                if (CellShadows.ContainsKey(item)) {
                    RemoveItemShadow(item);
                }
            }


            //Remove Item Shadows
        }

        private void RemoveItemShadow(IInventoryItem item) {
            if (CellShadows.ContainsKey(item)) {
                var shadow = CellShadows[item];
                shadow.gameObject.SetActive(false);
                RecycleImage(shadow);
                CellShadows.Remove(item);
            }
        }

        // 3. Fix UpdateCellSprite in InventoryRenderer.cs:
        private void UpdateCellSprite(Vector2Int cellPos, bool isOccupied) {
            if (cellPos.x < 0 || cellPos.x >= inventory.width || cellPos.y < 0 || cellPos.y >= inventory.height)
                return;

            // FIXED: No coordinate flip
            int index = cellPos.y * inventory.width + cellPos.x;
            _grids[index].sprite = isOccupied
                                       ? _cellSpriteUsed
                                       : _cellSpriteEmpty;
        }

        /*
        Handler for when inventory.OnResized is invoked
        */
        private void HandleResized() {
            ReRenderGrid();
            ReRenderAllItems();
        }

        /*
         * Create an image with given sprite and settings
         */
        private Image CreateImage(Sprite sprite, bool raycastTarget) {
            var img = _imagePool.Take();
            img.gameObject.SetActive(true);
            img.sprite                  = sprite;
            img.rectTransform.sizeDelta = new Vector2(img.sprite.rect.width, img.sprite.rect.height);
            img.transform.SetAsLastSibling();
            img.type          = Image.Type.Simple;
            img.raycastTarget = raycastTarget;
            img.color         = Color.white;
            return img;
        }

        private Image CreateShadowImage(Sprite sprite, bool raycastTarget) {
            var img = _imagePool.Take();
            img.gameObject.SetActive(true);
            img.sprite                  = sprite;
            img.rectTransform.sizeDelta = new Vector2(img.sprite.rect.width, img.sprite.rect.height);
            img.transform.SetAsLastSibling();
            img.type          = Image.Type.Simple;
            img.raycastTarget = raycastTarget;
            return img;
        }

        /*
         * Recycles given image
         */
        private void RecycleImage(Image image) {
            image.gameObject.name = "Image";
            image.gameObject.SetActive(false);
            _imagePool.Recycle(image);
        }

        /// <summary>
        /// Selects a given item in the inventory
        /// </summary>
        /// <param name="item">Item to select</param>
        /// <param name="blocked">Should the selection be rendered as blocked</param>
        /// <param name="color">The color of the selection</param>
        /// <summary>
        /// Selects a given item in the inventory
        /// </summary>
        /// <param name="item">Item to select</param>
        /// <param name="blocked">Should the selection be rendered as blocked</param>
        /// <param name="color">The color of the selection</param>
        public void SelectItem(IInventoryItem item, bool blocked, Color color) {
            if (item == null) {
                return;
            }

            ClearSelection();

            switch (_renderMode) {
                case InventoryRenderMode.Single:
                    _grids[0].sprite = blocked
                                           ? _cellSpriteBlocked
                                           : _cellSpriteSelected;
                    _grids[0].color = color;
                    break;
                default:
                    for (var x = 0; x < item.width; x++) {
                        for (var y = 0; y < item.height; y++) {
                            if (item.IsPartOfShape(new Vector2Int(x, y))) {
                                var p = item.position + new Vector2Int(x, y);
                                if (p.x >= 0 && p.x < inventory.width && p.y >= 0 && p.y < inventory.height) {
                                    // FIXED: Use same index calculation as UpdateCellSprite and ReRenderGrid
                                    var index = p.y * inventory.width + p.x;
                                    _grids[index].sprite = blocked
                                                               ? _cellSpriteBlocked
                                                               : _cellSpriteSelected;
                                    _grids[index].color = color;
                                }
                            }
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Clears all selections made in this inventory
        /// </summary>
        public void ClearSelection() {
            for (var i = 0; i < _grids.Length; i++) {
                if (_grids[i].sprite != _cellSpriteUsed) {
                    _grids[i].sprite = _cellSpriteEmpty;
                    _grids[i].color  = Color.white;
                }
            }


            ReRenderAllItems();
        }

        /*
        Returns the appropriate offset of an item to make it fit nicely in the grid
        */
        internal Vector2 GetItemOffsetx(IInventoryItem item) {
            var x = (-(inventory.width * 0.5f) + item.position.x + item.width * 0.5f) * cellSize.x;
            var y = (-(inventory.height * 0.5f) + item.position.y + item.height * 0.5f) * cellSize.y;
            return new Vector2(x, y);
        }

        internal Vector2 GetItemOffset(IInventoryItem item) {
            // FIXED: Match the grid positioning logic exactly
            float x = (-(inventory.width * 0.5f) + item.position.x + item.width * 0.5f) * cellSize.x;
            float y = (-(inventory.height * 0.5f) + item.position.y + item.height * 0.5f) * cellSize.y;

            // Apply the same yoffset as used in grid positioning
            return new Vector2(x, y + yoffset);
        }
    }
}