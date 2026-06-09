using System.Collections.Generic;

public enum QuirkTriggerType
{
    BattleStart,
    TurnStart,
    TurnEnd,
    OnHit,
    OnCrit,
    OnTakeDamage,
    OnKill,
    OnStress,
    OnHeal,
    OnDeathsDoor,
    OnDodge
}

[System.Serializable]
public class QuirkEffect
{
    public StatType stat;
    public int amount;
    public bool isPercent;
}

[System.Serializable]
public class Quirk
{
    public string id;
    public string localizedName;
    public bool isPositive;
    public bool isLocked;
    public List<QuirkEffect> effects = new List<QuirkEffect>();

    public QuirkTriggerType triggerType = QuirkTriggerType.BattleStart;
    public float procChance = 1f;
    public string targetStat;
    public int effectValue;
    public int duration;

    public int GetFlatMod(StatType stat)
    {
        int sum = 0;
        foreach (var e in effects)
            if (e.stat == stat && !e.isPercent)
                sum += e.amount;
        return sum;
    }

    public float GetPercentMod(StatType stat)
    {
        float sum = 0;
        foreach (var e in effects)
            if (e.stat == stat && e.isPercent)
                sum += e.amount * 0.01f;
        return sum;
    }
}

/// <summary>
/// 传递给 QuirkTriggerSystem.CheckTriggers 的上下文数据
/// </summary>
public class QuirkContext
{
    public UnitData source;
    public int intValue;
    public bool valueModified;
}
