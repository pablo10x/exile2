using System.Collections.Generic;
using System.Linq;
using _Scripts.Items;
using Exile.Inventory;
using Exile.Inventory.Examples;
using Exile.Inventory.Network;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class ContainerBox : NetworkBehaviour, IInteractable {
    public                   string              ContainerName;

    [SerializeField] NetworkInventoryBehaviour _networkInventoryBehaviour;

    
    
 



    

 

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
    private void CmdInteract(NetworkConnection caller) {
        // 3. Server Logic: Received request from client.
        // Now we call the method on the NetworkInventoryBehaviour that sends the TargetRpc back.
       
       
        _networkInventoryBehaviour.SendFullInventory(caller,true);
    }


    public Vector3 GetPosition() {
        return transform.position;
    }

    public void Register() { }

    public void Unregister() { }
}