using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Chains boundary segments into closed loops.
    /// </summary>
    internal static class LoopTracer
    {
        public static List<OutlineLoop> Trace(List<BoundarySegment> segments, List<Vector2> points)
        {
            var used = new bool[segments.Count];
            var adjacency = new Dictionary<int, List<int>>();

            for (int i = 0; i < segments.Count; i++)
            {
                AddAdjacency(adjacency, segments[i].A, i);
                AddAdjacency(adjacency, segments[i].B, i);
            }

            var loops = new List<OutlineLoop>();

            for (int i = 0; i < segments.Count; i++)
            {
                if (used[i])
                    continue;

                List<int> ids = TraceOneLoop(i, segments, used, adjacency, points);

                if (ids == null || ids.Count < 3)
                    continue;

                var loop = new List<Vector2>(ids.Count);

                foreach (int id in ids)
                    loop.Add(points[id]);

                PolygonSimplifier.Simplify(loop, ProjectionTolerance.Epsilon);

                if (loop.Count >= 3 && Mathf.Abs(Geometry2D.SignedArea(loop)) > ProjectionTolerance.MinLoopArea)
                    loops.Add(new OutlineLoop(loop));
            }

            return loops;
        }

        private static void AddAdjacency(
            Dictionary<int, List<int>> adjacency,
            int node,
            int segmentIndex)
        {
            if (!adjacency.TryGetValue(node, out List<int> list))
            {
                list = new List<int>();
                adjacency.Add(node, list);
            }

            list.Add(segmentIndex);
        }

        private static List<int> TraceOneLoop(
            int startIndex,
            List<BoundarySegment> segments,
            bool[] used,
            Dictionary<int, List<int>> adjacency,
            List<Vector2> points)
        {
            BoundarySegment first = segments[startIndex];
            used[startIndex] = true;

            var ids = new List<int> { first.A };

            int current = first.B;
            Vector2 incoming = Geometry2D.DirectionBetween(points[first.A], points[current]);

            // Every step uses up one segment, so the walk can't be longer than this.
            int steps = segments.Count;

            while (steps-- > 0)
            {
                if (current == first.A)
                    return ids;

                ids.Add(current);

                if (!adjacency.TryGetValue(current, out List<int> candidates))
                    return null;

                int next = FindBestNextSegment(current, incoming, candidates, segments, used, points);

                if (next < 0)
                    return null;

                used[next] = true;

                int other = segments[next].A == current ? segments[next].B : segments[next].A;

                incoming = Geometry2D.DirectionBetween(points[current], points[other]);
                current = other;
            }

            return null;
        }

        private static int FindBestNextSegment(
            int current,
            Vector2 incoming,
            List<int> candidates,
            List<BoundarySegment> segments,
            bool[] used,
            List<Vector2> points)
        {
            int best = -1;
            float bestAngle = float.PositiveInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                int index = candidates[i];

                if (used[index])
                    continue;

                int other = segments[index].A == current ? segments[index].B : segments[index].A;
                Vector2 outgoing = Geometry2D.DirectionBetween(points[current], points[other]);

                float angle = Mathf.Atan2(
                    Geometry2D.Cross(incoming, outgoing),
                    Vector2.Dot(incoming, outgoing));

                if (angle < 0f)
                    angle += Mathf.PI * 2f;

                // Prefer the smallest left turn.
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = index;
                }
            }

            return best;
        }
    }
}
