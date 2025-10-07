using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using core.Managers;
using Exile.Inventory;
using Exile.Inventory.Examples;
using MTAssets.SkinnedMeshCombiner;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace core.player
{
    public enum BodyPartType
    {
        Head,
        Neck,
        UpperBody,
        LowerBody,
        Feet,
        RightShoulder,
        RightArm,
        RightHand,
        LeftShoulder,
        LeftArm,
        LeftHand
    }

    public enum ClothingSlots
    {
        Tshirt,
        Pants,
        Hoodie,
        Shoes,
        Suit
    }

    [Serializable] public struct ClothingPlaceHolder
    {
        public ClothingSlots       Part;
        public SkinnedMeshRenderer skinnedMesh;
    }

    [RequireComponent(typeof(SkinnedMeshCombiner))] public class CharacterEquipmentManager : MonoBehaviour
    {
        #region CharacterBodyParts

        
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer Head;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer Neck;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer UpperBody;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer LowerBody;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer LowerBody_short;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer RightShoulder;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer RightArm;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer Right_Hand;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer LeftShoulder;

        [FormerlySerializedAs("LefttArm"), FoldoutGroup("CharacterBodyParts")] [SerializeField]
        private SkinnedMeshRenderer LeftArm;

        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer Left_Hand;
        [FoldoutGroup("CharacterBodyParts")] [SerializeField] private SkinnedMeshRenderer Feet;

        #endregion

        #region Character Rig Data

        [FoldoutGroup("Rig Data")] public SkinnedMeshRenderer baseRigRenderer;
        [FoldoutGroup("Rig Data")] public SkinnedMeshRenderer ClothingRigRederer;

        #endregion

        public                              ItemCloth[]               dummyitem;
        public                              List<ClothingPlaceHolder> _ClothingPlaceHolder;
        private                             List<ItemCloth>           equiped_clothes = new List<ItemCloth>();
        [SerializeField] [Required] private SkinnedMeshCombiner       Combiner;
        [Required] [SerializeField] private characterskin             defaultskin;

        private void Awake()
        {
            SetSkin(GameManager.Instance.GlobalConfig.defaultcharacterSkin, false);
        }

        
        [Button("Assign parts")]
        private void AutoAssignBodyParts()
        {
            SkinnedMeshRenderer[] allRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Debug.Log($"Found {allRenderers.Length} SkinnedMeshRenderers in total.");

            FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                          .Where(f => f.FieldType == typeof(SkinnedMeshRenderer)).ToArray();

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name.ToLower().Replace("_", "");
                SkinnedMeshRenderer matchingRenderer =
                    allRenderers.FirstOrDefault(r => r.name.ToLower().Replace(" ", "").Replace("_", "")
                                                      .Contains(fieldName));

                if (matchingRenderer != null)
                {
                    field.SetValue(this, matchingRenderer);
                    Debug.Log($"Assigned {matchingRenderer.name} to {field.Name}");
                }
                else
                {
                    Debug.LogWarning(
                        $"No matching renderer found for {field.Name}. Available renderers: {string.Join(", ", allRenderers.Select(r => r.name))}");
                }
            }

            // Check for unassigned fields after the process
            foreach (FieldInfo field in fields)
            {
                if (field.GetValue(this) == null)
                {
                    Debug.LogError($"{field.Name} is still unassigned after auto-assignment.");
                }
            }
        }

        /// <summary>
        /// Shows body parts based on the item's mask
        /// </summary>
        /// <param name="item">The clothing item</param>
        private void ShowPartsWithMask(ItemCloth item)
        {
            void ShowIfNeeded(SkinnedMeshRenderer renderer_, bool condition)
            {
                if (condition && !renderer_.gameObject.activeSelf)
                {
                    renderer_.gameObject.SetActive(true);
                }
            }

            ShowIfNeeded(Head, item.C_HEAD);
            ShowIfNeeded(Neck, item.C_NECK);
            ShowIfNeeded(Feet, item.BODY_FOOT);
            ShowIfNeeded(Left_Hand, item.LEFT_HAND);
            ShowIfNeeded(LowerBody, item.BODY_LOWER);
            ShowIfNeeded(LowerBody_short, item.BODY_LOWER_SHORT);
            ShowIfNeeded(UpperBody, item.BODY_UPPER);
            ShowIfNeeded(Right_Hand, item.RIGHT_HAND);
            ShowIfNeeded(LeftArm, item.LEFT_LOWER_ARM);
            ShowIfNeeded(RightArm, item.RIGHT_LOWER_ARM);
            ShowIfNeeded(LeftShoulder, item.LEFT_SHOULDER);
            ShowIfNeeded(RightShoulder, item.RIGHT_SHOULDER);
        }

        /// <summary>
        /// Hides body parts based on the item's mask
        /// </summary>
        /// <param name="item">The clothing item</param>
        private void HidePartsWithMask(ItemCloth item)
        {
            void HideIfNeeded(SkinnedMeshRenderer renderer, bool condition)
            {
                if (condition && renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(false);
                }
            }

            HideIfNeeded(Head, item.C_HEAD);
            HideIfNeeded(Neck, item.C_NECK);
            HideIfNeeded(Feet, item.BODY_FOOT);
            HideIfNeeded(Left_Hand, item.LEFT_HAND);
            HideIfNeeded(LowerBody, item.BODY_LOWER);
            HideIfNeeded(LowerBody_short, item.BODY_LOWER_SHORT);
            HideIfNeeded(UpperBody, item.BODY_UPPER);
            HideIfNeeded(Right_Hand, item.RIGHT_HAND);
            HideIfNeeded(LeftArm, item.LEFT_LOWER_ARM);
            HideIfNeeded(RightArm, item.RIGHT_LOWER_ARM);
            HideIfNeeded(LeftShoulder, item.LEFT_SHOULDER);
            HideIfNeeded(RightShoulder, item.RIGHT_SHOULDER);
        }

        /// <summary>
        /// Equips all dummy items for testing purposes
        /// </summary>
        [Button("Equip")]
        private void EquipDummyItems()
        {
            if (Combiner.isMeshesCombined()) Combiner.UndoCombineMeshes(false, false);

            foreach (var item in dummyitem)
            {
                EquipItem(item);
            }

            if (dummyitem.Length > 0)
            {
                Stopwatch w = Stopwatch.StartNew();
                Combiner.CombineMeshes();
                Debug.Log($"Combining meshes took {w.ElapsedMilliseconds} ms");
            }
        }

        /// <summary>
        /// Removes all equipped clothes
        /// </summary>
        [Button("Remove all clothes")]
        private void RemoveAllClothes()
        {
            if (equiped_clothes.Count == 0)
            {
#if UNITY_EDITOR
                Debug.Log("No clothes equipped");
#endif
                return;
            }

            Combiner.UndoCombineMeshes(false, false);


            for (int i = 0; i < equiped_clothes.Count; i++)
            {
                DeEquipItem(equiped_clothes[i]);
            }


            equiped_clothes.Clear();
            Combiner.CombineMeshes();
        }

        /// <summary>
        /// Removes a specific clothing item
        /// </summary>
        /// <param name="item">The item to remove</param>
        private void DeEquipItem(ItemCloth item)
        {
            if (!equiped_clothes.Contains(item)) return;

            if (Combiner.isMeshesCombined())
            {
#if UNITY_EDITOR
                Debug.LogError("Couldn't dequip item, the character is merged");
#endif
                return;
            }

            ShowPartsWithMask(item);
            Debug.Log($"removing {item.name}");
            var cl = _ClothingPlaceHolder.First(x => x.Part == item.itemClothingSlot);
            cl.skinnedMesh.sharedMesh = null;
            cl.skinnedMesh.gameObject.SetActive(false);
            cl.skinnedMesh.sharedMaterial = null;
            equiped_clothes.Remove(item);
        }

        /// <summary>
        /// Equips a clothing item
        /// </summary>
        /// <param name="item">The item to equip</param>
        public void EquipItem(ItemCloth item)
        {
            if (equiped_clothes.Contains(item)) return;

            if (item.Type != ItemType.Clothing)
            {
#if UNITY_EDITOR
                Debug.LogError($"Trying to equip item with type {item.Type}");
#endif
                return;
            }


            HidePartsWithMask(item);
            var cl = _ClothingPlaceHolder.First(x => x.Part == item.itemClothingSlot);
            cl.skinnedMesh.sharedMesh = item.itemMesh;
            cl.skinnedMesh.gameObject.SetActive(true);
            cl.skinnedMesh.sharedMaterial = item.itemMaterial;
            equiped_clothes.Add(item);
        }

        /// <summary>
        /// Sets the character's skin
        /// </summary>
        /// <param name="skin">The skin to apply</param>
        /// 
        [Button("Setskin")]
        public void SetSkin(characterskin skin, bool Combine)
        {
            bool combined = Combiner.isMeshesCombined();
            if (combined) Combiner.UndoCombineMeshes(false, false);

            ApplySkinMeshes(skin);
            SetCharacterMaterial(skin.skinMaterial);

            if (Combine) Combiner.CombineMeshes();
        }

        /// <summary>
        /// Applies skin meshes to the character's body parts
        /// </summary>
        /// <param name="skin">The skin containing the meshes</param>
        private void ApplySkinMeshes(characterskin skin)
        {
            Head.sharedMesh            = skin.Head;
            Neck.sharedMesh            = skin.Neck;
            UpperBody.sharedMesh       = skin.UpperBody;
            RightShoulder.sharedMesh   = skin.RightShoulder;
            RightArm.sharedMesh        = skin.RightArm;
            Right_Hand.sharedMesh      = skin.RightHand;
            LeftShoulder.sharedMesh    = skin.LeftShoulder;
            LeftArm.sharedMesh         = skin.LeftArm;
            Left_Hand.sharedMesh       = skin.LeftHand;
            LowerBody.sharedMesh       = skin.LowerBody;
            LowerBody_short.sharedMesh = skin.LowerBodyShort;
            Feet.sharedMesh            = skin.Feet;
        }

        /// <summary>
        /// Builds the character with the given skin and optional items
        /// </summary>
        /// <param name="sk">The skin to apply</param>
        /// <param name="items">Optional list of items to equip</param>
        [Button("Build")]
        public void BuildCharacter(characterskin sk, List<ItemCloth> items = null)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    EquipItem(item);
                }
            }

            SetSkin(sk,true);
        }

        /// <summary>
        /// Sets the material for all character body parts
        /// </summary>
        /// <param name="material">The material to apply</param>
        private void SetCharacterMaterial(Material material)
        {
            if (material == null) return;

            SkinnedMeshRenderer[] renderers =
            {
                Head, Neck, UpperBody, RightShoulder, RightArm, Right_Hand, LeftShoulder, LeftArm, Left_Hand,
                LowerBody, LowerBody_short, Feet
            };

            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}