using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Items;
using core.Managers;
using core.ui;
using Exile.Inventory;
using Exile.Inventory.Examples;
using LeTai.Asset.TranslucentImage;
using Sirenix.OdinInspector;
using UnityEngine;

public class InventoryUIManager : Singleton<InventoryUIManager> {
    [SerializeField] private CanvasGroupToggler _mainInventoryCanvasGroupToggler;

    [SerializeField] [Required] private ItemDatabase itemDatabase;
    [SerializeField]            private uint         _MaxPoolSize = 15;

    [SerializeField] private Transform     PoolContainer;
    [SerializeField] private Transform     LootPoolContainer;
    [SerializeField] private InventoryView _inventoryViewPrefab;

    [FoldoutGroup("Equipments slots")] public InventoryView Headgear;
    [FoldoutGroup("Equipments slots")] public InventoryView Vest;
    [FoldoutGroup("Equipments slots")] public InventoryView Tshirt;
    [FoldoutGroup("Equipments slots")] public InventoryView Pants;
    [FoldoutGroup("Equipments slots")] public InventoryView Backpack;
    [FoldoutGroup("Equipments slots")] public InventoryView Shoes;

    private Dictionary<GameObject, InventoryView> _inventoryViews = new Dictionary<GameObject, InventoryView>();

    [FoldoutGroup("InventoryRenderingConfigs")] [SerializeField] private TranslucentImage _translucentImage;

    private void Awake() {
        Initialize();
    }

    private void Start() {
        Pants.InventoryManager.onItemAdded += item => { Debug.Log($"Detected item added on pants {item.ItemName}"); };

        GameManager.Instance.OnPlayerSpawned += () => { _translucentImage.source = GameManager.Instance.character.orbitCamera.GetComponent<TranslucentImageSource>(); };
    }

    void Initialize() {
        for (int i = 0; i < _MaxPoolSize; i++) {
            var ob = Instantiate(_inventoryViewPrefab, PoolContainer);
            ob.gameObject.SetActive(false);
            _inventoryViews.Add(ob.gameObject, ob);
        }
    }

    public void AssignItemToPlayerTab(ItemBase item) {
        if (item._iscontainer == false) return;
        InventoryView view = GetFreeTab();
        if (view == null) return;

        view._renderMode      = InventoryRenderMode.Grid;
        view.InventoryManager = item.ContainerInventory;


        view.CreateInventoryView(ref item);


        view.gameObject.SetActive(true);
        Debug.Log($"Created view for {item.name}");

        view.isActive = true;
    }

    public void AddContainerToLoot(NetItemContainer container) {
        InventoryView view = GetFreeTab();
        if (view == null) return;

        view.transform.SetParent(LootPoolContainer);
        view.gameObject.SetActive(true);
        view._renderMode      = InventoryRenderMode.Grid;
        view.InventoryManager = new InventoryManager(new InventoryProvider(InventoryRenderMode.Grid, -1, ItemType.Any), container.Width, container.Height, true); //(new InventoryProvider(InventoryRenderMode.Grid, -1, ItemType.Any))


        view.CreateInventoryView(ref container);

        // add items - these should be added AFTER the inventory view is fully created


        StartCoroutine(AddItemsDelayed(view, container.Items));


        view.isActive = true;
    }

    private IEnumerator AddItemsDelayed(InventoryView view, List<NetItem> items) {
        yield return new WaitForEndOfFrame();


        foreach (var item in items) {
            var it = itemDatabase.GetItem(item.ItemID);
            // Debug.Log($"item name: {it.ItemName}");
            // bool added = mgr.TryAddAt(it, new Vector2Int(item.X, item.Y));
            it.Quantity = item.Quantity;
            view.AddItem(it);

            // var tmp = mgr.GetAtPoint(new Vector2Int(item.X, item.Y));
            // if(tmp != null) Debug.Log($"Added item {item.ItemID} to inventory at {item.X},{item.Y}");
            // else Debug.Log($"Failed to add item {item.ItemID} to inventory at {item.X},{item.Y}");
            //
            //
            // // (mgr.TryAddAt(iit as IInventoryItem, new Vector2Int(item.X, item.Y)))
            // if (!added) Debug.LogError($"Failed to add item {item.ItemID} to inventory");
        }

        yield break;
    }

    private InventoryView GetFreeTab() {
        return _inventoryViews.FirstOrDefault(x => x.Value.isActive == false)
                              .Value;
    }

    #region public methods

    public void EnableInventory() {
        _mainInventoryCanvasGroupToggler.Enable(false);
    }

    public void AddLootContainer(ItemBase item) {
        if (item._iscontainer == false) return;
    }

    #endregion
}