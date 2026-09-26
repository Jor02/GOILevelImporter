using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GOILevelImporter.Components
{
    [AddComponentMenu("GOI Level Importer/Player Start")]
    public class PlayerStart : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string PlayerMeshGuid = "40236731e3b225443be9b229873fa091";
        
        private static readonly Color GizmoColor = new Color(0f, 1f, 0f, 0.35f);
        private static readonly Vector3 CustomScale = new Vector3(1f, 0.7613872f, 1f);

        private static Mesh cachedPlayerMesh;
        private static bool meshLookupDone;

        private void OnDrawGizmos()
        {
            Mesh playerMesh = GetPlayerMesh();
            if (playerMesh != null)
            {
                Gizmos.color = GizmoColor;
                
                Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, CustomScale);
                Gizmos.DrawMesh(playerMesh);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

        private static Mesh GetPlayerMesh()
        {
            if (meshLookupDone && cachedPlayerMesh != null)
            {
                return cachedPlayerMesh;
            }

            string path = AssetDatabase.GUIDToAssetPath(PlayerMeshGuid);
            if (!string.IsNullOrEmpty(path))
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Mesh mesh)
                    {
                        cachedPlayerMesh = mesh;
                        meshLookupDone = true;
                        break;
                    }
                }
            }

            return cachedPlayerMesh;
        }
#endif
    }
}