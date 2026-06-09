using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 StressManager 的核心静态方法。
/// 阈值参考 StressConfig：
///   - maxStress = 200  (UnitData.MaxStressConst)
///   - virtueBreakMin = 100
///   - heartAttackThreshold = 200
///   - afflictionResetValue = 100
/// BreakdownEntry.statModPercent（从 BreakdownDatabase 查表）：
///   - fearful(Affliction)  = -0.20
///   - courageous(Virtue)    = +0.25
///   - paranoid(Affliction)  =  0.00
/// </summary>
[TestFixture]
public class StressManagerTests
{
    GameObject testGO;
    GameObject bmGO;
    UnitData unit;
    BattleManager bm;

    [SetUp]
    public void SetUp()
    {
        // 主机：UnitData 必须是 MonoBehaviour
        testGO = new GameObject("TestUnit_Stress");
        unit = testGO.AddComponent<UnitData>();
        unit.unitName = "TestHero_Stress";
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        unit.currentHP = 50;          // MaxHp 推导：VIT*5 = 50
        unit.stress = 0;
        unit.stressResistRate = 0f;   // 不抗压
        unit.breakdownState = BreakdownState.None;
        unit.currentBreakdownId = null;
        unit.breakdownCooldown = 0;
        unit.stressEventHistory = new List<StressEvent>();
        unit.positiveQuirkCount = 0;
        unit.modifiers = new List<AttributeModifier>();
        unit.quirks = new List<Quirk>();

        // BattleManager 供 CheckResolve 完整路径使用
        bmGO = new GameObject("TestBM");
        bm = bmGO.AddComponent<BattleManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (testGO != null) Object.DestroyImmediate(testGO);
        if (bmGO != null) Object.DestroyImmediate(bmGO);
        testGO = null;
        bmGO = null;
        unit = null;
        bm = null;
    }

    // ---------- 1. AddStress 累加 ----------
    [Test]
    public void AddStress_Basic_Accumulates()
    {
        unit.stress = 0;
        StressManager.AddStress(unit, 10, StressTag.Combat);
        Assert.AreEqual(10, unit.stress);
    }

    [Test]
    public void AddStress_MultipleCalls_Accumulate()
    {
        unit.stress = 0;
        StressManager.AddStress(unit, 30, StressTag.Combat);
        StressManager.AddStress(unit, 25, StressTag.Darkness);
        StressManager.AddStress(unit, 15, StressTag.Blood);
        Assert.AreEqual(70, unit.stress);
    }

    // ---------- 2. ReduceStress 减少 + 下限 0 ----------
    [Test]
    public void ReduceStress_Basic_Decreases()
    {
        unit.stress = 20;
        StressManager.ReduceStress(unit, 5);
        Assert.AreEqual(15, unit.stress);
    }

    [Test]
    public void ReduceStress_BelowZero_FlooredAtZero()
    {
        unit.stress = 5;
        StressManager.ReduceStress(unit, 20);
        Assert.AreEqual(0, unit.stress);
    }

    // ---------- 3. 到达 virtueBreakMin(100) 触发 Breakdown ----------
    [Test]
    public void CheckResolve_AtVirtueMin_TriggersBreakdown()
    {
        Random.InitState(42);
        unit.stress = 100;
        unit.breakdownState = BreakdownState.None;
        StressManager.CheckResolve(unit, null); // null bm 仍会进入 TryResolveBreakdown
        Assert.AreNotEqual(BreakdownState.None, unit.breakdownState);
        Assert.IsTrue(unit.breakdownState == BreakdownState.Virtue
                   || unit.breakdownState == BreakdownState.Affliction);
    }

    [Test]
    public void CheckResolve_AfflictionEntry_ResetsStressTo100()
    {
        // 强制正特质为 0 + 低抗性，让随机倾向 Affliction。
        // 多次种子重试，找到产生 Affliction 的种子。
        BreakdownState picked = BreakdownState.None;
        int seed = 0;
        for (; seed < 200; seed++)
        {
            unit.stress = 100;
            unit.breakdownState = BreakdownState.None;
            unit.breakdownCooldown = 0;
            unit.positiveQuirkCount = 0;
            unit.stressResistRate = 0f;
            Random.InitState(seed);
            StressManager.CheckResolve(unit, null);
            if (unit.breakdownState == BreakdownState.Affliction)
            {
                picked = unit.breakdownState;
                break;
            }
        }
        Assert.AreEqual(BreakdownState.Affliction, picked,
            "Expected at least one seed to pick Affliction; checked 200 seeds.");
        // Affliction 路径会重置压力到 afflictionResetValue = 100
        Assert.AreEqual(100, unit.stress);
    }

