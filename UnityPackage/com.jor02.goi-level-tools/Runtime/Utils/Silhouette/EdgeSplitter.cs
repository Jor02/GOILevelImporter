using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Records a split node on each pair of edges that cross or touch.
    /// </summary>
    internal static class EdgeSplitter
    {
        private const float Epsilon = ProjectionTolerance.Epsilon;

        /// <summary>
        /// Finds every crossing with a sweep along X, adding crossing points to the node table.
        /// </summary>
        public static void SplitAtIntersections(List<OutlineEdge> edges, PointMerger nodes)
        {
            int count = edges.Count;

            var bounds = new Aabb2D[count];
            var order = new int[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 a = nodes.Points[edges[i].A];
                Vector2 b = nodes.Points[edges[i].B];

                bounds[i] = new Aabb2D(Vector2.Min(a, b), Vector2.Max(a, b));
                order[i] = i;
            }

            Array.Sort(order, (a, b) => bounds[a].Min.x.CompareTo(bounds[b].Min.x));

            var active = new List<int>();

            for (int rank = 0; rank < count; rank++)
            {
                int i = order[rank];

                for (int k = active.Count - 1; k >= 0; k--)
                {
                    if (bounds[active[k]].Max.x >= bounds[i].Min.x - Epsilon)
                        continue;

                    active[k] = active[active.Count - 1];
                    active.RemoveAt(active.Count - 1);
                }

                for (int k = 0; k < active.Count; k++)
                {
                    int j = active[k];

                    if (!bounds[i].Overlaps(bounds[j], Epsilon))
                        continue;

                    IntersectEdges(edges[i], edges[j], nodes);
                }

                active.Add(i);
            }
        }

        // Both edges get the same node for a crossing, so the pieces on each side meet
        // at exactly the same point.
        private static void IntersectEdges(OutlineEdge a, OutlineEdge b, PointMerger nodes)
        {
            Vector2 pa = nodes.Points[a.A];
            Vector2 pb = nodes.Points[a.B];
            Vector2 qa = nodes.Points[b.A];
            Vector2 qb = nodes.Points[b.B];

            double px = pa.x, py = pa.y;
            double rx = pb.x - px, ry = pb.y - py;
            double qx = qa.x, qy = qa.y;
            double sx = qb.x - qx, sy = qb.y - qy;

            double lengthR = Math.Sqrt(rx * rx + ry * ry);
            double lengthS = Math.Sqrt(sx * sx + sy * sy);

            if (lengthR <= 0.0 || lengthS <= 0.0)
                return;

            double qpx = qx - px;
            double qpy = qy - py;

            // Distance of both ends of b from the line through a.
            double distanceStart = Math.Abs(qpx * ry - qpy * rx) / lengthR;
            double distanceEnd = Math.Abs((qpx + sx) * ry - (qpy + sy) * rx) / lengthR;

            if (distanceStart <= Epsilon && distanceEnd <= Epsilon)
            {
                SplitIfInside(a, b.A, nodes.Points);
                SplitIfInside(a, b.B, nodes.Points);
                SplitIfInside(b, a.A, nodes.Points);
                SplitIfInside(b, a.B, nodes.Points);
                return;
            }

            double rxs = rx * sy - ry * sx;

            // Parallel but not on the same line.
            if (Math.Abs(rxs) < 1e-12 * lengthR * lengthS)
                return;

            double t = (qpx * sy - qpy * sx) / rxs;
            double u = (qpx * ry - qpy * rx) / rxs;

            double tolerance = Epsilon / lengthR;
            double toleranceU = Epsilon / lengthS;

            if (t < -tolerance || t > 1.0 + tolerance ||
                u < -toleranceU || u > 1.0 + toleranceU)
            {
                return;
            }

            t = Math.Min(1.0, Math.Max(0.0, t));

            var hit = new Vector2((float)(px + rx * t), (float)(py + ry * t));
            int node = nodes.GetOrAdd(hit);

            a.AddSplit(node);
            b.AddSplit(node);
        }

        private static void SplitIfInside(OutlineEdge edge, int node, List<Vector2> points)
        {
            if (node == edge.A || node == edge.B)
                return;

            Vector2 start = points[edge.A];
            Vector2 r = points[edge.B] - start;
            Vector2 p = points[node] - start;

            float length = r.magnitude;

            if (Mathf.Abs(Geometry2D.Cross(p, r)) / length > 2f * Epsilon)
                return;

            float t = Vector2.Dot(p, r) / (length * length);
            float tolerance = Epsilon / length;

            if (t > tolerance && t < 1f - tolerance)
                edge.AddSplit(node);
        }
    }
}
