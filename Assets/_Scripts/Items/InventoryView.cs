using System;
using System.Collections.Generic;
using core.player;
using Exile.Inventory;
using Exile.Inventory.Examples;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Exile.Inventory {
    public enum inv_type {
        tab,
        player_hand,
        ground
    }

    public class InventoryView : MonoBehaviour {
        [FoldoutGroup("References")] [SerializeField] private inventoryIconPlaceHolder iconPlaceHolder;
        [FoldoutGroup("References")]                  public  InventoryRenderer        _iv_render;
        [FoldoutGroup("References")]                  public  RectTransform            mainrect;
        [FoldoutGroup("References")]                  public  RectTransform[]          parentrects;
        [FoldoutGroup("References")]                  public  TMP_Text                 inventoryViewTMp_Name;
        [FoldoutGroup("References")]                  public  Image                    itemImage;

        [FoldoutGroup("Inventory Settings")] public InventoryManager    InventoryManager;
        [FoldoutGroup("Inventory Settings")] public InventoryRenderMode _renderMode         = InventoryRenderMode.Grid;
        [FoldoutGroup("Inventory Settings")] public bool                _allow_adding_items = true;
        [FoldoutGroup("Inventory Settings")] public ItemType            AllowedItemType     = ItemType.Any;

        [FoldoutGroup("Grid Settings")]                                                                      public  int           cellsize                 = 60;
        [FoldoutGroup("Grid Settings")] [ShowIf("_renderMode", InventoryRenderMode.Single)] [SerializeField] private bool          UseRectForSingleGridSize = false;
        [FoldoutGroup("Grid Settings")] [Range(1, 6)]                                                        public  int           w                        = 5; // columns
        [FoldoutGroup("Grid Settings")] [Range(1, 30)]                                                       public  int           h                        = 4; // rows
        [FoldoutGroup("Grid Settings")]                                                                      public  int           incrementHeight          = 10;
        [FoldoutGroup("Grid Settings")]                                                                      public  int           incrementWidh            = 10;
        [FoldoutGroup("Grid Settings")] [SerializeField]                                                     private bool          RespectPanelWidth        = false;
        [FoldoutGroup("Grid Settings")]                                                                      public  RectTransform PanelRect;

        [BoxGroup("Debug")] [FoldoutGroup("Debug/Test")] public bool           test_AutoCreate = true;
        [FoldoutGroup("Debug/Test")]                     public List<ItemBase> items           = new List<ItemBase>();

        internal bool isActive = false;

        private void Start() {
            if (test_AutoCreate)
                createtab();
        }

        public InventoryManager CreateInventoryView(ref ItemBase item) {
            if (item._iscontainer) {
                h = item.container_shape.height;
                w = item.container_shape.width;
                return CreateInventoryView(item, _renderMode, AllowedItemType = ItemType.Any);
            }

            return null;
        }

        public InventoryManager CreateInventoryView(IInventoryItem item, InventoryRenderMode _render, ItemType _alloweditems = ItemType.Any) {
            _iv_render = GetComponentInChildren<InventoryRenderer>();
            if (_iv_render == null) {
                _iv_render = GetComponent<InventoryRenderer>();
            }

            if (_iv_render == null) {
                Debug.LogError("InventoryRenderer not found on this GameObject or its children.", this);
                return null;
            }

            if (item != null) {
                inventoryViewTMp_Name.text = item.ItemName;
                itemImage.enabled          = true;
                itemImage.sprite           = item.sprite;
            }

            int actualWidth  = w;
            int actualHeight = h;


            switch (_render) {
                case InventoryRenderMode.Single:
                    actualWidth  = 1;
                    actualHeight = 1;

                    Vector2 finalSlotSize;

                    if (UseRectForSingleGridSize && parentrects.Length > 0) {
                        float slotWidth  = parentrects[0].sizeDelta.x - incrementWidh;
                        float slotHeight = parentrects[0].sizeDelta.y - incrementHeight;
                        finalSlotSize      = new Vector2(slotWidth, slotHeight);
                        mainrect.sizeDelta = finalSlotSize;
                        foreach (var r in parentrects) {
                            r.sizeDelta = new Vector2(finalSlotSize.x + incrementWidh, finalSlotSize.y + incrementHeight);
                        }
                    }
                    else {
                        finalSlotSize      = new Vector2(cellsize * w, cellsize * h);
                        mainrect.sizeDelta = finalSlotSize;
                        foreach (var r in parentrects) {
                            r.sizeDelta = new Vector2(mainrect.sizeDelta.x + incrementWidh, mainrect.sizeDelta.y + incrementHeight);
                        }
                    }

                    _iv_render.cellSize = finalSlotSize;
                    break;
                case InventoryRenderMode.Grid:

                    mainrect.sizeDelta = new Vector2(cellsize * w, cellsize * h);
                    foreach (var r in parentrects) {
                        if (mainrect.sizeDelta.x + incrementWidh < PanelRect.sizeDelta.x && RespectPanelWidth) {
                            r.sizeDelta = new Vector2(PanelRect.sizeDelta.x, mainrect.sizeDelta.y + incrementHeight);
                        }
                        else {
                            r.sizeDelta = new Vector2(mainrect.sizeDelta.x + incrementWidh, mainrect.sizeDelta.y + incrementHeight);
                        }
                    }
                    
                    
                    _iv_render.cellSize = new Vector2(cellsize, cellsize);
                    break;
            }


            var prov = new InventoryProvider(_render,
                                             _render == InventoryRenderMode.Single
                                                 ? 1
                                                 : -1,
                                             _alloweditems);
            InventoryManager = new InventoryManager(prov, actualWidth, actualHeight, _allow_adding_items);
            _iv_render.SetInventory(InventoryManager, _render);

            if (iconPlaceHolder != null) {
                iconPlaceHolder.SetupEvents(InventoryManager);
            }


            InventoryManager.onItemAdded   += OnItemAdded;
            InventoryManager.onItemRemoved += OnItemRemoved;


            return InventoryManager;
        }

        private void OnItemAdded(IInventoryItem item) { }

        private void OnItemRemoved(IInventoryItem item) { }

        public void AddItem(ItemBase _item) {
            var it = _item.CreateInstance();
            if (InventoryManager != null) {
                if (InventoryManager.TryAddWithRotation(it)) { }
            }
            else Debug.LogError("mgr err");
        }

        [Button("Create TAb")]
        public void createtab() {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            CreateInventoryView(null, _renderMode, AllowedItemType);
        }

        [Button("Clear")]
        private void Clear() {
            if (InventoryManager != null) {
                if (_renderMode == InventoryRenderMode.Single) {
                    InventoryManager.Resize(1, 1);
                }
                else {
                    InventoryManager.Resize(w, h);
                }

                _iv_render.ClearAllItemInfoOverlays();
                InventoryManager.Rebuild();
            }
        }

        [Button("Add random item")]
        public void addrandom() {
            foreach (var it in items) {
                AddItem(it);
            }
        }

        public void ResetView() {
            if (InventoryManager != null) {
                InventoryManager.onItemAdded   -= OnItemAdded;
                InventoryManager.onItemRemoved -= OnItemRemoved;
                InventoryManager.Clear();   // Clear all items from the inventory
                InventoryManager.Dispose(); // Dispose of the inventory manager resources
                InventoryManager = null;
            }

            if (_iv_render != null) {
                _iv_render.ClearAllItemInfoOverlays();
                _iv_render.SetInventory(null, InventoryRenderMode.Grid); // Clear the renderer
            }

            // Reset UI elements
            if (inventoryViewTMp_Name != null) inventoryViewTMp_Name.text = "";
            if (itemImage != null) {
                itemImage.enabled = false;
                itemImage.sprite  = null;
            }

            // Reset to default dimensions or a known initial state
            w                   = 5;                        // Default width
            h                   = 4;                        // Default height
            _renderMode         = InventoryRenderMode.Grid; // Default render mode
            _allow_adding_items = true;
            AllowedItemType     = ItemType.Any;

            // Deactivate the GameObject itself, handled by the pool manager
            gameObject.SetActive(false);
        }
    }
}