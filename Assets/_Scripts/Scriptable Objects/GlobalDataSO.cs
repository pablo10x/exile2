using Sirenix.OdinInspector;
using UnityEngine;



    [CreateAssetMenu(fileName = "globalSettings", menuName = "Exile/GlobalSetting")]
    public class GlobalDataSO : ScriptableObject {
        public Sprite DefaultItemSprite;

        
        [FoldoutGroup("Character")]
      
        public characterskin  defaultcharacterSkin;
        public characterskin[] characterSkins;
        
        
        public float playerSpeed = 5.0f;
// Loot Rarity Colors

        [FoldoutGroup("Colors")]

        // Loot Rarity Colors
        
       
        public Color itemVariationColor_common;
        [FoldoutGroup("Colors/itemsVariation")]  public Color itemVariationColor_uncommon;
        [FoldoutGroup("Colors/itemsVariation")] public Color itemVariationColor_rare;
        [FoldoutGroup("Colors/itemsVariation")] public Color itemVariationColor_epic;
        [FoldoutGroup("Colors/itemsVariation")]  public Color itemVariationColor_legendary;
        [FoldoutGroup("Colors/itemsVariation")]  public Color itemVariationColor_mythic;
        [FoldoutGroup("Colors/itemsVariation")]  public Color itemVariationColor_exotic;
    }
