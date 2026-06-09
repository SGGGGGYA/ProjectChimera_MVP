using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="Quirk"/> 的 StatMod 计算（与 Equipment 类似但走 effects 列表）。
///
/// 公式回顾：
///   GetFlatMod(s)   = sum(e.amount for e in effects where e.stat==s and !e.isPercent)
///   GetPercentMod(s)= sum(e.amount * 0.01 for e in effects where e.stat==s and e.isPercent)
/// </summary>
[TestFixture]
public class QuirkTests
{
    // ============== GetFlatMod ==============

    [Test]
    public void GetFlatMod_EmptyEffects_ReturnsZero()
    {
        var q = new Quirk { id = "test" };
        Assert.AreEqual(0, q.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_NullEffects_ReturnsZero()
    {
        var q = new Quirk { id = "test", effects = null };
        Assert.AreEqual(0, q.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_SingleFlat_SumsAmount()
    {
        var q = new Quirk
        {
            id = "strong_back",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 4, isPercent = false }
            }
        };
        Assert.AreEqual(4, q.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_MultipleFlat_Accumulates()
    {
        var q = new Quirk
        {
            id = "multi",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 1, isPercent = false },
                new QuirkEffect { stat = StatType.STR, amount = 2, isPercent = false },
                new QuirkEffect { stat = StatType.STR, amount = 3, isPercent = false },
            }
        };
        Assert.AreEqual(6, q.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_PercentEffectsIgnored()
    {
        var q = new Quirk
        {
            id = "percent_only",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 50, isPercent = true }
            }
        };
        Assert.AreEqual(0, q.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_MixedFlatAndPercent_OnlyFlatCounted()
    {
        var q = new Quirk
        {
            id = "mixed",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 5, isPercent = false },
                new QuirkEffect { stat = StatType.STR, amount = 25, isPercent = true },
            }
        };
        Assert.AreEqual(5, q.GetFlatMod(StatType.STR));
    }

    [Test]
    public void GetFlatMod_DifferentStat_NotCounted()
    {
        var q = new Quirk
        {
            id = "test",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.DEF, amount = 8, isPercent = false }
            }
        };
        Assert.AreEqual(0, q.GetFlatMod(StatType.STR));
    }

    // ============== GetPercentMod ==============

    [Test]
    public void GetPercentMod_EmptyEffects_ReturnsZero()
    {
        var q = new Quirk { id = "test" };
        Assert.AreEqual(0f, q.GetPercentMod(StatType.STR));
    }

    [Test]
    public void GetPercentMod_SinglePercent_DividesByHundred()
    {
        var q = new Quirk
        {
            id = "haste",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 25, isPercent = true }
            }
        };
        Assert.AreEqual(0.25f, q.GetPercentMod(StatType.STR), 0.0001f);
    }

    [Test]
    public void GetPercentMod_MultiplePercent_Accumulates()
    {
        var q = new Quirk
        {
            id = "stacked",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 10, isPercent = true },
                new QuirkEffect { stat = StatType.STR, amount = 15, isPercent = true },
            }
        };
        // 0.1 + 0.15 = 0.25
        Assert.AreEqual(0.25f, q.GetPercentMod(StatType.STR), 0.0001f);
    }

    [Test]
    public void GetPercentMod_FlatEffectsIgnored()
    {
        var q = new Quirk
        {
            id = "test",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 7, isPercent = false }
            }
        };
        Assert.AreEqual(0f, q.GetPercentMod(StatType.STR));
    }

    // ============== 默认字段 ==============

    [Test]
    public void DefaultFields_HaveReasonableDefaults()
    {
        var q = new Quirk { id = "test" };
        // 默认 trigger 是 BattleStart
        Assert.AreEqual(QuirkTriggerType.BattleStart, q.triggerType);
        // 默认 procChance = 1 (100%)
        Assert.AreEqual(1f, q.procChance);
        // 默认 effects 是空 list（不是 null）
        Assert.IsNotNull(q.effects);
        Assert.AreEqual(0, q.effects.Count);
    }

    [Test]
    public void IsPositive_FlagCanBeSet()
    {
        var pos = new Quirk { id = "good", isPositive = true };
        var neg = new Quirk { id = "bad", isPositive = false };
        Assert.IsTrue(pos.isPositive);
        Assert.IsFalse(neg.isPositive);
    }
}
