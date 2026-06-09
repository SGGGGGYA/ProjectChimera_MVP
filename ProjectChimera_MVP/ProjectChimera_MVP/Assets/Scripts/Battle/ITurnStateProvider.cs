using System;
using UnityEngine;

/// <summary>
/// 回合状态提供者 — 把 TurnManager 的对外只读状态抽象为接口，让 BattleManager 不再
/// 直接引用 TurnManager 类，彻底打破 TurnManager ↔ BattleManager 循环依赖。
///
/// 调用链（修复后）：
///   BattleSetup → bm.SetTurnState(tm); tm.SetBattleContext(bm);
///
/// BattleManager 通过此接口获取：
///   - 当前回合单位（替代 TurnManager.Instance.currentUnit）
///   - 是否玩家回合（替代 TurnManager.Instance.isPlayerTurn）
///   - 订阅 TurnStart/TurnEnd 事件来更新 UI
///
/// TurnManager 通过 BattleContext 接口（见 BattleContext.cs）调用 BattleManager 的
/// 状态查询/UI 方法（替代 FindObjectOfType<BattleManager>() + 直接字段访问）。
/// </summary>
public interface ITurnStateProvider
{
    /// <summary>当前行动单位（可能是 null 在回合切换瞬间）。</summary>
    UnitData CurrentUnit { get; }

    /// <summary>当前回合是否属于玩家阵营。</summary>
    bool IsPlayerTurn { get; }

    /// <summary>当前轮次（从 1 开始）。</summary>
    int RoundCount { get; }

    /// <summary>
    /// 战斗是否已结束（BattleContext 拥有真相，TurnManager 转发读）。
    /// 写入 BattleManager.battleOver 字段，不暴露给 TurnManager。
    /// </summary>
    bool IsBattleOver { get; }

    /// <summary>TurnStart 事件 — TurnManager 在 StartNextTurn 末尾发布。</summary>
    event Action<UnitData> TurnStarted;

    /// <summary>TurnEnd 事件 — TurnManager 在 EndTurn 末尾发布。</summary>
    event Action<UnitData> TurnEnded;

    /// <summary>RoundStart 事件 — TurnManager 在 StartRound 开头发布。</summary>
    event Action<int> RoundStarted;
}
