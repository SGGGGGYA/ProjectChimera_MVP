using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BreakdownEntry
{
    public string id;
    public BreakdownEffectType effectType;
    public bool isVirtue;
    public string displayName;
    public string description;
    public List<StressTag> tags = new List<StressTag>();
    public float baseWeight = 1f;
    public int durationTurns = 2;
    public float statModPercent;
    public int stressChange;
    public int ongoingStressPerTurn;
    public int selfDamage;
    public bool skipTurn;
    public bool randomAttack;
}
