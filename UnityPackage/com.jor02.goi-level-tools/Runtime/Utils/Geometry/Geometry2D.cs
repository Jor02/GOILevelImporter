using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// 2D geometry helpers for vectors, segments and polygons.
    /// </summary>
    public static class Geometry2D
    {
        /// <summary>
        /// Returns the z component of the 3D cross product of two 2D vectors.
        /// </summary>
        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        /// <summary>
        /// Returns the polygon area, positive when the points wind counter-clockwise.
        /// </summary>
        public static float SignedArea(List<Vector2> polygon)
        {
            float area = 0f;

            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];

                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        /// <summary>
        /// Even-odd containment test. A point exactly on the boundary can land on either side.
        /// </summary>
        public static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];

                bool intersects =
                    ((a.y > point.y) != (b.y > point.y)) &&
                    (point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x);

                if (intersects)
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// Returns the signed distance from p to the line through a and b, positive on the left.
        /// </summary>
        /// <remarks>
        /// Returns 0 when a and b are the same point.
        /// </remarks>
        public static float SideOf(Vector2 a, Vector2 b, Vector2 p)
        {
            float length = (b - a).magnitude;

            if (length <= 0f)
                return 0f;

            return Cross(b - a, p - a) / length;
        }

        /// <summary>
        /// True when segments ab and cd cross at a single point that is not an end of either one.
        /// </summary>
        /// <remarks>
        /// An end that sits within the tolerance of the other segment's line counts as touching,
        /// not crossing.
        /// </remarks>
        public static bool SegmentsProperlyIntersect(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            float tolerance)
        {
            float abC = SideOf(a, b, c);
            float abD = SideOf(a, b, d);
            float cdA = SideOf(c, d, a);
            float cdB = SideOf(c, d, b);

            return
                ((abC > tolerance && abD < -tolerance) || (abC < -tolerance && abD > tolerance)) &&
                ((cdA > tolerance && cdB < -tolerance) || (cdA < -tolerance && cdB > tolerance));
        }

        /// <summary>
        /// True when the points are closer together than the tolerance.
        /// </summary>
        public static bool Near(Vector2 a, Vector2 b, float tolerance)
        {
            return (a - b).sqrMagnitude < tolerance * tolerance;
        }

        /// <summary>
        /// Returns the unit vector pointing from one point to another. The points must differ.
        /// </summary>
        public static Vector2 DirectionBetween(Vector2 from, Vector2 to)
        {
            // Vector2.normalized returns zero under 1e-5 long, and short segments
            // (for example in a normalized 1 x 1 space) can be shorter than that.
            Vector2 delta = to - from;
            return delta / delta.magnitude;
        }
    }
}
