using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectChimera.Core;

/// <summary>游戏平衡常量 — 集中管理所有数值配置，便于调整和维护</summary>
public static class GameBalanceConstants
{
    // ===== 概率常量 =====
    public const float DIPLOMATIC_EVENT_CHANCE = 0.08f;      // 外交事件概率
    public const float FACTION_EXPAND_CHANCE = 0.15f;        // 势力扩张概率
    public const float FACTION_ATTACK_CHANCE = 0.08f;        // 势力攻击概率
    public const float FACTION_RETREAT_CHANCE = 0.05f;       // 势力撤退概率
    public const float BASE_ENEMY_ATTACK_CHANCE = 0.10f;     // 基础敌人攻击概率
    public const float ENEMY_TILE_ATTACK_SCALING = 0.02f;    // 每块敌方领土增加的攻击概率

    // ===== 资源常量 =====
    public const int BASE_DAILY_GOLD_INCOME = 10;            // 基础每日金币收入
    public const int GOLD_PER_DIFFICULTY_LEVEL = 3;          // 每难度等级增加的金币
    public const int FOOD_HEAL_PER_UNIT = 3;                 // 每单位食物恢复HP
    public const int PRODUCTION_GOLD_PER_UNIT = 8;           // 每单位生产力转换金币

    // ===== 难度曲线 =====
    public const float DIFFICULTY_SCALE_INTERVAL = 5f;       // 难度提升间隔（天）
    public const float FACTION_EXPAND_BONUS_PER_LEVEL = 0.02f; // 每难度等级扩张概率增加

    // ===== 每日事件概率 =====
    public const float DAILY_EVENT_RESOURCE_BOUNTY = 0.3f;   // 资源赏金事件概率
    public const float DAILY_EVENT_ENEMY_INVASION = 0.2f;    // 敌人入侵事件概率
    public const float DAILY_EVENT_TECH_BREAKTHROUGH = 0.2f; // 科技突破事件概率
    public const float DAILY_EVENT_MORALE_BOOST = 0.2f;      // 士气提升事件概率

    // ===== 建筑系统 =====
    public const float BUILDING_COST_MULTIPLIER = 1.5f;      // 建筑成本倍率
    public const int BUILDING_MAX_LEVEL = 5;                 // 建筑最大等级
}

public enum GameState
{
    WorldMap,
    Battle,
    Menu,
    Dungeon
}

/// <summary>建筑定义</summary>
[System.Serializable]
public class BuildingDefinition
{
    public string id;
    public string name;
    public string description;
    public int baseCost;
    public float costMultiplier = GameBalanceConstants.BUILDING_COST_MULTIPLIER;
    public int maxLevel = GameBalanceConstants.BUILDING_MAX_LEVEL;
}

/// <summary>科技定义</summary>
[System.Serializable]
public class TechDefinition
{
    public string id;
    public string name;
    public string description;
    public int cost;
    public List<string> prerequisites = new List<string>();
}

/// <summary>游戏统计数据</summary>
[System.Serializable]
public class GameStatistics
{
    public int totalDaysSurvived;
    public int totalBattlesWon;
    public int totalBattlesLost;
    public int totalEnemiesKilled;
    public int totalGoldEarned;
    public int totalGoldSpent;
    public int totalBuildingsBuilt;
    public int totalTechnologiesResearched;
    public int totalTilesCaptured;
    public int totalResourcesCollected;
    public int totalStressGained;
    public int totalStressReduced;
    public int totalHealingDone;
    public int totalDamageDealt;
    public int totalDamageTaken;
    public int totalCrits;
    public int totalMisses;
    public int totalPerfectVictories;
    public int totalNamedHeroes;
    public int totalSovereignLevel;
}

/// <summary>成就信息</summary>
[System.Serializable]
public class AchievementInfo
{
    public string id;
    public string name;
    public string description;
    public bool unlocked;
}

[System.Serializable]
public class UnitBattleData
{
    public string unitName;
    public int VIT = 10;
    public int STR = 10;
    public int DEF = 5;
    public int AGI = 5;
    public int INT = 3;
    public int weaponAttack;
    public int level = 1;
    public int currentExp;
    public int stress;
    public bool isPlayer;

    [Header("阵型")]
    public int rank; // 0=前排(近敌), 3=后排(远敌)

    public Weapon equippedWeapon;
    public Armor equippedArmor;
    public List<Quirk> quirks = new List<Quirk>();
    public int currentHP;
    public int maxHp;

    // ========== 命名系统（持久化字段） ==========

    /// <summary>是否已命名（炮灰→英雄的质变节点）。</summary>
    public bool isNamed;
    /// <summary>命名后压力上限额外加值（命名时写为 20）。</summary>
    public int stressCapBonus;
    /// <summary>专属技能名（对应 classData.skillPool 中某项的 skillName）。空串=未选。</summary>
    public string exclusiveSkillId;

    // ========== 命名里程碑追踪（持久化字段） ==========

    /// <summary>累计承受伤害总量（命名阈值: 100）。</summary>
    public int milestoneDamageTaken;
    /// <summary>累计击杀数（命名阈值: 5）。</summary>
    public int milestoneKillCount;
    /// <summary>是否已救过队友（援护/嘲讽保护队友免受致命伤害）。</summary>
    public bool milestoneSavedAlly;

    /// <summary>命名阈值：累计承伤量。</summary>
    public const int NAMING_DAMAGE_THRESHOLD = 100;
    /// <summary>命名阈值：累计击杀数。</summary>
    public const int NAMING_KILL_THRESHOLD = 5;

    // ========== 领袖系统（持久化字段） ==========

    /// <summary>是否为领袖（永不死的主角单位）。</summary>
    public bool isSovereign;
    /// <summary>领袖等级上限（设计文档：领袖 15 级，冒险者 10 级）。</summary>
    public const int SOVEREIGN_MAX_LEVEL = 15;
    /// <summary>冒险者等级上限。</summary>
    public const int ADVENTURER_MAX_LEVEL = 10;

    /// <summary>获取实际等级上限（领袖 15，冒险者 10）。</summary>
    public int GetMaxLevel()
    {
        return isSovereign ? SOVEREIGN_MAX_LEVEL : ADVENTURER_MAX_LEVEL;
    }

    /// <summary>检查是否满足命名条件。</summary>
    public bool CanBeNamed()
    {
        if (isNamed) return false;
        if (level < 3) return false;
        return milestoneDamageTaken >= NAMING_DAMAGE_THRESHOLD
            || milestoneKillCount >= NAMING_KILL_THRESHOLD
            || milestoneSavedAlly;
    }

    /// <summary>执行命名：全属性 +10%，压力上限 +20，标记已命名。</summary>
    public void Anoint(string title)
    {
        if (isNamed) return;
        isNamed = true;
        stressCapBonus = 20;

        // 全属性 +10%（向上取整，至少 +1）
        VIT = Mathf.Max(VIT + 1, Mathf.CeilToInt(VIT * 1.1f));
        STR = Mathf.Max(STR + 1, Mathf.CeilToInt(STR * 1.1f));
        AGI = Mathf.Max(AGI + 1, Mathf.CeilToInt(AGI * 1.1f));
        INT = Mathf.Max(INT + 1, Mathf.CeilToInt(INT * 1.1f));

        // 重算 maxHp
        maxHp = ComputeMaxHP();
        currentHP = Mathf.Min(currentHP, maxHp);

        BattleLog.Add($"[命名] {unitName} 获得称号「{title}」！全属性 +10%，压力上限 +20");
    }

    // ========== V0.5 加点系统（持久化字段） ==========

    /// <summary>未分配属性点（V0.5 引入：战斗升级时累加，英雄厅手动分配）</summary>
    public int unassignedPoints;

    /// <summary>
    /// V0.5 加点入口（持久化层）：从 unassignedPoints 取出 1 点分配到指定主属性。
    /// 持久化层数据源：UnitBattleData；下次 BattleSetup.CreateUnit 同步到 UnitData.primaryAttributes。
    /// </summary>
    /// <returns>是否成功分配</returns>
    public bool AllocatePoint(StatType stat)
    {
        if (unassignedPoints <= 0) return false;
        switch (stat)
        {
            case StatType.VIT: VIT++; break;
            case StatType.STR: STR++; break;
            case StatType.AGI: AGI++; break;
            case StatType.INT: INT++; break;
            default: return false;
        }
        unassignedPoints--;
        BattleLog.Add($"[加点] {unitName} 分配 1 点 → {stat}（剩余 {unassignedPoints}）");
        return true;
    }

    /// <summary>优先查 ClassDefinition.baseHp，否则返回 0（不再硬编码职业名）</summary>
    public int GetClassBaseHP()
    {
        var cd = GameManager.Instance != null ? GameManager.Instance.GetClassDefinition(unitName) : null;
        if (cd != null) return cd.baseHp;
        return 0;
    }

    public int ComputeMaxHP()
    {
        return VIT * 5 + GetClassBaseHP();
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("游戏状态")]
    public GameState currentState = GameState.WorldMap;

    [Header("世界地图持久数据")]
    public Vector2Int savedSquadPos = Vector2Int.zero;

    [Header("宏观天系统")]
    /// <summary>当前天数（宏观天回合数）</summary>
    public int currentDay = 1;
    /// <summary>每回合行动点（移动力）</summary>
    public int actionPoints = 3;
    /// <summary>当前剩余行动点</summary>
    public int currentActionPoints = 3;
    /// <summary>每日金币产出（首都建筑提供）</summary>
    public int dailyGoldIncome = 10;
    /// <summary>每日压力恢复（营地/休息提供）</summary>
    public int dailyStressRecovery = 5;
    /// <summary>是否为玩家回合</summary>
    public bool isPlayerTurn = true;

    [Header("建筑系统")]
    /// <summary>已建造的建筑列表</summary>
    public List<string> builtBuildings = new List<string>();
    /// <summary>建筑等级字典（建筑ID -> 等级）</summary>
    public Dictionary<string, int> buildingLevels = new Dictionary<string, int>();

    [Header("科技系统")]
    /// <summary>已研究的科技列表</summary>
    public List<string> researchedTechnologies = new List<string>();
    /// <summary>当前正在研究的科技</summary>
    public string currentResearch = "";
    /// <summary>研究进度（天数）</summary>
    public int researchProgress = 0;
    /// <summary>每日科研点数</summary>
    public int dailyResearchPoints = 1;

    [Header("胜利条件系统")]
    /// <summary>游戏是否结束</summary>
    public bool isGameOver = false;
    /// <summary>是否胜利</summary>
    public bool isVictory = false;
    /// <summary>胜利条件：占领地块数量</summary>
    public int victoryTileCount = 15;
    /// <summary>胜利条件：研究科技数量</summary>
    public int victoryTechCount = 3;
    /// <summary>胜利条件：存活天数</summary>
    public int victoryDayCount = 30;
    /// <summary>失败条件：领袖死亡（领袖不会死，但可以设置其他失败条件）</summary>
    public bool leaderDead = false;
    /// <summary>失败条件：金币耗尽</summary>
    public int bankruptcyThreshold = -100;

    [Header("成就系统")]
    /// <summary>已解锁的成就列表</summary>
    public List<string> unlockedAchievements = new List<string>();
    /// <summary>统计数据</summary>
    public GameStatistics statistics = new GameStatistics();

    [Header("小队数据")]
    public List<UnitBattleData> playerTeamData;
    public List<UnitBattleData> enemyTeamData;

    [Header("背包")]
    public List<ItemStack> inventory = new List<ItemStack>();
    public int gold = 100;

    [Header("商店")]
    public ShopData shopData = new ShopData();

    [Header("中文字体（拖入 Unifont_Universal_SDF）")]
    public TMPro.TMP_FontAsset chineseFont;

    [Header("物品定义")]
    public List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();

    [Header("职业定义")]
    public List<ClassDefinition> classDefinitions = new List<ClassDefinition>();

    [Header("地牢系统")]
    public DungeonSession dungeonSession = new DungeonSession();

    public Dictionary<int, List<UnitBattleData>> enemyTemplates = new Dictionary<int, List<UnitBattleData>>();

