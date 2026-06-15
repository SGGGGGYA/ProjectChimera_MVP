using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectChimera.Core;

/// <summary>
/// 六边形世界地图 — 文明6风格视觉效果（多层 TileLayerData 系统）
/// </summary>
public class HexWorldMap : MonoBehaviour
{
    [Header("地图设置")]
    public float hexSize = 2.0f;
    public int mapWidth = 40;    // 地图宽度（列数）
    public int mapHeight = 30;   // 地图高度（行数）

    [Header("基础地形颜色")]
    // 草地
    public Color grasslandLight = new Color(0.6f, 0.82f, 0.45f, 1f);
    public Color grasslandDark = new Color(0.35f, 0.55f, 0.25f, 1f);
    // 平原
    public Color plainsLight = new Color(0.72f, 0.78f, 0.42f, 1f);
    public Color plainsDark = new Color(0.5f, 0.55f, 0.28f, 1f);
    // 沙漠
    public Color desertLight = new Color(0.95f, 0.85f, 0.55f, 1f);
    public Color desertDark = new Color(0.75f, 0.65f, 0.4f, 1f);
    // 山脉
    public Color mountainLight = new Color(0.6f, 0.55f, 0.5f, 1f);
    public Color mountainDark = new Color(0.4f, 0.35f, 0.3f, 1f);
    // 冻土
    public Color tundraLight = new Color(0.85f, 0.9f, 0.95f, 1f);
    public Color tundraDark = new Color(0.65f, 0.7f, 0.8f, 1f);
    // 海洋
    public Color oceanLight = new Color(0.15f, 0.35f, 0.75f, 1f);
    public Color oceanDark = new Color(0.08f, 0.18f, 0.5f, 1f);
    // 海岸
    public Color coastLight = new Color(0.3f, 0.6f, 0.85f, 1f);
    public Color coastDark = new Color(0.18f, 0.4f, 0.7f, 1f);

    [Header("地貌颜色")]
    // 森林
    public Color forestLight = new Color(0.25f, 0.55f, 0.3f, 1f);
    public Color forestDark = new Color(0.12f, 0.35f, 0.15f, 1f);
    // 雨林
    public Color rainforestLight = new Color(0.15f, 0.6f, 0.25f, 1f);
    public Color rainforestDark = new Color(0.05f, 0.38f, 0.12f, 1f);
    // 沼泽
    public Color marshLight = new Color(0.4f, 0.55f, 0.35f, 1f);
    public Color marshDark = new Color(0.25f, 0.38f, 0.2f, 1f);
    // 绿洲
    public Color oasisLight = new Color(0.3f, 0.75f, 0.55f, 1f);
    public Color oasisDark = new Color(0.15f, 0.55f, 0.35f, 1f);
    // 泛滥平原
    public Color floodplainLight = new Color(0.55f, 0.7f, 0.4f, 1f);
    public Color floodplainDark = new Color(0.38f, 0.5f, 0.25f, 1f);
    // 暗礁
    public Color reefLight = new Color(0.2f, 0.5f, 0.6f, 1f);
    public Color reefDark = new Color(0.1f, 0.3f, 0.42f, 1f);
    // 冰川
    public Color iceLight = new Color(0.9f, 0.95f, 1f, 1f);
    public Color iceDark = new Color(0.75f, 0.82f, 0.9f, 1f);
    // 林地
    public Color woodsLight = new Color(0.35f, 0.6f, 0.32f, 1f);
    public Color woodsDark = new Color(0.2f, 0.4f, 0.18f, 1f);

    [Header("边框颜色")]
    public Color borderNormal = new Color(0.2f, 0.18f, 0.15f, 0.8f);
    public Color borderPlayer = new Color(0.2f, 0.7f, 1f, 0.9f);
    public Color borderEnemy1 = new Color(0.85f, 0.15f, 0.15f, 0.9f);
    public Color borderEnemy2 = new Color(0.65f, 0.1f, 0.6f, 0.9f);
    public Color borderNeutral = new Color(0.4f, 0.38f, 0.35f, 0.7f);

    [Header("地块类型颜色")]
    public Color battleOverlay = new Color(0.9f, 0.2f, 0.15f, 0.3f);
    public Color eliteOverlay = new Color(0.7f, 0.15f, 0.55f, 0.3f);
    public Color treasureOverlay = new Color(0.95f, 0.8f, 0.2f, 0.3f);
    public Color campOverlay = new Color(0.2f, 0.75f, 0.45f, 0.3f);
    public Color eventOverlay = new Color(0.2f, 0.6f, 0.85f, 0.3f);

    [Header("小队设置")]
    public float moveSpeed = 5f;
    public Color squadColor = new Color(0.1f, 0.95f, 1f, 1f);
    public Color squadGlow = new Color(0.3f, 1f, 1f, 0.4f);
    public float squadPulseSpeed = 1.5f;

    [Header("事件概率")]
    [Range(0, 1)] public float battleChance = 0.18f;
    [Range(0, 1)] public float eliteChance = 0.07f;
    [Range(0, 1)] public float treasureChance = 0.09f;
    [Range(0, 1)] public float campChance = 0.05f;
    [Range(0, 1)] public float eventChance = 0.06f;

    [Header("视觉效果")]
    public bool enableShadows = true;
    public bool enableGridLines = true;
    public float gridLineThickness = 0.02f;
    public bool enableTerrainVariation = true;
    public bool enableFog = true;
    public float fogDensity = 0.6f;

    // 内部状态
    Dictionary<HexCoord, HexTile> _tiles = new Dictionary<HexCoord, HexTile>();
    HexTile[,] _offsetTiles;  // 偏移坐标快速查找 [row, col]

    // 地图快照（跨场景持久化）
    static HexMapSnapshot _pendingSnapshot;
    static HexCoord? _pendingSquadCoord;
    HexCoord _squadCoord;
    GameObject _squadMarker;
    bool _isMoving;
    Coroutine _moveRoutine;
    bool _eventTriggered;
    GameObject _tileParent;
    GameObject _ghostParent;  // 幽灵地图容器
    FogOfWarController _fogController;
    float _mapWorldWidth;     // 环形地图：世界宽度

    // 精灵缓存
    Dictionary<TerrainType, Sprite> _terrainSprites = new Dictionary<TerrainType, Sprite>();
    Dictionary<FeatureType, Sprite> _featureSprites = new Dictionary<FeatureType, Sprite>();
    Dictionary<WorldTileType, Sprite> _overlaySprites = new Dictionary<WorldTileType, Sprite>();
    Dictionary<WorldTileType, Sprite> _iconSprites = new Dictionary<WorldTileType, Sprite>();
    Sprite _borderSprite;
    Sprite _squadSprite;

    // 常量
    const int TEXTURE_SIZE = 256;
    const float HEX_INNER_RATIO = 0.92f;
    const float BORDER_WIDTH = 0.08f;

    public static bool IsUIPanelOpen()
    {
        if (UIShopController.Instance != null && UIShopController.Instance.panel != null && UIShopController.Instance.panel.activeSelf) return true;
        if (UIInventoryController.Instance != null && UIInventoryController.Instance.panel != null && UIInventoryController.Instance.panel.activeSelf) return true;
        if (UIFormationController.Instance != null && UIFormationController.Instance.panel != null && UIFormationController.Instance.panel.activeSelf) return true;
        if (UIPauseMenu.Instance != null && UIPauseMenu.Instance.panel != null && UIPauseMenu.Instance.panel.activeSelf) return true;
        if (UIRecruitController.Instance != null && UIRecruitController.Instance.panel != null && UIRecruitController.Instance.panel.activeSelf) return true;
        if (UIDungeonMapController.Instance != null && UIDungeonMapController.Instance.panel != null && UIDungeonMapController.Instance.panel.activeSelf) return true;
        if (UICampController.Instance != null && UICampController.Instance.panel != null && UICampController.Instance.panel.activeSelf) return true;
        return false;
    }

