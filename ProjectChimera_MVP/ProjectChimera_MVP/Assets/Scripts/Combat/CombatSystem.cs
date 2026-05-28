using UnityEngine;

// ============================================================================
// Project Chimera — 战斗公式系统
// 所有战斗交互公式的唯一入口，纯静态类，无 MonoBehaviour 依赖
//
// 【公式一览】
//   1. ACC 命中率 = 75% + (attackerACC - targetDOD) × 0.3%
//      范围: [10%, 95%]
//
//   2. CRT 暴击率 = 3% + attackerCRT × 0.5%
//      范围: [1%, 50%]
//
//   3. 状态命中率 = baseChance + attackerINT × 1%
//      范围: [5%, 95%]
//
//   4. 伤害:
//      a. 基础 = skill.baseDamage + attacker.weaponAttack
//      b. STR/AGI 加成 = attacker.GetEffectiveSTR() × skill.strScaling + attacker.AGI × skill.agiScaling
//      c. 伤害波动 = ±15%  (Random.Range(0.85, 1.15))
//      d. 防御减免 = target.DEF_Effective × 0.5  (线性减免)
//      e. 标记增伤 = target.GetIncomingDamageMultiplier()
//      f. 压力修正 = attacker.GetDamageModifier()
//      g. 暴击倍率 = ×1.5
//      h. 最终 = max(1, 取整)
// ============================================================================

public static class CombatSystem
{
    /// <summary>
    /// 命中判定
    /// </summary>
    public static bool IsHit(UnitData attacker, UnitData target)
    {
        float baseHitChance = 0.75f;
        int attackerACC = attacker.ACC;
        int targetDOD = target.DOD;
        float finalChance = Mathf.Clamp(baseHitChance + (attackerACC - targetDOD) * 0.003f, 0.10f, 0.95f);
        return Random.value < finalChance;
    }

    /// <summary>
    /// 暴击判定
    /// </summary>
    public static bool IsCrit(UnitData attacker, UnitData target)
    {
        float baseCrit = 0.03f;
        int attackerCRT = attacker.CRT;
        float finalCrit = Mathf.Clamp(baseCrit + attackerCRT * 0.005f, 0.01f, 0.50f);
        return Random.value < finalCrit;
    }

    /// <summary>
    /// 计算伤害（不含暴击乘区，暴击由调用方自行处理）
    /// </summary>
    public static int CalculateDamage(UnitData attacker, UnitData target, SkillData skill)
    {
        float baseVal = skill.baseDamage + attacker.weaponAttack;
        float scaling = attacker.GetEffectiveSTR() * skill.strScaling + attacker.AGI * skill.agiScaling;
        float raw = baseVal + scaling;

        raw *= Random.Range(0.85f, 1.15f);

        raw -= target.DEF_Effective * 0.5f;

        raw *= target.GetIncomingDamageMultiplier();

        raw *= attacker.GetDamageModifier();

        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    /// <summary>
    /// 状态命中判定
    /// baseChance 来自技能/效果的基准概率
    /// attackerINT 增强状态命中（每点 INT +1%）
    /// </summary>
    public static bool IsStatusHit(UnitData attacker, UnitData target, float baseChance)
    {
        float intMod = attacker.GetEffectiveINT() * 0.01f;
        float finalChance = Mathf.Clamp(baseChance + intMod, 0.05f, 0.95f);
        return Random.value < finalChance;
    }
}
