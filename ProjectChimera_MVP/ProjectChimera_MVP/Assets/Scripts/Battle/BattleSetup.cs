using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    // ==================== DD1 映射表数据结构 ====================
    [System.Serializable]
    public class DD1CharacterMap
    {
        public DD1CharacterEntry[] entries;
    }

    [System.Serializable]
    public class DD1CharacterEntry
    {
        public string unitName;
        public string category;
        public string defaultAnimation;
        public string idlePath;
        public string combatPath;
        public string attackPath;
        public string damagedPath;
        public bool usedSpriteFallback;
        // 精灵回退专用路径（Spine 不兼容时使用）
        public string spriteIdlePath;
        public string spriteCombatPath;
        public string spriteAttackPath;
        public DD1AnimMapping[] animations;
    }

    [System.Serializable]
    public class DD1AnimMapping
    {
        public string state;
        public string assetPath;
    }

    static DD1CharacterMap _dd1MapCache;

    /// <summary>
    /// 从 Resources 加载 dd1_character_map.json 映射表
    /// </summary>
    static DD1CharacterMap LoadDD1Map()
    {
        if (_dd1MapCache != null) return _dd1MapCache;

        TextAsset jsonAsset = Resources.Load<TextAsset>("dd1_character_map");
        if (jsonAsset == null)
        {
            Log.Info("[BattleSetup] 未找到 Resources/dd1_character_map.json");
            return null;
        }

        try
        {
            _dd1MapCache = JsonUtility.FromJson<DD1CharacterMap>(jsonAsset.text);
        }
        catch (System.Exception ex)
        {
            Log.Warn($"[BattleSetup] 解析 dd1_character_map.json 失败: {ex.Message}");
        }

        return _dd1MapCache;
    }

    /// <summary>
    /// 根据单位名和动画状态从 DD1 映射表加载 SkeletonDataAsset
    /// </summary>
    Spine.Unity.SkeletonDataAsset TryLoadDD1SkeletonData(string unitName, string animState = "idle")
    {
        var map = LoadDD1Map();
        if (map == null || map.entries == null) return null;

        DD1CharacterEntry entry = null;
        foreach (var e in map.entries)
        {
            if (e.unitName == unitName)
            {
                entry = e;
                break;
            }
        }

        if (entry == null) return null;

        // 根据动画状态选择路径，回退策略：idle -> combat -> attack -> damaged -> 任意可用
        string path = null;
        switch (animState.ToLowerInvariant())
        {
            case "idle": path = entry.idlePath; break;
            case "combat": path = entry.combatPath; break;
            case "attack": path = entry.attackPath; break;
            case "damaged": path = entry.damagedPath; break;
        }

        if (string.IsNullOrEmpty(path)) path = entry.idlePath;
        if (string.IsNullOrEmpty(path)) path = entry.combatPath;
        if (string.IsNullOrEmpty(path)) path = entry.attackPath;
        if (string.IsNullOrEmpty(path)) path = entry.damagedPath;
        if (string.IsNullOrEmpty(path))
        {
            // 尝试从 animations 数组中找一个可用路径
            if (entry.animations != null && entry.animations.Length > 0)
                path = entry.animations[0].assetPath;
        }

        if (string.IsNullOrEmpty(path)) return null;

#if UNITY_EDITOR
        var asset = AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>(path);
        if (asset != null)
            Log.Info($"[BattleSetup] 从 DD1 映射表加载骨骼: {unitName} [{animState}] -> {asset.name}");
        return asset;
#else
        // 构建版本：若资源已移至 Resources 目录下可尝试加载
        string resPath = path.Replace("Assets/Resources/", "").Replace(".asset", "");
        return Resources.Load<Spine.Unity.SkeletonDataAsset>(resPath);
#endif
    }

    /// <summary>
    /// 动画状态映射：尝试播放目标动画，不存在则智能回退到 idle
    /// </summary>
    static void PlaySpineAnimation(Spine.Unity.SkeletonAnimation skeleton, string desiredState)
    {
        if (skeleton == null || skeleton.skeletonDataAsset == null) return;

        var data = skeleton.skeletonDataAsset.GetSkeletonData(false);
        if (data == null) return;

        string[] candidates = GetAnimationCandidates(desiredState);
        foreach (var candidate in candidates)
        {
            var anim = data.FindAnimation(candidate);
            if (anim != null)
            {
                skeleton.state.SetAnimation(0, anim, true);
                return;
            }
        }

        // 最终回退到 idle
        var idleAnim = data.FindAnimation("idle");
        if (idleAnim != null)
            skeleton.state.SetAnimation(0, idleAnim, true);
    }

    /// <summary>
    /// 当 Spine 骨骼不可用时，尝试从 DD1 映射表加载精灵贴图作为中间层回退
    /// 精灵来自 DD1 图集 PNG（在导入时已复制到 SpineCharacters/DD1/ 下）
    /// </summary>
    /// <returns>true 表示成功加载了 DD1 精灵</returns>
    bool TryAttachDD1Sprite(GameObject go, string unitName)
    {
        if (go == null || string.IsNullOrEmpty(unitName)) return false;

        var map = LoadDD1Map();
        if (map == null || map.entries == null) return false;

        DD1CharacterEntry entry = null;
        foreach (var e in map.entries)
        {
            if (e.unitName == unitName)
            {
                entry = e;
                break;
            }
        }

        if (entry == null) return false;

        // 优先使用 idle 精灵，否则尝试 combat，最后尝试 attack
        string spritePath = null;
        if (!string.IsNullOrEmpty(entry.spriteIdlePath))
            spritePath = entry.spriteIdlePath;
        else if (!string.IsNullOrEmpty(entry.spriteCombatPath))
            spritePath = entry.spriteCombatPath;
        else if (!string.IsNullOrEmpty(entry.spriteAttackPath))
            spritePath = entry.spriteAttackPath;

        if (string.IsNullOrEmpty(spritePath)) return false;

        // 加载精灵
        Sprite dd1Sprite = null;
#if UNITY_EDITOR
        dd1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
#else
        // 构建版本：尝试从 Resources 加载
        string resPath = spritePath.Replace("Assets/Resources/", "").Replace(".png", "");
        dd1Sprite = Resources.Load<Sprite>(resPath);
        // 如果不在 Resources 下，尝试从 AssetBundle 或直接加载
        if (dd1Sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(resPath);
            if (tex != null)
                dd1Sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f));
        }
