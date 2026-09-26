using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

/// <summary>
/// Inspector for CustomLevelObject. Drawing lives here; validation and building live in
/// LevelValidation and LevelBuilder.
/// </summary>
[CustomEditor(typeof(CustomLevelObject))]
public class CustomLevelObjectEditor : Editor
{
    private const float AspectWidth = 718f;
    private const float AspectHeight = 400f;
    private const float PlaceholderPadding = 4f;
    private const float MetadataPadding = 10f;

    private int pickerControlID = -1;

    private bool showValidationErrors;

    private SerializedProperty levelNameProp;
    private SerializedProperty authorProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty levelScenesProp;
    private ReorderableList sceneList;

    private void OnEnable()
    {
        levelNameProp = serializedObject.FindProperty("LevelName");
        authorProp = serializedObject.FindProperty("Author");
        descriptionProp = serializedObject.FindProperty("Description");
        levelScenesProp = serializedObject.FindProperty("LevelScenes");

        sceneList = new ReorderableList(serializedObject, levelScenesProp, 
            draggable: true, 
            displayHeader: false,
            displayAddButton: true, 
            displayRemoveButton: true);

        sceneList.onAddCallback = (ReorderableList list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.objectReferenceValue = null;
        };

        sceneList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            const float iconWidth = 20f;
            const float iconPadding = 4f;

            SerializedProperty element = sceneList.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            var messages = LevelValidation.ValidateScene(element.objectReferenceValue as SceneAsset);
            var worst = LevelValidation.Message.Worst(messages);
            bool hasMessage = worst.HasValue;

            Rect fieldRect = rect;
            if (hasMessage)
            {
                fieldRect.width -= (iconWidth + iconPadding);
            }

            EditorGUI.PropertyField(fieldRect, element, GUIContent.none);

            if (hasMessage)
            {
                Rect iconRect = new Rect(
                    rect.xMax - iconWidth,
                    rect.y,
                    iconWidth,
                    EditorGUIUtility.singleLineHeight
                );

                GUIContent messageIcon = EditorGUIUtility.IconContent(LevelValidation.IconName(worst.Severity));
                messageIcon.tooltip = string.Join("\n", messages
                    .Where(m => m.HasValue)
                    .Select(LevelValidation.Describe));

                GUI.Label(iconRect, messageIcon);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space();
        DrawThumbnail();
        EditorGUILayout.Space();
        DrawMetadataBox();
        EditorGUILayout.Space(4);
        DrawBuildButton();
        EditorGUILayout.Space();
        
        DrawPropertiesExcluding(serializedObject, 
            "m_Script", 
            "Thumbnail", 
            "LevelName", 
            "Author", 
            "Description",
            "LevelScenes"
        );

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMetadataBox()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(MetadataPadding);
        EditorGUILayout.BeginVertical();

        GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField("Level Details", titleStyle);

        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));

        EditorGUILayout.Space(4);

