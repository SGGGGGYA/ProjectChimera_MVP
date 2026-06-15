using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  文明6风格 多层地块数据结构
//  每个六角形单元格 = 基础地形 + 地貌 + 资源 + 区域（多层叠加）
// ============================================================

#region 势力与探索

/// <summary>地块势力归属</summary>
public enum TileFaction
{
    Neutral,    // 中立
    Player,     // 玩家
    Enemy1,     // 敌对势力1
    Enemy2      // 敌对势力2
}

/// <summary>战争迷雾状态</summary>
public enum FogState
{
    Hidden,     // 未探索（完全不可见）
    Explored,   // 已探索（可见但不实时更新）
    Visible     // 当前视野（实时更新）
}

#endregion

#region 地图快照（战斗/场景切换持久化）

/// <summary>
/// 六边形地图快照 — 用于在场景切换（如进入战斗）时保存地图状态
/// 返回世界地图时从快照恢复，避免重新生成丢失进度
/// </summary>
[System.Serializable]
public class HexMapSnapshot
{
    public int width;
    public int height;
    public int squadQ;
    public int squadR;
    public TileLayerData[] tiles; // 按 row*width+col 线性存储

    public HexMapSnapshot(int w, int h)
    {
        width = w;
        height = h;
        tiles = new TileLayerData[w * h];
    }

    public int GetIndex(int col, int row) => row * width + col;
}

#endregion

#region 基础地形（Base Terrain）

/// <summary>
/// 基础地形类型 — 决定地块的基础产出和移动消耗
/// </summary>
public enum TerrainType
{
    Grassland,   // 平原 - 基础食物产出
    Plains,      // 草原 - 食物+生产力
    Desert,      // 沙漠 - 干燥贫瘠
    Tundra,      // 冻土 - 寒冷贫瘠
    Ocean,       // 海洋 - 不可通行（陆地单位）
    Coast,       // 海岸 - 浅水，可通行
    Mountain     // 山脉 - 不可通行，阻挡视线
}

#endregion

#region 地貌（Features）

/// <summary>
/// 地貌类型 — 叠加在基础地形之上，提供额外产出或特殊效果
/// </summary>
public enum FeatureType
{
    None,        // 无地貌
    Forest,      // 森林 - +1生产力，增加防御
    Rainforest,  // 雨林 - +1食物，增加移动消耗
    Marsh,       // 沼泽 - +1食物，大幅增加移动消耗
    Oasis,       // 绿洲 - +3食物+1金币，沙漠中的宝地
    Floodplain,  // 泛滥平原 - +2食物，洪水风险
    Reef,        // 暗礁 - 海岸地貌，阻挡船只
    Ice,         // 冰川 - 不可通行
    Woods        // 林地 - 轻微减速
}

#endregion

#region 自然资源（Resources）

/// <summary>
/// 资源分类
/// </summary>
public enum ResourceCategory
{
    Bonus,       // 加成资源 - 提供基础产出
    Strategic,   // 战略资源 - 建造高级单位/建筑必需
    Luxury       // 奢侈资源 - 提供宜居度
}

/// <summary>
/// 自然资源类型
/// </summary>
public enum ResourceType
{
    None,

    // 加成资源
    Wheat,       // 小麦 - +1食物
    Rice,        // 大米 - +1食物
    Cattle,      // 牛 - +1食物
    Fish,        // 鱼 - +1食物
    Deer,        // 鹿 - +1食物
    Berries,     // 浆果 - +1食物

    // 战略资源
    Iron,        // 铁 - 兵器
    Horses,      // 马 - 骑兵
    Coal,        // 煤 - 工业
    Oil,         // 石油 - 现代单位
    Niter,       // 硝石 - 火药
    Aluminum,    // 铝 - 航空

    // 奢侈资源
    Gems,        // 宝石 - +3金币
    Silk,        // 丝绸 - +3金币
    Spices,      // 香料 - +2金币
    Silver,      // 白银 - +2金币
    Gold_Ore,    // 金矿 - +3金币
    Ivory,       // 象牙 - +2金币
    Dye,         // 染料 - +2金币
    Sugar        // 糖 - +2金币
}

