using System.Collections.Generic;
using System.Linq;
using core.Managers;
using core.ui;
using Exile.Inventory;
using Exile.Inventory.Network;
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


    [BoxGroup("Player related")] public InventoryView BodyView;
    
    
    
    private Dictionary<GameObject, InventoryView> _inventoryViews = new Dictionary<GameObject, InventoryView>();

    [FoldoutGroup("InventoryRenderingConfigs")] [SerializeField] private TranslucentImage _translucentImage;

    
    private void Awake() {
        Initialize();
        _mainInventoryCanvasGroupToggler.OnClosed += () => {
            
            //clean up loot containers
            var childs = LootPoolContainer.GetComponentsInChildren<InventoryView>();
            foreach (var child in childs) {
                Destroy(child.gameObject);
            }
        };
    }

    private void Start() {
      //  Pants.InventoryManager.onItemAdded += item => { Debug.Log($"Detected item added on pants {item.ItemName}"); };

        GameManager.Instance.OnPlayerSpawned += () => { _translucentImage.source = GameManager.Instance.character.orbitCamera.GetComponent<TranslucentImageSource>(); };
    }

    void Initialize() {
        for (int i = 0; i < _MaxPoolSize; i++) {
            var ob = Instantiate(_inventoryViewPrefab, PoolContainer);
            ob.gameObject.SetActive(false);
            _inventoryViews.Add(ob.gameObject, ob);
        }
    }

    public void addBaseCharacterInventory(InventoryManager inventoryManager, NetWorkedInventoryData inv) {
        InventoryView view = BodyView;

     
        view._renderMode      = InventoryRenderMode.Grid;
        view.InventoryManager = inventoryManager;//new InventoryManager(new InventoryProvider(), inv.InventoryWidth, inv.InventoryHeight, true, "MainBody Inv");
        view.CreateInventoryView(view.InventoryManager,null,InventoryRenderMode.Grid,ItemType.Any);
        view.gameObject.SetActive(true);
        view.inventoryViewTMp_Name.text = "Body ";
        view.isActive = true;
       
    }
    
    public void AssignItemToPlayerTab(ItemBase item) {
        if (!item.isContainer) return;
        InventoryView view = GetFreeTab();
        if (view == null) return;

        view._renderMode      = InventoryRenderMode.Grid;
        view.InventoryManager = item.ContainerInventory;


        view.CreateInventoryView(ref item);


        view.gameObject.SetActive(true);
        Debug.Log($"Created view for {item.name}");

        view.isActive = true;
    }

    public void AddContainerToLoot(  InventoryManager _inventoryManager) {

        InventoryView view = GetFreeTab();
        if (view == null) return;

        view.transform.SetParent(LootPoolContainer);
        view.gameObject.SetActive(true);
        view._renderMode      = InventoryRenderMode.Grid;
      //  view.InventoryManager = new InventoryManager(new InventoryProvider(InventoryRenderMode.Grid, -1, ItemType.Any), container.Width, container.Height, true); //(new InventoryProvider(InventoryRenderMode.Grid, -1, ItemType.Any))
        view.InventoryManager = _inventoryManager;

        view.CreateInventoryView(ref _inventoryManager);
        
        // The InventoryManager passed in is already populated by the client-side SyncList logic.
        // There is no need to add items manually here. The InventoryView just needs to render it.
        view.isActive = true;
    }

    private InventoryView GetFreeTab() {
        return _inventoryViews.FirstOrDefault(x => x.Value.isActive == false)
                              .Value;
    }
    
    

    #region public methods

    public void EnableInventory() {
        _mainInventoryCanvasGroupToggler.Enable(false);
    }


    public void ClearLootContainers() {
        
    }
    public void AddLootContainer(ItemBase item) {
        if (item._iscontainer == false) return;
    }

    #endregion
}