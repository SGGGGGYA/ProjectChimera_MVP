using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;

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
    public GameObject stressBarPrefab; // 压力条预制体
    public Canvas battleCanvas;       // 战斗场景的 Canvas

    // 独立调试用的默认队伍数据
    // ==================== 默认装备工厂 ====================

    static Weapon MakeIronSword() => new Weapon
    {
        id = "iron_sword", equipmentName = "铁剑",
        mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 3, isPercent = false } }
    };
    static Armor MakePlateArmor() => new Armor
    {
        id = "plate_armor", equipmentName = "铠甲",
        mods = new List<StatMod> { new StatMod { stat = StatType.DEF, amount = 3, isPercent = false } }
    };
    static Weapon MakeShortBow() => new Weapon
    {
        id = "short_bow", equipmentName = "短弓",
        mods = new List<StatMod>
        {
            new StatMod { stat = StatType.AGI, amount = 2, isPercent = false },
            new StatMod { stat = StatType.STR, amount = 1, isPercent = false }
        }
    };
    static Armor MakeLeatherArmor() => new Armor
    {
        id = "leather_armor", equipmentName = "皮甲",
        mods = new List<StatMod> { new StatMod { stat = StatType.DEF, amount = 2, isPercent = false } }
    };
    static Weapon MakeRustySword() => new Weapon
    {
        id = "rusty_sword", equipmentName = "锈剑",
        mods = new List<StatMod> { new StatMod { stat = StatType.STR, amount = 1, isPercent = false } }
    };
    static Armor MakeClothArmor() => new Armor
    {
        id = "cloth_armor", equipmentName = "布甲",
        mods = new List<StatMod> { new StatMod { stat = StatType.DEF, amount = 1, isPercent = false } }
    };
    static Weapon MakeSpiritStaff() => new Weapon
    {
        id = "spirit_staff", equipmentName = "法杖",
        mods = new List<StatMod> { new StatMod { stat = StatType.INT, amount = 3, isPercent = false } }
    };
    static Weapon MakeSharpClaws() => new Weapon
    {
        id = "sharp_claws", equipmentName = "利爪",
        mods = new List<StatMod>
        {
            new StatMod { stat = StatType.STR, amount = 2, isPercent = false },
            new StatMod { stat = StatType.AGI, amount = 1, isPercent = false }
        }
    };
    static Armor MakeThickHide() => new Armor
    {
        id = "thick_hide", equipmentName = "厚皮",
        mods = new List<StatMod>
        {
            new StatMod { stat = StatType.DEF, amount = 2, isPercent = false },
            new StatMod { stat = StatType.MaxHP, amount = 10, isPercent = false }
        }
    };

    // ==================== 默认队伍 ====================

    private static List<UnitBattleData> GetDefaultPlayerTeam()
    {
        return new List<UnitBattleData>
        {
            new UnitBattleData
            {
                unitName = "战士", isPlayer = true, rank = 0,
                VIT = 8, STR = 10, DEF = 6, AGI = 5, INT = 3,
                weaponAttack = 5, level = 3,
                equippedWeapon = MakeIronSword(),
                equippedArmor = MakePlateArmor()
            },
            new UnitBattleData
            {
                unitName = "狂战士", isPlayer = true, rank = 1,
                VIT = 10, STR = 12, DEF = 4, AGI = 4, INT = 2,
                weaponAttack = 6, level = 3,
                equippedWeapon = MakeIronSword(),
                equippedArmor = MakeLeatherArmor()
            },
            new UnitBattleData
            {
                unitName = "游侠", isPlayer = true, rank = 2,
                VIT = 5, STR = 6, DEF = 4, AGI = 10, INT = 4,
                weaponAttack = 3, level = 3,
                equippedWeapon = MakeShortBow(),
                equippedArmor = MakeLeatherArmor()
            },
            new UnitBattleData
            {
                unitName = "学者", isPlayer = true, rank = 3,
                VIT = 4, STR = 3, DEF = 3, AGI = 7, INT = 12,
                weaponAttack = 2, level = 3,
                equippedWeapon = MakeSpiritStaff(),
                equippedArmor = MakeClothArmor()
            }
        };
    }

    static UnitBattleData MakeGoblinWarrior(int rank) => new UnitBattleData
    {
        unitName = "哥布林战士", isPlayer = false, rank = rank,
        VIT = 2, STR = 10, DEF = 4, AGI = 4, INT = 1,
        weaponAttack = 3, level = 2,
        equippedWeapon = MakeRustySword(), equippedArmor = MakeClothArmor()
    };
    static UnitBattleData MakeGoblinArcher(int rank) => new UnitBattleData
    {
        unitName = "哥布林弓手", isPlayer = false, rank = rank,
        VIT = 1, STR = 7, DEF = 2, AGI = 9, INT = 2,
        weaponAttack = 2, level = 2,
        equippedWeapon = MakeShortBow(), equippedArmor = MakeClothArmor()
    };
    static UnitBattleData MakeGoblinShaman(int rank) => new UnitBattleData
    {
        unitName = "哥布林萨满", isPlayer = false, rank = rank,
        VIT = 2, STR = 4, DEF = 2, AGI = 3, INT = 8,
        weaponAttack = 0, level = 2,
        equippedWeapon = MakeSpiritStaff(), equippedArmor = MakeClothArmor()
    };
    static UnitBattleData MakeWolf(int rank) => new UnitBattleData
    {
        unitName = "野狼", isPlayer = false, rank = rank,
        VIT = 4, STR = 9, DEF = 3, AGI = 12, INT = 1,
        weaponAttack = 2, level = 2,
        equippedWeapon = MakeSharpClaws(), equippedArmor = MakeThickHide()
    };

    static List<UnitBattleData> GetFullEnemyTeam()
    {
        int template = RandomProvider.Current.Range(0, 10);
        switch (template)
        {
            case 0: return new List<UnitBattleData> { MakeGoblinWarrior(0), MakeGoblinWarrior(1), MakeGoblinArcher(2), MakeGoblinArcher(3) };
            case 1: return new List<UnitBattleData> { MakeGoblinWarrior(0), MakeWolf(1), MakeGoblinArcher(2), MakeGoblinShaman(3) };
            case 2: return new List<UnitBattleData> { MakeWolf(0), MakeWolf(1), MakeGoblinArcher(2), MakeGoblinShaman(3) };
            case 3: return new List<UnitBattleData> { MakeGoblinWarrior(0), MakeGoblinWarrior(1), MakeGoblinWarrior(2), MakeGoblinShaman(3) };
            case 4: return new List<UnitBattleData> { MakeGoblinWarrior(0), MakeGoblinShaman(1), MakeGoblinArcher(2), MakeGoblinArcher(3) };
            case 5: return new List<UnitBattleData> { MakeGoblinWarrior(0), MakeGoblinWarrior(1), MakeWolf(2), MakeGoblinShaman(3) };
            case 6: return new List<UnitBattleData> { MakeGoblinArcher(0), MakeGoblinArcher(1), MakeGoblinArcher(2), MakeGoblinShaman(3) };
            case 7: return new List<UnitBattleData> { MakeWolf(0), MakeWolf(1), MakeGoblinWarrior(2), MakeGoblinWarrior(3) };
            case 8: return new List<UnitBattleData> { MakeGoblinWarrior(0), MakeGoblinWarrior(1), MakeGoblinWarrior(2), MakeGoblinArcher(3) };
            default: return new List<UnitBattleData> { MakeWolf(0), MakeGoblinArcher(1), MakeGoblinShaman(2), MakeGoblinArcher(3) };
        }
    }

    void Awake()
    {
        Log.Info("[BattleSetup] Awake 开始执行...");

        // 获取队伍数据（优先用 GameManager，没有则用默认调试数据）
        List<UnitBattleData> playerTeamData;
        List<UnitBattleData> enemyTeamData;

        // 玩家数据：优先用 GameManager，没有则用默认
        if (GameManager.Instance != null &&
            GameManager.Instance.playerTeamData != null &&
            GameManager.Instance.playerTeamData.Count > 0)
        {
            playerTeamData = GameManager.Instance.playerTeamData;
            Log.Info("[BattleSetup] 使用 GameManager 的玩家数据");
        }
        else
        {
            playerTeamData = GetDefaultPlayerTeam();
            Log.Info("[BattleSetup] 无 GameManager，使用默认玩家数据");
        }

        // 敌人数据：优先使用 GameManager 预设的模板，否则随机生成
        if (GameManager.Instance != null &&
            GameManager.Instance.enemyTeamData != null &&
            GameManager.Instance.enemyTeamData.Count > 0)
        {
            enemyTeamData = GameManager.Instance.enemyTeamData;
            GameManager.Instance.enemyTeamData = null;
            Log.Info($"[BattleSetup] 使用 GameManager 预设敌人数据 ({enemyTeamData.Count}人)");
        }
        else
        {
            enemyTeamData = GetFullEnemyTeam();
            Log.Info($"[BattleSetup] 随机遭遇 - {enemyTeamData[0].unitName} ({enemyTeamData.Count}人)");
        }

        // 创建所有玩家单位（前排在中间、后排两侧）
        List<UnitData> playerUnits = new List<UnitData>();
        for (int i = 0; i < playerTeamData.Count; i++)
        {
            Vector3 pos = Vector3.zero;
            if (i < playerSpawnPoints.Count && playerSpawnPoints[i] != null)
                pos = playerSpawnPoints[i].position;
            else
                pos = FormationPosition(true, playerTeamData[i].rank);
            UnitData unit = CreateUnit(playerTeamData[i], pos);
            if (unit != null)
            {
                unit.rank = playerTeamData[i].rank;
                playerUnits.Add(unit);
            }
            else
                Log.Error($"[BattleSetup] 玩家 {playerTeamData[i].unitName} 生成失败");
        }

        // 创建所有敌人单位（前排在中间、后排两侧）
        List<UnitData> enemyUnits = new List<UnitData>();
        for (int i = 0; i < enemyTeamData.Count; i++)
        {
            Vector3 pos = Vector3.zero;
            if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null)
                pos = enemySpawnPoints[i].position;
            else
                pos = FormationPosition(false, enemyTeamData[i].rank);
            UnitData unit = CreateUnit(enemyTeamData[i], pos);
            if (unit != null)
            {
                unit.rank = enemyTeamData[i].rank;
                enemyUnits.Add(unit);
            }
            else
                Log.Error($"[BattleSetup] 敌人 {enemyTeamData[i].unitName} 生成失败");
        }

        Log.Info($"[BattleSetup] 创建完成 - 玩家:{playerUnits.Count}人, 敌人:{enemyUnits.Count}人");

        // 设置 BattleManager（如果场景没放，自动创建一个）
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm == null)
        {
            GameObject bmGO = new GameObject("BattleManager");
            bm = bmGO.AddComponent<BattleManager>();
            Log.Info("[BattleSetup] 自动创建了 BattleManager");
        }
        bm.playerUnits = playerUnits;
        bm.enemyUnits = enemyUnits;
        Log.Info("[BattleSetup] BattleManager 编队设置完成");

        EnemyAIEngine.InitializeThreats(bm);

        // 初始化回合制（如果场景没放 TurnManager，自动创建一个）
        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm == null)
        {
            GameObject tmGO = new GameObject("TurnManager");
            tm = tmGO.AddComponent<TurnManager>();
            Log.Info("[BattleSetup] 自动创建了 TurnManager");
        }

        // 装配 BattleManager ↔ TurnManager 双向引用（替代 FindObjectOfType 和 Instance 单例调用）
        //  - bm.TurnState = tm：让 BattleManager 通过 ITurnStateProvider 读回合状态
        //  - tm.Context = bm：让 TurnManager 通过 IBattleContext 调 BattleManager UI/编队
        //  - BattleContextProvider.Set(bm)：让挂在预制体上、无法被直接注入的组件
        //    （如 UnitClickDetector）通过 Provider 拿到当前上下文
        // 这一步打破了原 TurnManager ↔ BattleManager 循环依赖（双方都通过接口看对方）
        bm.TurnState = tm;
        tm.Context = bm;
        ProjectChimera.Core.BattleContextProvider.Set(bm);
        Log.Info("[BattleSetup] TurnManager ↔ BattleManager 双向引用已注入");

        tm.InitializeBattle(playerUnits, enemyUnits);
        Log.Info("[BattleSetup] TurnManager 初始化完成");
    }

    /// <summary>
    /// 计算阵型坐标（暗黑地牢风格）
    /// 前排(rank 0-1)：靠中线近，后排(rank 2-3)：远离中线
    /// Y轴错落：rank奇偶交替
    /// </summary>
    static Vector3 FormationPosition(bool isPlayer, int rank)
    {
        float x;
        // 前排靠中线(0)，后排远离中线(±3)
        if (isPlayer)
            x = (rank <= 1) ? -1.2f - rank * 0.6f : -2.8f - (rank - 2) * 0.6f;
        else
            x = (rank <= 1) ? 1.2f + rank * 0.6f : 2.8f + (rank - 2) * 0.6f;

        float y = (rank % 2 == 0) ? 0.2f : -0.2f;
        return new Vector3(x, y, 0f);
    }


    UnitData CreateUnit(UnitBattleData data, Vector3 position)
    {
        // ==================== 防御性克隆 ====================

        // 检查万能预制体
        if (universalCharacterPrefab == null)
        {
            Log.Error($"[BattleSetup] universalCharacterPrefab 未绑定！无法生成 {data.unitName}");
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
            Log.Error($"[BattleSetup] 克隆预制体失败: {e.Message}");
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
                                Log.Info($"[Spine] {data.unitName} 加载骨骼: {entry.skeletonDataAsset.name}");
                            }
                            catch (System.Exception ex)
                            {
                                Log.Warn($"[Spine] {data.unitName} Initialize 失败: {ex.Message}");
                            }
                            skeletonAssigned = true;
                            break;
                        }
                    }
                }

                if (!skeletonAssigned)
                {
                    Log.Warn($"[Spine] {data.unitName} 未找到对应的 SkeletonDataAsset，使用默认外观");
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
            Log.Error($"[BattleSetup] {data.unitName} 的 GameObject 为 null，无法初始化 UnitData");
            return null;
        }

        UnitData unit = go.GetComponent<UnitData>();
        if (unit == null)
            unit = go.AddComponent<UnitData>();

        unit.unitName = data.unitName;
        unit.isPlayer = data.isPlayer;

        var classDef = GameManager.Instance?.GetClassDefinition(data.unitName);
        unit.classData = classDef;

        unit.VIT = data.VIT;
        unit.STR = data.STR;
        unit.baseDefense = data.DEF;
        unit.AGI = data.AGI;
        unit.INT = data.INT;
        unit.weaponAttack = data.weaponAttack;
        unit.level = data.level;
        unit.currentExp = data.currentExp;
        unit.currentHP = data.currentHP > 0 ? Mathf.Min(data.currentHP, unit.MaxHp) : unit.MaxHp;
        unit.stress = data.stress;
        unit.stressResistRate = Mathf.Min(StressManager.config.baseResist + data.level * StressManager.config.resistPerLevel, StressManager.config.maxResist);
        unit.skills = classDef != null ? new List<SkillData>(classDef.skillPool) : new List<SkillData>();

        unit.equippedWeapon = data.equippedWeapon;
        unit.equippedArmor = data.equippedArmor;
        unit.quirks = data.quirks ?? new List<Quirk>();

        // 同步命名系统持久化字段：isNamed / stressCapBonus / exclusiveSkill
        unit.isNamed = data.isNamed;
        unit.stressCapBonus = data.stressCapBonus;
        if (data.isNamed && !string.IsNullOrEmpty(data.exclusiveSkillId))
        {
            SkillData matched = unit.ApplyExclusiveSkillFromId(data.exclusiveSkillId);
            if (matched != null)
                Log.Info($"[命名] {data.unitName} 加载专属技能：{matched.skillName}");
            else
                Log.Warn($"[命名] {data.unitName} 找不到专属技能 '{data.exclusiveSkillId}'，将留空");
        }

        // 同步 V0.5 加点系统持久化字段：unassignedPoints
        unit.unassignedPoints = data.unassignedPoints;

        // 初始化 AI 状态
        unit.skillCooldowns = new Dictionary<string, int>();
        EnemyAIEngine.Reset(unit);

        // UnitClickDetector 已在预制体上，无需 AddComponent

        // 添加碰撞体用于点击检测
        var col = go.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 2f);
            col.offset = new Vector2(0f, 0.3f);
        }

        // ==================== 血条 + 压力条 + 状态图标 ====================

        CreateHPBar(unit);
        CreateStressBar(unit);
        unit.gameObject.AddComponent<StatusIconFollower>();
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
                    Log.Info($"[HPBar] 通过 Resources.Load(\"{path}\") 自动加载了 HPBar 预制体");
                    break;
                }
            }
        }

        if (prefab == null)
        {
            Log.Error("[HPBar] hpBarPrefab 未绑定，且 Resources.Load 也未找到！" +
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
                Log.Info("[HPBar] 通过 FindObjectOfType 自动找到了场景中的 Canvas");
        }

        if (canvas == null)
        {
            Log.Error("[HPBar] battleCanvas 未绑定，且场景中找不到 Canvas！");
            return;
        }

        GameObject bar = Instantiate(prefab, canvas.transform);
        bar.name = $"HPBar_{unit.unitName}";

        HPBarFollower follower = bar.GetComponent<HPBarFollower>();
        if (follower == null)
        {
            Log.Error($"[HPBar] HPBar 预制体上找不到 HPBarFollower 脚本！请检查预制体");
            return;
        }

        follower.SetTarget(unit.transform);
        follower.UpdateHP(unit.currentHP, unit.MaxHp);

        unit.hpBarFollower = follower;
    }

    void CreateStressBar(UnitData unit)
    {
        Canvas canvas = battleCanvas;
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null) return;

        GameObject bar;
        if (stressBarPrefab != null)
        {
            bar = Instantiate(stressBarPrefab, canvas.transform);
        }
        else
        {
            bar = new GameObject($"StressBar_{unit.unitName}", typeof(RectTransform));
            bar.transform.SetParent(canvas.transform, false);
            bar.layer = 5;
            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 12);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.layer = 5;
            fill.transform.SetParent(bar.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fill.AddComponent<CanvasRenderer>();
            Image img = fill.AddComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            img.raycastTarget = false;
            img.color = Color.green;
        }

        StressBarFollower follower = bar.GetComponent<StressBarFollower>();
        if (follower == null)
            follower = bar.AddComponent<StressBarFollower>();

        if (follower.fillImage == null)
            follower.fillImage = bar.GetComponentInChildren<Image>();

        if (follower.stressText == null)
        {
            GameObject label = new GameObject("Label", typeof(RectTransform));
            label.layer = 5;
            label.transform.SetParent(bar.transform, false);
            RectTransform lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(1, 0);
            lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(4, 0);
            lrt.sizeDelta = new Vector2(50, 14);
            TextMeshProUGUI tmp = UIFonts.CreateLabel(label, "TMP", "0/200", 12, Color.green);
            tmp.alignment = TextAlignmentOptions.Left;
            follower.stressText = tmp;
        }

        follower.SetTarget(unit.transform, unit);
        unit.stressBarFollower = follower;
    }
}
