using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GOILevelImporter.Components;

[CustomEditor(typeof(CustomLevelObject))]
public class CustomLevelObjectEditor : Editor
{
    /// <summary>
    /// How serious a validation message is. Errors block the build, warnings only nag.
    /// </summary>
    public enum ValidationSeverity
    {
        None,
        Warning,
        Error
    }

    public readonly struct ValidationMessage
    {
        public readonly ValidationSeverity Severity;
        public readonly string Message;

        private ValidationMessage(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public static ValidationMessage Error(string message) => new(ValidationSeverity.Error, message);
        public static ValidationMessage Warning(string message) => new(ValidationSeverity.Warning, message);

        public bool HasValue => Severity != ValidationSeverity.None && !string.IsNullOrEmpty(Message);

        public static ValidationMessage Worst(params ValidationMessage[] messages)
        {
            var worst = default(ValidationMessage);

            foreach (var message in messages)
            {
                if (message.Severity > worst.Severity)
                {
                    worst = message;
                }
            }

            return worst;
        }
    }

    /// <summary>
    /// How many instances of a marker a level may have, and how bad it is when it does not match.
    /// </summary>
    public readonly struct MarkerRule
    {
        public readonly Type Type;
        public readonly int Min;
        public readonly int Max;
        public readonly ValidationSeverity Severity;

        public MarkerRule(Type type, int min, int max, ValidationSeverity severity)
        {
            Type = type;
            Min = min;
            Max = max;
            Severity = severity;
        }
    }

    private const float AspectWidth = 718f;
    private const float AspectHeight = 400f;
    private const float PlaceholderPadding = 4f;
    private const float MetadataPadding = 10f;
    private const string LevelFileExtension = ".glf";
    private const string DefaultBuildFolderName = "LevelBuilds";
    private const string LastBuildFolderKey = "GOILevelTools.LastBuildFolder";
    
    private int pickerControlID = -1;

    private bool showValidationErrors;

    private SerializedProperty levelNameProp;
    private SerializedProperty authorProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty levelScenesProp;
    private ReorderableList sceneList;

    private static readonly Dictionary<string, (Hash128 Hash, ValidationMessage[] Messages)> SceneValidationCache = new();

    // Errors block the build, warnings do not. A level allows exactly one player start
    // and at least one goal, since most levels can have several goals.
    private static readonly MarkerRule[] MarkerRules =
    {
        new(typeof(PlayerStart), min: 1, max: 1, ValidationSeverity.Error),
        new(typeof(Goal), min: 1, max: int.MaxValue, ValidationSeverity.Warning)
    };

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

            var messages = GetSceneMessages(element);
            var worst = ValidationMessage.Worst(messages);
            bool hasMessage = worst.HasValue;

            Rect fieldRect = rect;
            if (hasMessage)
            {
                fieldRect.width -= (iconWidth + iconPadding);
            }

            // Draw the scene property field
            EditorGUI.PropertyField(fieldRect, element, GUIContent.none);

            if (hasMessage)
            {
                Rect iconRect = new Rect(
                    rect.xMax - iconWidth,
                    rect.y,
                    iconWidth,
                    EditorGUIUtility.singleLineHeight
                );

                string iconName = worst.Severity == ValidationSeverity.Error
                    ? "console.erroricon.sml"
                    : "console.warnicon.sml";

                GUIContent messageIcon = EditorGUIUtility.IconContent(iconName);
                messageIcon.tooltip = string.Join("\n", messages
                    .Where(m => m.HasValue)
                    .Select(m => $"{Prefix(m.Severity)}{m.Message}"));

                GUI.Label(iconRect, messageIcon);
            }
        };
    }

    private static string Prefix(ValidationSeverity severity) =>
        severity == ValidationSeverity.Error ? "Error: " : "Warning: ";

    private ValidationMessage[] GetSceneMessages(SerializedProperty element)
    {
        if (element.objectReferenceValue == null)
        {
            return new[] { ValidationMessage.Error("Scene reference is missing or unassigned.") };
        }

        var sceneAsset = element.objectReferenceValue as SceneAsset;
        string scenePath = AssetDatabase.GetAssetPath(sceneAsset);

        if (string.IsNullOrEmpty(scenePath))
        {
            return new[] { ValidationMessage.Error("Invalid scene path.") };
        }

        return GetOrValidateSceneContent(scenePath);
    }

    private ValidationMessage[] GetOrValidateSceneContent(string scenePath)
    {
        Hash128 currentHash = AssetDatabase.GetAssetDependencyHash(scenePath);

        if (SceneValidationCache.TryGetValue(scenePath, out var cachedResult) && cachedResult.Hash == currentHash)
        {
            return cachedResult.Messages;
        }

        ValidationMessage[] newMessages = ValidateSceneContents(scenePath);
        SceneValidationCache[scenePath] = (currentHash, newMessages);

        return newMessages;
    }

    private static ValidationMessage[] ValidateSceneContents(string scenePath)
    {
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);

        return EvaluateRules(GetCachedComponentMarkers(scenePath), sceneName).ToArray();
    }

    private List<ValidationMessage> GetLevelMessages()
    {
        return EvaluateRules(GetLevelMarkerCounts(), null);
    }

    private Dictionary<Type, int> GetLevelMarkerCounts()
    {
        var counts = new Dictionary<Type, int>();

        foreach (var scene in levelScenesProp.arraySize > 0
                     ? Enumerable.Range(0, levelScenesProp.arraySize)
                         .Select(i => levelScenesProp.GetArrayElementAtIndex(i).objectReferenceValue as SceneAsset)
                     : Enumerable.Empty<SceneAsset>())
        {
            if (scene == null)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(scene);

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            foreach (var (type, count) in GetCachedComponentMarkers(path))
            {
                counts[type] = counts.TryGetValue(type, out int existing) ? existing + count : count;
            }
        }

        return counts;
    }

    private static List<ValidationMessage> EvaluateRules(Dictionary<Type, int> counts, string sceneName)
    {
        string where = sceneName == null ? "This level" : $"Scene '{sceneName}'";

        var messages = new List<ValidationMessage>();

        foreach (var rule in MarkerRules)
        {
            counts.TryGetValue(rule.Type, out int count);

            if (count < rule.Min)
            {
                messages.Add(ToMessage(rule.Severity, $"{where} has no '{rule.Type.Name}'."));
                continue;
            }

            if (count > rule.Max)
            {
                messages.Add(ToMessage(
                    rule.Severity,
                    $"{where} has {count} '{rule.Type.Name}' components, but at most {rule.Max} allowed."));
            }
        }

        return messages;
    }

    private static ValidationMessage ToMessage(ValidationSeverity severity, string message) =>
        severity == ValidationSeverity.Error
            ? ValidationMessage.Error(message)
            : ValidationMessage.Warning(message);

    private static readonly Dictionary<string, (Hash128 Hash, Dictionary<Type, int> Counts)> SceneMarkerCache = new();

    private static Dictionary<Type, int> GetCachedComponentMarkers(string scenePath)
    {
        Hash128 currentHash = AssetDatabase.GetAssetDependencyHash(scenePath);

        if (SceneMarkerCache.TryGetValue(scenePath, out var cached) && cached.Hash == currentHash)
        {
            return cached.Counts;
        }

        // Opening scenes is expensive, so only do it when the scene actually changed.
        var counts = CountComponentsInScene(scenePath);
        SceneMarkerCache[scenePath] = (currentHash, counts);

        return counts;
    }

    /// <summary>
    /// Counts instances of every required marker type, including on inactive objects.
    /// </summary>
    private static Dictionary<Type, int> CountComponentsInScene(string scenePath)
    {
        var counts = new Dictionary<Type, int>();

        Scene openScene = SceneManager.GetActiveScene();
        bool wasOpen = openScene.IsValid() && openScene.path == scenePath;

        Scene scene = wasOpen ? openScene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        try
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    var type = component.GetType();
                    counts[type] = counts.TryGetValue(type, out int existing) ? existing + 1 : 1;
                }
            }
        }
        finally
        {
            if (!wasOpen && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        return counts;
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

        EditorGUILayout.EndVertical();
        GUILayout.Space(MetadataPadding);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUILayout.EndVertical();
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

        var levelMessages = GetLevelMessages();
        var errors = levelMessages.Where(m => m.Severity == ValidationSeverity.Error).ToList();
        var warnings = levelMessages.Where(m => m.Severity == ValidationSeverity.Warning).ToList();

        foreach (var warning in warnings)
        {
            Debug.LogWarning($"{level.LevelName}: {warning.Message}");
        }


        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Build Failed",
                string.Join("\n", errors.Select(e => e.Message)) + "\n\nFix the errors before building.",
                "OK");
            return;
        }

        showValidationErrors = false;

        string outputPath = EditorUtility.SaveFilePanel(
            "Build Level",
            GetDefaultBuildFolder(),
            GetDefaultFileName(level),
            LevelFileExtension);

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        if (!outputPath.EndsWith(LevelFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            outputPath += LevelFileExtension;
        }

        EditorPrefs.SetString(LastBuildFolderKey, Path.GetDirectoryName(outputPath));

        BuildLevel(level, outputPath);
    }

    private static string GetDefaultBuildFolder()
    {
        string lastFolder = EditorPrefs.GetString(LastBuildFolderKey, string.Empty);

        if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder))
        {
            return lastFolder;
        }

        string levelsDirectory = GoiInstall.LevelsDirectory;

        if (!string.IsNullOrEmpty(levelsDirectory))
        {
            return levelsDirectory;
        }

        return Path.Combine(Application.dataPath, "..", DefaultBuildFolderName);
    }

    private static void BuildLevel(CustomLevelObject level, string outputPath)
    {
        var scenePaths = GetValidScenePaths(level, out var skipped);

        if (scenePaths.Length == 0)
        {
            EditorUtility.DisplayDialog("Build Failed", "This level has no valid scenes assigned.", "OK");
            return;
        }

        string bundleName = SanitizeFileName(
            string.IsNullOrEmpty(level.LevelName) ? level.name : level.LevelName);

        var build = new AssetBundleBuild
        {
            assetBundleName = bundleName,
            assetNames = scenePaths
        };

        string tempFolder = Path.Combine(Path.GetTempPath(), "GOILevelBuild_" + bundleName);
        Directory.CreateDirectory(tempFolder);

        string bundlePath = Path.Combine(tempFolder, bundleName);

        var target = EditorUserBuildSettings.activeBuildTarget;
        var options = BuildAssetBundleOptions.ChunkBasedCompression;

        Debug.Log($"Building '{level.LevelName}' from {scenePaths.Length} scene(s) for {target}:");

        try
        {
            EditorUtility.DisplayProgressBar("Building Level", bundleName, 0.5f);

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                tempFolder,
                new[] { build },
                options,
                target);

            if (manifest == null || !File.Exists(bundlePath))
            {
                EditorUtility.DisplayDialog(
                    "Build Failed",
                    "The build pipeline did not produce a bundle. Check the console for details.",
                    "OK");
                return;
            }

            GltWriter.Write(level, bundlePath, outputPath);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Build Failed", e.Message, "OK");
            Debug.LogException(e);
            return;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            DeleteTempFolder(tempFolder);
        }

        if (skipped > 0)
        {
            Debug.LogWarning($"Skipped {skipped} scene entr(y/ies) that had no asset on disk.");
        }

        Debug.Log($"Built '{outputPath}'\n" +
                  string.Join("\n", scenePaths.Select(path => $"  - {Path.GetFileName(path)}")));

        EditorUtility.RevealInFinder(outputPath);
    }

    private static void DeleteTempFolder(string tempFolder)
    {
        try
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not clean up '{tempFolder}': {e.Message}");
        }
    }

    private static string[] GetValidScenePaths(CustomLevelObject level, out int skipped)
    {
        var paths = new List<string>();
        var seen = new HashSet<string>();
        skipped = 0;

        foreach (var scene in level.LevelScenes)
        {
            if (scene == null)
            {
                skipped++;
                continue;
            }

            string path = AssetDatabase.GetAssetPath(scene);

            if (string.IsNullOrEmpty(path) || !seen.Add(path))
            {
                skipped++;
                continue;
            }

            paths.Add(path);
        }

        return paths.ToArray();
    }

    private static string GetDefaultFileName(CustomLevelObject level)
    {
        return SanitizeFileName(
            string.IsNullOrEmpty(level.LevelName) ? level.name : level.LevelName);
    }

    private static string SanitizeFileName(string name)
    {
        var builder = new StringBuilder(name.Length);

        foreach (char c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
        }

        string sanitized = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "Level" : sanitized;
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