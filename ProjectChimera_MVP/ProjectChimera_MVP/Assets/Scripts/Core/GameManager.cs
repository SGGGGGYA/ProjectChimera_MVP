using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    WorldMap,
    Battle,
    Menu
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

    [Header("中文字体（拖入 Unifont_Universal_SDF）")]
    public TMPro.TMP_FontAsset chineseFont;

    [Header("物品定义")]
    public List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();

    [Header("职业定义")]
    public List<ClassDefinition> classDefinitions = new List<ClassDefinition>();

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

        if (playerTeamData == null || playerTeamData.Count == 0)
            CreateTestData();
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
                skillName = "盾牌猛击", description = "伤害+眩晕",
                baseDamage = 12, strScaling = 0.8f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = false,
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
                skillName = "嗜血斩击", description = "伤害+自损10%HP+吸血50%",
                baseDamage = 18,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = true,
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
                    new ModifyAttributeCommand { modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "狂暴之力") },
                    new ModifyAttributeCommand { modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.DEF, -3f, "狂暴之力") },
                    new ApplyStatusCommand { statusType = StatusType.Berserk, duration = 3 }
                }
            },
            new SkillData
            {
                skillName = "殊死一搏", description = "血量越低伤害越高,最高+100%",
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
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
                skillName = "精神冲击", description = "精神伤害+15压力",
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
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
                    new ModifyAttributeCommand { modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.INT, 2f, "智慧启迪") },
                    new ApplyStatusCommand { statusType = StatusType.Enlightened, duration = 2 }
                }
            },
            new SkillData
            {
                skillName = "禁忌知识", description = "高额精神伤害,自身+25压力",
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 2, maxUserRank = 3,
                canTargetFrontRank = true, canTargetBackRank = true,
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
                skillName = "重劈", description = "用力砍下去",
                baseDamage = 8, strScaling = 0.6f,
                targetType = SkillTargetType.SingleEnemy,
                minUserRank = 0, maxUserRank = 1,
                canTargetFrontRank = true, canTargetBackRank = false,
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

        Debug.Log($"[ClassDefinition] 已加载 {classDefinitions.Count} 个职业定义");
    }

    public ClassDefinition GetClassDefinition(string unitName)
    {
        return classDefinitions.Find(c => c.unitName == unitName);
    }

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
                equippedArmor = MakeTestArmor("plate_armor", "铠甲", 3)
            },
            new UnitBattleData
            {
                unitName = "狂战士", rank = 1,
                VIT = 10, STR = 12, DEF = 4, AGI = 4, INT = 2,
                weaponAttack = 6, level = 3, isPlayer = true,
                equippedWeapon = MakeTestWeapon("great_axe", "巨斧", StatType.STR, 5),
                equippedArmor = MakeTestArmor("hide_armor", "硬皮甲", 2)
            },
            new UnitBattleData
            {
                unitName = "游侠", rank = 2,
                VIT = 5, STR = 6, DEF = 4, AGI = 10, INT = 4,
                weaponAttack = 3, level = 3, isPlayer = true,
                equippedWeapon = MakeTestWeapon("short_bow", "短弓", StatType.AGI, 2),
                equippedArmor = MakeTestArmor("leather_armor", "皮甲", 2)
            },
            new UnitBattleData
            {
                unitName = "学者", rank = 3,
                VIT = 4, STR = 3, DEF = 3, AGI = 7, INT = 12,
                weaponAttack = 2, level = 3, isPlayer = true,
                equippedWeapon = MakeTestWeapon("arcane_staff", "奥术法杖", StatType.INT, 4),
                equippedArmor = MakeTestArmor("robe", "法袍", 1)
            }
        };

        inventory = new List<ItemStack>
        {
            new ItemStack("health_potion", 2),
            new ItemStack("stress_herb", 1),
        };
    }

    
    public void StartBattle()
    {
        Debug.Log("[GameManager] StartBattle() 被调用");
        
        string sceneName = "BattleScene";
        bool canLoad = Application.CanStreamedLevelBeLoaded(sceneName);
        Debug.Log($"[GameManager] 场景 '{sceneName}' 是否可以加载: {canLoad}");
        
        if (!canLoad)
        {
            Debug.LogError($"[GameManager] 场景 '{sceneName}' 不在 Build Settings 中！请在 File > Build Settings 中添加该场景。");
            return;
        }

        int template = Random.Range(0, 2);
        if (template == 0)
        {
            enemyTeamData = new List<UnitBattleData>
            {
                new UnitBattleData { unitName = "哥布林战士", isPlayer = false, rank = 0,
                    VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1, weaponAttack = 3, level = 2,
                    equippedWeapon = MakeTestWeapon("rusty_sword", "锈剑", StatType.STR, 1),
                    equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1) },
                new UnitBattleData { unitName = "哥布林战士", isPlayer = false, rank = 1,
                    VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1, weaponAttack = 3, level = 2,
                    equippedWeapon = MakeTestWeapon("rusty_sword", "锈剑", StatType.STR, 1),
                    equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1) },
                new UnitBattleData { unitName = "哥布林弓手", isPlayer = false, rank = 2,
                    VIT = 1, STR = 7, DEF = 2, AGI = 9, INT = 2, weaponAttack = 2, level = 2,
                    equippedWeapon = MakeTestWeapon("short_bow", "短弓", StatType.AGI, 1),
                    equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1) },
                new UnitBattleData { unitName = "哥布林弓手", isPlayer = false, rank = 3,
                    VIT = 1, STR = 7, DEF = 2, AGI = 9, INT = 2, weaponAttack = 2, level = 2,
                    equippedWeapon = MakeTestWeapon("short_bow", "短弓", StatType.AGI, 1),
                    equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1) }
            };
        }
        else
        {
            enemyTeamData = new List<UnitBattleData>
            {
                new UnitBattleData { unitName = "哥布林战士", isPlayer = false, rank = 0,
                    VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1, weaponAttack = 3, level = 2,
                    equippedWeapon = MakeTestWeapon("rusty_sword", "锈剑", StatType.STR, 1),
                    equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1) },
                new UnitBattleData { unitName = "野狼", isPlayer = false, rank = 1,
                    VIT = 4, STR = 9, DEF = 3, AGI = 12, INT = 1, weaponAttack = 2, level = 2,
                    equippedWeapon = MakeTestWeapon("sharp_claws", "利爪", StatType.STR, 2),
                    equippedArmor = MakeTestArmor("thick_hide", "厚皮", 2) },
                new UnitBattleData { unitName = "哥布林弓手", isPlayer = false, rank = 2,
                    VIT = 1, STR = 7, DEF = 2, AGI = 9, INT = 2, weaponAttack = 2, level = 2,
                    equippedWeapon = MakeTestWeapon("short_bow", "短弓", StatType.AGI, 1),
                    equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1) },
                new UnitBattleData { unitName = "哥布林萨满", isPlayer = false, rank = 3,
                    VIT = 2, STR = 4, DEF = 2, AGI = 3, INT = 8, weaponAttack = 0, level = 2,
                    equippedWeapon = MakeTestWeapon("spirit_staff", "法杖", StatType.INT, 3),
                    equippedArmor = MakeTestArmor("cloth_armor", "布甲", 1) }
            };
        }

        currentState = GameState.Battle;
        Debug.Log("[GameManager] 准备加载 BattleScene...");
        SceneManager.LoadScene("BattleScene", LoadSceneMode.Single);
        Debug.Log("[GameManager] LoadScene 已调用");
    }

    public void ReturnToWorldMap()
    {
        // 返回前自动存档
        SaveGame();

        string sceneName = "WorldMap";
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[GameManager] 场景 '{sceneName}' 不在 Build Settings 中！请在 File > Build Settings 中添加该场景。");
            return;
        }

        currentState = GameState.WorldMap;
        Debug.Log("[GameManager] 返回世界地图...");
        SceneManager.LoadScene("WorldMap", LoadSceneMode.Single);
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

    /// <summary>保存游戏（队伍数据 + 地图位置 + 背包）</summary>
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.playerTeam = playerTeamData;
        data.squadX = savedSquadPos.x;
        data.squadY = savedSquadPos.y;
        data.inventory = inventory;
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
        Debug.Log($"[存档] 已加载 - 队伍{playerTeamData.Count}人, 位置({data.squadX},{data.squadY}), 背包{inventory.Count}件");
        return true;
    }
}
