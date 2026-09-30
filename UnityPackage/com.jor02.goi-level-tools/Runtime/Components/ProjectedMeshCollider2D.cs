using GOILevelImporter;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GOILevelImporter.Components
{
    [ExecuteInEditMode]
    [AddComponentMenu("GOI Level Importer/Projected Mesh Collider 2D")]
    public class ProjectedMeshCollider2D : MonoBehaviour
    {
        private const string ColliderChildName = "Collider2D";
        private const double UpdateDelay = 0.3;
        private const float ZAxisDotThreshold = 0.999f;
        private const float AngleNoiseThreshold = 0.01f;
        private const float DeterminantEpsilon = 1e-12f;

        [Tooltip("Keep holes in the mesh as holes in the collider instead of filling them in.")]
        [SerializeField] private bool allowHoles = true;

        public bool AllowHoles
        {
            get { return allowHoles; }
            set { allowHoles = value; }
        }

        private Quaternion _lastRotation;
        private Vector3 _lastScale;

#if UNITY_EDITOR
        private bool _needsUpdate;
        private double _timeSinceLastChange;
#endif

        private void Reset()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            _lastRotation = transform.rotation;
            _lastScale = transform.lossyScale;
            RegenerateCollider();
#endif
        }

        private void OnEnable()
        {
            _lastRotation = transform.rotation;
            _lastScale = transform.lossyScale;

#if UNITY_EDITOR
            EditorApplication.update += HandleEditorUpdate;
            Undo.undoRedoPerformed += HandleUndoRedo;

            if (Application.isPlaying)
                return;

            PolygonCollider2D existing = FindCollider();
            if (existing == null || existing.pathCount == 0)
                RegenerateCollider();
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= HandleEditorUpdate;
            Undo.undoRedoPerformed -= HandleUndoRedo;
#endif
        }

        [ContextMenu("Regenerate Collider")]
        public void RegenerateNow()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            _needsUpdate = false;
            _lastRotation = transform.rotation;
            _lastScale = transform.lossyScale;
            RegenerateCollider();
#endif
        }

