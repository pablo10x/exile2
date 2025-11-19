using System.Collections.Generic;
using _Scripts.Items;
using Exile.Inventory;
using Exile.Inventory.Examples;
using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public class ContainerBox : NetworkBehaviour, IInteractable {
    public                   NetItemContainer    container;
    public                   string              ContainerName;
    [SerializeField] private List<ItemSpawnInfo> _itemsToAdd;
    [SerializeField] private InventoryShape      inventoryShape;

    private InventoryManager _inventoryManager;

    public override void OnSpawnServer(NetworkConnection connection) {
        base.OnSpawnServer(connection);


        int tempw = inventoryShape.width, temph = inventoryShape.height;
        container = new NetItemContainer { Name = ContainerName, Items = new List<NetItem>(), Width = tempw, Height = temph };

       


        _inventoryManager = new InventoryManager(new InventoryProvider(InventoryRenderMode.Grid, -1, ItemType.Any), inventoryShape.width, inventoryShape.height, true);



        #region Populate Inventory

        //populate inventory with items
        foreach (var itemInfo in _itemsToAdd) {
            if (Random.Range(0, 101) <= itemInfo.SpawnPercentage) {
                var itemInstance = itemInfo.Item.CreateInstance();

                if (itemInfo.UseRandomRange) {
                    itemInstance.Quantity = Random.Range(itemInfo.MinAmount, itemInfo.MaxAmount + 1);
                }
                else {
                    itemInstance.Quantity = itemInfo.Quantity;
                }

                if (_inventoryManager.TryAddWithRotation(itemInstance)) {
                    Debug.Log($"Item {itemInstance.ItemName} has been added with quantity {itemInstance.Quantity}");
                }
            }
        }

        #endregion
    }

    // Convert server-side InventoryManager to a list of NetItem for sending to a client
    private List<NetItem> ConvertToNetItems() {
        var list = new List<NetItem>();

        // IMPORTANT: adapt this loop to how your InventoryManager exposes items.
        // I assume InventoryManager has a collection like `PlacedItems` or `Items` that
        // returns entries with X,Y,Width,Height, Rotated ,Stack, and ItemBase reference.
        foreach (var item in _inventoryManager.allItems) // replace .Items with the real property
        {
            if (item == null)
                continue;

            NetItem net = new NetItem {
                ItemID   = item.ID,
                Quantity = item.Quantity,
                X        = item.position.x,
                Y        = item.position.y,
                Rotated  = item.Rotated
            };
            list.Add(net);
        }

        return list;
    }

    #region RPCs: client requests -> server replies (TargetRpc)

    // Client calls this to ask for the current contents of the container.
    // RequireOwnership = false so non-owned players can call it.
    [ServerRpc(RequireOwnership = false, Logging = LoggingType.Common)]
    public void RequestContainerItems_ServerRpc(NetworkConnection conn = null) {
        // Server converts its inventory into NetItem list
 
        container.Items = ConvertToNetItems();
        var tmpcont = new NetItemContainer(
            ContainerName,
            ConvertToNetItems(),
           inventoryShape.width,
           inventoryShape.height
                
        );
        
        

        Debug.Log($"SERVER:  container widh: {container.Width} and height: {container.Height}");
        // Send only to the requesting client
        SendContainerItems_TargetRpc(conn ?? Owner, tmpcont);
    }

    // Server sends the serialized list to the single client that requested it.
    [TargetRpc]
    private void SendContainerItems_TargetRpc(NetworkConnection conn, NetItemContainer netItemContainer) {
        foreach (var it in netItemContainer.Items) {
            Debug.Log($"item: {it.ItemID} quantity: {it.Quantity}");
        }

        Debug.Log(netItemContainer.Name);

        InventoryUIManager.Instance.AddContainerToLoot(netItemContainer);
        // InventoryUIManager.Instance.AssignItemToLootGround()
    }

    #endregion

    public string GetInteractionPrompt() {
        throw new System.NotImplementedException();
    }

    public void Interact(PlayerInteraction player) {
        InventoryUIManager.Instance.EnableInventory();

        RequestContainerItems_ServerRpc(Owner);
    }

    public Vector3 GetPosition() {
        return transform.position;
    }

    public void Register() { }

    public void Unregister() { }
}