    // 缓存引用，避免重复 FindObjectOfType
    private HexWorldMap _cachedHexMap;
    private GridManager _cachedGridManager;

    /// <summary>获取 HexWorldMap 缓存引用</summary>
    HexWorldMap GetHexMap()
    {
        if (_cachedHexMap == null)
            _cachedHexMap = FindObjectOfType<HexWorldMap>();
        return _cachedHexMap;
    }

    /// <summary>获取 GridManager 缓存引用</summary>
    GridManager GetGridManager()
    {
        if (_cachedGridManager == null)
            _cachedGridManager = FindObjectOfType<GridManager>();
        return _cachedGridManager;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 全局异常捕获
            Application.logMessageReceived += OnLogMessageReceived;

            AudioListener al = GetComponent<AudioListener>();
            if (al != null) Destroy(al);

            Camera cam = GetComponent<Camera>();
            if (cam != null) Destroy(cam);

            EnsureSceneTransitionManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Application.logMessageReceived -= OnLogMessageReceived;
    }

    void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            Log.Error($"[全局异常] {condition}\n{stackTrace}");
        }
    }

    void EnsureSceneTransitionManager()
    {
        if (FindObjectOfType<SceneTransitionManager>() == null)
        {
            var go = new GameObject("SceneTransitionManager");
            go.AddComponent<SceneTransitionManager>();
            DontDestroyOnLoad(go);
        }
    }

    /// <summary>
    /// 反射方式确保某个 UI 控制器存在。若场景中已有一个，跳过；否则新建一个挂到 DontDestroyOnLoad 上。
    /// 使用"TypeName, AssemblyName"格式查找，避免 Core 编译期依赖 UI 程序集。
    /// </summary>
    void EnsureUiController(string typeName, string assemblyName)
    {
        var t = System.Type.GetType($"{typeName}, {assemblyName}");
        if (t == null)
        {
            Debug.LogWarning($"[GameManager] 找不到类型 {typeName} ({assemblyName})，跳过创建。");
            return;
        }
        if (FindObjectOfType(t) != null) return;
        var go = new GameObject(typeName);
        DontDestroyOnLoad(go);
        go.AddComponent(t);
    }

    void Start()
    {
        if (!StressManager.initialized)
            StressManager.InitFromConfig(new StressConfig());

        if (chineseFont != null)
            UIFonts.chineseFont = chineseFont;

        ItemDatabase.Initialize(itemDefinitions);
        InitClassDefinitions();

        // 反射方式创建 UI 控制器：避免 Core → UI 编译期依赖
        // （GameManager 在 Core，但所有 UI*Controller 在 UI 模块；
        //   如果用强类型，需要 Core 引用 UI，UI 又引用 Core，形成循环。）
        EnsureUiController("UIInventoryController", "ProjectChimera.UI");
        EnsureUiController("UIFormationController", "ProjectChimera.UI");
        EnsureUiController("UIPauseMenu", "ProjectChimera.UI");
        EnsureUiController("UIRecruitController", "ProjectChimera.UI");
        EnsureUiController("UIDungeonMapController", "ProjectChimera.UI");
        EnsureUiController("UIDebugConsole", "ProjectChimera.UI");
        EnsureUiController("UICampController", "ProjectChimera.UI");

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            DontDestroyOnLoad(es);
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        if (dungeonSession.isInDungeon)
        {
            var dmType = System.Type.GetType("UIDungeonMapController, ProjectChimera.UI");
            if (dmType != null)
            {
                var ctrl = FindObjectOfType(dmType) as MonoBehaviour;
                if (ctrl != null)
                {
                    var openMethod = dmType.GetMethod("Open");
                    openMethod?.Invoke(ctrl, null);
                }
            }
        }

        InitEnemyTemplates();

        if (playerTeamData == null || playerTeamData.Count == 0)
            CreateTestData();

        if (shopData == null) shopData = new ShopData();
        shopData.RefreshShop();
    }

    void InitClassDefinitions()
    {
        classDefinitions = new List<ClassDefinition>();

        // ===== 战士 =====
        var warrior = ScriptableObject.CreateInstance<ClassDefinition>();
        warrior.id = "warrior";
        warrior.unitName = "战士";
        warrior.baseVIT = 8; warrior.baseSTR = 10; warrior.baseDEF = 6;
        warrior.baseAGI = 5; warrior.baseINT = 3; warrior.baseWeaponAttack = 5;
        warrior.baseHp = 40;
        warrior.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "盾牌猛击", description = "伤害+眩晕+击退",
                baseDamage = 12, strScaling = 0.8f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = false,
                targetShift = 1,
                aiCategory = SkillEffectType.Stun,
                effectValue = 1, effectDuration = 1,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 12, strScaling = 0.8f },
                    new ApplyStatusCommand { statusType = StatusType.Stun, duration = 1 }
                }
            },
            new SkillData
            {
                skillName = "嘲讽", description = "强迫敌人攻击自己",
                baseDamage = 3, strScaling = 0.3f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = false,
                aiCategory = SkillEffectType.Taunt,
                effectDuration = 1,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 3, strScaling = 0.3f },
                    new ApplyStatusCommand { statusType = StatusType.Taunt, duration = 1 }
                }
            },
            new SkillData
            {
                skillName = "援护", description = "替一名友方承受伤害",
                baseDamage = 0,
                targetType = SkillTargetType.SingleAlly,
                minUserRank = 0, maxUserRank = 3,
                aiCategory = SkillEffectType.Protect,
                effectDuration = 1,
                commands = new List<Command>
                {
                    new RemoveStatusCommand { statusType = StatusType.Protected, removeFromAllAllies = true },
                    new ApplyStatusCommand { statusType = StatusType.Protected, duration = 1 }
                }
            },
            // 领袖专属技能：王者之证
            new SkillData
            {
                skillName = "王者之证", description = "领袖专属：全体攻击+防御提升，持续3回合",
                baseDamage = 0,
                targetType = SkillTargetType.Self,
                minUserRank = 0, maxUserRank = 3,
                aiCategory = SkillEffectType.BerserkBuff,
                effectDuration = 3,
                commands = new List<Command>
                {
                    new ModifyAttributeCommand { duration = 3, modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 3f, "王者之证") },
                    new ModifyAttributeCommand { duration = 3, modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.DEF, 3f, "王者之证") },
                    new ApplyStatusCommand { statusType = StatusType.Berserk, duration = 3 }
                }
            }
        };
        classDefinitions.Add(warrior);

        // ===== 游侠 =====
        var ranger = ScriptableObject.CreateInstance<ClassDefinition>();
        ranger.id = "ranger";
        ranger.unitName = "游侠";
        ranger.baseVIT = 5; ranger.baseSTR = 6; ranger.baseDEF = 4;
        ranger.baseAGI = 10; ranger.baseINT = 4; ranger.baseWeaponAttack = 3;
        ranger.baseHp = 30;
        ranger.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "精准射击", description = "高单体伤害",
                baseDamage = 14, agiScaling = 0.8f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                aiCategory = SkillEffectType.DirectDamage,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 14, agiScaling = 0.8f }
                }
            },
            new SkillData
            {
                skillName = "标记", description = "受伤+20%，持续2回合",
                baseDamage = 3, agiScaling = 0.2f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                aiCategory = SkillEffectType.Mark,
                effectValue = 0.2f, effectDuration = 2,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 3, agiScaling = 0.2f },
                    new ApplyStatusCommand { statusType = StatusType.Mark, duration = 2, effectValue = 0.2f }
                }
            },
            new SkillData
            {
                skillName = "弹幕覆盖", description = "对全体敌人造成伤害",
                baseDamage = 7, agiScaling = 0.3f,
                targetType = SkillTargetType.AllEnemies,
                minUserRank = 2, maxUserRank = 3,
                aiCategory = SkillEffectType.AoEDamage,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 7, agiScaling = 0.3f, isAoE = true }
                }
            }
        };
        classDefinitions.Add(ranger);

        // ===== 狂战士 =====
        var berserker = ScriptableObject.CreateInstance<ClassDefinition>();
        berserker.id = "berserker";
        berserker.unitName = "狂战士";
        berserker.baseVIT = 10; berserker.baseSTR = 12; berserker.baseDEF = 4;
        berserker.baseAGI = 4; berserker.baseINT = 2; berserker.baseWeaponAttack = 6;
        berserker.baseHp = 50;
        berserker.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "嗜血斩击", description = "冲锋+伤害+自损10%HP+吸血50%",
                baseDamage = 18,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = true,
                selfShift = -1,
                aiCategory = SkillEffectType.BloodthirstyStrike,
                commands = new List<Command>
                {
                    new BloodthirstyStrikeCommand { baseDamage = 18, strScaling = 1.0f }
                }
            },
            new SkillData
            {
                skillName = "狂暴之力", description = "STR+5, DEF-3, 持续3回合",
                targetType = SkillTargetType.Self,
                minUserRank = 0, maxUserRank = 3,
                aiCategory = SkillEffectType.BerserkBuff,
                effectDuration = 3,
                commands = new List<Command>
                {
                    new ModifyAttributeCommand { duration = 3, modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "狂暴之力") },
                    new ModifyAttributeCommand { duration = 3, modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.DEF, -3f, "狂暴之力") },
                    new ApplyStatusCommand { statusType = StatusType.Berserk, duration = 3 }
                }
            },
            new SkillData
            {
                skillName = "殊死一搏", description = "冲锋+血量越低伤害越高,最高+100%",
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                targetShift = 1, selfShift = -1,
                aiCategory = SkillEffectType.DesperateStrike,
                commands = new List<Command>
                {
                    new DesperateStrikeCommand { baseDamage = 12, strScaling = 0.8f, maxBonusMultiplier = 1.0f }
                }
            }
        };
        classDefinitions.Add(berserker);

        // ===== 学者 =====
        var scholar = ScriptableObject.CreateInstance<ClassDefinition>();
        scholar.id = "scholar";
        scholar.unitName = "学者";
        scholar.baseVIT = 4; scholar.baseSTR = 3; scholar.baseDEF = 3;
        scholar.baseAGI = 7; scholar.baseINT = 12; scholar.baseWeaponAttack = 2;
        scholar.baseHp = 25;
        scholar.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "精神冲击", description = "精神伤害+15压力+击退",
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                targetShift = 1,
                aiCategory = SkillEffectType.MindShock,
                commands = new List<Command>
                {
                    new MindShockCommand { baseDamage = 10, intScaling = 1.2f, stressAmount = 15 }
                }
            },
            new SkillData
            {
                skillName = "智慧启迪", description = "减压20点+INT+2持续2回合",
                targetType = SkillTargetType.SingleAlly,
                minUserRank = 2, maxUserRank = 3,
                aiCategory = SkillEffectType.WisdomEnlightenment,
                effectDuration = 2,
                commands = new List<Command>
                {
                    new ConsumeResourceCommand { resourceType = ConsumeResourceCommand.ResourceType.Stress, amount = -20 },
                    new ModifyAttributeCommand { duration = 2, modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.INT, 2f, "智慧启迪") },
                    new ApplyStatusCommand { statusType = StatusType.Enlightened, duration = 2 }
                }
            },
            new SkillData
            {
                skillName = "禁忌知识", description = "高额精神伤害+击退,自身+25压力",
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                targetShift = 1,
                aiCategory = SkillEffectType.ForbiddenKnowledge,
                commands = new List<Command>
                {
                    new MindShockCommand { baseDamage = 20, intScaling = 1.5f, stressAmount = 0 },
                    new SelfInflictCommand { inflictType = SelfInflictCommand.SelfInflictType.Stress, amount = 25 }
                }
            }
        };
        classDefinitions.Add(scholar);

        // ===== 哥布林战士 =====
        var gobWarrior = ScriptableObject.CreateInstance<ClassDefinition>();
        gobWarrior.id = "goblin_warrior";
        gobWarrior.unitName = "哥布林战士";
        gobWarrior.baseVIT = 2; gobWarrior.baseSTR = 10; gobWarrior.baseDEF = 4;
        gobWarrior.baseAGI = 4; gobWarrior.baseINT = 1; gobWarrior.baseWeaponAttack = 3;
        gobWarrior.baseHp = 40;
        gobWarrior.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "重劈", description = "用力砍下去+击退",
                baseDamage = 8, strScaling = 0.6f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = false,
                targetShift = 1,
                aiCategory = SkillEffectType.DirectDamage,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 8, strScaling = 0.6f }
                }
            }
        };
        classDefinitions.Add(gobWarrior);

        // ===== 哥布林弓手 =====
        var gobArcher = ScriptableObject.CreateInstance<ClassDefinition>();
        gobArcher.id = "goblin_archer";
        gobArcher.unitName = "哥布林弓手";
        gobArcher.baseVIT = 1; gobArcher.baseSTR = 7; gobArcher.baseDEF = 2;
        gobArcher.baseAGI = 9; gobArcher.baseINT = 2; gobArcher.baseWeaponAttack = 2;
        gobArcher.baseHp = 40;
        gobArcher.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "精准射击", description = "远程射击",
                baseDamage = 10, agiScaling = 0.6f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                aiCategory = SkillEffectType.DirectDamage,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 10, agiScaling = 0.6f }
                }
            }
        };
        classDefinitions.Add(gobArcher);

        // ===== 哥布林萨满 =====
        var gobShaman = ScriptableObject.CreateInstance<ClassDefinition>();
        gobShaman.id = "goblin_shaman";
        gobShaman.unitName = "哥布林萨满";
        gobShaman.baseVIT = 2; gobShaman.baseSTR = 4; gobShaman.baseDEF = 2;
        gobShaman.baseAGI = 3; gobShaman.baseINT = 8; gobShaman.baseWeaponAttack = 0;
        gobShaman.baseHp = 40;
        gobShaman.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "治疗术", description = "恢复一名队友生命",
                baseDamage = 15, strScaling = 0.5f, agiScaling = 0.5f,
                targetType = SkillTargetType.SingleAlly,
                minUserRank = 2, maxUserRank = 3,
                aiCategory = SkillEffectType.Heal,
                commands = new List<Command>
                {
                    new HealCommand { baseHeal = 15, intScaling = 1.0f }
                }
            },
            new SkillData
            {
                skillName = "图腾护盾", description = "为队友附加护盾吸收伤害",
                baseDamage = 0,
                targetType = SkillTargetType.SingleAlly,
                minUserRank = 2, maxUserRank = 3,
                aiCategory = SkillEffectType.Shield,
                effectValue = 12, effectDuration = 2,
                commands = new List<Command>
                {
                    new ShieldCommand { shieldAmount = 12 }
                }
            },
            new SkillData
            {
                skillName = "火球术", description = "对血最少的敌人造成伤害",
                baseDamage = 12, agiScaling = 0.4f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                aiCategory = SkillEffectType.DirectDamage,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 12, agiScaling = 0.4f }
                }
            },
            new SkillData
            {
                skillName = "恐吓", description = "施加压力伤害",
                baseDamage = 0,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
                aiCategory = SkillEffectType.Stress,
                effectValue = 15,
                commands = new List<Command>
                {
                    new ConsumeResourceCommand { resourceType = ConsumeResourceCommand.ResourceType.Stress, amount = 15 }
                }
            }
        };
        classDefinitions.Add(gobShaman);

        // ===== 野狼 =====
        var wolf = ScriptableObject.CreateInstance<ClassDefinition>();
        wolf.id = "wolf";
        wolf.unitName = "野狼";
        wolf.baseVIT = 3; wolf.baseSTR = 8; wolf.baseDEF = 3;
        wolf.baseAGI = 10; wolf.baseINT = 1; wolf.baseWeaponAttack = 2;
        wolf.baseHp = 40;
        wolf.skillPool = new List<SkillData>
        {
            new SkillData
            {
                skillName = "撕咬", description = "造成伤害 + 30%概率流血",
                baseDamage = 10, strScaling = 0.8f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = false,
                aiCategory = SkillEffectType.Bleed,
                effectValue = 4, effectDuration = 2,
                commands = new List<Command>
                {
                    new DealDamageCommand { baseDamage = 10, strScaling = 0.8f },
                    new ApplyStatusCommand { statusType = StatusType.Bleed, duration = 2, effectValue = 4 }
                }
            }
        };
        classDefinitions.Add(wolf);

        Log.Info($"[ClassDefinition] 已加载 {classDefinitions.Count} 个职业定义");
    }

    public ClassDefinition GetClassDefinition(string unitName)
    {
        return classDefinitions.Find(c => c.unitName == unitName);
    }

    // ========== 敌人模板系统 ==========

    void InitEnemyTemplates()
    {
        enemyTemplates.Clear();

        // ===== 普通战斗 (1-3星) =====
        enemyTemplates[0] = new List<UnitBattleData>
        {
            MakeGoblinWarrior(0), MakeGoblinWarrior(1),
            MakeGoblinArcher(2), MakeGoblinArcher(3)
        };
        enemyTemplates[1] = new List<UnitBattleData>
        {
            MakeGoblinWarrior(0), MakeWolf(1),
            MakeGoblinArcher(2), MakeGoblinShaman(3)
        };
        enemyTemplates[2] = new List<UnitBattleData>
        {
            MakeWolf(0), MakeWolf(1),
            MakeGoblinArcher(2), MakeGoblinShaman(3)
        };
        enemyTemplates[3] = new List<UnitBattleData>
        {
            MakeGoblinWarrior(0), MakeGoblinWarrior(1),
            MakeGoblinWarrior(2), MakeGoblinShaman(3)
        };
        // 前锋 + 萨满 + 双弓手
        enemyTemplates[4] = new List<UnitBattleData>
        {
            MakeGoblinWarrior(0), MakeGoblinShaman(1),
            MakeGoblinArcher(2), MakeGoblinArcher(3)
        };
        // 双战士 + 狼 + 萨满
        enemyTemplates[5] = new List<UnitBattleData>
        {
            MakeGoblinWarrior(0), MakeGoblinWarrior(1),
            MakeWolf(2), MakeGoblinShaman(3)
        };
        // 三弓手 + 萨满 (玻璃大炮)
        enemyTemplates[6] = new List<UnitBattleData>
        {
            MakeGoblinArcher(0), MakeGoblinArcher(1),
            MakeGoblinArcher(2), MakeGoblinShaman(3)
        };
        // 双狼 + 双战士 (纯近战)
        enemyTemplates[7] = new List<UnitBattleData>
        {
            MakeWolf(0), MakeWolf(1),
            MakeGoblinWarrior(2), MakeGoblinWarrior(3)
        };
        // 三战士 + 弓手
        enemyTemplates[8] = new List<UnitBattleData>
        {
            MakeGoblinWarrior(0), MakeGoblinWarrior(1),
            MakeGoblinWarrior(2), MakeGoblinArcher(3)
        };
        // 狼 + 弓手 + 萨满 + 弓手
        enemyTemplates[9] = new List<UnitBattleData>
        {
            MakeWolf(0), MakeGoblinArcher(1),
            MakeGoblinShaman(2), MakeGoblinArcher(3)
        };

        // ===== 精英战斗 (3-4星) =====
        enemyTemplates[10] = new List<UnitBattleData>
        {
            MakeEliteGoblinWarrior(0), MakeGoblinWarrior(1),
            MakeGoblinArcher(2), MakeGoblinShaman(3)
        };
        enemyTemplates[11] = new List<UnitBattleData>
        {
            MakeWolf(0), MakeEliteWolf(1),
            MakeGoblinArcher(2), MakeGoblinShaman(3)
        };
        // 双精英战士 + 弓手 + 萨满
        enemyTemplates[12] = new List<UnitBattleData>
        {
            MakeEliteGoblinWarrior(0), MakeEliteGoblinWarrior(1),
            MakeGoblinArcher(2), MakeGoblinShaman(3)
        };
        // 精英狼 + 精英战士 + 双弓手
        enemyTemplates[13] = new List<UnitBattleData>
        {
            MakeEliteWolf(0), MakeEliteGoblinWarrior(1),
            MakeGoblinArcher(2), MakeGoblinArcher(3)
        };
        // 双精英狼 + 萨满 + 弓手
        enemyTemplates[14] = new List<UnitBattleData>
        {
            MakeEliteWolf(0), MakeEliteWolf(1),
            MakeGoblinShaman(2), MakeGoblinArcher(3)
        };
        // 精英战士 + 精英狼 + 精英战士 + 萨满 (全精英)
        enemyTemplates[15] = new List<UnitBattleData>
        {
            MakeEliteGoblinWarrior(0), MakeEliteWolf(1),
            MakeEliteGoblinWarrior(2), MakeGoblinShaman(3)
        };

        // ===== Boss战 (5星) =====
        enemyTemplates[100] = new List<UnitBattleData>
        {
            MakeBossWarrior(0), MakeBossWarrior(1),
            MakeBossShaman(2), MakeBossArcher(3)
        };
        // Boss 变体: 督军 + 大萨满 + 双精锐弓手
        enemyTemplates[101] = new List<UnitBattleData>
        {
            MakeBossWarrior(0), MakeBossShaman(1),
            MakeBossArcher(2), MakeBossArcher(3)
        };

        // ===== 新敌人类型 (迭代5) =====

        // 骷髅兵
        enemyTemplates[200] = new List<UnitBattleData>
        {
            MakeSkeletonWarrior(0), MakeSkeletonWarrior(1),
            MakeSkeletonArcher(2), MakeSkeletonMage(3)
        };

        // 幽灵
        enemyTemplates[201] = new List<UnitBattleData>
        {
            MakeGhost(0), MakeGhost(1),
            MakeGhost(2), MakeGhost(3)
        };

        // 龙
        enemyTemplates[202] = new List<UnitBattleData>
        {
            MakeDragon(0), MakeDragon(1),
            MakeDragon(2), MakeDragon(3)
        };
    }

    // ===== 新敌人类型定义 =====

    UnitBattleData MakeSkeletonWarrior(int rank) => new UnitBattleData
    {
        unitName = "骷髅战士", isPlayer = false, rank = rank,
        VIT = 3, STR = 12, DEF = 5, AGI = 6, INT = 1,
        weaponAttack = 4, level = 3,
        equippedWeapon = MakeTestWeapon("bone_sword", "骨剑", StatType.STR, 2),
        equippedArmor = MakeTestArmor("bone_armor", "骨甲", 2),
        quirks = new List<Quirk>
        {
            new Quirk { id = "undead", localizedName = "亡灵", isPositive = false, triggerType = QuirkTriggerType.BattleStart, procChance = 1f },
            new Quirk { id = "fearless", localizedName = "无畏", isPositive = true, triggerType = QuirkTriggerType.BattleStart, procChance = 1f }
        }
    };

    UnitBattleData MakeSkeletonArcher(int rank) => new UnitBattleData
    {
        unitName = "骷髅弓手", isPlayer = false, rank = rank,
        VIT = 2, STR = 8, DEF = 3, AGI = 11, INT = 2,
        weaponAttack = 3, level = 3,
        equippedWeapon = MakeTestWeapon("bone_bow", "骨弓", StatType.AGI, 2),
        equippedArmor = MakeTestArmor("bone_armor", "骨甲", 1),
        quirks = new List<Quirk>
        {
            new Quirk { id = "undead", localizedName = "亡灵", isPositive = false, triggerType = QuirkTriggerType.BattleStart, procChance = 1f }
        }
    };

    UnitBattleData MakeSkeletonMage(int rank) => new UnitBattleData
    {
        unitName = "骷髅法师", isPlayer = false, rank = rank,
        VIT = 2, STR = 4, DEF = 2, AGI = 5, INT = 12,
        weaponAttack = 1, level = 3,
        equippedWeapon = MakeTestWeapon("bone_staff", "骨杖", StatType.INT, 4),
        equippedArmor = MakeTestArmor("bone_robe", "骨袍", 1),
        quirks = new List<Quirk>
        {
            new Quirk { id = "undead", localizedName = "亡灵", isPositive = false, triggerType = QuirkTriggerType.BattleStart, procChance = 1f },
            new Quirk { id = "dark_magic", localizedName = "暗黑魔法", isPositive = true, triggerType = QuirkTriggerType.OnHit, procChance = 0.3f }
        }
    };

    UnitBattleData MakeGhost(int rank) => new UnitBattleData
    {
        unitName = "幽灵", isPlayer = false, rank = rank,
        VIT = 3, STR = 6, DEF = 1, AGI = 14, INT = 10,
        weaponAttack = 2, level = 4,
        equippedWeapon = MakeTestWeapon("ghostly_touch", "幽灵之触", StatType.INT, 3),
        equippedArmor = MakeTestArmor("ethereal_body", "虚无之体", 0),
        quirks = new List<Quirk>
        {
            new Quirk { id = "ethereal", localizedName = "虚无", isPositive = true, triggerType = QuirkTriggerType.OnTakeDamage, procChance = 0.5f },
            new Quirk { id = "fear", localizedName = "恐惧", isPositive = false, triggerType = QuirkTriggerType.TurnStart, procChance = 1f }
        }
    };

    UnitBattleData MakeDragon(int rank) => new UnitBattleData
    {
        unitName = "龙", isPlayer = false, rank = rank,
        VIT = 15, STR = 18, DEF = 12, AGI = 8, INT = 15,
        weaponAttack = 10, level = 6,
        equippedWeapon = MakeTestWeapon("dragon_claw", "龙爪", StatType.STR, 8),
        equippedArmor = MakeTestArmor("dragon_scale", "龙鳞", 10),
        quirks = new List<Quirk>
        {
            new Quirk { id = "dragon_breath", localizedName = "龙息", isPositive = true, triggerType = QuirkTriggerType.OnHit, procChance = 0.4f },
            new Quirk { id = "fear_aura", localizedName = "恐惧光环", isPositive = false, triggerType = QuirkTriggerType.BattleStart, procChance = 1f },
            new Quirk { id = "regeneration", localizedName = "再生", isPositive = true, triggerType = QuirkTriggerType.TurnStart, procChance = 1f }
        }
    };

    UnitBattleData MakeGoblinWarrior(int rank) => new UnitBattleData
    {
        unitName = "哥布林战士", isPlayer = false, rank = rank,
        VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1,
        weaponAttack = 3, level = 2,
        equippedWeapon = MakeTestWeapon("rusty_sword", "锈剑", StatType.STR, 1),
        equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1),
        quirks = new List<Quirk> { new Quirk { id = "nuoRuo", localizedName = "懦弱", isPositive = false, triggerType = QuirkTriggerType.BattleStart, procChance = 1f } }
    };
    UnitBattleData MakeGoblinArcher(int rank) => new UnitBattleData
    {
        unitName = "哥布林弓手", isPlayer = false, rank = rank,
        VIT = 1, STR = 7, DEF = 2, AGI = 9, INT = 2,
        weaponAttack = 2, level = 2,
        equippedWeapon = MakeTestWeapon("short_bow", "短弓", StatType.AGI, 1),
        equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1),
        quirks = new List<Quirk> { new Quirk { id = "cuiRuo", localizedName = "脆弱", isPositive = false, triggerType = QuirkTriggerType.TurnStart, procChance = 1f } }
    };
    UnitBattleData MakeGoblinShaman(int rank) => new UnitBattleData
    {
        unitName = "哥布林萨满", isPlayer = false, rank = rank,
        VIT = 2, STR = 4, DEF = 2, AGI = 3, INT = 8,
        weaponAttack = 0, level = 2,
        equippedWeapon = MakeTestWeapon("spirit_staff", "法杖", StatType.INT, 3),
        equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1),
        quirks = new List<Quirk> { new Quirk { id = "buXiangYuGan", localizedName = "不祥预感", isPositive = false, triggerType = QuirkTriggerType.TurnStart, procChance = 1f } }
    };
    UnitBattleData MakeWolf(int rank) => new UnitBattleData
    {
        unitName = "野狼", isPlayer = false, rank = rank,
        VIT = 4, STR = 9, DEF = 3, AGI = 12, INT = 1,
        weaponAttack = 2, level = 2,
        equippedWeapon = MakeTestWeapon("sharp_claws", "利爪", StatType.STR, 2),
        equippedArmor = MakeTestArmor("thick_hide", "厚皮", 2),
        quirks = new List<Quirk> { new Quirk { id = "shiXue", localizedName = "嗜血", isPositive = true, triggerType = QuirkTriggerType.TurnStart, procChance = 1f } }
    };
    UnitBattleData MakeEliteGoblinWarrior(int rank) => new UnitBattleData
    {
        unitName = "哥布林精英战士", isPlayer = false, rank = rank,
        VIT = 5, STR = 13, DEF = 6, AGI = 5, INT = 2,
        weaponAttack = 5, level = 4,
        equippedWeapon = MakeTestWeapon("iron_sword", "铁剑", StatType.STR, 3),
        equippedArmor = MakeTestArmor("leather_armor", "皮甲", 2),
        quirks = new List<Quirk>
        {
            new Quirk { id = "nuoRuo", localizedName = "懦弱", isPositive = false, triggerType = QuirkTriggerType.BattleStart, procChance = 1f },
            new Quirk { id = "yiShang", localizedName = "易伤", isPositive = false, triggerType = QuirkTriggerType.OnTakeDamage, procChance = 1f }
        }
    };
    UnitBattleData MakeEliteWolf(int rank) => new UnitBattleData
    {
        unitName = "精英野狼", isPlayer = false, rank = rank,
        VIT = 7, STR = 12, DEF = 5, AGI = 14, INT = 1,
        weaponAttack = 4, level = 4,
        equippedWeapon = MakeTestWeapon("sharp_claws", "利爪", StatType.STR, 3),
        equippedArmor = MakeTestArmor("thick_hide", "厚皮", 3),
        quirks = new List<Quirk>
        {
            new Quirk { id = "shiXue", localizedName = "嗜血", isPositive = true, triggerType = QuirkTriggerType.TurnStart, procChance = 1f },
            new Quirk { id = "zhanYiAngYang", localizedName = "战役昂扬", isPositive = true, triggerType = QuirkTriggerType.BattleStart, procChance = 1f }
        }
    };
    UnitBattleData MakeBossWarrior(int rank) => new UnitBattleData
    {
        unitName = "哥布林督军", isPlayer = false, rank = rank,
        VIT = 10, STR = 16, DEF = 8, AGI = 6, INT = 3,
        weaponAttack = 8, level = 6,
        equippedWeapon = MakeTestWeapon("great_axe", "巨斧", StatType.STR, 5),
        equippedArmor = MakeTestArmor("plate_armor", "铠甲", 4),
        quirks = new List<Quirk>
        {
            new Quirk { id = "tieBi", localizedName = "铁臂", isPositive = true, triggerType = QuirkTriggerType.BattleStart, procChance = 1f },
            new Quirk { id = "zhanYiAngYang", localizedName = "战役昂扬", isPositive = true, triggerType = QuirkTriggerType.BattleStart, procChance = 1f }
        }
    };
    UnitBattleData MakeBossShaman(int rank) => new UnitBattleData
    {
        unitName = "哥布林大萨满", isPlayer = false, rank = rank,
        VIT = 6, STR = 6, DEF = 4, AGI = 5, INT = 12,
        weaponAttack = 2, level = 6,
        equippedWeapon = MakeTestWeapon("arcane_staff", "奥术法杖", StatType.INT, 6),
        equippedArmor = MakeTestArmor("robe", "法袍", 3),
        quirks = new List<Quirk>
        {
            new Quirk { id = "buXiangYuGan", localizedName = "不祥预感", isPositive = false, triggerType = QuirkTriggerType.TurnStart, procChance = 1f },
            new Quirk { id = "yiShang", localizedName = "易伤", isPositive = false, triggerType = QuirkTriggerType.OnTakeDamage, procChance = 1f }
        }
    };
    UnitBattleData MakeBossArcher(int rank) => new UnitBattleData
    {
        unitName = "哥布林精锐弓手", isPlayer = false, rank = rank,
        VIT = 4, STR = 10, DEF = 4, AGI = 12, INT = 3,
        weaponAttack = 5, level = 6,
        equippedWeapon = MakeTestWeapon("long_bow", "长弓", StatType.AGI, 4),
        equippedArmor = MakeTestArmor("leather_armor", "皮甲", 3),
        quirks = new List<Quirk>
        {
            new Quirk { id = "cuiRuo", localizedName = "脆弱", isPositive = false, triggerType = QuirkTriggerType.TurnStart, procChance = 1f },
            new Quirk { id = "lengJing", localizedName = "冷静", isPositive = true, triggerType = QuirkTriggerType.TurnStart, procChance = 1f }
        }
    };

    Weapon MakeTestWeapon(string id, string name, StatType stat, int amount) => new Weapon
    {
        id = id, equipmentName = name,
        mods = new List<StatMod> { new StatMod { stat = stat, amount = amount, isPercent = false } }
    };
    Armor MakeTestArmor(string id, string name, int def) => new Armor
    {
        id = id, equipmentName = name,
        mods = new List<StatMod> { new StatMod { stat = StatType.DEF, amount = def, isPercent = false } }
    };

    void CreateTestData()
    {
        playerTeamData = new List<UnitBattleData>
        {
            new UnitBattleData
            {
                unitName = "战士", rank = 0,
                VIT = 8, STR = 10, DEF = 6, AGI = 5, INT = 3,
                weaponAttack = 5, level = 3, isPlayer = true,
                isSovereign = true, // 领袖：永不死的主角单位
                equippedWeapon = MakeTestWeapon("iron_sword", "铁剑", StatType.STR, 3),
                equippedArmor = MakeTestArmor("plate_armor", "铠甲", 3),
                quirks = new List<Quirk>
                {
                    new Quirk { id = "tieBi", localizedName = "铁臂", isPositive = true, triggerType = QuirkTriggerType.BattleStart, procChance = 1f },
                    new Quirk { id = "wanQiang", localizedName = "顽强", isPositive = true, triggerType = QuirkTriggerType.OnDeathsDoor, procChance = 1f }
                }
            },
            new UnitBattleData
            {
                unitName = "狂战士", rank = 1,
                VIT = 10, STR = 12, DEF = 4, AGI = 4, INT = 2,
                weaponAttack = 6, level = 3, isPlayer = true,
                equippedWeapon = MakeTestWeapon("great_axe", "巨斧", StatType.STR, 5),
                equippedArmor = MakeTestArmor("hide_armor", "硬皮甲", 2),
                quirks = new List<Quirk>
                {
                    new Quirk { id = "zhanYiAngYang", localizedName = "战役昂扬", isPositive = true, triggerType = QuirkTriggerType.BattleStart, procChance = 1f },
                    new Quirk { id = "shiXue", localizedName = "嗜血", isPositive = true, triggerType = QuirkTriggerType.TurnStart, procChance = 1f },
                    new Quirk { id = "ziNue", localizedName = "自虐", isPositive = false, triggerType = QuirkTriggerType.TurnStart, procChance = 1f }
                }
            },
            new UnitBattleData
            {
                unitName = "游侠", rank = 2,
                VIT = 5, STR = 6, DEF = 4, AGI = 10, INT = 4,
                weaponAttack = 3, level = 3, isPlayer = true,
                equippedWeapon = MakeTestWeapon("short_bow", "短弓", StatType.AGI, 2),
                equippedArmor = MakeTestArmor("leather_armor", "皮甲", 2),
                quirks = new List<Quirk>
                {
                    new Quirk { id = "lengJing", localizedName = "冷静", isPositive = true, triggerType = QuirkTriggerType.TurnStart, procChance = 1f }
                }
            },
            new UnitBattleData
            {
                unitName = "学者", rank = 3,
                VIT = 4, STR = 3, DEF = 3, AGI = 7, INT = 12,
                weaponAttack = 2, level = 3, isPlayer = true,
                equippedWeapon = MakeTestWeapon("arcane_staff", "奥术法杖", StatType.INT, 4),
                equippedArmor = MakeTestArmor("robe", "法袍", 1),
                quirks = new List<Quirk>
                {
                    new Quirk { id = "wanQiang", localizedName = "顽强", isPositive = true, triggerType = QuirkTriggerType.OnDeathsDoor, procChance = 1f }
                }
            }
        };

        foreach (var u in playerTeamData)
        {
            u.maxHp = u.ComputeMaxHP();
            u.currentHP = u.maxHp;
        }

        gold = 100;
        inventory = new List<ItemStack>
        {
            new ItemStack("health_potion", 2),
            new ItemStack("stress_herb", 1),
        };
    }

    
    public void StartBattle()
    {
        Log.Info("[GameManager] StartBattle() 被调用");
        int template = RandomProvider.Current.Range(0, 10);
        enemyTeamData = CreateEnemyTeamFromTemplate(template);
        foreach (var u in enemyTeamData)
        {
            u.maxHp = u.ComputeMaxHP();
            u.currentHP = u.maxHp;
        }
        LoadBattleScene();
    }

    public void StartDungeonBattle(int templateId)
    {
        if (!enemyTemplates.ContainsKey(templateId))
        {
            Log.Error($"[GameManager] 找不到敌人模板 {templateId}，使用默认模板");
            templateId = 0;
        }
        enemyTeamData = CreateEnemyTeamFromTemplate(templateId);
        foreach (var u in enemyTeamData)
        {
            u.maxHp = u.ComputeMaxHP();
            u.currentHP = u.maxHp;
        }
        currentState = GameState.Battle;
        LoadBattleScene();
    }

    public void StartBossBattle(int bossId)
    {
        StartDungeonBattle(bossId);
    }

    List<UnitBattleData> CreateEnemyTeamFromTemplate(int templateId)
    {
        if (enemyTemplates.ContainsKey(templateId))
        {
            var template = enemyTemplates[templateId];
            var result = new List<UnitBattleData>();
            foreach (var src in template)
                result.Add(CloneUnitBattleData(src));
            return result;
        }
        return new List<UnitBattleData>();
    }

    UnitBattleData CloneUnitBattleData(UnitBattleData src)
    {
        return new UnitBattleData
        {
            unitName = src.unitName,
            VIT = src.VIT, STR = src.STR, DEF = src.DEF, AGI = src.AGI, INT = src.INT,
            weaponAttack = src.weaponAttack, level = src.level, currentExp = src.currentExp,
            stress = src.stress, isPlayer = src.isPlayer, rank = src.rank,
            currentHP = src.currentHP, maxHp = src.maxHp,
            equippedWeapon = CloneEquipment(src.equippedWeapon) as Weapon,
            equippedArmor = CloneEquipment(src.equippedArmor) as Armor,
            quirks = new List<Quirk>(src.quirks ?? new List<Quirk>())
        };
    }

    Equipment CloneEquipment(Equipment src)
    {
        if (src == null) return null;
        Equipment dst = src is Weapon ? new Weapon() : new Armor();
        dst.id = src.id;
        dst.equipmentName = src.equipmentName;
        dst.mods = new List<StatMod>(src.mods);
        return dst;
    }

    void LoadBattleScene()
    {
        string sceneName = "BattleScene";
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Log.Error($"[GameManager] 场景 '{sceneName}' 不在 Build Settings 中！");
            return;
        }
        currentState = GameState.Battle;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionTo(() => LoadSceneClean("BattleScene"));
        }
        else
        {
            LoadSceneClean("BattleScene");
        }
    }

    public void ReturnToWorldMap()
    {
        System.Action doReturn = () =>
        {
            SaveGame();
            currentState = GameState.WorldMap;
            string sceneName = "WorldMap";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Log.Error($"[GameManager] 场景 '{sceneName}' 不在 Build Settings 中！");
                return;
            }
            LoadSceneClean("WorldMap");
        };

        if (dungeonSession.isInDungeon)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo(doReturn);
            else
                doReturn();
            return;
        }

        // 返回前自动存档
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(doReturn);
        else
            doReturn();
    }

    // ========== 背包操作 ==========

    public int GetItemQuantity(string itemId)
    {
        var stack = inventory.Find(s => s.itemId == itemId);
        return stack?.quantity ?? 0;
    }

    public void AddItem(string itemId, int qty = 1)
    {
        if (qty <= 0) return;
        var def = ItemDatabase.Get(itemId);
        int maxStack = def != null ? def.maxStack : 1;

        if (maxStack > 1)
        {
            var stack = inventory.Find(s => s.itemId == itemId);
            if (stack != null) { stack.quantity += qty; return; }
        }
        inventory.Add(new ItemStack(itemId, qty));
    }

    public bool RemoveItem(string itemId, int qty = 1)
    {
        var stack = inventory.Find(s => s.itemId == itemId);
        if (stack == null || stack.quantity < qty) return false;
        stack.quantity -= qty;
        if (stack.quantity <= 0) inventory.Remove(stack);
        return true;
    }

    // ========== 宏观天系统 ==========

    /// <summary>消耗行动点（移动/探索时调用）</summary>
    public bool ConsumeActionPoint()
    {
        if (currentActionPoints <= 0) return false;
        currentActionPoints--;
        Log.Info($"[宏观天] 消耗 1 行动点（剩余 {currentActionPoints}/{actionPoints}）");
        return true;
    }

    /// <summary>检查是否还有行动点</summary>
    public bool HasActionPoints()
    {
        return currentActionPoints > 0;
    }

    /// <summary>结束玩家回合，进入结算阶段</summary>
    public void EndPlayerTurn()
    {
        if (!isPlayerTurn) return;
        if (isGameOver)
        {
            Log.Warn("[宏观天] 游戏已结束，无法结束回合");
            return;
        }

        isPlayerTurn = false;
        Log.Info($"[宏观天] 第 {currentDay} 天 - 玩家回合结束");

        try
        {
            ProcessDayEnd();
        }
        catch (System.Exception e)
        {
            Log.Error($"[宏观天] 每日结算异常: {e.Message}\n{e.StackTrace}");
            // 恢复玩家回合状态，允许重试
            isPlayerTurn = true;
        }
    }

    /// <summary>每日结算：资源产出、压力恢复、敌人行动</summary>
    void ProcessDayEnd()
    {
        using (new PerformanceScope("ProcessDayEnd"))
        {
            // 1. 资源产出
            gold += dailyGoldIncome;
            Log.Info($"[宏观天] 金币 +{dailyGoldIncome}（当前 {gold}）");

            // 2. 4X 系统：地块资源产出
            using (new PerformanceScope("TileResourceProduction"))
                ProcessTileResourceProduction();

            // 3. 压力恢复
            if (playerTeamData != null)
            {
                foreach (var unit in playerTeamData)
                {
                    if (unit != null && unit.currentHP > 0)
                    {
                        int oldStress = unit.stress;
                        unit.stress = Mathf.Max(unit.stress - dailyStressRecovery, 0);
                        if (unit.stress != oldStress)
                            Log.Info($"[宏观天] {unit.unitName} 压力 -{dailyStressRecovery}（当前 {unit.stress}）");
                    }
                }
            }

            // 4. 建筑效果
            using (new PerformanceScope("BuildingEffects"))
                ProcessBuildingEffects();

            // 4. 科技研究
            using (new PerformanceScope("Research"))
                ProcessResearch();

            // 5. 敌人行动（简化：随机触发事件）
            using (new PerformanceScope("EnemyActions"))
                ProcessEnemyActions();

            // 6. 外交事件
            using (new PerformanceScope("DiplomaticEvents"))
                ProcessDiplomaticEvents();

            // 7. 胜利条件检查
            using (new PerformanceScope("VictoryCheck"))
                CheckVictoryConditions();

            // 8. 进入新的一天
            StartNewDay();
        }
    }

    /// <summary>4X 系统：地块资源产出</summary>
    void ProcessTileResourceProduction()
    {
        var hexMap = GetHexMap();
        if (hexMap == null) return;

        // 获取各类资源产出（使用新的多层产出系统）
        TileYield totalYield = hexMap.GetPlayerTotalYield();
        int foodProduction = totalYield.food;
        int prodProduction = totalYield.production;
        int goldProduction = totalYield.gold;
        int scienceProduction = totalYield.science;
        int faithProduction = totalYield.faith;

        // 应用资源产出效果
        if (foodProduction > 0)
        {
            // 食物：全队恢复HP
            if (playerTeamData != null)
            {
                foreach (var unit in playerTeamData)
                {
                    if (unit != null && unit.currentHP > 0)
                    {
                        int heal = foodProduction * 3;
                        unit.currentHP = Mathf.Min(unit.currentHP + heal, unit.maxHp);
                    }
                }
            }
            Log.Info($"[4X] 食物产出: 全队 HP +{foodProduction * 3}");
        }

        if (prodProduction > 0)
        {
            // 生产力：增加金币
            int prodGold = prodProduction * 8;
            gold += prodGold;
            Log.Info($"[4X] 生产力产出: 金币 +{prodGold}");
        }

        if (goldProduction > 0)
        {
            // 金币：直接增加
            int goldBonus = goldProduction * 12;
            gold += goldBonus;
            Log.Info($"[4X] 金币产出: +{goldBonus}");
        }

        if (scienceProduction > 0)
        {
            // 科研：增加研究进度
            if (!string.IsNullOrEmpty(currentResearch))
            {
                researchProgress += scienceProduction;
                Log.Info($"[4X] 科研产出: 研究进度 +{scienceProduction}");
            }
        }

        if (faithProduction > 0)
        {
            // 信仰：全队减压
            if (playerTeamData != null)
            {
                foreach (var unit in playerTeamData)
                {
                    if (unit != null && unit.currentHP > 0)
                    {
                        unit.stress = Mathf.Max(unit.stress - faithProduction * 2, 0);
                    }
                }
            }
            Log.Info($"[4X] 信仰产出: 全队压力 -{faithProduction * 2}");
        }
    }

    /// <summary>建筑效果处理</summary>
    void ProcessBuildingEffects()
    {
        // 市场：额外金币
        if (builtBuildings.Contains("market"))
        {
            int marketBonus = buildingLevels.ContainsKey("market") ? buildingLevels["market"] * 5 : 5;
            gold += marketBonus;
            Log.Info($"[建筑] 市场产出 +{marketBonus} 金币");
        }

        // 兵营：经验加成
        if (builtBuildings.Contains("barracks"))
        {
            if (playerTeamData != null)
            {
                foreach (var unit in playerTeamData)
                {
                    if (unit != null && unit.currentHP > 0)
                    {
                        unit.currentExp += 10;
                        Log.Info($"[建筑] 兵营提供 +10 经验给 {unit.unitName}");
                    }
                }
            }
        }

        // 治疗所：额外恢复
        if (builtBuildings.Contains("infirmary"))
        {
            int healBonus = buildingLevels.ContainsKey("infirmary") ? buildingLevels["infirmary"] * 10 : 10;
            if (playerTeamData != null)
            {
                foreach (var unit in playerTeamData)
                {
                    if (unit != null && unit.currentHP > 0 && unit.currentHP < unit.maxHp)
                    {
                        int heal = Mathf.Min(healBonus, unit.maxHp - unit.currentHP);
                        unit.currentHP += heal;
                        Log.Info($"[建筑] 治疗所恢复 {unit.unitName} {heal} HP");
                    }
                }
            }
        }

        // 研究所：科研点数
        if (builtBuildings.Contains("laboratory"))
        {
            dailyResearchPoints = 1 + (buildingLevels.ContainsKey("laboratory") ? buildingLevels["laboratory"] : 1);
            Log.Info($"[建筑] 研究所提供每日 {dailyResearchPoints} 科研点");
        }
    }

    /// <summary>科技研究处理</summary>
    void ProcessResearch()
    {
        if (string.IsNullOrEmpty(currentResearch)) return;

        researchProgress += dailyResearchPoints;
        Log.Info($"[科技] {currentResearch} 研究进度: {researchProgress}/{GetResearchCost(currentResearch)}");

        // 检查是否完成
        if (researchProgress >= GetResearchCost(currentResearch))
        {
            CompleteResearch(currentResearch);
        }
    }

    /// <summary>获取科技研究成本</summary>
    int GetResearchCost(string techId)
    {
        switch (techId)
        {
            case "advanced_weapons": return 5;
            case "magic_training": return 8;
            case "defense_tactics": return 6;
            case "exploration": return 4;
            default: return 5;
        }
    }

    /// <summary>完成科技研究</summary>
    void CompleteResearch(string techId)
    {
        researchedTechnologies.Add(techId);
        currentResearch = "";
        researchProgress = 0;

        // 应用科技效果
        switch (techId)
        {
            case "advanced_weapons":
                // 高级武器：全队攻击+2
                if (playerTeamData != null)
                {
                    foreach (var unit in playerTeamData)
                    {
                        if (unit != null)
                            unit.STR += 2;
                    }
                }
                Log.Info("[科技] 高级武器研究完成！全队 STR +2");
                break;
            case "magic_training":
                // 魔法训练：全队智力+3
                if (playerTeamData != null)
                {
                    foreach (var unit in playerTeamData)
                    {
                        if (unit != null)
                            unit.INT += 3;
                    }
                }
                Log.Info("[科技] 魔法训练研究完成！全队 INT +3");
                break;
            case "defense_tactics":
                // 防御战术：全队防御+2
                if (playerTeamData != null)
                {
                    foreach (var unit in playerTeamData)
                    {
                        if (unit != null)
                            unit.DEF += 2;
                    }
                }
                Log.Info("[科技] 防御战术研究完成！全队 DEF +2");
                break;
            case "exploration":
                // 探索技术：行动点+1
                actionPoints++;
                Log.Info("[科技] 探索技术研究完成！行动点 +1");
                break;
        }
    }

    /// <summary>外交事件处理</summary>
    void ProcessDiplomaticEvents()
    {
        float roll = RandomProvider.Current.Value;

        if (roll < GameBalanceConstants.DIPLOMATIC_EVENT_CHANCE)
        {
            int eventType = RandomProvider.Current.Range(0, 4);
            switch (eventType)
            {
                case 0: // 商人来访
                    int merchantGold = RandomProvider.Current.Range(20, 50);
                    gold += merchantGold;
                    Log.Info($"[外交] 商人来访！获得 {merchantGold} 金币");
                    break;
                case 1: // 难民加入
                    if (playerTeamData != null && playerTeamData.Count < 6)
                    {
                        var newUnit = CreateRandomAdventurer();
                        playerTeamData.Add(newUnit);
                        Log.Info($"[外交] 难民 {newUnit.unitName} 加入队伍！");
                    }
                    break;
                case 2: // 友好势力援助
                    int aidGold = RandomProvider.Current.Range(30, 80);
                    gold += aidGold;
                    Log.Info($"[外交] 友好势力援助！获得 {aidGold} 金币");
                    break;
                case 3: // 敌对势力威胁
                    // 增加敌人袭击概率
                    pendingEnemyAttack = true;
                    Log.Info("[外交] 敌对势力威胁！准备战斗...");
                    break;
            }
        }
    }

    /// <summary>创建随机冒险者</summary>
    UnitBattleData CreateRandomAdventurer()
    {
        string[] names = { "流浪者", "佣兵", "学徒", "旅人" };
        string name = names[RandomProvider.Current.Range(0, names.Length)];

        return new UnitBattleData
        {
            unitName = name,
            VIT = RandomProvider.Current.Range(5, 10),
            STR = RandomProvider.Current.Range(5, 10),
            DEF = RandomProvider.Current.Range(3, 7),
            AGI = RandomProvider.Current.Range(5, 10),
            INT = RandomProvider.Current.Range(3, 7),
            level = 1,
            isPlayer = true,
            maxHp = 30,
            currentHP = 30
        };
    }

    /// <summary>敌人行动处理（AI势力行为系统）</summary>
    void ProcessEnemyActions()
    {
        // AI势力行为系统
        ProcessFactionActions(TileFaction.Enemy1, "敌对势力1");
        ProcessFactionActions(TileFaction.Enemy2, "敌对势力2");

        // 随机敌人袭击（基于势力强度）
        int enemy1Tiles = GetFactionTileCount(TileFaction.Enemy1);
        int enemy2Tiles = GetFactionTileCount(TileFaction.Enemy2);
        float attackChance = GameBalanceConstants.BASE_ENEMY_ATTACK_CHANCE + 
                           (enemy1Tiles + enemy2Tiles) * GameBalanceConstants.ENEMY_TILE_ATTACK_SCALING;

        float roll = RandomProvider.Current.Value;
        if (roll < attackChance)
        {
            Log.Info("[宏观天] 敌人来袭！准备战斗...");
            pendingEnemyAttack = true;
        }
    }

    /// <summary>处理势力行为</summary>
    void ProcessFactionActions(TileFaction faction, string factionName)
    {
        var hexMap = GetHexMap();
        if (hexMap == null) return;

        float expandChance = GameBalanceConstants.FACTION_EXPAND_CHANCE;
        float attackChance = GameBalanceConstants.FACTION_ATTACK_CHANCE;
        float retreatChance = GameBalanceConstants.FACTION_RETREAT_CHANCE;

        float roll = RandomProvider.Current.Value;

        if (roll < expandChance)
        {
            // 势力扩张：占领相邻中立地块
            if (hexMap.ExpandFaction(faction, 1))
            {
                Log.Info($"[AI势力] {factionName} 扩张了领土");
            }
        }
        else if (roll < expandChance + attackChance)
        {
            // 势力攻击：攻击玩家领地
            if (hexMap.AttackPlayerTerritory(faction))
            {
                Log.Info($"[AI势力] {factionName} 攻击了玩家领地");
                pendingEnemyAttack = true;
            }
        }
        else if (roll < expandChance + attackChance + retreatChance)
        {
            // 势力撤退：放弃边缘领地
            if (hexMap.RetreatFaction(faction, 1))
            {
                Log.Info($"[AI势力] {factionName} 撤退了");
            }
        }
    }

    /// <summary>获取势力控制的地块数量</summary>
    int GetFactionTileCount(TileFaction faction)
    {
        var hexMap = GetHexMap();
        if (hexMap == null) return 0;
        return hexMap.GetFactionTileCount(faction);
    }

    // ========== 胜利条件系统 ==========

    /// <summary>检查胜利条件</summary>
    public void CheckVictoryConditions()
    {
        if (isGameOver) return;

        try
        {
            // 检查胜利条件
            if (CheckVictory())
            {
                isGameOver = true;
                isVictory = true;
                ShowVictoryScreen();
                return;
            }

            // 检查失败条件
            if (CheckDefeat())
            {
                isGameOver = true;
                isVictory = false;
                ShowDefeatScreen();
                return;
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[胜利条件] 检查异常: {e.Message}");
        }
    }

    /// <summary>检查是否满足胜利条件</summary>
    bool CheckVictory()
    {
        var hexMap = GetHexMap();
        if (hexMap == null) return false;

        // 条件1：占领足够数量的地块
        int playerTiles = hexMap.GetFactionTileCount(TileFaction.Player);
        if (playerTiles >= victoryTileCount)
        {
            Log.Info($"[胜利] 占领地块数量达标: {playerTiles}/{victoryTileCount}");
            return true;
        }

        // 条件2：研究足够数量的科技
        if (researchedTechnologies.Count >= victoryTechCount)
        {
            Log.Info($"[胜利] 科技研究数量达标: {researchedTechnologies.Count}/{victoryTechCount}");
            return true;
        }

        // 条件3：存活足够天数
        if (currentDay >= victoryDayCount)
        {
            Log.Info($"[胜利] 存活天数达标: {currentDay}/{victoryDayCount}");
            return true;
        }

        // 条件4：消灭所有敌对势力
        int enemy1Tiles = hexMap.GetFactionTileCount(TileFaction.Enemy1);
        int enemy2Tiles = hexMap.GetFactionTileCount(TileFaction.Enemy2);
        if (enemy1Tiles == 0 && enemy2Tiles == 0)
        {
            Log.Info("[胜利] 消灭所有敌对势力");
            return true;
        }

        return false;
    }

    /// <summary>检查是否满足失败条件</summary>
    bool CheckDefeat()
    {
        // 条件1：金币耗尽（破产）
        if (gold < bankruptcyThreshold)
        {
            Log.Info($"[失败] 破产: 金币 {gold} < 阈值 {bankruptcyThreshold}");
            return true;
        }

        // 条件2：领袖死亡（领袖不会死，但可以检查其他条件）
        if (leaderDead)
        {
            Log.Info("[失败] 领袖死亡");
            return true;
        }

        // 条件3：所有队员死亡
        if (playerTeamData != null)
        {
            bool allDead = true;
            foreach (var unit in playerTeamData)
            {
                if (unit != null && unit.currentHP > 0)
                {
                    allDead = false;
                    break;
                }
            }
            if (allDead)
            {
                Log.Info("[失败] 所有队员死亡");
                return true;
            }
        }

        return false;
    }

    /// <summary>显示胜利画面</summary>
    void ShowVictoryScreen()
    {
        Log.Info("[游戏结束] 胜利！");
        BattleLog.Add("<color=#ffdd44>★ 游戏胜利 ★</color>");
        BattleLog.Add($"存活天数: {currentDay}");
        BattleLog.Add($"占领地块: {GetFactionTileCount(TileFaction.Player)}");
        BattleLog.Add($"研究科技: {researchedTechnologies.Count}");

        // 显示胜利UI
        var gridManager = GetGridManager();
        if (gridManager != null)
        {
            gridManager.ShowVictoryUI();
        }
    }

    /// <summary>显示失败画面</summary>
    void ShowDefeatScreen()
    {
        Log.Info("[游戏结束] 失败！");
        BattleLog.Add("<color=#ff4444>★ 游戏失败 ★</color>");
        BattleLog.Add($"存活天数: {currentDay}");
        BattleLog.Add($"金币: {gold}");

        // 显示失败UI
        var gridManager = GetGridManager();
        if (gridManager != null)
        {
            gridManager.ShowDefeatUI();
        }
    }

    /// <summary>重置游戏</summary>
    public void ResetGame()
    {
        isGameOver = false;
        isVictory = false;
        currentDay = 1;
        gold = 100;
        actionPoints = 3;
        currentActionPoints = 3;
        dailyGoldIncome = 10;
        builtBuildings.Clear();
        buildingLevels.Clear();
        researchedTechnologies.Clear();
        currentResearch = "";
        researchProgress = 0;
        pendingEnemyAttack = false;

        // 重新创建测试数据
        CreateTestData();

        Log.Info("[游戏] 已重置");
    }

    // ========== 成就系统 ==========

    /// <summary>检查并解锁成就</summary>
    public void CheckAchievements()
    {
        // 生存成就
        if (currentDay >= 1) TryUnlockAchievement("first_day");
        if (currentDay >= 7) TryUnlockAchievement("survivor_7");
        if (currentDay >= 14) TryUnlockAchievement("survivor_14");
        if (currentDay >= 30) TryUnlockAchievement("survivor_30");

        // 战斗成就
        if (statistics.totalBattlesWon >= 1) TryUnlockAchievement("first_victory");
        if (statistics.totalBattlesWon >= 10) TryUnlockAchievement("veteran");
        if (statistics.totalBattlesWon >= 50) TryUnlockAchievement("legend");
        if (statistics.totalPerfectVictories >= 1) TryUnlockAchievement("perfect_victory");
        if (statistics.totalPerfectVictories >= 5) TryUnlockAchievement("perfectionist");

        // 经济成就
        if (statistics.totalGoldEarned >= 1000) TryUnlockAchievement("rich");
        if (statistics.totalGoldEarned >= 10000) TryUnlockAchievement("tycoon");
        if (statistics.totalBuildingsBuilt >= 5) TryUnlockAchievement("builder");
        if (statistics.totalBuildingsBuilt >= 10) TryUnlockAchievement("architect");

        // 科技成就
        if (statistics.totalTechnologiesResearched >= 1) TryUnlockAchievement("researcher");
        if (statistics.totalTechnologiesResearched >= 3) TryUnlockAchievement("scientist");
        if (statistics.totalTechnologiesResearched >= 5) TryUnlockAchievement("genius");

        // 征服成就
        if (statistics.totalTilesCaptured >= 10) TryUnlockAchievement("conqueror");
        if (statistics.totalTilesCaptured >= 20) TryUnlockAchievement("emperor");
        if (statistics.totalEnemiesKilled >= 100) TryUnlockAchievement("slayer");
        if (statistics.totalEnemiesKilled >= 500) TryUnlockAchievement("destroyer");

        // 英雄成就
        if (statistics.totalNamedHeroes >= 1) TryUnlockAchievement("first_hero");
        if (statistics.totalNamedHeroes >= 4) TryUnlockAchievement("full_party");
        if (statistics.totalSovereignLevel >= 10) TryUnlockAchievement("sovereign_10");
        if (statistics.totalSovereignLevel >= 15) TryUnlockAchievement("sovereign_15");

        // 特殊成就
        if (statistics.totalCrits >= 50) TryUnlockAchievement("crit_master");
        if (statistics.totalHealingDone >= 1000) TryUnlockAchievement("healer");
        if (statistics.totalDamageDealt >= 10000) TryUnlockAchievement("damage_dealer");
        if (statistics.totalStressReduced >= 500) TryUnlockAchievement("stress_manager");
    }

    /// <summary>尝试解锁成就</summary>
    bool TryUnlockAchievement(string achievementId)
    {
        if (unlockedAchievements.Contains(achievementId)) return false;

        unlockedAchievements.Add(achievementId);
        string achievementName = GetAchievementName(achievementId);
        Log.Info($"[成就] 解锁: {achievementName}");
        BattleLog.Add($"<color=#ffdd44>[成就] 解锁: {achievementName}</color>");
        return true;
    }

    /// <summary>获取成就名称</summary>
    string GetAchievementName(string achievementId)
    {
        switch (achievementId)
        {
            case "first_day": return "初来乍到";
            case "survivor_7": return "一周生存";
            case "survivor_14": return "两周生存";
            case "survivor_30": return "月度生存";
            case "first_victory": return "首战告捷";
            case "veteran": return "老兵";
            case "legend": return "传奇";
            case "perfect_victory": return "完美胜利";
            case "perfectionist": return "完美主义者";
            case "rich": return "富翁";
            case "tycoon": return "大亨";
            case "builder": return "建筑师";
            case "architect": return "首席建筑师";
            case "researcher": return "研究者";
            case "scientist": return "科学家";
            case "genius": return "天才";
            case "conqueror": return "征服者";
            case "emperor": return "帝王";
            case "slayer": return "屠杀者";
            case "destroyer": return "毁灭者";
            case "first_hero": return "首位英雄";
            case "full_party": return "满编队伍";
            case "sovereign_10": return "领袖10级";
            case "sovereign_15": return "领袖15级";
            case "crit_master": return "暴击大师";
            case "healer": return "治疗者";
            case "damage_dealer": return "伤害输出";
            case "stress_manager": return "压力管理";
            default: return achievementId;
        }
    }

    /// <summary>获取所有成就列表</summary>
    public List<AchievementInfo> GetAllAchievements()
    {
        var achievements = new List<AchievementInfo>();
        achievements.Add(new AchievementInfo { id = "first_day", name = "初来乍到", description = "存活1天", unlocked = unlockedAchievements.Contains("first_day") });
        achievements.Add(new AchievementInfo { id = "survivor_7", name = "一周生存", description = "存活7天", unlocked = unlockedAchievements.Contains("survivor_7") });
        achievements.Add(new AchievementInfo { id = "survivor_14", name = "两周生存", description = "存活14天", unlocked = unlockedAchievements.Contains("survivor_14") });
        achievements.Add(new AchievementInfo { id = "survivor_30", name = "月度生存", description = "存活30天", unlocked = unlockedAchievements.Contains("survivor_30") });
        achievements.Add(new AchievementInfo { id = "first_victory", name = "首战告捷", description = "赢得第一场战斗", unlocked = unlockedAchievements.Contains("first_victory") });
        achievements.Add(new AchievementInfo { id = "veteran", name = "老兵", description = "赢得10场战斗", unlocked = unlockedAchievements.Contains("veteran") });
        achievements.Add(new AchievementInfo { id = "legend", name = "传奇", description = "赢得50场战斗", unlocked = unlockedAchievements.Contains("legend") });
        achievements.Add(new AchievementInfo { id = "perfect_victory", name = "完美胜利", description = "无伤胜利", unlocked = unlockedAchievements.Contains("perfect_victory") });
        achievements.Add(new AchievementInfo { id = "perfectionist", name = "完美主义者", description = "无伤胜利5次", unlocked = unlockedAchievements.Contains("perfectionist") });
        achievements.Add(new AchievementInfo { id = "rich", name = "富翁", description = "获得1000金币", unlocked = unlockedAchievements.Contains("rich") });
        achievements.Add(new AchievementInfo { id = "tycoon", name = "大亨", description = "获得10000金币", unlocked = unlockedAchievements.Contains("tycoon") });
        achievements.Add(new AchievementInfo { id = "builder", name = "建筑师", description = "建造5座建筑", unlocked = unlockedAchievements.Contains("builder") });
        achievements.Add(new AchievementInfo { id = "architect", name = "首席建筑师", description = "建造10座建筑", unlocked = unlockedAchievements.Contains("architect") });
        achievements.Add(new AchievementInfo { id = "researcher", name = "研究者", description = "研究1项科技", unlocked = unlockedAchievements.Contains("researcher") });
        achievements.Add(new AchievementInfo { id = "scientist", name = "科学家", description = "研究3项科技", unlocked = unlockedAchievements.Contains("scientist") });
        achievements.Add(new AchievementInfo { id = "genius", name = "天才", description = "研究5项科技", unlocked = unlockedAchievements.Contains("genius") });
        achievements.Add(new AchievementInfo { id = "conqueror", name = "征服者", description = "占领10个地块", unlocked = unlockedAchievements.Contains("conqueror") });
        achievements.Add(new AchievementInfo { id = "emperor", name = "帝王", description = "占领20个地块", unlocked = unlockedAchievements.Contains("emperor") });
        achievements.Add(new AchievementInfo { id = "slayer", name = "屠杀者", description = "击杀100个敌人", unlocked = unlockedAchievements.Contains("slayer") });
        achievements.Add(new AchievementInfo { id = "destroyer", name = "毁灭者", description = "击杀500个敌人", unlocked = unlockedAchievements.Contains("destroyer") });
        achievements.Add(new AchievementInfo { id = "first_hero", name = "首位英雄", description = "命名第一位英雄", unlocked = unlockedAchievements.Contains("first_hero") });
        achievements.Add(new AchievementInfo { id = "full_party", name = "满编队伍", description = "命名4位英雄", unlocked = unlockedAchievements.Contains("full_party") });
        achievements.Add(new AchievementInfo { id = "sovereign_10", name = "领袖10级", description = "领袖达到10级", unlocked = unlockedAchievements.Contains("sovereign_10") });
        achievements.Add(new AchievementInfo { id = "sovereign_15", name = "领袖15级", description = "领袖达到15级", unlocked = unlockedAchievements.Contains("sovereign_15") });
        achievements.Add(new AchievementInfo { id = "crit_master", name = "暴击大师", description = "暴击50次", unlocked = unlockedAchievements.Contains("crit_master") });
        achievements.Add(new AchievementInfo { id = "healer", name = "治疗者", description = "治疗1000点伤害", unlocked = unlockedAchievements.Contains("healer") });
        achievements.Add(new AchievementInfo { id = "damage_dealer", name = "伤害输出", description = "造成10000点伤害", unlocked = unlockedAchievements.Contains("damage_dealer") });
        achievements.Add(new AchievementInfo { id = "stress_manager", name = "压力管理", description = "减少500点压力", unlocked = unlockedAchievements.Contains("stress_manager") });
        return achievements;
    }

    /// <summary>开始新的一天</summary>
    void StartNewDay()
    {
        currentDay++;
        currentActionPoints = actionPoints;
        isPlayerTurn = true;
        Log.Info($"[宏观天] === 第 {currentDay} 天开始 === 行动点已恢复");

        // 难度曲线系统
        ApplyDifficultyCurve();
    }

    /// <summary>应用难度曲线</summary>
    void ApplyDifficultyCurve()
    {
        // 基础难度增长
        int difficultyLevel = (int)(currentDay / GameBalanceConstants.DIFFICULTY_SCALE_INTERVAL);

        // 金币产出增长（鼓励扩张）
        dailyGoldIncome = GameBalanceConstants.BASE_DAILY_GOLD_INCOME + 
                         difficultyLevel * GameBalanceConstants.GOLD_PER_DIFFICULTY_LEVEL;

        // 敌人强度增长（通过模板ID体现）
        // 高天数时使用更高级的敌人模板
        int maxTemplateId = Mathf.Min(9, 4 + difficultyLevel);

        // 势力扩张速度增长
        float factionExpandBonus = difficultyLevel * GameBalanceConstants.FACTION_EXPAND_BONUS_PER_LEVEL;

        // 资源产出增长（鼓励占领）
        int resourceBonus = difficultyLevel * 2;

        if (currentDay % GameBalanceConstants.DIFFICULTY_SCALE_INTERVAL == 0)
        {
            Log.Info($"[难度] 第 {currentDay} 天：难度等级 {difficultyLevel}");
            Log.Info($"[难度] 金币产出: {dailyGoldIncome}/天");
            Log.Info($"[难度] 敌人模板上限: {maxTemplateId}");
            Log.Info($"[难度] 势力扩张加成: +{factionExpandBonus:P0}");
            Log.Info($"[难度] 资源产出加成: +{resourceBonus}");
        }

        // 每10天触发一次特殊事件
        if (currentDay % 10 == 0)
        {
            TriggerDayEvent(currentDay);
        }
    }

    /// <summary>触发每日特殊事件</summary>
    void TriggerDayEvent(int day)
    {
        float roll = RandomProvider.Current.Value;

        if (roll < GameBalanceConstants.DAILY_EVENT_RESOURCE_BOUNTY)
        {
            // 资源丰收
            int bonus = day * 2;
            gold += bonus;
            Log.Info($"[事件] 第 {day} 天：资源丰收！金币 +{bonus}");
            BattleLog.Add($"<color=#ffdd44>[事件] 资源丰收！金币 +{bonus}</color>");
        }
        else if (roll < GameBalanceConstants.DAILY_EVENT_RESOURCE_BOUNTY + GameBalanceConstants.DAILY_EVENT_ENEMY_INVASION)
        {
            // 敌人入侵
            pendingEnemyAttack = true;
            Log.Info($"[事件] 第 {day} 天：敌人大规模入侵！");
            BattleLog.Add($"<color=#ff4444>[事件] 敌人大规模入侵！</color>");
        }
        else if (roll < GameBalanceConstants.DAILY_EVENT_RESOURCE_BOUNTY + GameBalanceConstants.DAILY_EVENT_ENEMY_INVASION + GameBalanceConstants.DAILY_EVENT_TECH_BREAKTHROUGH)
        {
            // 科技突破
            if (!string.IsNullOrEmpty(currentResearch))
            {
                researchProgress += 3;
                Log.Info($"[事件] 第 {day} 天：科技突破！研究进度 +3");
                BattleLog.Add($"<color=#66ccff>[事件] 科技突破！研究进度 +3</color>");
            }
        }
        else
        {
            // 民众欢庆
            if (playerTeamData != null)
            {
                foreach (var unit in playerTeamData)
                {
                    if (unit != null && unit.currentHP > 0)
                    {
                        unit.stress = Mathf.Max(unit.stress - 10, 0);
                    }
                }
                Log.Info($"[事件] 第 {day} 天：民众欢庆！全队压力 -10");
                BattleLog.Add($"<color=#88ff88>[事件] 民众欢庆！全队压力 -10</color>");
            }
        }
    }

    /// <summary>是否有待处理的敌人袭击</summary>
    public bool pendingEnemyAttack = false;

    /// <summary>处理敌人袭击</summary>
    public void ProcessPendingEnemyAttack()
    {
        if (!pendingEnemyAttack) return;
        pendingEnemyAttack = false;

        // 触发一场战斗
        int templateId = RandomProvider.Current.Range(0, 10);
        StartDungeonBattle(templateId);
    }

    // ========== 建筑系统 ==========

    /// <summary>建筑定义</summary>
    public static readonly Dictionary<string, BuildingDefinition> BuildingDefinitions = new Dictionary<string, BuildingDefinition>
    {
        {"market", new BuildingDefinition { id = "market", name = "市场", description = "每日产出金币", baseCost = 50, costMultiplier = 1.5f, maxLevel = 5 }},
        {"barracks", new BuildingDefinition { id = "barracks", name = "兵营", description = "每日提供经验", baseCost = 80, costMultiplier = 1.8f, maxLevel = 3 }},
        {"infirmary", new BuildingDefinition { id = "infirmary", name = "治疗所", description = "每日恢复HP", baseCost = 60, costMultiplier = 1.6f, maxLevel = 5 }},
        {"laboratory", new BuildingDefinition { id = "laboratory", name = "研究所", description = "提供科研点数", baseCost = 100, costMultiplier = 2.0f, maxLevel = 3 }},
        {"watchtower", new BuildingDefinition { id = "watchtower", name = "瞭望塔", description = "降低敌人袭击概率", baseCost = 70, costMultiplier = 1.4f, maxLevel = 3 }}
    };

    /// <summary>建造建筑</summary>
    public bool BuildBuilding(string buildingId)
    {
        if (!BuildingDefinitions.ContainsKey(buildingId))
        {
            Log.Warn($"[建筑] 未知建筑: {buildingId}");
            return false;
        }

        var def = BuildingDefinitions[buildingId];
        int currentLevel = buildingLevels.ContainsKey(buildingId) ? buildingLevels[buildingId] : 0;

        if (currentLevel >= def.maxLevel)
        {
            Log.Warn($"[建筑] {def.name} 已达到最大等级 {def.maxLevel}");
            return false;
        }

        int cost = Mathf.RoundToInt(def.baseCost * Mathf.Pow(def.costMultiplier, currentLevel));
        if (gold < cost)
        {
            Log.Warn($"[建筑] 金币不足！需要 {cost}，当前 {gold}");
            return false;
        }

        gold -= cost;
        if (!builtBuildings.Contains(buildingId))
            builtBuildings.Add(buildingId);

        buildingLevels[buildingId] = currentLevel + 1;
        Log.Info($"[建筑] 建造/升级 {def.name} 至等级 {currentLevel + 1}，花费 {cost} 金币");

        // 立即应用建筑效果
        ApplyBuildingEffect(buildingId, currentLevel + 1);

        return true;
    }

    /// <summary>应用建筑效果</summary>
    void ApplyBuildingEffect(string buildingId, int level)
    {
        switch (buildingId)
        {
            case "market":
                dailyGoldIncome += 5 * level;
                Log.Info($"[建筑] 市场效果：每日金币 +{5 * level}");
                break;
            case "infirmary":
                dailyStressRecovery += 2 * level;
                Log.Info($"[建筑] 治疗所效果：每日压力恢复 +{2 * level}");
                break;
            case "watchtower":
                // 瞭望塔降低敌人袭击概率（在 ProcessEnemyActions 中检查）
                Log.Info($"[建筑] 瞭望塔效果：敌人袭击概率降低");
                break;
        }
    }

    // ========== 科技系统 ==========

    /// <summary>科技定义</summary>
    public static readonly Dictionary<string, TechDefinition> TechDefinitions = new Dictionary<string, TechDefinition>
    {
        {"advanced_weapons", new TechDefinition { id = "advanced_weapons", name = "高级武器", description = "全队攻击+2", cost = 5, prerequisites = new List<string>() }},
        {"magic_training", new TechDefinition { id = "magic_training", name = "魔法训练", description = "全队智力+3", cost = 8, prerequisites = new List<string> {"advanced_weapons"} }},
        {"defense_tactics", new TechDefinition { id = "defense_tactics", name = "防御战术", description = "全队防御+2", cost = 6, prerequisites = new List<string>() }},
        {"exploration", new TechDefinition { id = "exploration", name = "探索技术", description = "行动点+1", cost = 4, prerequisites = new List<string>() }},
        {"healing_arts", new TechDefinition { id = "healing_arts", name = "治疗术", description = "压力恢复+10", cost = 7, prerequisites = new List<string> {"defense_tactics"} }}
    };

    /// <summary>开始研究科技</summary>
    public bool StartResearch(string techId)
    {
        if (!TechDefinitions.ContainsKey(techId))
        {
            Log.Warn($"[科技] 未知科技: {techId}");
            return false;
        }

        if (researchedTechnologies.Contains(techId))
        {
            Log.Warn($"[科技] {techId} 已研究完成");
            return false;
        }

        if (!string.IsNullOrEmpty(currentResearch))
        {
            Log.Warn($"[科技] 当前正在研究 {currentResearch}，请先完成或取消");
            return false;
        }

        var def = TechDefinitions[techId];
        // 检查前置科技
        foreach (var prereq in def.prerequisites)
        {
            if (!researchedTechnologies.Contains(prereq))
            {
                Log.Warn($"[科技] 需要先研究前置科技: {TechDefinitions[prereq].name}");
                return false;
            }
        }

        currentResearch = techId;
        researchProgress = 0;
        Log.Info($"[科技] 开始研究: {def.name}");
        return true;
    }

    // ========== 存档系统 ==========

    /// <summary>保存游戏（队伍数据 + 地图位置 + 背包 + 地牢状态 + 宏观天 + 建筑 + 科技）</summary>
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.playerTeam = playerTeamData;
        data.squadX = savedSquadPos.x;
        data.squadY = savedSquadPos.y;
        data.inventory = inventory;
        data.gold = gold;
        data.dungeonFloor = dungeonSession.isInDungeon ? dungeonSession.currentFloor : 0;

        // 宏观天系统
        data.currentDay = currentDay;
        data.actionPoints = actionPoints;
        data.currentActionPoints = currentActionPoints;
        data.dailyGoldIncome = dailyGoldIncome;

        // 建筑系统
        data.builtBuildings = new List<string>(builtBuildings);
        data.buildingLevelKeys = new List<string>(buildingLevels.Keys);
        data.buildingLevelValues = new List<int>(buildingLevels.Values);

        // 科技系统
        data.researchedTechnologies = new List<string>(researchedTechnologies);
        data.currentResearch = currentResearch;
        data.researchProgress = researchProgress;

        SaveManager.Save(data);
    }

    /// <summary>读取存档，返回是否成功</summary>
    public bool LoadGame()
    {
        SaveData data = SaveManager.Load();
        if (data == null) return false;

        playerTeamData = data.playerTeam;
        savedSquadPos = new Vector2Int(data.squadX, data.squadY);
        inventory = data.inventory ?? new List<ItemStack>();
        gold = data.gold;
        foreach (var u in playerTeamData)
        {
            if (u.maxHp <= 0) u.maxHp = u.ComputeMaxHP();
            if (u.currentHP <= 0) u.currentHP = u.maxHp;
        }
        if (data.dungeonFloor > 0 && dungeonSession != null)
        {
            dungeonSession.currentFloor = data.dungeonFloor;
            dungeonSession.currentMap = DungeonGenerator.GenerateMap(data.dungeonFloor, data.dungeonFloor);
            dungeonSession.isInDungeon = true;
        }

        // 宏观天系统
        currentDay = data.currentDay > 0 ? data.currentDay : 1;
        actionPoints = data.actionPoints > 0 ? data.actionPoints : 3;
        currentActionPoints = data.currentActionPoints > 0 ? data.currentActionPoints : actionPoints;
        dailyGoldIncome = data.dailyGoldIncome > 0 ? data.dailyGoldIncome : 10;
        isPlayerTurn = true;

        // 建筑系统
        builtBuildings = data.builtBuildings ?? new List<string>();
        buildingLevels = new Dictionary<string, int>();
        if (data.buildingLevelKeys != null && data.buildingLevelValues != null)
        {
            for (int i = 0; i < data.buildingLevelKeys.Count && i < data.buildingLevelValues.Count; i++)
                buildingLevels[data.buildingLevelKeys[i]] = data.buildingLevelValues[i];
        }

        // 科技系统
        researchedTechnologies = data.researchedTechnologies ?? new List<string>();
        currentResearch = data.currentResearch ?? "";
        researchProgress = data.researchProgress;

        Log.Info($"[存档] 已加载 - 队伍{playerTeamData.Count}人, 位置({data.squadX},{data.squadY}), 背包{inventory.Count}件, 金币{gold}, 第{currentDay}天, 建筑{builtBuildings.Count}座, 科技{researchedTechnologies.Count}项");
        return true;
    }

    // ========== 平衡分析工具 ==========

    /// <summary>导出平衡数据到日志</summary>
    public void ExportBalanceData()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 平衡数据分析 ===");
        sb.AppendLine($"当前天数: {currentDay}");
        sb.AppendLine($"金币: {gold}");
        sb.AppendLine($"每日金币收入: {dailyGoldIncome}");
        sb.AppendLine($"行动点: {currentActionPoints}/{actionPoints}");
        sb.AppendLine($"建筑数量: {builtBuildings.Count}");
        sb.AppendLine($"科技数量: {researchedTechnologies.Count}");
        sb.AppendLine($"队伍人数: {playerTeamData?.Count ?? 0}");

        // 队伍属性统计
        if (playerTeamData != null && playerTeamData.Count > 0)
        {
            sb.AppendLine("\n--- 队伍属性统计 ---");
            int totalHP = 0, totalSTR = 0, totalDEF = 0, totalLevel = 0;
            foreach (var unit in playerTeamData)
            {
                if (unit == null) continue;
                totalHP += unit.maxHp;
                totalSTR += unit.STR;
                totalDEF += unit.DEF;
                totalLevel += unit.level;
            }
            sb.AppendLine($"平均HP: {totalHP / playerTeamData.Count}");
            sb.AppendLine($"平均STR: {totalSTR / playerTeamData.Count}");
            sb.AppendLine($"平均DEF: {totalDEF / playerTeamData.Count}");
            sb.AppendLine($"平均等级: {totalLevel / playerTeamData.Count:F1}");
        }

        // 建筑统计
        if (builtBuildings.Count > 0)
        {
            sb.AppendLine("\n--- 建筑统计 ---");
            foreach (var building in builtBuildings)
            {
                int level = buildingLevels.ContainsKey(building) ? buildingLevels[building] : 1;
                sb.AppendLine($"{building}: 等级 {level}");
            }
        }

        // 胜利条件进度
        sb.AppendLine("\n--- 胜利条件进度 ---");
        var hexMap = GetHexMap();
        if (hexMap != null)
        {
            int playerTiles = hexMap.GetFactionTileCount(TileFaction.Player);
            sb.AppendLine($"占领地块: {playerTiles}/{victoryTileCount}");
        }
        sb.AppendLine($"研究科技: {researchedTechnologies.Count}/{victoryTechCount}");
        sb.AppendLine($"存活天数: {currentDay}/{victoryDayCount}");

        Log.Info(sb.ToString());
    }

    /// <summary>获取游戏难度评估</summary>
    public string GetDifficultyAssessment()
    {
        int difficultyLevel = (int)(currentDay / GameBalanceConstants.DIFFICULTY_SCALE_INTERVAL);
        float goldPerDay = dailyGoldIncome;
        int teamPower = 0;

        if (playerTeamData != null)
        {
            foreach (var unit in playerTeamData)
            {
                if (unit != null)
                    teamPower += unit.STR + unit.DEF + unit.level * 2;
            }
        }

        string assessment = $"难度等级: {difficultyLevel}\n";
        assessment += $"金币效率: {goldPerDay:F0}/天\n";
        assessment += $"队伍战力: {teamPower}\n";

        if (teamPower < difficultyLevel * 10)
            assessment += "评估: 队伍实力不足，建议优先升级和装备";
        else if (teamPower > difficultyLevel * 20)
            assessment += "评估: 队伍实力强劲，可以积极扩张";
        else
            assessment += "评估: 队伍实力适中，稳步推进";

        return assessment;
    }

    public static void LoadSceneClean(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
