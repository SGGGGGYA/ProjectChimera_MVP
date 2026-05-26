using System.Collections.Generic;
using UnityEngine;

public static class ItemDatabase
{
    static Dictionary<string, ItemDefinition> _items;
    static bool _initialized;

    public static void Initialize(List<ItemDefinition> definitions)
    {
        _items = new Dictionary<string, ItemDefinition>();
        if (definitions != null)
            foreach (var def in definitions)
                if (def != null && !string.IsNullOrEmpty(def.itemId))
                    _items[def.itemId] = def;
        InitBuiltins();
        _initialized = true;
    }

    static void InitBuiltins()
    {
        if (_items.ContainsKey("health_potion")) return;
        var potion = ScriptableObject.CreateInstance<ItemDefinition>();
        potion.itemId = "health_potion";
        potion.itemName = "生命药水";
        potion.description = "使用后恢复 20 点生命";
        potion.category = ItemCategory.Consumable;
        potion.maxStack = 5;
        potion.healAmount = 20;
        potion.buyPrice = 80;
        potion.sellPrice = 40;
        _items["health_potion"] = potion;

        if (_items.ContainsKey("stress_herb")) return;
        var herb = ScriptableObject.CreateInstance<ItemDefinition>();
        herb.itemId = "stress_herb";
        herb.itemName = "镇静草药";
        herb.description = "使用后降低 15 点压力";
        herb.category = ItemCategory.Consumable;
        herb.maxStack = 5;
        herb.stressRelief = 15;
        herb.buyPrice = 120;
        herb.sellPrice = 60;
        _items["stress_herb"] = herb;

        if (_items.ContainsKey("iron_sword")) return;
        var ironSword = ScriptableObject.CreateInstance<ItemDefinition>();
        ironSword.itemId = "iron_sword";
        ironSword.itemName = "铁剑";
        ironSword.description = "一把普通的铁剑\nSTR+3";
        ironSword.category = ItemCategory.Weapon;
        ironSword.maxStack = 1;
        ironSword.buyPrice = 120;
        ironSword.sellPrice = 60;
        ironSword.weaponBaseAttack = 5;
        ironSword.weaponTemplate = new Weapon { id = "iron_sword", equipmentName = "铁剑",
            mods = new List<StatMod>{ new StatMod{stat=StatType.STR, amount=3} } };
        _items["iron_sword"] = ironSword;

        if (_items.ContainsKey("leather_armor")) return;
        var leatherArmor = ScriptableObject.CreateInstance<ItemDefinition>();
        leatherArmor.itemId = "leather_armor";
        leatherArmor.itemName = "皮甲";
        leatherArmor.description = "轻便的皮甲\nDEF+2";
        leatherArmor.category = ItemCategory.Armor;
        leatherArmor.maxStack = 1;
        leatherArmor.buyPrice = 100;
        leatherArmor.sellPrice = 50;
        leatherArmor.armorTemplate = new Armor { id = "leather_armor", equipmentName = "皮甲",
            mods = new List<StatMod>{ new StatMod{stat=StatType.DEF, amount=2} } };
        _items["leather_armor"] = leatherArmor;

        if (_items.ContainsKey("life_ring")) return;
        var lifeRing = ScriptableObject.CreateInstance<ItemDefinition>();
        lifeRing.itemId = "life_ring";
        lifeRing.itemName = "生命戒指";
        lifeRing.description = "蕴含生命能量的戒指\nMaxHP+15";
        lifeRing.category = ItemCategory.Weapon;
        lifeRing.maxStack = 1;
        lifeRing.buyPrice = 200;
        lifeRing.sellPrice = 100;
        lifeRing.weaponTemplate = new Weapon { id = "life_ring", equipmentName = "生命戒指",
            mods = new List<StatMod>{ new StatMod{stat=StatType.MaxHP, amount=15} } };
        _items["life_ring"] = lifeRing;

        if (_items.ContainsKey("speed_boots")) return;
        var speedBoots = ScriptableObject.CreateInstance<ItemDefinition>();
        speedBoots.itemId = "speed_boots";
        speedBoots.itemName = "速度之靴";
        speedBoots.description = "轻快的靴子\nSPD+3";
        speedBoots.category = ItemCategory.Armor;
        speedBoots.maxStack = 1;
        speedBoots.buyPrice = 180;
        speedBoots.sellPrice = 90;
        speedBoots.armorTemplate = new Armor { id = "speed_boots", equipmentName = "速度之靴",
            mods = new List<StatMod>{ new StatMod{stat=StatType.SPD, amount=3} } };
        _items["speed_boots"] = speedBoots;
    }

    public static ItemDefinition Get(string id)
    {
        if (!_initialized || string.IsNullOrEmpty(id)) return null;
        _items.TryGetValue(id, out var def);
        return def;
    }

    public static List<ItemDefinition> GetAll() =>
        _initialized ? new List<ItemDefinition>(_items.Values) : new List<ItemDefinition>();

    public static List<ItemDefinition> GetByCategory(ItemCategory cat)
    {
        var result = new List<ItemDefinition>();
        if (!_initialized) return result;
        foreach (var item in _items.Values)
            if (item.category == cat)
                result.Add(item);
        return result;
    }
}
