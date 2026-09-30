using System;
using UnityEngine;

namespace GOILevelImporter
{
    /// <summary>
    /// Turns a mesh into the 2D outline of its silhouette, as closed paths.
    /// </summary>
    public static class MeshProjectionCollider
    {
        /// <summary>
        /// Returns the outline of the mesh as seen along a direction, one closed path per island.
        /// </summary>
        /// <remarks>
        /// Coordinates are in mesh units, measured on a basis built from the direction.
        /// Outer paths wind counter-clockwise. With createSeams off, holes are dropped and each
        /// path is a filled silhouette. With it on, each hole is joined to its outer path by a
        /// zero width seam.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The mesh is null.</exception>
        /// <exception cref="ArgumentException">The direction is zero.</exception>
        /// <exception cref="InvalidOperationException">The mesh is not readable.</exception>
        public static Vector2[][] GenerateProjectedPaths(
            Mesh mesh,
            Vector3 direction,
            bool createSeams)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            if (direction.sqrMagnitude < ProjectionPlane.ParallelSqrTolerance)
                throw new ArgumentException("Direction must not be zero.", nameof(direction));

            direction.Normalize();

            ProjectionPlane basis = ProjectionPlane.FromDirection(direction);
            Vector3[] vertices = ReadVertices(mesh);

            var projected = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
                projected[i] = basis.Project(vertices[i]);

            return SilhouetteTracer.Trace(projected, mesh.triangles, createSeams);
        }

        /// <summary>
        /// Returns the outline of the mesh after moving it into a plane's space and dropping Z.
        /// </summary>
        /// <remarks>
        /// The matrix maps mesh space to the plane's space, where X and Y are the path
        /// coordinates and Z is depth. For a Collider2D, pass
        /// target.worldToLocalMatrix * meshTransform.localToWorldMatrix and the paths come
        /// back in the collider's local space. Otherwise same rules as the direction overload.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The mesh is null.</exception>
        /// <exception cref="InvalidOperationException">The mesh is not readable.</exception>
        public static Vector2[][] GenerateProjectedPaths(
            Mesh mesh,
            Matrix4x4 meshToPlane,
            bool createSeams)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            Vector3[] vertices = ReadVertices(mesh);

            var projected = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = meshToPlane.MultiplyPoint3x4(vertices[i]);
                projected[i] = new Vector2(p.x, p.y);
            }

            return SilhouetteTracer.Trace(projected, mesh.triangles, createSeams);
        }

        private static Vector3[] ReadVertices(Mesh mesh)
        {
            if (!mesh.isReadable)
            {
                throw new InvalidOperationException(
                    "Mesh '" + mesh.name + "' is not readable. Turn on Read/Write in its import settings.");
            }

            return mesh.vertices;
        }
    }
}