/// <summary>
/// 资源产出定义
/// </summary>
[System.Serializable]
public class ResourceYield
{
    public int food;
    public int production;
    public int gold;
    public int science;
    public int faith;

    public ResourceYield(int food = 0, int production = 0, int gold = 0, int science = 0, int faith = 0)
    {
        this.food = food;
        this.production = production;
        this.gold = gold;
        this.science = science;
        this.faith = faith;
    }

    public static ResourceYield operator +(ResourceYield a, ResourceYield b)
    {
        return new ResourceYield(
            a.food + b.food,
            a.production + b.production,
            a.gold + b.gold,
            a.science + b.science,
            a.faith + b.faith
        );
    }
}

#endregion

#region 区域系统（Districts）

/// <summary>
/// 区域类型 — 城市规划的核心
/// </summary>
public enum DistrictType
{
    None,              // 无区域
    CityCenter,        // 城市中心
    Campus,            // 学院 - 科研
    HolySite,          // 圣地 - 信仰
    CommercialHub,     // 商业中心 - 金币
    IndustrialZone,    // 工业区 - 生产力
    TheaterSquare,     // 剧院广场 - 文化
    Encampment,        // 军营 - 军事
    Harbor,            // 港口 - 海军+金币
    Aqueduct,          // 引水渠 - 住房
    Neighborhood,      // 社区 - 住房
    Spaceport,         // 航天基地 - 胜利
    Aerodrome,         // 机场 - 空军
    EntertainmentComplex, // 娱乐区 - 宜居度
    WaterPark,         // 水上乐园 - 宜居度
    Preserve           // 自然保护区
}

/// <summary>
/// 区域数据
/// </summary>
[System.Serializable]
public class DistrictData
{
    public DistrictType type;
    public int level;           // 建筑等级 (0-3)
    public bool isPillaged;     // 是否被掠夺

    public DistrictData(DistrictType type)
    {
        this.type = type;
        this.level = 0;
        this.isPillaged = false;
    }
}

#endregion

#region 城市系统（City）

/// <summary>
/// 城市数据
/// </summary>
[System.Serializable]
public class CityData
{
    public string cityName;
    public int population;          // 人口
    public int foodStockpile;       // 食物储备
    public int foodPerTurn;         // 每回合食物
    public int productionPerTurn;   // 每回合生产力
    public int goldPerTurn;         // 每回合金币
    public int sciencePerTurn;      // 每回合科研
    public int culturePerTurn;      // 每回合文化
    public int faithPerTurn;        // 每回合信仰
    public int housing;             // 住房上限
    public int amenity;             // 宜居度

    // 城市中心坐标
    public HexCoord centerCoord;

    // 已控制的工作地块（3环内）
    public HashSet<HexCoord> workedTiles = new HashSet<HexCoord>();

    // 已建造的区域
    public List<HexCoord> districts = new List<HexCoord>();

    // 边界扩张进度
    public int cultureAccumulated;
    public int cultureBorderThreshold;

    public CityData(string name, HexCoord center)
    {
        cityName = name;
        centerCoord = center;
        population = 1;
        foodStockpile = 0;
        housing = 4;       // 基础住房
        cultureBorderThreshold = 10;
        cultureAccumulated = 0;
    }
}

#endregion

#region 地块产出汇总

/// <summary>
/// 地块总产出（多层叠加计算结果）
/// </summary>
[System.Serializable]
public class TileYield
{
    public int food;
    public int production;
    public int gold;
    public int science;
    public int culture;
    public int faith;

    public TileYield() { }

    public TileYield(int food, int production, int gold, int science = 0, int culture = 0, int faith = 0)
    {
        this.food = food;
        this.production = production;
        this.gold = gold;
        this.science = science;
        this.culture = culture;
        this.faith = faith;
    }

