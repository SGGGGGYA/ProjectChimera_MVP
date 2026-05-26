using System.Collections.Generic;
using UnityEngine;

public static class DungeonGenerator
{
    public static DungeonMap GenerateMap(int floor, int difficulty)
    {
        int seed = floor * 10000 + System.DateTime.Now.DayOfYear * 100 + System.DateTime.Now.Hour;
        System.Random rng = new System.Random(seed);

        DungeonMap map = new DungeonMap();
        map.currentFloor = floor;

        int cols0 = rng.Next(3, 5);
        int cols1 = rng.Next(3, 5);
        int cols2 = rng.Next(3, 5);
        map.maxNodesPerRow = Mathf.Max(cols0, cols1, cols2);

        List<DungeonNode> nodes = new List<DungeonNode>();

        int row0Start = 0;
        for (int c = 0; c < cols0; c++)
        {
            nodes.Add(new DungeonNode { x = c, y = 0, type = DungeonNodeType.Battle, visited = false, enemyTemplateId = RangedInt(rng, 0, 3) });
        }

        int row1Start = nodes.Count;
        for (int c = 0; c < cols1; c++)
        {
            float roll = (float)rng.NextDouble();
            DungeonNodeType type;
            int templateId = -1;
            if (roll < 0.35f) { type = DungeonNodeType.Rest; }
            else if (roll < 0.55f) { type = DungeonNodeType.Elite; templateId = 10 + RangedInt(rng, 0, 1); }
            else if (roll < 0.80f) { type = DungeonNodeType.Battle; templateId = RangedInt(rng, 0, 3); }
            else { type = DungeonNodeType.Treasure; }
            nodes.Add(new DungeonNode { x = c, y = 1, type = type, visited = false, enemyTemplateId = templateId });
        }

        int row2Start = nodes.Count;
        int bossCol = rng.Next(0, cols2);
        for (int c = 0; c < cols2; c++)
        {
            DungeonNodeType type;
            int templateId = 0;
            if (c == bossCol)
            {
                type = DungeonNodeType.Boss;
                templateId = 100;
            }
            else
            {
                float roll = (float)rng.NextDouble();
                if (roll < 0.4f) { type = DungeonNodeType.Battle; templateId = RangedInt(rng, 0, 3); }
                else if (roll < 0.7f) { type = DungeonNodeType.Elite; templateId = 10 + RangedInt(rng, 0, 1); }
                else { type = DungeonNodeType.Treasure; templateId = -1; }
            }
            nodes.Add(new DungeonNode { x = c, y = 2, type = type, visited = false, enemyTemplateId = templateId });
        }

        int exitIndex = nodes.Count;
        DungeonNode exitNode = new DungeonNode { x = bossCol, y = 3, type = DungeonNodeType.Exit, visited = false, enemyTemplateId = -1 };
        nodes.Add(exitNode);

        ConnectRows(nodes, row0Start, row1Start, cols0, cols1, rng);
        ConnectRows(nodes, row1Start, row2Start, cols1, cols2, rng);

        int bossNodeIndex = row2Start + bossCol;
        nodes[bossNodeIndex].nextNodeIndices.Add(exitIndex);

        map.nodes = nodes;
        return map;
    }

    static void ConnectRows(List<DungeonNode> nodes, int rowStart, int nextRowStart, int currentCols, int nextCols, System.Random rng)
    {
        for (int i = 0; i < currentCols; i++)
        {
            int nodeIndex = rowStart + i;
            float ratio = currentCols > 1 ? (float)i / (currentCols - 1) : 0f;
            int targetCol = Mathf.RoundToInt(ratio * (nextCols - 1));
            targetCol = Mathf.Clamp(targetCol, 0, nextCols - 1);

            nodes[nodeIndex].nextNodeIndices.Add(nextRowStart + targetCol);

            if (rng.Next(0, 2) == 0 && targetCol + 1 < nextCols)
                nodes[nodeIndex].nextNodeIndices.Add(nextRowStart + targetCol + 1);
            else if (targetCol - 1 >= 0)
                nodes[nodeIndex].nextNodeIndices.Add(nextRowStart + targetCol - 1);
        }
    }

    static int RangedInt(System.Random rng, int min, int max)
    {
        return rng.Next(min, max + 1);
    }
}
