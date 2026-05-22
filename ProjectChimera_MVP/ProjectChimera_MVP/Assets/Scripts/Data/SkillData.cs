using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillEffectType
{
    DirectDamage,
    Stun,
    Taunt,
    Mark,
    Protect,
    AoEDamage,
    Heal,
    Shield,
    Bleed,
    Stress,
    // 扩展（狂战士/学者等技能使用）
    BloodthirstyStrike,
    BerserkBuff,
    DesperateStrike,
    MindShock,
    WisdomEnlightenment,
    ForbiddenKnowledge
}

public enum SkillTargetType
{
    SingleEnemy,
    SingleAlly,
    AllEnemies,
    Self
}

[System.Serializable]
public class SkillData
{
    [Header("基本信息")]
    public string skillName = "未命名技能";
    public string description = "";

    [Header("目标选择")]
    public SkillTargetType targetType = SkillTargetType.SingleEnemy;
    public int[] validTargetPositions;
    public bool canTargetSelf;
    public bool canTargetAlly;

    [Header("原子命令队列")]
    public List<Command> commands = new List<Command>();

    // 旧版参数（向后兼容）
    public int baseDamage;
    public float strScaling;
    public float agiScaling;

    [Header("AI辅助标记（可选）")]
    public SkillEffectType aiCategory;
    public float effectValue;
    public int effectDuration;
}
