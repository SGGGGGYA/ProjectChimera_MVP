using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态效果类型
/// </summary>
public enum StatusType
{
    Stun,       // 眩晕：跳过回合
    Mark,       // 标记：受伤增加 effectValue%
    Taunt,      // 嘲讽：强制攻击施加者
    Protected,  // 援护：伤害转移给施加者
    Bleed,      // 流血：每回合扣 effectValue 点血
    Berserk,    // 狂暴：STR+5, DEF-3
    Enlightened // 启迪：INT+2
}

/// <summary>
/// 运行时状态效果，挂在单位身上随时间 tick
/// </summary>
[System.Serializable]
public class StatusEffect
{
    public StatusType type;
    public string sourceName;       // 谁施加的（用于嘲讽/援护查找目标）
    public int remainingTurns;      // 剩余回合数
    public float value;             // 标记时 = 增伤百分比 (0.2 = +20%)
    public bool expired => remainingTurns <= 0;

    /// <summary>每回合结束时调用，减1回合</summary>
    public void Tick()
    {
        if (remainingTurns > 0)
            remainingTurns--;
    }

    public StatusEffect(StatusType type, string sourceName, int duration, float value = 0)
    {
        this.type = type;
        this.sourceName = sourceName;
        this.remainingTurns = duration;
        this.value = value;
    }
}
