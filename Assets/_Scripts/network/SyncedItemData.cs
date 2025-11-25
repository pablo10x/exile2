// SyncedItemData.cs

using System;
using UnityEngine;

[Serializable]
public struct SyncedItemData : IEquatable<SyncedItemData> {
    public string definitionId; // name or GUID of ItemDefinition ScriptableObject
    public int    quantity;
    public bool   rotated;

    public SyncedItemData(string id, int qty = 1, bool rot = false) {
        definitionId = id;
        quantity     = qty;
        rotated      = rot;
    }

    public bool Equals(SyncedItemData other) => definitionId == other.definitionId && quantity == other.quantity && rotated == other.rotated;
}