using UnityEngine;

// ── Primary category (drives which tab shows this item) ────────────────────
public enum RomanItemType
{
    Weapon,
    Armor,
    Consumable,     // "Food & Drink" tab
    Material        // not shown in shop tabs unless All is active
}

// ── Sub-types (optional, dev sets these on each ScriptableObject) ──────────
public enum WeaponSubType
{
    None,
    Sword,
    Shield,
    Spear,
    Bow,
    Axe,
    Dagger
}

public enum ArmorSubType
{
    None,
    Helmet,
    Chestplate,
    Legguards,
    Boots,
    Gauntlets
}

public enum FoodDrinkSubType
{
    None,
    Food,
    Drink,
    Potion
}

// ── Class restriction ──────────────────────────────────────────────────────
public enum RomanClassRequirement
{
    Any,
    Legionary,
    Gladiator,
    Archer
}

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Roman Vanguard/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Basic Item Info")]
    public string itemName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;
    public RomanItemType itemType = RomanItemType.Material;
    public RomanClassRequirement classRequirement = RomanClassRequirement.Any;

    // ── Sub-type (set the one that matches your itemType; others are ignored) ──
    [Header("Sub-Type")]
    [Tooltip("Set this when itemType = Weapon")]
    public WeaponSubType weaponSubType = WeaponSubType.None;

    [Tooltip("Set this when itemType = Armor")]
    public ArmorSubType armorSubType = ArmorSubType.None;

    [Tooltip("Set this when itemType = Consumable")]
    public FoodDrinkSubType foodDrinkSubType = FoodDrinkSubType.None;

    // ── World prefab (assigned by dev; spawned when item is dropped) ───────
    [Header("World Prefab")]
    [Tooltip("The GameObject prefab that represents this item in the world (pickup / drop).")]
    public GameObject worldPrefab;

    // ── Shop ───────────────────────────────────────────────────────────────
    [Header("Shop")]
    [Min(0)] public int priceDenarii = 1;
    public bool canBeSoldInShop = true;

    // ── Inventory ─────────────────────────────────────────────────────────
    [Header("Inventory")]
    public bool stackable = true;
    [Min(1)] public int maxStack = 20;

    // ── Equipment Stats ───────────────────────────────────────────────────
    [Header("Equipment Stats")]
    public int damageBonus;
    public int defenseBonus;
    public int maxHealthBonus;
    public float moveSpeedBonus;
    public float attackRateBonus;
    public float attackRangeBonus;
    public float criticalChanceBonus;

    // ── Consumable Stats ──────────────────────────────────────────────────
    [Header("Consumable Stats")]
    public int healAmount;

    // ── Helpers ───────────────────────────────────────────────────────────
    public bool IsEquipment => itemType == RomanItemType.Weapon || itemType == RomanItemType.Armor;
    public bool IsConsumable => itemType == RomanItemType.Consumable;

    /// <summary>Returns a human-readable sub-type label, or empty string if None.</summary>
    public string GetSubTypeLabel()
    {
        switch (itemType)
        {
            case RomanItemType.Weapon:
                return weaponSubType != WeaponSubType.None ? weaponSubType.ToString() : string.Empty;
            case RomanItemType.Armor:
                return armorSubType != ArmorSubType.None ? armorSubType.ToString() : string.Empty;
            case RomanItemType.Consumable:
                return foodDrinkSubType != FoodDrinkSubType.None ? foodDrinkSubType.ToString() : string.Empty;
            default:
                return string.Empty;
        }
    }

    public bool CanUseWithClass(CharacterClassData currentClass)
    {
        if (classRequirement == RomanClassRequirement.Any)
            return true;

        if (currentClass == null)
            return false;

        return currentClass.className == classRequirement.ToString();
    }

    public string GetClassText()
    {
        return classRequirement == RomanClassRequirement.Any ? "Any class" : classRequirement.ToString();
    }

    public string GetPriceText()
    {
        return RomanCurrency.FormatDenarii(priceDenarii);
    }

    public string GetStatsText()
    {
        string text = "";

        if (damageBonus != 0)         text += $"+{damageBonus} Damage\n";
        if (defenseBonus != 0)        text += $"+{defenseBonus} Defense\n";
        if (maxHealthBonus != 0)      text += $"+{maxHealthBonus} Max HP\n";
        if (healAmount != 0)          text += $"Restores {healAmount} HP\n";
        if (moveSpeedBonus != 0)      text += $"{moveSpeedBonus:+0.##;-0.##} Move Speed\n";
        if (attackRateBonus != 0)     text += $"{attackRateBonus:+0.##;-0.##} Attack Rate\n";
        if (attackRangeBonus != 0)    text += $"{attackRangeBonus:+0.##;-0.##} Attack Range\n";
        if (criticalChanceBonus != 0) text += $"{criticalChanceBonus * 100f:+0;-0}% Critical Chance\n";

        if (string.IsNullOrWhiteSpace(text))
            text = "Utility material.";

        return text.TrimEnd();
    }
}
