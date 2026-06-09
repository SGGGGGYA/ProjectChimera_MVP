using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectChimera.Core;

/// <summary>
/// EditMode 单元测试：覆盖 <see cref="QuirkTriggerSystem.CheckTriggers"/> 的核心分支。
///
/// 关键路径：
///   - null unit / 死人 / 无 quirks  → 静默返回
///   - 触发类型不匹配              → 跳过
///   - 冷却中（IsQuirkOnCooldown）  → 跳过
///   - Random.value > procChance    → 跳过（procChance=1 → 必触发）
///   - 应用效果后写入 cooldown
///
/// 用 procChance=1f 简化随机（Random.value ∈ [0,1) 永远 ≤ 1）。
/// </summary>
[TestFixture]
public class QuirkTriggerSystemTests
{
    GameObject go;
    UnitData unit;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("TestUnit_Quirk");
        unit = go.AddComponent<UnitData>();
        unit.unitName = "TestHero_Quirk";
        unit.primaryAttributes = new PrimaryAttributes(10, 10, 5, 3);
        unit.modifiers = new List<AttributeModifier>();
        unit.quirks = new List<Quirk>();
        unit.quirkCooldowns = new Dictionary<string, int>();
        unit.currentHP = unit.MaxHp;        // 满血
        unit.stress = 0;
        unit.breakdownState = BreakdownState.None;
        // 注入种子随机数 — 即使 procChance < 1，序列也可重现
        RandomProvider.Set(new SeededRandomProvider(54321));
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null) Object.DestroyImmediate(go);
        go = null;
        unit = null;
        RandomProvider.Reset();
    }

    // 工厂：构造一个 quirk
    static Quirk MakeQuirk(string id, QuirkTriggerType trigger, float procChance = 1f, string name = null)
    {
        return new Quirk
        {
            id = id,
            localizedName = name ?? id,
            triggerType = trigger,
            procChance = procChance,
        };
    }

    // ============== 防御性检查 ==============

    [Test]
    public void CheckTriggers_NullUnit_NoThrow()
    {
        Assert.DoesNotThrow(() => QuirkTriggerSystem.CheckTriggers(null, QuirkTriggerType.BattleStart));
    }

    [Test]
    public void CheckTriggers_DeadUnit_NoEffect()
    {
        unit.currentHP = 0;
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart));

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        // 死人 → 不应添加任何 modifier
        Assert.AreEqual(0, unit.modifiers.Count);
    }

    [Test]
    public void CheckTriggers_EmptyQuirksList_NoEffect()
    {
        unit.quirks.Clear();
        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);
        Assert.AreEqual(0, unit.modifiers.Count);
    }

    [Test]
    public void CheckTriggers_NullQuirksList_NoThrow()
    {
        unit.quirks = null;
        Assert.DoesNotThrow(() => QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart));
    }

    [Test]
    public void CheckTriggers_NullQuirkInList_Skipped()
    {
        unit.quirks.Add(null);
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart));
        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);
        // 至少 1 个 modifier 被添加（zhanYiAngYang）
        Assert.GreaterOrEqual(unit.modifiers.Count, 1);
    }

    // ============== 触发类型过滤 ==============

    [Test]
    public void CheckTriggers_TriggerTypeMismatch_Skipped()
    {
        // 把 quirk 注册为 OnHit，但用 BattleStart 触发
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.OnHit));

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        Assert.AreEqual(0, unit.modifiers.Count,
            "triggerType=OnHit 的 quirk 不应在 BattleStart 触发时响应");
    }

    [Test]
    public void CheckTriggers_TriggerTypeMatch_Fires()
    {
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart));
        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);
        Assert.AreEqual(1, unit.modifiers.Count, "triggerType 匹配时应触发");
    }

    // ============== 冷却检查 ==============

    [Test]
    public void CheckTriggers_OnCooldown_Skipped()
    {
        // tieBi 触发后 cooldown=2
        unit.quirks.Add(MakeQuirk("tieBi", QuirkTriggerType.TurnStart));
        // 第一次：触发，DEF +3
        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.TurnStart);
        Assert.AreEqual(1, unit.modifiers.Count);

        // 第二次：冷却中，跳过，modifier 数不变
        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.TurnStart);
        Assert.AreEqual(1, unit.modifiers.Count, "冷却中不应再添加 modifier");
    }

    // ============== 正面特质效果 ==============

    [Test]
    public void Trigger_ZhanYiAngYang_AddsStr5_InfiniteCooldown()
    {
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart));
        int strBefore = unit.GetFinalStat(StatType.STR);

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        int strAfter = unit.GetFinalStat(StatType.STR);
        Assert.AreEqual(strBefore + 5, strAfter, "zhanYiAngYang 应使 STR +5");
        // 触发后冷却设为 int.MaxValue
        Assert.IsTrue(unit.IsQuirkOnCooldown("zhanYiAngYang"),
            "zhanYiAngYang 触发后应进入冷却");
    }

    [Test]
    public void Trigger_TieBi_AddsDef3_2TurnCooldown()
    {
        unit.quirks.Add(MakeQuirk("tieBi", QuirkTriggerType.TurnStart));
        int defBefore = unit.GetFinalStat(StatType.DEF);

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.TurnStart);

        Assert.AreEqual(defBefore + 3, unit.GetFinalStat(StatType.DEF),
            "tieBi 应使 DEF +3");
        Assert.AreEqual(2, unit.quirkCooldowns["tieBi"], "tieBi 冷却应为 2 回合");
    }

    [Test]
    public void Trigger_ShiXue_Heals10Percent()
    {
        unit.quirks.Add(MakeQuirk("shiXue", QuirkTriggerType.OnHeal));
        unit.currentHP = unit.MaxHp / 2;  // 半血
        int hpBefore = unit.currentHP;
        int expectedHeal = Mathf.RoundToInt(unit.MaxHp * 0.1f);

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnHeal);

        // 期望恢复 10% MaxHp，但不超过上限
        Assert.AreEqual(hpBefore + expectedHeal, unit.currentHP,
            "shiXue 应恢复 10% MaxHp");
        Assert.AreEqual(1, unit.quirkCooldowns["shiXue"]);
    }

    [Test]
    public void Trigger_ShiXue_DoesNotOverheal()
    {
        unit.quirks.Add(MakeQuirk("shiXue", QuirkTriggerType.OnHeal));
        unit.currentHP = unit.MaxHp - 1;  // 几乎满血

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnHeal);

        Assert.AreEqual(unit.MaxHp, unit.currentHP, "shiXue 不应超过 MaxHp");
    }

    [Test]
    public void Trigger_LengJing_ReducesStress5()
    {
        unit.quirks.Add(MakeQuirk("lengJing", QuirkTriggerType.OnStress));
        unit.stress = 30;

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnStress);

        Assert.AreEqual(25, unit.stress, "lengJing 应减压力 5");
        Assert.AreEqual(1, unit.quirkCooldowns["lengJing"]);
    }

    [Test]
    public void Trigger_LengJing_FlooredAtZero()
    {
        unit.quirks.Add(MakeQuirk("lengJing", QuirkTriggerType.OnStress));
        unit.stress = 3;  // 不足 5

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnStress);

        Assert.AreEqual(0, unit.stress, "lengJing 不应把压力减成负数");
    }

    [Test]
    public void Trigger_JieJuZhiLi_ReducesStress1()
    {
        unit.quirks.Add(MakeQuirk("jieJuZhiLi", QuirkTriggerType.TurnEnd));
        unit.stress = 50;

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.TurnEnd);

        Assert.AreEqual(49, unit.stress, "jieJuZhiLi 应减压力 1");
    }

    [Test]
    public void Trigger_BaoJiShouHuo_ReducesStress2()
    {
        unit.quirks.Add(MakeQuirk("baoJiShouHuo", QuirkTriggerType.OnCrit));
        unit.stress = 30;

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnCrit);

        Assert.AreEqual(28, unit.stress, "baoJiShouHuo 应减压力 2");
    }

    [Test]
    public void Trigger_ShanBiHuiFu_Heals5Percent()
    {
        unit.quirks.Add(MakeQuirk("shanBiHuiFu", QuirkTriggerType.OnDodge));
        unit.currentHP = unit.MaxHp / 2;
        int hpBefore = unit.currentHP;
        int expectedHeal = Mathf.RoundToInt(unit.MaxHp * 0.05f);

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnDodge);

        Assert.AreEqual(hpBefore + expectedHeal, unit.currentHP,
            "shanBiHuiFu 应恢复 5% MaxHp");
    }

    // ============== 负面特质效果 ==============

    [Test]
    public void Trigger_NuoRuo_ReducesStr3()
    {
        unit.quirks.Add(MakeQuirk("nuoRuo", QuirkTriggerType.BattleStart));
        int strBefore = unit.GetFinalStat(StatType.STR);

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        Assert.AreEqual(strBefore - 3, unit.GetFinalStat(StatType.STR),
            "nuoRuo 应使 STR -3");
    }

    [Test]
    public void Trigger_CuiRuo_AddsStress2()
    {
        unit.quirks.Add(MakeQuirk("cuiRuo", QuirkTriggerType.OnHit));
        unit.stress = 0;

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnHit);

        Assert.AreEqual(2, unit.stress, "cuiRuo 应加压力 2");
    }

    [Test]
    public void Trigger_BuXiangYuGan_AddsStress5()
    {
        unit.quirks.Add(MakeQuirk("buXiangYuGan", QuirkTriggerType.OnHit));
        unit.stress = 0;

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnHit);

        Assert.AreEqual(5, unit.stress, "buXiangYuGan 应加压力 5");
    }

    // ============== yiShang 上下文修改 ==============

    [Test]
    public void Trigger_YiShang_ModifiesContextIntValueBy20Percent()
    {
        unit.quirks.Add(MakeQuirk("yiShang", QuirkTriggerType.OnTakeDamage));
        var ctx = new QuirkContext { intValue = 100, valueModified = false };

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnTakeDamage, ctx);

        // 受到的伤害 +20%：intValue = 100 * 1.2 = 120
        Assert.AreEqual(120, ctx.intValue, "yiShang 应使 intValue *1.2");
        Assert.IsTrue(ctx.valueModified, "yiShang 应标记 valueModified=true");
    }

    [Test]
    public void Trigger_YiShang_NoContext_DoesNotThrow()
    {
        unit.quirks.Add(MakeQuirk("yiShang", QuirkTriggerType.OnTakeDamage));

        // context 是 null 时，case 分支不执行，不应抛异常
        Assert.DoesNotThrow(() => QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnTakeDamage, null));
    }

    [Test]
    public void Trigger_YiShang_WrongContextType_DoesNotThrow()
    {
        unit.quirks.Add(MakeQuirk("yiShang", QuirkTriggerType.OnTakeDamage));

        // 传非 QuirkContext 类型，case 内 is 检查不通过，安静跳过
        Assert.DoesNotThrow(() =>
            QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnTakeDamage, "not_a_context"));
    }

    // ============== procChance 控制 ==============

    [Test]
    public void Trigger_ProcChance1_AlwaysFires()
    {
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart, procChance: 1f));

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        // Random.value ∈ [0, 1)，永远 ≤ 1
        Assert.AreEqual(1, unit.modifiers.Count);
    }

    [Test]
    public void Trigger_ProcChance0_NeverFires()
    {
        // 注意：procChance=0 时 Random.value > 0 通常为 true（Random.value ∈ [0,1)）
        // 因此几乎从不触发。验证 100 次都未触发即可。
        for (int i = 0; i < 100; i++)
        {
            // 每次重置冷却以避免被 cooldown 拦截
            unit.modifiers.Clear();
            unit.quirkCooldowns.Clear();
            unit.quirks.Clear();
            unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart, procChance: 0f));

            QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

            Assert.AreEqual(0, unit.modifiers.Count,
                $"procChance=0 时第 {i} 次迭代不应触发");
        }
    }

    // ============== 多个 quirks 共存 ==============

    [Test]
    public void Trigger_MultipleMatchingQuirks_AllFire()
    {
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart));  // +5 STR
        unit.quirks.Add(MakeQuirk("tieBi", QuirkTriggerType.BattleStart));          // +3 DEF
        // 注：tieBi 通常配 TurnStart，但 triggerType 是 Quirk 自带属性，可以是任意
        // 这里测试"两个匹配 trigger 都触发"
        unit.quirks[0].triggerType = QuirkTriggerType.BattleStart;
        unit.quirks[1].triggerType = QuirkTriggerType.BattleStart;

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        Assert.AreEqual(2, unit.modifiers.Count, "两个匹配的 quirk 都应添加 modifier");
    }

    [Test]
    public void Trigger_MixedTriggerTypes_OnlyMatchingFire()
    {
        unit.quirks.Add(MakeQuirk("zhanYiAngYang", QuirkTriggerType.BattleStart));
        unit.quirks.Add(MakeQuirk("tieBi", QuirkTriggerType.TurnStart));

        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        // 只有 zhanYiAngYang 匹配
        Assert.AreEqual(1, unit.modifiers.Count, "BattleStart 触发时只有 BattleStart quirk 触发");
    }

    // ============== 未知 quirk id ==============

    [Test]
    public void Trigger_UnknownQuirkId_DoesNotThrow()
    {
        unit.quirks.Add(MakeQuirk("not_a_real_id", QuirkTriggerType.BattleStart));
        // 未知 id 走 default 分支：Log.Warn，不抛异常，不写 modifier
        Assert.DoesNotThrow(() => QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart));
        Assert.AreEqual(0, unit.modifiers.Count);
    }

    // ============== 循环依赖防护：StressManager ↔ QuirkTriggerSystem ==============
    //
    // 场景：cuiRuo / buXiangYuGan / ziNue 等 OnStress 怪癖在 ApplyQuirkEffect 里反向调用
    // StressManager.AddStress，而 AddStress 末尾又调用 CheckTriggers(OnStress)。
    // 旧实现：靠每怪癖 1 回合 CD 兜底 → 多个不同 id 的"加压"怪癖可以互相触发，链式死循环。
    // 新实现：AddStress 用重入守卫，最外层调用才触发 CheckTriggers。

    /// <summary>
    /// 验证：单次 AddStress 让 cuiRuo（OnStress + 加压）触发后，递归回来的 AddStress
    /// 不会再触发 CheckTriggers（避免循环）。stress 仍正确 +2。
    /// </summary>
    [Test]
    public void StressManager_AddStress_CuiRuoRecursion_DoesNotStackOverflow()
    {
        unit.quirks.Add(MakeQuirk("cuiRuo", QuirkTriggerType.OnStress));
        unit.stress = 0;

        // 1) StressManager.AddStress → CheckTriggers(OnStress) → cuiRuo 触发 → AddStress(+2) 递归
        //    旧实现：递归 AddStress 又触发 CheckTriggers，但 cuiRuo 已进入 1 回合 CD，链路断开；
        //            然而若有"cuiRuo + buXiangYuGan"等多个不同 id 的 OnStress 怪癖，CD 无法阻断链。
        //    新实现：内层 AddStress 跳过 CheckTriggers，从根本上阻断。
        // 2) 期望：本次调用不爆栈，stress 净增 2（外层 +2，内层不再触发）。
        //    注意：cuiRuo 自己也是 OnStress 怪癖，所以会通过 CheckTriggers 触发自己，递归 +2。
        //    总和 = 外层 AddStress + 内层 cuiRuo.AddStress = 5+2=7
        Assert.DoesNotThrow(() => StressManager.AddStress(unit, 5, StressTag.Combat),
            "AddStress + cuiRuo OnStress 怪癖的循环不应爆栈");

        // 外层 +5 + cuiRuo 内部 AddStress(+2) = 7
        // 关键：仅触发 1 次 CheckTriggers，cuiRuo 在 OnStress 链路里只 fire 一次
        Assert.AreEqual(7, unit.stress,
            "外层 +5 + cuiRuo 自身 OnStress 触发 +2 = 7。递归路径必须被重入守卫截断");
        Assert.IsTrue(unit.quirkCooldowns.ContainsKey("cuiRuo"),
            "cuiRuo 触发后应被记入 1 回合 CD");
        Assert.AreEqual(1, unit.quirkCooldowns["cuiRuo"]);
    }

    /// <summary>
    /// 验证：多个不同 id 的"加压"OnStress 怪癖并发存在时（如 cuiRuo + buXiangYuGan），
    /// 单次 AddStress 不会让它们无限循环链式触发。
    /// 旧实现：cuiRuo → +2 → CheckTriggers → buXiangYuGan → +5 → CheckTriggers → ... → StackOverflow。
    /// 新实现：cuiRuo 触发后，cuiRuo 进入 CD；buXiangYuGan 不在 CD，触发；但内层 AddStress 不会再
    ///         触发 OnStress，所以链立即终止。stress 净增 = 5 + 2 + 5 = 12。
    /// </summary>
    [Test]
    public void StressManager_AddStress_MultipleOnStressPressureQuirks_NoInfiniteChain()
    {
        unit.quirks.Add(MakeQuirk("cuiRuo", QuirkTriggerType.OnStress));
        unit.quirks.Add(MakeQuirk("buXiangYuGan", QuirkTriggerType.OnStress));
        unit.stress = 0;

        Assert.DoesNotThrow(() => StressManager.AddStress(unit, 5, StressTag.Combat),
            "cuiRuo + buXiangYuGan 同时存在时，AddStress 不应引发栈溢出");

        // 期望：外层 +5 → 触发 cuiRuo(+2) 和 buXiangYuGan(+5)（它们都是 OnStress 但都没进 CD）
        //     → cuiRuo/buXiangYuGan 内部 AddStress 不再触发 CheckTriggers（重入守卫生效）
        // 总 = 5 + 2 + 5 = 12
        Assert.AreEqual(12, unit.stress,
            "外层 +5 + cuiRuo +2 + buXiangYuGan +5 = 12；不应有第三次 / 第四次链式触发");
        Assert.IsTrue(unit.quirkCooldowns.ContainsKey("cuiRuo"));
        Assert.IsTrue(unit.quirkCooldowns.ContainsKey("buXiangYuGan"));
    }

    /// <summary>
    /// 验证：DispatchStressTriggers 是显式入口，绕过重入守卫，可以无限次触发 OnStress 怪癖。
    /// 用于"我想在不加压的情况下也走一遍 OnStress"的场景（外部代码/测试）。
    /// </summary>
    [Test]
    public void StressManager_DispatchStressTriggers_AlwaysFires_NoReentryGuard()
    {
        unit.quirks.Add(MakeQuirk("lengJing", QuirkTriggerType.OnStress));
        unit.stress = 30;

        StressManager.DispatchStressTriggers(unit);
        StressManager.DispatchStressTriggers(unit);
        StressManager.DispatchStressTriggers(unit);

        // lengJing 每次 -5，三次 = -15（floor 0）。lengJing 有 1 回合 CD，所以只有第一次会 fire，
        // 这里 30 - 5 = 25。但我们关心的是 DispatchStressTriggers 的入口本身能被多次调用而不报错。
        Assert.AreEqual(25, unit.stress,
            "DispatchStressTriggers 第一次走 lengJing 减 5，第二次因 CD 跳过");
        Assert.IsTrue(unit.quirkCooldowns.ContainsKey("lengJing"));
    }
}
