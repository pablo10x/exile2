using LiteNetLib.Utils;
using UnityEngine;

namespace ExileSurvival.Networking.Data
{
    public enum InventoryOperation : byte
    {
        Move = 0,
        Rotate = 1,
        Split = 2,
        Merge = 3,
        Drop = 4,
        PickUp = 5 // From world to inventory
    }

    public struct InventoryOperationPacket : INetSerializable
    {
        public int SourceInventoryId;
        public int TargetInventoryId; // Can be same as Source
        public int ItemRuntimeId;
        public int TargetItemRuntimeId; // For merge/swap
        public InventoryOperation Operation;
        public int TargetX;
        public int TargetY;
        public int Quantity;
        public bool IsRotated;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(SourceInventoryId);
            writer.Put(TargetInventoryId);
            writer.Put(ItemRuntimeId);
            writer.Put(TargetItemRuntimeId);
            writer.Put((byte)Operation);
            writer.Put(TargetX);
            writer.Put(TargetY);
            writer.Put(Quantity);
            writer.Put(IsRotated);
        }

        public void Deserialize(NetDataReader reader)
        {
            SourceInventoryId = reader.GetInt();
            TargetInventoryId = reader.GetInt();
            ItemRuntimeId = reader.GetInt();
            TargetItemRuntimeId = reader.GetInt();
            Operation = (InventoryOperation)reader.GetByte();
            TargetX = reader.GetInt();
            TargetY = reader.GetInt();
            Quantity = reader.GetInt();
            IsRotated = reader.GetBool();
        }
    }

    public struct InventoryItemData : INetSerializable
    {
        public int RuntimeId;
        public int ItemId; // Static ID from database
        public int X;
        public int Y;
        public int Quantity;
        public bool Rotated;
        public float Durability;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RuntimeId);
            writer.Put(ItemId);
            writer.Put(X);
            writer.Put(Y);
            writer.Put(Quantity);
            writer.Put(Rotated);
            writer.Put(Durability);
        }

        public void Deserialize(NetDataReader reader)
        {
            RuntimeId = reader.GetInt();
            ItemId = reader.GetInt();
            X = reader.GetInt();
            Y = reader.GetInt();
            Quantity = reader.GetInt();
            Rotated = reader.GetBool();
            Durability = reader.GetFloat();
        }
    }

    public struct InventorySyncPacket : INetSerializable
    {
        public int InventoryId;
        public InventoryItemData[] Items;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(InventoryId);
            writer.PutArray(Items);
        }

        public void Deserialize(NetDataReader reader)
        {
            InventoryId = reader.GetInt();
            Items = reader.GetArray<InventoryItemData>();
        }
    }
    
    public struct RequestInventorySyncPacket : INetSerializable
    {
        public int InventoryId;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(InventoryId);
        }
        
        public void Deserialize(NetDataReader reader)
        {
            InventoryId = reader.GetInt();
        }
    }
}