    public static TileYield operator +(TileYield a, TileYield b)
    {
        return new TileYield(
            a.food + b.food,
            a.production + b.production,
            a.gold + b.gold,
            a.science + b.science,
            a.culture + b.culture,
            a.faith + b.faith
        );
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (food != 0) parts.Add($"食+{food}");
        if (production != 0) parts.Add($"产+{production}");
        if (gold != 0) parts.Add($"金+{gold}");
        if (science != 0) parts.Add($"科+{science}");
        if (culture != 0) parts.Add($"文+{culture}");
        if (faith != 0) parts.Add($"信+{faith}");
        return string.Join(" ", parts);
    }
}

#endregion

#region 地块层定义

/// <summary>
/// 单个地块的完整数据 — 文明6多层叠加结构
/// 从底到顶：地形 → 地貌 → 资源 → 区域
/// </summary>
[System.Serializable]
public class TileLayerData
{
    // === 第1层：基础地形 ===
    public TerrainType terrain;

    // === 第2层：地貌 ===
    public FeatureType feature = FeatureType.None;

    // === 第3层：资源 ===
    public ResourceType resource = ResourceType.None;
    public bool resourceImproved = false;   // 是否已改良

    // === 第4层：区域/建筑 ===
    public DistrictData district = null;

    // === 归属 ===
    public TileFaction faction = TileFaction.Neutral;
    public string ownerCityName = null;     // 所属城市名

    // === 探索状态 ===
    public FogState fogState = FogState.Hidden;
    public bool isExplored = false;

    // === 游戏事件（保留原有系统）=== 
    public WorldTileType eventType = WorldTileType.Normal;

    // === 移动相关 ===
    public bool IsPassable
    {
        get
        {
            if (terrain == TerrainType.Mountain) return false;
            if (terrain == TerrainType.Ocean) return false;
            if (feature == FeatureType.Ice) return false;
            return true;
        }
    }

    /// <summary>计算移动消耗（100为1点行动力）</summary>
    public int MovementCost
    {
        get
        {
            if (!IsPassable) return 9999;

            int cost = 100; // 基础1点

            // 地形修正
            switch (terrain)
            {
                case TerrainType.Plains:
                    cost += 25;   // 草原 +0.25
                    break;
                case TerrainType.Coast:
                    cost += 50;   // 海岸 +0.5
                    break;
                case TerrainType.Tundra:
                    cost += 25;   // 冻土 +0.25
                    break;
            }

            // 地貌修正
            switch (feature)
            {
                case FeatureType.Forest:
                case FeatureType.Woods:
                    cost += 50;   // 森林 +0.5
                    break;
                case FeatureType.Rainforest:
                    cost += 75;   // 雨林 +0.75
                    break;
                case FeatureType.Marsh:
                    cost += 100;  // 沼泽 +1
                    break;
            }

            return cost;
        }
    }

    /// <summary>防御加成百分比</summary>
    public int DefenseBonus
    {
        get
        {
            int bonus = 0;
            if (feature == FeatureType.Forest || feature == FeatureType.Woods)
                bonus += 3;
            if (feature == FeatureType.Rainforest)
                bonus += 3;
            return bonus;
        }
    }

