using System;
using System.Collections.Generic;
using System.Linq;
using core.Managers;
using Exile.Inventory;
using LeTai.Asset.TranslucentImage;
using Sirenix.OdinInspector;
using UnityEngine;

public class InventoryUIManager : Singleton<InventoryUIManager> {
    [SerializeField] private uint _MaxPoolSize = 15;

    [SerializeField] private Transform     PoolContainer;
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

        GameManager.Instance.OnPlayerSpawned += () => {
            Debug.Log($"Player spawned ; setting up inventory blur effect for player camera");
            _translucentImage.source = GameManager.Instance.character.orbitCamera.GetComponent<TranslucentImageSource>();
        };

    }

    void Initialize() {
        for (int i = 0; i < _MaxPoolSize; i++) {
            var ob = Instantiate(_inventoryViewPrefab, PoolContainer);
            ob.gameObject.SetActive(false);
            _inventoryViews.Add(ob.gameObject, ob);
        }
    }

    public void AssignItemToTab(ItemBase item) {
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

    public InventoryView GetFreeTab() {
        return _inventoryViews.FirstOrDefault(x => x.Value.isActive == false)
                              .Value;
    }
}