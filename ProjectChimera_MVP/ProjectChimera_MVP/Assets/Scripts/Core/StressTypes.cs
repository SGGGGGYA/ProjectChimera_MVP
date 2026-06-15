// Core/StressTypes.cs
// 把 Stress 模块的纯数据/枚举/Config 集中到 Core,目的是打破 Core ↔ Stress 的循环引用。
// Stress 模块的"行为"(StressManager / QuirkTriggerSystem / BreakdownDatabase 等)仍然留在 Stress 里,
// 并通过 asmdef 引用 Core 来使用这些类型。
using System;
using UnityEngine;

[System.Serializable]
public class StressConfig
{
    public int maxStress = 200;
    public int virtueBreakMin = 100;
    public int onTakeDamage = 2;
    public int onCriticalHit = 5;
    public int onAllyDeath = 20;
    public int onAllyDeathNamed = 30;
    public int onKillEnemy = -10;
    public int onCriticalDeal = -5;
    public int onAllyCritical = -3;
    public int onBleedTick = 1;
    public float baseResist = 0f;
    public float resistPerLevel = 0.02f;
    public float maxResist = 0.9f;
    public float resistDiminishThreshold = 0.6f;
    [Range(0f, 1f)] public float virtueChance = 0.25f;
    public float virtueChancePerPositiveQuirk = 0.05f;
    public float virtueChancePerStressResist = 0.01f;
    public int virtueDurationMin = 2;
    public int virtueDurationMax = 4;
    public int afflictionDurationMin = 3;
    public int afflictionDurationMax = 5;
    public int afflictionResetValue = 100;
    public int heartAttackThreshold = 200;
    [Range(0f, 1f)] public float heartAttackResistChance = 0.2f;
    [Range(0f, 1f)] public float heartAttackDamagePercent = 0.5f;
    public bool virtueStressImmunity = true;
    public int campReduceBase = 15;
    public int townReducePerDay = 5;
    public int tavernReducePerVisit = 15;
    public int lowStressThreshold = 25;
    public int lowStressRecovery = -3;
    public int breakdownCooldown = 1;
}

public enum BreakdownState
{
    None,
    Virtue,
    Affliction,
    HeartAttack
}

public enum MentalState
{
    Normal,
    Afflicted,
    Virtuous,
    HeartAttack
}

public enum BreakdownEffectType
{
    Hopeless,
    Paranoid,
    Fearful,
    Abusive,
    Selfish,
    Courageous,
    Focused,
    Protective,
    Inspired,
    Resilient
}

public enum StressTag
{
    Combat,
    AllyDown,
    CriticalHit,
    Darkness,
    Blood,
    SelfHarm,
    AllyHarm,
    Victory,
    Heal,
    Exploration
}

[System.Serializable]
public class StressEvent
{
    public int amount;
    public StressTag tag;
    public float gameTime;

    public StressEvent(int amount, StressTag tag)
    {
        this.amount = amount;
        this.tag = tag;
        this.gameTime = Time.time;
    }
}
