using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("回合设置")]
    public List<UnitData> allUnits = new List<UnitData>();
    private List<UnitData> turnOrder = new List<UnitData>();
    private int currentTurnIndex = 0;
    public int roundCount = 1;

    [Header("当前回合")]
    public UnitData currentUnit;
    public bool isPlayerTurn;

    public static TurnManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void InitializeBattle(List<UnitData> playerUnits, List<UnitData> enemyUnits)
    {
        allUnits.Clear();
        allUnits.AddRange(playerUnits);
        allUnits.AddRange(enemyUnits);

        EnemyAIEngine.ResetAll();

        CalculateTurnOrder();
        StartRound();
    }

    void CalculateTurnOrder()
    {
        turnOrder = new List<UnitData>(allUnits);
        turnOrder.Sort((a, b) => b.AGI.CompareTo(a.AGI));

        Debug.Log("[TurnManager] 回合顺序（按AGI排序）：");
        for (int i = 0; i < turnOrder.Count; i++)
        {
            Debug.Log($"{i + 1}. {turnOrder[i].unitName} (AGI: {turnOrder[i].AGI})");
        }
    }

    void StartRound()
    {
        currentTurnIndex = 0;
        BattleLog.Add($"===== 第 {roundCount} 轮开始 =====");
        StartNextTurn();
    }

    void StartNextTurn()
    {
        // 战斗已结束，停止轮转回合
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm != null && bm.IsBattleOver)
            return;

        if (currentTurnIndex >= turnOrder.Count)
        {
            roundCount++;
            StartRound();
            return;
        }

        currentUnit = turnOrder[currentTurnIndex];

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
        bool alive = currentUnit.ProcessBleed();
        if (!alive)
        {
            BattleLog.Add($"{currentUnit.unitName} 因流血而死亡！");
            BattleManager bmCheck = FindObjectOfType<BattleManager>();
            if (bmCheck != null)
            {
                bool allEnemyDead = bmCheck.GetFirstAlive(bmCheck.enemyUnits) == null;
                bool allPlayerDead = bmCheck.GetFirstAlive(bmCheck.playerUnits) == null;
                if (allEnemyDead || allPlayerDead)
                {
                    currentTurnIndex++;
                    StartNextTurn();
                    return;
                }
            }
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        // 压力阈值检析（心脏衰竭优先，然后崩溃/美德判定）
        StressManager.CheckResolve(currentUnit, bm);

        if (currentUnit.currentHP <= 0)
        {
            BattleLog.Add($"{currentUnit.unitName} 在压力判定后死亡！");
            bool allEnemyDead = bm.GetFirstAlive(bm.enemyUnits) == null;
            bool allPlayerDead = bm.GetFirstAlive(bm.playerUnits) == null;
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

        if (!isPlayerTurn)
        {
            StartCoroutine(EnemyTurn());
        }
    }

    public void EndTurn()
    {
        Debug.Log($"[TurnManager] {currentUnit.unitName} 结束回合");
        currentUnit.TickStatusEffects();
        currentUnit.TickQuirkCooldowns();
        StressManager.TickBreakdown(currentUnit);
        currentTurnIndex++;
        StartNextTurn();
    }

    IEnumerator EnemyTurn()
    {
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm == null)
        {
            EndTurn();
            yield break;
        }

        yield return EnemyAIEngine.ExecuteTurn(currentUnit, bm, this, () =>
        {
            EndTurn();
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
        if (front.Count > 0) return front[Random.Range(0, front.Count)];
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
