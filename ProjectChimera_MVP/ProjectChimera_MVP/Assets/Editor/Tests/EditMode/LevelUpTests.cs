using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="UnitData.GainExp"/> 与 <see cref="UnitData.GetExpForLevel"/>。
///
/// 公式回顾（V0.5）：
///   GetExpForLevel(lvl):
///     lvl <= 0       → 100
///     lvl >= MAX_LEVEL (10) → int.MaxValue（达到等级上限）
///     lvl in 1..9    → round(100 * lvl^1.8)
///       1→100, 2→348, 3→722, 4→1213, 5→1812, 6→2523, 7→3346, 8→4280, 9→5315
///
///   GainExp(amount):
///     level >= MAX_LEVEL (10) → 静默返回（封顶）
///     否则累加 currentExp；当 currentExp >= ExpToNextLevel 时升级：
///       1) currentExp -= ExpToNextLevel
///       2) level++
///       3) unassignedPoints += POINTS_PER_LEVEL (3)  ← V0.5 改为可分配
///       4) currentHP = MaxHp
///       5) 循环直到 currentExp < ExpToNextLevel 或 level >= MAX_LEVEL
/// </summary>
[TestFixture]
public class LevelUpTests
{
    GameObject go;
    UnitData unit;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("TestUnit_LevelUp");
        unit = go.AddComponent<UnitData>();
        unit.unitName = "TestHero_LevelUp";
        // VIT=10, STR=10, AGI=5, INT=3
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        unit.modifiers = new List<AttributeModifier>();
        unit.quirks = new List<Quirk>();
        unit.equippedWeapon = null;
        unit.equippedArmor = null;
        unit.classData = null;
        unit.breakdownState = BreakdownState.None;
        unit.isPlayer = false;
        unit.skills = new List<SkillData>();
        unit.currentHP = unit.MaxHp;
        unit.level = 1;
        unit.currentExp = 0;
        unit.unassignedPoints = 0;  // V0.5: 显式重置
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null) Object.DestroyImmediate(go);
        go = null;
        unit = null;
    }

    // ============== GetExpForLevel ==============

    [Test]
    public void GetExpForLevel_Level0_Returns100()
    {
        Assert.AreEqual(100, UnitData.GetExpForLevel(0));
    }

    [Test]
    public void GetExpForLevel_Level1_Returns100()
    {
        // 100 * 1^1.8 = 100
        Assert.AreEqual(100, UnitData.GetExpForLevel(1));
    }

    [Test]
    public void GetExpForLevel_Level2_Returns348()
    {
        // round(100 * 2^1.8) = round(348.22) = 348
        Assert.AreEqual(348, UnitData.GetExpForLevel(2));
    }

    [Test]
    public void GetExpForLevel_Level3_Returns722()
    {
        // round(100 * 3^1.8) = round(722.44) = 722
        Assert.AreEqual(722, UnitData.GetExpForLevel(3));
    }

    [Test]
    public void GetExpForLevel_Level4_Returns1213()
    {
        // round(100 * 4^1.8) = round(1212.57) = 1213
        Assert.AreEqual(1213, UnitData.GetExpForLevel(4));
    }

    [Test]
    public void GetExpForLevel_Level5_Returns1812()
    {
        // round(100 * 5^1.8) = round(1811.93) = 1812
        Assert.AreEqual(1812, UnitData.GetExpForLevel(5));
    }

    [Test]
    public void GetExpForLevel_Level6_Returns2523()
    {
        // round(100 * 6^1.8) = round(2522.94) = 2523
        Assert.AreEqual(2523, UnitData.GetExpForLevel(6));
    }

    [Test]
    public void GetExpForLevel_Level7_Returns3346()
    {
        // round(100 * 7^1.8) = round(3345.94) = 3346
        Assert.AreEqual(3346, UnitData.GetExpForLevel(7));
    }

    [Test]
    public void GetExpForLevel_Level8_Returns4280()
    {
        // round(100 * 8^1.8) = round(4280.11) = 4280
        Assert.AreEqual(4280, UnitData.GetExpForLevel(8));
    }

    [Test]
    public void GetExpForLevel_Level9_Returns5315()
    {
        // round(100 * 9^1.8) = round(5314.56) = 5315
        Assert.AreEqual(5315, UnitData.GetExpForLevel(9));
    }

    [Test]
    public void GetExpForLevel_Level10_ReturnsMaxValue()
    {
        // 等级上限封顶（lvl >= MAX_LEVEL = 10）
        Assert.AreEqual(int.MaxValue, UnitData.GetExpForLevel(10),
            "level >= MAX_LEVEL 时 ExpToNextLevel 强制为 int.MaxValue（封顶）");
    }

    [Test]
    public void GetExpForLevel_Level11_ReturnsMaxValue()
    {
        Assert.AreEqual(int.MaxValue, UnitData.GetExpForLevel(11));
    }

    [Test]
    public void GetExpForLevel_NegativeLevel_Returns100()
    {
        Assert.AreEqual(100, UnitData.GetExpForLevel(-1));
        Assert.AreEqual(100, UnitData.GetExpForLevel(-100));
    }

    [Test]
    public void ExpToNextLevel_PropertyDelegatesToStatic()
    {
        unit.level = 3;
        Assert.AreEqual(722, unit.ExpToNextLevel);
    }

    // ============== GainExp 升级触发条件 ==============

    [Test]
    public void GainExp_BelowThreshold_NoLevelUp()
    {
        int lvlBefore = unit.level;
        unit.GainExp(50);  // < 100
        Assert.AreEqual(lvlBefore, unit.level, "未到阈值不升级");
        Assert.AreEqual(50, unit.currentExp);
    }

    [Test]
    public void GainExp_ExactlyThreshold_LevelUp()
    {
        unit.GainExp(100);  // 恰好 100
        Assert.AreEqual(2, unit.level);
        Assert.AreEqual(0, unit.currentExp);
    }

    [Test]
    public void GainExp_OverThreshold_LevelUpWithRemainder()
    {
        unit.GainExp(150);  // 100 升级后剩 50
        Assert.AreEqual(2, unit.level);
        Assert.AreEqual(50, unit.currentExp);
    }

    [Test]
    public void GainExp_LargeAmount_MultiLevelUp()
    {
        // V0.5 公式 100*L^1.8：1→2: 100, 2→3: 348 → 总 448
        // 一次给 448: 1→2 (剩 348), 2→3 (剩 0)
        unit.GainExp(448);
        Assert.AreEqual(3, unit.level);
        Assert.AreEqual(0, unit.currentExp);
    }

    [Test]
    public void GainExp_CrossLevelBoundaries_AccumulatesCorrectly()
    {
        // 第一次 50
        unit.GainExp(50);
        Assert.AreEqual(1, unit.level);
        Assert.AreEqual(50, unit.currentExp);
        // 第二次 60 → 累计 110 → 升 1 级，剩 10
        unit.GainExp(60);
        Assert.AreEqual(2, unit.level);
        Assert.AreEqual(10, unit.currentExp);
    }

    // ============== 等级上限封顶 ==============

    [Test]
    public void GainExp_AtMaxLevel_NoEffect()
    {
        unit.level = UnitData.MAX_LEVEL;
        int lvlBefore = unit.level;
        int expBefore = unit.currentExp;
        unit.GainExp(9999);
        Assert.AreEqual(lvlBefore, unit.level, "level=MAX_LEVEL 已封顶，不应再升");
        Assert.AreEqual(expBefore, unit.currentExp, "封顶时不应累加 currentExp");
    }

    [Test]
    public void GainExp_MultiLevelReachingCap_StopsAt10()
    {
        // 1→2→...→10 总经验 100+348+722+1213+1812+2523+3346+4280+5315 = 19359
        unit.GainExp(20000);
        Assert.AreEqual(UnitData.MAX_LEVEL, unit.level, "应封顶在 MAX_LEVEL=10");
        Assert.AreEqual(20000 - 19359, unit.currentExp, "应保留剩余经验 641");
    }

    [Test]
    public void GainExp_MultiLevelReachingCap_AccumulatesUnassignedPoints()
    {
        // 升 9 次（1→10），每次 +3 点 → unassignedPoints = 27
        unit.GainExp(19359);
        Assert.AreEqual(UnitData.MAX_LEVEL, unit.level);
        Assert.AreEqual(9 * UnitData.POINTS_PER_LEVEL, unit.unassignedPoints,
            "1→10 共升 9 级，应累加 27 属性点");
    }

    // ============== 升级加点分配（V0.5 新增） ==============

    [Test]
    public void GainExp_LevelUp_AccumulatesUnassignedPoints()
    {
        // 1 级 → 2 级，升 1 次 → +3 点
        unit.GainExp(100);
        Assert.AreEqual(1, unit.level);
        Assert.AreEqual(UnitData.POINTS_PER_LEVEL, unit.unassignedPoints);
    }

    [Test]
    public void GainExp_LevelUp_DoesNotChangePrimaryAttributes()
    {
        // V0.5: 升级不再自动加主属性，所有主属性变化必须显式 AllocatePoint
        int vitBefore = unit.primaryAttributes.VIT;
        int strBefore = unit.primaryAttributes.STR;
        int agiBefore = unit.primaryAttributes.AGI;
        int intBefore = unit.primaryAttributes.INT;

        unit.GainExp(100);  // 升 1 级

        Assert.AreEqual(vitBefore, unit.primaryAttributes.VIT, "VIT 不应自动增加");
        Assert.AreEqual(strBefore, unit.primaryAttributes.STR, "STR 不应自动增加");
        Assert.AreEqual(agiBefore, unit.primaryAttributes.AGI, "AGI 不应自动增加");
        Assert.AreEqual(intBefore, unit.primaryAttributes.INT, "INT 不应自动增加");
    }

    // ---- AllocatePoint ----

    [Test]
    public void AllocatePoint_VIT_IncrementsVIT()
    {
        unit.unassignedPoints = 3;
        bool ok = unit.AllocatePoint(StatType.VIT);
        Assert.IsTrue(ok);
        Assert.AreEqual(11, unit.primaryAttributes.VIT);
        Assert.AreEqual(2, unit.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_STR_IncrementsSTR()
    {
        unit.unassignedPoints = 1;
        bool ok = unit.AllocatePoint(StatType.STR);
        Assert.IsTrue(ok);
        Assert.AreEqual(11, unit.primaryAttributes.STR);
        Assert.AreEqual(0, unit.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_AGI_IncrementsAGI()
    {
        unit.unassignedPoints = 2;
        unit.AllocatePoint(StatType.AGI);
        Assert.AreEqual(6, unit.primaryAttributes.AGI);
        Assert.AreEqual(1, unit.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_INT_IncrementsINT()
    {
        unit.unassignedPoints = 1;
        unit.AllocatePoint(StatType.INT);
        Assert.AreEqual(4, unit.primaryAttributes.INT);
        Assert.AreEqual(0, unit.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_NoPoints_ReturnsFalse()
    {
        unit.unassignedPoints = 0;
        bool ok = unit.AllocatePoint(StatType.STR);
        Assert.IsFalse(ok, "unassignedPoints=0 时应返回 false");
        Assert.AreEqual(10, unit.primaryAttributes.STR, "属性不变");
    }

    [Test]
    public void AllocatePoint_InvalidStat_ReturnsFalse()
    {
        unit.unassignedPoints = 5;
        // SPD/DEF/ACC/DOD/CRT 等是派生属性，不在加点范围内
        bool okSpd = unit.AllocatePoint(StatType.SPD);
        bool okDef = unit.AllocatePoint(StatType.DEF);
        bool okMaxHp = unit.AllocatePoint(StatType.MaxHP);

        Assert.IsFalse(okSpd, "SPD 是派生属性，不可分配");
        Assert.IsFalse(okDef, "DEF 是派生属性，不可分配");
        Assert.IsFalse(okMaxHp, "MaxHP 是派生属性，不可分配");
        Assert.AreEqual(5, unit.unassignedPoints, "失败时点数不变");
    }

    [Test]
    public void AllocatePoint_MultipleAllocations_DecrementsCounter()
    {
        unit.unassignedPoints = 9;
        // 3 次 VIT + 3 次 STR + 3 次 AGI = 9 点
        for (int i = 0; i < 3; i++) unit.AllocatePoint(StatType.VIT);
        for (int i = 0; i < 3; i++) unit.AllocatePoint(StatType.STR);
        for (int i = 0; i < 3; i++) unit.AllocatePoint(StatType.AGI);

        Assert.AreEqual(13, unit.primaryAttributes.VIT);
        Assert.AreEqual(13, unit.primaryAttributes.STR);
        Assert.AreEqual(8, unit.primaryAttributes.AGI);
        Assert.AreEqual(0, unit.unassignedPoints);
    }

    [Test]
    public void AllocatePoint_DoesNotChangeUnrelatedStats()
    {
        unit.unassignedPoints = 1;
        int intBefore = unit.primaryAttributes.INT;
        unit.AllocatePoint(StatType.STR);
        Assert.AreEqual(intBefore, unit.primaryAttributes.INT, "分配 STR 不应影响 INT");
    }

    // ============== 升级 HP 恢复 ==============

    [Test]
    public void GainExp_LevelUp_RestoresHPToMax()
    {
        unit.currentHP = 1;  // 残血
        int maxHpBefore = unit.MaxHp;
        unit.GainExp(100);
        Assert.AreEqual(maxHpBefore + 5, unit.MaxHp,
            "VIT+1 → MaxHp +5");
        Assert.AreEqual(unit.MaxHp, unit.currentHP,
            "升级后 currentHP 应等于新的 MaxHp");
    }

    [Test]
    public void GainExp_NoLevelUp_DoesNotRestoreHP()
    {
        unit.currentHP = 1;
        unit.GainExp(50);  // 不足以升级
        Assert.AreEqual(1, unit.currentHP, "未升级时 HP 不变");
    }

    // ============== 累加性 ==============

    [Test]
    public void GainExp_MultipleSmallGains_Accumulates()
    {
        for (int i = 0; i < 10; i++)
            unit.GainExp(10);
        Assert.AreEqual(100, unit.currentExp);
        Assert.AreEqual(1, unit.level);
    }
}
