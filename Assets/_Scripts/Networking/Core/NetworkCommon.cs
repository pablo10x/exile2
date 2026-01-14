using LiteNetLib.Utils;
using ExileSurvival.Networking.Data;

namespace ExileSurvival.Networking.Core
{
    public static class NetworkCommon
    {
        public static void RegisterPackets(NetPacketProcessor processor)
        {
            // Register nested packet types
            processor.RegisterNestedType<PlayerInputPacket>();
            processor.RegisterNestedType<PlayerStatePacket>();
            processor.RegisterNestedType<JoinAcceptPacket>();
            processor.RegisterNestedType<JoinRequestPacket>();
            processor.RegisterNestedType<SpawnPacket>();
            processor.RegisterNestedType<ClientReadyPacket>();
            processor.RegisterNestedType<EntityDestroyPacket>();
            
            // Inventory Packets
            processor.RegisterNestedType<InventoryOperationPacket>();
            processor.RegisterNestedType<InventorySyncPacket>();
            processor.RegisterNestedType<RequestInventorySyncPacket>();
            processor.RegisterNestedType<InventoryItemData>();
        }
    }
}