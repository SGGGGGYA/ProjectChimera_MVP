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
    private int roundCount = 1;

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

        isPlayerTurn = currentUnit.isPlayer;

        BattleLog.Add($"回合: {currentUnit.unitName}");

        if (!isPlayerTurn)
        {
            StartCoroutine(EnemyTurn());
        }
    }

    public void EndTurn()
    {
        Debug.Log($"[TurnManager] {currentUnit.unitName} 结束回合");
        currentUnit.TickStatusEffects();
        StressManager.TickBreakdown(currentUnit);
        currentTurnIndex++;
        StartNextTurn();
    }

    IEnumerator EnemyTurn()
    {
        Debug.Log($"[TurnManager] {currentUnit.unitName} 正在思考...");
        yield return new WaitForSeconds(1f);

        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm != null)
        {
            MentalState mentalState = currentUnit.GetMentalState();

            // Afflicted: 50%概率随机攻击本方或对方
            if (mentalState == MentalState.Afflicted && Random.value < 0.5f)
            {
                var allTargets = new List<UnitData>();
                allTargets.AddRange(bm.playerUnits.FindAll(u => u.currentHP > 0));
                allTargets.AddRange(bm.enemyUnits.FindAll(u => u.currentHP > 0 && u != currentUnit));
                if (allTargets.Count > 0)
                {
                    UnitData randomTarget = allTargets[Random.Range(0, allTargets.Count)];
                    int dmg = Mathf.Max(1, currentUnit.GetEffectiveSTR() + currentUnit.weaponAttack - randomTarget.GetEffectiveDEF());
                    BattleLog.Add($"<color=red>[崩溃 AI] {currentUnit.unitName} 失控攻击了 {randomTarget.unitName}！</color>");
                    bm.DealDamage(currentUnit, randomTarget, dmg);
                    yield return new WaitForSeconds(0.5f);
                    EndTurn();
                    yield break;
                }
            }

            // 检查嘲讽
            UnitData target = null;
            StatusEffect taunt = currentUnit.GetStatus(StatusType.Taunt);
            if (taunt != null)
            {
                target = bm.playerUnits.Find(u => u.unitName == taunt.sourceName && u.currentHP > 0);
                if (target != null)
                    BattleLog.Add($"[嘲讽] {currentUnit.unitName} 被嘲讽，强制攻击 {target.unitName}");
            }

            UnitData chosenTarget = null;
            SkillData chosenSkill = null;

            switch (currentUnit.unitName)
            {
                case "哥布林萨满":
                    ExecuteShamanAI(bm, out chosenTarget, out chosenSkill);
                    break;

                case "野狼":
                    ExecuteWolfAI(bm, out chosenTarget, out chosenSkill);
                    break;

                default:
                    chosenTarget = target ?? bm.GetFirstAlive(bm.playerUnits);
                    if (currentUnit.skills.Count > 0 && Random.value < 0.5f)
                    {
                        int skillIdx = Random.Range(0, currentUnit.skills.Count);
                        chosenSkill = currentUnit.skills[skillIdx];
                    }
                    break;
            }

            if (chosenTarget != null)
            {
                if (chosenSkill != null)
                {
                    BattleLog.Add($"[敌人] {currentUnit.unitName} 使用 [{chosenSkill.skillName}]");
                    bm.ExecuteSkill(currentUnit, chosenSkill, chosenTarget);
                }
                else
                {
                    int rawDmg = currentUnit.GetEffectiveSTR() + currentUnit.weaponAttack;
                    float strCoeff = currentUnit.GetStrengthCoefficient();
                    int damage = Mathf.Max(1, Mathf.RoundToInt(rawDmg * strCoeff - chosenTarget.GetEffectiveDEF()));
                    bm.DealDamage(currentUnit, chosenTarget, damage);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        EndTurn();
    }

    // ==================== 哥布林萨满 AI ====================

    void ExecuteShamanAI(BattleManager bm, out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        // 获取所有活着的友方
        var allies = bm.enemyUnits.Where(u => u.currentHP > 0 && u != currentUnit).ToList();

        // 情况1：有队友血量低于50% → 治疗血最少的
        var lowHpAlly = allies.Where(u => (float)u.currentHP / u.MaxHp < 0.5f)
                              .OrderBy(u => (float)u.currentHP / u.MaxHp)
                              .FirstOrDefault();
        if (lowHpAlly != null)
        {
            skill = currentUnit.skills.Find(s => s.aiCategory == SkillEffectType.Heal);
            target = lowHpAlly;
            if (skill != null) return;
        }

        // 情况2a：有玩家压力超过50 → 恐吓施加压力
        var highStressPlayer = bm.playerUnits.Where(u => u.currentHP > 0 && u.stress >= 50)
                                              .OrderByDescending(u => u.stress)
                                              .FirstOrDefault();
        if (highStressPlayer != null)
        {
            skill = currentUnit.skills.Find(s => s.aiCategory == SkillEffectType.Stress);
            if (skill != null)
            {
                target = highStressPlayer;
                return;
            }
        }

        // 情况2b：队友都健康且有多个敌人 → 给随机队友上护盾
        if (allies.Count > 0)
        {
            skill = currentUnit.skills.Find(s => s.aiCategory == SkillEffectType.Shield);
            if (skill != null)
            {
                target = allies[Random.Range(0, allies.Count)];
                return;
            }
        }

        // 情况3：只剩自己或没有治疗/护盾/压力技能 → 火球打血最少玩家
        skill = currentUnit.skills.Find(s => s.aiCategory == SkillEffectType.DirectDamage
                                          || s.skillName == "火球术");
        var players = bm.playerUnits.Where(u => u.currentHP > 0)
                                    .OrderBy(u => u.currentHP)
                                    .ToList();
        target = players.FirstOrDefault();
    }

    // ==================== 野狼 AI ====================

    void ExecuteWolfAI(BattleManager bm, out UnitData target, out SkillData skill)
    {
        target = null;
        skill = null;

        // 狼总是用撕咬
        skill = currentUnit.skills.Find(s => s.skillName == "撕咬");
        if (skill == null) return;

        // 优先攻击有标记或流血的目标（嗜血被动+20%伤害，在 skill 层面处理）
        // 如果技能有 Bleed，先找有 Mark/Bleed 的玩家
        var players = bm.playerUnits.Where(u => u.currentHP > 0).ToList();

        var priorityTarget = players.FirstOrDefault(u =>
            u.HasStatus(StatusType.Mark) || u.HasStatus(StatusType.Bleed));

        if (priorityTarget != null)
        {
            target = priorityTarget;
            BattleLog.Add($"[嗜血] 野狼感知到 {target.unitName} 身上的伤口，伤害提升20%");
            // 临时提升技能伤害 20%
            skill.baseDamage = Mathf.RoundToInt(skill.baseDamage * 1.2f);
            skill.strScaling *= 1.2f;
        }
        else
        {
            // 没有标记目标 → 攻击前排（战士优先）
            target = players.FirstOrDefault(u => u.unitName.Contains("战士")) ?? players[0];
        }
    }

    public bool CanCurrentUnitAct()
    {
        return currentUnit != null && currentUnit.currentHP > 0;
    }
}
