using System;
using Exile.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


namespace Inventory {
    public class UI_Inventorytab : MonoBehaviour {
        public InventoryRenderer _render;

        // refs
        //public TMP_Text tabname;
        public Image tabicon;


        public bool _fillEmpty = false;
        [FoldoutGroup("Config")] public int Cellsize = 50;
        [FoldoutGroup("Config")] public bool useRectScale = false;
        [FoldoutGroup("Config")] public int Height;
        [FoldoutGroup("Config")] public int Width;


        public InventoryManager currentAssignedManager;


        public RectTransform mainrect;
        public RectTransform parentrect;

        private void Start() {
            /*if (item_tocreate != null){
            CreateInventoryView(item_tocreate);
        }*/
        }


        public void Cleantab() {
            currentAssignedManager = null;
        }

        /// <summary>
        /// Creates Ui grid view based on a clothing item
        /// </summary>
        /// <param name="_item aka the container"></param>
        /// <returns>return gameobject of the ui view ( to be able to destroy )</returns>
        public GameObject CreateTab(ref ItemBase _item) {
            if (!_item.isContainer) {
                Debug.LogError("item is not container ");
                return null;
            }

            if (useRectScale) {
                Height = Convert.ToInt32(mainrect.rect.height / Cellsize);
                Width = Convert.ToInt32(mainrect.rect.width / Cellsize);
            }


            _render.cellSize = new Vector2(Cellsize, Cellsize);
            _render.SetInventory(_item.ContainerInventory, InventoryRenderMode.Layered);
            currentAssignedManager = _item.ContainerInventory;
            parentrect.sizeDelta = new Vector2(parentrect.rect.width, mainrect.rect.height + (Cellsize + 40));

            /*if (tabname != null) {
                tabname.text = _item.item_name + $" playerid <{_item.item_key.ToUpper()}>";
            }*/

            if (tabicon != null) {
                tabicon.sprite = _item.sprite;
            }

            currentAssignedManager.onRebuilt = () => {
                gameObject.SetActive(false);
                gameObject.SetActive(true);
            };


            return gameObject;
        }
    }
}