using System.Collections.Generic;
using DG.Tweening;
using Exile.Inventory;
using FishNet.Connection;
using FishNet.Object;
using Sirenix.OdinInspector;
using UnityEngine;

public class ContainerBox : NetworkBehaviour, IInteractable {
    public                   string              ContainerName;

    [SerializeField] NetworkInventoryBehaviour _networkInventoryBehaviour;

    

    [BoxGroup("Items")] [SerializeField] private List<ItemBase>  _items = new List<ItemBase>();
    public override void OnStartServer() {
        base.OnStartServer();


   
        
    }
    
    

 
    public override void OnStartClient() {
        base.OnStartClient();
        int randomNumber = Random.Range(0, 10);
        for (int i = 0; i < randomNumber; i++)
        {
            AddRandomItem();
            base.Despawn(DespawnType.Destroy);


        }
    }

    [Button("Add Random Item")]
    public void AddRandomItem() {
        
        if(!IsServerStarted) return;
        var randomItemFromList = Random.Range(0, _items.Count);
        _networkInventoryBehaviour.RequestAddServerRpc(_items[randomItemFromList].ID);
    }
  

    public string GetInteractionPrompt() {
        return $"Open {ContainerName}";
    }

    public void Interact(PlayerInteraction player) {
        // 1. Client Logic: Open UI locally if you want immediate feedback (optional)

        InventoryUIManager.Instance.EnableInventory();
        // 2. Network Logic: Tell the server we want to interact
        // We need to send an RPC to the server. 
        // Since 'Interact' is called on the client, we must call a ServerRpc.
        CmdInteract(player.Owner);

    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdInteract(NetworkConnection caller)
    {
        // 3. Server Logic: Received request from client.
        // Now we call the method in the NetworkInventoryBehaviour that sends the TargetRpc back.

        _networkInventoryBehaviour.SendFullInventory(caller, true);


    }


    public Vector3 GetPosition() {
        return transform.position;
    }

    public void Register() { }

    public void Unregister() { }
}