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
        _items["health_potion"] = potion;

        if (_items.ContainsKey("stress_herb")) return;
        var herb = ScriptableObject.CreateInstance<ItemDefinition>();
        herb.itemId = "stress_herb";
        herb.itemName = "镇静草药";
        herb.description = "使用后降低 15 点压力";
        herb.category = ItemCategory.Consumable;
        herb.maxStack = 5;
        herb.stressRelief = 15;
        _items["stress_herb"] = herb;
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
