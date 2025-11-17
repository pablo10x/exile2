using core.player;
using Exile.Inventory;
using Exile.Inventory.Examples;
using FishNet.Object;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour {
    [SerializeField] private CharacterEquipmentManager equipmentManager;

    public ItemDatabase itemDatabase;

    [BoxGroup("Debuging")] public ItemBase itemToEquip;

    private void Awake() { }

    public override void OnStartClient() {
        Debug.Log($"initializing inventory for player | owner: {IsOwner} | is server: {IsServerInitialized}");
    }

    [Button("Test equip ")]
    public void TestEquip() {
        var it = itemDatabase.GetItem(itemToEquip.name);
        equipmentManager.EquipBodyItem(it as ItemCloth);
        InventoryUIManager.Instance.AssignItemToTab(it);
    }

    public void EquipHeadGear(ItemBase item) {
        if (IsItemEquiped(item)) {
            Debug.Log("ITEM ALREADY EQUIPED");
            return;
        }

        switch (item.Type) {
            case ItemType.ClothingPants:
                bool result = InventoryUIManager.Instance.Pants.InventoryManager.TryAdd(item);
                
                
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