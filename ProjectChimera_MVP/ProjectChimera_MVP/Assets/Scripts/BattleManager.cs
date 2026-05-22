using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum BattleInputState
{
    Normal,        // 空闲：可攻击/选技能/结束回合
    SkillSelected, // 技能已选，等待进入选目标
    Targeting      // 正在选目标：Tab 切换，空格确认
}

public class BattleManager : MonoBehaviour
{
    public GameObject damagePopupPrefab;

    [Header("编队")]
    public List<UnitData> playerUnits = new List<UnitData>();
    public List<UnitData> enemyUnits = new List<UnitData>();

    [Header("受击效果设置")]
    public float jumpHeight = 0.3f;
    public float jumpTime = 0.1f;
    public float fallTime = 0.2f;

    [Header("胜利失败 UI")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryTitleText;

    [Header("技能栏 UI")]
    public UISkillBarController skillBarController;  // 技能栏控制器


    // 状态
    private Dictionary<UnitData, Vector3> originalPositions = new Dictionary<UnitData, Vector3>();
    private Dictionary<UnitData, Color> originalColors = new Dictionary<UnitData, Color>();
    private bool battleOver;
    public bool IsBattleOver => battleOver;
    private string battleResult;
    private Coroutine hitFeedbackRoutine;

    // 回合追踪
    private UnitData lastTurnUnit;       // 上一轮行动的单位
    private UnitData currentSelectedUnit; // 当前选中的单位（高亮）

    // 技能输入状态
    private BattleInputState inputState = BattleInputState.Normal;
    private bool isInTargetingMode;              // 是否处于选目标模式
    private int selectedSkillIndex = -1;
    private SkillData pendingSkill;
    private List<UnitData> validTargets;
    private int targetCycleIndex;
    private UnitData highlightedTarget;

    void Start()
    {
        BattleLog.Clear();

        // 初始隐藏胜利/失败面板
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        // 自动寻找 UI 控制器
        if (skillBarController == null)
            skillBarController = FindObjectOfType<UISkillBarController>(true);

        // 订阅单位点击事件（鼠标选目标 + 面板触发）
        UnitClickDetector.OnUnitClicked += OnUnitClicked;

        foreach (var unit in playerUnits)
        {
            if (unit != null)
            {
                originalPositions[unit] = unit.transform.position;
                var sr = unit.GetComponent<SpriteRenderer>();
                if (sr != null) originalColors[unit] = sr.color;
            }
        }
        foreach (var unit in enemyUnits)
        {
            if (unit != null)
            {
                originalPositions[unit] = unit.transform.position;
                var sr = unit.GetComponent<SpriteRenderer>();
                if (sr != null) originalColors[unit] = sr.color;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearHighlights();
            ReturnToMap();
            return;
        }

        if (battleOver) return;
        if (TurnManager.Instance == null) return;

        var current = TurnManager.Instance.currentUnit;
        if (current != lastTurnUnit)
        {
            lastTurnUnit = current;

            if (current != null && current.IsBreakdownActive() && StressManager.ShouldSkipTurn(current))
            {
                BattleLog.Add($"[{current.unitName}] 处于崩溃状态，行动跳过");
                if (skillBarController != null)
                    skillBarController.RefreshSkills(null);
                EndPlayerTurn();
                return;
            }

            if (skillBarController != null)
                skillBarController.RefreshSkills(lastTurnUnit);
        }

        if (!TurnManager.Instance.isPlayerTurn)
        {
            if (skillBarController != null)
                skillBarController.RefreshSkills(null);
            return;
        }

        switch (inputState)
        {
            case BattleInputState.Normal: HandleNormalInput(); break;
            case BattleInputState.SkillSelected: HandleSkillSelectedInput(); break;
            case BattleInputState.Targeting: HandleTargetingInput(); break;
        }
    }

    // ==================== 输入处理 ====================

    void HandleNormalInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DoBasicAttack();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            EndPlayerTurn();
        }
        else
        {
            CheckSkillKeyPress();
        }
    }

    void HandleSkillSelectedInput()
    {
        EnterTargetingMode();
    }

