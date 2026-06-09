using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 UnitData.GetFinalStat 的属性管线计算。
/// 管线顺序（实际实现）：
///   additive  = baseVal(classBase + primary) + equipFlat + quirkFlat + buffFlat
///   multiplier = 1 + equipPct + quirkPct + buffPct
///   return round(additive * max(0, multiplier) * stressMod)
///   - STR/INT/DEF 还会乘以 StressManager.GetStatModifier(unit)
///   - MaxHP 是特例：baseHp = VIT*5 + (classData?.baseHp ?? GetClassBaseHP())
/// </summary>
[TestFixture]
public class StatCalculationTests
{
    GameObject testGO;
    UnitData unit;
    readonly List<Object> spawnedAssets = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        // MonoBehaviour 必须先有宿主 GameObject 才能 AddComponent
        testGO = new GameObject("TestUnit");
        unit = testGO.AddComponent<UnitData>();

        // 重置受测字段，避免组件默认值干扰
        unit.unitName = "TestUnit";
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        unit.modifiers = new List<AttributeModifier>();
        unit.quirks = new List<Quirk>();
        unit.equippedWeapon = null;
        unit.equippedArmor = null;
        unit.classData = null;
        unit.breakdownState = BreakdownState.None; // 防止 STR/INT/DEF 走 stressMod
        unit.stress = 0;
    }

    [TearDown]
    public void TearDown()
    {
        if (testGO != null)
        {
            Object.DestroyImmediate(testGO);
            testGO = null;
            unit = null;
        }
        foreach (var a in spawnedAssets)
            if (a != null) Object.DestroyImmediate(a);
        spawnedAssets.Clear();
    }

    // ---------- 1. 空 modifiers：返回 base 原值 ----------
    [Test]
    public void GetFinalStat_EmptyModifiers_ReturnsBaseValue()
    {
        unit.modifiers.Clear();
        // STR base = classBase(0) + primary.STR(10) = 10
        Assert.AreEqual(10, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_EmptyModifiers_SPDDerivedFromAGI()
    {
        unit.modifiers.Clear();
        // 修复前：SPD 返回 0（GetClassBaseByType 漏处理）。
        // 修复后：SPD = AGI*2 + classBaseSPD(0) = 5*2 + 0 = 10。
        Assert.AreEqual(10, unit.GetFinalStat(StatType.SPD),
            "SPD 不再是 0，应由 AGI 派生");
    }

    // ---------- 2. 单个 Add：原值 + value ----------
    [Test]
    public void GetFinalStat_SingleAdd_AddsValue()
    {
        unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "buff_add"));
        // 10 + 5 = 15
        Assert.AreEqual(15, unit.GetFinalStat(StatType.STR));
    }

    // ---------- 3. 多个 Add 累加 ----------
    [Test]
    public void GetFinalStat_MultipleAdds_Accumulate()
    {
        unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "a"));
        unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 3f, "b"));
        unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 7f, "c"));
        // 10 + 5 + 3 + 7 = 25
        Assert.AreEqual(25, unit.GetFinalStat(StatType.STR));
    }

    // ---------- 4. 单个 Mul：相乘 ----------
    [Test]
    public void GetFinalStat_SingleMul_Multiplies()
    {
        unit.modifiers.Add(new AttributeModifier(ModifierType.Mul, AttributeTarget.STR, 0.5f, "buff_mul"));
        // round(10 * 1.5) = 15
        Assert.AreEqual(15, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_MulWithZeroBase_StaysZero()
    {
        // 修复 P0 后 SPD 公式变为 AGI*2+classBaseSPD，单靠空 modifier 拿不到 0 base。
        // 这里把 AGI 设为 0 且不挂 classData，让 SPD 走通到 0，验证 0*multiplier=0。
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 0, 3);
        unit.modifiers.Add(new AttributeModifier(ModifierType.Mul, AttributeTarget.SPD, 0.5f, "mul"));
        // SPD base = 0*2+0 = 0; round(0 * 1.5) = 0
        Assert.AreEqual(0, unit.GetFinalStat(StatType.SPD));
    }

    // ---------- 5. Add + Mul 混合：先 Add 后 Mul（实现是 additive 求和后再乘以总 multiplier）----------
    [Test]
    public void GetFinalStat_AddAndMulMixed_AddsFirstThenMultiplies()
    {
        unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 10f, "add"));
        unit.modifiers.Add(new AttributeModifier(ModifierType.Mul, AttributeTarget.STR, 0.5f, "mul"));
        // additive = 10 + 10 = 20
        // multiplier = 1 + 0.5 = 1.5
        // round(20 * 1.5) = 30
        Assert.AreEqual(30, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_MultipleMuls_CombineAdditively()
    {
        unit.modifiers.Add(new AttributeModifier(ModifierType.Mul, AttributeTarget.STR, 0.2f, "m1"));
        unit.modifiers.Add(new AttributeModifier(ModifierType.Mul, AttributeTarget.STR, 0.3f, "m2"));
        // multiplier = 1 + 0.2 + 0.3 = 1.5
        // round(10 * 1.5) = 15
        Assert.AreEqual(15, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_MulOnUnrelatedTarget_DoesNotAffectStat()
    {
        unit.modifiers.Add(new AttributeModifier(ModifierType.Mul, AttributeTarget.INT, 0.5f, "wrong_target"));
        // target 不匹配，STR 仍为 10
        Assert.AreEqual(10, unit.GetFinalStat(StatType.STR));
    }

    // ---------- 6. equipment 字段不为 null 时生效 ----------
    [Test]
    public void GetFinalStat_WeaponEquipped_FlatModApplies()
    {
        unit.equippedWeapon = new Weapon
        {
            id = "wpn_test",
            equipmentName = "Test Sword",
            mods = new List<StatMod>
            {
                new StatMod { stat = StatType.STR, amount = 7, isPercent = false }
            }
        };
        // 10 + 7(weapon) = 17
        Assert.AreEqual(17, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_WeaponAndArmor_FlatModsStack()
    {
        unit.equippedWeapon = new Weapon
        {
            id = "wpn_test",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 3, isPercent = false } }
        };
        unit.equippedArmor = new Armor
        {
            id = "arm_test",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 2, isPercent = false } }
        };
        // 10 + 3 + 2 = 15
        Assert.AreEqual(15, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_EquipmentPercent_AppliesAfterBase()
    {
        unit.equippedWeapon = new Weapon
        {
            id = "wpn_test",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 50, isPercent = true } }
        };
        // Equipment.GetPercentMod 中 isPercent 用 amount*0.01f 折算
        // multiplier = 1 + 0.5 = 1.5
        // round(10 * 1.5) = 15
        Assert.AreEqual(15, unit.GetFinalStat(StatType.STR));
    }

    // ---------- 额外：Quirk / 全管线混合，确保不破坏既有约定 ----------
    [Test]
    public void GetFinalStat_QuirkFlatMod_Applies()
    {
        var quirk = new Quirk
        {
            id = "quirk_test",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 4, isPercent = false }
            }
        };
        unit.quirks.Add(quirk);
        // 10 + 4(quirk) = 14
        Assert.AreEqual(14, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_FullPipeline_BaseEquipQuirkBuff()
    {
        // 装备：+5 flat
        unit.equippedWeapon = new Weapon
        {
            id = "w",
            mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 5, isPercent = false } }
        };
        // Quirk：+20% (isPercent 用 amount*0.01f)
        unit.quirks.Add(new Quirk
        {
            id = "q",
            effects = new List<QuirkEffect>
            {
                new QuirkEffect { stat = StatType.STR, amount = 20, isPercent = true }
            }
        });
        // Buff：+3 Add
        unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 3f, "buff"));

        // additive = 10 + 5(weapon) + 0(quirk flat) + 3(buff) = 18
        // multiplier = 1 + 0(equip pct) + 0.2(quirk pct) + 0(buff pct) = 1.2
        // round(18 * 1.2) = round(21.6) = 22
        Assert.AreEqual(22, unit.GetFinalStat(StatType.STR));
    }

    [Test]
    public void GetFinalStat_MaxHP_UsesVitTimesFive()
    {
        unit.primaryAttributes = new PrimaryAttributes(20, 10, 5, 3);
        // MaxHP 公式：VIT*5 + (classData?.baseHp ?? GetClassBaseHP()) = 20*5 + 0 = 100
        Assert.AreEqual(100, unit.GetFinalStat(StatType.MaxHP));
    }

    // ---------- P0 修复：SPD/ACC/DOD/CRT 不再恒为 0 ----------

    /// <summary>
    /// 验证 P0 致命 Bug 修复：未挂 classData 时 SPD/ACC/DOD/CRT 仍由 AGI 派生。
    /// 公式（与 DerivedAttributes 一致）：
    ///   SPD = AGI*2
    ///   ACC = 5 + AGI*5
    ///   DOD = AGI*2
    ///   CRT = round(AGI*1.5)
    /// primary = (10, 10, 5, 3) → AGI=5
    /// </summary>
    [Test]
    public void DerivedAttributes_WithoutClass_AgiDrivesAll()
    {
        Assert.AreEqual(10, unit.GetFinalStat(StatType.SPD), "SPD = AGI*2");
        Assert.AreEqual(30, unit.GetFinalStat(StatType.ACC), "ACC = 5 + AGI*5");
        Assert.AreEqual(10, unit.GetFinalStat(StatType.DOD), "DOD = AGI*2");
        Assert.AreEqual(8, unit.GetFinalStat(StatType.CRT),  "CRT = round(AGI*1.5)");
    }

    /// <summary>
    /// 验证 classBase 字段被正确叠加：装上 ClassDefinition 后基础值会加上职业 baseSPD/baseACC/baseDOD/baseCRT。
    /// </summary>
    [Test]
    public void DerivedAttributes_WithClass_AddsClassBase()
    {
        var classDef = ScriptableObject.CreateInstance<ClassDefinition>();
        spawnedAssets.Add(classDef);
        classDef.baseSPD = 3;
        classDef.baseACC = 10;
        classDef.baseDOD = 5;
        classDef.baseCRT = 2;
        unit.classData = classDef;

        // SPD = 5*2 + 3  = 13
        // ACC = 5 + 5*5 + 10 = 40
        // DOD = 5*2 + 5  = 15
        // CRT = round(5*1.5) + 2 = 8 + 2 = 10
        Assert.AreEqual(13, unit.GetFinalStat(StatType.SPD));
        Assert.AreEqual(40, unit.GetFinalStat(StatType.ACC));
        Assert.AreEqual(15, unit.GetFinalStat(StatType.DOD));
        Assert.AreEqual(10, unit.GetFinalStat(StatType.CRT));
    }

    /// <summary>
    /// 验证 ACC modifier 走完整管线：基础 ACC + Add modifier + Mul modifier。
    /// </summary>
    [Test]
    public void DerivedAttributes_FullPipeline_ACC_BuffsStack()
    {
        unit.modifiers.Add(new AttributeModifier(ModifierType.Add, AttributeTarget.ACC, 5f, "buff_add"));
        unit.modifiers.Add(new AttributeModifier(ModifierType.Mul, AttributeTarget.ACC, 0.2f, "buff_mul"));
        // additive  = 30 (base) + 5 (buff add) = 35
        // multiplier = 1 + 0.2 = 1.2
        // round(35 * 1.2) = 42
        Assert.AreEqual(42, unit.GetFinalStat(StatType.ACC));
    }

    /// <summary>
    /// 验证高 AGI 时 4 个属性都会同步放大（敏捷型职业存在的核心价值）。
    /// 之前 SPD=ACC=DOD=CRT=0 时，敏捷完全没意义。
    /// </summary>
    [Test]
    public void DerivedAttributes_HighAgi_AllStatsScale()
    {
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 20, 3);
        // SPD = 20*2  = 40
        // ACC = 5 + 20*5 = 105
        // DOD = 20*2  = 40
        // CRT = round(20*1.5) = 30
        Assert.AreEqual(40,  unit.GetFinalStat(StatType.SPD));
        Assert.AreEqual(105, unit.GetFinalStat(StatType.ACC));
        Assert.AreEqual(40,  unit.GetFinalStat(StatType.DOD));
        Assert.AreEqual(30,  unit.GetFinalStat(StatType.CRT));
    }
}
