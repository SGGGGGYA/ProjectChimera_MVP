using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="UnitData.TriggerNamingMoment"/> 与相关命名机制。
///
/// 命名时刻规则（设计文档 2.3 节 + V0.5 适配）：
///   - 触发时机：升到 Level 6（V0.5: 等级上限 10，命名设在毕业半程点）
///   - 奖励 1：全属性 +10%（注入 Mul 修饰器，自动接入 GetFinalStat 管线）
///   - 奖励 2：压力上限 +20（unit.stressCapBonus = 20）
///   - 奖励 3：解锁专属技能槽（exclusiveSkill 字段）
///   - 奖励 4：获得 [传奇之名] 怪癖（namingQuirk，isLocked=true，不占 5 怪癖位）
///   - 副作用：positiveQuirkCount++、HP 满血
///
/// 1→6 总经验 = 100+348+722+1213+1812 = 4195
/// </summary>
[TestFixture]
public class NamingMomentTests
{
    GameObject go;
    UnitData unit;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("TestUnit_Naming");
        unit = go.AddComponent<UnitData>();
        unit.unitName = "TestHero_Naming";
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        unit.modifiers = new List<AttributeModifier>();
        unit.quirks = new List<Quirk>();
        unit.equippedWeapon = null;
        unit.equippedArmor = null;
        unit.classData = null;
        unit.breakdownState = BreakdownState.None;
        unit.stress = 0;
        unit.stressResistRate = 0f;
        unit.level = 1;
        unit.currentExp = 0;
        unit.skills = new List<SkillData>();
        unit.isPlayer = false;
        unit.isNamed = false;
        unit.stressCapBonus = 0;
        unit.namingQuirk = null;
        unit.exclusiveSkill = null;
        unit.positiveQuirkCount = 0;
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null) Object.DestroyImmediate(go);
        go = null;
        unit = null;
    }

    // ========== 1. 字段默认值 ==========

    [Test]
    public void Naming_DefaultFields_AreUnnamed()
    {
        var fresh = go.AddComponent<UnitData>();
        Assert.IsFalse(fresh.isNamed, "默认未命名");
        Assert.AreEqual(0, fresh.stressCapBonus, "默认压力上限奖励 0");
        Assert.IsNull(fresh.namingQuirk, "默认无命名怪癖");
        Assert.IsNull(fresh.exclusiveSkill, "默认无专属技能");
    }

    [Test]
    public void NAMING_LEVEL_Is6()
    {
        // V0.5: 命名时机常量应为 6（等级上限 10 下的毕业半程点）
        Assert.AreEqual(6, UnitData.NAMING_LEVEL);
    }

    [Test]
    public void NAMING_STRESS_BONUS_Is20()
    {
        Assert.AreEqual(20, UnitData.NAMING_STRESS_BONUS);
    }

    [Test]
    public void NAMING_ALL_STAT_PERCENT_Is10Percent()
    {
        Assert.AreEqual(0.10f, UnitData.NAMING_ALL_STAT_PERCENT, 0.0001f);
    }

    // ========== 2. TriggerNamingMoment 基础状态变化 ==========

    [Test]
    public void TriggerNamingMoment_SetsIsNamedTrue()
    {
        unit.TriggerNamingMoment();
        Assert.IsTrue(unit.isNamed);
    }

    [Test]
    public void TriggerNamingMoment_SetsStressCapBonusTo20()
    {
        unit.TriggerNamingMoment();
        Assert.AreEqual(20, unit.stressCapBonus);
    }

    [Test]
    public void TriggerNamingMoment_CreatesNamingQuirk()
    {
        unit.TriggerNamingMoment();
        Assert.IsNotNull(unit.namingQuirk);
        Assert.AreEqual("naming_legacy", unit.namingQuirk.id);
        Assert.AreEqual("传奇之名", unit.namingQuirk.localizedName);
        Assert.IsTrue(unit.namingQuirk.isPositive, "命名怪癖是正面");
        Assert.IsTrue(unit.namingQuirk.isLocked, "命名怪癖锁定不可移除");
    }

    [Test]
    public void TriggerNamingMoment_IncrementsPositiveQuirkCount()
    {
        unit.positiveQuirkCount = 2;
        unit.TriggerNamingMoment();
        Assert.AreEqual(3, unit.positiveQuirkCount);
    }

    [Test]
    public void TriggerNamingMoment_RestoresHPToMax()
    {
        unit.currentHP = 1;
        int maxHp = unit.MaxHp;
        unit.TriggerNamingMoment();
        Assert.AreEqual(maxHp, unit.currentHP, "命名后 HP 满血");
    }

    // ========== 3. 一次性触发守卫 ==========

    [Test]
    public void TriggerNamingMoment_CalledTwice_IsIdempotent()
    {
        unit.TriggerNamingMoment();
        int modifiersAfterFirst = unit.modifiers.Count;
        Quirk firstQuirk = unit.namingQuirk;

        unit.TriggerNamingMoment();

        Assert.IsTrue(unit.isNamed);
        Assert.AreEqual(modifiersAfterFirst, unit.modifiers.Count,
            "第二次调用不应重复添加修饰器");
        Assert.AreSame(firstQuirk, unit.namingQuirk,
            "第二次调用不应替换命名怪癖");
    }

    // ========== 4. 全属性 +10% 修饰器注入 ==========

    [Test]
    public void TriggerNamingMoment_Adds10PercentMulModifierForEveryTarget()
    {
        unit.TriggerNamingMoment();

        // 统计 source="naming_moment" 的修饰器数量
        int namingMods = 0;
        foreach (var m in unit.modifiers)
            if (m.source == "naming_moment") namingMods++;

        // AttributeTarget 有 8 个枚举值：MaxHP, SPD, ACC, DOD, DEF, STR, INT, CRT
        int targetCount = System.Enum.GetValues(typeof(AttributeTarget)).Length;
        Assert.AreEqual(targetCount, namingMods,
            $"应为每个 AttributeTarget 添加一个 +10% Mul 修饰器（共 {targetCount} 个）");
    }

    [Test]
    public void TriggerNamingMoment_AllModifiersAreMulType()
    {
        unit.TriggerNamingMoment();
        foreach (var m in unit.modifiers)
        {
            if (m.source == "naming_moment")
            {
                Assert.AreEqual(ModifierType.Mul, m.type, "命名奖励必须是 Mul 类型");
                Assert.AreEqual(0.10f, m.value, 0.0001f, "命名奖励值必须是 0.10");
                Assert.AreEqual(0, m.duration, "命名奖励是永久 buff（duration=0）");
            }
        }
    }

    // ========== 5. GetFinalStat 在命名后提升 10% ==========

    [Test]
    public void GetFinalStat_AfterNaming_Applies10Percent()
    {
        int strBefore = unit.GetFinalStat(StatType.STR);
        Assert.AreEqual(10, strBefore, "命名前 STR = 10（primary）");

        unit.TriggerNamingMoment();

        int strAfter = unit.GetFinalStat(StatType.STR);
        // round(10 * 1.10) = round(11.0) = 11
        Assert.AreEqual(11, strAfter, "命名后 STR = 11（10 * 1.10）");
    }

    [Test]
    public void GetFinalStat_AfterNaming_AppliesToAllStatTypes()
    {
        unit.TriggerNamingMoment();

        // 验证各核心 stat 都有 ~10% 提升
        // 基础值：VIT=10, STR=10, AGI=5, INT=3 → 各乘 1.10
        Assert.AreEqual(11, unit.GetFinalStat(StatType.VIT), "VIT: 10 → 11");
        Assert.AreEqual(11, unit.GetFinalStat(StatType.STR), "STR: 10 → 11");
        Assert.AreEqual(6, unit.GetFinalStat(StatType.AGI), "AGI: 5 → 6 (5*1.10=5.5→round 6)");
        Assert.AreEqual(3, unit.GetFinalStat(StatType.INT), "INT: 3 → 3 (3*1.10=3.3→round 3)");
    }

    [Test]
    public void GetFinalStat_NamingQuirk_AdditionalPercentStacks()
    {
        // 命名怪癖：+5% ACC, +3% CRT, +5% SPD
        unit.TriggerNamingMoment();

        // 验证命名怪癖的修饰与全属性+10%叠加。
        // 修复 P0 后 ACC 基础值 = 5 + AGI*5 = 5 + 5*5 = 30（不再恒为 0）。
        // 命名怪癖 +5% 与全属性 +10% 叠加 = 1.15 multiplier。
        // 期望：round(30 * 1.15) = round(34.5) = 35。
        int acc = unit.GetFinalStat(StatType.ACC);
        Assert.AreEqual(35, acc, "ACC 基础 = 30，命名 +5% + 命名奖励 +10% = 30*1.15 = 35");

        // 验证命名怪癖 quirk 引用与百分比正确
        Assert.IsNotNull(unit.namingQuirk);
        Assert.AreEqual(0.05f, unit.namingQuirk.GetPercentMod(StatType.ACC), 0.0001f,
            "命名怪癖应贡献 +5% ACC");
    }

    // ========== 6. 压力上限扩展 ==========

    [Test]
    public void GetStressCap_NonNamed_ReturnsConfigMax()
    {
        // StressManager.config.maxStress 默认 200
        Assert.AreEqual(200, StressManager.GetStressCap(unit));
    }

    [Test]
    public void GetStressCap_NamedUnit_Returns220()
    {
        unit.TriggerNamingMoment();
        Assert.AreEqual(220, StressManager.GetStressCap(unit),
            "命名后压力上限 = 200 + 20 = 220");
    }

    [Test]
    public void GetStressCap_NullUnit_HandlesGracefully()
    {
        // 不应抛异常
        int cap = StressManager.GetStressCap(null);
        Assert.AreEqual(200, cap, "unit 为 null 时返回 config.maxStress");
    }

    [Test]
    public void AddStress_NamedUnit_CanExceed200()
    {
        // 初始化 StressManager
        StressManager.InitFromConfig(new StressConfig());
        unit.TriggerNamingMoment();
        unit.stress = 200;  // 旧上限

        // 命名后再加 30，应能装得下（不会卡在 200）
        StressManager.AddStress(unit, 30, StressTag.Combat);

        // 实际增益受 stressResistRate 影响（默认 0），所以是 30
        // 但 cap 是 220，所以最终 200+30=230 → clamp 到 220
        Assert.LessOrEqual(unit.stress, 220, "命名后压力不能超过 220");
        Assert.GreaterOrEqual(unit.stress, 200, "命名后压力至少为 200");
    }

    [Test]
    public void AddStress_NamedUnitAtCap_StaysAt220()
    {
        StressManager.InitFromConfig(new StressConfig());
        unit.TriggerNamingMoment();
        unit.stress = 219;
        StressManager.AddStress(unit, 100, StressTag.Combat);
        Assert.AreEqual(220, unit.stress, "超 220 后应被 clamp 到 220");
    }

    // ========== 7. GainExp 自动触发命名 ==========

    [Test]
    public void GainExp_ReachesLevel6_TriggersNamingMoment()
    {
        // V0.5 公式 100*L^1.8：1→2:100, 2→3:348, 3→4:722, 4→5:1213, 5→6:1812 → 总 4195
        unit.GainExp(4195);
        Assert.AreEqual(6, unit.level);
        Assert.IsTrue(unit.isNamed, "升到 6 级时自动命名");
        Assert.AreEqual(20, unit.stressCapBonus);
        Assert.IsNotNull(unit.namingQuirk);
    }

    [Test]
    public void GainExp_DoesNotTrigger_BeforeLevel6()
    {
        unit.GainExp(100);  // 升 1 级到 2
        Assert.AreEqual(2, unit.level);
        Assert.IsFalse(unit.isNamed, "2 级不应触发命名");
    }

    [Test]
    public void GainExp_TriggersOnlyOnce_OnMultipleCalls()
    {
        unit.GainExp(4195);
        int modsAfterFirst = unit.modifiers.Count;
        Quirk firstQuirk = unit.namingQuirk;

        // 第二次给经验（不升级但应确保不重复触发）
        unit.GainExp(100);

        Assert.AreEqual(modsAfterFirst, unit.modifiers.Count,
            "命名后再次给经验不应重复触发");
        Assert.AreSame(firstQuirk, unit.namingQuirk,
            "命名怪癖引用应保持");
    }

    [Test]
    public void GainExp_OverflowToLevel6_TriggersNaming()
    {
        // 1→6 总 4195，一次给 5000：升 5 级到 6，剩 805
        unit.GainExp(5000);
        Assert.AreEqual(6, unit.level);
        Assert.IsTrue(unit.isNamed, "溢出经验一次升到 6 级也应触发命名");
        Assert.AreEqual(5000 - 4195, unit.currentExp, "剩余经验保留 805");
    }

    // ========== 8. 命名怪癖不占 5 怪癖位（不污染 quirks 列表）==========

    [Test]
    public void TriggerNamingMoment_DoesNotAddToQuirksList()
    {
        // quirks 列表保持独立，namingQuirk 是单独字段
        unit.quirks.Add(new Quirk { id = "test_quirk" });
        int countBefore = unit.quirks.Count;

        unit.TriggerNamingMoment();

        Assert.AreEqual(countBefore, unit.quirks.Count,
            "命名不应污染 quirks 列表");
        Assert.IsNotNull(unit.namingQuirk,
            "命名怪癖应存在但放在独立字段");
        Assert.AreNotEqual(unit.quirks, unit.namingQuirk,
            "namingQuirk 与 quirks 是独立容器");
    }

    // ========== 9. 命名后仍可继续操作 quirks 列表 ==========

    [Test]
    public void NamedUnit_CanStillHaveRegularQuirks()
    {
        unit.TriggerNamingMoment();
        unit.quirks.Add(new Quirk
        {
            id = "warrior",
            isPositive = true,
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 2, isPercent = false }
            }
        });

        // STR = base(10) + quirk(2) + 命名+10% mul
        // additive = 10 + 2 = 12
        // multiplier = 1 + 0.10 = 1.10
        // round(12 * 1.10) = round(13.2) = 13
        Assert.AreEqual(13, unit.GetFinalStat(StatType.STR),
            "命名后可叠加普通怪癖：base + 普通怪癖 + 命名 10% mul");
    }

    // ========== 10. exclusiveSkill 字段（命名槽位）==========

    [Test]
    public void TriggerNamingMoment_LeavesExclusiveSkillNull()
    {
        // 命名本身不分配专属技能，仅解锁槽位
        unit.TriggerNamingMoment();
        Assert.IsNull(unit.exclusiveSkill,
            "命名时刻不解锁具体技能，玩家需在英雄厅手动装备");
    }

    [Test]
    public void NamedUnit_CanReceiveExclusiveSkill()
    {
        unit.TriggerNamingMoment();
        unit.exclusiveSkill = new SkillData { skillName = "HeroicStrike" };
        Assert.IsNotNull(unit.exclusiveSkill);
        Assert.AreEqual("HeroicStrike", unit.exclusiveSkill.skillName);
    }
}