#if UNITY_EDITOR
        private void HandleEditorUpdate()
        {
            if (Application.isPlaying || this == null || transform == null)
                return;

            if (transform.lossyScale != _lastScale)
            {
                _lastScale = transform.lossyScale;
                _needsUpdate = true;
                _timeSinceLastChange = EditorApplication.timeSinceStartup;
            }

            if (transform.rotation != _lastRotation)
            {
                Quaternion before = _lastRotation;
                _lastRotation = transform.rotation;

                // 2D colliders rotate around Z fine.
                if (IsZOnlySpin(before, transform.rotation))
                {
                    AlignColliderChild(GetOrCreateCollider());
                    return;
                }

                ClearColliderPaths();
                _needsUpdate = true;
                _timeSinceLastChange = EditorApplication.timeSinceStartup;
            }

            if (_needsUpdate && EditorApplication.timeSinceStartup - _timeSinceLastChange > UpdateDelay)
            {
                _needsUpdate = false;
                RegenerateCollider();
            }
        }

        private static bool IsZOnlySpin(Quaternion before, Quaternion after)
        {
            Quaternion delta = after * Quaternion.Inverse(before);
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle < AngleNoiseThreshold || angle > 360f - AngleNoiseThreshold)
                return true;

            return Mathf.Abs(Vector3.Dot(axis.normalized, Vector3.forward)) >= ZAxisDotThreshold;
        }

        private void RegenerateCollider()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            PolygonCollider2D collider = GetOrCreateCollider();
            if (collider == null)
                return;

            AlignColliderChild(collider);

            Quaternion twist = ExtractZTwist(transform.rotation);
            SetChildToTwist(collider.transform, twist);

            if (!TryBuildMeshToPlane(collider.transform, out Matrix4x4 meshToPlane))
                return;

            Vector2[][] paths;
            try
            {
                paths = MeshProjectionCollider.GenerateProjectedPaths(meshFilter.sharedMesh, meshToPlane, allowHoles);
            }
            catch (System.InvalidOperationException e)
            {
                Debug.LogWarning("Could not project '" + meshFilter.sharedMesh.name + "': " + e.Message, this);
                return;
            }

            collider.pathCount = paths.Length;
            for (int i = 0; i < paths.Length; i++)
                collider.SetPath(i, paths[i]);

            EditorUtility.SetDirty(collider);
        }

        private void HandleUndoRedo()
        {
            if (Application.isPlaying || this == null || transform == null)
                return;

            Transform child = transform.Find(ColliderChildName);
            if (child == null)
            {
                _lastRotation = transform.rotation;
                _lastScale = transform.lossyScale;
                _needsUpdate = false;
                return;
            }

            AlignColliderChild(child.GetComponent<PolygonCollider2D>());

            _lastRotation = transform.rotation;
            _lastScale = transform.lossyScale;
            _needsUpdate = false;
            RegenerateCollider();
        }

        private void ClearColliderPaths()
        {
            PolygonCollider2D collider = FindCollider();
            if (collider == null || collider.pathCount == 0)
                return;

            collider.pathCount = 0;
            EditorUtility.SetDirty(collider);
        }

        private PolygonCollider2D FindCollider()
        {
            Transform child = transform.Find(ColliderChildName);
            if (child == null)
                return null;

            return child.GetComponent<PolygonCollider2D>();
        }

        private PolygonCollider2D GetOrCreateCollider()
        {
            PolygonCollider2D collider = FindCollider();
            if (collider != null)
                return collider;

            var childObject = new GameObject(ColliderChildName);
            Undo.RegisterCreatedObjectUndo(childObject, "Create Projected Collider");
            childObject.transform.SetParent(transform, false);

            collider = Undo.AddComponent<PolygonCollider2D>(childObject);
            AlignColliderChild(collider);
            return collider;
        }

        private void AlignColliderChild(PolygonCollider2D collider)
        {
            if (collider == null)
                return;

            SetChildToTwist(collider.transform, ExtractZTwist(transform.rotation));
        }

        private static Quaternion ExtractZTwist(Quaternion rotation)
        {
            Vector3 twistAxis = rotation * Vector3.forward;
            Quaternion swing = Quaternion.FromToRotation(Vector3.forward, twistAxis);

            Vector3 swingAxis = Vector3.Cross(Vector3.forward, twistAxis);
            if (swingAxis.sqrMagnitude > 1e-12f)
                swing = Quaternion.AngleAxis(Vector3.Angle(Vector3.forward, twistAxis), swingAxis.normalized);

            return Quaternion.Inverse(swing) * rotation;
        }

        private bool TryBuildMeshToPlane(Transform child, out Matrix4x4 meshToPlane)
        {
            meshToPlane = Matrix4x4.identity;

            Matrix4x4 childWorld = child.localToWorldMatrix;

            float a = childWorld.m00;
            float b = childWorld.m01;
            float c = childWorld.m10;
            float d = childWorld.m11;

            float det = a * d - b * c;
            if (Mathf.Abs(det) < DeterminantEpsilon)
                return false;

            float invA = d / det;
            float invB = -b / det;
            float invC = -c / det;
            float invD = a / det;

            float tx = childWorld.m03;
            float ty = childWorld.m13;

            Matrix4x4 worldToPlane = Matrix4x4.identity;
            worldToPlane.m00 = invA;
            worldToPlane.m01 = invB;
            worldToPlane.m10 = invC;
            worldToPlane.m11 = invD;
            worldToPlane.m03 = -(invA * tx + invB * ty);
            worldToPlane.m13 = -(invC * tx + invD * ty);

            meshToPlane = worldToPlane * transform.localToWorldMatrix;
            return true;
        }

        private void SetChildToTwist(Transform child, Quaternion twist)
        {
            child.position = transform.position;
            child.rotation = twist;
            child.localScale = Vector3.one;
        }
#endif
    }
}