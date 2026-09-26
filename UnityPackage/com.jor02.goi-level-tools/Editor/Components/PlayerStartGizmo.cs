using UnityEditor;
using UnityEngine;
using GOILevelImporter.Components;

namespace GOILevelImporter.Editor
{
    public class PlayerStartGizmo
    {
        private static GUIStyle cachedLabelStyle;
        private static readonly GUIContent LabelContent = new GUIContent("Spawn");
        private static Vector2 cachedLabelSize;

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawGizmo(PlayerStart playerStart, GizmoType gizmoType)
        {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            if (sceneView == null || sceneView.camera == null) return;
            Camera camera = sceneView.camera;

            Vector3 labelPosition = playerStart.transform.position + Vector3.up * 1.6f;

            Vector3 viewportPos = camera.WorldToViewportPoint(labelPosition);
            if (viewportPos.z < 0) return;

            float distance = Vector3.Distance(camera.transform.position, labelPosition);
            const float fadeStart = 12f;
            const float fadeEnd = 25f;

            float alpha = 1f - Mathf.InverseLerp(fadeStart, fadeEnd, distance);
            alpha = Mathf.SmoothStep(0f, 1f, alpha);

            if (alpha > 0f)
            {
                EnsureStyleInitialized();

                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    Handles.BeginGUI();

                    Vector2 guiPoint = HandleUtility.WorldToGUIPoint(labelPosition);

                    const float referenceDistance = 5f;
                    float rawScale = referenceDistance / Mathf.Max(distance, 0.001f);
                    float worldScale = Mathf.Clamp(rawScale, 0.1f, 3.0f);

                    Matrix4x4 savedMatrix = GUI.matrix;
                    Color savedColor = GUI.color;

                    GUIUtility.ScaleAroundPivot(Vector2.one * worldScale, guiPoint);
                    GUI.color = new Color(1f, 1f, 1f, alpha);

                    Rect labelRect = new Rect(
                        guiPoint.x - (cachedLabelSize.x * 0.5f),
                        guiPoint.y - (cachedLabelSize.y * 0.5f),
                        cachedLabelSize.x,
                        cachedLabelSize.y
                    );

                    GUI.Label(labelRect, LabelContent, cachedLabelStyle);

                    GUI.color = savedColor;
                    GUI.matrix = savedMatrix;

                    Handles.EndGUI();
                }
            }
        }

        private static void EnsureStyleInitialized()
        {
            if (cachedLabelStyle != null) return;

            Texture2D badge = EditorGUIUtility.IconContent("sv_label_3").image as Texture2D;

            cachedLabelStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { background = badge, textColor = Color.white },
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(7, 7, 1, 1)
            };
            
            cachedLabelSize = cachedLabelStyle.CalcSize(LabelContent);
        }
    }
}