namespace Exile.Inventory.Network {
    public class ItemManagerRuntime : NetSingleton<ItemManagerRuntime> {
        private int _nextItemId = 1;
        private int _nextContainerId = 1;
        public int GetNextItemId() {
            return _nextItemId++;
        }

        

        public int GetNextContainerId() {
            return _nextContainerId++;
        }
    }
}