    /// <summary>计算地块总产出</summary>
    public TileYield CalculateYield()
    {
        var y = new TileYield();

        // 第1层：基础地形产出
        switch (terrain)
        {
            case TerrainType.Grassland:
                y.food = 2;
                break;
            case TerrainType.Plains:
                y.food = 1;
                y.production = 1;
                break;
            case TerrainType.Desert:
                // 无基础产出
                break;
            case TerrainType.Tundra:
                y.food = 1;
                break;
            case TerrainType.Coast:
                y.food = 1;
                y.gold = 1;
                break;
            case TerrainType.Ocean:
                y.food = 1;
                y.gold = 1;
                break;
            case TerrainType.Mountain:
                break;
        }

        // 第2层：地貌修正
        switch (feature)
        {
            case FeatureType.Forest:
                y.production += 1;
                break;
            case FeatureType.Rainforest:
                y.food += 1;
                break;
            case FeatureType.Marsh:
                y.food += 1;
                break;
            case FeatureType.Oasis:
                y.food += 3;
                y.gold += 1;
                break;
            case FeatureType.Floodplain:
                y.food += 2;
                break;
        }

        // 第3层：资源加成
        switch (resource)
        {
            case ResourceType.Wheat:
            case ResourceType.Rice:
                y.food += 1;
                break;
            case ResourceType.Cattle:
                y.food += 1;
                break;
            case ResourceType.Fish:
                y.food += 1;
                break;
            case ResourceType.Deer:
                y.food += 1;
                y.production += 1;
                break;
            case ResourceType.Iron:
                y.production += 1;
                break;
            case ResourceType.Horses:
                y.production += 1;
                break;
            case ResourceType.Gems:
                y.gold += 3;
                break;
            case ResourceType.Silk:
            case ResourceType.Ivory:
                y.gold += 3;
                break;
            case ResourceType.Spices:
            case ResourceType.Silver:
            case ResourceType.Dye:
            case ResourceType.Sugar:
                y.gold += 2;
                break;
            case ResourceType.Gold_Ore:
                y.gold += 3;
                break;
            case ResourceType.Coal:
            case ResourceType.Niter:
                y.production += 1;
                break;
            case ResourceType.Berries:
                y.food += 1;
                break;
        }

        // 第4层：区域加成
        if (district != null)
        {
            switch (district.type)
            {
                case DistrictType.Campus:
                    y.science += 2;
                    break;
                case DistrictType.HolySite:
                    y.faith += 2;
                    break;
                case DistrictType.CommercialHub:
                    y.gold += 3;
                    break;
                case DistrictType.IndustrialZone:
                    y.production += 2;
                    break;
                case DistrictType.TheaterSquare:
                    y.culture += 2;
                    break;
            }
        }

        return y;
    }
}

#endregion

#region 区域相邻加成

/// <summary>
/// 区域相邻加成计算器
/// </summary>
public static class AdjacencyBonus
{
    /// <summary>
    /// 计算指定坐标的区域相邻加成
    /// </summary>
    public static int CalculateBonus(HexCoord coord, DistrictType districtType,
        Dictionary<HexCoord, TileLayerData> tileMap, int mapRadius)
    {
        int bonus = 0;
        var neighbors = GetValidNeighbors(coord, tileMap, mapRadius);

        foreach (var nCoord in neighbors)
        {
            if (!tileMap.ContainsKey(nCoord)) continue;
            var neighbor = tileMap[nCoord];

            switch (districtType)
            {
                case DistrictType.Campus:
                    // 学院：每相邻1个山脉/雨林/区域 +1科研
                    if (neighbor.terrain == TerrainType.Mountain) bonus += 1;
                    if (neighbor.feature == FeatureType.Rainforest) bonus += 1;
                    if (neighbor.district != null) bonus += 1;
                    break;

                case DistrictType.HolySite:
                    // 圣地：每相邻1个山脉/森林/自然奇观 +1信仰
                    if (neighbor.terrain == TerrainType.Mountain) bonus += 1;
                    if (neighbor.feature == FeatureType.Forest || neighbor.feature == FeatureType.Woods) bonus += 1;
                    if (neighbor.feature == FeatureType.Oasis) bonus += 2;
                    break;

                case DistrictType.CommercialHub:
                    // 商业中心：每相邻1条河流/港口 +2金币
                    if (neighbor.terrain == TerrainType.Coast) bonus += 2;
                    if (neighbor.district != null && neighbor.district.type == DistrictType.Harbor) bonus += 2;
                    if (neighbor.district != null) bonus += 1;
                    break;

                case DistrictType.IndustrialZone:
                    // 工业区：每相邻1个丘陵/战略资源/区域 +1生产力
                    if (neighbor.terrain == TerrainType.Plains) bonus += 1;
                    if (neighbor.resource != ResourceType.None && IsStrategicResource(neighbor.resource)) bonus += 1;
                    if (neighbor.district != null) bonus += 1;
                    break;

                case DistrictType.TheaterSquare:
                    // 剧院广场：每相邻1个奇观/区域 +1文化
                    if (neighbor.district != null) bonus += 1;
                    break;

                case DistrictType.Harbor:
                    // 港口：每相邻1个海洋资源 +1金币
                    if (neighbor.terrain == TerrainType.Coast || neighbor.terrain == TerrainType.Ocean)
                        bonus += 1;
                    if (neighbor.resource != ResourceType.None && IsWaterResource(neighbor.resource))
                        bonus += 2;
                    break;
            }
        }

        return bonus;
    }

