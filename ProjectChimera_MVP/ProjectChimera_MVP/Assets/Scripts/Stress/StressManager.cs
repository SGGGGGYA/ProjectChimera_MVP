using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StressManager
{
    public static StressConfig config = new StressConfig();
    public static bool initialized;

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

    public static void AddStress(UnitData unit, int rawAmount, StressTag tag)
    {
        if (unit == null || unit.currentHP <= 0) return;
        int actual = CalcActualGain(rawAmount, unit.stressResistRate);
        unit.stress = Mathf.Min(unit.stress + actual, config.maxStress);

        unit.stressEventHistory.Add(new StressEvent(actual, tag));
        if (unit.stressEventHistory.Count > 5)
            unit.stressEventHistory.RemoveAt(0);

        BattleLog.Add($"[压力] {unit.unitName} 压力 +{actual} (原始{rawAmount}, 抗性{unit.stressResistRate * 100:F0}%) ({unit.stress}/{config.maxStress})");
    }

    public static void ReduceStress(UnitData unit, int amount)
    {
        if (unit == null) return;
        unit.stress = Mathf.Max(unit.stress - amount, 0);
        BattleLog.Add($"[压力] {unit.unitName} 压力 -{amount} ({unit.stress}/{config.maxStress})");
    }

    public static void TryResolveBreakdown(UnitData unit, BattleManager bm)
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
        virtueProb += (unit.stressResistRate * 100f / 10f) * config.virtueChancePerStressResist;

        bool isVirtue = Random.value < virtueProb;
        BreakdownEntry entry = SelectBreakdown(isVirtue, contextTags);
        if (entry == null) return;

        unit.breakdownState = isVirtue ? BreakdownState.Virtue : BreakdownState.Affliction;
        unit.currentBreakdownId = entry.id;
        int durMin = isVirtue ? config.virtueDurationMin : config.afflictionDurationMin;
        int durMax = isVirtue ? config.virtueDurationMax : config.afflictionDurationMax;
        unit.breakdownDurationRemaining = Random.Range(durMin, durMax + 1);
        unit.breakdownCooldown = config.breakdownCooldown;

        BattleLog.Add($"<color={(isVirtue ? "#44ff44" : "#ff4444")}>【{(isVirtue ? "美德" : "折磨")}】{unit.unitName} 触发 [{entry.displayName}]！{entry.description}</color>");

        ApplyBreakdownEffect(unit, entry, bm);
    }

    static BreakdownEntry SelectBreakdown(bool isVirtue, List<StressTag> contextTags)
    {
        var contextMatches = BreakdownDatabase.GetByTag(contextTags)
            .Where(e => e.isVirtue == isVirtue).ToList();
        var pool = new List<BreakdownEntry>();

        if (contextMatches.Count > 0 && Random.value < 0.8f)
        {
            float totalWeight = contextMatches.Sum(e => e.baseWeight);
            float roll = Random.value * totalWeight;
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
            float roll = Random.value * totalWeight;
            float cumulative = 0;
            foreach (var entry in all)
            {
                cumulative += entry.baseWeight;
                if (roll <= cumulative) { pool.Add(entry); break; }
            }
        }

        return pool.Count > 0 ? pool[0] : null;
    }

    static void ApplyBreakdownEffect(UnitData unit, BreakdownEntry entry, BattleManager bm)
    {
        if (entry.stressChange > 0)
            unit.stress = Mathf.Min(unit.stress + entry.stressChange, config.maxStress);
        else if (entry.stressChange < 0)
            unit.stress = Mathf.Max(unit.stress + entry.stressChange, 0);

        if (entry.selfDamage != 0 && bm != null)
        {
            int dmg = Mathf.Abs(entry.selfDamage);
            if (entry.selfDamage > 0)
                bm.DealDamage(unit, unit, dmg);
            else
            {
                int heal = Mathf.Min(dmg, unit.MaxHp - unit.currentHP);
                unit.currentHP += heal;
                unit.UpdateHPUI();
                BattleLog.Add($"[自愈] {unit.unitName} 恢复了 {heal} 点生命");
            }
        }

        if (entry.randomAttack && bm != null)
        {
            var allAlive = new List<UnitData>();
            allAlive.AddRange(bm.playerUnits.FindAll(u => u.currentHP > 0 && u != unit));
            allAlive.AddRange(bm.enemyUnits.FindAll(u => u.currentHP > 0 && u != unit));
            if (allAlive.Count > 0)
            {
                UnitData target = allAlive[Random.Range(0, allAlive.Count)];
                int dmg = Mathf.Max(1, unit.GetEffectiveSTR() + unit.weaponAttack - target.GetEffectiveDEF());
                BattleLog.Add($"[崩溃] {unit.unitName} 失控攻击了 {target.unitName}！");
                bm.DealDamage(unit, target, dmg);
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
            unit.stress = Mathf.Clamp(unit.stress + entry.ongoingStressPerTurn, 0, config.maxStress);
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
        var entry = BreakdownDatabase.Get(unit.currentBreakdownId);
        if (entry != null && entry.statModPercent != 0)
            return 1f + entry.statModPercent;
        return 1f;
    }

    public static bool ShouldSkipTurn(UnitData unit)
    {
        if (unit.breakdownState == BreakdownState.None) return false;
        var entry = BreakdownDatabase.Get(unit.currentBreakdownId);
        return entry != null && entry.skipTurn;
    }

    public static Color GetStressColor(int stress)
    {
        if (stress < 25) return Color.green;
        if (stress < 50) return Color.yellow;
        if (stress < 75) return new Color(1f, 0.6f, 0f);
        return Color.red;
    }

    public static string GetStressLevelLabel(int stress)
    {
        if (stress < 25) return "安心";
        if (stress < 50) return "正常";
        if (stress < 75) return "警惕";
        if (stress < 100) return "危险";
        return "濒临崩溃";
    }
}
