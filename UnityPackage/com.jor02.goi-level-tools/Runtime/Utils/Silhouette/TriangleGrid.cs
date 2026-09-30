using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Uniform grid over a triangle list.
    /// </summary>
    internal sealed class TriangleGrid
    {
        private const float Epsilon = ProjectionTolerance.Epsilon;

        // Keeps the cell size above zero when every triangle sits on a single point.
        private const float MinCellSize = 1e-5f;

        private readonly Dictionary<Cell, List<int>> cells =
            new Dictionary<Cell, List<int>>();

        private readonly List<ProjectedTriangle> triangles;

        private readonly float cellSize;

        public TriangleGrid(List<ProjectedTriangle> triangles)
        {
            this.triangles = triangles;

            Aabb2D bounds = ComputeBounds(triangles);

            float width = bounds.Max.x - bounds.Min.x;
            float height = bounds.Max.y - bounds.Min.y;

            int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(triangles.Count)));

            cellSize = Mathf.Max(
                Mathf.Max(width, height) / count,
                MinCellSize);

            var pad = new Vector2(Epsilon, Epsilon);

            for (int i = 0; i < triangles.Count; i++)
            {
                ProjectedTriangle triangle = triangles[i];

                Cell min = ToCell(triangle.Min - pad);
                Cell max = ToCell(triangle.Max + pad);

                for (int y = min.Y; y <= max.Y; y++)
                {
                    for (int x = min.X; x <= max.X; x++)
                    {
                        Cell cell = new Cell(x, y);

                        if (!cells.TryGetValue(cell, out List<int> list))
                        {
                            list = new List<int>();
                            cells.Add(cell, list);
                        }

                        list.Add(i);
                    }
                }
            }
        }

        public bool IsInside(Vector2 point)
        {
            if (!cells.TryGetValue(ToCell(point), out List<int> candidates))
                return false;

            for (int i = 0; i < candidates.Count; i++)
            {
                ProjectedTriangle triangle = triangles[candidates[i]];

                if (point.x < triangle.Min.x - Epsilon ||
                    point.x > triangle.Max.x + Epsilon ||
                    point.y < triangle.Min.y - Epsilon ||
                    point.y > triangle.Max.y + Epsilon)
                {
                    continue;
                }

                if (triangle.Contains(point))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Combines SideCoverage over every triangle near the point.
        /// </summary>
        public int SideCoverage(Vector2 point, Vector2 direction)
        {
            if (!cells.TryGetValue(ToCell(point), out List<int> candidates))
                return 0;

            int mask = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                mask |= triangles[candidates[i]].SideCoverage(point, direction);

                if (mask == 3)
                    break;
            }

            return mask;
        }

        private static Aabb2D ComputeBounds(List<ProjectedTriangle> triangles)
        {
            Aabb2D bounds = Aabb2D.Empty;

            for (int i = 0; i < triangles.Count; i++)
                bounds = bounds.Encapsulate(new Aabb2D(triangles[i].Min, triangles[i].Max));

            return bounds;
        }

        private Cell ToCell(Vector2 point)
        {
            return new Cell(
                Mathf.FloorToInt(point.x / cellSize),
                Mathf.FloorToInt(point.y / cellSize));
        }

        private readonly struct Cell : IEquatable<Cell>
        {
            public readonly int X;
            public readonly int Y;

            public Cell(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(Cell other)
            {
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                return obj is Cell other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return X * 397 ^ Y;
                }
            }
        }
    }
}
