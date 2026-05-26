using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory
{
    Weapon,
    Armor,
    Consumable,
    Material
}

[System.Serializable]
public class ItemStack
{
    public string itemId;
    public int quantity = 1;

    public ItemStack(string id, int qty = 1)
    {
        itemId = id;
        quantity = qty;
    }
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Chimera/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public ItemCategory category;
    public int maxStack = 1;
    public string iconPath;

    public Weapon weaponTemplate;
    public Armor armorTemplate;

    public bool IsEquipment => category == ItemCategory.Weapon || category == ItemCategory.Armor;

    [Header("消耗品效果")]
    public int healAmount;
    public int stressRelief;
}
