using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Maps a point set into a space where it fits in a 1 x 1 box centered on the origin.
    /// </summary>
    public readonly struct NormalizedSpace
    {
        private readonly Vector2 center;
        private readonly float extent;

        private NormalizedSpace(Vector2 center, float extent)
        {
            this.center = center;
            this.extent = extent;
        }

        /// <summary>
        /// Builds the mapping for a point set. Fails for an empty set, a set with zero size
        /// on both axes, or one with NaN or infinite coordinates.
        /// </summary>
        public static bool TryCreate(Vector2[] points, out NormalizedSpace space)
        {
            space = default(NormalizedSpace);

            if (points.Length == 0)
                return false;

            Aabb2D bounds = Aabb2D.FromPoints(points);

            float extent = Mathf.Max(bounds.Max.x - bounds.Min.x, bounds.Max.y - bounds.Min.y);

            // Also rejects NaN, since every comparison with NaN is false.
            if (!(extent > 0f) || float.IsInfinity(extent))
                return false;

            space = new NormalizedSpace((bounds.Min + bounds.Max) * 0.5f, extent);
            return true;
        }

        public Vector2 Apply(Vector2 p)
        {
            return (p - center) / extent;
        }

        public Vector2 Restore(Vector2 p)
        {
            return p * extent + center;
        }
    }
}