    // ---------- 4. Virtue 状态：种随机可触发 ----------
    [Test]
    public void CheckResolve_VirtueEntry_ReachableWithPositiveQuirks()
    {
        BreakdownState picked = BreakdownState.None;
        int seed = 0;
        for (; seed < 500; seed++)
        {
            unit.stress = 100;
            unit.breakdownState = BreakdownState.None;
            unit.breakdownCooldown = 0;
            unit.positiveQuirkCount = 5; // +25% virtue 概率
            unit.stressResistRate = 0.5f;
            Random.InitState(seed);
            StressManager.CheckResolve(unit, null);
            if (unit.breakdownState == BreakdownState.Virtue)
            {
                picked = unit.breakdownState;
                break;
            }
        }
        Assert.AreEqual(BreakdownState.Virtue, picked,
            "Expected at least one seed to pick Virtue with buffs.");
    }

    // ---------- 5. 压力上限 200 (MaxStressConst) ----------
    [Test]
    public void AddStress_CapsAtMaxStressConst()
    {
        unit.stress = 180;
        StressManager.AddStress(unit, 100, StressTag.Combat);
        Assert.AreEqual(UnitData.MaxStressConst, unit.stress);
        Assert.AreEqual(200, UnitData.MaxStressConst); // 防止常数值漂移
    }

    [Test]
    public void AddStress_AtMax_OverflowsAbove()
    {
        unit.stress = 200;
        StressManager.AddStress(unit, 50, StressTag.Combat);
        Assert.AreEqual(200, unit.stress);
    }

    // ---------- 6. GetStatModifier(None) = 1f ----------
    [Test]
    public void GetStatModifier_None_Returns1()
    {
        unit.breakdownState = BreakdownState.None;
        Assert.AreEqual(1f, StressManager.GetStatModifier(unit));
    }

    // ---------- 7. GetStatModifier(Affliction, 有 statMod) < 1f ----------
    [Test]
    public void GetStatModifier_Affliction_Fearful_ReturnsBelow1()
    {
        BreakdownDatabase.Initialize();
        unit.breakdownState = BreakdownState.Affliction;
        unit.currentBreakdownId = "fearful"; // statModPercent = -0.2
        Assert.AreEqual(0.8f, StressManager.GetStatModifier(unit), 0.001f);
    }

    // ---------- 8. GetStatModifier(Virtue, 有 statMod) > 1f ----------
    [Test]
    public void GetStatModifier_Virtue_Courageous_ReturnsAbove1()
    {
        BreakdownDatabase.Initialize();
        unit.breakdownState = BreakdownState.Virtue;
        unit.currentBreakdownId = "courageous"; // statModPercent = +0.25
        Assert.AreEqual(1.25f, StressManager.GetStatModifier(unit), 0.001f);
    }

    [Test]
    public void GetStatModifier_Affliction_NoStatMod_Returns1()
    {
        BreakdownDatabase.Initialize();
        unit.breakdownState = BreakdownState.Affliction;
        unit.currentBreakdownId = "paranoid"; // statModPercent = 0
        Assert.AreEqual(1f, StressManager.GetStatModifier(unit));
    }

    [Test]
    public void GetStatModifier_UnknownEntryId_Returns1()
    {
        BreakdownDatabase.Initialize();
        unit.breakdownState = BreakdownState.Affliction;
        unit.currentBreakdownId = "does_not_exist";
        Assert.AreEqual(1f, StressManager.GetStatModifier(unit));
    }

    [Test]
    public void GetStatModifier_HeartAttack_ReturnsHalf()
    {
        unit.breakdownState = BreakdownState.HeartAttack;
        Assert.AreEqual(0.5f, StressManager.GetStatModifier(unit));
    }

