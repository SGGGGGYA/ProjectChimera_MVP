using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;

[System.Serializable]
public class DropEntry
{
    public string itemId;
    [Range(0f, 1f)] public float chance = 1f;
    public int minQuantity = 1;
    public int maxQuantity = 1;
}

public static class DropTable
{
    private static Dictionary<string, List<DropEntry>> _itemTables;
    private static Dictionary<string, Vector2Int> _goldRanges;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _itemTables = new Dictionary<string, List<DropEntry>>();
        _goldRanges = new Dictionary<string, Vector2Int>();

        Register("哥布林战士", new Vector2Int(5, 12),
            new DropEntry { itemId = "health_potion", chance = 0.30f },
            new DropEntry { itemId = "iron_sword", chance = 0.08f },
            new DropEntry { itemId = "leather_armor", chance = 0.08f }
        );

        Register("哥布林弓手", new Vector2Int(5, 12),
            new DropEntry { itemId = "health_potion", chance = 0.25f },
            new DropEntry { itemId = "stress_herb", chance = 0.10f },
            new DropEntry { itemId = "speed_boots", chance = 0.05f }
        );

        Register("哥布林萨满", new Vector2Int(8, 16),
            new DropEntry { itemId = "stress_herb", chance = 0.50f },
            new DropEntry { itemId = "life_ring", chance = 0.05f }
        );

        Register("野狼", new Vector2Int(3, 8),
            new DropEntry { itemId = "health_potion", chance = 0.20f },
            new DropEntry { itemId = "speed_boots", chance = 0.02f }
        );

        Register("treasure_chest", new Vector2Int(10, 30),
            new DropEntry { itemId = "health_potion", chance = 0.50f },
            new DropEntry { itemId = "stress_herb", chance = 0.40f },
            new DropEntry { itemId = "iron_sword", chance = 0.10f },
            new DropEntry { itemId = "leather_armor", chance = 0.10f },
            new DropEntry { itemId = "life_ring", chance = 0.05f },
            new DropEntry { itemId = "speed_boots", chance = 0.05f }
        );

        _initialized = true;
    }

    private static void Register(string unitName, Vector2Int goldRange, params DropEntry[] entries)
    {
        _itemTables[unitName] = new List<DropEntry>(entries);
        _goldRanges[unitName] = goldRange;
    }

    public static List<DropEntry> GetDropTable(string unitName)
    {
        Initialize();
        _itemTables.TryGetValue(unitName, out var table);
        return table;
    }

    public static int RollGold(string unitName)
    {
        Initialize();
        if (_goldRanges.TryGetValue(unitName, out var range))
            return RandomProvider.Current.Range(range.x, range.y + 1);
        return RandomProvider.Current.Range(2, 6);
    }

    public static List<ItemStack> RollDrops(string unitName)
    {
        Initialize();
        var table = GetDropTable(unitName);
        if (table == null) return new List<ItemStack>();

        var result = new List<ItemStack>();
        foreach (var entry in table)
        {
            if (RandomProvider.Current.Value < entry.chance)
            {
                int qty = RandomProvider.Current.Range(entry.minQuantity, entry.maxQuantity + 1);
                if (qty > 0)
                    result.Add(new ItemStack(entry.itemId, qty));
            }
        }
        return result;
    }
}
