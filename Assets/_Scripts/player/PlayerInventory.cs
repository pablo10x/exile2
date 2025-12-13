using core.player;
using Exile.Inventory;
using Exile.Inventory.Examples;
using Exile.Inventory.Network;
using FishNet.Object;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour {
    [SerializeField] private CharacterEquipmentManager equipmentManager;

    public ItemDatabase itemDatabase;

    public                        NetworkInventoryBehaviour BodyInventory;
    [BoxGroup("Debuging")] public ItemBase                  itemToEquip;

    private void Awake() { }

    public override void OnStartServer() {
        base.OnStartServer();

        // Server side: only handle server logic here.
        // // Do NOT touch InventoryUIManager (client-only) here.
        // BodyInventory.OnInventoryInitialized += () => {
        //     Debug.Log($"[SERVER] Body Inventory Initialized {BodyInventory.Inventory.height} | {BodyInventory.Inventory.width}");
        //     // If you need to notify the owner client when the server inventory is ready,
        //     // call a TargetRpc from here (see below).
        //     if (Owner.IsValid) {
        //         var itemsdata = BodyInventory.ConvertItemsToNetworkItems();
        //         var inv       = new NetWorkedInventoryData(BodyInventory.Inventory.NetworkInventoryId, BodyInventory.Inventory.height, BodyInventory.Inventory.width, itemsdata);
        //         
        //         
        //         
        //         //Target_InventoryInitialized(base.Owner, inv);
        //     }
        // };
    }

    
    

    public override void OnStartClient() {
        base.OnStartClient();
// Only build UI for the local player
        if (!IsOwner) return;
        if (BodyInventory == null) {
            Debug.LogWarning("[CLIENT] PlayerInventory: BodyInventory reference is null.");
            return;
        }

        // Subscribe to client-side inventory initialized event
        BodyInventory.OnInventoryInitialized += HandleBodyInventoryInitializedClient;

        // If the inventory is ALREADY initialized (e.g. we are Host, or Client joined late and init happened before this Start),
        // we manually trigger the handler because we might have missed the event or it won't fire again.
        if (BodyInventory.Inventory != null) {
            HandleBodyInventoryInitializedClient();
        }
    }

// Runs on client, for the local player, when BodyInventory.Inventory is ready.
    private void HandleBodyInventoryInitializedClient() {
        if (!IsOwner) return; // safety

        var invManager = BodyInventory.Inventory;
        if (invManager == null) {
            Debug.LogWarning("[CLIENT] BodyInventory.Inventory is null in HandleBodyInventoryInitializedClient.");
            return;
        }

        Debug.Log($"[CLIENT] Body Inventory Initialized {invManager.height} | {invManager.width}, building UI.");

        // Option 1: build using NetworkedItemData snapshot
        var itemsdata = BodyInventory.ConvertItemsToNetworkItems();
        var invData   = new NetWorkedInventoryData(invManager.NetworkInventoryId, invManager.height, invManager.width, itemsdata);
        InventoryUIManager.Instance.addBaseCharacterInventory(BodyInventory.Inventory,invData);

        // OR Option 2: if your UI can work directly with InventoryManager:
        // InventoryUIManager.Instance.AddBaseCharacterInventory(invManager);
    }

   


    [Button("Test equip ")]
    public void TestEquip() {
        var it = itemDatabase.GetItem(itemToEquip.name);
        equipmentManager.EquipBodyItem(it as ItemCloth);
        InventoryUIManager.Instance.AssignItemToPlayerTab(it);
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