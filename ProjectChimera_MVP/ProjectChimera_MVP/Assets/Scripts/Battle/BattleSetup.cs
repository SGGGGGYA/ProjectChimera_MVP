using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSetup : MonoBehaviour
{
    [Header("位置锚点模板")]
    public List<Transform> playerSpawnPoints;  // 拖入 Player_Pos_0 ~ 3
    public List<Transform> enemySpawnPoints;   // 拖入 Enemy_Pos_0 ~ 3

    [Header("角色预制体")]
    public GameObject universalCharacterPrefab;  // 万能角色预制体（Spine骨骼）

    [System.Serializable]
    public class CharacterDataEntry
    {
        public string unitName;                   // 匹配 GameManager 里的单位名
        public Spine.Unity.SkeletonDataAsset skeletonDataAsset;  // 对应的骨骼数据
    }

    public List<CharacterDataEntry> characterDataEntries;  // 拖入所有角色的骨骼数据

    [Header("UI 引用")]
    public GameObject hpBarPrefab;    // 血条预制体
    public Canvas battleCanvas;       // 战斗场景的 Canvas

    // 独立调试用的默认队伍数据
    private static List<UnitBattleData> GetDefaultPlayerTeam()
    {
        return new List<UnitBattleData>
        {
            new UnitBattleData
            {
                unitName = "战士", isPlayer = true,
                VIT = 8, STR = 10, DEF = 6, AGI = 5, INT = 3,
                weaponAttack = 5, level = 3
            },
            new UnitBattleData
            {
                unitName = "游侠", isPlayer = true,
                VIT = 5, STR = 6, DEF = 4, AGI = 10, INT = 4,
                weaponAttack = 3, level = 3
            }
        };
    }

    private static List<UnitBattleData> GetDefaultEnemyTeam()
    {
        // 随机遭遇配置
        var encounters = new List<List<UnitBattleData>>
        {
            // 遭遇1：原版哥布林组合
            new List<UnitBattleData>
            {
                new UnitBattleData
                {
                    unitName = "哥布林战士", isPlayer = false,
                    VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1,
                    weaponAttack = 3, level = 2
                },
                new UnitBattleData
                {
                    unitName = "哥布林弓手", isPlayer = false,
                    VIT = 1, STR = 7, DEF = 2, AGI = 9, INT = 2,
                    weaponAttack = 2, level = 2
                }
            },
            // 遭遇2：哥布林萨满 + 哥布林战士（护卫+辅助）
            new List<UnitBattleData>
            {
                new UnitBattleData
                {
                    unitName = "哥布林萨满", isPlayer = false,
                    VIT = 2, STR = 4, DEF = 2, AGI = 3, INT = 8,
                    weaponAttack = 0, level = 2
                },
                new UnitBattleData
                {
                    unitName = "哥布林战士", isPlayer = false,
                    VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1,
                    weaponAttack = 3, level = 2
                }
            },
            // 遭遇3：野狼群（双狼）
            new List<UnitBattleData>
            {
                new UnitBattleData
                {
                    unitName = "野狼", isPlayer = false,
                    VIT = 4, STR = 9, DEF = 3, AGI = 12, INT = 1,
                    weaponAttack = 2, level = 2
                },
                new UnitBattleData
                {
                    unitName = "野狼", isPlayer = false,
                    VIT = 3, STR = 8, DEF = 3, AGI = 10, INT = 1,
                    weaponAttack = 2, level = 2
                }
            }
        };

        return encounters[Random.Range(0, encounters.Count)];
    }

    void Awake()
    {
        Debug.Log("[BattleSetup] Awake 开始执行...");

        // 获取队伍数据（优先用 GameManager，没有则用默认调试数据）
        List<UnitBattleData> playerTeamData;
        List<UnitBattleData> enemyTeamData;

        // 玩家数据：优先用 GameManager，没有则用默认
        if (GameManager.Instance != null &&
            GameManager.Instance.playerTeamData != null &&
            GameManager.Instance.playerTeamData.Count > 0)
        {
            playerTeamData = GameManager.Instance.playerTeamData;
            Debug.Log("[BattleSetup] 使用 GameManager 的玩家数据");
        }
        else
        {
            playerTeamData = GetDefaultPlayerTeam();
            Debug.Log("[BattleSetup] 无 GameManager，使用默认玩家数据");
        }

        // 敌人数据：始终随机遭遇，增加多样性
        enemyTeamData = GetDefaultEnemyTeam();
        Debug.Log($"[BattleSetup] 随机遭遇 - {enemyTeamData[0].unitName} + {enemyTeamData[1].unitName}");

        // 创建所有玩家单位（吸附到 PlayerPos 锚点）
        List<UnitData> playerUnits = new List<UnitData>();
        for (int i = 0; i < playerTeamData.Count; i++)
        {
            Vector3 pos = Vector3.zero;
            if (i < playerSpawnPoints.Count && playerSpawnPoints[i] != null)
                pos = playerSpawnPoints[i].position;
            else
                pos = new Vector3(-5f + i * 2f, -0.5f, 0f);  // 兜底
            UnitData unit = CreateUnit(playerTeamData[i], pos);
            if (unit != null)
                playerUnits.Add(unit);
            else
                Debug.LogError($"[BattleSetup] 玩家 {playerTeamData[i].unitName} 生成失败");
        }

        // 创建所有敌人单位（吸附到 EnemyPos 锚点）
        List<UnitData> enemyUnits = new List<UnitData>();
        for (int i = 0; i < enemyTeamData.Count; i++)
        {
            Vector3 pos = Vector3.zero;
            if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null)
                pos = enemySpawnPoints[i].position;
            else
                pos = new Vector3(2f + i * 2f, -0.5f, 0f);  // 兜底
            UnitData unit = CreateUnit(enemyTeamData[i], pos);
            if (unit != null)
                enemyUnits.Add(unit);
            else
                Debug.LogError($"[BattleSetup] 敌人 {enemyTeamData[i].unitName} 生成失败");
        }

        Debug.Log($"[BattleSetup] 创建完成 - 玩家:{playerUnits.Count}人, 敌人:{enemyUnits.Count}人");

        // 设置 BattleManager（如果场景没放，自动创建一个）
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm == null)
        {
            GameObject bmGO = new GameObject("BattleManager");
            bm = bmGO.AddComponent<BattleManager>();
            Debug.Log("[BattleSetup] 自动创建了 BattleManager");
        }
        bm.playerUnits = playerUnits;
        bm.enemyUnits = enemyUnits;
        Debug.Log("[BattleSetup] BattleManager 编队设置完成");

        // 初始化回合制（如果场景没放 TurnManager，自动创建一个）
        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm == null)
        {
            GameObject tmGO = new GameObject("TurnManager");
            tm = tmGO.AddComponent<TurnManager>();
            Debug.Log("[BattleSetup] 自动创建了 TurnManager");
        }
        tm.InitializeBattle(playerUnits, enemyUnits);
        Debug.Log("[BattleSetup] TurnManager 初始化完成");
    }

    /// <summary>
    /// 计算阵型坐标（暗黑地牢风格）
    /// 玩家：左半场，index 0 = 前排（靠右近敌），以此类推
    /// 敌人：右半场，index 0 = 前排（靠左近敌），以此类推
    /// Y 轴错落：奇偶交替，避免死板直线
    /// </summary>


    UnitData CreateUnit(UnitBattleData data, Vector3 position)
    {
        // ==================== 防御性克隆 ====================

        // 检查万能预制体
        if (universalCharacterPrefab == null)
        {
            Debug.LogError($"[BattleSetup] universalCharacterPrefab 未绑定！无法生成 {data.unitName}");
            return null;
        }

        // 克隆预制体
        GameObject go;
        try
        {
            go = Instantiate(universalCharacterPrefab, position, Quaternion.identity);
            go.name = data.unitName;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleSetup] 克隆预制体失败: {e.Message}");
            return null;
        }

        // ==================== Spine 骨骼动画 ====================

        if (go != null)
        {
            Spine.Unity.SkeletonAnimation skeleton = null;
            try
            {
                skeleton = go.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
            }
            catch { /* Spine 未安装时跳过 */ }

            if (skeleton != null)
            {
                bool skeletonAssigned = false;

                // 查找对应角色名的骨骼数据
                if (characterDataEntries != null)
                {
                    foreach (var entry in characterDataEntries)
                    {
                        if (entry == null) continue;
                        if (entry.unitName == data.unitName && entry.skeletonDataAsset != null)
                        {
                            skeleton.skeletonDataAsset = entry.skeletonDataAsset;
                            try
                            {
                                skeleton.Initialize(true);
                                Debug.Log($"[Spine] {data.unitName} 加载骨骼: {entry.skeletonDataAsset.name}");
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"[Spine] {data.unitName} Initialize 失败: {ex.Message}");
                            }
                            skeletonAssigned = true;
                            break;
                        }
                    }
                }

                if (!skeletonAssigned)
                {
                    Debug.LogWarning($"[Spine] {data.unitName} 未找到对应的 SkeletonDataAsset，使用默认外观");
                }

                // 阵营调色（Spine 3.6 API）
                try
                {
                    if (data.isPlayer)
                    {
                        skeleton.skeleton.r = 1f; skeleton.skeleton.g = 1f;
                        skeleton.skeleton.b = 1f; skeleton.skeleton.a = 1f;
                    }
                    else
                    {
                        skeleton.skeleton.r = 1f; skeleton.skeleton.g = 0.6f;
                        skeleton.skeleton.b = 0.6f; skeleton.skeleton.a = 1f;
                    }
                }
                catch { /* 调色失败不影响运行 */ }

                // 面对面镜像翻转（控制 GameObject 本地缩放 X）
                try
                {
                    Vector3 ls = go.transform.localScale;
                    ls.x = data.isPlayer ? -Mathf.Abs(ls.x) : Mathf.Abs(ls.x);
                    go.transform.localScale = ls;
                }
                catch { }

                // 体型缩放（缩小至黄金比例）
                try { go.transform.localScale = Vector3.one * 0.75f; } catch { }
            }
        }

        // ==================== UnitData 初始化 ====================

        if (go == null)
        {
            Debug.LogError($"[BattleSetup] {data.unitName} 的 GameObject 为 null，无法初始化 UnitData");
            return null;
        }

        UnitData unit = go.GetComponent<UnitData>();
        if (unit == null)
            unit = go.AddComponent<UnitData>();

        unit.unitName = data.unitName;
        unit.isPlayer = data.isPlayer;
        unit.VIT = data.VIT;
        unit.STR = data.STR;
        unit.baseDefense = data.DEF;
        unit.AGI = data.AGI;
        unit.INT = data.INT;
        unit.weaponAttack = data.weaponAttack;
        unit.level = data.level;
        unit.currentExp = data.currentExp;
        unit.currentHP = unit.MaxHp;
        unit.stress = data.stress;
        unit.stressResistRate = Mathf.Min(StressManager.config.baseResist + data.level * StressManager.config.resistPerLevel, StressManager.config.maxResist);
        unit.skills = GameManager.GetDefaultSkills(data.unitName);

        // UnitClickDetector 已在预制体上，无需 AddComponent

        // ==================== 血条 ====================

        CreateHPBar(unit);
        return unit;
    }

    void CreateHPBar(UnitData unit)
    {
        // === 双重保险：优先用面板绑定，失败则自动加载 ===

        // 第一重：面板拖拽绑定
        GameObject prefab = hpBarPrefab;

        // 第二重：Resources 动态加载（面板为空时自动救命）
        if (prefab == null)
        {
            // 尝试多个可能路径
            string[] searchPaths = {
                "Prefabs/HPBar",
                "Prefabs/UI/HPBar",
                "HPBar"
            };
            foreach (string path in searchPaths)
            {
                prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    Debug.Log($"[HPBar] 通过 Resources.Load(\"{path}\") 自动加载了 HPBar 预制体");
                    break;
                }
            }
        }

        if (prefab == null)
        {
            Debug.LogError("[HPBar] hpBarPrefab 未绑定，且 Resources.Load 也未找到！" +
                           $"请将 HPBar 预制体放到 Assets/Resources/ 目录下，或在 Inspector 中拖拽绑定。" +
                           $"当前预制体实际位置：Assets/Prefabs/UI/HPBar.prefab（可将其移到 Resources 文件夹下）");
            return;
        }

        // Canvas：优先用面板绑定，失败则场景中自动查找
        Canvas canvas = battleCanvas;
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
                Debug.Log("[HPBar] 通过 FindObjectOfType 自动找到了场景中的 Canvas");
        }

        if (canvas == null)
        {
            Debug.LogError("[HPBar] battleCanvas 未绑定，且场景中找不到 Canvas！");
            return;
        }

        GameObject bar = Instantiate(prefab, canvas.transform);
        bar.name = $"HPBar_{unit.unitName}";

        HPBarFollower follower = bar.GetComponent<HPBarFollower>();
        if (follower == null)
        {
            Debug.LogError($"[HPBar] HPBar 预制体上找不到 HPBarFollower 脚本！请检查预制体");
            return;
        }

        follower.SetTarget(unit.transform);
        follower.UpdateHP(unit.currentHP, unit.MaxHp);

        unit.hpBarFollower = follower;
    }


}
