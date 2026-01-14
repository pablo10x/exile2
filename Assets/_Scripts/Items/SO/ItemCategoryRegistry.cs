using UnityEngine;

public class ItemCategoryRegistry : MonoBehaviour
{
    public ItemCategory Weapons;
    public ItemCategory MeleeWeapons;
    public ItemCategory RangedWeapons;

    public ItemCategory ClothingShirt;
    public ItemCategory ClothingPants;
    public ItemCategory ClothingShoes;

    public ItemCategory GearHelmet;
    public ItemCategory GearChest;
    public ItemCategory GearBackpack;

    public ItemCategory Food;

    private void Awake()
    {
        // Example of how you might define item types within categories
        MeleeWeapons.itemTypes.Add("Sword");
        MeleeWeapons.itemTypes.Add("Axe");
        MeleeWeapons.itemTypes.Add("Mace");

        RangedWeapons.itemTypes.Add("Bow");
        RangedWeapons.itemTypes.Add("Crossbow");

        Food.itemTypes.Add("Fruit");
        Food.itemTypes.Add("Vegetable");
        Food.itemTypes.Add("CookedMeal");
    }
}
