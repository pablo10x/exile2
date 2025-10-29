using System;
using System.Collections.Generic;
using core.Managers;
using DG.Tweening;
using Exile.Inventory;
using Exile.Shared;
using Sirenix.OdinInspector;
using TMPro;
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

        Dictionary<IInventoryItem, GameObject> ItemInfoOverlays = new Dictionary<IInventoryItem, GameObject>();
        private Pool<GameObject>               _overlayPool;

        // In Awake(), after creating _imagePool, add:
        private void InitializeOverlayPool() {
            var overlayContainer = new GameObject("Overlay Pool").AddComponent<RectTransform>();
            overlayContainer.transform.SetParent(transform);
            overlayContainer.transform.localPosition = Vector3.zero;
            overlayContainer.transform.localScale    = Vector3.one;

            _overlayPool = new Pool<GameObject>(delegate {
                var overlayObj    = new GameObject("ItemOverlay");
                var rectTransform = overlayObj.AddComponent<RectTransform>();
                rectTransform.SetParent(overlayContainer);
                rectTransform.localScale = Vector3.one;

                // Create background image
                // var bgImage = overlayObj.AddComponent<Image>();
                // bgImage.color         = new Color(0, 0, 0, 0.2f); // Semi-transparent black background
                // bgImage.raycastTarget = false;

                // Create text for item name
                
                
                //todo fix batching due to material change
                var textObj  = new GameObject("ItemName");
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.SetParent(rectTransform);
                textRect.localScale       = Vector3.one;
                textRect.anchorMin        = new Vector2(0, 1); // Top-left anchor
                textRect.anchorMax        = new Vector2(0, 1);
                textRect.pivot            = new Vector2(0, 1);
                textRect.anchoredPosition = new Vector2(5, -5); // 5px padding from top-left
                var text = textObj.AddComponent<TextMeshProUGUI>();
                text.fontSize           = 14;
                text.color              = Color.white;
                text.alignment          = TextAlignmentOptions.TopLeft;
                text.raycastTarget      = false;
                text.textWrappingMode = TextWrappingModes.PreserveWhitespace;
                text.material = GameManager.Instance.GlobalConfig.FontTextMaterial;
                text.outlineWidth = 0.3f;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.fontSize     = 15f;




                // Create progress bar container at bottom
                var progressBarObj  = new GameObject("ProgressBar");
                var progressBarRect = progressBarObj.AddComponent<RectTransform>();
                progressBarRect.SetParent(rectTransform);
                progressBarRect.localScale       = Vector3.one;
                progressBarRect.anchorMin        = new Vector2(0, 0); // Bottom-left anchor
                progressBarRect.anchorMax        = new Vector2(1, 0); // Bottom-right anchor
                progressBarRect.pivot            = new Vector2(0.5f, 0);
                progressBarRect.anchoredPosition = new Vector2(0, 5);   // 5px padding from bottom
                progressBarRect.sizeDelta        = new Vector2(-20, 3); // Full width minus 10px padding, 8px height

                // Progress bar background
                var progressBg = progressBarObj.AddComponent<Image>();
                progressBg.color         = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                progressBg.raycastTarget = false;

                // Progress bar fill
                var progressFillObj  = new GameObject("ProgressFill");
                var progressFillRect = progressFillObj.AddComponent<RectTransform>();
                progressFillRect.SetParent(progressBarRect);
                progressFillRect.localScale       = Vector3.one;
                progressFillRect.anchorMin        = new Vector2(0, 0);
                progressFillRect.anchorMax        = new Vector2(0, 1);
                progressFillRect.pivot            = new Vector2(0, 0.5f);
                progressFillRect.anchoredPosition = Vector2.zero;
                progressFillRect.sizeDelta        = new Vector2(0, 0); // Will be set dynamically

                var progressFill = progressFillObj.AddComponent<Image>();
                progressFill.color         = new Color(0.34f, 0.68f, 0.34f, 0.9f); // Green fill
                progressFill.raycastTarget = false;
                
                
                
                overlayObj.SetActive(false);
                return overlayObj;
            });
        }

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

            InitializeOverlayPool();
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

                ClearAllItemInfoOverlays();
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

        private void SetSpritesShadows(IInventoryItem item) {
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
                    // case ItemTier.Common:
                    //     shadow.color = GlobalDataSo.itemVariationColor_common;
                    //     break;
                    // case ItemTier.Uncommon:
                    //     shadow.color = GlobalDataSo.itemVariationColor_uncommon;
                    //     break;
                    // case ItemTier.Rare:
                    //     shadow.color = GlobalDataSo.itemVariationColor_rare;
                    //     break;
                    // case ItemTier.Epic:
                    //     shadow.color = GlobalDataSo.itemVariationColor_epic;
                    //     break;
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
        }

        private void HandleItemAdded(IInventoryItem item) {
            // SetSpritesShadows(item);


            var img = CreateImage(item.sprite, false);

            // if (_renderMode == InventoryRenderMode.Single) {
            //     img.rectTransform.localPosition = rectTransform.rect.center;
            //     UpdateCellSprite(Vector2Int.zero, true);
            // }
            // else {
            //     img.rectTransform.localPosition = GetItemOffset(item);
            //
            //
            //     // Update all cells this item occupies
            //     for (int x = 0; x < item.width; x++) {
            //         for (int y = 0; y < item.height; y++) {
            //             if (item.IsPartOfShape(new Vector2Int(x, y))) {
            //                 var pos = item.position + new Vector2Int(x, y);
            //                 UpdateCellSprite(pos, true);
            //             }
            //         }
            //     }
            // }

            if (_renderMode == InventoryRenderMode.Single) {
                img.rectTransform.localPosition = rectTransform.rect.center;
                UpdateCellSprite(Vector2Int.zero, true);
            }
            else {
                //hide all sprites that covered by this item
                for (int x = 0; x < item.width; x++) {
                    for (int y = 0; y < item.height; y++) {
                        if (item.IsPartOfShape(new Vector2Int(x, y))) {
                            var pos = item.position + new Vector2Int(x, y);
                            UpdateCellSprite(pos, true);
                            //HideGridSprite(new Vector2Int(x, y));
                        }
                    }
                }

                img.rectTransform.localPosition = GetItemOffset(item);
                // img.rectTransform.sizeDelta = new Vector2(item.width * cellSize.x, item.height * cellSize.y);


                if (item.Rotated) {
                    img.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
                    img.rectTransform.sizeDelta     = new Vector2(item.height * cellSize.y / 1.3f, item.width * cellSize.x / 1.3f);
                }
                else {
                    img.rectTransform.localRotation = Quaternion.Euler(0, 0, 0f);
                    img.rectTransform.sizeDelta     = new Vector2(item.width * cellSize.x / 1.3f, item.height * cellSize.y / 1.3f);
                }


                
            }

            SetItemInfoOverlay(item);
            _items.Add(item, img);
        }

        private void HideGridSprite(Vector2Int cellPos) {
            int index = cellPos.y * inventory.width + cellPos.x;
            _grids[index].enabled = false;
        }

        /*
        Handler for when inventory.OnItemRemoved is invoked
        */
        private void HandleItemRemoved(IInventoryItem item) {
            if (_items.ContainsKey(item)) {
                RemoveItemInfoOverlay(item);
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

            //try to change color instead of switching sprites and use dotween for linear smooth transition


            if (isOccupied) {
                _grids[index].sprite = _cellSpriteUsed;
                // _grids[index].color = new Color(1f, 1f, 1f, 0f);
                // _grids[index]
                //     .DOFade(0.9f, 5.5f);
            }
            else {
                _grids[index].sprite = _cellSpriteEmpty;
                // _grids[index].color = new Color(1f, 1f, 1f, 0f);
                // // _grids[index].color = Color.white;
                // _grids[index]
                //     .DOFade(0.9f, 0.5f);
            }


            // _grids[index].sprite = isOccupied
            //                            ? _cellSpriteUsed
            //                            : _cellSpriteEmpty;
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

        private void SetItemInfoOverlay(IInventoryItem item) {
            GameObject overlayObj;

            if (ItemInfoOverlays.ContainsKey(item)) {
                // Update existing overlay
                overlayObj = ItemInfoOverlays[item];
                overlayObj.SetActive(true);
            }
            else {
                // Create new overlay
                overlayObj = _overlayPool.Take();
                overlayObj.SetActive(true);
                ItemInfoOverlays.Add(item, overlayObj);
            }

            // Get components from the overlay object
            var overlayRectTransform = overlayObj.GetComponent<RectTransform>();
            var itemNameText = overlayObj.GetComponentInChildren<TextMeshProUGUI>();
            var progressBarContainer = overlayObj.transform.Find("ProgressBar");
            var progressBarFillRect = progressBarContainer.Find("ProgressFill").GetComponent<RectTransform>();
            var progressBarFillImage = progressBarFillRect.GetComponent<Image>();

            // Position and size the overlay to match the item's occupied cells
            overlayRectTransform.localPosition = GetItemOffset(item);
            overlayRectTransform.sizeDelta = new Vector2(item.width * cellSize.x, item.height * cellSize.y);

            // Update text
            itemNameText.text = item.ItemName;

            // Calculate progress for the progress bar
            float progress = 0.8f;
            // if (item.Stackable && item.maxQuantity > 0) {
            //     progress = (float)item.Quantity / item.maxQuantity;
            // }
            // else {
            //     // If not stackable or maxQuantity is 0, hide the progress bar
            //     progressBarContainer.gameObject.SetActive(false);
            //     // No further progress bar updates needed
            //     overlayRectTransform.SetAsLastSibling();
            //     return;
            // }
            
            // Ensure progress bar is active if it's stackable
            progressBarContainer.gameObject.SetActive(true);

            // Set progress bar fill width
            var progressBarRect = progressBarContainer.GetComponent<RectTransform>();
            //float fullWidth = progressBarRect.sizeDelta.x;
            float fullWidth = overlayRectTransform.sizeDelta.x;
            Debug.Log(fullWidth);
            progressBarFillRect.sizeDelta = new Vector2(fullWidth * progress, 0);

            // Change progress bar fill color based on progress
            if (progress > 0.66f) {
                progressBarFillImage.color = new Color(0.74f, 1f, 0.69f, 0.9f); // Green
            }
            else if (progress > 0.33f) {
                progressBarFillImage.color = new Color(0.99f, 1f, 0.63f, 0.9f); // Yellow
            }
            else {
                progressBarFillImage.color = new Color(1, 0, 0, 0.9f); // Red
            }
            
            // // Optional: Color the background based on item tier
            // switch (item.ItemTier) {
            //     case ItemTier.Legendary:
            //         itemNameText.color = new Color(GlobalDataSo.itemVariationColor_legendary.r, GlobalDataSo.itemVariationColor_legendary.g, GlobalDataSo.itemVariationColor_legendary.b, 0.3f);
            //         break;
            //     case ItemTier.Mythic:
            //         itemNameText.color = new Color(GlobalDataSo.itemVariationColor_mythic.r, GlobalDataSo.itemVariationColor_mythic.g, GlobalDataSo.itemVariationColor_mythic.b, 0.3f);
            //         break;
            //     case ItemTier.Exotic:
            //         itemNameText.color = new Color(GlobalDataSo.itemVariationColor_exotic.r, GlobalDataSo.itemVariationColor_exotic.g, GlobalDataSo.itemVariationColor_exotic.b, 0.3f);
            //         break;
            //     default:
            //         itemNameText.color = new Color(0.89f, 0.89f, 0.89f, 0.5f);
            //         break;
            // }

            overlayRectTransform.SetAsLastSibling();
        }

// Add method to remove overlay:
        private void RemoveItemInfoOverlay(IInventoryItem item) {
            if (ItemInfoOverlays.ContainsKey(item)) {
                var overlayObj = ItemInfoOverlays[item];
                overlayObj.SetActive(false);
                _overlayPool.Recycle(overlayObj);
                ItemInfoOverlays.Remove(item);
            }
        }


        private void ClearAllItemInfoOverlays() {
            foreach (var overlay in ItemInfoOverlays.Values) {
                overlay.SetActive(false);
                _overlayPool.Recycle(overlay);
            }

            ItemInfoOverlays.Clear();
        }
    }
}