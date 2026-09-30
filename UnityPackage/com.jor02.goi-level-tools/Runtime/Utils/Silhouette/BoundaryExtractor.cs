using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Keeps the pieces of split edges that sit on the silhouette.
    /// </summary>
    internal static class BoundaryExtractor
    {
        public static List<BoundarySegment> Extract(
            List<OutlineEdge> edges,
            List<Vector2> points,
            TriangleGrid grid)
        {
            var seen = new HashSet<long>();
            var boundary = new List<BoundarySegment>();

            foreach (OutlineEdge edge in edges)
            {
                List<int> chain = BuildChain(edge, points);

                for (int i = 0; i < chain.Count - 1; i++)
                {
                    int a = chain[i];
                    int b = chain[i + 1];

                    if (a == b || !seen.Add(OutlineEdge.PairKey(a, b)))
                        continue;

                    if (SeparatesFilledFromEmpty(points[a], points[b], grid))
                        boundary.Add(new BoundarySegment(a, b));
                }
            }

            return boundary;
        }

        private static List<int> BuildChain(OutlineEdge edge, List<Vector2> points)
        {
            var chain = new List<int> { edge.A };

            if (edge.SplitNodes != null)
            {
                Vector2 start = points[edge.A];
                Vector2 r = points[edge.B] - start;

                int count = edge.SplitNodes.Count;
                var ids = edge.SplitNodes.ToArray();
                var keys = new float[count];

                for (int i = 0; i < count; i++)
                    keys[i] = Vector2.Dot(points[ids[i]] - start, r);

                Array.Sort(keys, ids);

                for (int i = 0; i < count; i++)
                {
                    if (ids[i] != chain[chain.Count - 1])
                        chain.Add(ids[i]);
                }
            }

            if (edge.B != chain[chain.Count - 1])
                chain.Add(edge.B);

            return chain;
        }

        private static bool SeparatesFilledFromEmpty(Vector2 a, Vector2 b, TriangleGrid grid)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;

            if (length <= 0f)
                return false;

            int coverage = grid.SideCoverage((a + b) * 0.5f, delta / length);

            return coverage == 1 || coverage == 2;
        }
    }
}
