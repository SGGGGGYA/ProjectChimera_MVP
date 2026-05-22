using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能目标类型
/// </summary>
public enum SkillTargetType
{
    SingleEnemy,  // 选择一个敌人
    SingleAlly,   // 选择一个友方（含自己）
    AllEnemies,   // 全体敌人，无需选择目标
    Self          // 仅对自己生效
}

/// <summary>
/// 技能效果类型
/// </summary>
public enum SkillEffectType
{
    DirectDamage,  // 直接伤害
    Stun,          // 眩晕（跳过下回合）
    Taunt,         // 嘲讽（强制敌人攻击自己）
    Mark,          // 标记（受伤增加）
    Protect,       // 援护（替友方承伤）
    AoEDamage,     // 群体伤害
    Heal,          // 治疗
    Shield,        // 护盾
    Bleed,         // 流血
    Stress         // 压力
}

/// <summary>
/// 技能定义数据（序列化，可在 Inspector 中编辑）
/// </summary>
[System.Serializable]
public class SkillData
{
    [Header("基本信息")]
    public string skillName = "未命名技能";
    public string description = "";

    [Header("伤害参数")]
    public int baseDamage;
    public float strScaling;   // STR 系数，如 1.0 = 100% STR
    public float agiScaling;   // AGI 系数

    [Header("目标与效果")]
    public SkillTargetType targetType = SkillTargetType.SingleEnemy;
    public SkillEffectType effectType = SkillEffectType.DirectDamage;

    [Header("目标选择")]
    public int[] validTargetPositions;  // 可攻击的敌方位置编号 1-4（0=不限制）
    public bool canTargetSelf;         // 是否可选自身
    public bool canTargetAlly;         // 是否可选友军

    [Header("状态效果参数")]
    public float effectValue;   // 眩晕回合数 / 标记增伤倍率(0.2=+20%) / 等
    public int effectDuration;  // 效果持续几回合（0=瞬间）
}