    void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.WorldMap)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(AudioKeys.BGM_MAP);

        _tileParent = new GameObject("Tiles");
        _tileParent.transform.SetParent(transform);

        _ghostParent = new GameObject("GhostMaps");
        _ghostParent.transform.SetParent(transform);

        _fogController = gameObject.AddComponent<FogOfWarController>();

        _offsetTiles = new HexTile[mapHeight, mapWidth];

        GenerateAllSprites();
        GenerateMap();

        // 计算世界宽度（必须在 CreateGhostMaps 之前）
        _mapWorldWidth = mapWidth * hexSize * Mathf.Sqrt(3f);

        // 从快照恢复地图状态（战斗返回时）
        if (_pendingSnapshot != null)
        {
            RestoreFromSnapshot(_pendingSnapshot);
            _pendingSnapshot = null;
        }

        CreateGhostMaps();
        CreateSquad();

        // 恢复小队位置（如果有快照）
        if (_pendingSquadCoord.HasValue)
        {
            _squadCoord = _pendingSquadCoord.Value;
            _squadMarker.transform.position = _squadCoord.ToWorld(hexSize);
            _pendingSquadCoord = null;
        }

        if (enableFog)
            UpdateFogOfWar();

        // 设置摄像机边界（水平不限制，支持环绕）
        var cam = Camera.main;
        if (cam != null)
        {
            var controller = cam.GetComponent<HexCameraController>();
            if (controller == null)
                controller = cam.gameObject.AddComponent<HexCameraController>();

            float mapWorldHeight = mapHeight * hexSize * 1.5f;
            // X边界放宽到包含幽灵地图
            controller.SetBounds(-_mapWorldWidth, _mapWorldWidth * 2f, -2f, mapWorldHeight + 2f);

            // 将摄像机居中到地图中心
            cam.transform.position = new Vector3(_mapWorldWidth / 2f, mapWorldHeight / 2f, -10f);
        }
    }

    void Update()
    {
        // 小队呼吸动画
        if (_squadMarker != null)
        {
            float pulse = (Mathf.Sin(Time.time * squadPulseSpeed) + 1f) * 0.5f;
            var sr = _squadMarker.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = squadColor;
                c.a = 0.7f + pulse * 0.3f;
                sr.color = c;
            }
            float scale = 1f + pulse * 0.08f;
            _squadMarker.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void OnDestroy()
    {
        foreach (var kv in _tiles)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        if (_squadMarker != null) Destroy(_squadMarker);
        if (_tileParent != null) Destroy(_tileParent);
        if (_ghostParent != null) Destroy(_ghostParent);
    }

    #region 六边形工具（尖顶朝上 Pointy-top）

    /// <summary>尖顶朝上六边形内判断（circumradius = 中心到顶点距离 = 纹理高度/2）</summary>
    bool IsInsideHexagon(float dx, float dy, float circumradius)
    {
        float absDx = Mathf.Abs(dx);
        float absDy = Mathf.Abs(dy);
        float halfW = circumradius * Mathf.Sqrt(3f) / 2f;
        if (absDx > halfW) return false;
        float maxDy = circumradius - absDx / Mathf.Sqrt(3f);
        return absDy <= maxDy;
    }

    /// <summary>到六边形边缘的最短垂直距离（尖顶朝上）</summary>
    float DistanceToHexEdge(float dx, float dy, float circumradius)
    {
        float absDx = Mathf.Abs(dx);
        float absDy = Mathf.Abs(dy);
        float halfW = circumradius * Mathf.Sqrt(3f) / 2f;
        // 到垂直边（左右）的距离
        float sideDist = halfW - absDx;
        // 到斜边（上下对角线）的垂直距离
        float diagDist = (circumradius * Mathf.Sqrt(3f) - absDx - Mathf.Sqrt(3f) * absDy) * 0.5f;
        return Mathf.Min(sideDist, diagDist);
    }

    /// <summary>尖顶朝上六边形顶点（起始角 -90° 使第一个顶点朝正上方）</summary>
    Vector2[] GetHexVertices(float radius)
    {
        var vertices = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i - 90f);
            vertices[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }
        return vertices;
    }

    #endregion

    #region 精灵生成

    void GenerateAllSprites()
    {
        // 基础地形精灵（新 TerrainType）
        _terrainSprites[TerrainType.Grassland] = CreateTerrainSprite(TEXTURE_SIZE, grasslandLight, grasslandDark, true);
        _terrainSprites[TerrainType.Plains] = CreateTerrainSprite(TEXTURE_SIZE, plainsLight, plainsDark, true);
        _terrainSprites[TerrainType.Desert] = CreateTerrainSprite(TEXTURE_SIZE, desertLight, desertDark, true);
        _terrainSprites[TerrainType.Mountain] = CreateTerrainSprite(TEXTURE_SIZE, mountainLight, mountainDark, true, "mountain_icon");
        _terrainSprites[TerrainType.Tundra] = CreateTerrainSprite(TEXTURE_SIZE, tundraLight, tundraDark, true);
        _terrainSprites[TerrainType.Ocean] = CreateTerrainSprite(TEXTURE_SIZE, oceanLight, oceanDark, true);
        _terrainSprites[TerrainType.Coast] = CreateTerrainSprite(TEXTURE_SIZE, coastLight, coastDark, true);

        // 地貌精灵（FeatureType 叠加层，标准六边形尺寸）
        _featureSprites[FeatureType.Forest] = CreateFeatureSprite(TEXTURE_SIZE, forestLight, forestDark, 0.7f, "forest_icon");
        _featureSprites[FeatureType.Rainforest] = CreateFeatureSprite(TEXTURE_SIZE, rainforestLight, rainforestDark, 0.7f);
        _featureSprites[FeatureType.Marsh] = CreateFeatureSprite(TEXTURE_SIZE, marshLight, marshDark, 0.5f);
        _featureSprites[FeatureType.Oasis] = CreateFeatureSprite(TEXTURE_SIZE, oasisLight, oasisDark, 0.8f);
        _featureSprites[FeatureType.Floodplain] = CreateFeatureSprite(TEXTURE_SIZE, floodplainLight, floodplainDark, 0.5f);
        _featureSprites[FeatureType.Reef] = CreateFeatureSprite(TEXTURE_SIZE, reefLight, reefDark, 0.4f);
        _featureSprites[FeatureType.Ice] = CreateFeatureSprite(TEXTURE_SIZE, iceLight, iceDark, 0.8f);
        _featureSprites[FeatureType.Woods] = CreateFeatureSprite(TEXTURE_SIZE, woodsLight, woodsDark, 0.55f);

        // 叠加层精灵
        _overlaySprites[WorldTileType.Battle] = CreateOverlaySprite(128, battleOverlay);
        _overlaySprites[WorldTileType.Elite] = CreateOverlaySprite(128, eliteOverlay);
        _overlaySprites[WorldTileType.Treasure] = CreateOverlaySprite(128, treasureOverlay);
        _overlaySprites[WorldTileType.Camp] = CreateOverlaySprite(128, campOverlay);
        _overlaySprites[WorldTileType.Event] = CreateOverlaySprite(128, eventOverlay);

        // 图标精灵
        _iconSprites[WorldTileType.Battle] = CreateIconSprite(64, "sword");
        _iconSprites[WorldTileType.Elite] = CreateIconSprite(64, "skull");
        _iconSprites[WorldTileType.Treasure] = CreateIconSprite(64, "chest");
        _iconSprites[WorldTileType.Camp] = CreateIconSprite(64, "tent");
        _iconSprites[WorldTileType.Event] = CreateIconSprite(64, "star");

        // 边框精灵
        _borderSprite = CreateBorderSprite(TEXTURE_SIZE);

        // 小队精灵
        _squadSprite = CreateSquadSprite(128);
    }

    /// <summary>创建地形精灵 - 尖顶朝上（Pointy-top），带2.5D倒角光照</summary>
    Sprite CreateTerrainSprite(int size, Color lightColor, Color darkColor, bool addNoise, string iconType = "")
    {
        // 尖顶朝上：宽 < 高，比例 √3 : 2
        int width = Mathf.RoundToInt(size * Mathf.Sqrt(3f) / 2f);
        int height = size;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        float halfW = width * 0.5f;
        float circumradius = height * 0.5f;

        // === 光照参数：主光源来自左上角 ===
        const float lightX = -0.7071f; // -1/√2
        const float lightY =  0.7071f; //  1/√2  （纹理坐标系Y向上）
        float bevelRange = circumradius * 0.30f; // 倒角影响范围

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - halfW;
                float dy = y - circumradius;

                if (!IsInsideHexagon(dx, dy, circumradius))
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float distToEdge = DistanceToHexEdge(dx, dy, circumradius);

                // ── 第1层：径向渐变基础色 ──
                float distFromCenter = Mathf.Sqrt(dx * dx + dy * dy);
                float gradient = 1f - (distFromCenter / circumradius) * 0.3f;
                Color baseColor = Color.Lerp(darkColor, lightColor, gradient);

                // ── 第2层：柏林噪点（内部纹理细节）──
                if (addNoise)
                {
                    float noise1 = Mathf.PerlinNoise(x * 0.05f + 50f, y * 0.05f + 50f) * 0.08f;
                    float noise2 = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.04f;
                    float noise = noise1 + noise2;
                    baseColor.r = Mathf.Clamp01(baseColor.r + noise);
                    baseColor.g = Mathf.Clamp01(baseColor.g + noise);
                    baseColor.b = Mathf.Clamp01(baseColor.b + noise);
                }

                // ── 第3层：2.5D 倒角光照（高光/阴影）──
                // 原理：像素距边缘越近，其"表面法线"越趋向外；
                //       用(像素-中心)方向近似法线，与光源方向求点积。
                //       注意：纹理坐标系 Y 轴向下，光源左上 → lightY 取负。
                if (distToEdge < bevelRange && distFromCenter > 0.5f)
                {
                    // 倒角强度：边缘=1，向内二次衰减到0
                    float t = 1f - distToEdge / bevelRange;
                    float bevelStrength = t * t;

                    // 表面法线近似：从中心指向像素（单位化）
                    float nx = dx / distFromCenter;
                    float ny = -dy / distFromCenter; // 取反：纹理Y向下，但光照Y向上

                    // 点积：法线与光源方向
                    float NdotL = nx * lightX + ny * lightY; // [-1, 1]

                    // 映射到 [阴影, 高光]，强度系数0.35
                    float bevelEffect = NdotL * bevelStrength * 0.35f;

                    if (bevelEffect > 0f)
                    {
                        // 高光：向白色混合
                        baseColor.r = Mathf.Lerp(baseColor.r, 1f, bevelEffect);
                        baseColor.g = Mathf.Lerp(baseColor.g, 1f, bevelEffect);
                        baseColor.b = Mathf.Lerp(baseColor.b, 1f, bevelEffect);
                    }
                    else
                    {
                        // 阴影：向黑色混合
                        float shadow = -bevelEffect;
                        baseColor.r = Mathf.Lerp(baseColor.r, 0f, shadow);
                        baseColor.g = Mathf.Lerp(baseColor.g, 0f, shadow);
                        baseColor.b = Mathf.Lerp(baseColor.b, 0f, shadow);
                    }
                }

                // ── 第4层：边缘抗锯齿 + 柔和高光 ──
                if (distToEdge < circumradius * 0.15f)
                {
                    float edgeHighlight = distToEdge / (circumradius * 0.15f);
                    edgeHighlight = edgeHighlight * edgeHighlight;
                    baseColor = Color.Lerp(baseColor, baseColor * 1.15f, edgeHighlight);
                }

                // 内部图标（在六边形中心绘制简单符号）
                if (!string.IsNullOrEmpty(iconType) && distToEdge > circumradius * 0.15f)
                {
                    if (iconType == "mountain_icon")
                    {
                        // 白色三角形山峰图标
                        float iconSize = circumradius * 0.4f;
                        float iconDx = Mathf.Abs(dx);
                        float iconDy = dy + iconSize * 0.2f; // 略微下移
                        float peakWidth = iconSize * (1f - (iconDy + iconSize) / (iconSize * 2f));
                        if (iconDy > -iconSize && iconDy < iconSize * 0.8f && iconDx < peakWidth)
                        {
                            float blend = Mathf.Clamp01(1f - Mathf.Abs(iconDx / peakWidth));
                            Color iconColor = new Color(0.95f, 0.95f, 1f, 0.6f * blend);
                            baseColor = Color.Lerp(baseColor, iconColor, 0.5f * blend);
                        }
                    }
                }

                if (distToEdge < circumradius * 0.04f)
                {
                    float alpha = distToEdge / (circumradius * 0.04f);
                    baseColor.a = alpha;
                }

                tex.SetPixel(x, y, baseColor);
            }
        }

        tex.Apply();
        ApplyBleed(tex);
        float ppu = height / (2f * hexSize);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    /// <summary>创建地貌叠加精灵（尖顶朝上，半透明，带2.5D倒角）</summary>
    Sprite CreateFeatureSprite(int size, Color lightColor, Color darkColor, float opacity, string iconType = "")
    {
        int width = Mathf.RoundToInt(size * Mathf.Sqrt(3f) / 2f);
        int height = size;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        float halfW = width * 0.5f;
        float circumradius = height * 0.5f;

        const float lightX = -0.7071f;
        const float lightY =  0.7071f;
        float bevelRange = circumradius * 0.25f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - halfW;
                float dy = y - circumradius;

                if (!IsInsideHexagon(dx, dy, circumradius))
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float distFromCenter = Mathf.Sqrt(dx * dx + dy * dy);
                float gradient = 1f - (distFromCenter / circumradius) * 0.3f;
                Color baseColor = Color.Lerp(darkColor, lightColor, gradient);

                // 柏林噪点
                float noise1 = Mathf.PerlinNoise(x * 0.08f + 150f, y * 0.08f + 150f) * 0.06f;
                float noise2 = Mathf.PerlinNoise(x * 0.15f + 75f, y * 0.15f + 75f) * 0.03f;
                float noise = noise1 + noise2;
                baseColor.r = Mathf.Clamp01(baseColor.r + noise);
                baseColor.g = Mathf.Clamp01(baseColor.g + noise);
                baseColor.b = Mathf.Clamp01(baseColor.b + noise);

                // 2.5D 倒角光照
                float distToEdge = DistanceToHexEdge(dx, dy, circumradius);
                if (distToEdge < bevelRange && distFromCenter > 0.5f)
                {
                    float t = 1f - distToEdge / bevelRange;
                    float bevelStrength = t * t;
                    float nx = dx / distFromCenter;
                    float ny = -dy / distFromCenter;
                    float NdotL = nx * lightX + ny * lightY;
                    float bevelEffect = NdotL * bevelStrength * 0.30f;

                    if (bevelEffect > 0f)
                    {
                        baseColor.r = Mathf.Lerp(baseColor.r, 1f, bevelEffect);
                        baseColor.g = Mathf.Lerp(baseColor.g, 1f, bevelEffect);
                        baseColor.b = Mathf.Lerp(baseColor.b, 1f, bevelEffect);
                    }
                    else
                    {
                        float shadow = -bevelEffect;
                        baseColor.r = Mathf.Lerp(baseColor.r, 0f, shadow);
                        baseColor.g = Mathf.Lerp(baseColor.g, 0f, shadow);
                        baseColor.b = Mathf.Lerp(baseColor.b, 0f, shadow);
                    }
                }

                // 边缘抗锯齿
                float edgeAlpha = 1f;
                if (distToEdge < circumradius * 0.06f)
                    edgeAlpha = distToEdge / (circumradius * 0.06f);

                // 内部图标（在六边形中心绘制简单符号）
                if (!string.IsNullOrEmpty(iconType) && distToEdge > circumradius * 0.15f)
                {
                    if (iconType == "forest_icon")
                    {
                        // 简单绿色圆形树冠图标
                        float iconRadius = circumradius * 0.3f;
                        float dist = Mathf.Sqrt(dx * dx + (dy + iconRadius * 0.3f) * (dy + iconRadius * 0.3f));
                        if (dist < iconRadius)
                        {
                            float blend = 1f - dist / iconRadius;
                            Color treeColor = new Color(0.15f, 0.45f, 0.15f, 0.5f * blend);
                            baseColor = Color.Lerp(baseColor, treeColor, 0.4f * blend);
                        }
                    }
                }

                baseColor.a = opacity * edgeAlpha;

                tex.SetPixel(x, y, baseColor);
            }
        }

        tex.Apply();
        ApplyBleed(tex);
        float ppu = height / (2f * hexSize);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    /// <summary>创建叠加层精灵（尖顶朝上）</summary>
    Sprite CreateOverlaySprite(int size, Color overlayColor)
    {
        int width = Mathf.RoundToInt(size * Mathf.Sqrt(3f) / 2f);
        int height = size;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        float halfW = width * 0.5f;
        float circumradius = height * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - halfW;
                float dy = y - circumradius;

                if (!IsInsideHexagon(dx, dy, circumradius))
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float distToEdge = DistanceToHexEdge(dx, dy, circumradius);

                if (distToEdge < circumradius * BORDER_WIDTH * 2f)
                {
                    float edgeAlpha = distToEdge / (circumradius * BORDER_WIDTH * 2f);
                    Color edgeColor = overlayColor;
                    edgeColor.a = 0.7f * edgeAlpha;
                    tex.SetPixel(x, y, edgeColor);
                }
                else
                {
                    Color innerColor = overlayColor;
                    innerColor.a = 0.15f;
                    tex.SetPixel(x, y, innerColor);
                }
            }
        }

        tex.Apply();
        ApplyBleed(tex);
        float ppu = height / (2f * hexSize);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    /// <summary>创建边框精灵（尖顶朝上）</summary>
    Sprite CreateBorderSprite(int size)
    {
        int width = Mathf.RoundToInt(size * Mathf.Sqrt(3f) / 2f);
        int height = size;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        float halfW = width * 0.5f;
        float circumradius = height * 0.5f;
        float borderWidth = circumradius * BORDER_WIDTH;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - halfW;
                float dy = y - circumradius;

                if (!IsInsideHexagon(dx, dy, circumradius))
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float distToEdge = DistanceToHexEdge(dx, dy, circumradius);

                if (distToEdge < borderWidth)
                {
                    float alpha = distToEdge / borderWidth;
                    alpha = 1f - alpha * alpha;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.9f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        ApplyBleed(tex);
        float ppu = height / (2f * hexSize);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    /// <summary>创建小队精灵（尖顶朝上纹理内的圆形标记）</summary>
    Sprite CreateSquadSprite(int size)
    {
        int width = Mathf.RoundToInt(size * Mathf.Sqrt(3f) / 2f);
        int height = size;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float markerRadius = halfW * 0.65f;
        float outerRadius = halfW;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - halfW;
                float dy = y - halfH;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > outerRadius)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                if (dist > markerRadius)
                {
                    float glowAlpha = (dist - markerRadius) / (outerRadius - markerRadius);
                    glowAlpha = 1f - glowAlpha;
                    tex.SetPixel(x, y, new Color(squadGlow.r, squadGlow.g, squadGlow.b, glowAlpha * squadGlow.a));
                }
                else if (dist > markerRadius * 0.85f)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.9f));
                }
                else
                {
                    float innerGradient = dist / (markerRadius * 0.85f);
                    innerGradient = 1f - innerGradient * innerGradient;
                    Color innerColor = Color.Lerp(squadColor, squadColor * 1.3f, innerGradient);
                    tex.SetPixel(x, y, innerColor);
                }
            }
        }

        tex.Apply();
        float ppu = height / (2f * hexSize);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    /// <summary>创建图标精灵（简单符号，正方形）</summary>
    Sprite CreateIconSprite(int size, string iconType)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        float center = size * 0.5f;
        float iconRadius = size * 0.3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > size * 0.45f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool filled = false;
                Color iconColor = Color.white;

                switch (iconType)
                {
                    case "sword":
                        filled = (Mathf.Abs(dx) < 3f && Mathf.Abs(dy) < iconRadius) ||
                                 (Mathf.Abs(dx) < iconRadius * 0.5f && dy > -iconRadius * 0.3f && dy < iconRadius * 0.3f);
                        iconColor = new Color(1f, 0.95f, 0.8f, 0.9f);
                        break;

                    case "skull":
                        float skullDist = Mathf.Sqrt(dx * dx + (dy + 2f) * (dy + 2f));
                        filled = skullDist < iconRadius * 0.8f ||
                                 (Mathf.Abs(dx) < 4f && dy > -iconRadius && dy < -iconRadius * 0.3f);
                        iconColor = new Color(1f, 1f, 0.95f, 0.9f);
                        break;

                    case "chest":
                        filled = (Mathf.Abs(dx) < iconRadius * 0.9f && Mathf.Abs(dy) < iconRadius * 0.6f) ||
                                 (Mathf.Abs(dx) < iconRadius * 0.7f && dy > iconRadius * 0.3f && dy < iconRadius * 0.6f);
                        iconColor = new Color(1f, 0.85f, 0.3f, 0.9f);
                        break;

                    case "tent":
                        filled = dy > -iconRadius * 0.5f &&
                                 Mathf.Abs(dx) < (iconRadius - dy * 0.7f);
                        iconColor = new Color(0.9f, 0.9f, 0.85f, 0.9f);
                        break;

                    case "star":
                        float angle = Mathf.Atan2(dy, dx);
                        float starDist = dist / (iconRadius * 0.6f);
                        float starShape = Mathf.Cos(angle * 2.5f) * 0.3f + 0.7f;
                        filled = starDist < starShape;
                        iconColor = new Color(1f, 0.95f, 0.5f, 0.9f);
                        break;
                }

                if (filled)
                {
                    float edgeDist = Mathf.Abs(dist - iconRadius * 0.8f);
                    if (edgeDist < 2f)
                    {
                        float alpha = 1f - edgeDist / 2f;
                        iconColor.a *= alpha;
                    }
                    tex.SetPixel(x, y, iconColor);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / hexSize);
    }

    /// <summary>
    /// 边缘填充（Bleed）— 将透明像素外扩1-2个像素，用最近的内部颜色填充。
    /// 防止尖顶朝上六边形拼接时因浮点精度和抗锯齿产生接缝。
    /// </summary>
    void ApplyBleed(Texture2D tex)
    {
        int w = tex.width, h = tex.height;
        var pixels = tex.GetPixels();
        var result = (Color[])pixels.Clone();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                if (pixels[idx].a > 0.01f) continue; // 跳过非透明像素

                // 在半径2像素范围内寻找最近的非透明邻居
                Color sum = Color.clear;
                int count = 0;
                for (int dy = -2; dy <= 2; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= h) continue;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = x + dx;
                        if (nx < 0 || nx >= w) continue;
                        var c = pixels[ny * w + nx];
                        if (c.a > 0.01f)
                        {
                            sum += c;
                            count++;
                        }
                    }
                }
                if (count > 0)
                {
                    result[idx] = sum / count;
                    result[idx].a = 1f; // 填充为完全不透明
                }
            }
        }
        tex.SetPixels(result);
        tex.Apply();
    }

    #endregion

    #region 地图生成

    /// <summary>Odd-r offset 坐标转轴坐标（pointy-top）</summary>
    HexCoord OffsetToAxial(int col, int row)
    {
        int q = col - (row - (row & 1)) / 2;
        int r = row;
        return new HexCoord(q, r);
    }

    /// <summary>轴坐标转 Odd-r offset 坐标</summary>
    void AxialToOffset(HexCoord coord, out int col, out int row)
    {
        row = coord.r;
        col = coord.q + (row - (row & 1)) / 2;
    }

    /// <summary>环形列坐标包裹（水平无缝循环）</summary>
    int WrapCol(int col)
    {
        return ((col % mapWidth) + mapWidth) % mapWidth;
    }

    /// <summary>通过轴坐标获取地块（自动处理环形包裹）</summary>
    HexTile GetTileWrapped(HexCoord coord)
    {
        AxialToOffset(coord, out int col, out int row);
        if (row < 0 || row >= mapHeight) return null;
        col = WrapCol(col);
        return _offsetTiles[row, col];
    }

    /// <summary>保存当前地图状态到快照（进入战斗前调用）</summary>
    public void SaveState()
    {
        var snapshot = new HexMapSnapshot(mapWidth, mapHeight);
        snapshot.squadQ = _squadCoord.q;
        snapshot.squadR = _squadCoord.r;

        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                var tile = _offsetTiles[row, col];
                if (tile != null)
                    snapshot.tiles[snapshot.GetIndex(col, row)] = tile.layer;
            }
        }

        _pendingSnapshot = snapshot;
        _pendingSquadCoord = _squadCoord;
        Log.Info($"[地图] 状态已保存 ({mapWidth}x{mapHeight}, 小队={_squadCoord})");
    }

    /// <summary>从快照恢复地图状态</summary>
    void RestoreFromSnapshot(HexMapSnapshot snapshot)
    {
        if (snapshot.width != mapWidth || snapshot.height != mapHeight)
        {
            Log.Warn("[地图] 快照尺寸不匹配，跳过恢复");
            return;
        }

        int restored = 0;
        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                var savedLayer = snapshot.tiles[snapshot.GetIndex(col, row)];
                if (savedLayer == null) continue;

                var tile = _offsetTiles[row, col];
                if (tile == null) continue;

                // 恢复地块数据
                tile.layer = savedLayer;
                restored++;

                // 更新边框颜色
                UpdateBorderColor(tile);

                // 更新事件叠加层视觉
                UpdateTileEventVisuals(tile, savedLayer.eventType);
            }
        }

        Log.Info($"[地图] 从快照恢复 {restored} 个地块");
    }

    /// <summary>更新地块的事件叠加层精灵</summary>
    void UpdateTileEventVisuals(HexTile tile, WorldTileType eventType)
    {
        // 移除旧的事件叠加和图标子物体
        var oldOverlay = tile.transform.Find("Overlay");
        if (oldOverlay != null) Destroy(oldOverlay.gameObject);
        var oldIcon = tile.transform.Find("Icon");
        if (oldIcon != null) Destroy(oldIcon.gameObject);

        // 如果有事件，重新创建叠加层
        if (eventType != WorldTileType.Normal && _overlaySprites.ContainsKey(eventType))
        {
            int baseSortOrder = -Mathf.RoundToInt(tile.transform.position.y * 100);

            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(tile.transform, false);
            overlayGo.transform.localPosition = new Vector3(0, 0, -0.01f);
            var overlaySr = overlayGo.AddComponent<SpriteRenderer>();
            overlaySr.sprite = _overlaySprites[eventType];
            overlaySr.sortingOrder = baseSortOrder + 3;

            if (_iconSprites.ContainsKey(eventType))
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(tile.transform, false);
                iconGo.transform.localPosition = new Vector3(0, 0, -0.02f);
                iconGo.transform.localScale = Vector3.one * 0.5f;
                var iconSr = iconGo.AddComponent<SpriteRenderer>();
                iconSr.sprite = _iconSprites[eventType];
                iconSr.sortingOrder = baseSortOrder + 4;
            }

            // 注册到迷雾控制器
            if (enableFog && _fogController != null)
                _fogController.RegisterTile(tile.coord, tile);
        }
    }

    /// <summary>创建左右幽灵地图（视觉克隆，实现无缝环绕）</summary>
    void CreateGhostMaps()
    {
        float offsetX = _mapWorldWidth; // mapWidth * sqrt(3) * hexSize

        for (int side = -1; side <= 1; side += 2) // -1=左, +1=右
        {
            var ghostSide = new GameObject(side == -1 ? "GhostLeft" : "GhostRight");
            ghostSide.transform.SetParent(_ghostParent.transform);
            ghostSide.transform.localPosition = new Vector3(side * offsetX, 0, 0);

            // 克隆主地图的所有地块
            foreach (var kv in _tiles)
            {
                var mainTile = kv.Value;
                var ghostGo = new GameObject(mainTile.gameObject.name);
                ghostGo.transform.SetParent(ghostSide.transform);
                ghostGo.transform.position = mainTile.transform.position + new Vector3(side * offsetX, 0, 0);

                // 复制所有子物体的 SpriteRenderer
                foreach (var mainSr in mainTile.GetComponentsInChildren<SpriteRenderer>())
                {
                    var childGo = new GameObject(mainSr.gameObject.name);
                    childGo.transform.SetParent(ghostGo.transform);
                    childGo.transform.localPosition = mainSr.transform.localPosition;
                    childGo.transform.localScale = mainSr.transform.localScale;
                    var ghostSr = childGo.AddComponent<SpriteRenderer>();
                    ghostSr.sprite = mainSr.sprite;
                    ghostSr.color = mainSr.color;
                    ghostSr.sortingOrder = mainSr.sortingOrder;
                    ghostSr.sharedMaterial = mainSr.sharedMaterial;
                }
            }
        }
    }

    /// <summary>包裹小队世界坐标（水平环绕）</summary>
    Vector3 WrapWorldPosition(Vector3 worldPos)
    {
        if (_mapWorldWidth <= 0) return worldPos;
        float x = worldPos.x;
        x = ((x % _mapWorldWidth) + _mapWorldWidth) % _mapWorldWidth;
        return new Vector3(x, worldPos.y, worldPos.z);
    }

    void GenerateMap()
    {
        int tileIndex = 0;

        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                HexCoord coord = OffsetToAxial(col, row);

                // 生成 TileLayerData（地形 + 地貌 + 资源）
                TileLayerData layerData = GenerateTileLayerData(coord, col, row);

                // 生成地块事件类型
                WorldTileType eventType = WorldTileType.Normal;
                if (tileIndex > 0)
                {
                    float roll = RandomProvider.Current.Value;
                    float acc = 0f;
                    acc += battleChance;
                    if (roll < acc) { eventType = WorldTileType.Battle; }
                    else
                    {
                        acc += eliteChance;
                        if (roll < acc) { eventType = WorldTileType.Elite; }
                        else
                        {
                            acc += treasureChance;
                            if (roll < acc) { eventType = WorldTileType.Treasure; }
                            else
                            {
                                acc += campChance;
                                if (roll < acc) { eventType = WorldTileType.Camp; }
                                else
                                {
                                    acc += eventChance;
                                    if (roll < acc) { eventType = WorldTileType.Event; }
                                }
                            }
                        }
                    }
                }
                layerData.eventType = eventType;

                // 分配势力归属
                TileFaction faction = TileFaction.Neutral;
                int centerCol = mapWidth / 2;
                int centerRow = mapHeight / 2;
                int distToCenter = Mathf.Max(Mathf.Abs(col - centerCol), Mathf.Abs(row - centerRow));
                if (distToCenter <= 2)
                    faction = TileFaction.Player;
                else if (distToCenter >= Mathf.Min(mapWidth, mapHeight) / 2 - 2)
                    faction = RandomProvider.Current.Value < 0.5f ? TileFaction.Enemy1 : TileFaction.Enemy2;
                layerData.faction = faction;

                // 创建地块
                CreateHexTile(coord, layerData);
                _offsetTiles[row, col] = _tiles[coord];
                tileIndex++;
            }
        }
    }

    /// <summary>使用多层柏林噪声生成大陆地形的 TileLayerData</summary>
    TileLayerData GenerateTileLayerData(HexCoord coord, int col, int row)
    {
        var layer = new TileLayerData();

        // 归一化坐标
        float nx = (float)col / mapWidth;
        float ny = (float)row / mapHeight;

        // 海拔：多层叠加大陆噪声
        float elevation = 0f;
        elevation += Mathf.PerlinNoise(nx * 3f + 100f, ny * 3f + 100f) * 0.5f;
        elevation += Mathf.PerlinNoise(nx * 6f + 200f, ny * 6f + 200f) * 0.25f;
        elevation += Mathf.PerlinNoise(nx * 12f + 300f, ny * 12f + 300f) * 0.125f;
        elevation /= 0.875f; // 归一化

        // 湿度
        float moisture = Mathf.PerlinNoise(nx * 4f + 500f, ny * 4f + 500f);

        // 温度（基于纬度 + 噪声扰动）
        float latitude = Mathf.Abs(ny - 0.5f) * 2f; // 赤道=0，极地=1
        float tempNoise = Mathf.PerlinNoise(nx * 2f + 700f, ny * 2f + 700f) * 0.2f;
        float temperature = 1f - latitude + tempNoise;

        // === 第1层：根据海拔决定基础地形 ===
        if (elevation < 0.35f)
            layer.terrain = TerrainType.Ocean;
        else if (elevation < 0.42f)
            layer.terrain = TerrainType.Coast;
        else if (elevation > 0.82f)
            layer.terrain = TerrainType.Mountain;
        else if (latitude > 0.75f)
            layer.terrain = TerrainType.Tundra;
        else if (moisture < 0.35f && temperature > 0.5f)
            layer.terrain = TerrainType.Desert;
        else if (moisture > 0.55f && temperature > 0.4f)
            layer.terrain = TerrainType.Grassland;
        else
            layer.terrain = TerrainType.Plains;

        // === 第2层：根据地形 + 湿度 + 温度添加地貌 ===
        layer.feature = GenerateFeature(layer.terrain, moisture, temperature, elevation);

        // === 第3层：根据地形 + 地貌分配资源 ===
        layer.resource = GenerateResource(layer.terrain, layer.feature, coord);

        return layer;
    }

    /// <summary>根据地形和环境参数决定地貌</summary>
    FeatureType GenerateFeature(TerrainType terrain, float moisture, float temperature, float elevation)
    {
        float featureRoll = RandomProvider.Current.Value;

        switch (terrain)
        {
            case TerrainType.Ocean:
                // 海洋：小概率暗礁
                if (featureRoll < 0.1f) return FeatureType.Reef;
                return FeatureType.None;

            case TerrainType.Coast:
                // 海岸：小概率暗礁
                if (featureRoll < 0.08f) return FeatureType.Reef;
                return FeatureType.None;

            case TerrainType.Mountain:
                // 山脉：高海拔可能有冰川
                if (temperature < 0.3f && featureRoll < 0.3f) return FeatureType.Ice;
                return FeatureType.None;

            case TerrainType.Desert:
                // 沙漠：绿洲（稀有）或泛滥平原
                if (moisture > 0.5f && featureRoll < 0.08f) return FeatureType.Oasis;
                if (moisture > 0.4f && featureRoll < 0.15f) return FeatureType.Floodplain;
                return FeatureType.None;

            case TerrainType.Tundra:
                // 冻土：林地或冰川
                if (temperature < 0.2f && featureRoll < 0.25f) return FeatureType.Ice;
                if (moisture > 0.4f && featureRoll < 0.3f) return FeatureType.Woods;
                return FeatureType.None;

            case TerrainType.Grassland:
                // 草地：森林、雨林（温暖湿润）、沼泽
                if (moisture > 0.7f && temperature > 0.6f && featureRoll < 0.3f) return FeatureType.Rainforest;
                if (moisture > 0.55f && featureRoll < 0.35f) return FeatureType.Forest;
                if (moisture > 0.6f && elevation < 0.4f && featureRoll < 0.12f) return FeatureType.Marsh;
                return FeatureType.None;

            case TerrainType.Plains:
                // 平原：森林、林地
                if (moisture > 0.6f && featureRoll < 0.25f) return FeatureType.Forest;
                if (moisture > 0.45f && featureRoll < 0.15f) return FeatureType.Woods;
                return FeatureType.None;

            default:
                return FeatureType.None;
        }
    }

    /// <summary>根据地形和地貌分配资源</summary>
    ResourceType GenerateResource(TerrainType terrain, FeatureType feature, HexCoord coord)
    {
        float roll = RandomProvider.Current.Value;

        // 整体资源概率约30%
        if (roll > 0.3f) return ResourceType.None;

        float resourceRoll = RandomProvider.Current.Value;

        switch (terrain)
        {
            case TerrainType.Grassland:
                if (resourceRoll < 0.3f) return ResourceType.Wheat;
                if (resourceRoll < 0.5f) return ResourceType.Cattle;
                if (resourceRoll < 0.6f) return ResourceType.Rice;
                if (resourceRoll < 0.7f) return ResourceType.Horses;
                if (resourceRoll < 0.8f) return ResourceType.Iron;
                return ResourceType.Berries;

            case TerrainType.Plains:
                if (resourceRoll < 0.3f) return ResourceType.Wheat;
                if (resourceRoll < 0.5f) return ResourceType.Horses;
                if (resourceRoll < 0.65f) return ResourceType.Iron;
                if (resourceRoll < 0.75f) return ResourceType.Coal;
                return ResourceType.Cattle;

            case TerrainType.Desert:
                if (feature == FeatureType.Oasis)
                    return resourceRoll < 0.5f ? ResourceType.Dye : ResourceType.Spices;
                if (resourceRoll < 0.25f) return ResourceType.Silver;
                if (resourceRoll < 0.4f) return ResourceType.Gold_Ore;
                if (resourceRoll < 0.55f) return ResourceType.Oil;
                if (resourceRoll < 0.7f) return ResourceType.Niter;
                return ResourceType.Gems;

            case TerrainType.Tundra:
                if (resourceRoll < 0.3f) return ResourceType.Deer;
                if (resourceRoll < 0.5f) return ResourceType.Berries;
                if (resourceRoll < 0.65f) return ResourceType.Iron;
                if (resourceRoll < 0.8f) return ResourceType.Silver;
                return ResourceType.Coal;

            case TerrainType.Ocean:
                if (resourceRoll < 0.5f) return ResourceType.Fish;
                if (resourceRoll < 0.7f) return ResourceType.Oil;
                return ResourceType.Aluminum;

            case TerrainType.Coast:
                if (resourceRoll < 0.5f) return ResourceType.Fish;
                if (resourceRoll < 0.7f) return ResourceType.Sugar;
                return ResourceType.Ivory;

            case TerrainType.Mountain:
                if (resourceRoll < 0.4f) return ResourceType.Iron;
                if (resourceRoll < 0.6f) return ResourceType.Gems;
                if (resourceRoll < 0.8f) return ResourceType.Silver;
                return ResourceType.Gold_Ore;

            default:
                return ResourceType.None;
        }
    }

    /// <summary>创建六边形地块</summary>
    void CreateHexTile(HexCoord coord, TileLayerData layerData)
    {
        var go = new GameObject($"Hex_{coord.q}_{coord.r}");
        go.transform.SetParent(_tileParent.transform);

        Vector3 pos = coord.ToWorld(hexSize);
        go.transform.position = pos;

        // Y-Sorting: 较低的 Y 值（屏幕下方）渲染在上层（较高的 sortingOrder）
        int baseSortOrder = -Mathf.RoundToInt(pos.y * 100);

        // 基础地形层（始终使用标准六边形精灵）
        TerrainType terrain = layerData.terrain;
        Sprite terrainSprite = _terrainSprites.ContainsKey(terrain) ? _terrainSprites[terrain] : null;

        if (terrainSprite != null)
        {
            var terrainSr = go.AddComponent<SpriteRenderer>();
            terrainSr.sprite = terrainSprite;
            terrainSr.sortingOrder = baseSortOrder + 0;
        }

        // 地貌叠加层（始终使用标准六边形精灵）
        FeatureType feature = layerData.feature;
        if (feature != FeatureType.None && _featureSprites.ContainsKey(feature))
        {
            var featureGo = new GameObject("Feature");
            featureGo.transform.SetParent(go.transform, false);
            featureGo.transform.localPosition = new Vector3(0, 0, -0.005f);
            var featureSr = featureGo.AddComponent<SpriteRenderer>();
            featureSr.sprite = _featureSprites[feature];
            featureSr.sortingOrder = baseSortOrder + 1;
        }

        // 地块事件叠加层（非普通地块）
        WorldTileType eventType = layerData.eventType;
        if (eventType != WorldTileType.Normal && _overlaySprites.ContainsKey(eventType))
        {
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(go.transform, false);
            overlayGo.transform.localPosition = new Vector3(0, 0, -0.01f);
            var overlaySr = overlayGo.AddComponent<SpriteRenderer>();
            overlaySr.sprite = _overlaySprites[eventType];
            overlaySr.sortingOrder = baseSortOrder + 3;
        }

        // 图标层
        if (eventType != WorldTileType.Normal && _iconSprites.ContainsKey(eventType))
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.transform.localPosition = new Vector3(0, 0, -0.02f);
            iconGo.transform.localScale = Vector3.one * 0.5f;
            var iconSr = iconGo.AddComponent<SpriteRenderer>();
            iconSr.sprite = _iconSprites[eventType];
            iconSr.sortingOrder = baseSortOrder + 4;
        }

        // 边框层
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(go.transform, false);
        borderGo.transform.localPosition = new Vector3(0, 0, -0.03f);
        var borderSr = borderGo.AddComponent<SpriteRenderer>();
        borderSr.sprite = _borderSprite;
        borderSr.sortingOrder = baseSortOrder + 5;
        borderSr.color = borderNormal;

        // 阴影层（模拟光照方向）
        if (enableShadows && terrainSprite != null)
        {
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(go.transform, false);
            shadowGo.transform.localPosition = new Vector3(0.05f, -0.05f, -0.005f);
            var shadowSr = shadowGo.AddComponent<SpriteRenderer>();
            shadowSr.sprite = terrainSprite;
            shadowSr.sortingOrder = baseSortOrder + 2;
            shadowSr.color = new Color(0, 0, 0, 0.15f);
        }

        // 添加地块组件并设置数据
        var tile = go.AddComponent<HexTile>();
        tile.coord = coord;
        tile.layer = layerData;

        UpdateBorderColor(tile);

        _tiles[coord] = tile;

        if (enableFog && _fogController != null)
            _fogController.RegisterTile(coord, tile);
    }

    /// <summary>更新地块边框颜色</summary>
    void UpdateBorderColor(HexTile tile)
    {
        var borderGo = tile.transform.Find("Border");
        if (borderGo == null) return;

        var sr = borderGo.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        switch (tile.faction)
        {
            case TileFaction.Player:
                sr.color = borderPlayer;
                break;
            case TileFaction.Enemy1:
                sr.color = borderEnemy1;
                break;
            case TileFaction.Enemy2:
                sr.color = borderEnemy2;
                break;
            default:
                sr.color = borderNeutral;
                break;
        }
    }

    #endregion

    #region 迷雾系统

    void UpdateFogOfWar()
    {
        if (_fogController != null)
            _fogController.UpdateVisibility(_squadCoord, 2, _tiles);
    }

    #endregion

    #region 小队系统

    public void OnTileClicked(HexTile tile)
    {
        if (tile == null) return;
        MoveSquad(tile.coord);
    }

    void CreateSquad()
    {
        _squadMarker = new GameObject("SquadMarker");
        _squadMarker.transform.SetParent(transform);

        var sr = _squadMarker.AddComponent<SpriteRenderer>();
        sr.sprite = _squadSprite;
        sr.sortingOrder = 20;

        _squadCoord = OffsetToAxial(mapWidth / 2, mapHeight / 2);
        _squadMarker.transform.position = _squadCoord.ToWorld(hexSize);
    }

    public bool MoveSquad(HexCoord target)
    {
        if (_isMoving) return false;
        if (!GameManager.Instance.HasActionPoints()) return false;

        if (!_tiles.ContainsKey(target)) return false;

        var targetTile = _tiles[target];

        // 使用 TileLayerData 的 IsPassable 判断是否可通行
        if (!targetTile.layer.IsPassable) return false;

        HexCoord current = _squadCoord;
        List<HexCoord> path = HexCoord.GetPath(current, target, hexSize);

        if (path.Count > GameManager.Instance.currentActionPoints)
        {
            Log.Warn("[地图] 行动点不足");
            return false;
        }

        _moveRoutine = StartCoroutine(MoveSquadRoutine(path));
        return true;
    }

    IEnumerator MoveSquadRoutine(List<HexCoord> path)
    {
        _isMoving = true;

        foreach (var coord in path)
        {
            Vector3 startPos = _squadMarker.transform.position;
            Vector3 endPos = coord.ToWorld(hexSize);

            // 处理跨边界移动：选择最短路径（可能穿过幽灵地图区域）
            float directDist = Mathf.Abs(endPos.x - startPos.x);
            float wrapDist = _mapWorldWidth - directDist;
            if (wrapDist < directDist && _mapWorldWidth > 0)
            {
                // 环绕路径更短：先移到幽灵区域，再瞬移回来
                float wrapDir = endPos.x > startPos.x ? -1f : 1f;
                endPos.x += wrapDir * _mapWorldWidth;
            }

            float elapsed = 0;
            float duration = 1f / moveSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _squadMarker.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // 到达目标后，包裹坐标回主地图范围
            _squadMarker.transform.position = WrapWorldPosition(endPos);
            _squadCoord = coord;

            GameManager.Instance.ConsumeActionPoint();
            HandleTileEntry(coord);

            if (enableFog)
                UpdateFogOfWar();

            if (_eventTriggered)
            {
                _isMoving = false;
                yield break;
            }
        }

        _isMoving = false;
    }

    #endregion

    #region 事件处理

    void HandleTileEntry(HexCoord coord)
    {
        if (!_tiles.ContainsKey(coord)) return;

        var tile = _tiles[coord];
        _eventTriggered = false;

        // 自动占领中立地块
        if (tile.faction == TileFaction.Neutral)
        {
            tile.faction = TileFaction.Player;
            UpdateBorderColor(tile);
            Log.Info($"[4X] 占领地块 ({coord.q},{coord.r})");
        }

        // 收集资源
        CollectTileResources(tile);

        // 触发事件
        switch (tile.tileType)
        {
            case WorldTileType.Battle:
                Log.Info("[地图] 敌人出现！准备战斗...");
                _eventTriggered = true;
                SaveState();
                GameManager.Instance.StartBattle();
                break;
            case WorldTileType.Elite:
                Log.Info("[地图] 精英敌人出现！准备战斗...");
                _eventTriggered = true;
                SaveState();
                int eliteId = RandomProvider.Current.Range(10, 16);
                GameManager.Instance.StartDungeonBattle(eliteId);
                break;
            case WorldTileType.Treasure:
                HandleTreasure(tile);
                break;
            case WorldTileType.Camp:
                HandleCamp();
                break;
            case WorldTileType.Event:
                HandleRandomEvent();
                break;
        }
    }

    /// <summary>宝藏事件 — 金币 + 物品 + 全队小恢复</summary>
    void HandleTreasure(HexTile tile)
    {
        var gm = GameManager.Instance;
        int goldFound = RandomProvider.Current.Range(15, 40);
        gm.gold += goldFound;

        // 尝试掉落实物
        string lootStr = "";
        var drops = DropTable.RollDrops("treasure_chest");
        if (drops != null)
        {
            foreach (var stack in drops)
            {
                gm.AddItem(stack.itemId, stack.quantity);
                var def = ItemDatabase.Get(stack.itemId);
                string name = def != null ? def.itemName : stack.itemId;
                lootStr += $"  {name} x{stack.quantity}\n";
            }
        }

        // 小量HP恢复
        if (gm.playerTeamData != null)
        {
            foreach (var u in gm.playerTeamData)
            {
                if (u != null && u.currentHP > 0 && u.currentHP < u.maxHp)
                {
                    int heal = RandomProvider.Current.Range(5, 15);
                    u.currentHP = Mathf.Min(u.currentHP + heal, u.maxHp);
                }
            }
        }

        // 消耗事件格（变为普通）
        tile.tileType = WorldTileType.Normal;

        Log.Info($"[地图] 发现宝藏！金币 +{goldFound}");
        if (!string.IsNullOrEmpty(lootStr))
            Log.Info($"[地图] 获得物品:\n{lootStr}");
    }

    /// <summary>营地事件 — 全队大量恢复HP + 减压</summary>
    void HandleCamp()
    {
        var gm = GameManager.Instance;
        if (gm.playerTeamData == null) return;

        foreach (var u in gm.playerTeamData)
        {
            if (u == null) continue;
            // 恢复50%缺失HP
            int missing = u.maxHp - u.currentHP;
            int heal = Mathf.RoundToInt(missing * 0.5f);
            u.currentHP = Mathf.Min(u.currentHP + heal, u.maxHp);
            // 减压30点
            u.stress = Mathf.Max(u.stress - 30, 0);
        }

        // 消耗事件格
        tile.tileType = WorldTileType.Normal;

        Log.Info("[地图] 到达营地！队伍休整完毕，HP恢复50%，压力-30");
    }

    /// <summary>随机事件 — 多种可能</summary>
    void HandleRandomEvent()
    {
        var gm = GameManager.Instance;
        float roll = RandomProvider.Current.Value;

        if (roll < 0.25f)
        {
            // 遗迹发现：大量金币
            int gold = RandomProvider.Current.Range(20, 50);
            gm.gold += gold;
            Log.Info($"[地图] 探索遗迹！发现古代宝藏，金币 +{gold}");
        }
        else if (roll < 0.50f)
        {
            // 草药：治疗最低HP的队友
            if (gm.playerTeamData != null)
            {
                var weakest = gm.playerTeamData.Find(u => u != null && u.currentHP > 0 && u.currentHP < u.maxHp);
                if (weakest != null)
                {
                    int heal = RandomProvider.Current.Range(15, 30);
                    weakest.currentHP = Mathf.Min(weakest.currentHP + heal, weakest.maxHp);
                    Log.Info($"[地图] 发现草药！{weakest.unitName} HP +{heal}");
                }
                else
                    Log.Info("[地图] 发现草药，但没人需要治疗。");
            }
        }
        else if (roll < 0.75f)
        {
            // 险境：全队压力上升
            if (gm.playerTeamData != null)
            {
                int stress = RandomProvider.Current.Range(5, 15);
                foreach (var u in gm.playerTeamData)
                    if (u != null) u.stress = Mathf.Min(u.stress + stress, 200);
                Log.Info($"[地图] 遭遇险境！全队压力 +{stress}");
            }
        }
        else
        {
            // 心情愉悦：全队减压
            if (gm.playerTeamData != null)
            {
                int stress = RandomProvider.Current.Range(5, 15);
                foreach (var u in gm.playerTeamData)
                    if (u != null) u.stress = Mathf.Max(u.stress - stress, 0);
                Log.Info($"[地图] 风景宜人！全队压力 -{stress}");
            }
        }
    }

    /// <summary>收集地块资源 — 基于 TileLayerData 的产出系统</summary>
    void CollectTileResources(HexTile tile)
    {
        // 使用 TileLayerData 计算总产出
        TileYield yield = tile.GetYield();

        if (yield.food == 0 && yield.production == 0 && yield.gold == 0 &&
            yield.science == 0 && yield.culture == 0 && yield.faith == 0)
            return;

        // 食物 → 恢复全队HP
        if (yield.food > 0)
        {
            if (GameManager.Instance.playerTeamData != null)
            {
                foreach (var unit in GameManager.Instance.playerTeamData)
                {
                    if (unit != null && unit.currentHP > 0)
                    {
                        unit.currentHP = Mathf.Min(unit.currentHP + yield.food, unit.maxHp);
                    }
                }
            }
            Log.Info($"[4X] 食物：全队 HP +{yield.food}");
        }

        // 生产力 → 转换为金币
        if (yield.production > 0)
        {
            GameManager.Instance.gold += yield.production * 8;
            Log.Info($"[4X] 生产力：金币 +{yield.production * 8}");
        }

        // 金币
        if (yield.gold > 0)
        {
            GameManager.Instance.gold += yield.gold;
            Log.Info($"[4X] 金币：+{yield.gold}");
        }

        // 科学
        if (yield.science > 0)
        {
            GameManager.Instance.researchProgress += yield.science;
            Log.Info($"[4X] 科学：研究进度 +{yield.science}");
        }

        // 文化（新增）
        if (yield.culture > 0)
        {
            Log.Info($"[4X] 文化：+{yield.culture}");
        }

        // 信仰（新增）
        if (yield.faith > 0)
        {
            Log.Info($"[4X] 信仰：+{yield.faith}");
        }
    }

    #endregion

    #region 4X系统接口

    public int GetFactionTileCount(TileFaction faction)
    {
        int count = 0;
        foreach (var kv in _tiles)
        {
            if (kv.Value.faction == faction)
                count++;
        }
        return count;
    }

    /// <summary>获取玩家所有指定资源的总产出</summary>
    public int GetResourceProduction(ResourceType resourceType)
    {
        int total = 0;
        foreach (var kv in _tiles)
        {
            if (kv.Value.faction == TileFaction.Player && kv.Value.layer.resource == resourceType)
            {
                var yield = kv.Value.GetYield();
                // 根据资源类型累加对应产出
                switch (resourceType)
                {
                    case ResourceType.Wheat:
                    case ResourceType.Rice:
                    case ResourceType.Cattle:
                    case ResourceType.Fish:
                    case ResourceType.Deer:
                    case ResourceType.Berries:
                        total += yield.food;
                        break;
                    case ResourceType.Iron:
                    case ResourceType.Horses:
                    case ResourceType.Coal:
                    case ResourceType.Oil:
                    case ResourceType.Niter:
                    case ResourceType.Aluminum:
                        total += yield.production;
                        break;
                    case ResourceType.Gems:
                    case ResourceType.Silk:
                    case ResourceType.Spices:
                    case ResourceType.Silver:
                    case ResourceType.Gold_Ore:
                    case ResourceType.Ivory:
                    case ResourceType.Dye:
                    case ResourceType.Sugar:
                        total += yield.gold;
                        break;
                }
            }
        }
        return total;
    }

    /// <summary>获取玩家地块总产出汇总</summary>
    public TileYield GetPlayerTotalYield()
    {
        var total = new TileYield();
        foreach (var kv in _tiles)
        {
            if (kv.Value.faction == TileFaction.Player)
            {
                total = total + kv.Value.GetYield();
            }
        }
        return total;
    }

    public bool ExpandFaction(TileFaction faction, int count)
    {
        var expandable = new List<HexCoord>();

        foreach (var kv in _tiles)
        {
            if (kv.Value.faction == TileFaction.Neutral)
            {
                foreach (var neighbor in kv.Key.Neighbors())
                {
                    if (_tiles.ContainsKey(neighbor) && _tiles[neighbor].faction == faction)
                    {
                        expandable.Add(kv.Key);
                        break;
                    }
                }
            }
        }

        int expanded = 0;
        foreach (var coord in expandable)
        {
            if (expanded >= count) break;
            _tiles[coord].faction = faction;
            UpdateBorderColor(_tiles[coord]);
            expanded++;
        }

        return expanded > 0;
    }

    public bool AttackPlayerTerritory(TileFaction attackerFaction)
    {
        var attackTargets = new List<HexCoord>();

        foreach (var kv in _tiles)
        {
            if (kv.Value.faction == TileFaction.Player)
            {
                foreach (var neighbor in kv.Key.Neighbors())
                {
                    if (_tiles.ContainsKey(neighbor) && _tiles[neighbor].faction == attackerFaction)
                    {
                        attackTargets.Add(kv.Key);
                        break;
                    }
                }
            }
        }

        if (attackTargets.Count == 0) return false;

        float successChance = 0.3f;
        if (RandomProvider.Current.Value < successChance)
        {
            var target = attackTargets[RandomProvider.Current.Range(0, attackTargets.Count)];
            _tiles[target].faction = attackerFaction;
            UpdateBorderColor(_tiles[target]);
            return true;
        }

        return false;
    }

    public bool RetreatFaction(TileFaction faction, int count)
    {
        var edgeTiles = new List<HexCoord>();

        foreach (var kv in _tiles)
        {
            if (kv.Value.faction == faction)
            {
                bool isEdge = false;
                foreach (var neighbor in kv.Key.Neighbors())
                {
                    if (!_tiles.ContainsKey(neighbor) || _tiles[neighbor].faction != faction)
                    {
                        isEdge = true;
                        break;
                    }
                }
                if (isEdge) edgeTiles.Add(kv.Key);
            }
        }

        int retreated = 0;
        foreach (var coord in edgeTiles)
        {
            if (retreated >= count) break;
            _tiles[coord].faction = TileFaction.Neutral;
            UpdateBorderColor(_tiles[coord]);
            retreated++;
        }

        return retreated > 0;
    }

    #endregion
}
