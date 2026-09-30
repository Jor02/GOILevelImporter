using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Axis-aligned 2D bounding box.
    /// </summary>
    public readonly struct Aabb2D
    {
        public readonly Vector2 Min;
        public readonly Vector2 Max;

        public Aabb2D(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// A box that contains nothing. Encapsulating any point into it gives a box around that point.
        /// </summary>
        public static Aabb2D Empty
        {
            get
            {
                return new Aabb2D(
                    new Vector2(float.PositiveInfinity, float.PositiveInfinity),
                    new Vector2(float.NegativeInfinity, float.NegativeInfinity));
            }
        }

        /// <summary>
        /// Returns the tightest box around the points, or Empty when there are none.
        /// </summary>
        public static Aabb2D FromPoints(IReadOnlyList<Vector2> points)
        {
            if (points.Count == 0)
                return Empty;

            Vector2 min = points[0];
            Vector2 max = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return new Aabb2D(min, max);
        }

        public Aabb2D Encapsulate(Aabb2D other)
        {
            return new Aabb2D(
                Vector2.Min(Min, other.Min),
                Vector2.Max(Max, other.Max));
        }

        /// <summary>
        /// True when the point is inside the box or on its edge.
        /// </summary>
        public bool Contains(Vector2 point)
        {
            return point.x >= Min.x &&
                   point.x <= Max.x &&
                   point.y >= Min.y &&
                   point.y <= Max.y;
        }

        /// <summary>
        /// True when the boxes overlap or come within the tolerance of each other.
        /// </summary>
        public bool Overlaps(Aabb2D other, float tolerance)
        {
            return
                Max.x >= other.Min.x - tolerance &&
                Min.x <= other.Max.x + tolerance &&
                Max.y >= other.Min.y - tolerance &&
                Min.y <= other.Max.y + tolerance;
        }
    }
}
