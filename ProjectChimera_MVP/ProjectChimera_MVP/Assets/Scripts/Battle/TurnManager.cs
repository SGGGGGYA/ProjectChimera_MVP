using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;

/// <summary>
/// 回合管理器（解耦后）：
///   - 实现 <see cref="ITurnStateProvider"/>：让 BattleManager 通过接口查询回合状态
///   - 持有 <see cref="IBattleContext"/>：把对 BattleManager 的 3 处 FindObjectOfType 全部消除
///   - 订阅 <see cref="BattleEvents"/>：玩家"结束回合"和"敌人回合完成"改为事件驱动
///   - 发布 <see cref="BattleEvents"/>：TurnStart / TurnEnd / RoundStart 改为事件广播
///
/// 这样 TurnManager ↔ BattleManager 不再有 C# 类级别的相互引用，循环依赖彻底消除。
/// </summary>
public class TurnManager : MonoBehaviour, ITurnStateProvider
{
    [Header("回合设置")]
    public List<UnitData> allUnits = new List<UnitData>();
    private List<UnitData> turnOrder = new List<UnitData>();
    public int currentTurnIndex = 0;
    public int roundCount = 1;

    [Header("当前回合")]
    public UnitData currentUnit;
    public bool isPlayerTurn;

    public static TurnManager Instance;

    /// <summary>
    /// 编辑器测试模式：为 true 时不使用 StartCoroutine 驱动敌人回合，
    /// 而是把敌人协程暴露给外部 ManualStep 调用。
    /// </summary>
    public static bool IsEditorTestMode = false;

    /// <summary>编辑器测试模式下活跃的敌人回合协程（手动 Step 用）</summary>
    public System.Collections.IEnumerator ActiveEnemyCoroutine { get; set; }

    /// <summary>
    /// 注入的 BattleManager 上下文（由 BattleSetup 在 StartBattle 时设置）。
    /// 在调用 BattleSetup 前保持 NullBattleContext，所有调用安静 no-op，行为与旧
    /// `FindObjectOfType<BattleManager>() == null` 兜底一致。
    /// </summary>
    public IBattleContext Context { get; set; } = new NullBattleContext();

    // -------- ITurnStateProvider 实现 --------

    public UnitData CurrentUnit => currentUnit;
    public bool IsPlayerTurn => isPlayerTurn;
    public int RoundCount => roundCount;
    public bool IsBattleOver => Context.IsBattleOver;

    public event Action<UnitData> TurnStarted;
    public event Action<UnitData> TurnEnded;
    public event Action<int> RoundStarted;

    void Awake()
    {
        Instance = this;
        // 订阅玩家结束回合请求（替代 BattleManager 直接调 TurnManager.Instance.EndTurn）
        BattleEvents.OnEndTurnRequested += HandleEndTurnRequested;
        BattleEvents.OnEnemyTurnCompleted += HandleEnemyTurnCompleted;
    }

    void OnDestroy()
    {
        BattleEvents.OnEndTurnRequested -= HandleEndTurnRequested;
        BattleEvents.OnEnemyTurnCompleted -= HandleEnemyTurnCompleted;
    }

    void HandleEndTurnRequested() => EndTurn();

    void HandleEnemyTurnCompleted(UnitData unit) => EndTurn();

    public void InitializeBattle(List<UnitData> playerUnits, List<UnitData> enemyUnits)
    {
        allUnits.Clear();
        if (playerUnits != null) allUnits.AddRange(playerUnits);
        if (enemyUnits != null) allUnits.AddRange(enemyUnits);

        EnemyAIEngine.ResetAll();

        CalculateTurnOrder();
        StartRound();

        // 战斗初始化完成，允许触发结束面板
        var bm = Context.RawContext as BattleManager;
        if (bm != null) bm.battleReady = true;
    }

    void CalculateTurnOrder()
    {
        turnOrder = new List<UnitData>(allUnits.FindAll(u => u != null));
        turnOrder.Sort((a, b) => b.AGI.CompareTo(a.AGI));

        Log.Info("[TurnManager] 回合顺序（按AGI排序）：");
        for (int i = 0; i < turnOrder.Count; i++)
        {
            Log.Info($"{i + 1}. {turnOrder[i].unitName} (AGI: {turnOrder[i].AGI})");
        }
    }

