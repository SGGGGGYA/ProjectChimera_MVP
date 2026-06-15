// Core/DungeonTypes.cs
// 把 MapGen 模块的纯数据类(DungeonMap / DungeonNode / 枚举)集中到 Core,
// 目的是打破 Core ↔ MapGen 的循环引用。
// MapGen 模块的"行为"(DungeonGenerator)仍然留在 MapGen 里,通过 asmdef 引用 Core 使用这些类型。
using System.Collections.Generic;

public enum DungeonNodeType
{
    Battle,
    Elite,
    Rest,
    Treasure,
    Boss,
    Exit
}

[System.Serializable]
public class DungeonNode
{
    public int x;
    public int y;
    public DungeonNodeType type;
    public bool visited;
    public List<int> nextNodeIndices = new List<int>();
    public int enemyTemplateId = -1;
}

[System.Serializable]
public class DungeonMap
{
    public int currentFloor;
    public List<DungeonNode> nodes = new List<DungeonNode>();
    public int currentNodeIndex = -1;
    public int maxNodesPerRow;
}
