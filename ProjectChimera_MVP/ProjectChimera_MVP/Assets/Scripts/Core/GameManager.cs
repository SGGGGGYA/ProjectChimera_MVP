using System.Collections;
using System.Collections.Generic;
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

        if (playerTeamData == null || playerTeamData.Count == 0)
            CreateTestData();
    }

    void CreateTestData()
    {
        playerTeamData = new List<UnitBattleData>
        {
            new UnitBattleData
            {
                unitName = "战士",
                VIT = 8, STR = 10, DEF = 6, AGI = 5, INT = 3,
                weaponAttack = 5, level = 3, isPlayer = true
            },
            new UnitBattleData
            {
                unitName = "游侠",
                VIT = 5, STR = 6, DEF = 4, AGI = 10, INT = 4,
                weaponAttack = 3, level = 3, isPlayer = true
            },
            new UnitBattleData
            {
                unitName = "狂战士",
                VIT = 10, STR = 12, DEF = 4, AGI = 4, INT = 2,
                weaponAttack = 6, level = 3, isPlayer = true
            },
            new UnitBattleData
            {
                unitName = "学者",
                VIT = 4, STR = 3, DEF = 3, AGI = 7, INT = 12,
                weaponAttack = 2, level = 3, isPlayer = true
            }
        };
    }

    /// <summary>
    /// 根据单位名称返回预设技能列表
    /// </summary>
    public static List<SkillData> GetDefaultSkills(string unitName)
    {
        var skills = new List<SkillData>();

        switch (unitName)
        {
            case "战士":
                skills.Add(new SkillData
                {
                    skillName = "盾牌猛击", description = "伤害+眩晕",
                    baseDamage = 12, strScaling = 0.8f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.Stun,
                    effectValue = 1, effectDuration = 1,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 12, strScaling = 0.8f },
                        new ApplyStatusCommand { statusType = StatusType.Stun, duration = 1 }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "嘲讽", description = "强迫敌人攻击自己",
                    baseDamage = 3, strScaling = 0.3f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.Taunt,
                    effectDuration = 1,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 3, strScaling = 0.3f },
                        new ApplyStatusCommand { statusType = StatusType.Taunt, duration = 1 }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "援护", description = "替一名友方承受伤害",
                    baseDamage = 0,
                    targetType = SkillTargetType.SingleAlly,
                    aiCategory = SkillEffectType.Protect,
                    effectDuration = 1,
                    commands = new List<Command>
                    {
                        new RemoveStatusCommand { statusType = StatusType.Protected, removeFromAllAllies = true },
                        new ApplyStatusCommand { statusType = StatusType.Protected, duration = 1 }
                    }
                });
                break;

            case "游侠":
                skills.Add(new SkillData
                {
                    skillName = "精准射击", description = "高单体伤害",
                    baseDamage = 14, agiScaling = 0.8f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.DirectDamage,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 14, agiScaling = 0.8f }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "标记", description = "受伤+20%，持续2回合",
                    baseDamage = 3, agiScaling = 0.2f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.Mark,
                    effectValue = 0.2f, effectDuration = 2,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 3, agiScaling = 0.2f },
                        new ApplyStatusCommand { statusType = StatusType.Mark, duration = 2, effectValue = 0.2f }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "弹幕覆盖", description = "对全体敌人造成伤害",
                    baseDamage = 7, agiScaling = 0.3f,
                    targetType = SkillTargetType.AllEnemies,
                    aiCategory = SkillEffectType.AoEDamage,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 7, agiScaling = 0.3f, isAoE = true }
                    }
                });
                break;

            case "狂战士":
                skills.Add(new SkillData
                {
                    skillName = "嗜血斩击", description = "伤害+自损10%HP+吸血50%",
                    baseDamage = 18,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.BloodthirstyStrike,
                    commands = new List<Command>
                    {
                        new BloodthirstyStrikeCommand { baseDamage = 18, strScaling = 1.0f }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "狂暴之力", description = "STR+5, DEF-3, 持续3回合",
                    targetType = SkillTargetType.Self,
                    aiCategory = SkillEffectType.BerserkBuff,
                    effectDuration = 3,
                    commands = new List<Command>
                    {
                        new ModifyAttributeCommand { modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.STR, 5f, "狂暴之力") },
                        new ModifyAttributeCommand { modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.DEF, -3f, "狂暴之力") },
                        new ApplyStatusCommand { statusType = StatusType.Berserk, duration = 3 }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "殊死一搏", description = "血量越低伤害越高,最高+100%",
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.DesperateStrike,
                    commands = new List<Command>
                    {
                        new DesperateStrikeCommand { baseDamage = 12, strScaling = 0.8f, maxBonusMultiplier = 1.0f }
                    }
                });
                break;

            case "学者":
                skills.Add(new SkillData
                {
                    skillName = "精神冲击", description = "精神伤害+15压力",
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.MindShock,
                    commands = new List<Command>
                    {
                        new MindShockCommand { baseDamage = 10, intScaling = 1.2f, stressAmount = 15 }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "智慧启迪", description = "减压20点+INT+2持续2回合",
                    targetType = SkillTargetType.SingleAlly,
                    aiCategory = SkillEffectType.WisdomEnlightenment,
                    effectDuration = 2,
                    commands = new List<Command>
                    {
                        new ConsumeResourceCommand { resourceType = ConsumeResourceCommand.ResourceType.Stress, amount = -20 },
                        new ModifyAttributeCommand { modifier = new AttributeModifier(ModifierType.Add, AttributeTarget.INT, 2f, "智慧启迪") },
                        new ApplyStatusCommand { statusType = StatusType.Enlightened, duration = 2 }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "禁忌知识", description = "高额精神伤害,自身+25压力",
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.ForbiddenKnowledge,
                    commands = new List<Command>
                    {
                        new MindShockCommand { baseDamage = 20, intScaling = 1.5f, stressAmount = 0 },
                        new SelfInflictCommand { inflictType = SelfInflictCommand.SelfInflictType.Stress, amount = 25 }
                    }
                });
                break;

            case "哥布林战士":
                skills.Add(new SkillData
                {
                    skillName = "重劈", description = "用力砍下去",
                    baseDamage = 8, strScaling = 0.6f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.DirectDamage,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 8, strScaling = 0.6f }
                    }
                });
                break;

            case "哥布林弓手":
                skills.Add(new SkillData
                {
                    skillName = "精准射击", description = "远程射击",
                    baseDamage = 10, agiScaling = 0.6f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.DirectDamage,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 10, agiScaling = 0.6f }
                    }
                });
                break;

            case "哥布林萨满":
                skills.Add(new SkillData
                {
                    skillName = "治疗术", description = "恢复一名队友生命",
                    baseDamage = 15, strScaling = 0.5f, agiScaling = 0.5f,
                    targetType = SkillTargetType.SingleAlly,
                    aiCategory = SkillEffectType.Heal,
                    commands = new List<Command>
                    {
                        new HealCommand { baseHeal = 15, intScaling = 1.0f }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "图腾护盾", description = "为队友附加护盾吸收伤害",
                    baseDamage = 0,
                    targetType = SkillTargetType.SingleAlly,
                    aiCategory = SkillEffectType.Shield,
                    effectValue = 12, effectDuration = 2,
                    commands = new List<Command>
                    {
                        new ShieldCommand { shieldAmount = 12 }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "火球术", description = "对血最少的敌人造成伤害",
                    baseDamage = 12, agiScaling = 0.4f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.DirectDamage,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 12, agiScaling = 0.4f }
                    }
                });
                skills.Add(new SkillData
                {
                    skillName = "恐吓", description = "施加压力伤害",
                    baseDamage = 0,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.Stress,
                    effectValue = 15,
                    commands = new List<Command>
                    {
                        new ConsumeResourceCommand { resourceType = ConsumeResourceCommand.ResourceType.Stress, amount = 15 }
                    }
                });
                break;

            case "野狼":
                skills.Add(new SkillData
                {
                    skillName = "撕咬", description = "造成伤害 + 30%概率流血",
                    baseDamage = 10, strScaling = 0.8f,
                    targetType = SkillTargetType.SingleEnemy,
                    aiCategory = SkillEffectType.Bleed,
                    effectValue = 4, effectDuration = 2,
                    commands = new List<Command>
                    {
                        new DealDamageCommand { baseDamage = 10, strScaling = 0.8f },
                        new ApplyStatusCommand { statusType = StatusType.Bleed, duration = 2, effectValue = 4 }
                    }
                });
                break;
        }

        return skills;
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

        enemyTeamData = new List<UnitBattleData>
        {
            new UnitBattleData
            {
                unitName = "哥布林战士",
                VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1,
                weaponAttack = 3, level = 2, isPlayer = false
            },
            new UnitBattleData
            {
                unitName = "哥布林弓手",
                VIT = 1, STR = 7, DEF = 2, AGI = 9, INT = 2,
                weaponAttack = 2, level = 2, isPlayer = false
            }
        };

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

    // ========== 存档系统 ==========

    /// <summary>保存游戏（队伍数据 + 地图位置）</summary>
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.playerTeam = playerTeamData;
        data.squadX = savedSquadPos.x;
        data.squadY = savedSquadPos.y;
        SaveManager.Save(data);
    }

    /// <summary>读取存档，返回是否成功</summary>
    public bool LoadGame()
    {
        SaveData data = SaveManager.Load();
        if (data == null) return false;

        playerTeamData = data.playerTeam;
        savedSquadPos = new Vector2Int(data.squadX, data.squadY);
        Debug.Log($"[存档] 已加载 - 队伍{playerTeamData.Count}人, 位置({data.squadX},{data.squadY})");
        return true;
    }
}
