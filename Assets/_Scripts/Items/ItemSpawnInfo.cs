using Exile.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Scripts.Items {
    [System.Serializable]
    public class ItemSpawnInfo {
       [FoldoutGroup("itemData")] public ItemBase Item;                   // The ScriptableObject
        [FoldoutGroup("itemData")] public int      Quantity;     // Fixed amount to spawn (ignored if useRange = true)
        [FoldoutGroup("itemData")] public bool     UseRandomRange = false; // Use random amount between min/max
        [FoldoutGroup("itemData")] [ShowIf("UseRandomRange")] public int      MinAmount      = 1;
        [FoldoutGroup("itemData")] [ShowIf("UseRandomRange")]  public int      MaxAmount      = 3;

        [FoldoutGroup("itemData")]  [Range(0, 100)] public int SpawnPercentage = 100; // 100 = always spawn, 50 = 50% chance

      
    }
}