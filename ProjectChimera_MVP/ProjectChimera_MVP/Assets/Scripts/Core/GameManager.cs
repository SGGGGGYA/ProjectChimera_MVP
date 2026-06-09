using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectChimera.Core;

public enum GameState
{
    WorldMap,
    Battle,
    Menu,
    Dungeon
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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AudioListener al = GetComponent<AudioListener>();
            if (al != null) Destroy(al);

            Camera cam = GetComponent<Camera>();
            if (cam != null) Destroy(cam);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (!StressManager.initialized)
            StressManager.InitFromConfig(new StressConfig());

        if (chineseFont != null)
            UIFonts.chineseFont = chineseFont;

        ItemDatabase.Initialize(itemDefinitions);
        InitClassDefinitions();

        if (FindObjectOfType<UIInventoryController>() == null)
        {
            var invGO = new GameObject("UIInventoryController");
            DontDestroyOnLoad(invGO);
            invGO.AddComponent<UIInventoryController>();
        }

        if (FindObjectOfType<UIFormationController>() == null)
        {
            var fcGO = new GameObject("UIFormationController");
            DontDestroyOnLoad(fcGO);
            fcGO.AddComponent<UIFormationController>();
        }

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            DontDestroyOnLoad(es);
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        if (FindObjectOfType<UIPauseMenu>() == null)
        {
            var pmGO = new GameObject("UIPauseMenu");
            DontDestroyOnLoad(pmGO);
            pmGO.AddComponent<UIPauseMenu>();
        }

        if (FindObjectOfType<UIRecruitController>() == null)
        {
            var rcGO = new GameObject("UIRecruitController");
            DontDestroyOnLoad(rcGO);
            rcGO.AddComponent<UIRecruitController>();
        }

        if (FindObjectOfType<UIDungeonMapController>() == null)
        {
            var dgo = new GameObject("UIDungeonMapController");
            DontDestroyOnLoad(dgo);
            dgo.AddComponent<UIDungeonMapController>();
        }

        if (FindObjectOfType<UIDebugConsole>() == null)
        {
            var dcg = new GameObject("UIDebugConsole");
            DontDestroyOnLoad(dcg);
            dcg.AddComponent<UIDebugConsole>();
        }

        if (FindObjectOfType<UICampController>() == null)
        {
            var cpGO = new GameObject("UICampController");
            DontDestroyOnLoad(cpGO);
            cpGO.AddComponent<UICampController>();
        }

        if (dungeonSession.isInDungeon)
        {
            var ctrl = FindObjectOfType<UIDungeonMapController>();
            if (ctrl != null) ctrl.Open();
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
    }

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
        LoadSceneClean("BattleScene");
    }

    public void ReturnToWorldMap()
    {
        if (dungeonSession.isInDungeon)
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
            return;
        }

        // 返回前自动存档
        SaveGame();

        string sceneName2 = "WorldMap";
        if (!Application.CanStreamedLevelBeLoaded(sceneName2))
        {
            Log.Error($"[GameManager] 场景 '{sceneName2}' 不在 Build Settings 中！请在 File > Build Settings 中添加该场景。");
            return;
        }

        currentState = GameState.WorldMap;
        Log.Info("[GameManager] 返回世界地图...");
        LoadSceneClean("WorldMap");
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

    // ========== 存档系统 ==========

    /// <summary>保存游戏（队伍数据 + 地图位置 + 背包 + 地牢状态）</summary>
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.playerTeam = playerTeamData;
        data.squadX = savedSquadPos.x;
        data.squadY = savedSquadPos.y;
        data.inventory = inventory;
        data.gold = gold;
        data.dungeonFloor = dungeonSession.isInDungeon ? dungeonSession.currentFloor : 0;
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
        Log.Info($"[存档] 已加载 - 队伍{playerTeamData.Count}人, 位置({data.squadX},{data.squadY}), 背包{inventory.Count}件, 金币{gold}");
        return true;
    }

    public static void LoadSceneClean(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