        DrawPropertyWithPlaceholder(levelNameProp, "Level Name", "My Level", isRequired: true);
        DrawRequiredError(levelNameProp);
        DrawPropertyWithPlaceholder(authorProp, "Author", "John Doe");

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);

        descriptionProp.stringValue = EditorGUILayout.TextArea(
            descriptionProp.stringValue,
            EditorStyles.textArea,
            GUILayout.MinHeight(60)
        );

        if (string.IsNullOrEmpty(descriptionProp.stringValue))
        {
            Rect textAreaRect = GUILayoutUtility.GetLastRect();

            textAreaRect.x += PlaceholderPadding;
            textAreaRect.y += 2;

            GUIStyle placeholderStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperLeft
            };
            placeholderStyle.normal.textColor = Color.gray;
            
            GUI.Label(textAreaRect, "This is a level that is very good.", placeholderStyle);
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);

        sceneList.DoLayoutList();

        DrawAddOpenSceneButton();

        EditorGUILayout.Space(4);

        DrawLevelValidation();

        EditorGUILayout.EndVertical();
        GUILayout.Space(MetadataPadding);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUILayout.EndVertical();
    }

    private void DrawLevelValidation()
    {
        var level = target as CustomLevelObject;

        if (level == null || level.LevelScenes == null)
        {
            return;
        }

        foreach (var message in LevelValidation.Validate(level.LevelScenes))
        {
            if (!message.HasValue)
            {
                continue;
            }

            GUIContent icon = EditorGUIUtility.IconContent(LevelValidation.IconName(message.Severity));
            icon.tooltip = message.Text;

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = LevelValidation.TextColor(message.Severity) }
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            GUILayout.Label(message.Text, style);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawBuildButton()
    {
        var level = target as CustomLevelObject;

        using (new EditorGUI.DisabledScope(level == null || level.LevelScenes == null
                                           || level.LevelScenes.Count == 0))
        {
            if (GUILayout.Button("Build", GUILayout.Height(28)))
            {
                showValidationErrors = true;
                BuildLevel(level);
            }
        }
    }

    private void BuildLevel(CustomLevelObject level)
    {
        if (string.IsNullOrWhiteSpace(level.LevelName))
        {
            return;
        }

        var levelMessages = LevelValidation.Validate(level.LevelScenes);
        var errors = levelMessages.Where(m => m.Severity == LevelValidation.Severity.Error).ToList();
        var warnings = levelMessages.Where(m => m.Severity == LevelValidation.Severity.Warning).ToList();

        foreach (var warning in warnings)
        {
            Debug.LogWarning($"{level.LevelName}: {warning.Text}");
        }

        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Build Failed",
                string.Join("\n", errors.Select(e => e.Text)) + "\n\nFix the errors before building.",
                "OK");
            return;
        }

        showValidationErrors = false;

        string outputPath = EditorUtility.SaveFilePanel(
            "Build Level",
            LevelBuilder.GetDefaultBuildFolder(),
            LevelBuilder.GetDefaultFileName(level),
            LevelBuilder.LevelFileExtension);

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        if (!outputPath.EndsWith(LevelBuilder.LevelFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            outputPath += LevelBuilder.LevelFileExtension;
        }

        LevelBuilder.RememberBuildFolder(outputPath);

        LevelBuilder.Build(level, outputPath);
    }

    private void DrawAddOpenSceneButton()
    {
        Scene openScene = SceneManager.GetActiveScene();

        bool canAdd = openScene.IsValid() && openScene.isLoaded && !string.IsNullOrEmpty(openScene.path);

        using (new EditorGUI.DisabledScope(!canAdd))
        {
            string label = canAdd
                ? $"Add Open Scene ({openScene.name})"
                : "Add Open Scene";

            if (!GUILayout.Button(label))
            {
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(openScene.path);
            if (sceneAsset == null)
            {
                Debug.LogWarning($"Could not load the scene at '{openScene.path}' as an asset.");
                return;
            }

            Undo.RecordObject(target, "Add Open Scene");

            levelScenesProp.arraySize++;
            levelScenesProp.GetArrayElementAtIndex(levelScenesProp.arraySize - 1).objectReferenceValue = sceneAsset;
        }
    }

    private void DrawRequiredError(SerializedProperty prop)
    {
        if (!showValidationErrors || !string.IsNullOrWhiteSpace(prop.stringValue))
        {
            return;
        }

        GUIStyle errorStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.9f, 0.35f, 0.35f) }
        };

        // Indented to sit under the field rather than under the label column.
        Rect rect = EditorGUILayout.GetControlRect();
        rect.x += EditorGUIUtility.labelWidth + PlaceholderPadding;

        GUI.Label(rect, "A level name is required.", errorStyle);
    }

    private void DrawPropertyWithPlaceholder(SerializedProperty prop, string label, string placeholder, bool isRequired = false)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.PropertyField(prop, new GUIContent(label));
        
        // Grab the rect immediately after the PropertyField so the placeholder aligns correctly
        Rect fieldRect = GUILayoutUtility.GetLastRect();

        // If the field is marked required and is empty, append the icon
        if (isRequired && string.IsNullOrEmpty(prop.stringValue))
        {
            // Grab a standard Unity UI icon and attach the requested tooltip
            GUIContent requiredIcon = EditorGUIUtility.IconContent("console.erroricon.sml");
            requiredIcon.tooltip = "This field is required.";
            
            // Draw the icon inline to the right of the property field
            GUILayout.Label(requiredIcon, GUILayout.Width(20), GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        EditorGUILayout.EndHorizontal();

        if (string.IsNullOrEmpty(prop.stringValue))
        {
            fieldRect.x += EditorGUIUtility.labelWidth + PlaceholderPadding;
            fieldRect.width -= EditorGUIUtility.labelWidth + PlaceholderPadding;

            GUIStyle placeholderStyle = new GUIStyle(EditorStyles.label);
            placeholderStyle.normal.textColor = Color.gray;
            
            GUI.Label(fieldRect, placeholder, placeholderStyle);
        }
    }

    private void DrawThumbnail()
    {
        var level = (CustomLevelObject)target;
        
        float aspectRatio = AspectWidth / AspectHeight;
        var rect = GUILayoutUtility.GetAspectRect(aspectRatio);
        
        var evt = Event.current;
        bool isHovering = rect.Contains(evt.mousePosition);

        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

        if (level.Thumbnail != null)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            GUI.DrawTexture(rect, level.Thumbnail, ScaleMode.ScaleToFit, true);

            if (isHovering)
            {
                EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.1f));
            }

            Rect clearRect = new Rect(rect.xMax - 24, rect.y + 4, 20, 20);
            if (GUI.Button(clearRect, "X"))
            {
                Undo.RecordObject(level, "Clear Thumbnail");
                level.Thumbnail = null;
                EditorUtility.SetDirty(level);
                GUIUtility.ExitGUI();
            }
        }
        else
        {
            GUIStyle placeholderStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                wordWrap = true,
                richText = true
            };

            string labelText = isHovering 
                ? "<color=#ffffff><b>Click</b> or <b>Drag & Drop</b>\nto select thumbnail</color>" 
                : "<b>No Thumbnail</b>\nClick or Drag & Drop to select";

            GUI.Label(rect, labelText, placeholderStyle);
        }

        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

        if (evt.type == EventType.MouseDown && evt.button == 0 && isHovering)
        {
            pickerControlID = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<Texture2D>(level.Thumbnail, false, "", pickerControlID);
            evt.Use();
        }

        if (evt.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == pickerControlID)
        {
            Undo.RecordObject(level, "Set Thumbnail");
            level.Thumbnail = (Texture2D)EditorGUIUtility.GetObjectPickerObject();
            EditorUtility.SetDirty(level);
            evt.Use();
        }

        if (isHovering)
        {
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = IsDragValid() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform && IsDragValid())
            {
                DragAndDrop.AcceptDrag();
                Undo.RecordObject(level, "Set Thumbnail");
                level.Thumbnail = (Texture2D)DragAndDrop.objectReferences[0];
                EditorUtility.SetDirty(level);
                evt.Use();
            }
        }
    }

    private bool IsDragValid()
    {
        return DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Texture2D;
    }
}
