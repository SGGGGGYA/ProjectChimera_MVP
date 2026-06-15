using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using ProjectChimera.Core;

public enum BattleInputState
{
    Normal,        // 空闲：可攻击/选技能/结束回合
    SkillSelected, // 技能已选，等待进入选目标
    Targeting      // 正在选目标：Tab 切换，空格确认
}

public class BattleManager : MonoBehaviour, IBattleContext
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

    /// <summary>战斗初始化完成标志：只有 InitializeBattle 完成后才能触发结束面板</summary>
    [System.NonSerialized] public bool battleReady;

    /// <summary>重置战斗结束状态（BattleSetup 在初始化前调用，防止残留的 battleOver 状态）</summary>
    public void ResetBattleState()
    {
        battleOver = false;
        battleReady = false;
        battleResult = null;
        battleStartTime = Time.time;
        if (victoryPanel != null) victoryPanel.SetActive(false);
        // 清理动态创建的奖励/失败面板（ShowRewardPanel/ShowGameOverPanel 动态生成）
        foreach (var name in new[] { "RewardPanel", "GameOverPanel", "RewardCanvas", "GameOverCanvas" })
        {
            var go = GameObject.Find(name);
            if (go != null) Destroy(go);
        }
    }

    // 回合状态提供者（由 BattleSetup 注入）。生产路径 BattleSetup 一定会先注入；
    // 老测试/编辑器临时用场景中如果没有 BattleSetup，需要在测试 SetUp 里手动赋值。
    [System.NonSerialized]
    public ITurnStateProvider TurnState;

    // 回合追踪
    private UnitData lastTurnUnit;       // 上一轮行动的单位
    private UnitData currentSelectedUnit; // 当前选中的单位（高亮）

    // 技能输入状态
    private BattleInputState inputState = BattleInputState.Normal;
    private bool isInTargetingMode;
    private int selectedSkillIndex = -1;
    private SkillData pendingSkill;
    private List<UnitData> validTargets;
    private int targetCycleIndex;
    private UnitData highlightedTarget;

    // 战斗中物品使用
    private List<ItemStack> battleConsumables;
    private ItemDefinition pendingItemDef;
    private ItemStack pendingItemStack;
    private bool isSelectingItem;

    // 目标指示器
    private GameObject targetArrow;
    private Canvas targetArrowCanvas;

    // 战后奖励追踪
    private int totalExpPerUnit;
    private int goldEarned;
    private List<ItemStack> pendingDrops = new List<ItemStack>();
    private List<string> lootDisplayLines;

    private float battleStartTime; // 战斗开始时间，用于防早期误触发
    private bool rewardCollected;

    void Start()
    {
        BattleLog.Clear();

        totalExpPerUnit = 0;
        goldEarned = 0;
        pendingDrops.Clear();
        lootDisplayLines = null;
        rewardCollected = false;

        VFXManager.Initialize(this);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioKeys.BGM_BATTLE);

        // 初始隐藏胜利/失败面板
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        // 自动生成回合数 UI
        if (FindObjectOfType<UIRoundCounter>() == null)
        {
            var rcGO = new GameObject("UIRoundCounter");
            rcGO.AddComponent<UIRoundCounter>();
        }

        // 自动寻找 UI 控制器
        if (skillBarController == null)
            skillBarController = FindObjectOfType<UISkillBarController>(true);

        // 订阅单位点击事件（鼠标选目标 + 面板触发）
        UnitClickDetector.OnUnitClicked += OnUnitClicked;

        // 触发 BattleStart 特质
        foreach (var unit in playerUnits.ToList())
            QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);
        foreach (var unit in enemyUnits.ToList())
            QuirkTriggerSystem.CheckTriggers(unit, QuirkTriggerType.BattleStart);

        foreach (var unit in playerUnits.ToList())
        {
            if (unit != null)
            {
                originalPositions[unit] = unit.transform.position;
                var sr = unit.GetComponent<SpriteRenderer>();
                if (sr != null) originalColors[unit] = sr.color;
            }
        }
        foreach (var unit in enemyUnits.ToList())
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
            if (battleOver)
            {
                // 游戏结束 → 返回主菜单；胜利后必须点"继续"按钮
                if (battleResult != null && battleResult.Contains("失败"))
                    ReturnToMenu();
                return;
            }
            else
                AttemptRetreat();
            return;
        }

        // 等待战利品面板的"继续"按钮
        if (battleOver && battleResult != null && battleResult.Contains("胜利"))
        {
            if (rewardCollected)
                ReturnToMap();
            return;
        }

        if (battleOver) return;
        if (TurnState == null) return;

        var current = TurnState.CurrentUnit;
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

        if (!TurnState.IsPlayerTurn)
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
        if (isSelectingItem)
        {
            HandleItemSelectionKeys();
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            CyclePlayerSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            CyclePlayerSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            DoBasicAttack();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            EndPlayerTurn();
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            ShowBattleItems();
        }
        else
        {
            CheckSkillKeyPress();
        }
    }

    void CyclePlayerSelection(int direction)
    {
        if (playerUnits.Count == 0) return;
        int idx = currentSelectedUnit != null ? playerUnits.IndexOf(currentSelectedUnit) : -1;
        idx = (idx + direction + playerUnits.Count) % playerUnits.Count;
        if (currentSelectedUnit != null)
            currentSelectedUnit.SetHighlight(false);
        currentSelectedUnit = playerUnits[idx];
        currentSelectedUnit.SetHighlight(true);
    }

    // ==================== 战斗物品系统 ====================

    void ShowBattleItems()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        battleConsumables = gm.inventory.FindAll(s =>
        {
            var def = ItemDatabase.Get(s.itemId);
            return def != null && def.category == ItemCategory.Consumable && s.quantity > 0;
        });

        if (battleConsumables.Count == 0)
        {
            BattleLog.Add("[物品] 背包中没有可用消耗品");
            return;
        }

        isSelectingItem = true;
        BattleLog.Add("── 选择物品 ──");
        for (int i = 0; i < battleConsumables.Count; i++)
        {
            var def = ItemDatabase.Get(battleConsumables[i].itemId);
            BattleLog.Add($"[{i+1}] {def?.itemName ?? battleConsumables[i].itemId} x{battleConsumables[i].quantity}");
        }
        BattleLog.Add("按数字键选择，按 E 取消");
    }

    void HandleItemSelectionKeys()
    {
        for (int i = 0; i < battleConsumables.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectBattleItem(i);
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            CancelItemSelection();
        }
    }

    void SelectBattleItem(int index)
    {
        if (index < 0 || index >= battleConsumables.Count) return;

        pendingItemStack = battleConsumables[index];
        pendingItemDef = ItemDatabase.Get(pendingItemStack.itemId);
        if (pendingItemDef == null) return;

        BattleLog.Add($"[物品] 选择: {pendingItemDef.itemName}");
        isSelectingItem = false;

        // 进入选目标模式（自动选择友方）
        pendingSkill = MakeItemSkill(pendingItemDef);
        selectedSkillIndex = -1;
        inputState = BattleInputState.SkillSelected;
        EnterTargetingMode();
    }

    /// <summary>根据物品动态合成一个 SkillData，用于复用选目标系统</summary>
    SkillData MakeItemSkill(ItemDefinition def)
    {
        var skill = new SkillData();
        skill.skillName = def.itemName;
        skill.description = def.description;
        skill.targetType = SkillTargetType.SingleAlly;
        skill.minUserRank = 0; skill.maxUserRank = 3;
        skill.canTargetFrontRank = true; skill.canTargetBackRank = true;

        if (def.healAmount > 0)
            skill.commands = new List<Command> { new HealCommand { baseHeal = def.healAmount } };
        else if (def.stressRelief > 0)
            skill.commands = new List<Command> { new ConsumeResourceCommand { resourceType = ConsumeResourceCommand.ResourceType.Stress, amount = -def.stressRelief } };
        else
            skill.commands = new List<Command> { new HealCommand { baseHeal = 20 } };

        return skill;
    }

    void CancelItemSelection()
    {
        isSelectingItem = false;
        pendingItemDef = null;
        pendingItemStack = null;
        BattleLog.Add("[物品] 取消选择");
    }

    void HandleSkillSelectedInput()
    {
        EnterTargetingMode();
    }

    void HandleTargetingInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.D))
        {
            CycleTarget(1);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            CycleTarget(-1);
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

    void OnEnable()
    {
        BattleEvents.OnBattleEnded += OnBattleEnded;
    }

    void OnDisable()
    {
        BattleEvents.OnBattleEnded -= OnBattleEnded;
    }

    void OnDestroy()
    {
        UnitClickDetector.OnUnitClicked -= OnUnitClicked;
        if (targetArrow != null) Destroy(targetArrow);
    }

    void OnBattleEnded(bool isVictory)
    {
        if (battleOver) return;
        // 防初始化误触发：TurnManager 就绪后才能响应结束事件
        if (!battleReady)
        {
            Debug.LogWarning($"[BattleManager] 忽略过早的 BattleEnded 事件 (battleReady=false, isVictory={isVictory})");
            return;
        }
        battleOver = true;
        battleResult = isVictory ? "胜利！" : "失败...";
        if (isVictory)
        {
            BattleLog.Add("【胜利】战斗胜利！");
            ShowRewardPanel();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_VICTORY);
                AudioManager.Instance.StartCoroutine(DelayedBGMChange(AudioKeys.BGM_MAP, 1.5f));
            }
        }
        else
        {
            BattleLog.Add("【失败】全军覆没！");
            ShowGameOverPanel();
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_DEFEAT);
        }
    }

    /// <summary>鼠标点击单位时触发：选目标 / 选中取消 Toggle</summary>
    void OnUnitClicked(UnitData clickedUnit)
    {
        if (clickedUnit == null || battleOver) return;

        Log.Info($"OnUnitClicked: {clickedUnit.unitName}, isInTargetingMode={isInTargetingMode}, inputState={inputState}, currentSelectedUnit={currentSelectedUnit?.unitName}");

        // ===== 选目标阶段：固定高亮，不做 Toggle =====
        if (isInTargetingMode)
        {
            if (IsValidTarget(clickedUnit))
            {
                int idx = validTargets != null ? validTargets.IndexOf(clickedUnit) : -1;
                if (idx < 0) return;
                targetCycleIndex = idx;
                ConfirmTarget();
                return;
            }
            Log.Info($"[选目标] {clickedUnit.unitName} 不是当前技能的合法目标");
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
        UnitData attacker = TurnState?.CurrentUnit;
        if (attacker == null || skillIndex < 0 || skillIndex >= attacker.skills.Count)
        {
            Log.Warn($"[技能] 无效的技能索引: {skillIndex}");
            return;
        }

        var skill = attacker.skills[skillIndex];
        if (!IsSkillUsableFromRank(attacker, skill))
        {
            BattleLog.Add($"[阵型] {attacker.unitName} 在站位 {attacker.rank} 无法使用 [{skill.skillName}]");
            return;
        }

        selectedSkillIndex = skillIndex;
        pendingSkill = skill;
        inputState = BattleInputState.SkillSelected;
        Log.Info($"[技能] 选中 {pendingSkill.skillName}");

        // 立即进入选目标模式，不等下一帧 Update
        EnterTargetingMode();
    }

    void CheckSkillKeyPress()
    {
        UnitData attacker = TurnState.CurrentUnit;
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
            Log.Info($"[技能] 选中 {pendingSkill.skillName}");
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
                result = enemyUnits.Contains(unit) && IsRankValidTarget(unit, pendingSkill);
                break;
            case SkillTargetType.SingleAlly:
                result = playerUnits.Contains(unit) && IsRankValidTarget(unit, pendingSkill);
                break;
            case SkillTargetType.AllEnemies:
                result = enemyUnits.Contains(unit);
                break;
            case SkillTargetType.Self:
                result = unit == TurnState?.CurrentUnit;
                break;
            default:
                result = false;
                break;
        }
        return result;
    }

    bool IsRankValidTarget(UnitData unit, SkillData skill)
    {
        bool isFront = unit.rank <= 1;
        if (isFront && !skill.canTargetFrontRank) return false;
        if (!isFront && !skill.canTargetBackRank) return false;
        return true;
    }

    bool IsSkillUsableFromRank(UnitData user, SkillData skill)
    {
        if (skill == null) return true;
        return user.rank >= skill.minUserRank && user.rank <= skill.maxUserRank;
    }

    /// <summary>进入选目标模式：计算合法目标，设置 Dimmed 状态</summary>
    void EnterTargetingMode()
    {
        Log.Info("EnterTargetingMode 被调用");
        UnitData attacker = TurnState.CurrentUnit;
        if (attacker == null || pendingSkill == null) { CancelSkillSelection(); return; }

        if (!IsSkillUsableFromRank(attacker, pendingSkill))
        {
            BattleLog.Add($"[阵型] {attacker.unitName} 在站位 {attacker.rank} 无法使用 [{pendingSkill.skillName}]");
            CancelSkillSelection();
            return;
        }

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

        // 阵型过滤
        if (pendingSkill.targetType != SkillTargetType.AllEnemies)
        {
            validTargets = validTargets.Where(u => IsRankValidTarget(u, pendingSkill)).ToList();
        }

        if (validTargets.Count == 0)
        {
            Log.Info("[技能] 没有合法目标！");
            CancelSkillSelection();
            return;
        }

        isInTargetingMode = true;
        Log.Info($"EnterTargetingMode 完成，isInTargetingMode={isInTargetingMode}, 技能={pendingSkill?.skillName}");

        // 预选第一个合法目标
        targetCycleIndex = 0;

        // 所有单位设置 Dimmed，合法目标恢复 Normal，第一个预选为 Highlighted
        ApplyTargetingOverlay();

        Log.Info($"[选目标] 合法目标数: {validTargets.Count}");
        foreach (var t in validTargets)
            Log.Info($"[选目标]  → {t.unitName}");

        inputState = BattleInputState.Targeting;
    }

    /// <summary>对所有单位应用选目标时的覆盖层状态</summary>
    void ApplyTargetingOverlay()
    {
        // 玩家方统一 Normal（不变暗）
        foreach (var u in playerUnits.ToList())
        {
            if (u != null)
                u.SetHighlightState(UnitData.HighlightState.Normal);
        }

        if (validTargets == null) return;

        // 重置所有 selectCircle（先将它们全部禁用）
        foreach (var u in enemyUnits.ToList())
        {
            if (u != null)
                UpdateTargetHighlight(u, false, false);
        }
        foreach (var u in playerUnits.ToList())
        {
            if (u != null)
                UpdateTargetHighlight(u, false, false);
        }

        // 当前预选的目标
        UnitData currentHighlight = (targetCycleIndex >= 0 && targetCycleIndex < validTargets.Count)
            ? validTargets[targetCycleIndex] : null;

        // 敌方：合法目标 → 显示光圈，非法目标 → Dimmed
        foreach (var u in enemyUnits.ToList())
        {
            if (u == null) continue;
            if (validTargets.Contains(u))
            {
                if (u == currentHighlight)
                {
                    u.SetHighlightState(UnitData.HighlightState.Highlighted);
                    UpdateTargetHighlight(u, true, true);
                    ShowTargetArrow(u, true); // DD1 高亮箭头: dd1_target_h_{rank+1}
                }
                else
                {
                    u.SetHighlightState(UnitData.HighlightState.Normal);
                    UpdateTargetHighlight(u, true, false);
                }
            }
            else
            {
                u.SetHighlightState(UnitData.HighlightState.Dimmed);
            }
        }

        // 玩家方合法目标也显示光圈（技能选队友时）
        foreach (var u in playerUnits.ToList())
        {
            if (u == null) continue;
            if (validTargets.Contains(u))
            {
                bool isCurrent = (u == currentHighlight);
                UpdateTargetHighlight(u, true, isCurrent);
            }
        }
    }

    /// <summary>
    /// 在目标头顶显示 DD1 风格目标箭头，按 rank 自动切换 dd1_target_1~4
    /// </summary>
    void ShowTargetArrow(UnitData target, bool isHighlighted = false)
    {
        if (target == null) return;
        if (targetArrow == null) CreateTargetArrow();
        if (targetArrow == null) return; // 创建失败后静默跳过

        // 按目标 rank 加载对应箭头图片（1~4）
        int arrowIdx = Mathf.Clamp(target.rank + 1, 1, 4);
        string prefix = isHighlighted ? "dd1_target_h" : "dd1_target";
        Sprite arrowSprite = SafeLoadSprite($"UI/Overlays/{prefix}_{arrowIdx}");

        var img = targetArrow.GetComponent<UnityEngine.UI.Image>();
        if (img != null && arrowSprite != null)
        {
            img.sprite = arrowSprite;
            img.color = Color.white;
        }

        if (Camera.main == null) return;
        Vector3 worldPos = target.transform.position + Vector3.up * 1.8f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        var rt = targetArrow.GetComponent<RectTransform>();
        if (rt != null) rt.position = screenPos;
        targetArrow.SetActive(true);
    }

    Sprite SafeLoadSprite(string path)
    {
        var sp = Resources.Load<Sprite>(path);
        if (sp == null)
            Log.Warn($"[DD1Art] 加载失败: {path}，将使用灰色占位");
        return sp;
    }

    /// <summary>
    /// 创建目标箭头（DD1 风格：使用 dd1_target_*.png，按目标 rank 自动切换 1~4）
    /// </summary>
    void CreateTargetArrow()
    {
        try
        {
            // 如果 targetArrowCanvas 为空或已被销毁，重新查找或创建
            if (targetArrowCanvas == null)
            {
                Canvas canvas = null;
                try { canvas = FindObjectOfType<Canvas>(); }
                catch (System.Exception) { /* 查找失败时 canvas 保持 null */ }

                if (canvas == null)
                {
                    var cgo = new GameObject("TargetArrowCanvas", typeof(RectTransform));
                    cgo.layer = 5;
                    targetArrowCanvas = cgo.AddComponent<Canvas>();
                    targetArrowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
                    cgo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                else
                {
                    targetArrowCanvas = canvas;
                }
            }

            // 如果 targetArrowCanvas 创建后仍然无效，直接返回
            if (targetArrowCanvas == null) return;

            // 如果 targetArrow 为空或已被销毁，重新创建（不预加载固定 Sprite，由 ShowTargetArrow 动态设置）
            if (targetArrow == null)
            {
                targetArrow = new GameObject("TargetArrow", typeof(RectTransform));
                targetArrow.layer = 5;
                targetArrow.transform.SetParent(targetArrowCanvas.transform, false);
                var rt = targetArrow.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(48, 48);

                // 初始加载 target_1 作为占位，实际会在 ShowTargetArrow 中按 rank 替换
                Sprite targetSprite = SafeLoadSprite("UI/Overlays/dd1_target_1");
                var img = targetArrow.AddComponent<UnityEngine.UI.Image>();
                img.sprite = targetSprite;
                img.color = targetSprite != null ? Color.white : Color.yellow;
                img.raycastTarget = false;
                targetArrow.SetActive(false);
            }
        }
        catch (System.Exception ex)
        {
            Log.Warn($"[BattleManager] CreateTargetArrow 创建失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新目标高亮：使用 dd1_selected_*.png 替换纯色选中光圈
    /// </summary>
    void UpdateTargetHighlight(UnitData unit, bool isHighlighted, bool isCurrent)
    {
        if (unit == null || unit.selectCircle == null) return;

        var sr = unit.selectCircle.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (!isHighlighted)
        {
            sr.enabled = false;
            return;
        }

        // 加载对应 selected 图片（按目标 rank 取 1~4，兜底 selected_1）
        int selectedIdx = Mathf.Clamp(unit.rank + 1, 1, 4);
        Sprite selectedSprite = SafeLoadSprite($"UI/Overlays/dd1_selected_{selectedIdx}");

        if (selectedSprite != null)
        {
            sr.sprite = selectedSprite;
            sr.color = isCurrent ? Color.yellow : Color.white;
        }
        else
        {
            // fallback：纯色光圈
            sr.color = isCurrent ? Color.yellow : Color.cyan;
        }
        sr.enabled = true;
    }

    /// <summary>退出选目标模式：清除所有覆盖层</summary>
    void ExitTargetingMode()
    {
        isInTargetingMode = false;
        isSelectingItem = false;
        foreach (var u in playerUnits.ToList())
        {
            if (u != null)
                u.SetHighlightState(UnitData.HighlightState.Normal);
        }
        foreach (var u in enemyUnits.ToList())
        {
            if (u != null)
                u.SetHighlightState(UnitData.HighlightState.Normal);
        }
        // 禁用所有 selectCircle
        foreach (var u in enemyUnits.ToList())
        {
            if (u != null)
                UpdateTargetHighlight(u, false, false);
        }
        foreach (var u in playerUnits.ToList())
        {
            if (u != null)
                UpdateTargetHighlight(u, false, false);
        }
        ClearHighlights();
        if (targetArrow != null) targetArrow.SetActive(false);
        inputState = BattleInputState.Normal;
        selectedSkillIndex = -1;
        pendingSkill = null;
        pendingItemDef = null;
        pendingItemStack = null;
    }

    void CycleTarget(int direction)
    {
        if (validTargets == null || validTargets.Count == 0) return;
        ClearHighlight(highlightedTarget);
        int count = validTargets.Count;
        targetCycleIndex = (targetCycleIndex + direction + count) % count;
        HighlightTarget(validTargets[targetCycleIndex]);
        ApplyTargetingOverlay();
    }

    void ConfirmTarget()
    {
        UnitData attacker = TurnState.CurrentUnit;
        if (attacker == null || pendingSkill == null) return;

        UnitData target = validTargets != null && targetCycleIndex >= 0 && targetCycleIndex < validTargets.Count
            ? validTargets[targetCycleIndex] : null;

        SkillData skillToExecute = pendingSkill;

        // 若为物品，先消耗再执行
        if (pendingItemStack != null)
        {
            GameManager.Instance.RemoveItem(pendingItemStack.itemId, 1);
            BattleLog.Add($"[物品] 使用了 {pendingItemDef?.itemName ?? pendingItemStack.itemId}");
            pendingItemStack = null;
            pendingItemDef = null;
        }

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
        if (unit == null) return;
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

        // 播放攻击动画
        string animName = string.IsNullOrEmpty(skill.animationName) ? attacker.attackAnimName : skill.animationName;
        attacker.Play(animName);

        // 移位(推进/击退/冲锋)
        if (skill.targetShift != 0 && target != null && target.currentHP > 0)
            ApplyRankShift(target, skill.targetShift, enemyUnits.Contains(target) ? enemyUnits : playerUnits);
        if (skill.selfShift != 0)
            ApplyRankShift(attacker, skill.selfShift, attacker.isPlayer ? playerUnits : enemyUnits);
    }

    /// <summary>移位: 将单位向指定方向移动若干站位</summary>
    void ApplyRankShift(UnitData unit, int shift, List<UnitData> teamList)
    {
        int newRank = Mathf.Clamp(unit.rank + shift, 0, 3);
        if (newRank == unit.rank) return;

        // 检查目标站位是否被占据
        UnitData occupant = teamList.Find(u => u != unit && u.currentHP > 0 && u.rank == newRank);
        if (occupant != null)
        {
            int occupantDir = (shift > 0) ? 1 : -1;
            int occupantNew = Mathf.Clamp(occupant.rank + occupantDir, 0, 3);
            if (occupantNew == occupant.rank) return; // 挤不动，不移位
            BattleLog.Add($"[移位] {occupant.unitName} 被挤到站位 {occupantNew}");
            occupant.rank = occupantNew;
        }

        BattleLog.Add($"[移位] {unit.unitName} 从站位 {unit.rank} 移动到 {newRank}");
        unit.rank = newRank;
    }

    // ==================== 基础攻击 ====================

    string GetHitSoundKey(UnitData attacker)
    {
        if (attacker == null) return AudioKeys.SFX_SWORD_HIT;
        string name = attacker.unitName.ToLower();
        if (name.Contains("游侠") || name.Contains("弓箭") || name.Contains("射手"))
            return AudioKeys.SFX_ARROW_HIT;
        if (name.Contains("萨满") || name.Contains("术士") || name.Contains("法师") || name.Contains("学者"))
            return AudioKeys.SFX_MAGIC_HIT;
        return AudioKeys.SFX_SWORD_HIT;
    }

    void DoBasicAttack()
    {
        UnitData attacker = TurnState.CurrentUnit;
        if (attacker == null) return;
        if (!playerUnits.Contains(attacker)) return;

        // 前排角色只能平A前排，后排角色可任意
        bool attackerIsFront = attacker.rank <= 1;
        List<UnitData> alive = enemyUnits.FindAll(u => u != null && u.currentHP > 0);
        if (alive.Count == 0)
        {
            battleOver = true;
            BattleLog.Add("【胜利】所有敌人被击败！");
            return;
        }
        UnitData target;
        if (attackerIsFront)
        {
            target = alive.Find(u => u.rank <= 1) ?? alive[0];
        }
        else
        {
            target = alive[0];
        }

        attacker.Play(attacker.attackAnimName);

        if (!CombatSystem.IsHit(attacker, target))
        {
            BattleLog.Add($"{attacker.unitName} 攻击 {target.unitName} —— <color=#aaaaaa>未命中！</color>");
            SpawnDamagePopup(target.transform.position + Vector3.up * 1.5f, 0, PopupType.Miss);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_MISS);
            // 触发 OnDodge 特质（被攻击目标闪避）
            QuirkTriggerSystem.CheckTriggers(target, QuirkTriggerType.OnDodge);
            StressManager.AddStress(attacker, 2, StressTag.Combat);
            if (!battleOver)
                EndPlayerTurn();
            return;
        }

        bool isCrit = CombatSystem.IsCrit(attacker, target);
        int damage = CombatSystem.CalculateDamage(attacker, target, new SkillData { baseDamage = 0, strScaling = 0, agiScaling = 0 }, isCrit);
        if (isCrit)
        {
            damage = Mathf.RoundToInt(damage * 1.5f);
            BattleLog.Add($"<color=#ffdd00>暴击！</color> {attacker.unitName} 对 {target.unitName} 造成致命一击！");
        }

        DealDamage(attacker, target, damage, isCrit);

        if (!battleOver)
            EndPlayerTurn();
    }

    void EndPlayerTurn()
    {
        // 不再直接调 TurnManager.Instance.EndTurn()，改为发布事件让 TurnManager 自己处理
        // 这样 BattleManager ↔ TurnManager 没有任何命令式调用，循环依赖彻底消除
        BattleEvents.RaiseEndTurnRequested();
    }

    // ==================== 自动化测试支持 ====================

    /// <summary>供自动化测试调用：在当前玩家回合自动选择技能并随机执行</summary>
    public void AutoExecuteTurn()
    {
        UnitData attacker = TurnState?.CurrentUnit;
        if (attacker == null || !playerUnits.Contains(attacker) || battleOver) return;

        // 随机选择一个可用技能
        var usableSkills = attacker.skills
            .Select((s, i) => new { skill = s, index = i })
            .Where(x => x.skill != null && IsSkillUsableFromRank(attacker, x.skill))
            .ToList();

        if (usableSkills.Count == 0)
        {
            DoBasicAttack();
            return;
        }

        var chosen = usableSkills[UnityEngine.Random.Range(0, usableSkills.Count)];

        // 如果目标类型是 Self 或 AllEnemies，直接执行不需要选目标
        if (chosen.skill.targetType == SkillTargetType.Self || chosen.skill.targetType == SkillTargetType.AllEnemies)
        {
            pendingSkill = chosen.skill;
            selectedSkillIndex = chosen.index;
            ExecuteSkill(attacker, chosen.skill, chosen.skill.targetType == SkillTargetType.Self ? attacker : null);
            if (!battleOver)
                EndPlayerTurn();
            return;
        }

        pendingSkill = chosen.skill;
        selectedSkillIndex = chosen.index;

        // 计算合法目标
        List<UnitData> targets = null;
        if (chosen.skill.targetType == SkillTargetType.SingleEnemy)
            targets = enemyUnits.Where(u => u != null && u.currentHP > 0 && IsRankValidTarget(u, chosen.skill)).ToList();
        else if (chosen.skill.targetType == SkillTargetType.SingleAlly)
            targets = playerUnits.Where(u => u != null && u.currentHP > 0 && IsRankValidTarget(u, chosen.skill)).ToList();

        if (targets == null || targets.Count == 0)
        {
            DoBasicAttack();
            return;
        }

        var target = targets[UnityEngine.Random.Range(0, targets.Count)];
        ExecuteSkill(attacker, chosen.skill, target);
        if (!battleOver)
            EndPlayerTurn();
    }

    /// <summary>供自动化测试调用：触发撤退</summary>
    public void TriggerRetreat()
    {
        AttemptRetreat();
    }

    // ==================== 公共伤害方法 ====================

    public void DealDamage(UnitData attacker, UnitData target, int damage, bool isCrit = false)
    {
        if (attacker == null || target == null) return;

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
            SpawnDamagePopup(target.transform.position + Vector3.up * 1.5f, absorbed, PopupType.Shield);
            BattleLog.Add($"[护盾] {target.unitName} 的护盾吸收了 {absorbed} 点伤害");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_SHIELD);
            if (target.shieldHP <= 0 && damage > 0)
                BattleLog.Add($"[护盾] {target.unitName} 的护盾破碎了");
        }

        // 触发 OnTakeDamage 特质（允许修改伤害）
        if (damage > 0)
        {
            var ctx = new QuirkContext { source = attacker, intValue = damage };
            QuirkTriggerSystem.CheckTriggers(target, QuirkTriggerType.OnTakeDamage, ctx);
            if (ctx.valueModified)
                damage = ctx.intValue;
        }

        target.currentHP -= damage;
        target.UpdateHPUI();
        if (damage > 0)
        {
            BattleLog.Add($"{attacker.unitName} 对 {target.unitName} 造成 {damage} 点伤害！");
            StressManager.AddStress(target, StressManager.config.onTakeDamage, StressTag.Combat);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_STRESS);
            // 触发 OnHit 特质（攻击命中时）
            QuirkTriggerSystem.CheckTriggers(attacker, QuirkTriggerType.OnHit);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(isCrit ? AudioKeys.SFX_CRIT : GetHitSoundKey(attacker));
            if (isCrit)
            {
                // 触发 OnCrit 特质
                QuirkTriggerSystem.CheckTriggers(attacker, QuirkTriggerType.OnCrit);
                VFXManager.ScreenShake(0.5f, 0.35f);
                VFXManager.HitStop(0.06f);
                VFXManager.FlashScreen(new Color(1f, 1f, 1f, 0.3f), 0.25f);
                VFXManager.FlashSkeleton(target, Color.red, 0.2f);
            }
            else
            {
                VFXManager.ScreenShake(0.2f, 0.15f);
                VFXManager.FlashSkeleton(target, Color.white, 0.1f);
            }
            VFXManager.PlayHitEffect(target.transform.position + Vector3.up * 0.5f);
        }

        // 死亡之门判定（仅玩家单位）
        if (target.isPlayer && target.currentHP <= 0 && !target.isOnDeathsDoor)
        {
            target.currentHP = 0;
            target.isOnDeathsDoor = true;
            target.UpdateHPUI();
            BattleLog.Add($"<color=red>{target.unitName} 进入死亡之门！</color>");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_DEATHS_DOOR);
            StressManager.AddStress(target, 20, StressTag.Combat);
            // 触发 OnDeathsDoor 特质
            QuirkTriggerSystem.CheckTriggers(target, QuirkTriggerType.OnDeathsDoor);
        }
        else if (target.isPlayer && target.currentHP <= 0 && target.isOnDeathsDoor)
        {
            target.currentHP = 0;
            float roll = RandomProvider.Current.Value;
            bool survived = roll < target.deathsDoorResist;
            BattleLog.Add($"<color=red>[死亡之门] {target.unitName} 死亡抗性判定: {(survived ? "存活" : "死亡")} (roll:{roll:F2} < resist:{target.deathsDoorResist})</color>");
            if (survived)
            {
                target.currentHP = 1;
                target.Revive();  // 死亡之门存活：立刻恢复动画
                StressManager.AddStress(target, 20, StressTag.Combat);
            }
            else
            {
                target.currentHP = 0;
                target.isOnDeathsDoor = false;
                BattleLog.Add($"<color=red>{target.unitName} 重伤不治！</color>");
            }
            target.UpdateHPUI();
        }
        else if (!target.isPlayer && target.currentHP <= 0)
        {
            target.currentHP = 0;
            target.UpdateHPUI();
        }

        if (damage > 0)
            target.Play(target.damagedAnimName);

        if (damage > 0)
            StressManager.CheckResolve(target, this);

        if (target.currentHP > 0 && target.isOnDeathsDoor)
        {
            BattleLog.Add($"{target.unitName} 脱离死亡之门！");
            target.isOnDeathsDoor = false;
        }

        SpawnDamagePopup(target.transform.position + Vector3.up * 1.5f, damage, isCrit ? PopupType.Crit : PopupType.Damage);

        if (hitFeedbackRoutine != null) StopCoroutine(hitFeedbackRoutine);
        if (originalPositions.TryGetValue(target, out Vector3 origPos))
            hitFeedbackRoutine = StartCoroutine(HitFeedbackEffect(target.transform, origPos));

        if (target.currentHP <= 0)
        {
            if (!target.isOnDeathsDoor)
                target.Play(target.deathAnimName);
            BattleLog.Add($"{target.unitName} 被击败！");
            AudioManager.Instance?.PlaySFX(AudioKeys.SFX_DEFEAT);
            // 触发 OnKill 特质
            QuirkTriggerSystem.CheckTriggers(attacker, QuirkTriggerType.OnKill);

            var allies = (enemyUnits.Contains(target) ? playerUnits : enemyUnits).ToList();
            foreach (var ally in allies)
            {
                if (ally != null && ally.currentHP > 0)
                {
                    StressManager.AddStress(ally, StressManager.config.onAllyDeath, StressTag.AllyDown);
                    StressManager.CheckResolve(ally, this);
                }
            }

            if (enemyUnits.Contains(target))
            {
                int expReward = 50 * target.level;
                totalExpPerUnit += expReward;
                foreach (var pu in playerUnits.ToList())
                {
                    if (pu == null) continue;
                    if (pu.currentHP > 0)
                        pu.GainExp(expReward);
                }

                int goldDrop = DropTable.RollGold(target.unitName);
                goldEarned += goldDrop;
                if (lootDisplayLines == null) lootDisplayLines = new List<string>();
                lootDisplayLines.Add($"{target.unitName}: {goldDrop}金");

                var drops = DropTable.RollDrops(target.unitName);
                foreach (var stack in drops)
                {
                    pendingDrops.Add(stack);
                    var def = ItemDatabase.Get(stack.itemId);
                    string name = def != null ? def.itemName : stack.itemId;
                    if (stack.quantity > 1) name += $" x{stack.quantity}";
                    lootDisplayLines.Add($"  掉落: {name}");
                }
            }

            bool allEnemyDead = GetFirstAlive(enemyUnits) == null;
            bool allPlayerDead = GetFirstAlive(playerUnits) == null;

            if (allEnemyDead)
            {
                battleOver = true;
                battleResult = "胜利！";
                BattleLog.Add("【胜利】战斗胜利！");
                ShowRewardPanel();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioKeys.SFX_VICTORY);
                    AudioManager.Instance.StartCoroutine(DelayedBGMChange(AudioKeys.BGM_MAP, 1.5f));
                }
            }
            else if (allPlayerDead)
            {
                battleOver = true;
                battleResult = "失败...";
                BattleLog.Add("【失败】全军覆没！");
                ShowGameOverPanel();
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(AudioKeys.SFX_DEFEAT);
            }
        }
    }

    void ShowRewardPanel()
    {
        // P1 防御：Canvas 查找可能因场景未初始化而失败，用 try-catch 兜底
        Canvas canvas = null;
        try { canvas = FindObjectOfType<Canvas>(); }
        catch (System.Exception ex) { Log.Warn($"[BattleManager] ShowRewardPanel 查找 Canvas 失败: {ex.Message}"); }

        if (canvas == null)
        {
            try
            {
                var cgo = new GameObject("RewardCanvas", typeof(RectTransform));
                cgo.layer = 5;
                canvas = cgo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
                cgo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[BattleManager] ShowRewardPanel 创建 Canvas 失败: {ex.Message}");
                return;
            }
        }

        var root = new GameObject("RewardPanel", typeof(RectTransform));
        root.layer = 5;
        root.transform.SetParent(canvas.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 400);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5;
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

        // Title
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.layer = 5; titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1); titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1); titleRt.sizeDelta = new Vector2(400, 36);
        titleRt.anchoredPosition = new Vector2(0, -12);
        titleGo.AddComponent<CanvasRenderer>();
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "战斗胜利！";
        titleTmp.fontSize = 22; titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.yellow; UIFonts.Apply(titleTmp);

        // Content area
        float yOff = -50;
        foreach (var pu in playerUnits.ToList())
        {
            if (pu == null) continue;
            var line = new GameObject("ExpLine", typeof(RectTransform));
            line.layer = 5; line.transform.SetParent(root.transform, false);
            var lrt = line.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 1); lrt.anchorMax = new Vector2(0.5f, 1);
            lrt.pivot = new Vector2(0.5f, 1); lrt.sizeDelta = new Vector2(400, 20);
            lrt.anchoredPosition = new Vector2(0, yOff);
            line.AddComponent<CanvasRenderer>();
            var tmp = line.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{pu.unitName}  Lv.{pu.level}  经验 +{totalExpPerUnit}  ({pu.currentExp}/{pu.ExpToNextLevel})";
            tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.white; UIFonts.Apply(tmp);
            yOff -= 22;
        }

        // Gold line
        var goldLine = new GameObject("GoldLine", typeof(RectTransform));
        goldLine.layer = 5; goldLine.transform.SetParent(root.transform, false);
        var glRt = goldLine.GetComponent<RectTransform>();
        glRt.anchorMin = new Vector2(0.5f, 1); glRt.anchorMax = new Vector2(0.5f, 1);
        glRt.pivot = new Vector2(0.5f, 1); glRt.sizeDelta = new Vector2(400, 20);
        glRt.anchoredPosition = new Vector2(0, yOff);
        goldLine.AddComponent<CanvasRenderer>();
        var glTmp = goldLine.AddComponent<TextMeshProUGUI>();
        int currentGold = GameManager.Instance != null ? GameManager.Instance.gold : 0;
        glTmp.text = $"金币 +{goldEarned}  ({currentGold} → {currentGold + goldEarned})";
        glTmp.fontSize = 15; glTmp.alignment = TextAlignmentOptions.Left;
        glTmp.color = Color.yellow; UIFonts.Apply(glTmp);
        yOff -= 22;

        // Loot lines
        if (lootDisplayLines != null)
        {
            foreach (var lineText in lootDisplayLines)
            {
                var line = new GameObject("LootLine", typeof(RectTransform));
                line.layer = 5; line.transform.SetParent(root.transform, false);
                var lrt = line.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.5f, 1); lrt.anchorMax = new Vector2(0.5f, 1);
                lrt.pivot = new Vector2(0.5f, 1); lrt.sizeDelta = new Vector2(400, 18);
                lrt.anchoredPosition = new Vector2(0, yOff);
                line.AddComponent<CanvasRenderer>();
                var tmp = line.AddComponent<TextMeshProUGUI>();
                tmp.text = "  " + lineText;
                tmp.fontSize = 13; tmp.alignment = TextAlignmentOptions.Left;
                tmp.color = Color.cyan; UIFonts.Apply(tmp);
                yOff -= 20;
            }
        }

        // Continue button
        var btnGo = new GameObject("ContinueBtn", typeof(RectTransform));
        btnGo.layer = 5; btnGo.transform.SetParent(root.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0); btnRt.anchorMax = new Vector2(0.5f, 0);
        btnRt.pivot = new Vector2(0.5f, 0.5f); btnRt.sizeDelta = new Vector2(160, 40);
        btnRt.anchoredPosition = new Vector2(0, 30);
        btnGo.AddComponent<CanvasRenderer>();
        var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.2f, 0.4f, 0.2f);
        var btnLbl = new GameObject("Label", typeof(RectTransform));
        btnLbl.layer = 5; btnLbl.transform.SetParent(btnGo.transform, false);
        var lblRt = btnLbl.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
        var lblTmp = btnLbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text = "继续"; lblTmp.fontSize = 16; lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.color = Color.white; UIFonts.Apply(lblTmp);
        var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.gold += goldEarned;
                foreach (var stack in pendingDrops)
                    GameManager.Instance.AddItem(stack.itemId, stack.quantity);
            }
            rewardCollected = true;
        });

        // Disable old victory panel if exists
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    void ShowGameOverPanel()
    {
        // P1 防御：Canvas 查找可能因场景未初始化而失败，用 try-catch 兜底
        Canvas canvas = null;
        try { canvas = FindObjectOfType<Canvas>(); }
        catch (System.Exception ex) { Log.Warn($"[BattleManager] ShowGameOverPanel 查找 Canvas 失败: {ex.Message}"); }

        if (canvas == null)
        {
            try
            {
                var cgo = new GameObject("GameOverCanvas", typeof(RectTransform));
                cgo.layer = 5;
                canvas = cgo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
                cgo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[BattleManager] ShowGameOverPanel 创建 Canvas 失败: {ex.Message}");
                return;
            }
        }

        var root = new GameObject("GameOverPanel", typeof(RectTransform));
        root.layer = 5; root.transform.SetParent(canvas.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 200);
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;

        var bg = new GameObject("BG", typeof(RectTransform));
        bg.layer = 5; bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.15f, 0.05f, 0.05f, 0.95f);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.layer = 5; titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f); titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f); titleRt.sizeDelta = new Vector2(300, 80);
        titleRt.anchoredPosition = Vector2.zero;
        titleGo.AddComponent<CanvasRenderer>();
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "全军覆没\n按 R 返回主菜单";
        titleTmp.fontSize = 20; titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.red; UIFonts.Apply(titleTmp);

        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    public UnitData GetFirstAlive(IReadOnlyList<UnitData> units)
    {
        if (units == null) return null;
        foreach (var u in units)
            if (u != null && u.currentHP > 0) return u;
        return null;
    }

    // ==================== IBattleContext 实现（用于 TurnManager 解耦）====================

    IReadOnlyList<UnitData> IBattleContext.PlayerUnits => playerUnits;
    IReadOnlyList<UnitData> IBattleContext.EnemyUnits => enemyUnits;
    object IBattleContext.RawContext => this;

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

    IEnumerator DelayedBGMChange(string bgmKey, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(bgmKey);
    }

    void AttemptRetreat()
    {
        if (battleOver) return;
        if (!TurnState.IsPlayerTurn)
        {
            BattleLog.Add("[撤退] 只有玩家回合才能撤退");
            return;
        }

        var alive = playerUnits.FindAll(u => u.currentHP > 0);
        if (alive.Count == 0) return;

        bool allSucceed = true;
        foreach (var unit in alive)
        {
            float chance = Mathf.Max(0.1f, 0.4f - Mathf.Max(0, unit.stress - 100) * 0.01f);
            if (RandomProvider.Current.Value >= chance)
            {
                allSucceed = false;
                StressManager.AddStress(unit, 30, StressTag.Combat);
            }
        }

        if (allSucceed)
        {
            BattleLog.Add("【撤退成功】全队成功撤离！");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioKeys.SFX_RETREAT);
            foreach (var unit in alive)
            {
                StressManager.AddStress(unit, 15, StressTag.Combat);
                unit.CleanupForRetreat();  // 撤退时清空动画轨道
            }
            ReturnToMap();
        }
        else
        {
            BattleLog.Add("<color=red>【撤退失败】部分队员未能脱战！</color>");
            EndPlayerTurn();
        }
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
                if (unit == null || data == null) continue;
                data.level = unit.level;
                data.currentExp = unit.currentExp;
                data.VIT = unit.VIT;
                data.STR = unit.STR;
                data.DEF = unit.baseDefense;
                data.AGI = unit.AGI;
                data.INT = unit.INT;
                data.weaponAttack = unit.weaponAttack;
                data.stress = unit.stress;
                data.equippedWeapon = unit.equippedWeapon;
                data.equippedArmor = unit.equippedArmor;
                data.quirks = unit.quirks;
                data.rank = unit.rank;
                data.currentHP = unit.currentHP;
                data.maxHp = unit.MaxHp;
                // 同步 V0.5 加点系统：累加的战斗未分配点写回持久化层
                data.unassignedPoints = unit.unassignedPoints;
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

    public void SpawnDamagePopup(Vector3 position, int amount, PopupType type)
    {
        GameObject go = damagePopupPrefab != null
            ? Instantiate(damagePopupPrefab, position, Quaternion.identity)
            : CreateDefaultPopup(position);
        var popup = go.GetComponent<DamagePopup>();
        if (popup != null)
            popup.Setup(amount, type);
    }

    public void SpawnTextPopup(Vector3 position, string text, Color color, float fontSize = 4f)
    {
        GameObject go = damagePopupPrefab != null
            ? Instantiate(damagePopupPrefab, position, Quaternion.identity)
            : CreateDefaultPopup(position);
        var popup = go.GetComponent<DamagePopup>();
        if (popup != null)
            popup.SetupText(text, color, fontSize);
    }

    GameObject CreateDefaultPopup(Vector3 position)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = position;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 3.5f;
        tmp.color = Color.red;
        go.AddComponent<DamagePopup>();
        return go;
    }

}
