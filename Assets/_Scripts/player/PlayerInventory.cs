using System;
using System.Collections.Generic;
using core.player;
using Exile.Inventory;
using Exile.Inventory.Examples;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour {
    [SerializeField] private CharacterEquipmentManager equipmentManager;

    public ItemDatabase itemDatabase;

    
    [BoxGroup("Debuging")] public ItemBase itemToEquip;


    
    
    
    
    private readonly SyncList<int> equippedItems = new SyncList<int>();

    private void Awake() {
        equippedItems.OnChange += (op, index, item, newItem, server) => {
            
        };
    }

    public override void OnStartClient() {
        Debug.Log($"initializing inventory for player | owner: {IsOwner} | is server: {IsServerInitialized}");
    }

    [Button("Test equip ")]
    public void TestEquip() {
        var it = itemDatabase.GetItem(itemToEquip.name);
        equippedItems.Add(it.Id);
        equipmentManager.EquipBodyItem(it as ItemCloth);
        InventoryUIManager.Instance.AssignItemToTab(it);
    }

    public void EquipHeadGear(ItemBase item) {

        if (IsItemEquiped(item)) {
            Debug.Log("ITEM ALREADY EQUIPED");
            return;
        }

        switch (item.Type) {
            case ItemType.Clothing_tshirt:
                equippedItems.Add(item.Id);
        //InventoryUIManager.Instance
                break;
        }
        
    }


    private bool IsItemEquiped(ItemBase item) {
        if (InventoryUIManager.Instance.Headgear.InventoryManager.Contains(item)) return true;
        if (InventoryUIManager.Instance.Vest.InventoryManager.Contains(item)) return true;
        if (InventoryUIManager.Instance.Tshirt.InventoryManager.Contains(item)) return true;
        if (InventoryUIManager.Instance.Pants.InventoryManager.Contains(item)) return true;
        if (InventoryUIManager.Instance.Backpack.InventoryManager.Contains(item)) return true;
        if (InventoryUIManager.Instance.Shoes.InventoryManager.Contains(item)) return true;
        
        return false;
    }
}