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
