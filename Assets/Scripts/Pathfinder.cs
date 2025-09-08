// Pathfinder.cs
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    GridSystem grid;
    void Awake() { grid = GetComponent<GridSystem>(); }

    public struct ReachableResult
    {
        public HashSet<GridSystem.Node> set;
        public Dictionary<GridSystem.Node, GridSystem.Node> parent;
        public Dictionary<GridSystem.Node, float> cost;
    }

    public ReachableResult ReachableWithParents(GridSystem.Node start, int mov, MoveKind kind, bool[,] occupied)
    {
        var res = new ReachableResult
        {
            set = new HashSet<GridSystem.Node>(),
            parent = new Dictionary<GridSystem.Node, GridSystem.Node>(),
            cost = new Dictionary<GridSystem.Node, float>()
        };

        var open = new List<GridSystem.Node> { start };
        res.cost[start] = 0f;

        while (open.Count > 0)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (res.cost[open[i]] < res.cost[open[best]]) best = i;
            var current = open[best]; open.RemoveAt(best);

            foreach (var next in grid.GetNeighbors(current))
            {
                if (next.blocked) continue;
                if (occupied != null && occupied[next.x, next.z]) continue;

                float step = Mathf.Max(0.0001f, next.terrain.GetCost(kind));
                float newCost = res.cost[current] + step;

                if (newCost <= mov && (!res.cost.ContainsKey(next) || newCost < res.cost[next]))
                {
                    res.cost[next] = newCost;
                    res.parent[next] = current;
                    if (!res.set.Contains(next)) res.set.Add(next);
                    if (!open.Contains(next)) open.Add(next);
                }
            }
        }
        return res;
    }

    public List<GridSystem.Node> ReconstructPath(GridSystem.Node start, GridSystem.Node goal,
        Dictionary<GridSystem.Node, GridSystem.Node> parent)
    {
        var path = new List<GridSystem.Node>();
        var cur = goal;
        if (cur == start) { path.Add(start); return path; }

        while (parent.ContainsKey(cur))
        {
            path.Add(cur);
            cur = parent[cur];
            if (cur == start) { path.Add(start); break; }
        }
        path.Reverse();
        return path;
    }
}
