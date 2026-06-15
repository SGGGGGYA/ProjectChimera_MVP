using UnityEngine;

/// <summary>
/// 地牢类型枚举
/// </summary>
public enum DungeonType
{
    Ruins,
    Warrens,
    Weald,
    Cove,
    Courtyard
}

public class DungeonSession
{
    public DungeonMap currentMap;
    public int currentFloor = 1;
    public bool isInDungeon;
    public Vector2Int savedWorldMapPos;
    public DungeonNode lastVisitedNode;
    public DungeonType dungeonType = DungeonType.Ruins;

    /// <summary>
    /// 根据当前 DungeonType 返回对应的背景图 Resources 路径
    /// 当前仅导入 ruins.png，其他类型暂时回退到 ruins
    /// </summary>
    public string GetBackgroundPath()
    {
        switch (dungeonType)
        {
            case DungeonType.Ruins:
                return "Backgrounds/Dungeon/ruins";
            case DungeonType.Warrens:
                // TODO: 导入 warrens 背景后切换
                return "Backgrounds/Dungeon/ruins";
            case DungeonType.Weald:
                // TODO: 导入 weald 背景后切换
                return "Backgrounds/Dungeon/ruins";
            case DungeonType.Cove:
                // TODO: 导入 cove 背景后切换
                return "Backgrounds/Dungeon/ruins";
            case DungeonType.Courtyard:
                // TODO: 导入 courtyard 背景后切换
                return "Backgrounds/Dungeon/ruins";
            default:
                return "Backgrounds/Dungeon/ruins";
        }
    }

    /// <summary>
    /// 获取地牢类型的显示名称
    /// </summary>
    public string GetDungeonTypeDisplayName()
    {
        switch (dungeonType)
        {
            case DungeonType.Ruins: return "遗迹";
            case DungeonType.Warrens: return "兽窟";
            case DungeonType.Weald: return "荒野";
            case DungeonType.Cove: return "海湾";
            case DungeonType.Courtyard: return "庭院";
            default: return "未知";
        }
    }

    public void EnterDungeon()
    {
        savedWorldMapPos = GameManager.Instance.savedSquadPos;
        currentFloor = 1;
        currentMap = DungeonGenerator.GenerateMap(currentFloor, 1);
        isInDungeon = true;
        lastVisitedNode = null;
    }

    public void NextFloor()
    {
        currentFloor++;
        currentMap = DungeonGenerator.GenerateMap(currentFloor, currentFloor);
        lastVisitedNode = null;
    }

    public void ExitDungeon()
    {
        GameManager.Instance.savedSquadPos = savedWorldMapPos;
        isInDungeon = false;
        currentMap = null;
        lastVisitedNode = null;
    }
}
