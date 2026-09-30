using UnityEditor;
using UnityEngine;
using GOILevelImporter.Components;

namespace GOILevelImporter.Editor
{
    [CustomEditor(typeof(CustomGroundCol))]
    public class CustomGroundColEditor : UnityEditor.Editor
    {
        private static readonly string[] BuiltInMaterials =
        {
            "rock",
            "wood",
            "metal",
            "plastic",
            "furniture",
            "snow",
            "cardboard",
            "none",
            "snake",
            "solidmetal"
        };

        private SerializedProperty groundColProp;
        private SerializedProperty materialProp;

        private void OnEnable()
        {
            groundColProp = serializedObject.FindProperty("groundCol");
            materialProp = serializedObject.FindProperty("material");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var groundCol = (CustomGroundCol)target;
            if (groundCol.GetComponent<Collider2D>() == null)
            {
                EditorGUILayout.HelpBox(
                    "No Collider2D on this GameObject. The game looks up GroundCol on the exact collider hit, so put this on the same object as its Collider2D.",
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(groundColProp);
            DrawMaterialDropdown();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMaterialDropdown()
        {
            var values = new System.Collections.Generic.List<string>(BuildMaterialValues());

            string saved = materialProp.stringValue;
            if (!string.IsNullOrEmpty(saved) && !values.Contains(saved))
            {
                values.Add(saved);
            }

            string[] labels = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                labels[i] = ToDisplayLabel(values[i]);
            }

            int currentIndex = string.IsNullOrEmpty(saved) ? 0 : values.IndexOf(saved);

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup(
                new GUIContent("Material", "Which hit, hard hit, and scrape sounds play here. Built-in names use the game's sounds, custom names come from the Custom Material Provider in the scene. Empty falls back to Rock."),
                currentIndex,
                labels);
            if (EditorGUI.EndChangeCheck())
            {
                materialProp.stringValue = values[picked];
            }
        }

        private static string[] BuildMaterialValues()
        {
            var values = new System.Collections.Generic.List<string> { string.Empty };
            values.AddRange(BuiltInMaterials);

            var provider = Object.FindObjectOfType<CustomMaterialProvider>();
            if (provider != null && provider.hitSounds != null)
            {
                foreach (var sound in provider.hitSounds)
                {
                    if (sound == null || string.IsNullOrEmpty(sound.name))
                    {
                        continue;
                    }

                    if (!values.Contains(sound.name))
                    {
                        values.Add(sound.name);
                    }
                }
            }

            return values.ToArray();
        }

        private static string ToDisplayLabel(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "(None)";
            }

            // Built-in sound names are lowercase enum values. Show them in
            // Pascal Case and split Solid Metal into two words.
            if (value == "solidmetal")
            {
                return "Solid Metal";
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
