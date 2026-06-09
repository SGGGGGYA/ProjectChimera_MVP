using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="Equipment"/> / <see cref="Weapon"/> / <see cref="Armor"/>
/// 的 StatMod 累加与定价逻辑。
///
/// 公式回顾：
///   GetFlatMod(s)   = sum(mods[i].amount for m in mods where m.stat==s and !m.isPercent)
///   GetPercentMod(s)= sum(mods[i].amount * 0.01 for m in mods where m.stat==s and m.isPercent)
///   GetBuyPrice()   = 50 + Σ rnd(30..80) per mod,  rng 由 id 哈希决定（同 id 同价）
/// </summary>
[TestFixture]
public class EquipmentTests
{
    // ============== GetFlatMod ==============

    [Test]
    public void GetFlatMod_EmptyMods_ReturnsZero()
    {
        var eq = new Equipment { id = "test" };
        Assert.AreEqual(0, eq.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_NullModsList_ReturnsZero()
    {
        var eq = new Equipment { id = "test", mods = null };
        Assert.AreEqual(0, eq.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_SingleFlat_SumsAmount()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 7, isPercent = false } }
        };
        Assert.AreEqual(7, eq.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_MultipleFlat_Accumulates()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 3, isPercent = false },
                new StatMod { stat = StatType.STR, amount = 5, isPercent = false },
                new StatMod { stat = StatType.STR, amount = 2, isPercent = false },
            }
        };
        // 3 + 5 + 2 = 10
        Assert.AreEqual(10, eq.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_PercentModsIgnored()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 50, isPercent = true },
            }
        };
        // isPercent=true 不参与 GetFlatMod
        Assert.AreEqual(0, eq.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_DifferentStat_NotCounted()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.DEF, amount = 5, isPercent = false },
            }
        };
        Assert.AreEqual(0, eq.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_NegativeAmount_Allowed()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 10, isPercent = false },
                new StatMod { stat = StatType.STR, amount = -3, isPercent = false },
            }
        };
        // 10 - 3 = 7（debuff 装备可叠加扣减）
        Assert.AreEqual(7, eq.GetFlatMod(StatType.STR));
    }

    // ============== GetPercentMod ==============

    [Test]
    public void GetPercentMod_EmptyMods_ReturnsZero()
    {
        var eq = new Equipment { id = "test" };
        Assert.AreEqual(0f, eq.GetPercentMod(StatType.STR));
    }

    [Test]
    public void GetPercentMod_SinglePercent_DividesByHundred()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 50, isPercent = true } }
        };
        // 50% → 0.5
        Assert.AreEqual(0.5f, eq.GetPercentMod(StatType.STR), 0.0001f);
    }

    [Test]
    public void GetPercentMod_MultiplePercent_Accumulates()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 20, isPercent = true },
                new StatMod { stat = StatType.STR, amount = 30, isPercent = true },
            }
        };
        // 0.2 + 0.3 = 0.5
        Assert.AreEqual(0.5f, eq.GetPercentMod(StatType.STR), 0.0001f);
    }

    [Test]
    public void GetPercentMod_FlatModsIgnored()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 7, isPercent = false },
            }
        };
        Assert.AreEqual(0f, eq.GetPercentMod(StatType.STR));
    }

    [Test]
    public void GetPercentMod_NegativePercent_Allowed()
    {
        var eq = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = -10, isPercent = true },
            }
        };
        // -10% → -0.1（debuff 装备）
        Assert.AreEqual(-0.1f, eq.GetPercentMod(StatType.STR), 0.0001f);
    }

    // ============== GetBuyPrice ==============

    [Test]
    public void GetBuyPrice_EmptyId_Returns50()
    {
        var eq = new Equipment { id = "" };
        // id 为空直接返回 50
        Assert.AreEqual(50, eq.GetBuyPrice());
    }

    [Test]
    public void GetBuyPrice_NullId_Returns50()
    {
        var eq = new Equipment { id = null };
        Assert.AreEqual(50, eq.GetBuyPrice());
    }

    [Test]
    public void GetBuyPrice_NoMods_Returns50()
    {
        var eq = new Equipment { id = "wpn_test" };
        // id 非空但无 mod：只返回 50，不消耗 rng
        Assert.AreEqual(50, eq.GetBuyPrice());
    }

    [Test]
    public void GetBuyPrice_SameId_Deterministic()
    {
        var eq1 = new Equipment
        {
            id = "wpn_consistent",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 5, isPercent = false },
                new StatMod { stat = StatType.DEF, amount = 10, isPercent = false },
            }
        };
        var eq2 = new Equipment
        {
            id = "wpn_consistent",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 99, isPercent = false },  // amount 故意不同
                new StatMod { stat = StatType.DEF, amount = 99, isPercent = false },
            }
        };
        // 关键性质：价格只跟 id 有关，跟 amount 无关（rng.Next(30,80) 不取 amount）
        Assert.AreEqual(eq1.GetBuyPrice(), eq2.GetBuyPrice());
    }

    [Test]
    public void GetBuyPrice_DifferentId_MayDiffer()
    {
        var eq1 = new Equipment
        {
            id = "wpn_a",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 1, isPercent = false } }
        };
        var eq2 = new Equipment
        {
            id = "wpn_b",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 1, isPercent = false } }
        };
        // 不同 id → 不同种子 → 大概率不同价（hash 冲突概率极小）
        Assert.AreNotEqual(eq1.GetBuyPrice(), eq2.GetBuyPrice());
    }

    [Test]
    public void GetBuyPrice_ModCountAffectsPrice()
    {
        var eq0 = new Equipment { id = "wpn", mods = new List<StatMod>() };
        var eq1 = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 1, isPercent = false } }
        };
        var eq2 = new Equipment
        {
            id = "wpn",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 1, isPercent = false },
                new StatMod { stat = StatType.DEF, amount = 1, isPercent = false },
            }
        };
        // 每个 mod 至少 +30（rng.Next 范围 30..80）
        Assert.Less(eq0.GetBuyPrice(), eq1.GetBuyPrice());
        Assert.Less(eq1.GetBuyPrice(), eq2.GetBuyPrice());
    }

    [Test]
    public void GetBuyPrice_FallsInReasonableRange()
    {
        var eq = new Equipment
        {
            id = "wpn_reasonable",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 1, isPercent = false },
                new StatMod { stat = StatType.DEF, amount = 1, isPercent = false },
                new StatMod { stat = StatType.ACC, amount = 1, isPercent = false },
            }
        };
        int price = eq.GetBuyPrice();
        // 基础 50 + 3 * [30..80] → [140, 290]
        Assert.GreaterOrEqual(price, 140, $"3 mod 装备价格应 >= 140，实际 {price}");
        Assert.LessOrEqual(price, 290, $"3 mod 装备价格应 <= 290，实际 {price}");
    }

    // ============== Weapon / Armor 继承行为 ==============

    [Test]
    public void Weapon_InheritsEquipmentBehavior()
    {
        var w = new Weapon
        {
            id = "sword",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 4, isPercent = false } }
        };
        Assert.AreEqual(4, w.GetFlatMod(StatType.STR));
        Assert.IsInstanceOf<Equipment>(w);
    }

    [Test]
    public void Armor_InheritsEquipmentBehavior()
    {
        var a = new Armor
        {
            id = "plate",
            mods = new List<StatMod> { new StatMod { stat = StatType.DEF, amount = 6, isPercent = false } }
        };
        Assert.AreEqual(6, a.GetFlatMod(StatType.DEF));
        Assert.IsInstanceOf<Equipment>(a);
    }
}
