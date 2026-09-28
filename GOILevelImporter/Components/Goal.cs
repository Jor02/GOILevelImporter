using UnityEngine;

namespace GOILevelImporter.Components
{
    [AddComponentMenu("GOI Level Importer/Goal")]
    public class Goal : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float radius = 1.5f;
    }
}