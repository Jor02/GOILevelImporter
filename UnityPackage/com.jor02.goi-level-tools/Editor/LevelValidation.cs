using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GOILevelImporter.Components;

/// <summary>
/// Checks that a level and its scenes contain the marker components the importer needs.
/// </summary>
public static class LevelValidation
{
    /// <summary>
    /// How serious a validation message is. Errors block the build, warnings only nag.
    /// </summary>
    public enum Severity
    {
        None,
        Warning,
        Error
    }

    public readonly struct Message
    {
        public readonly Severity Severity;
        public readonly string Text;

        private Message(Severity severity, string text)
        {
            Severity = severity;
            Text = text;
        }

        public static Message Error(string text) => new Message(Severity.Error, text);
        public static Message Warning(string text) => new Message(Severity.Warning, text);

        public bool HasValue => Severity != Severity.None && !string.IsNullOrEmpty(Text);

        public static Message Worst(params Message[] messages)
        {
            var worst = default(Message);

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
        public readonly Severity Severity;

        public MarkerRule(Type type, int min, int max, Severity severity)
        {
            Type = type;
            Min = min;
            Max = max;
            Severity = severity;
        }
    }

    // Errors block the build, warnings do not. A level allows exactly one player start
    // and at least one goal, since most levels can have several goals.
    private static readonly MarkerRule[] MarkerRules =
    {
        new MarkerRule(typeof(PlayerStart), min: 1, max: 1, Severity.Error),
        new MarkerRule(typeof(Goal), min: 1, max: int.MaxValue, Severity.Warning)
    };

    private static readonly Dictionary<string, (Hash128 Hash, Dictionary<Type, int> Counts)> SceneMarkerCache =
        new Dictionary<string, (Hash128 Hash, Dictionary<Type, int> Counts)>();

    /// <summary>
    /// Validates a single scene.
    /// </summary>
    public static Message[] ValidateScene(SceneAsset scene)
    {
        if (scene == null)
        {
            return new[] { Message.Error("Scene reference is missing or unassigned.") };
        }

        string scenePath = AssetDatabase.GetAssetPath(scene);

        if (string.IsNullOrEmpty(scenePath))
        {
            return new[] { Message.Error("Invalid scene path.") };
        }

        return EvaluateRules(GetCachedMarkerCounts(scenePath), Path.GetFileNameWithoutExtension(scenePath)).ToArray();
    }

    /// <summary>
    /// Validates a collection of scenes as one.
    /// </summary>
    public static List<Message> Validate(IEnumerable<SceneAsset> scenes, string sceneName = null)
    {
        var counts = new Dictionary<Type, int>();

        foreach (var scene in scenes)
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

            foreach (KeyValuePair<Type, int> entry in GetCachedMarkerCounts(path))
            {
                counts[entry.Key] = counts.TryGetValue(entry.Key, out int existing)
                    ? existing + entry.Value
                    : entry.Value;
            }
        }

        return EvaluateRules(counts, sceneName);
    }

    private static List<Message> EvaluateRules(Dictionary<Type, int> counts, string sceneName)
    {
        string where = sceneName == null ? "This level" : $"Scene '{sceneName}'";

        var messages = new List<Message>();

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

    private static Message ToMessage(Severity severity, string text) =>
        severity == Severity.Error ? Message.Error(text) : Message.Warning(text);

    private static Dictionary<Type, int> GetCachedMarkerCounts(string scenePath)
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
    /// Counts instances of every component type, including on inactive objects.
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

    public static string IconName(Severity severity) =>
        severity == Severity.Error ? "console.erroricon.sml" : "console.warnicon.sml";

    public static Color TextColor(Severity severity) =>
        severity == Severity.Error
            ? new Color(0.9f, 0.35f, 0.35f)
            : new Color(0.9f, 0.7f, 0.3f);

    public static string Describe(Message message) =>
        message.Severity == Severity.Error ? $"Error: {message.Text}" : $"Warning: {message.Text}";
}
