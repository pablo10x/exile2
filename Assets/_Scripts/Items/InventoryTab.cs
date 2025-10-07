using System;
using System.Collections.Generic;
using Exile.Inventory;
using Exile.Inventory.Examples;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Exile.Inventory {
    public enum inv_type {
        tab,
        player_hand,
        ground
    }

    public class InventoryTab : MonoBehaviour {
        [FoldoutGroup("items")] public List<ItemBase> itemz = new List<ItemBase>();

        // test
        public        InventoryManager    InventoryManager;
        public        string              Equipment_name;
        public        InventoryRenderMode _renderMode             = InventoryRenderMode.Grid;
        private const int                 _maximumAlowedItemCount = -1;
        public        bool                _allow_adding_items     = true;

        public                  int cellsize = 60;


        public                                             RectTransform     rect_;
        public                                             inv_type          TabType;
        public                                             RectTransform     mainrect;
        public                                             RectTransform[]     parentrects;
        [FoldoutGroup("Grid Size")] [Range(1, 30)] public int               w = 5; // columns
        [FoldoutGroup("Grid Size")] [Range(1, 30)] public int               h = 4; // rows
        public                                             InventoryRenderer _iv_render;

        [FormerlySerializedAs("incrementHeight")] public int incrementHeight = 10;
        [FormerlySerializedAs("incrementHeight")] public int incrementWidh = 10;

        public InventoryManager CreateTab(string _equipmentname, InventoryRenderMode _render, ItemType _alloweditems = ItemType.Any, inv_type _type = inv_type.tab) {

           
            _iv_render = GetComponentInChildren<InventoryRenderer>();
            _iv_render.cellSize = new Vector2(cellsize,cellsize);
            if (_iv_render == null) {
                _iv_render = GetComponent<InventoryRenderer>();
            }

            rect_ = _iv_render.gameObject.GetComponent<RectTransform>();

            _equipmentname      = Equipment_name;
            _iv_render.cellSize = new Vector2(cellsize, cellsize);

            // Set mainrect size based on columns (w) and rows (h)
            mainrect.sizeDelta   = new Vector2(cellsize * w, cellsize * h);
            foreach (var r in parentrects) {
                r.sizeDelta = new Vector2(mainrect.sizeDelta.x + incrementWidh, mainrect.sizeDelta.y + incrementHeight);
            }
           

            var prov = new InventoryProvider(_render, -1, _alloweditems);
            InventoryManager = new InventoryManager(prov, w, h, _allow_adding_items);

            _iv_render.SetInventory(InventoryManager, _render);
            return InventoryManager;
        }

        public void AddItem(ItemBase _item) {

            var it = _item.CreateInstance();
            if (InventoryManager != null) {
                if (InventoryManager.TryAdd(it)) {
                  
                }
               
            }
            else Debug.LogError("mgr err");
        }

        private void Start() {
            //   CreateTab("tab", rows, _renderMode,ItemType.Any,TabType);
        }

        [Button("Add random item")]
        public void addrandom() {
            foreach (var it in itemz) {
                AddItem(it);
            }
        }

        [Button("Create TAb")]
        public void createtab() {
         
            CreateTab("Equipment_name",  _renderMode, ItemType.Any, TabType);
        }

        /*void Starting()
    {

        _iv_render = GetComponentInChildren<InventoryRenderer>();
        rect_ = _iv_render.gameObject.GetComponent<RectTransform>();



        _iv_render.cellSize = new Vector2(cellsize, cellsize);

        mainrect.sizeDelta = new Vector2(mainrect.rect.width, cellsize * rows + 10);

        w = Convert.ToInt32(rect_.rect.width / cellsize);
        h = Convert.ToInt32(mainrect.rect.height / cellsize);


        mgr = new InventoryManager(
            new InventoryProvider(_renderMode, _maximumAlowedItemCount, EquipedItemType), w, h, _allow_adding_items);
        // Fill inventory with random items
        /*if (_fillRandomly)
        {
            var tries = (w * h) / 3;
            for (var i = 0; i < tries; i++)
            {
                mgr.TryAdd(_definitions[Random.Range(0, _definitions.Length)].CreateInstance());
            }
        }

        // Fill empty slots with first (1x1) item
        if (_fillEmpty)
        {
            for (var i = 0; i < w * h; i++)
            {
                mgr.TryAdd(_definitions[0].CreateInstance());
            }
        }

        // Sets the renderers's inventory to trigger drawing
        _iv_render.SetInventory(mgr, _renderMode);

        // Log items being dropped on the ground
        mgr.onItemDropped += (item) =>
        {
            if (item.canDrop && GameManager.Instance != null)
            {
                var pl = GameManager.Instance.OnLinePlayer;
                //var vec = new Vector3(pl.gameObject.transform.position.x,pl.gameObject.transform.position.y +2f,pl.gameObject.transform.position.z);
                Vector3 spawnpos = pl.Motor.Transform.position + pl.Motor.CharacterForward * 0.8f;
                var go = Instantiate(item.prefab, spawnpos, Quaternion.Euler(new Vector3(0, 0, 90)),
                    null);
            }
        };

        // Log when an item was unable to be placed on the ground (due to its canDrop being set to false)
        mgr.onItemDroppedFailed += (item) =>
        {
            //Debug.Log($"You're not allowed to drop {(item as ItemBase).Name} on the ground");
        };

        // Log when an item was unable to be placed on the ground (due to its canDrop being set to false)
        mgr.onItemAddedFailed += (item) => { Debug.Log($"You can't put {(item as ItemBase).name} there!"); };


        mgr.OnInventoryBlocked += (item) => { Debug.Log("You can't put items here"); };

        mgr.onItemAdded += (item) =>
        {
            Debug.Log("Item_added " + item.name);
            var socket = GameManager.Instance.ActivePlayer.GetComponent<PlayerInventory>();
            switch (InventoryType)
            {
                case inv_type.player_hand:
                {
                    //todo attach this weapon to player
                    if (item.Attachable)
                    {
                        Debug.Log("Item: " + item.name + " Added to player hand");
                        /*GameObject goitem = Instantiate(item.prefab, socket.Righ_hand.transform.position,Quaternion.identity) as GameObject;
                        //get attachable_item script from instantiated object
                        var att_item = goitem.GetComponent<Attachable_item>();
                        goitem.transform.parent = socket.Righ_hand.transform;
                        goitem.transform.localPosition = att_item.AttachPos;
                        goitem.transform.rotation = att_item.AttachRot;
                        goitem.transform.localScale = att_item.AttachScal;
                        socket.AddItemToPlayer(item, item.prefab);
                    }

                    break;
                }
            }
        };
    }*/

        [Button("Refresh")]
        private void refr() {
            InventoryManager.Resize(w, h);
            InventoryManager.Rebuild();
        }
    }
}