    void StartRound()
    {
        currentTurnIndex = 0;
        BattleLog.Add($"===== 第 {roundCount} 轮开始 =====");
        // 广播新轮开始（ITurnStateProvider.RoundStarted + BattleEvents）
        RoundStarted?.Invoke(roundCount);
        BattleEvents.RaiseRoundStarted(roundCount);
        StartNextTurn();
    }

    void StartNextTurn()
    {
        // 战斗已结束，停止轮转回合（通过 Context 查询，无须再 FindObjectOfType）
        if (Context.IsBattleOver)
            return;

        // 如果某一方已经全灭，结束战斗
        bool allEnemyDead = Context.GetFirstAlive(Context.EnemyUnits) == null;
        bool allPlayerDead = Context.GetFirstAlive(Context.PlayerUnits) == null;
        if (allEnemyDead || allPlayerDead)
        {
            BattleEvents.RaiseBattleEnded(allEnemyDead);
            return;
        }

        // 如果回合序列为空，尝试重建；若仍为空则结束战斗
        if (turnOrder == null || turnOrder.Count == 0)
        {
            CalculateTurnOrder();
            if (turnOrder == null || turnOrder.Count == 0)
            {
                BattleLog.Add("[TurnManager] 没有可行动单位，战斗结束");
                return;
            }
        }

        if (currentTurnIndex >= turnOrder.Count)
        {
            roundCount++;
            StartRound();
            return;
        }

        currentUnit = turnOrder[currentTurnIndex];

        if (currentUnit == null)
        {
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        if (currentUnit.currentHP <= 0)
        {
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        // 检查眩晕
        if (currentUnit.HasStatus(StatusType.Stun))
        {
            BattleLog.Add($"{currentUnit.unitName} 被眩晕，跳过回合！");
            currentUnit.RemoveStatus(StatusType.Stun);
            currentUnit.TickStatusEffects();
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        // 处理流血（回合开始时扣血，可能触发压力事件）
        StatusEffect bleedStatus = currentUnit.GetStatus(StatusType.Bleed);
        int bleedDmg = bleedStatus != null ? Mathf.RoundToInt(bleedStatus.value) : 0;
        bool alive = currentUnit.ProcessBleed();
        if (bleedDmg > 0 && alive)
            Context.SpawnDamagePopup(currentUnit.transform.position + Vector3.up * 1.5f, bleedDmg, PopupType.Damage);
        if (!alive)
        {
            BattleLog.Add($"{currentUnit.unitName} 因流血而死亡！");
            Context.SpawnDamagePopup(currentUnit.transform.position + Vector3.up * 1.5f, 0, PopupType.Damage);
            // 复用 StartNextTurn 入口已计算的全灭标记（避免内层作用域重名）
            if (allEnemyDead || allPlayerDead)
            {
                currentTurnIndex++;
                StartNextTurn();
                return;
            }
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        // 压力阈值检析（心脏衰竭优先，然后崩溃/美德判定）
        // 直接用 Context（已经是 IBattleContext）
        StressManager.CheckResolve(currentUnit, Context);

        if (currentUnit.currentHP <= 0)
        {
            BattleLog.Add($"{currentUnit.unitName} 在压力判定后死亡！");
            // 复用 StartNextTurn 入口已计算的全灭标记（避免内层作用域重名）
            if (allEnemyDead || allPlayerDead)
            {
                currentTurnIndex++;
                StartNextTurn();
                return;
            }
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        if (currentUnit.breakdownState != BreakdownState.None && StressManager.ShouldSkipTurn(currentUnit))
        {
            BattleLog.Add($"<color=red>{currentUnit.unitName} 因崩溃状态跳过回合！</color>");
            currentUnit.TickStatusEffects();
            StressManager.TickBreakdown(currentUnit);
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        // 低压力安心回压（仅限正常状态）
        if (currentUnit.breakdownState == BreakdownState.None &&
            currentUnit.stress < StressManager.config.lowStressThreshold && currentUnit.stress > 0)
        {
            currentUnit.stress = Mathf.Max(0, currentUnit.stress + StressManager.config.lowStressRecovery);
        }

        // 触发 TurnStart 特质
        QuirkTriggerSystem.CheckTriggers(currentUnit, QuirkTriggerType.TurnStart);

        isPlayerTurn = currentUnit.isPlayer;

        BattleLog.Add($"回合: {currentUnit.unitName}");
        if (isPlayerTurn && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioKeys.SFX_UI_CLICK, 0.5f);

        // 广播回合开始（ITurnStateProvider.TurnStarted + BattleEvents）
        TurnStarted?.Invoke(currentUnit);
        BattleEvents.RaiseTurnStarted(currentUnit);

        if (!isPlayerTurn)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_UI_HOVER);
            if (IsEditorTestMode)
            {
                // 编辑器模式：不依赖 StartCoroutine，暴露协程给测试循环手动驱动
                ActiveEnemyCoroutine = EnemyTurn();
            }
            else
            {
                StartCoroutine(EnemyTurn());
            }
        }
    }

    public void EndTurn()
    {
        if (currentUnit == null) return;
        Log.Info($"[TurnManager] {currentUnit.unitName} 结束回合");
        // 触发 TurnEnd 特质（在 tick 之前）
        QuirkTriggerSystem.CheckTriggers(currentUnit, QuirkTriggerType.TurnEnd);
        currentUnit.TickStatusEffects();
        currentUnit.TickQuirkCooldowns();
        currentUnit.TickModifiers();
        StressManager.TickBreakdown(currentUnit);
        // 广播回合结束
        TurnEnded?.Invoke(currentUnit);
        BattleEvents.RaiseTurnEnded(currentUnit);
        currentTurnIndex++;
        StartNextTurn();
    }

    IEnumerator EnemyTurn()
    {
        // P1 防御：调用 AI 前确认当前回合单位仍存活
        if (currentUnit == null || currentUnit.currentHP <= 0)
        {
            Log.Warn("[TurnManager] EnemyTurn 当前单位无效，跳过敌人回合");
            EndTurn();
            yield break;
        }

        // 通过 Context.RawContext 逃生通道拿到 BattleManager（仅在 EnemyAIEngine 完全迁移到
        // IBattleContext 之前使用；TODO: 重构 EnemyAIEngine 后改用 Context）。
        BattleManager bm = Context.RawContext as BattleManager;
        if (bm == null)
        {
            Log.Warn("[TurnManager] EnemyTurn 无法获取 BattleManager 引用，跳过敌人回合");
            EndTurn();
            yield break;
        }

        // 改为通过事件通知 TurnManager 推进（EnemyAIEngine 完成后调用 BattleEvents.RaiseEnemyTurnCompleted）
        yield return EnemyAIEngine.ExecuteTurn(currentUnit, bm, this, () =>
        {
            BattleEvents.RaiseEnemyTurnCompleted(currentUnit);
        });
    }

    public bool RankCanHit(UnitData attacker, UnitData target, SkillData skill)
    {
        bool targetIsFront = target.rank <= 1;
        if (targetIsFront && !skill.canTargetFrontRank) return false;
        if (!targetIsFront && !skill.canTargetBackRank) return false;
        return true;
    }

    public UnitData GetBestPlayerTarget(BattleManager bm)
    {
        var players = bm.playerUnits.Where(u => u.currentHP > 0).ToList();
        var front = players.Where(u => u.rank <= 1).ToList();
        if (front.Count > 0) return front[RandomProvider.Current.Range(0, front.Count)];
        return players.Count > 0 ? players[0] : null;
    }

    public bool SkillUsable(UnitData unit, SkillData skill)
    {
        return skill != null && unit.rank >= skill.minUserRank && unit.rank <= skill.maxUserRank;
    }

    public bool CanCurrentUnitAct()
    {
        return currentUnit != null && currentUnit.currentHP > 0;
    }
}
