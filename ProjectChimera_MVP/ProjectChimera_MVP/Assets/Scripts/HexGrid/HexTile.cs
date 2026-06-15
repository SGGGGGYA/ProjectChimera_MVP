using UnityEngine;

/// <summary>
/// 六边形地块组件 — 文明6多层叠加结构
/// 底层数据存储在 TileLayerData 中，本组件提供便捷访问
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class HexTile : MonoBehaviour
{
    public HexCoord coord;

    /// <summary>多层地块数据（地形→地貌→资源→区域）</summary>
    public TileLayerData layer = new TileLayerData();

    // === 便捷属性：代理到 layer ===

    public TerrainType terrainType
    {
        get => layer.terrain;
        set => layer.terrain = value;
    }

    public FeatureType featureType
    {
        get => layer.feature;
        set => layer.feature = value;
    }

    public ResourceType resourceType
    {
        get => layer.resource;
        set => layer.resource = value;
    }

    public DistrictData district
    {
        get => layer.district;
        set => layer.district = value;
    }

    public WorldTileType tileType
    {
        get => layer.eventType;
        set => layer.eventType = value;
    }

    public TileFaction faction
    {
        get => layer.faction;
        set => layer.faction = value;
    }

    public FogState fogState
    {
        get => layer.fogState;
        set => layer.fogState = value;
    }

    public bool isExplored
    {
        get => layer.isExplored;
        set => layer.isExplored = value;
    }

    public bool isAccessible => layer.IsPassable;

    public int movementCost => layer.MovementCost;

    public int defenseBonus => layer.DefenseBonus;

    // === 城市引用 ===
    public CityData ownerCity;

    // === 旧版兼容字段（供未迁移代码使用）===
    [System.Obsolete("使用 layer.resource 代替")]
    public TileResourceType legacyResourceType = TileResourceType.None;
    [System.Obsolete("使用 layer.CalculateYield() 代替")]
    public int resourceAmount = 0;

    [Header("地形效果")]
    public int visionRange = 1;

    HexWorldMap _map;

    void Awake()
    {
        _map = GetComponentInParent<HexWorldMap>();
        var col = GetComponent<PolygonCollider2D>();
        if (col != null && col.points.Length == 0)
        {
            col.points = HexTileShape.GeneratePoints(0.5f);
        }
    }

    void OnMouseDown()
    {
        if (_map != null && !HexWorldMap.IsUIPanelOpen())
            _map.OnTileClicked(this);
    }

    /// <summary>获取地块总产出</summary>
    public TileYield GetYield()
    {
        return layer.CalculateYield();
    }

    /// <summary>获取资源产出描述</summary>
    public string GetResourceDescription()
    {
        if (layer.resource == ResourceType.None) return "";
        var y = YieldTable.GetResourceYield(layer.resource);
        return $"{layer.resource}: {y}";
    }

    /// <summary>获取地块描述（多层信息）</summary>
    public string GetTileDescription()
    {
        // 第1层：基础地形
        string terrainName = "";
        switch (layer.terrain)
        {
            case TerrainType.Grassland: terrainName = "平原"; break;
            case TerrainType.Plains:    terrainName = "草原"; break;
            case TerrainType.Desert:    terrainName = "沙漠"; break;
            case TerrainType.Tundra:    terrainName = "冻土"; break;
            case TerrainType.Ocean:     terrainName = "海洋"; break;
            case TerrainType.Coast:     terrainName = "海岸"; break;
            case TerrainType.Mountain:  terrainName = "山脉"; break;
        }

        string desc = terrainName;

        // 第2层：地貌
        if (layer.feature != FeatureType.None)
        {
            string featureName = "";
            switch (layer.feature)
            {
                case FeatureType.Forest:      featureName = "森林"; break;
                case FeatureType.Rainforest:  featureName = "雨林"; break;
                case FeatureType.Marsh:       featureName = "沼泽"; break;
                case FeatureType.Oasis:       featureName = "绿洲"; break;
                case FeatureType.Floodplain:  featureName = "泛滥平原"; break;
                case FeatureType.Reef:        featureName = "暗礁"; break;
                case FeatureType.Ice:         featureName = "冰川"; break;
                case FeatureType.Woods:       featureName = "林地"; break;
            }
            desc += $" · {featureName}";
        }

        // 第3层：资源
        if (layer.resource != ResourceType.None)
        {
            var cat = YieldTable.GetResourceCategory(layer.resource);
            string catTag = cat == ResourceCategory.Strategic ? "[战略]" :
                            cat == ResourceCategory.Luxury ? "[奢侈]" : "[加成]";
            desc += $" {catTag}{layer.resource}";
        }

        // 第4层：区域
        if (layer.district != null)
        {
            string districtName = "";
            switch (layer.district.type)
            {
                case DistrictType.CityCenter:       districtName = "城市中心"; break;
                case DistrictType.Campus:           districtName = "学院"; break;
                case DistrictType.HolySite:         districtName = "圣地"; break;
                case DistrictType.CommercialHub:    districtName = "商业中心"; break;
                case DistrictType.IndustrialZone:   districtName = "工业区"; break;
                case DistrictType.TheaterSquare:    districtName = "剧院广场"; break;
                case DistrictType.Encampment:       districtName = "军营"; break;
                case DistrictType.Harbor:           districtName = "港口"; break;
                default: districtName = layer.district.type.ToString(); break;
            }
            desc += $" [{districtName}]";
        }

        // 事件类型
        switch (layer.eventType)
        {
            case WorldTileType.Battle:   desc += " [战斗区]"; break;
            case WorldTileType.Elite:    desc += " [精英区]"; break;
            case WorldTileType.Treasure: desc += " [宝藏区]"; break;
            case WorldTileType.Camp:     desc += " [营地]"; break;
            case WorldTileType.Event:    desc += " [事件区]"; break;
        }

        // 势力
        if (layer.faction != TileFaction.Neutral)
            desc += $" [{layer.faction}]";

        // 产出
        var tileYield = GetYield();
        if (tileYield.food > 0 || tileYield.production > 0 || tileYield.gold > 0)
            desc += $" ({tileYield})";

        return desc;
    }
}

public static class HexTileShape
{
    public static Vector2[] GeneratePoints(float size)
    {
        var pts = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i - 30f);
            pts[i] = new Vector2(size * Mathf.Cos(angle), size * Mathf.Sin(angle));
        }
        return pts;
    }
}