    // ---------- 9. CheckResolve 在 heartAttackThreshold(200) 触发 HeartAttack 路径 ----------
    [Test]
    public void CheckResolve_AtHeartAttack_ChangesState()
    {
        // stress=200 必然进入 CheckHeartAttack 分支
        // 该函数在 bm==null 时直接 return（安全护栏），所以这里用真实 bm
        unit.stress = 200;
        unit.currentHP = unit.MaxHp; // 满血，便于观察伤害
        int hpBefore = unit.currentHP;
        int stressBefore = unit.stress;

        StressManager.CheckResolve(unit, bm);

        // 状态必发生变化：要么抵抗（stress 降低到 100），要么受伤（HP 减少）
        bool stressChanged = unit.stress != stressBefore;
        bool hpChanged = unit.currentHP != hpBefore;
        Assert.IsTrue(stressChanged || hpChanged,
            "HeartAttack path should either reset stress or deal damage.");
    }

    [Test]
    public void CheckResolve_BelowHeartAttack_NoHeartAttackPath()
    {
        // stress < 200 不会进 CheckHeartAttack
        unit.stress = 199;
        int hpBefore = unit.currentHP;
        StressManager.CheckResolve(unit, null); // null bm 也安全
        Assert.AreEqual(199, unit.stress);
        Assert.AreEqual(hpBefore, unit.currentHP);
    }

    [Test]
    public void CheckResolve_DeadUnit_NoEffect()
    {
        unit.stress = 150;
        unit.currentHP = 0;
        StressManager.CheckResolve(unit, bm);
        Assert.AreEqual(BreakdownState.None, unit.breakdownState);
        Assert.AreEqual(150, unit.stress);
    }

    [Test]
    public void CheckResolve_AlreadyInBreakdown_NoTrigger()
    {
        // TryResolveBreakdown 要求 breakdownState == None 才进入
        unit.stress = 100;
        unit.breakdownState = BreakdownState.Affliction;
        unit.currentBreakdownId = "paranoid";
        StressManager.CheckResolve(unit, null);
        Assert.AreEqual(BreakdownState.Affliction, unit.breakdownState);
        Assert.AreEqual("paranoid", unit.currentBreakdownId);
    }

    [Test]
    public void CheckResolve_BelowVirtueBreakMin_NoTrigger()
    {
        unit.stress = 99;
        unit.breakdownState = BreakdownState.None;
        StressManager.CheckResolve(unit, null);
        Assert.AreEqual(BreakdownState.None, unit.breakdownState);
    }

    // ---------- 额外覆盖：抗性、免疫力、死人不掉压力 ----------
    [Test]
    public void AddStress_ResistReducesActualGain()
    {
        // 50% 抗性 → 实际获得 round(10 * 0.5) = 5
        unit.stressResistRate = 0.5f;
        unit.stress = 0;
        StressManager.AddStress(unit, 10, StressTag.Combat);
        Assert.AreEqual(5, unit.stress);
    }

    [Test]
    public void AddStress_HighResist_FloorsActualAt1()
    {
        // 99% 抗性 → 实际获得 max(1, round(10*0.01)) = max(1, 0) = 1
        unit.stressResistRate = 0.99f;
        unit.stress = 0;
        StressManager.AddStress(unit, 10, StressTag.Combat);
        Assert.AreEqual(1, unit.stress);
    }

    [Test]
    public void AddStress_DeadUnit_NoChange()
    {
        unit.currentHP = 0;
        unit.stress = 30;
        StressManager.AddStress(unit, 10, StressTag.Combat);
        Assert.AreEqual(30, unit.stress);
    }

    [Test]
    public void AddStress_VirtueImmune_NoChange()
    {
        // StressConfig.virtueStressImmunity 默认 true
        unit.breakdownState = BreakdownState.Virtue;
        unit.stress = 30;
        StressManager.AddStress(unit, 10, StressTag.Combat);
        Assert.AreEqual(30, unit.stress);
    }

    [Test]
    public void AddStress_AppendsToEventHistory_Max5()
    {
        for (int i = 0; i < 7; i++)
            StressManager.AddStress(unit, 1, StressTag.Combat);
        Assert.AreEqual(5, unit.stressEventHistory.Count);
    }
}
