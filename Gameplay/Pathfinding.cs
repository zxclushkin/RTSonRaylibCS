using TinyRts.World;

namespace TinyRts.Gameplay;

public sealed class Pathfinding
{
    public List<TileCoord> FindPath(MapGrid map, TileCoord start, TileCoord goal)
    {
        if (!map.IsInside(start) || !map.IsInside(goal)) return [];
        if (start == goal) return [start];

        goal = map.FindNearestWalkable(goal);

        var open = new PriorityQueue<TileCoord, float>();
        var cameFrom = new Dictionary<TileCoord, TileCoord>();
        var costSoFar = new Dictionary<TileCoord, float> { [start] = 0 };
        var closed = new HashSet<TileCoord>();

        open.Enqueue(start, 0);

        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (!closed.Add(current)) continue;
            if (current == goal) return ReconstructPath(cameFrom, start, goal);

            foreach (var next in map.GetNeighbors(current))
            {
                if (!map.CanWalk(next) && next != goal) continue;
                if (IsDiagonal(current, next) && BlocksDiagonal(map, current, next)) continue;

                var stepCost = IsDiagonal(current, next) ? 1.4142f : 1.0f;
                var newCost = costSoFar[current] + stepCost;
                if (costSoFar.TryGetValue(next, out var oldCost) && newCost >= oldCost) continue;

                costSoFar[next] = newCost;
                cameFrom[next] = current;
                open.Enqueue(next, newCost + Heuristic(next, goal));
            }
        }

        return [];
    }

    static List<TileCoord> ReconstructPath(Dictionary<TileCoord, TileCoord> cameFrom, TileCoord start, TileCoord goal)
    {
        var path = new List<TileCoord> { goal };
        var current = goal;
        while (current != start && cameFrom.TryGetValue(current, out var previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    static float Heuristic(TileCoord a, TileCoord b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dy = Math.Abs(a.Y - b.Y);
        return Math.Max(dx, dy);
    }

    static bool IsDiagonal(TileCoord a, TileCoord b) => a.X != b.X && a.Y != b.Y;

    static bool BlocksDiagonal(MapGrid map, TileCoord from, TileCoord to)
    {
        return !map.CanWalk(new TileCoord(from.X, to.Y)) || !map.CanWalk(new TileCoord(to.X, from.Y));
    }
}
