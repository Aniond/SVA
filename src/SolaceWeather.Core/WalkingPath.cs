namespace SolaceWeather.Core;

public readonly record struct WalkTile(int X, int Y);

public static class WalkingPath
{
    public static IReadOnlyList<WalkTile>? Find(WalkTile start, WalkTile goal, int width, int height,
        Func<WalkTile, bool> canWalk, int limit = 4096)
    {
        bool Inside(WalkTile p) => p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height;
        if (!Inside(start) || !Inside(goal) || limit <= 0 || !canWalk(goal)) return null;
        var open = new PriorityQueue<WalkTile, int>();
        var cost = new Dictionary<WalkTile, int> { [start] = 0 };
        var parent = new Dictionary<WalkTile, WalkTile>();
        var closed = new HashSet<WalkTile>();
        var passable = new Dictionary<WalkTile, bool> { [goal] = true };
        open.Enqueue(start, 0);
        while (open.Count > 0 && closed.Count < limit)
        {
            WalkTile current = open.Dequeue();
            if (!closed.Add(current)) continue;
            if (current == goal)
            {
                var path = new List<WalkTile> { current };
                while (parent.TryGetValue(current, out var previous)) { path.Add(previous); current = previous; }
                path.Reverse();
                return path;
            }
            foreach (WalkTile next in new WalkTile[]
                     { new(current.X, current.Y - 1), new(current.X + 1, current.Y), new(current.X, current.Y + 1), new(current.X - 1, current.Y) })
            {
                if (!Inside(next) || closed.Contains(next)) continue;
                if (!passable.TryGetValue(next, out bool free)) passable[next] = free = canWalk(next);
                if (!free) continue;
                int distance = cost[current] + 1;
                if (cost.TryGetValue(next, out int old) && old <= distance) continue;
                cost[next] = distance; parent[next] = current;
                open.Enqueue(next, distance + Math.Abs(goal.X - next.X) + Math.Abs(goal.Y - next.Y));
            }
        }
        return null;
    }
}
