using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns a CustomLevelObject into a .glf bundle
/// </summary>
public static class LevelBuilder
{
    public const string LevelFileExtension = ".glf";
    private const string DefaultBuildFolderName = "LevelBuilds";
    private const string LastBuildFolderKey = "GOILevelTools.LastBuildFolder";

    /// <summary>
    /// Builds the level bundle and writes it to outputPath. Reports failures via dialogs and logs.
    /// </summary>
    public static void Build(CustomLevelObject level, string outputPath)
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

    /// <summary>
    /// Collects scene paths for the build, dropping null and duplicate entries.
    /// </summary>
    public static string[] GetValidScenePaths(CustomLevelObject level, out int skipped)
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

    public static string GetDefaultBuildFolder()
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

    public static void RememberBuildFolder(string outputPath)
    {
        EditorPrefs.SetString(LastBuildFolderKey, Path.GetDirectoryName(outputPath));
    }

    public static string GetDefaultFileName(CustomLevelObject level)
    {
        return SanitizeFileName(
            string.IsNullOrEmpty(level.LevelName) ? level.name : level.LevelName);
    }

    /// <summary>
    /// Reduces a level name to characters that are safe in a file and bundle name.
    /// </summary>
    public static string SanitizeFileName(string name)
    {
        var builder = new StringBuilder(name.Length);

        foreach (char c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
        }

        string sanitized = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "Level" : sanitized;
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
}
