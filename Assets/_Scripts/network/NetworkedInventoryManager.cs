using System;
using Exile.Inventory;
using Exile.Inventory.Network;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace core.network {
    public class NetworkedInventoryManager : NetworkBehaviour {


        public readonly SyncList<NetworkedItemData> SyncedItems = new SyncList<NetworkedItemData>();
        
        public override void OnStartServer() {
            base.OnStartServer();
            
        }
    }
}