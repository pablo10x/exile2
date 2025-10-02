using Sirenix.OdinInspector;
using UnityEngine;



    [CreateAssetMenu(fileName = "globalSettings", menuName = "Exile/GlobalSetting")]
    public class GlobalDataSO : ScriptableObject {
        public Sprite DefaultItemSprite;

        public float playerSpeed = 5.0f;
// Loot Rarity Colors

        [FoldoutGroup("Colors")]

        // Loot Rarity Colors
        [FoldoutGroup("Colors/itemsVariation")]
        [ColorPalette]
        public Color itemVariationColor_common;
        [FoldoutGroup("Colors/itemsVariation")] [ColorPalette] public Color itemVariationColor_uncommon;
        [FoldoutGroup("Colors/itemsVariation")] [ColorPalette] public Color itemVariationColor_rare;
        [FoldoutGroup("Colors/itemsVariation")] [ColorPalette] public Color itemVariationColor_epic;
        [FoldoutGroup("Colors/itemsVariation")] [ColorPalette] public Color itemVariationColor_legendary;
        [FoldoutGroup("Colors/itemsVariation")] [ColorPalette] public Color itemVariationColor_mythic;
        [FoldoutGroup("Colors/itemsVariation")] [ColorPalette] public Color itemVariationColor_exotic;
    }
