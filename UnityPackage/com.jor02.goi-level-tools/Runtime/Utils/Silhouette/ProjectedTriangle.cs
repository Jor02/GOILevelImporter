using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// A counter-clockwise 2D triangle with precomputed edge lengths for fast side tests.
    /// </summary>
    internal readonly struct ProjectedTriangle
    {
        private const float Epsilon = ProjectionTolerance.Epsilon;

        // Sine of the angle under which an edge counts as parallel to a direction.
        private const float ParallelSineTolerance = 1e-3f;

        public readonly Vector2 A;
        public readonly Vector2 B;
        public readonly Vector2 C;

        public readonly Vector2 Min;
        public readonly Vector2 Max;

        private readonly float invLengthAB;
        private readonly float invLengthBC;
        private readonly float invLengthCA;

        /// <summary>
        /// The corners must wind counter-clockwise.
        /// </summary>
        public ProjectedTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;

            Min = Vector2.Min(a, Vector2.Min(b, c));
            Max = Vector2.Max(a, Vector2.Max(b, c));

            invLengthAB = 1f / (b - a).magnitude;
            invLengthBC = 1f / (c - b).magnitude;
            invLengthCA = 1f / (a - c).magnitude;
        }

        public bool Contains(Vector2 p)
        {
            return Geometry2D.Cross(B - A, p - A) * invLengthAB >= -Epsilon &&
                   Geometry2D.Cross(C - B, p - B) * invLengthBC >= -Epsilon &&
                   Geometry2D.Cross(A - C, p - C) * invLengthCA >= -Epsilon;
        }

        /// <summary>
        /// Tells which sides of a direction this triangle covers right at a point.
        /// </summary>
        /// <returns>
        /// Bit 0 for the left side, bit 1 for the right side. Zero when the point is outside.
        /// Both bits when it is inside, or on an edge that crosses the direction.
        /// </returns>
        public int SideCoverage(Vector2 p, Vector2 direction)
        {
            float d1 = Geometry2D.Cross(B - A, p - A) * invLengthAB;
            float d2 = Geometry2D.Cross(C - B, p - B) * invLengthBC;
            float d3 = Geometry2D.Cross(A - C, p - C) * invLengthCA;

            if (d1 < -Epsilon || d2 < -Epsilon || d3 < -Epsilon)
                return 0;

            int onEdge = (d1 <= Epsilon ? 1 : 0) + (d2 <= Epsilon ? 1 : 0) + (d3 <= Epsilon ? 1 : 0);

            // Strictly inside, or sitting on a corner where the answer is a wedge.
            if (onEdge != 1)
                return 3;

            Vector2 edge = d1 <= Epsilon ? (B - A) * invLengthAB
                         : d2 <= Epsilon ? (C - B) * invLengthBC
                         : (A - C) * invLengthCA;

            // The edge cuts across the direction, so both sides are covered near the point.
            if (Mathf.Abs(Geometry2D.Cross(direction, edge)) > ParallelSineTolerance)
                return 3;

            // Interior lies left of a counter-clockwise edge.
            return Vector2.Dot(direction, edge) > 0f ? 1 : 2;
        }
    }
}
