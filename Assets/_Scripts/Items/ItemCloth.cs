using core.player;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exile.Inventory.Examples {
    /// <summary>
    /// Scriptable Object representing a clothing item in the inventory
    /// </summary>
    [CreateAssetMenu(fileName = "ClothItem", menuName = "Inventory/Cloth Item", order = 2)]
    public class ItemCloth : ItemBase {
        public ClothingSlots itemClothingSlot;
        public Mesh          itemMesh;
        public Material      itemMaterial;

        #region Masking

        [FoldoutGroup("Cloth Masking")] public bool C_HEAD;
        [FoldoutGroup("Cloth Masking")] public bool C_NECK;
        [FoldoutGroup("Cloth Masking")] public bool RIGHT_SHOULDER;
        [FoldoutGroup("Cloth Masking")] public bool RIGHT_SHOULDER_TSHIRT;
        [FoldoutGroup("Cloth Masking")] public bool RIGHT_LOWER_ARM;
        [FoldoutGroup("Cloth Masking")] public bool RIGHT_HAND;
        [FoldoutGroup("Cloth Masking")] public bool LEFT_SHOULDER;
        [FoldoutGroup("Cloth Masking")] public bool LEFT_SHOULDER_TSHIRT;
        [FoldoutGroup("Cloth Masking")] public bool LEFT_LOWER_ARM;
        [FoldoutGroup("Cloth Masking")] public bool LEFT_HAND;
        [FoldoutGroup("Cloth Masking")] public bool BODY_UPPER;
        [FoldoutGroup("Cloth Masking")] public bool BODY_LOWER;
        [FoldoutGroup("Cloth Masking")] public bool BODY_LOWER_SHORT;
        [FoldoutGroup("Cloth Masking")] public bool BODY_FOOT;

        #endregion
    }
}