    /// <summary>获取有效邻居坐标</summary>
    static List<HexCoord> GetValidNeighbors(HexCoord coord,
        Dictionary<HexCoord, TileLayerData> tileMap, int mapRadius)
    {
        var result = new List<HexCoord>();
        foreach (var n in coord.Neighbors())
        {
            if (tileMap.ContainsKey(n))
                result.Add(n);
        }
        return result;
    }

    static bool IsStrategicResource(ResourceType r)
    {
        return r == ResourceType.Iron || r == ResourceType.Horses ||
               r == ResourceType.Coal || r == ResourceType.Oil ||
               r == ResourceType.Niter || r == ResourceType.Aluminum;
    }

    static bool IsWaterResource(ResourceType r)
    {
        return r == ResourceType.Fish;
    }
}

#endregion

#region 产出定义表

/// <summary>
/// 静态产出定义表（供查表使用）
/// </summary>
public static class YieldTable
{
    public static ResourceYield GetResourceYield(ResourceType resource)
    {
        switch (resource)
        {
            case ResourceType.Wheat:     return new ResourceYield(1, 0, 0);
            case ResourceType.Rice:      return new ResourceYield(1, 0, 0);
            case ResourceType.Cattle:    return new ResourceYield(1, 0, 0);
            case ResourceType.Fish:      return new ResourceYield(1, 0, 0);
            case ResourceType.Deer:      return new ResourceYield(1, 1, 0);
            case ResourceType.Berries:   return new ResourceYield(1, 0, 0);
            case ResourceType.Iron:      return new ResourceYield(0, 1, 0);
            case ResourceType.Horses:    return new ResourceYield(0, 1, 0);
            case ResourceType.Coal:      return new ResourceYield(0, 1, 0);
            case ResourceType.Oil:       return new ResourceYield(0, 1, 0);
            case ResourceType.Niter:     return new ResourceYield(0, 1, 0);
            case ResourceType.Aluminum:  return new ResourceYield(0, 1, 0);
            case ResourceType.Gems:      return new ResourceYield(0, 0, 3);
            case ResourceType.Silk:      return new ResourceYield(0, 0, 3);
            case ResourceType.Spices:    return new ResourceYield(0, 0, 2);
            case ResourceType.Silver:    return new ResourceYield(0, 0, 2);
            case ResourceType.Gold_Ore:  return new ResourceYield(0, 0, 3);
            case ResourceType.Ivory:     return new ResourceYield(0, 0, 3);
            case ResourceType.Dye:       return new ResourceYield(0, 0, 2);
            case ResourceType.Sugar:     return new ResourceYield(0, 0, 2);
            default:                     return new ResourceYield(0, 0, 0);
        }
    }

    public static ResourceCategory GetResourceCategory(ResourceType resource)
    {
        switch (resource)
        {
            case ResourceType.Wheat:
            case ResourceType.Rice:
            case ResourceType.Cattle:
            case ResourceType.Fish:
            case ResourceType.Deer:
            case ResourceType.Berries:
                return ResourceCategory.Bonus;

            case ResourceType.Iron:
            case ResourceType.Horses:
            case ResourceType.Coal:
            case ResourceType.Oil:
            case ResourceType.Niter:
            case ResourceType.Aluminum:
                return ResourceCategory.Strategic;

            case ResourceType.Gems:
            case ResourceType.Silk:
            case ResourceType.Spices:
            case ResourceType.Silver:
            case ResourceType.Gold_Ore:
            case ResourceType.Ivory:
            case ResourceType.Dye:
            case ResourceType.Sugar:
                return ResourceCategory.Luxury;

            default:
                return ResourceCategory.Bonus;
        }
    }
}

#endregion
