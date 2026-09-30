using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Joins each hole to its outer loop with a zero width seam, so a shape with holes
    /// becomes one path.
    /// </summary>
    internal static class SeamBuilder
    {
        private const float Epsilon = ProjectionTolerance.Epsilon;

        public static void CreateSeams(List<OutlineLoop> loops, TriangleGrid grid)
        {
            for (int i = 0; i < loops.Count; i++)
            {
                OutlineLoop hole = loops[i];

                if (!hole.IsHole)
                    continue;

                OutlineLoop outer = FindContainingOuter(hole, loops);

                if (outer == null)
                    continue;

                if (!FindBestSeam(hole, outer, loops, grid, out int holePoint, out int outerPoint))
                    continue;

                StitchHoleIntoOuter(hole, outer, holePoint, outerPoint);
            }

            loops.RemoveAll(x => x.IsHole);
        }

        private static OutlineLoop FindContainingOuter(OutlineLoop hole, List<OutlineLoop> loops)
        {
            OutlineLoop best = null;
            int bestDepth = -1;

            for (int i = 0; i < loops.Count; i++)
            {
                OutlineLoop candidate = loops[i];

                if (candidate.IsHole)
                    continue;

                if (!Geometry2D.PointInPolygon(hole.TestPoint, candidate.Points))
                    continue;

                if (candidate.Depth > bestDepth)
                {
                    bestDepth = candidate.Depth;
                    best = candidate;
                }
            }

            return best;
        }

        private static bool FindBestSeam(
            OutlineLoop hole,
            OutlineLoop outer,
            List<OutlineLoop> loops,
            TriangleGrid grid,
            out int holeIndex,
            out int outerIndex)
        {
            holeIndex = -1;
            outerIndex = -1;

            int holeCount = hole.Points.Count;
            int outerCount = outer.Points.Count;

            var distances = new float[holeCount * outerCount];
            var pairs = new int[distances.Length];

            int n = 0;

            for (int h = 0; h < holeCount; h++)
            {
                for (int o = 0; o < outerCount; o++)
                {
                    distances[n] = (hole.Points[h] - outer.Points[o]).sqrMagnitude;
                    pairs[n] = h * outerCount + o;
                    n++;
                }
            }

            Array.Sort(distances, pairs);

            for (int i = 0; i < pairs.Length; i++)
            {
                int h = pairs[i] / outerCount;
                int o = pairs[i] % outerCount;

                if (!IsValidSeam(hole.Points[h], outer.Points[o], loops, grid))
                    continue;

                holeIndex = h;
                outerIndex = o;
                return true;
            }

            return false;
        }

        private static bool IsValidSeam(
            Vector2 hole,
            Vector2 outer,
            List<OutlineLoop> loops,
            TriangleGrid grid)
        {
            if ((outer - hole).magnitude < Epsilon)
                return false;

            // The seam must run through filled space.
            if (!grid.IsInside((hole + outer) * 0.5f))
                return false;

            foreach (OutlineLoop loop in loops)
            {
                List<Vector2> points = loop.Points;

                for (int i = 0; i < points.Count; i++)
                {
                    if (Geometry2D.SegmentsProperlyIntersect(
                            hole, outer, points[i], points[(i + 1) % points.Count], Epsilon))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void StitchHoleIntoOuter(
            OutlineLoop hole,
            OutlineLoop outer,
            int holeIndex,
            int outerIndex)
        {
            List<Vector2> originalOuter = outer.Points;
            List<Vector2> originalHole = hole.Points;

            var result = new List<Vector2>(originalOuter.Count + originalHole.Count + 2);

            for (int i = 0; i <= outerIndex; i++)
                result.Add(originalOuter[i]);

            for (int i = 0; i < originalHole.Count; i++)
                result.Add(originalHole[(holeIndex + i) % originalHole.Count]);

            result.Add(originalHole[holeIndex]);
            result.Add(originalOuter[outerIndex]);

            for (int i = outerIndex + 1; i < originalOuter.Count; i++)
                result.Add(originalOuter[i]);

            outer.Points = result;
        }
    }
}
