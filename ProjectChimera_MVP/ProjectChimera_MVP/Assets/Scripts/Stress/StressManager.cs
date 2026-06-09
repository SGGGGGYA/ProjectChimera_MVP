using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;

public static class StressManager
{
    public static StressConfig config = new StressConfig();
    public static bool initialized;

    /// <summary>
    /// AddStress 重入计数器：防止 StressManager ↔ QuirkTriggerSystem 循环依赖导致栈溢出。
    /// 场景：cuiRuo / buXiangYuGan / ziNue 等 OnStress 怪癖在 ApplyQuirkEffect 里反向调用
    /// AddStress，而 AddStress 末尾又调用 CheckTriggers(OnStress)。如果 unit 同时持有多个不同
    /// id 的"加压"怪癖，单凭每怪癖 1 回合冷却（CD 兜底）无法阻断整个链：cuiRuo → +2 →
    /// CheckTriggers → buXiangYuGan → +5 → CheckTriggers → ziNue → +5 → ... 永远跑下去。
    ///
    /// 修复策略：只有最外层（首次进入）AddStress 才触发 OnStress 怪癖；内层只更新数值。
    /// 这样压力变化仍正确生效，但 OnStress 怪癖本轮不再被重复触发，避免死循环。
    /// </summary>
    static int addStressReentryDepth;

    public static void InitFromConfig(StressConfig cfg)
    {
        config = cfg;
        BreakdownDatabase.Initialize();
        initialized = true;
    }

    public static int CalcActualGain(int rawAmount, float resistRate)
    {
        float r = Mathf.Min(resistRate, config.maxResist);
        if (r > config.resistDiminishThreshold)
            r = config.resistDiminishThreshold + (r - config.resistDiminishThreshold) * 0.5f;
        int actual = Mathf.Max(1, Mathf.RoundToInt(rawAmount * (1f - r)));
        return actual;
    }

    /// <summary>
    /// 获取单位实际压力上限 = config.maxStress + 命名奖励 (stressCapBonus)
    /// 命名后默认 +20，使已命名英雄可承受更高压力。
    /// </summary>
    public static int GetStressCap(UnitData unit)
    {
        int bonus = (unit != null && unit.stressCapBonus > 0) ? unit.stressCapBonus : 0;
        return config.maxStress + bonus;
    }

