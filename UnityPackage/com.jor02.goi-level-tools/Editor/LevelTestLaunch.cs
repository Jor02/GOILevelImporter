using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the level into a temp folder and launches the game with --test-level
/// so the author lands straight in the level.
/// </summary>
public static class LevelTestLaunch
{
    private const string TestLevelArg = "--test-level";

    // Test builds land here instead of the game's Levels folder, which the mod
    // scans for the in game level list. One file per level name, overwritten on
    // every test run.
    private const string TestBuildFolderName = "GOILevelTest";

    public static void Test(CustomLevelObject level)
    {
        if (level.LevelScenes.Count == 0)
        {
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            bool goAhead = EditorUtility.DisplayDialog(
                "Test Level",
                "The active build target is " + EditorUserBuildSettings.activeBuildTarget +
                ", but Getting Over It is a Windows game. The test build may not load. Continue anyway?",
                "Continue",
                "Cancel");

            if (!goAhead)
            {
                return;
            }
        }

        GoiInstall.ClearCache();
        string gameExe = GoiInstall.GameExePath;

        if (string.IsNullOrEmpty(gameExe) || !File.Exists(gameExe))
        {
            EditorUtility.DisplayDialog(
                "Test Level",
                "Could not find the Getting Over It install. Build the level manually and copy it into the game's Levels folder.",
                "OK");
            return;
        }

        string versionProblem = LevelBuilder.GetUnityVersionProblem();

        if (versionProblem != null)
        {
            // The game cannot open a bundle built by a newer Unity than its own, so
            // launching it here would only show a load failure.
            EditorUtility.DisplayDialog("Test Level", versionProblem, "OK");
            return;
        }

        string outputPath = Path.Combine(
            Path.Combine(Path.GetTempPath(), TestBuildFolderName),
            LevelBuilder.GetDefaultFileName(level) + LevelBuilder.LevelFileExtension);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        if (!LevelBuilder.TryBuild(level, outputPath, showDialogs: true, revealInFinder: false))
        {
            return;
        }

        try
        {
            Process.Start(gameExe, TestLevelArg + " \"" + outputPath + "\"");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Test Level", "Could not start the game: " + e.Message, "OK");
            UnityEngine.Debug.LogException(e);
        }
    }
}
