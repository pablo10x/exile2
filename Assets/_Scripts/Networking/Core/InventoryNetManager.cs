using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using ExileSurvival.Networking.Data;
using Exile.Inventory;

namespace ExileSurvival.Networking.Core
{
    public class InventoryNetManager : Singleton<InventoryNetManager>
    {
        public ItemDatabase ItemDatabase; // Assign in inspector
        
        private readonly Dictionary<int, InventoryManager> _inventories = new Dictionary<int, InventoryManager>();
        private readonly Dictionary<int, HashSet<int>> _inventoryViewers = new Dictionary<int, HashSet<int>>(); // InventoryId -> ClientIds

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            
            if (ServerManager.Instance != null)
            {
                ServerManager.Instance.PacketProcessor.SubscribeNetSerializable<RequestInventorySyncPacket, NetPeer>(OnRequestSync);
                ServerManager.Instance.PacketProcessor.SubscribeNetSerializable<InventoryOperationPacket, NetPeer>(OnInventoryOperation);
            }
            
            if (ClientManager.Instance != null)
            {
                ClientManager.Instance.PacketProcessor.SubscribeNetSerializable<InventorySyncPacket, NetPeer>(OnInventorySyncReceived);
            }
        }

        public void RegisterInventory(InventoryManager inventory)
        {
            if (!_inventories.ContainsKey(inventory.NetworkInventoryId))
            {
                _inventories.Add(inventory.NetworkInventoryId, inventory);
            }
        }

        public void UnregisterInventory(int id)
        {
            if (_inventories.ContainsKey(id))
            {
                _inventories.Remove(id);
                _inventoryViewers.Remove(id);
            }
        }

        // --- Server Side Handlers ---

        private void OnRequestSync(RequestInventorySyncPacket packet, NetPeer peer)
        {
            if (_inventories.TryGetValue(packet.InventoryId, out var inventory))
            {
                if (!_inventoryViewers.ContainsKey(packet.InventoryId))
                    _inventoryViewers[packet.InventoryId] = new HashSet<int>();
                
                _inventoryViewers[packet.InventoryId].Add(peer.Id);
                SendSyncToClient(inventory, peer);
            }
        }

        private void OnInventoryOperation(InventoryOperationPacket packet, NetPeer peer)
        {
            if (_inventories.TryGetValue(packet.SourceInventoryId, out var sourceInv))
            {
                bool success = false;
                var item = sourceInv.GetItemByRuntimeID(packet.ItemRuntimeId);
                
                if (item != null)
                {
                    if (packet.Operation == InventoryOperation.Move)
                    {
                        if (sourceInv.TryRemove(item))
                        {
                            if (_inventories.TryGetValue(packet.TargetInventoryId, out var targetInv))
                            {
                                success = targetInv.TryAddAt(item, new Vector2Int(packet.TargetX, packet.TargetY));
                            }
                        }
                    }
                    // TODO: Implement other operations (Rotate, Split, etc.)
                }

                if (success)
                {
                    BroadcastSync(sourceInv);
                    if (packet.SourceInventoryId != packet.TargetInventoryId)
                        BroadcastSync(_inventories[packet.TargetInventoryId]);
                }
                else
                {
                    SendSyncToClient(sourceInv, peer); // Revert client state
                }
            }
        }

        private void BroadcastSync(InventoryManager inventory)
        {
            if (_inventoryViewers.TryGetValue(inventory.NetworkInventoryId, out var viewers))
            {
                var packet = CreateSyncPacket(inventory);
                foreach (var clientId in viewers)
                {
                    ServerManager.Instance.SendToClient(clientId, packet, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void SendSyncToClient(InventoryManager inventory, NetPeer peer)
        {
            var packet = CreateSyncPacket(inventory);
            ServerManager.Instance.SendToClient(peer.Id, packet, DeliveryMethod.ReliableOrdered);
        }

        private InventorySyncPacket CreateSyncPacket(InventoryManager inventory)
        {
            var items = new List<InventoryItemData>();
            foreach (var item in inventory.allItems)
            {
                if (item != null)
                {
                    items.Add(new InventoryItemData
                    {
                        RuntimeId = item.RuntimeID,
                        ItemId = item.ID,
                        X = item.position.x,
                        Y = item.position.y,
                        Quantity = item.Quantity,
                        Rotated = item.Rotated,
                        Durability = item.Durability
                    });
                }
            }

            return new InventorySyncPacket
            {
                InventoryId = inventory.NetworkInventoryId,
                Items = items.ToArray()
            };
        }

        // --- Client Side Handlers ---

        private void OnInventorySyncReceived(InventorySyncPacket packet, NetPeer peer)
        {
            if (_inventories.TryGetValue(packet.InventoryId, out var inventory))
            {
                inventory.Clear();
                
                foreach (var itemData in packet.Items)
                {
                    var itemBase = ItemDatabase.GetItem(itemData.ItemId);
                    if (itemBase != null)
                    {
                        var newItem = itemBase.CreateInstance(itemData.RuntimeId);
                        newItem.position = new Vector2Int(itemData.X, itemData.Y);
                        newItem.Quantity = itemData.Quantity;
                        newItem.Rotated = itemData.Rotated;
                        newItem.Durability = itemData.Durability;
                        inventory.TryAddAt(newItem, newItem.position);
                    }
                }
            }
        }
        
        // --- Client Request Methods ---
        
        public void RequestMoveItem(int sourceInvId, int itemRuntimeId, int targetInvId, int targetX, int targetY)
        {
            var packet = new InventoryOperationPacket
            {
                SourceInventoryId = sourceInvId,
                TargetInventoryId = targetInvId,
                ItemRuntimeId = itemRuntimeId,
                Operation = InventoryOperation.Move,
                TargetX = targetX,
                TargetY = targetY,
            };
            ClientManager.Instance.SendPacket(packet, DeliveryMethod.ReliableOrdered);
        }
    }
}