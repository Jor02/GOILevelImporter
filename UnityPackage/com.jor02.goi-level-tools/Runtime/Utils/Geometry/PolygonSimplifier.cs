using System.Collections.Generic;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Cleans up closed polygons by removing points that add nothing to the shape.
    /// </summary>
    public static class PolygonSimplifier
    {
        /// <summary>
        /// Drops repeated points, collinear points and spikes from a closed polygon, in place.
        /// </summary>
        /// <remarks>
        /// The tolerance is a distance, and also bounds the sine of the turn angle at a point.
        /// </remarks>
        public static void Simplify(List<Vector2> polygon, float tolerance)
        {
            var result = new List<Vector2>(polygon.Count);

            foreach (Vector2 point in polygon)
            {
                while (result.Count >= 2 &&
                       IsRedundant(result[result.Count - 2], result[result.Count - 1], point, tolerance))
                {
                    result.RemoveAt(result.Count - 1);
                }

                if (result.Count == 0 || !Geometry2D.Near(result[result.Count - 1], point, tolerance))
                    result.Add(point);
            }

            // The start of the loop can be the redundant point, and removing it can expose
            // the next one, so keep going until both ends are clean.
            bool changed = true;

            while (changed && result.Count >= 3)
            {
                changed = false;

                if (IsRedundant(result[result.Count - 1], result[0], result[1], tolerance))
                {
                    result.RemoveAt(0);
                    changed = true;
                }
                else if (IsRedundant(result[result.Count - 2], result[result.Count - 1], result[0], tolerance))
                {
                    result.RemoveAt(result.Count - 1);
                    changed = true;
                }
            }

            polygon.Clear();
            polygon.AddRange(result);
        }

        private static bool IsRedundant(Vector2 previous, Vector2 current, Vector2 next, float tolerance)
        {
            Vector2 a = current - previous;
            Vector2 b = next - current;

            float lengthA = a.magnitude;
            float lengthB = b.magnitude;

            if (lengthA < tolerance || lengthB < tolerance)
                return true;

            // Sine of the turn angle.
            return Mathf.Abs(Geometry2D.Cross(a, b)) < tolerance * lengthA * lengthB;
        }
    }
}
