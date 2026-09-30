using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Sorts loops into outer paths and holes, and fixes their winding to match.
    /// </summary>
    internal static class LoopClassifier
    {
        public static void Classify(List<OutlineLoop> loops)
        {
            var bounds = new Aabb2D[loops.Count];

            for (int i = 0; i < loops.Count; i++)
            {
                bounds[i] = Aabb2D.FromPoints(loops[i].Points);
                loops[i].TestPoint = FindTestPoint(loops[i].Points);
            }

            for (int i = 0; i < loops.Count; i++)
            {
                OutlineLoop loop = loops[i];

                int depth = 0;

                for (int j = 0; j < loops.Count; j++)
                {
                    if (i == j)
                        continue;

                    if (!bounds[j].Contains(loop.TestPoint))
                        continue;

                    if (Geometry2D.PointInPolygon(loop.TestPoint, loops[j].Points))
                        depth++;
                }

                loop.Depth = depth;

                // Odd containment depth means this is a hole.
                loop.IsHole = (depth & 1) != 0;

                // Outer loops wind counter-clockwise and holes clockwise.
                if ((Geometry2D.SignedArea(loop.Points) > 0f) == loop.IsHole)
                    loop.Points.Reverse();
            }
        }

        // Midpoint of the longest edge.
        private static Vector2 FindTestPoint(List<Vector2> points)
        {
            int best = 0;
            float bestLength = -1f;

            for (int i = 0; i < points.Count; i++)
            {
                float length = (points[(i + 1) % points.Count] - points[i]).sqrMagnitude;

                if (length > bestLength)
                {
                    bestLength = length;
                    best = i;
                }
            }

            return (points[best] + points[(best + 1) % points.Count]) * 0.5f;
        }
    }
}
