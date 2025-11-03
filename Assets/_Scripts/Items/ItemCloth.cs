using core.player;
using Salvage.ClothingCuller.Components;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exile.Inventory.Examples {
    /// <summary>
    /// Scriptable Object representing a clothing item in the inventory
    /// </summary>
    [CreateAssetMenu(fileName = "ClothItem", menuName = "Inventory/Cloth Item", order = 2)]
    public class ItemCloth : ItemBase {
        public ClothingSlots itemClothingSlot;
        public Occludee          ItemPrefab;
      

      
    }
}