using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="CombatSystem.CalculateDamage"/> 的核心公式分支。
/// 公式回顾：
///   baseVal    = skill.baseDamage + attacker.weaponAttack
///   scaling    = attacker.GetEffectiveSTR() * skill.strScaling + attacker.AGI * skill.agiScaling
///   raw        = (baseVal + scaling) * Random.Range(0.85, 1.15)
///   defReduction = DEF / (1 + DEF * 0.02)  (暴击时 *= 1 - CRIT_DEF_IGNORE)
///   raw        = (raw - defReduction) * target.GetIncomingDamageMultiplier() * attacker.GetDamageModifier()
///   return     = Mathf.Max(1, RoundToInt(raw))
/// </summary>
[TestFixture]
public class CombatDamageTests
{
    // 跟踪本测试创建的所有 GameObject，在 TearDown 统一清理，避免 EditMode 下场景污染。
    readonly List<GameObject> spawned = new List<GameObject>();
    readonly List<Object> spawnedAssets = new List<Object>();

    [TearDown]
    public void Cleanup()
    {
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();

        foreach (var asset in spawnedAssets)
            if (asset != null) Object.DestroyImmediate(asset);
        spawnedAssets.Clear();
    }

    // -------- 工厂方法 --------

    /// <summary>创建一个测试用 UnitData。baseDef>0 时挂一个 ClassDefinition 让 DEF_Effective 生效。</summary>
    UnitData CreateUnit(int str = 10, int agi = 5, int weaponAttack = 0, int baseDef = 0)
    {
        var go = new GameObject("TestUnit");
        spawned.Add(go);
        var unit = go.AddComponent<UnitData>();
        unit.primaryAttributes = new PrimaryAttributes(10, str, agi, 3);
        unit.weaponAttack = weaponAttack;

        if (baseDef > 0)
        {
            var classDef = ScriptableObject.CreateInstance<ClassDefinition>();
            classDef.baseDEF = baseDef;
            spawnedAssets.Add(classDef);
            unit.classData = classDef;
        }
        return unit;
    }

    SkillData CreateSkill(int baseDamage = 0, float strScaling = 0f, float agiScaling = 0f)
    {
        return new SkillData
        {
            baseDamage = baseDamage,
            strScaling = strScaling,
            agiScaling = agiScaling,
        };
    }

    // -------- 测试 1: 基础伤害落入理论区间 --------

    /// <summary>
    /// baseDamage=10, weapon=5, scaling=0, no def, no crit
    /// 理论 raw = 15 * [0.85, 1.15] = [12.75, 17.25]
    /// 期望：damage 落在 [12.75, 17.25] 区间
    /// </summary>
    [Test]
    public void BasicDamage_FallsWithinExpectedRange()
    {
        var attacker = CreateUnit(str: 10, agi: 5, weaponAttack: 5);
        var target = CreateUnit(baseDef: 0);
        var skill = CreateSkill(baseDamage: 10);

        int damage = CombatSystem.CalculateDamage(attacker, target, skill, isCrit: false);

        Assert.GreaterOrEqual(damage, 12.75f,
            $"伤害 {damage} 应 >= 12.75 (理论下界 = 15*0.85)");
        Assert.LessOrEqual(damage, 17.25f,
            $"伤害 {damage} 应 <= 17.25 (理论上界 = 15*1.15)");
    }

    // -------- 测试 2: 暴击伤害明显高于非暴击 --------

    /// <summary>
    /// 设 baseDef=10 (DEF_Effective=10)。
    /// 理论（新公式 defReduction = DEF / (1 + DEF*0.02)）：
    ///   non-crit defReduction = 10 / 1.2 ≈ 8.33  → raw in [10.75, 15.25] - 8.33 = [2.42, 6.92]
    ///   crit     defReduction = 8.33 * 0.5 ≈ 4.17 → raw in [10.75, 15.25] - 4.17 = [6.58, 11.08]
    /// 单次 RNG 区间有重叠，所以用多次取平均：crit 期望值比 non-crit 高 ~4.17。
    /// 断言：50 次平均后 crit - nonCrit >= 2.0（容忍随机波动）。
    /// </summary>
    [Test]
    public void Crit_NotablyHigherThanNonCrit_OnAverage()
    {
        const int iterations = 50;
        const int baseDef = 10;
        long sumCrit = 0, sumNonCrit = 0;

        for (int i = 0; i < iterations; i++)
        {
            var attacker = CreateUnit(str: 10, agi: 5, weaponAttack: 5);
            var target = CreateUnit(baseDef: baseDef);
            var skill = CreateSkill(baseDamage: 10);

            sumCrit += CombatSystem.CalculateDamage(attacker, target, skill, isCrit: true);
            sumNonCrit += CombatSystem.CalculateDamage(attacker, target, skill, isCrit: false);
        }

        float avgCrit = (float)sumCrit / iterations;
        float avgNonCrit = (float)sumNonCrit / iterations;
        float diff = avgCrit - avgNonCrit;

        Assert.Greater(diff, 2.0f,
            $"暴击平均({avgCrit:F2}) 应比非暴击平均({avgNonCrit:F2}) 至少高 2.0 (实际差={diff:F2})");
    }

