using UnityEngine;

public class DungeonSession
{
    public DungeonMap currentMap;
    public int currentFloor = 1;
    public bool isInDungeon;
    public Vector2Int savedWorldMapPos;
    public DungeonNode lastVisitedNode;

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
