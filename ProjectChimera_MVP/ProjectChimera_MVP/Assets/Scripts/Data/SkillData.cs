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

    [Header("阵型限制")]
    [Tooltip("使用者必须在的站位范围 (0=前排, 3=后排)")]
    public int minUserRank = 0;
    public int maxUserRank = 3;
    [Tooltip("可攻击的敌方站位 (0=敌方前排)")]
    public bool canTargetFrontRank = true;
    public bool canTargetBackRank = true;

    [Header("移位(推进/击退)")]
    [Tooltip("目标位移: 正数=向后推, 负数=向前拉")]
    public int targetShift = 0;
    [Tooltip("自身位移: 正数=向前冲, 负数=后退")]
    public int selfShift = 0;

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
