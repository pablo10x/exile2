using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "globalSettings", menuName = "Exile/GlobalSetting")]
public class GlobalDataSO : ScriptableObject {
    public Sprite DefaultItemSprite;

    [FoldoutGroup("Character")] public characterskin   defaultcharacterSkin;
    public                             characterskin[] characterSkins;

    //Character Camera framing / zooming Settings
    [FoldoutGroup("Camera Settings")]

    //TPM/TPP Thir person Mode
    [FoldoutGroup("Camera Settings/TPP")]
    public float TPP_CamSensitivity = 0.2f;
    [FoldoutGroup("Camera Settings/TPP")]                    public Vector2 TPP_FollowPointFraming        = new Vector2(0f, 0f);
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_FollowingSharpness        = 10000f;
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_DefaultDistance           = 2.5f;
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_MinDistance               = 1f;
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_MaxDistance               = 3.2f;
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_DistanceMovementSpeed     = 5f;
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_DistanceMovementSharpness = 10f;
    [FoldoutGroup("Camera Settings/TPP")] [Range(-90f, 90f)] public float   TPP_DefaultVerticalAngle      = 20f;
    [FoldoutGroup("Camera Settings/TPP")] [Range(-90f, 90f)] public float   TPP_MinVerticalAngle          = -90f;
    [FoldoutGroup("Camera Settings/TPP")] [Range(-90f, 90f)] public float   TPP_MaxVerticalAngle          = 90f;
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_RotationSpeed             = 1f;
    [FoldoutGroup("Camera Settings/TPP")]                    public float   TPP_RotationSharpness         = 10000f;

    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_CamSensitivity            = 0.2f;
    [FoldoutGroup("Camera Settings/FPP")]                    public Vector2 FPP_FollowPointFraming        = new Vector2(0f, 0f);
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_FollowingSharpness        = 10000f;
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_DefaultDistance           = 2.5f;
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_MinDistance               = 1f;
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_MaxDistance               = 3.2f;
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_DistanceMovementSpeed     = 5f;
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_DistanceMovementSharpness = 10f;
    [FoldoutGroup("Camera Settings/FPP")] [Range(-90f, 90f)] public float   FPP_DefaultVerticalAngle      = 20f;
    [FoldoutGroup("Camera Settings/FPP")] [Range(-90f, 90f)] public float   FPP_MinVerticalAngle          = -90f;
    [FoldoutGroup("Camera Settings/FPP")] [Range(-90f, 90f)] public float   FPP_MaxVerticalAngle          = 90f;
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_RotationSpeed             = 1f;
    [FoldoutGroup("Camera Settings/FPP")]                    public float   FPP_RotationSharpness         = 10000f;

    // Loot Rarity Colors

    [FoldoutGroup("ItemsColors")]
    // Loot Rarity Colors
    public Color itemVariationColor_common;
    [FoldoutGroup("ItemsColors/itemsVariation")] public Color itemVariationColor_uncommon;
    [FoldoutGroup("ItemsColors/itemsVariation")] public Color itemVariationColor_rare;
    [FoldoutGroup("ItemsColors/itemsVariation")] public Color itemVariationColor_epic;
    [FoldoutGroup("ItemsColors/itemsVariation")] public Color itemVariationColor_legendary;
    [FoldoutGroup("ItemsColors/itemsVariation")] public Color itemVariationColor_mythic;
    [FoldoutGroup("ItemsColors/itemsVariation")] public Color itemVariationColor_exotic;

    //Nodes settings
    [FoldoutGroup("Nodes")] [FoldoutGroup("Nodes/Stone")] public float stone_respawnTime = 5f;
    [FoldoutGroup("Nodes/Stone")]                         public int   stone_maxQuantity = 5;
    [FoldoutGroup("Nodes/Stone")]                         public float stone_coolDown    = 1f;
}