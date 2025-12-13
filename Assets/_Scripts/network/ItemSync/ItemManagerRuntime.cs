using System.Collections.Generic;

namespace Exile.Inventory.Network {
    public class ItemManagerRuntime : NetSingleton<ItemManagerRuntime> {
        private int _nextItemId = 1;
        private int _nextContainerId = 1;
        
        
        private Dictionary<int,NetworkInventoryBehaviour> _networkInventoryBehaviours = new Dictionary<int,NetworkInventoryBehaviour>();




        public  int RegisterInventory(NetworkInventoryBehaviour inventoryBehaviour) {
            var next = _nextContainerId++;
            _networkInventoryBehaviours.Add(next,inventoryBehaviour);
            return next;
        }


        public NetworkInventoryBehaviour GetInventoryByID(int InventoryID) {
            return _networkInventoryBehaviours.TryGetValue(InventoryID, out var id)
                       ? id
                       : null;
        }
        
        public int GetNextItemId() {
            return _nextItemId++;
        }

        

       
        
        
        
        
    }
}