using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Hands out one id per location. Points closer than the tolerance share an id.
    /// </summary>
    public sealed class PointMerger
    {
        /// <summary>
        /// The first point seen for each id, indexed by id.
        /// </summary>
        public readonly List<Vector2> Points = new List<Vector2>();

        private readonly float tolerance;

        // Cell to the newest node in it. Older nodes in the same cell are chained through next.
        private readonly Dictionary<long, int> head = new Dictionary<long, int>();
        private readonly List<int> next = new List<int>();

        /// <param name="tolerance">
        /// Merging distance. Coordinates divided by it must fit in an int, so 1e-6 suits
        /// points within about 2000 units of the origin.
        /// </param>
        public PointMerger(float tolerance)
        {
            if (!(tolerance > 0f))
                throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be positive.");

            this.tolerance = tolerance;
        }

        public int GetOrAdd(Vector2 p)
        {
            int cx = Mathf.FloorToInt(p.x / tolerance);
            int cy = Mathf.FloorToInt(p.y / tolerance);

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!head.TryGetValue(CellKey(cx + dx, cy + dy), out int id))
                        continue;

                    while (id >= 0)
                    {
                        if ((Points[id] - p).sqrMagnitude <= tolerance * tolerance)
                            return id;

                        id = next[id];
                    }
                }
            }

            int created = Points.Count;
            long key = CellKey(cx, cy);

            Points.Add(p);
            next.Add(head.TryGetValue(key, out int previous) ? previous : -1);
            head[key] = created;

            return created;
        }

        private static long CellKey(int x, int y)
        {
            return ((long)x << 32) | (uint)y;
        }
    }
}