    void HandleTargetingInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleTarget();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmTarget();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            CancelTargeting();
        }
    }

    /// <summary>供 UI 按钮调用的公共方法：选择技能</summary>
    // ==================== 鼠标点击选目标 ====================

    void OnDestroy()
    {
        UnitClickDetector.OnUnitClicked -= OnUnitClicked;
    }

    /// <summary>鼠标点击单位时触发：选目标 / 选中取消 Toggle</summary>
    void OnUnitClicked(UnitData clickedUnit)
    {
        if (clickedUnit == null || battleOver) return;

        Debug.Log($"OnUnitClicked: {clickedUnit.unitName}, isInTargetingMode={isInTargetingMode}, inputState={inputState}, currentSelectedUnit={currentSelectedUnit?.unitName}");

        // ===== 选目标阶段：固定高亮，不做 Toggle =====
        if (isInTargetingMode)
        {
            if (IsValidTarget(clickedUnit))
            {
                targetCycleIndex = validTargets.IndexOf(clickedUnit);
                ConfirmTarget();
                return;
            }
            Debug.Log($"[选目标] {clickedUnit.unitName} 不是当前技能的合法目标");
            return;
        }

        // ===== 非选目标阶段：Toggle 选中/取消 =====
        if (currentSelectedUnit == clickedUnit)
        {
            currentSelectedUnit.SetHighlight(false);
            currentSelectedUnit = null;
        }
        else
        {
            if (currentSelectedUnit != null)
                currentSelectedUnit.SetHighlight(false);
            currentSelectedUnit = clickedUnit;
            currentSelectedUnit.SetHighlight(true);
        }
    }

    public void SelectSkill(int skillIndex)
    {
        UnitData attacker = TurnManager.Instance?.currentUnit;
        if (attacker == null || skillIndex < 0 || skillIndex >= attacker.skills.Count)
        {
            Debug.LogWarning($"[技能] 无效的技能索引: {skillIndex}");
            return;
        }

        selectedSkillIndex = skillIndex;
        pendingSkill = attacker.skills[skillIndex];
        inputState = BattleInputState.SkillSelected;
        Debug.Log($"[技能] 选中 {pendingSkill.skillName}");

        // 立即进入选目标模式，不等下一帧 Update
        EnterTargetingMode();
    }

    void CheckSkillKeyPress()
    {
        UnitData attacker = TurnManager.Instance.currentUnit;
        if (attacker == null || attacker.skills.Count == 0) return;

        int key = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) key = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) key = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) key = 2;

        if (key >= 0 && key < attacker.skills.Count)
        {
            selectedSkillIndex = key;
            pendingSkill = attacker.skills[key];
            inputState = BattleInputState.SkillSelected;
            Debug.Log($"[技能] 选中 {pendingSkill.skillName}");
        }
    }

    // ==================== 暗黑地牢风格选目标 ====================

    /// <summary>当前是否处于选目标模式</summary>
    public bool IsInTargetingMode()
    {
        return inputState == BattleInputState.Targeting;
    }

    /// <summary>判断某单位是否当前技能的合法目标</summary>
    public bool IsValidTarget(UnitData unit)
    {
        if (unit == null || unit.currentHP <= 0) return false;
        if (pendingSkill == null) return false;

        bool result;
        switch (pendingSkill.targetType)
        {
            case SkillTargetType.SingleEnemy:
                result = enemyUnits.Contains(unit);
                break;
            case SkillTargetType.SingleAlly:
                result = playerUnits.Contains(unit);
                break;
            case SkillTargetType.AllEnemies:
                result = enemyUnits.Contains(unit);
                break;
            case SkillTargetType.Self:
                result = unit == TurnManager.Instance?.currentUnit;
                break;
            default:
                result = false;
                break;
        }
        Debug.Log($"[IsValidTarget] {unit.unitName} → {result} (targetType: {pendingSkill.targetType})");
        return result;
    }

    /// <summary>进入选目标模式：计算合法目标，设置 Dimmed 状态</summary>
    void EnterTargetingMode()
    {
        Debug.Log("EnterTargetingMode 被调用");
        UnitData attacker = TurnManager.Instance.currentUnit;
        if (attacker == null || pendingSkill == null) { CancelSkillSelection(); return; }

        validTargets = new List<UnitData>();
        switch (pendingSkill.targetType)
        {
            case SkillTargetType.SingleEnemy:
                validTargets = enemyUnits.Where(u => u.currentHP > 0).ToList();
                break;
            case SkillTargetType.SingleAlly:
                validTargets = playerUnits.Where(u => u.currentHP > 0).ToList();
                break;
            default:
                // AllEnemies / Self → 直接释放
                ExecuteSkill(attacker, pendingSkill, null);
                ExitTargetingMode();
                if (!battleOver)
                    EndPlayerTurn();
                return;
        }

        if (validTargets.Count == 0)
        {
            Debug.Log("[技能] 没有合法目标！");
            CancelSkillSelection();
            return;
        }

        isInTargetingMode = true;
        Debug.Log($"EnterTargetingMode 完成，isInTargetingMode={isInTargetingMode}, 技能={pendingSkill?.skillName}");

        // 预选第一个合法目标
        targetCycleIndex = 0;

        // 所有单位设置 Dimmed，合法目标恢复 Normal，第一个预选为 Highlighted
        ApplyTargetingOverlay();

        Debug.Log($"[选目标] 合法目标数: {validTargets.Count}");
        foreach (var t in validTargets)
            Debug.Log($"[选目标]  → {t.unitName}");

        inputState = BattleInputState.Targeting;
    }

    /// <summary>对所有单位应用选目标时的覆盖层状态</summary>
    void ApplyTargetingOverlay()
    {
        // 玩家方统一 Normal（不变暗）
        foreach (var u in playerUnits)
        {
            if (u != null)
                u.SetHighlightState(UnitData.HighlightState.Normal);
        }

        if (validTargets == null) return;

        // 当前预选的目标
        UnitData currentHighlight = (targetCycleIndex >= 0 && targetCycleIndex < validTargets.Count)
            ? validTargets[targetCycleIndex] : null;

        // 敌方：合法目标 → Normal/Highlighted，非法目标 → Dimmed
        foreach (var u in enemyUnits)
        {
            if (u == null) continue;
            if (validTargets.Contains(u))
            {
                if (u == currentHighlight)
                    u.SetHighlightState(UnitData.HighlightState.Highlighted);
                else
                    u.SetHighlightState(UnitData.HighlightState.Normal);
            }
            else
            {
                u.SetHighlightState(UnitData.HighlightState.Dimmed);
            }
        }
    }

    /// <summary>退出选目标模式：清除所有覆盖层</summary>
    void ExitTargetingMode()
    {
        isInTargetingMode = false;
        foreach (var u in playerUnits)
        {
            if (u != null)
                u.SetHighlightState(UnitData.HighlightState.Normal);
        }
        foreach (var u in enemyUnits)
        {
            if (u != null)
                u.SetHighlightState(UnitData.HighlightState.Normal);
        }
        ClearHighlights();
        inputState = BattleInputState.Normal;
        selectedSkillIndex = -1;
        pendingSkill = null;
    }

    void CycleTarget()
    {
        if (validTargets == null || validTargets.Count == 0) return;
        ClearHighlight(highlightedTarget);
        targetCycleIndex = (targetCycleIndex + 1) % validTargets.Count;
        HighlightTarget(validTargets[targetCycleIndex]);
        ApplyTargetingOverlay();
    }

    void ConfirmTarget()
    {
        UnitData attacker = TurnManager.Instance.currentUnit;
        if (attacker == null || pendingSkill == null) return;

        UnitData target = validTargets != null && targetCycleIndex < validTargets.Count
            ? validTargets[targetCycleIndex] : null;

        // 在 ExitTargetingMode 清空 pendingSkill 之前，先保存引用
        SkillData skillToExecute = pendingSkill;
        ExitTargetingMode();
        ExecuteSkill(attacker, skillToExecute, target);

        if (!battleOver)
            EndPlayerTurn();
    }

    void CancelTargeting()
    {
        ExitTargetingMode();
        inputState = BattleInputState.SkillSelected;
    }

    void CancelSkillSelection()
    {
        ExitTargetingMode();
    }

    // ==================== 目标高亮 ====================

    void HighlightTarget(UnitData unit)
    {
        highlightedTarget = unit;
        var sr = unit.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;
    }

    void ClearHighlight(UnitData unit)
    {
        if (unit == null) return;
        var sr = unit.GetComponent<SpriteRenderer>();
        if (sr != null && originalColors.ContainsKey(unit))
            sr.color = originalColors[unit];
    }

    void ClearHighlights()
    {
        if (highlightedTarget != null)
        {
            ClearHighlight(highlightedTarget);
            highlightedTarget = null;
        }
    }

    // ==================== 执行技能 ====================

    public void ExecuteSkill(UnitData attacker, SkillData skill, UnitData target)
    {
        if (attacker == null) return;

        if (skill.commands != null && skill.commands.Count > 0)
        {
            CommandExecutor.Execute(attacker, skill, target, this, playerUnits, enemyUnits);
        }
    }

    // ==================== 基础攻击 ====================

    void DoBasicAttack()
    {
        UnitData attacker = TurnManager.Instance.currentUnit;
        if (attacker == null) return;
        if (!playerUnits.Contains(attacker)) return;

        UnitData target = GetFirstAlive(enemyUnits);
        if (target == null)
        {
            battleOver = true;
            BattleLog.Add("【胜利】所有敌人被击败！");
            return;
        }

        int rawDmg = attacker.GetEffectiveSTR() + attacker.weaponAttack;
        float strCoeff = attacker.GetStrengthCoefficient();
        int damage = Mathf.Max(1, Mathf.RoundToInt(rawDmg * strCoeff - target.GetEffectiveDEF()));
        DealDamage(attacker, target, damage);

        if (!battleOver)
            EndPlayerTurn();
    }

    void EndPlayerTurn()
    {
        TurnManager.Instance?.EndTurn();
    }

    // ==================== 公共伤害方法 ====================

    public void DealDamage(UnitData attacker, UnitData target, int damage)
    {
        StatusEffect protect = target.GetStatus(StatusType.Protected);
        if (protect != null)
        {
            UnitData protector = playerUnits.Find(u => u.unitName == protect.sourceName);
            if (protector != null && protector.currentHP > 0 && protector != target)
            {
                BattleLog.Add($"[援护] {protector.unitName} 替 {target.unitName} 承受了 {damage} 点伤害");
                target = protector;
            }
        }

        // 护盾吸收
        if (target.shieldHP > 0 && damage > 0)
        {
            int absorbed = Mathf.Min(target.shieldHP, damage);
            target.shieldHP -= absorbed;
            damage -= absorbed;
            BattleLog.Add($"[护盾] {target.unitName} 的护盾吸收了 {absorbed} 点伤害");
            if (target.shieldHP <= 0 && damage > 0)
                BattleLog.Add($"[护盾] {target.unitName} 的护盾破碎了");
        }

        target.currentHP -= damage;
        target.UpdateHPUI();
        if (damage > 0)
        {
            BattleLog.Add($"{attacker.unitName} 对 {target.unitName} 造成 {damage} 点伤害！");
            StressManager.AddStress(target, StressManager.config.onTakeDamage, StressTag.Combat);
        }

        if (damagePopupPrefab != null)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * 1.5f;
            GameObject popupGo = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
            DamagePopup popupScript = popupGo.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(damage);
        }

        if (hitFeedbackRoutine != null) StopCoroutine(hitFeedbackRoutine);
        if (originalPositions.TryGetValue(target, out Vector3 origPos))
            hitFeedbackRoutine = StartCoroutine(HitFeedbackEffect(target.transform, origPos));

        if (target.currentHP <= 0)
        {
            target.currentHP = 0;
            target.UpdateHPUI();
            BattleLog.Add($"{target.unitName} 被击败！");

            var allies = enemyUnits.Contains(target) ? playerUnits : enemyUnits;
            foreach (var ally in allies)
            {
                if (ally.currentHP > 0)
                    StressManager.AddStress(ally, StressManager.config.onAllyDeath, StressTag.AllyDown);
            }

            if (enemyUnits.Contains(target))
            {
                int expReward = 50 * target.level;
                foreach (var pu in playerUnits)
                {
                    if (pu.currentHP > 0)
                        pu.GainExp(expReward);
                }
            }

            bool allEnemyDead = GetFirstAlive(enemyUnits) == null;
            bool allPlayerDead = GetFirstAlive(playerUnits) == null;

            if (allEnemyDead)
            {
                battleOver = true;
                battleResult = "胜利！🎉";
                BattleLog.Add("【胜利】战斗胜利！");
                ShowVictoryPanel();
            }
            else if (allPlayerDead)
            {
                battleOver = true;
                battleResult = "失败... 💀";
                BattleLog.Add("【失败】全军覆没！");
                ShowVictoryPanel();
            }
        }
    }

    void ShowVictoryPanel()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryTitleText != null)
            {
                victoryTitleText.text = battleResult;
                victoryTitleText.color = battleResult.Contains("胜利") ? Color.yellow : Color.red;
            }
        }
    }

    public UnitData GetFirstAlive(List<UnitData> units)
    {
        foreach (var u in units)
            if (u.currentHP > 0) return u;
        return null;
    }

    // ==================== 受击动画 ====================

    IEnumerator HitFeedbackEffect(Transform target, Vector3 originalPos)
    {
        target.position = originalPos;

        float timer = 0;
        while (timer < jumpTime)
        {
            timer += Time.deltaTime;
            target.position = Vector3.Lerp(originalPos, originalPos + Vector3.up * jumpHeight, timer / jumpTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.01f);

        timer = 0;
        while (timer < fallTime)
        {
            timer += Time.deltaTime;
            target.position = Vector3.Lerp(originalPos + Vector3.up * jumpHeight, originalPos, timer / fallTime);
            yield return null;
        }

        target.position = originalPos;
    }

    public void ReturnToMap()
    {
        if (GameManager.Instance != null)
        {
            var gm = GameManager.Instance;
            for (int i = 0; i < playerUnits.Count && i < gm.playerTeamData.Count; i++)
            {
                var data = gm.playerTeamData[i];
                var unit = playerUnits[i];
                data.level = unit.level;
                data.currentExp = unit.currentExp;
                data.VIT = unit.VIT;
                data.STR = unit.STR;
                data.DEF = unit.baseDefense;
                data.AGI = unit.AGI;
                data.INT = unit.INT;
                data.weaponAttack = unit.weaponAttack;
                data.stress = unit.stress;
            }
            gm.ReturnToWorldMap();
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ==================== UI ====================


}
