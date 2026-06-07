using UnityEngine;

public static class CombatSystem
{
    const float BASE_HIT_CHANCE = 0.75f;
    const float HIT_ACC_SCALE = 0.003f;
    const float HIT_MIN = 0.10f;
    const float HIT_MAX = 0.95f;
    const float BASE_CRIT = 0.03f;
    const float CRT_CRIT_SCALE = 0.005f;
    const float CRIT_MIN = 0.01f;
    const float CRIT_MAX = 0.70f;
    const float CRIT_OVERFLOW_RATE = 0.30f;
    const float CRIT_DEF_IGNORE = 0.50f;

    public static bool IsHit(UnitData attacker, UnitData target)
    {
        float finalChance = Mathf.Clamp(BASE_HIT_CHANCE + (attacker.ACC - target.DOD) * HIT_ACC_SCALE, HIT_MIN, HIT_MAX);
        return Random.value < finalChance;
    }

    public static bool IsCrit(UnitData attacker, UnitData target)
    {
        float rawHit = BASE_HIT_CHANCE + (attacker.ACC - target.DOD) * HIT_ACC_SCALE;
        float overflow = Mathf.Max(0, rawHit - HIT_MAX);
        float finalCrit = Mathf.Clamp(BASE_CRIT + attacker.CRT * CRT_CRIT_SCALE + overflow * CRIT_OVERFLOW_RATE, CRIT_MIN, CRIT_MAX);
        return Random.value < finalCrit;
    }

    public static int CalculateDamage(UnitData attacker, UnitData target, SkillData skill, bool isCrit = false)
    {
        float baseVal = skill.baseDamage + attacker.weaponAttack;
        float scaling = attacker.GetEffectiveSTR() * skill.strScaling + attacker.AGI * skill.agiScaling;
        float raw = baseVal + scaling;

        raw *= Random.Range(0.85f, 1.15f);

        float defReduction = target.DEF_Effective * 0.5f;
        if (isCrit)
            defReduction *= (1f - CRIT_DEF_IGNORE);
        raw -= defReduction;

        raw *= target.GetIncomingDamageMultiplier();
        raw *= attacker.GetDamageModifier();

        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    public static bool IsStatusHit(UnitData attacker, UnitData target, float baseChance)
    {
        float intMod = attacker.GetEffectiveINT() * 0.01f;
        float finalChance = Mathf.Clamp(baseChance + intMod, 0.05f, 0.95f);
        return Random.value < finalChance;
    }
}