#endif

        if (dd1Sprite == null)
        {
            Log.Warn($"[BattleSetup] 无法加载 DD1 精灵: {spritePath}");
            return false;
        }

        // 隐藏 Spine 组件
        var spine = go.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (spine != null) spine.enabled = false;

        // 创建或找到精灵承载 GameObject
        Transform spriteT = go.transform.Find("DD1Sprite");
        GameObject spriteGo;
        if (spriteT == null)
        {
            spriteGo = new GameObject("DD1Sprite");
            spriteGo.transform.SetParent(go.transform, false);
        }
        else
        {
            spriteGo = spriteT.gameObject;
        }
        spriteGo.layer = go.layer;

        // 添加 SpriteRenderer
        var sr = spriteGo.GetComponent<SpriteRenderer>();
        if (sr == null) sr = spriteGo.AddComponent<SpriteRenderer>();

        sr.sprite = dd1Sprite;

        // 缩放到合适高度（DD1 角色图集通常高约 500-600px，目标高度约 2m）
        float targetHeight = 2f;
        float spriteHeight = dd1Sprite.bounds.size.y;
        if (spriteHeight > 0.01f)
        {
            float scale = targetHeight / spriteHeight;
            spriteGo.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            spriteGo.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        }

        // 阵营调色
        UnitData ud = go.GetComponent<UnitData>();
        if (ud != null)
        {
            sr.color = ud.isPlayer ? Color.white : new Color(1f, 0.7f, 0.7f, 1f);
        }

        // 调整位置（抬高避免与血条重叠）
        spriteGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        sr.sortingOrder = 10;

        // 面对面朝向
        if (ud != null)
        {
            float dir = ud.isPlayer ? -1f : 1f;
            var currentScale = spriteGo.transform.localScale;
            spriteGo.transform.localScale = new Vector3(dir * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }

        Log.Info($"[BattleSetup] {unitName} 使用 DD1 精灵: {Path.GetFileName(spritePath)}");
        return true;
    }

    /// <summary>
    /// 当 Spine 资源不可用时，把单位附上一个肖像立绘 Sprite（用于替代骨骼动画）
    /// 会尝试 Resources/UI/Portraits/dd1_<unitName>_portrait.png，否则回退到彩色方块
    /// </summary>
    void TryAttachPortraitSprite(GameObject go, string unitName)
    {
        if (go == null || string.IsNullOrEmpty(unitName)) return;

        // 先隐藏 Spine（避免空骨骼渲染空白）
        var spine = go.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (spine != null) spine.enabled = false;

        // 1) 尝试加载肖像
        Sprite portrait = null;
        try
        {
            portrait = Resources.Load<Sprite>($"UI/Portraits/dd1_{unitName}_portrait");
        }
        catch { /* 加载失败忽略 */ }

        // 2) 创建/找到承载立绘的子 GameObject
        Transform portraitT = go.transform.Find("PortraitSprite");
        GameObject portraitGo;
        if (portraitT == null)
        {
            portraitGo = new GameObject("PortraitSprite");
            portraitGo.transform.SetParent(go.transform, false);
        }
        else
        {
            portraitGo = portraitT.gameObject;
        }
        portraitGo.layer = go.layer;

        // 3) 添加 SpriteRenderer
        var sr = portraitGo.GetComponent<SpriteRenderer>();
        if (sr == null) sr = portraitGo.AddComponent<SpriteRenderer>();

        if (portrait != null)
        {
            sr.sprite = portrait;
            // 缩放到合适高度（参考角色平均高度 2m）
            float targetHeight = 2f;
            float spriteHeight = portrait.bounds.size.y;
            if (spriteHeight > 0.01f)
            {
                float scale = targetHeight / spriteHeight;
                portraitGo.transform.localScale = new Vector3(scale, scale, 1f);
            }
            // 阵营调色
            UnitData ud = go.GetComponent<UnitData>();
            if (ud != null)
            {
                if (ud.isPlayer)
                    sr.color = Color.white;
                else
                    sr.color = new Color(1f, 0.7f, 0.7f, 1f); // 敌人偏红
            }
            // 调整到角色前方 + 抬高一些（避免与血条重叠）
            portraitGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            // SortingLayer 设为与角色一致
            sr.sortingOrder = 10;
        }
        else
        {
            // 没找到肖像，用颜色方块代替
            sr.sprite = CreateColoredSquareSprite();
            sr.color = UnitFallbackColor(unitName);
            portraitGo.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
            sr.sortingOrder = 10;
        }
    }

    /// <summary>
    /// 创建程序生成的纯色方块 Sprite（无肖像时的最终回退）
    /// </summary>
    static Sprite _coloredSquareCache;
    static Sprite CreateColoredSquareSprite()
    {
        if (_coloredSquareCache != null) return _coloredSquareCache;
        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var px = new Color[64];
        for (int i = 0; i < 64; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _coloredSquareCache = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0f));
        return _coloredSquareCache;
    }

    /// <summary>
    /// 根据单位名返回一个稳定的颜色（哈希算法 + 友好色）
    /// </summary>
    static Color UnitFallbackColor(string unitName)
    {
        if (string.IsNullOrEmpty(unitName)) return new Color(0.5f, 0.5f, 0.6f);
        int hash = 0;
        foreach (var c in unitName) hash = (hash * 31 + c) & 0x7FFFFFFF;
        // HSV 转 RGB（饱和度 0.5，明度 0.85）
        float h = (hash % 360) / 360f;
        return Color.HSVToRGB(h, 0.5f, 0.85f);
    }

    static string[] GetAnimationCandidates(string state)
    {
        switch (state.ToLowerInvariant())
        {
            case "idle":
                return new[] { "idle", "Idle", "IDLE", "combat_idle", "stand", "default" };
            case "combat":
                return new[] { "combat", "Combat", "COMBAT", "battle", "ready", "idle", "Idle" };
            case "attack":
                return new[] { "attack", "Attack", "ATTACK", "strike", "hit", "slash", "idle", "Idle" };
            case "damaged":
                return new[] { "damaged", "combat", "Combat", "COMBAT", "hurt", "idle", "Idle" };
            default:
                return new[] { state, "idle", "Idle" };
        }
    }

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

        // 自动创建战斗背景控制器
        if (FindObjectOfType<BattleBackgroundController>() == null)
        {
            var bgGo = new GameObject("BattleBackgroundController");
            bgGo.AddComponent<BattleBackgroundController>();
        }

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
            Debug.Log($"[BattleSetup] 创建敌人: {enemyTeamData[i].unitName}, HP={enemyTeamData[i].currentHP}, maxHp={enemyTeamData[i].maxHp}");
            UnitData unit = CreateUnit(enemyTeamData[i], pos);
            if (unit != null)
            {
                unit.rank = enemyTeamData[i].rank;
                enemyUnits.Add(unit);
                Debug.Log($"[BattleSetup] 敌人 {unit.unitName} 创建成功, currentHP={unit.currentHP}, MaxHp={unit.MaxHp}");
            }
            else
                Log.Error($"[BattleSetup] 敌人 {enemyTeamData[i].unitName} 生成失败");
        }

        Log.Info($"[BattleSetup] 创建完成 - 玩家:{playerUnits.Count}人, 敌人:{enemyUnits.Count}人");
        foreach (var u in enemyUnits)
            Debug.Log($"[BattleSetup] 敌人编队: {u.unitName} HP={u.currentHP}/{u.MaxHp}");

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
        bm.ResetBattleState();  // 确保无残留 battleOver 状态
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
                    // 尝试从 DD1 映射表动态加载 Spine SkeletonDataAsset
                    var dd1Asset = TryLoadDD1SkeletonData(data.unitName, "idle");
                    if (dd1Asset != null)
                    {
                        skeleton.skeletonDataAsset = dd1Asset;
                        bool initOk = false;
                        try
                        {
                            skeleton.Initialize(true);
                            Log.Info($"[Spine] {data.unitName} 从 DD1 映射表加载骨骼成功");

                            // 动画状态映射：默认播放 idle，回退机制在 PlaySpineAnimation 中处理
                            PlaySpineAnimation(skeleton, "idle");
                            initOk = true;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Warn($"[Spine] {data.unitName} DD1 Initialize 失败: {ex.Message}，" +
                                     "尝试 DD1 精灵回退");
                        }

                        if (initOk)
                        {
                            skeletonAssigned = true;
                        }
                        else
                        {
                            // Spine Initialize 失败，尝试 DD1 精灵回退
                            if (!TryAttachDD1Sprite(go, data.unitName))
                            {
                                // DD1 精灵也不可用，回退到肖像立绘
                                TryAttachPortraitSprite(go, data.unitName);
                            }
                        }
                    }
                    else
                    {
                        // DD1 SkeletonDataAsset 不存在，尝试 DD1 精灵回退
                        if (!TryAttachDD1Sprite(go, data.unitName))
                        {
                            // DD1 精灵也不可用，回退到肖像立绘
                            Log.Warn($"[Spine] {data.unitName} 无可用 DD1 资源，使用肖像立绘");
                            TryAttachPortraitSprite(go, data.unitName);
                        }
                    }
                }

                // 阵营调色（Spine 3.6 API）
                try
                {
                    if (data.isPlayer)
                    {
                        skeleton.skeleton.R = 1f; skeleton.skeleton.G = 1f;
                        skeleton.skeleton.B = 1f; skeleton.skeleton.A = 1f;
                    }
                    else
                    {
                        skeleton.skeleton.R = 1f; skeleton.skeleton.G = 0.6f;
                        skeleton.skeleton.B = 0.6f; skeleton.skeleton.A = 1f;
                    }
                }
                catch { /* 调色失败不影响运行 */ }

                // 体型缩放 + 面对面镜像翻转（合并处理，避免覆盖）
                try
                {
                    float dir = data.isPlayer ? -1f : 1f;
                    go.transform.localScale = new Vector3(dir * 0.75f, 0.75f, 0.75f);
                }
                catch { }
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
        unit.gameObject.AddComponent<StatusIconManager>();
        return unit;
    }

    // ==================== DD1 美术加载辅助 ====================

    Sprite SafeLoadSprite(string path)
    {
        var sp = Resources.Load<Sprite>(path);
        if (sp == null)
            Log.Warn($"[DD1Art] 加载失败: {path}，将使用灰色占位");
        return sp;
    }

    Image CreateUIImage(GameObject parent, string name, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = sprite != null ? color : Color.gray;
        img.raycastTarget = false;
        return img;
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

        // Canvas：优先用面板绑定，失败则场景中自动查找，再失败则自动创建
        Canvas canvas = battleCanvas;
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
                Log.Info("[HPBar] 通过 FindObjectOfType 自动找到了场景中的 Canvas");
        }
        if (canvas == null)
        {
            GameObject cgo = new GameObject("BattleCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            Log.Info("[HPBar] 自动创建了 Canvas");
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

        // === DD1 美术：背景底板 + 圆形肖像 ===
        RectTransform barRt = bar.GetComponent<RectTransform>();
        if (barRt == null) barRt = bar.AddComponent<RectTransform>();

        // 1. 背景底板（最底层）
        string panelName = unit.isPlayer ? "dd1_panel_hero" : "dd1_panel_monster";
        Sprite panelSprite = SafeLoadSprite($"UI/Panels/{panelName}");
        Image bgImg = CreateUIImage(bar, "DD1_Panel_BG", panelSprite, Color.white);
        RectTransform bgRt = bgImg.GetComponent<RectTransform>();
        bgRt.SetAsFirstSibling();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // 2. 血条左侧圆形肖像（仅玩家）
        if (unit.isPlayer)
        {
            // 先找匹配的职业肖像（按 unitName 模糊匹配）
            Sprite portraitSprite = FindHeroPortrait(unit.unitName);
            if (portraitSprite != null)
            {
                GameObject maskGo = new GameObject("PortraitMask", typeof(RectTransform));
                maskGo.layer = 5;
                maskGo.transform.SetParent(bar.transform, false);
                RectTransform maskRt = maskGo.GetComponent<RectTransform>();
                maskRt.anchorMin = new Vector2(0, 0.5f);
                maskRt.anchorMax = new Vector2(0, 0.5f);
                maskRt.pivot = new Vector2(0.5f, 0.5f);
                maskRt.sizeDelta = new Vector2(32, 32);
                maskRt.anchoredPosition = new Vector2(-18, 0);

                UnityEngine.UI.Mask mask = maskGo.AddComponent<UnityEngine.UI.Mask>();
                mask.showMaskGraphic = false;

                Image maskImg = maskGo.AddComponent<Image>();
                try
                {
                    maskImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Circle.png");
                }
                catch { /* 内置圆形不存在时静默 fallback */ }
                if (maskImg.sprite == null)
                {
                    // 若找不到内置圆形，fallback 为普通矩形遮罩
                    maskImg.color = Color.white;
                }

                Image portraitImg = CreateUIImage(maskGo, "Portrait", portraitSprite, Color.white);
                RectTransform prRt = portraitImg.GetComponent<RectTransform>();
                prRt.anchorMin = Vector2.zero;
                prRt.anchorMax = Vector2.one;
                prRt.offsetMin = Vector2.zero;
                prRt.offsetMax = Vector2.zero;
            }
        }
    }

    Sprite FindHeroPortrait(string unitName)
    {
        // 尝试直接匹配 dd1_<name>_portrait_roster
        string key = unitName.ToLower().Replace(" ", "_").Replace("\u6e38\u4fa0", "highwayman").Replace("\u6218\u58eb", "crusader").Replace("\u5b66\u8005", "antiquarian").Replace("\u72c2\u6218\u58eb", "hellion");
        Sprite sp = SafeLoadSprite($"UI/Portraits/dd1_{key}_portrait_roster");
        if (sp != null) return sp;

        // 模糊匹配：遍历所有已加载的 Portrait 资源
        var portraits = Resources.LoadAll<Sprite>("UI/Portraits");
        foreach (var p in portraits)
        {
            if (p.name.ToLower().Contains(key))
                return p;
        }
        return null;
    }

    void CreateStressBar(UnitData unit)
    {
        Canvas canvas = battleCanvas;
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            GameObject cgo = new GameObject("BattleCanvas", typeof(RectTransform));
            cgo.layer = 5;
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            Log.Info("[StressBar] 自动创建了 Canvas");
        }

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
            // 使用白色方块 Sprite 作为填充底图，配合 Filled 类型实现 DD1 风格渐进式压力条
            img.sprite = CreateColoredSquareSprite();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            img.raycastTarget = false;
            img.color = Color.green; // 初始绿色，StressBarFollower.Update() 会根据压力值动态调色
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

        // === DD1 美术：压力条背景底板 ===
        string panelName = unit.isPlayer ? "dd1_panel_hero" : "dd1_panel_monster";
        Sprite panelSprite = SafeLoadSprite($"UI/Panels/{panelName}");
        Image bgImg = CreateUIImage(bar, "DD1_Panel_BG", panelSprite, Color.white);
        RectTransform bgRt = bgImg.GetComponent<RectTransform>();
        bgRt.SetAsFirstSibling();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
    }
}
