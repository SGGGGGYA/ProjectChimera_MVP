using System.Collections.Generic;
using UnityEngine;

public struct HexCoord
{
    public int q;
    public int r;
    public int s => -q - r;

    public static HexCoord Zero = new HexCoord(0, 0);

    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    static readonly HexCoord[] _neighbors = new HexCoord[]
    {
        new HexCoord( 1,  0),
        new HexCoord( 1, -1),
        new HexCoord( 0, -1),
        new HexCoord(-1,  0),
        new HexCoord(-1,  1),
        new HexCoord( 0,  1),
    };

    public HexCoord Neighbor(int dir) => new HexCoord(q + _neighbors[dir].q, r + _neighbors[dir].r);

    public IEnumerable<HexCoord> Neighbors()
    {
        for (int i = 0; i < 6; i++)
            yield return Neighbor(i);
    }

    public IEnumerable<HexCoord> AllNeighbors()
    {
        for (int i = 0; i < 6; i++)
            yield return Neighbor(i);
    }

    public int Distance(HexCoord other)
    {
        return (Mathf.Abs(q - other.q) + Mathf.Abs(r - other.r) + Mathf.Abs(s - other.s)) / 2;
    }

    public static List<HexCoord> TilesInRadius(int radius)
    {
        var result = new List<HexCoord>();
        for (int dq = -radius; dq <= radius; dq++)
            for (int dr = Mathf.Max(-radius, -dq - radius); dr <= Mathf.Min(radius, -dq + radius); dr++)
                result.Add(new HexCoord(dq, dr));
        return result;
    }

    public Vector3 ToWorld(float size)
    {
        // 尖顶六边形坐标计算
        float x = size * Mathf.Sqrt(3f) * (q + r / 2f);
        float y = size * 1.5f * r;
        return new Vector3(x, y, 0);
    }

    public static HexCoord FromWorld(Vector3 pos, float size)
    {
        float q = (Mathf.Sqrt(3f) / 3f * pos.x - 1f / 3f * pos.y) / size;
        float r = (2f / 3f * pos.y) / size;
        return Round(q, r);
    }

    static HexCoord Round(float q, float r)
    {
        float s = -q - r;
        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);
        float dq = Mathf.Abs(rq - q);
        float dr = Mathf.Abs(rr - r);
        float ds = Mathf.Abs(rs - s);
        if (dq > dr && dq > ds)
            rq = -rr - rs;
        else if (dr > ds)
            rr = -rq - rs;
        return new HexCoord(rq, rr);
    }

    public override string ToString() => $"({q},{r})";

    public override bool Equals(object obj) => obj is HexCoord other && q == other.q && r == other.r;
    public override int GetHashCode() => (q << 16) ^ r;

    /// <summary>获取两个坐标之间的路径（BFS）</summary>
    public static List<HexCoord> GetPath(HexCoord start, HexCoord end, float size)
    {
        var path = new List<HexCoord>();
        var visited = new HashSet<HexCoord>();
        var queue = new Queue<List<HexCoord>>();

        queue.Enqueue(new List<HexCoord> { start });
        visited.Add(start);

        while (queue.Count > 0)
        {
            var currentPath = queue.Dequeue();
            var current = currentPath[currentPath.Count - 1];

            if (current.Equals(end))
            {
                path = currentPath;
                break;
            }

            foreach (var neighbor in current.Neighbors())
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    var newPath = new List<HexCoord>(currentPath) { neighbor };
                    queue.Enqueue(newPath);
                }
            }
        }

        return path;
    }
}
