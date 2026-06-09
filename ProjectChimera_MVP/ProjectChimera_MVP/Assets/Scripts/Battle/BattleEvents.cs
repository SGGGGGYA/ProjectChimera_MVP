using System;
using UnityEngine;

/// <summary>
/// 战斗事件总线 — 跨 BattleManager / TurnManager / UI 的全局事件通道。
///
/// 解决的问题：
///   TurnManager ↔ BattleManager 循环依赖中，命令类调用（如"结束回合"）可以直接走事件，
///   而不需要 BattleManager 持有 TurnManager.Instance 引用。
///
/// 使用模式：
///   - 发布方：`BattleEvents.RaiseEndTurnRequested();`（不订阅自己）
///   - 订阅方：在 OnEnable 中 `BattleEvents.OnTurnStarted += Handler;`
///              在 OnDisable 中 `BattleEvents.OnTurnStarted -= Handler;`
///
/// 命名约定：
///   - On*  = 订阅方关心的事件
///   - Raise* = 发布入口（保持静态方法签名一致，避免外部写 `?.Invoke()`）
/// </summary>
public static class BattleEvents
{
    // -------- TurnManager 发布，BattleManager/UI 订阅 --------

    /// <summary>新回合开始（含新一轮） — TurnManager 在 StartNextTurn 末尾发布。</summary>
    public static event Action<UnitData> OnTurnStarted;

    /// <summary>当前回合结束 — TurnManager 在 EndTurn 末尾发布。</summary>
    public static event Action<UnitData> OnTurnEnded;

    /// <summary>新一轮开始 — TurnManager 在 StartRound 开头发布。</summary>
    public static event Action<int> OnRoundStarted;

    /// <summary>单位死亡（任何原因）— 由 BattleManager 在 DealDamage 后发布。</summary>
    public static event Action<UnitData> OnUnitKilled;

    // -------- BattleManager 发布，TurnManager 订阅 --------

    /// <summary>玩家请求结束自己的回合（点击"结束回合"按钮）。TurnManager 订阅并执行 EndTurn。</summary>
    public static event Action OnEndTurnRequested;

    /// <summary>敌人回合完成（AI 协程完成）— TurnManager 订阅并推进到下一回合。</summary>
    public static event Action<UnitData> OnEnemyTurnCompleted;

    // -------- 战斗生命周期 --------

    /// <summary>战斗结束（玩家胜利/失败）。由 BattleManager 发布。</summary>
    public static event Action<bool> OnBattleEnded; // bool = isVictory

    // -------- 发布入口 --------

    public static void RaiseTurnStarted(UnitData unit)
        => OnTurnStarted?.Invoke(unit);

    public static void RaiseTurnEnded(UnitData unit)
        => OnTurnEnded?.Invoke(unit);

    public static void RaiseRoundStarted(int round)
        => OnRoundStarted?.Invoke(round);

    public static void RaiseUnitKilled(UnitData unit)
        => OnUnitKilled?.Invoke(unit);

    public static void RaiseEndTurnRequested()
        => OnEndTurnRequested?.Invoke();

    public static void RaiseEnemyTurnCompleted(UnitData unit)
        => OnEnemyTurnCompleted?.Invoke(unit);

    public static void RaiseBattleEnded(bool isVictory)
        => OnBattleEnded?.Invoke(isVictory);

    /// <summary>
    /// 清除所有订阅者 — 用于场景重置 / 编辑器 Play→Stop→Play 避免静态事件残留。
    /// 单元测试 TearDown 也会调用。
    /// </summary>
    public static void ClearAll()
    {
        OnTurnStarted = null;
        OnTurnEnded = null;
        OnRoundStarted = null;
        OnUnitKilled = null;
        OnEndTurnRequested = null;
        OnEnemyTurnCompleted = null;
        OnBattleEnded = null;
    }
}
