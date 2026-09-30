using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Turns a a collection of unordered 2D triangles into the outline of its silhouette as multiple closed paths.
    /// </summary>
    public static class SilhouetteTracer
    {
        private const float Epsilon = ProjectionTolerance.Epsilon;

        /// <summary>
        /// Returns the outline of the triangles with one closed path per island.
        /// </summary>
        /// <exception cref="ArgumentNullException">The points or indices are null.</exception>
        public static Vector2[][] Trace(Vector2[] points, int[] indices, bool createSeams)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            if (!NormalizedSpace.TryCreate(points, out NormalizedSpace space))
                return Array.Empty<Vector2[]>();

            var nodes = new PointMerger(Epsilon);
            var triangles = new List<ProjectedTriangle>(indices.Length / 3);
            var edges = new OutlineEdgeSet();

            CollectTriangles(points, indices, space, nodes, triangles, edges);

            if (triangles.Count == 0)
                return Array.Empty<Vector2[]>();

            var grid = new TriangleGrid(triangles);

            List<OutlineEdge> candidates = edges.CollectOutlineCandidates();

            EdgeSplitter.SplitAtIntersections(candidates, nodes);

            List<BoundarySegment> boundary = BoundaryExtractor.Extract(candidates, nodes.Points, grid);

            if (boundary.Count == 0)
                return Array.Empty<Vector2[]>();

            List<OutlineLoop> loops = LoopTracer.Trace(boundary, nodes.Points);

            if (loops.Count == 0)
                return Array.Empty<Vector2[]>();

            LoopClassifier.Classify(loops);

            if (createSeams)
                SeamBuilder.CreateSeams(loops, grid);

            return RestorePaths(loops, space);
        }

        private static void CollectTriangles(
            Vector2[] points,
            int[] indices,
            NormalizedSpace space,
            PointMerger nodes,
            List<ProjectedTriangle> triangles,
            OutlineEdgeSet edges)
        {
            var nodeOfVertex = new int[points.Length];

            for (int i = 0; i < nodeOfVertex.Length; i++)
                nodeOfVertex[i] = -1;

            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                int na = VertexNode(indices[i], points, nodeOfVertex, space, nodes);
                int nb = VertexNode(indices[i + 1], points, nodeOfVertex, space, nodes);
                int nc = VertexNode(indices[i + 2], points, nodeOfVertex, space, nodes);

                if (na == nb || nb == nc || nc == na)
                    continue;

                Vector2 a = nodes.Points[na];
                Vector2 b = nodes.Points[nb];
                Vector2 c = nodes.Points[nc];

                float cross = Geometry2D.Cross(b - a, c - a);
                float longest = Mathf.Sqrt(Mathf.Max(
                    (b - a).sqrMagnitude,
                    Mathf.Max((c - b).sqrMagnitude, (a - c).sqrMagnitude)));

                if (Mathf.Abs(cross) < 2f * Epsilon * longest)
                    continue;

                bool counterClockwise = cross > 0f;

                triangles.Add(counterClockwise
                    ? new ProjectedTriangle(a, b, c)
                    : new ProjectedTriangle(a, c, b));

                edges.AddCoverage(na, nb, counterClockwise);
                edges.AddCoverage(nb, nc, counterClockwise);
                edges.AddCoverage(nc, na, counterClockwise);
            }
        }

        private static int VertexNode(
            int vertex,
            Vector2[] points,
            int[] cache,
            NormalizedSpace space,
            PointMerger nodes)
        {
            int node = cache[vertex];

            if (node < 0)
            {
                node = nodes.GetOrAdd(space.Apply(points[vertex]));
                cache[vertex] = node;
            }

            return node;
        }

        private static Vector2[][] RestorePaths(List<OutlineLoop> loops, NormalizedSpace space)
        {
            var result = new List<Vector2[]>();

            foreach (OutlineLoop loop in loops)
            {
                if (loop.IsHole)
                    continue;

                var path = new Vector2[loop.Points.Count];

                for (int i = 0; i < path.Length; i++)
                    path[i] = space.Restore(loop.Points[i]);

                result.Add(path);
            }

            return result.ToArray();
        }
    }
}
