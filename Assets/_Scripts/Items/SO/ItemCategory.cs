using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Category", order = 1)]
public class ItemCategory : ScriptableObject
{
    [Tooltip("Display name of this category (e.g., Weapons, Food, Clothing).")] public string categoryName;


    [Tooltip("List of specific item types within this category (e.g., Sword, Axe for Melee Weapons).")]
    public List<string> itemTypes = new List<string>();
}