    // -------- 测试 3: 防御减伤生效 --------

    /// <summary>
    /// 同样输入，对比 def=0 vs def=10 的伤害。
    /// 理论（新公式 defReduction = DEF / (1 + DEF*0.02)）：
    ///   no-def    defReduction = 0
    ///   with-def  defReduction = 10 / 1.2 ≈ 8.33
    /// 期望 dmgNoDef - dmgWithDef 在 [7, 10] 区间（容忍四舍五入±1）。
    /// </summary>
    [Test]
    public void Defense_ReducesDamage()
    {
        var attacker = CreateUnit(str: 10, agi: 5, weaponAttack: 5);
        var targetNoDef = CreateUnit(baseDef: 0);
        var targetWithDef = CreateUnit(baseDef: 10);
        var skill = CreateSkill(baseDamage: 10);

        int dmgNoDef = CombatSystem.CalculateDamage(attacker, targetNoDef, skill, isCrit: false);
        int dmgWithDef = CombatSystem.CalculateDamage(attacker, targetWithDef, skill, isCrit: false);
        int diff = dmgNoDef - dmgWithDef;

        Assert.Greater(diff, 0,
            $"有防伤害({dmgWithDef}) 应 < 无防伤害({dmgNoDef})");
        Assert.GreaterOrEqual(diff, 7,
            $"伤害差应 >= 7 (理论=8.33, 容忍舍入), 实际差={diff} (noDef={dmgNoDef}, withDef={dmgWithDef})");
        Assert.LessOrEqual(diff, 10,
            $"伤害差应 <= 10 (理论=8.33, 容忍舍入), 实际差={diff}");
    }

    // -------- 测试 4: 受伤倍率 < 1 时伤害按比例缩小 --------

    /// <summary>
    /// 给目标挂 Mark 状态，value=-0.5 → GetIncomingDamageMultiplier() = 1 + (-0.5) = 0.5
    /// 期望：dmgDebuffed ≈ dmgNormal * 0.5
    /// </summary>
    [Test]
    public void IncomingDamageMultiplier_LessThanOne_HalvesDamage()
    {
        var attacker = CreateUnit(str: 10, agi: 5, weaponAttack: 5);
        var targetNormal = CreateUnit(baseDef: 0);
        var targetDebuffed = CreateUnit(baseDef: 0);
        // remainingTurns=5 > 0，状态未过期；value=-0.5 表示 -50% 受伤（即减半）
        targetDebuffed.statusEffects.Add(new StatusEffect(StatusType.Mark, "test", 5, -0.5f));

        var skill = CreateSkill(baseDamage: 10);

        int dmgNormal = CombatSystem.CalculateDamage(attacker, targetNormal, skill, isCrit: false);
        int dmgDebuffed = CombatSystem.CalculateDamage(attacker, targetDebuffed, skill, isCrit: false);

        Assert.Less(dmgDebuffed, dmgNormal,
            $"挂 -50% debuff 后伤害({dmgDebuffed}) 应 < 正常伤害({dmgNormal})");
        // dmgDebuffed 理论上为 dmgNormal 的一半；允许 ±1 的舍入误差
        int expectedHalfCeil = Mathf.CeilToInt(dmgNormal * 0.5f);
        Assert.LessOrEqual(dmgDebuffed, expectedHalfCeil + 1,
            $"减半伤害({dmgDebuffed}) 应 <= ceil(normal*0.5)+1 = {expectedHalfCeil + 1}");
        Assert.GreaterOrEqual(dmgDebuffed, Mathf.FloorToInt(dmgNormal * 0.5f) - 1,
            $"减半伤害({dmgDebuffed}) 应 >= floor(normal*0.5)-1 = {Mathf.FloorToInt(dmgNormal * 0.5f) - 1}");
    }

    // -------- 测试 5: baseDamage=0 不崩溃且最小伤害 ≥ 0 --------

    /// <summary>
    /// baseDamage=0, weapon=0, 极低其他加成，但目标高 DEF 让 raw 必然为负。
    /// 公式末位 Mathf.Max(1, RoundToInt(raw)) 保证结果 >= 1。
    /// 期望：调用不抛异常，伤害 >= 1（即 >= 0 一定满足）。
    /// </summary>
    [Test]
    public void ZeroBaseDamage_DoesNotCrash_MinDamageAtLeastOne()
    {
        var attacker = CreateUnit(str: 0, agi: 0, weaponAttack: 0);
        var target = CreateUnit(baseDef: 100); // 极大防御让 raw 一定 < 0
        var skill = CreateSkill(baseDamage: 0);

        // 不应抛异常
        int damage = -1;
        Assert.DoesNotThrow(() =>
        {
            damage = CombatSystem.CalculateDamage(attacker, target, skill, isCrit: false);
        }, "baseDamage=0 + 高防场景不应抛异常");

        Assert.GreaterOrEqual(damage, 0,
            $"伤害不应为负，实际={damage}");
        Assert.GreaterOrEqual(damage, 1,
            $"Mathf.Max(1, ...) 保证最小伤害为 1，实际={damage}");
    }
}
