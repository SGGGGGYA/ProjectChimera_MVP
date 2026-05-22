using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StressConfig
{
    public int maxStress = 100;
    public int virtueBreakMin = 100;
    public int virtueBreakMax = 150;
    public int onTakeDamage = 2;
    public int onCriticalHit = 5;
    public int onAllyDeath = 20;
    public int onAllyDeathNamed = 30;
    public int onMissAttack = 2;
    public int onKillEnemy = -5;
    public int onCriticalDeal = -5;
    public int onAllyCritical = -3;
    public int onBleedTick = 1;
    public float baseResist = 0f;
    public float resistPerLevel = 0.02f;
    public float maxResist = 0.9f;
    public float resistDiminishThreshold = 0.6f;
    [Range(0f, 1f)] public float virtueChance = 0.25f;
    public float virtueChancePerPositiveQuirk = 0.05f;
    public float virtueChancePerStressResist = 0.1f;
    public int virtueDurationMin = 2;
    public int virtueDurationMax = 4;
    public int afflictionDurationMin = 3;
    public int afflictionDurationMax = 5;
    public int campReduceBase = 15;
    public int townReducePerDay = 5;
    public int tavernReducePerVisit = 15;
    public int lowStressThreshold = 25;
    public int lowStressRecovery = -3;
    public int breakdownCooldown = 1;
}