    public static void AddStress(UnitData unit, int rawAmount, StressTag tag)
    {
        if (unit == null || unit.currentHP <= 0) return;
        if (unit.IsStressImmune())
        {
            BattleLog.Add($"[压力免疫] {unit.unitName} 处于美德状态，免疫压力");
            return;
        }
        int actual = CalcActualGain(rawAmount, unit.stressResistRate);
        // 命名后压力上限 = config.maxStress + unit.stressCapBonus（默认 0，命名后 +20）
        int cap = GetStressCap(unit);
        unit.stress = Mathf.Min(unit.stress + actual, cap);

        unit.stressEventHistory.Add(new StressEvent(actual, tag));
        if (unit.stressEventHistory.Count > 5)
            unit.stressEventHistory.RemoveAt(0);

        BattleLog.Add($"[压力] {unit.unitName} 压力 +{actual} (原始{rawAmount}, 抗性{unit.stressResistRate * 100:F0}%) ({unit.stress}/{GetStressCap(unit)})");

        // 重入守卫：只有最外层调用才触发 OnStress 怪癖，阻断 StressManager ↔ QuirkTriggerSystem 循环。
        // 内层调用（来自 cuiRuo 等怪癖的 ApplyQuirkEffect）只更新压力数值，不再次触发 CheckTriggers。
        bool isOutermost = (addStressReentryDepth == 0);
        addStressReentryDepth++;
        try
        {
            if (isOutermost)
                QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnStress);
        }
        finally
        {
            addStressReentryDepth--;
        }
    }

    /// <summary>
    /// 显式触发 OnStress 怪癖检查，不走 AddStress。供以下场景使用：
    ///   1. 单元测试：直接验证 OnStress 怪癖的触发逻辑
    ///   2. 外部代码在非"加压"路径上需要重检 OnStress（例如：战斗结束、回合开始时压力归零）
    /// 与 AddStress 不同：不会因重入而吞掉触发，每次调用都会真正 fire。
    /// </summary>
    public static void DispatchStressTriggers(UnitData unit)
    {
        if (unit == null) return;
        QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.OnStress);
    }

    public static void ReduceStress(UnitData unit, int amount)
    {
        if (unit == null) return;
        unit.stress = Mathf.Max(unit.stress - amount, 0);
        BattleLog.Add($"[压力] {unit.unitName} 压力 -{amount} ({unit.stress}/{GetStressCap(unit)})");
    }

    public static void CheckResolve(UnitData unit, IBattleContext context)
    {
        if (unit == null || unit.currentHP <= 0) return;

        if (unit.stress >= config.heartAttackThreshold)
        {
            CheckHeartAttack(unit, context);
        }
        else if (unit.stress >= config.virtueBreakMin && unit.breakdownState == BreakdownState.None)
        {
            TryResolveBreakdown(unit, context);
        }
    }

    static void CheckHeartAttack(UnitData unit, IBattleContext context)
    {
        if (unit == null || context == null) return;

        float roll = RandomProvider.Current.Value;
        if (roll < config.heartAttackResistChance)
        {
            unit.stress = config.afflictionResetValue;
            unit.breakdownState = BreakdownState.None;
            BattleLog.Add($"<color=orange>【心脏衰竭抵抗】{unit.unitName} 凭借意志抵抗了心脏衰竭，压力回落到 {config.afflictionResetValue}！</color>");
            context.SpawnTextPopup(unit.transform.position + Vector3.up * 2f, "抵抗!", Color.yellow, 4f);
            VFXManager.FlashScreen(new Color(1f, 0.84f, 0f, 0.2f), 0.4f);
            VFXManager.FlashSkeleton(unit, Color.yellow, 0.3f);
        }
        else
        {
            int damage = Mathf.RoundToInt(unit.MaxHp * config.heartAttackDamagePercent);
            unit.currentHP -= damage;
            unit.UpdateHPUI();
            BattleLog.Add($"<color=red>【心脏衰竭】{unit.unitName} 心脏不堪重负，受到 {damage} 点致命伤害！</color>");
            context.SpawnTextPopup(unit.transform.position + Vector3.up * 2f, "心脏衰竭!", Color.red, 5f);
            VFXManager.ScreenShake(0.6f, 0.4f);
            VFXManager.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.5f);
            VFXManager.FlashSkeleton(unit, Color.red, 0.4f);
            if (unit.currentHP <= 0)
            {
                unit.currentHP = 0;
                BattleLog.Add($"{unit.unitName} 死于心脏衰竭！");
            }
            unit.stress = Mathf.Max(0, config.afflictionResetValue - RandomProvider.Current.Range(10, 31));
        }
    }

    public static void TryResolveBreakdown(UnitData unit, IBattleContext context)
    {
        if (unit == null || unit.breakdownCooldown > 0) return;

        List<StressTag> contextTags = unit.stressEventHistory
            .GroupBy(e => e.tag)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        float virtueProb = config.virtueChance;
        virtueProb += unit.positiveQuirkCount * config.virtueChancePerPositiveQuirk;
        virtueProb += (unit.stressResistRate * 100f) * config.virtueChancePerStressResist;

        bool isVirtue = RandomProvider.Current.Value < virtueProb;
        BreakdownEntry entry = SelectBreakdown(isVirtue, contextTags);
        if (entry == null) return;

        unit.breakdownState = isVirtue ? BreakdownState.Virtue : BreakdownState.Affliction;
        unit.currentBreakdownId = entry.id;
        int durMin = isVirtue ? config.virtueDurationMin : config.afflictionDurationMin;
        int durMax = isVirtue ? config.virtueDurationMax : config.afflictionDurationMax;
        unit.breakdownDurationRemaining = RandomProvider.Current.Range(durMin, durMax + 1);
        unit.breakdownCooldown = config.breakdownCooldown;

        if (!isVirtue)
            unit.stress = config.afflictionResetValue;

        BattleLog.Add($"<color={(isVirtue ? "#44ff44" : "#ff4444")}>【{(isVirtue ? "美德" : "折磨")}】{unit.unitName} 触发 [{entry.displayName}]！{entry.description}</color>");

        if (context != null)
        {
            Vector3 pos = unit.transform.position + Vector3.up * 2f;
            if (isVirtue)
                context.SpawnTextPopup(pos, entry.displayName, Color.green, 5f);
            else
                context.SpawnTextPopup(pos, entry.displayName, Color.red, 5f);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(isVirtue ? AudioKeys.SFX_VIRTUE : AudioKeys.SFX_BREAKDOWN);

        // 屏幕特效
        if (isVirtue)
        {
            VFXManager.FlashScreen(new Color(1f, 0.84f, 0f, 0.25f), 0.6f);
            VFXManager.FlashSkeleton(unit, Color.yellow, 0.5f);
        }
        else
        {
            VFXManager.FlashScreen(new Color(0.5f, 0f, 0.5f, 0.35f), 0.6f);
            VFXManager.FlashSkeleton(unit, new Color(0.5f, 0f, 0.5f), 0.5f);
        }

        ApplyBreakdownEffect(unit, entry, context);
    }

    static BreakdownEntry SelectBreakdown(bool isVirtue, List<StressTag> contextTags)
    {
        var contextMatches = BreakdownDatabase.GetByTag(contextTags)
            .Where(e => e.isVirtue == isVirtue).ToList();
        var pool = new List<BreakdownEntry>();

        if (contextMatches.Count > 0 && RandomProvider.Current.Value < 0.8f)
        {
            float totalWeight = contextMatches.Sum(e => e.baseWeight);
            float roll = RandomProvider.Current.Value * totalWeight;
            float cumulative = 0;
            foreach (var entry in contextMatches)
            {
                cumulative += entry.baseWeight;
                if (roll <= cumulative) { pool.Add(entry); break; }
            }
        }

        if (pool.Count == 0)
        {
            var all = BreakdownDatabase.GetAll(isVirtue);
            float totalWeight = all.Sum(e => e.baseWeight);
            float roll = RandomProvider.Current.Value * totalWeight;
            float cumulative = 0;
            foreach (var entry in all)
            {
                cumulative += entry.baseWeight;
                if (roll <= cumulative) { pool.Add(entry); break; }
            }
        }

        return pool.Count > 0 ? pool[0] : null;
    }

    static void ApplyBreakdownEffect(UnitData unit, BreakdownEntry entry, IBattleContext context)
    {
        if (entry.stressChange > 0)
            unit.stress = Mathf.Min(unit.stress + entry.stressChange, GetStressCap(unit));
        else if (entry.stressChange < 0)
            unit.stress = Mathf.Max(unit.stress + entry.stressChange, 0);

        if (entry.selfDamage != 0 && context != null)
        {
            int dmg = Mathf.Abs(entry.selfDamage);
            if (entry.selfDamage > 0)
                context.DealDamage(unit, unit, dmg);
            else
            {
                int heal = Mathf.Min(dmg, unit.MaxHp - unit.currentHP);
                unit.currentHP += heal;
                unit.UpdateHPUI();
                if (heal > 0)
                    context.SpawnDamagePopup(unit.transform.position + Vector3.up * 1.5f, heal, PopupType.Heal);
                BattleLog.Add($"[自愈] {unit.unitName} 恢复了 {heal} 点生命");
            }
        }

        if (entry.randomAttack && context != null)
        {
            var allAlive = new List<UnitData>();
            allAlive.AddRange(context.PlayerUnits.FindAll(u => u.currentHP > 0 && u != unit));
            allAlive.AddRange(context.EnemyUnits.FindAll(u => u.currentHP > 0 && u != unit));
            if (allAlive.Count > 0)
            {
                UnitData target = allAlive[RandomProvider.Current.Range(0, allAlive.Count)];
                int dmg = Mathf.Max(1, unit.GetEffectiveSTR() + unit.weaponAttack - target.GetEffectiveDEF());
                BattleLog.Add($"[崩溃] {unit.unitName} 失控攻击了 {target.unitName}！");
                context.DealDamage(unit, target, dmg);
            }
        }
    }

    public static void TickBreakdown(UnitData unit)
    {
        if (unit == null || unit.breakdownState == BreakdownState.None) return;

        if (unit.breakdownCooldown > 0)
            unit.breakdownCooldown--;

        unit.breakdownDurationRemaining--;

        var entry = BreakdownDatabase.Get(unit.currentBreakdownId);
        if (entry != null && entry.ongoingStressPerTurn != 0)
        {
            unit.stress = Mathf.Clamp(unit.stress + entry.ongoingStressPerTurn, 0, GetStressCap(unit));
            string sign = entry.ongoingStressPerTurn > 0 ? "+" : "";
            BattleLog.Add($"[{entry.displayName}] {unit.unitName} 压力持续变化 ({sign}{entry.ongoingStressPerTurn})");
        }

        if (unit.breakdownDurationRemaining <= 0)
        {
            BattleLog.Add($"[崩溃] {unit.unitName} 从崩溃状态中恢复");
            unit.breakdownState = BreakdownState.None;
            unit.currentBreakdownId = null;
        }
    }

    public static float GetStatModifier(UnitData unit)
    {
        if (unit.breakdownState == BreakdownState.None) return 1f;
        if (unit.breakdownState == BreakdownState.HeartAttack) return 0.5f;
        var entry = BreakdownDatabase.Get(unit.currentBreakdownId);
        if (entry != null && entry.statModPercent != 0)
            return 1f + entry.statModPercent;
        return 1f;
    }

    public static bool ShouldSkipTurn(UnitData unit)
    {
        if (unit.breakdownState == BreakdownState.None) return false;
        if (unit.breakdownState == BreakdownState.HeartAttack) return false;
        var entry = BreakdownDatabase.Get(unit.currentBreakdownId);
        return entry != null && entry.skipTurn;
    }

    public static Color GetStressColor(int stress)
    {
        if (stress < 50) return Color.green;
        if (stress < 100) return Color.yellow;
        if (stress < 150) return new Color(1f, 0.6f, 0f);
        return Color.red;
    }

    public static string GetStressLevelLabel(int stress)
    {
        if (stress < 25) return "安心";
        if (stress < 50) return "正常";
        if (stress < 75) return "警惕";
        if (stress < 100) return "危险";
        if (stress < 150) return "濒临崩溃";
        if (stress < 200) return "极限";
        return "心脏衰竭";
    }
}
