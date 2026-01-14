// SyncedItemData.cs

using System;
using LiteNetLib.Utils;

[Serializable]
public struct SyncedItemData : IEquatable<SyncedItemData>, INetSerializable {
    public string definitionId; // name or GUID of ItemDefinition ScriptableObject
    public int    quantity;
    public bool   rotated;

    public SyncedItemData(string id, int qty = 1, bool rot = false) {
        definitionId = id;
        quantity     = qty;
        rotated      = rot;
    }

    public bool Equals(SyncedItemData other) => definitionId == other.definitionId && quantity == other.quantity && rotated == other.rotated;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(definitionId);
        writer.Put(quantity);
        writer.Put(rotated);
    }

    public void Deserialize(NetDataReader reader)
    {
        definitionId = reader.GetString();
        quantity = reader.GetInt();
        rotated = reader.GetBool();
    }
}