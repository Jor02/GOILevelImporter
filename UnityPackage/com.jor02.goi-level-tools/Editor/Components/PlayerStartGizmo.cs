using UnityEditor;
using UnityEngine;
using GOILevelImporter.Components;

namespace GOILevelImporter.Editor
{
    public static class PlayerStartGizmo
    {
        private static GUIStyle cachedLabelStyle;
        private static readonly GUIContent LabelContent = new GUIContent("Spawn");
        private static int baseFontSize;
        private static int basePadLeft;
        private static int basePadRight;
        private static int basePadTop;
        private static int basePadBottom;

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

            float halfFovTan = Mathf.Tan(Mathf.Max(camera.fieldOfView, 1f) * 0.5f * Mathf.Deg2Rad);
            float effectiveDistance = camera.orthographic
                ? camera.orthographicSize / Mathf.Max(halfFovTan, 0.001f)
                : distance;

            const float fadeStart = 12f;
            const float fadeEnd = 25f;

            float alpha = 1f - Mathf.InverseLerp(fadeStart, fadeEnd, effectiveDistance);
            alpha = Mathf.SmoothStep(0f, 1f, alpha);

            if (alpha > 0f)
            {
                EnsureStyleInitialized();

                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    Handles.BeginGUI();

                    Vector2 guiPoint = HandleUtility.WorldToGUIPoint(labelPosition);

                    float rawScale;
                    const float referenceDistance = 5f;
                    if (camera.orthographic)
                    {
                        float referenceOrthoSize = referenceDistance * halfFovTan;
                        rawScale = referenceOrthoSize / Mathf.Max(camera.orthographicSize, 0.001f);
                    }
                    else
                    {
                        rawScale = referenceDistance / Mathf.Max(distance, 0.001f);
                    }

                    float worldScale = Mathf.Clamp(rawScale, 0.1f, 3.0f);

                    ApplyScale(worldScale);
                    Vector2 scaledSize = cachedLabelStyle.CalcSize(LabelContent);

                    Color savedColor = GUI.color;

                    GUI.color = new Color(1f, 1f, 1f, alpha);

                    Rect labelRect = new Rect(
                        guiPoint.x - (scaledSize.x * 0.5f),
                        guiPoint.y - (scaledSize.y * 0.5f),
                        scaledSize.x,
                        scaledSize.y
                    );

                    GUI.Label(labelRect, LabelContent, cachedLabelStyle);

                    GUI.color = savedColor;

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
                padding = new RectOffset(10, 10, 4, 4)
            };

            baseFontSize = cachedLabelStyle.fontSize;
            if (baseFontSize <= 0)
            {
                int skinSize = GUI.skin != null ? GUI.skin.label.fontSize : 0;
                baseFontSize = skinSize > 0 ? skinSize : 12;
            }
            RectOffset basePadding = cachedLabelStyle.padding;
            basePadLeft = basePadding.left;
            basePadRight = basePadding.right;
            basePadTop = basePadding.top;
            basePadBottom = basePadding.bottom;
        }

        private static void ApplyScale(float worldScale)
        {
            const float textScale = 0.8f;
            cachedLabelStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseFontSize * worldScale * textScale));
            RectOffset padding = cachedLabelStyle.padding;
            padding.left = Mathf.Max(6, Mathf.RoundToInt(basePadLeft * worldScale));
            padding.right = Mathf.Max(6, Mathf.RoundToInt(basePadRight * worldScale));
            padding.top = Mathf.Max(3, Mathf.RoundToInt(basePadTop * worldScale));
            padding.bottom = Mathf.Max(3, Mathf.RoundToInt(basePadBottom * worldScale));
        }
    }
}