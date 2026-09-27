using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomLevelObject : ScriptableObject
{
    public string LevelName;
    public string Author;
    [TextArea(3, 5)]
    public string Description;

    [Header("Thumbnail")]
    public Texture2D Thumbnail;

    public List<SceneAsset> LevelScenes = new List<SceneAsset>();

    [Serializable]
    public struct LevelProperty
    {
        public string Key;
        public string Value;
    }

    [Header("Level Properties")]
    public List<LevelProperty> Settings = new List<LevelProperty>();

    /// <summary>
    /// Reads one saved property, falling back when the key is missing.
    /// </summary>
    public string GetSetting(string key, string fallback = "")
    {
        if (Settings != null)
        {
            foreach (var entry in Settings)
            {
                if (entry.Key == key)
                    return entry.Value ?? string.Empty;
            }
        }

        if (LevelPropertyRegistry.TryGet(key, out var def))
            return def.DefaultValue;

        return fallback;
    }

    /// <summary>
    /// Saves one property value, adding the entry when it is missing.
    /// </summary>
    public void SetSetting(string key, string value)
    {
        if (Settings == null)
            Settings = new List<LevelProperty>();

        for (int i = 0; i < Settings.Count; i++)
        {
            if (Settings[i].Key == key)
            {
                Settings[i] = new LevelProperty { Key = key, Value = value ?? string.Empty };
                return;
            }
        }

        Settings.Add(new LevelProperty { Key = key, Value = value ?? string.Empty });
    }

    /// <summary>
    /// Saved properties as a plain dict for the .glf writer.
    /// </summary>
    public Dictionary<string, string> ToSettingsDictionary()
    {
        var dict = new Dictionary<string, string>();
        if (Settings == null)
            return dict;

        foreach (var entry in Settings)
        {
            if (string.IsNullOrEmpty(entry.Key) || dict.ContainsKey(entry.Key))
                continue;
            dict[entry.Key] = entry.Value ?? string.Empty;
        }

        return dict;
    }

    private void OnValidate()
    {
        PruneUnknownSettings();

        if (LevelScenes == null || LevelScenes.Count < 2)
        {
            return;
        }

        var seen = new HashSet<SceneAsset>();
        for (int i = LevelScenes.Count - 1; i >= 0; i--)
        {
            if (LevelScenes[i] == null)
                continue;

            if (!seen.Add(LevelScenes[i]))
            {
                LevelScenes.RemoveAt(i);
            }
        }
    }

    // Drops saved entries whose key left the registry, and collapses dupes.
    private void PruneUnknownSettings()
    {
        if (Settings == null || Settings.Count == 0)
            return;

        var seen = new HashSet<string>();
        for (int i = Settings.Count - 1; i >= 0; i--)
        {
            string key = Settings[i].Key;
            if (string.IsNullOrEmpty(key) || !LevelPropertyRegistry.Types.ContainsKey(key) || !seen.Add(key))
                Settings.RemoveAt(i);
        }
    }

    [MenuItem("Assets/Create/Custom Level", false, 1)]
    private static void CreateFromScene(MenuCommand command)
    {
        SceneAsset scene = command.context as SceneAsset;

        if (scene == null)
        {
            return;
        }

        string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(scene)) ?? "Assets";

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{directory}/{scene.name} Level.asset");

        var level = CreateInstance<CustomLevelObject>();
        level.LevelName = scene.name;
        level.LevelScenes = new List<SceneAsset> { scene };

        AssetDatabase.CreateAsset(level, assetPath);

        Undo.RegisterCreatedObjectUndo(level, "Create Custom Level");
        EditorGUIUtility.PingObject(level);
    }

    [MenuItem("Assets/Create/Custom Level", true, 1)]
    private static bool ValidateCreateFromScene(MenuCommand command)
    {
        return command.context is SceneAsset;
    }
}