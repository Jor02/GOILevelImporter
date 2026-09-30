using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// A 2D coordinate frame on the plane that faces a view direction.
    /// </summary>
    public readonly struct ProjectionPlane
    {
        /// <summary>
        /// Squared length of cross(up, direction) under which direction counts as parallel to up.
        /// Also the cutoff below which a direction counts as zero.
        /// </summary>
        public const float ParallelSqrTolerance = 1e-6f;

        public readonly Vector3 Right;
        public readonly Vector3 Up;

        public ProjectionPlane(Vector3 right, Vector3 up)
        {
            Right = right;
            Up = up;
        }

        /// <summary>
        /// Builds a frame looking along a direction, with Up as close to world up as possible.
        /// </summary>
        /// <remarks>
        /// The direction must already be normalized. When it is parallel to world up, world
        /// right picks the frame instead.
        /// </remarks>
        public static ProjectionPlane FromDirection(Vector3 direction)
        {
            Vector3 right = Vector3.Cross(Vector3.up, direction);

            if (right.sqrMagnitude < ParallelSqrTolerance)
                right = Vector3.Cross(Vector3.right, direction);

            right.Normalize();

            Vector3 up = Vector3.Cross(direction, right).normalized;

            return new ProjectionPlane(right, up);
        }

        public Vector2 Project(Vector3 p)
        {
            return new Vector2(
                Vector3.Dot(p, Right),
                Vector3.Dot(p, Up));
        }
    }
}
