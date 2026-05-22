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
        // 如果没有队伍数据（新游戏），创建默认测试数据
        // 如果已经有数据（读档/继续游戏），跳过不做覆盖
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
                    effectType = SkillEffectType.Stun,
                    effectValue = 1, effectDuration = 1
                });
                skills.Add(new SkillData
                {
                    skillName = "嘲讽", description = "强迫敌人攻击自己",
                    baseDamage = 3, strScaling = 0.3f,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.Taunt,
                    effectDuration = 1
                });
                skills.Add(new SkillData
                {
                    skillName = "援护", description = "替一名友方承受伤害",
                    baseDamage = 0,
                    targetType = SkillTargetType.SingleAlly,
                    effectType = SkillEffectType.Protect,
                    effectDuration = 1
                });
                break;

            case "游侠":
                skills.Add(new SkillData
                {
                    skillName = "精准射击", description = "高单体伤害",
                    baseDamage = 14, agiScaling = 0.8f,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.DirectDamage
                });
                skills.Add(new SkillData
                {
                    skillName = "标记", description = "受伤+20%，持续2回合",
                    baseDamage = 3, agiScaling = 0.2f,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.Mark,
                    effectValue = 0.2f, effectDuration = 2
                });
                skills.Add(new SkillData
                {
                    skillName = "弹幕覆盖", description = "对全体敌人造成伤害",
                    baseDamage = 7, agiScaling = 0.3f,
                    targetType = SkillTargetType.AllEnemies,
                    effectType = SkillEffectType.AoEDamage
                });
                break;

            case "哥布林战士":
                skills.Add(new SkillData
                {
                    skillName = "重劈", description = "用力砍下去",
                    baseDamage = 8, strScaling = 0.6f,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.DirectDamage
                });
                break;

            case "哥布林弓手":
                skills.Add(new SkillData
                {
                    skillName = "精准射击", description = "远程射击",
                    baseDamage = 10, agiScaling = 0.6f,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.DirectDamage
                });
                break;

            case "哥布林萨满":
                skills.Add(new SkillData
                {
                    skillName = "治疗术", description = "恢复一名队友生命",
                    baseDamage = 15, strScaling = 0.5f, agiScaling = 0.5f,
                    targetType = SkillTargetType.SingleAlly,
                    effectType = SkillEffectType.Heal
                });
                skills.Add(new SkillData
                {
                    skillName = "图腾护盾", description = "为队友附加护盾吸收伤害",
                    baseDamage = 0,
                    targetType = SkillTargetType.SingleAlly,
                    effectType = SkillEffectType.Shield,
                    effectValue = 12, effectDuration = 2
                });
                skills.Add(new SkillData
                {
                    skillName = "火球术", description = "对血最少的敌人造成伤害",
                    baseDamage = 12, agiScaling = 0.4f,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.DirectDamage
                });
                skills.Add(new SkillData
                {
                    skillName = "恐吓", description = "施加压力伤害",
                    baseDamage = 0,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.Stress,
                    effectValue = 15
                });
                break;

            case "野狼":
                skills.Add(new SkillData
                {
                    skillName = "撕咬", description = "造成伤害 + 30%概率流血",
                    baseDamage = 10, strScaling = 0.8f,
                    targetType = SkillTargetType.SingleEnemy,
                    effectType = SkillEffectType.Bleed,
                    effectValue = 4, effectDuration = 2
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
