using UnityEngine;

namespace GOILevelImporter.Components
{
    [AddComponentMenu("GOI Level Importer/Goal")]
    public class Goal : MonoBehaviour
    {
#if UNITY_EDITOR
        private static readonly Color GizmoColor = new Color(1f, 0.85f, 0.1f, 0.35f);
        private const float DefaultRadius = 1.5f;

        [SerializeField, Min(0.1f)]
        [Tooltip("Radius drawn for the goal gizmo, in meters.")]
        private float radius = DefaultRadius;

        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        private void OnValidate()
        {
            if (radius < 0.1f)
            {
                radius = 0.1f;
            }
        }
#endif
    }
}
