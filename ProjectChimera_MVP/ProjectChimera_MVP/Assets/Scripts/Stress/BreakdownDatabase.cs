using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BreakdownDatabase
{
    private static Dictionary<string, BreakdownEntry> _entries;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _entries = new Dictionary<string, BreakdownEntry>();
        _initialized = true;

        Register(new BreakdownEntry
        {
            id = "hopeless", effectType = BreakdownEffectType.Hopeless, isVirtue = false,
            displayName = "绝望", description = "被绝望吞噬，无法行动，伤害自己",
            tags = new List<StressTag> { StressTag.Combat, StressTag.AllyDown, StressTag.Darkness },
            baseWeight = 1.5f, durationTurns = 3, skipTurn = true, selfDamage = 4, stressChange = 5, ongoingStressPerTurn = 5
        });

        Register(new BreakdownEntry
        {
            id = "paranoid", effectType = BreakdownEffectType.Paranoid, isVirtue = false,
            displayName = "偏执", description = "觉得所有人都要害自己，随机攻击队友",
            tags = new List<StressTag> { StressTag.Combat, StressTag.CriticalHit, StressTag.AllyHarm },
            baseWeight = 1.2f, durationTurns = 3, randomAttack = true, stressChange = 3
        });

        Register(new BreakdownEntry
        {
            id = "fearful", effectType = BreakdownEffectType.Fearful, isVirtue = false,
            displayName = "怯懦", description = "恐惧支配了内心，畏缩不前",
            tags = new List<StressTag> { StressTag.Combat, StressTag.Darkness, StressTag.Blood },
            baseWeight = 1.0f, durationTurns = 2, skipTurn = true, statModPercent = -0.2f
        });

        Register(new BreakdownEntry
        {
            id = "abusive", effectType = BreakdownEffectType.Abusive, isVirtue = false,
            displayName = "暴怒", description = "失去理智，攻击场上所有单位",
            tags = new List<StressTag> { StressTag.Combat, StressTag.Blood, StressTag.SelfHarm },
            baseWeight = 0.8f, durationTurns = 2, randomAttack = true, skipTurn = false
        });

        Register(new BreakdownEntry
        {
            id = "selfish", effectType = BreakdownEffectType.Selfish, isVirtue = false,
            displayName = "自利", description = "只顾自己，强制治疗自己浪费行动",
            tags = new List<StressTag> { StressTag.Combat, StressTag.AllyDown, StressTag.Heal },
            baseWeight = 0.5f, durationTurns = 2, skipTurn = true, selfDamage = -8
        });

        Register(new BreakdownEntry
        {
            id = "courageous", effectType = BreakdownEffectType.Courageous, isVirtue = true,
            displayName = "英勇", description = "直面恐惧，全属性大幅提升",
            tags = new List<StressTag> { StressTag.Combat, StressTag.Victory, StressTag.Heal },
            baseWeight = 1.0f, durationTurns = 3, statModPercent = 0.25f, stressChange = -10, ongoingStressPerTurn = -3
        });

        Register(new BreakdownEntry
        {
            id = "focused", effectType = BreakdownEffectType.Focused, isVirtue = true,
            displayName = "专注", description = "心如止水，下个技能必暴击",
            tags = new List<StressTag> { StressTag.Combat, StressTag.Heal, StressTag.Exploration },
            baseWeight = 1.0f, durationTurns = 2, stressChange = -8
        });

        Register(new BreakdownEntry
        {
            id = "protective", effectType = BreakdownEffectType.Protective, isVirtue = true,
            displayName = "守护", description = "誓死保护队友，自动替队友承受伤害",
            tags = new List<StressTag> { StressTag.Combat, StressTag.AllyDown, StressTag.Blood },
            baseWeight = 0.8f, durationTurns = 2, stressChange = -5
        });

        Register(new BreakdownEntry
        {
            id = "inspired", effectType = BreakdownEffectType.Inspired, isVirtue = true,
            displayName = "鼓舞", description = "斗志昂扬，为全队减压",
            tags = new List<StressTag> { StressTag.Victory, StressTag.Heal, StressTag.Combat },
            baseWeight = 0.7f, durationTurns = 1, stressChange = -15, ongoingStressPerTurn = -3
        });

        Register(new BreakdownEntry
        {
            id = "resilient", effectType = BreakdownEffectType.Resilient, isVirtue = true,
            displayName = "坚韧", description = "钢铁意志，清除负面状态并获得护盾",
            tags = new List<StressTag> { StressTag.Combat, StressTag.Darkness, StressTag.AllyDown },
            baseWeight = 0.5f, durationTurns = 2, statModPercent = 0.1f, stressChange = -5
        });
    }

    static void Register(BreakdownEntry entry)
    {
        if (!_entries.ContainsKey(entry.id))
            _entries.Add(entry.id, entry);
    }

    public static BreakdownEntry Get(string id)
    {
        if (!_initialized) Initialize();
        return _entries.TryGetValue(id, out var entry) ? entry : null;
    }

    public static List<BreakdownEntry> GetAll(bool isVirtue)
    {
        if (!_initialized) Initialize();
        var result = new List<BreakdownEntry>();
        foreach (var entry in _entries.Values)
            if (entry.isVirtue == isVirtue)
                result.Add(entry);
        return result;
    }

    public static List<BreakdownEntry> GetByTag(List<StressTag> contextTags)
    {
        if (!_initialized) Initialize();
        var scored = new List<KeyValuePair<BreakdownEntry, float>>();
        foreach (var entry in _entries.Values)
        {
            float score = 0;
            foreach (var tag in contextTags)
                if (entry.tags.Contains(tag))
                    score += 1f / entry.tags.Count;
            if (score > 0)
                scored.Add(new KeyValuePair<BreakdownEntry, float>(entry, score));
        }
        scored.Sort((a, b) => b.Value.CompareTo(a.Value));
        var result = new List<BreakdownEntry>();
        foreach (var kv in scored)
            result.Add(kv.Key);
        return result;
    }
}
