using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(CustomLevelObject))]
public class CustomLevelObjectEditor : Editor
{
    private const float AspectWidth = 718f;
    private const float AspectHeight = 400f;
    private const float PlaceholderPadding = 4f;
    private const float MetadataPadding = 10f;
    private const string LevelFileExtension = ".glf";
    private const string DefaultBuildFolderName = "LevelBuilds";
    private const string LastBuildFolderKey = "GOILevelTools.LastBuildFolder";
    
    private int pickerControlID = -1;

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

        sceneList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = sceneList.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            EditorGUI.PropertyField(rect, element, GUIContent.none);
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

        DrawPropertyWithPlaceholder(levelNameProp, "Level Name", "My Level");
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

        EditorGUILayout.EndVertical();
        GUILayout.Space(MetadataPadding);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUILayout.EndVertical();
    }

    private void DrawBuildButton()
    {
        var level = target as CustomLevelObject;
        bool canBuild = level != null && level.LevelScenes != null && level.LevelScenes.Count > 0;

        using (new EditorGUI.DisabledScope(!canBuild))
        {
            if (GUILayout.Button("Build", GUILayout.Height(28)))
            {
                BuildLevel(level);
            }
        }
    }

    private static void BuildLevel(CustomLevelObject level)
    {
        if (level.LevelScenes == null || level.LevelScenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Build Failed", "This level has no scenes assigned.", "OK");
            return;
        }

        string defaultFolder = GetDefaultBuildFolder();

        string outputFolder = EditorUtility.SaveFolderPanel(
            "Build Level",
            defaultFolder,
            DefaultBuildFolderName);

        if (string.IsNullOrEmpty(outputFolder))
        {
            return;
        }

        EditorPrefs.SetString(LastBuildFolderKey, outputFolder);

        BuildLevel(level, outputFolder);
    }

    /// <summary>
    /// Remembers the last used output folder so repeat builds are one click.
    /// Falls back to a folder under Assets the first time around.
    /// </summary>
    private static string GetDefaultBuildFolder()
    {
        string lastFolder = EditorPrefs.GetString(LastBuildFolderKey, string.Empty);

        if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder))
        {
            return lastFolder;
        }

        return Path.Combine(Application.dataPath, "..", DefaultBuildFolderName);
    }

    private static void BuildLevel(CustomLevelObject level, string outputFolder)
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

        string outputPath = Path.Combine(outputFolder, bundleName + LevelFileExtension);

        // The bundle has to be a standalone file so it can be appended to the
        // .glf, so it goes to a temp folder instead of straight to the output.
        string tempFolder = Path.Combine(Path.GetTempPath(), "GOILevelBuild_" + bundleName);
        Directory.CreateDirectory(tempFolder);

        string bundlePath = Path.Combine(tempFolder, bundleName);

        var target = EditorUserBuildSettings.activeBuildTarget;
        var options = BuildAssetBundleOptions.ChunkBasedCompression
                      | BuildAssetBundleOptions.DeterministicAssetBundle;

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

    /// <summary>
    /// The temp folder is created outside the project, so nothing is left
    /// behind even when the build throws.
    /// </summary>
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

    /// <summary>
    /// Grabs the scene paths that can actually be bundled. Entries that lost
    /// their asset (deleted, moved, unresaved) would fail the whole build, so
    /// they get dropped and reported instead.
    /// </summary>
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

    /// <summary>
    /// Bundle names end up as file names and manifest keys, so anything the
    /// file system would object to gets swapped for an underscore.
    /// </summary>
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

    private void DrawPropertyWithPlaceholder(SerializedProperty prop, string label, string placeholder)
    {
        EditorGUILayout.PropertyField(prop, new GUIContent(label));

        if (string.IsNullOrEmpty(prop.stringValue))
        {
            Rect fieldRect = GUILayoutUtility.GetLastRect();

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