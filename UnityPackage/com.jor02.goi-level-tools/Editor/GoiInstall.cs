using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using UnityEngine;

/// <summary>
/// Finds the Getting Over It install
/// </summary>
public static class GoiInstall
{
    private const string SteamAppNumber = "240720";
    private const string GameFolderName = "Getting Over It";
    private const string GameDataFolderName = "GettingOverIt_Data";
    private const string LevelsFolderName = "Levels";

    private const string UninstallKeyPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + SteamAppNumber;

    private const string SteamKeyPath = @"SOFTWARE\Valve\Steam";

    private static string cachedGameDirectory;
    private static bool hasSearched;

    /// <summary>
    /// The game folder, or null when the install could not be found.
    /// The result is cached because the registry lookups are not free.
    /// </summary>
    public static string GameDirectory
    {
        get
        {
            if (!hasSearched)
            {
                cachedGameDirectory = DetectGameDirectory();
                hasSearched = true;
            }

            return cachedGameDirectory;
        }
    }

    /// <summary>
    /// The Levels folder the mod scans at runtime, or null when the game
    /// folder is unknown. Matches LevelLoader.GetLevelPath, which resolves to
    /// "Levels" next to the game's data folder.
    /// </summary>
    public static string LevelsDirectory
    {
        get
        {
            string gameDirectory = GameDirectory;
            return string.IsNullOrEmpty(gameDirectory) ? null : Path.Combine(gameDirectory, LevelsFolderName);
        }
    }

    public static bool IsInstalled => !string.IsNullOrEmpty(GameDirectory);

    /// <summary>
    /// Forgets the cached result so a freshly installed or moved game is picked
    /// up without restarting the editor.
    /// </summary>
    public static void ClearCache()
    {
        hasSearched = false;
        cachedGameDirectory = null;
    }

    /// <summary>
    /// Opens a registry value, returning null for anything that goes wrong.
    /// A missing key is normal on a machine without Steam, so failures are
    /// swallowed rather than logged.
    /// </summary>
    private static string ReadRegistryString(RegistryKey root, string keyPath, string valueName)
    {
        try
        {
            using (RegistryKey key = root.OpenSubKey(keyPath))
            {
                return key?.GetValue(valueName) as string;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// A folder only counts as the install when the Unity data folder is inside
    /// it. The registry value is trusted by the MSBuild side, but here a wrong
    /// guess would silently send built levels somewhere the game never reads.
    /// </summary>
    private static bool IsGameFolder(string path)
    {
        return !string.IsNullOrEmpty(path)
               && Directory.Exists(Path.Combine(path, GameDataFolderName));
    }

    private static string DetectGameDirectory()
    {
        // Registry is Windows only. Unity runs on macOS and Linux too, where
        // touching Registry.LocalMachine throws before the try/catch in the
        // reader could help, because the property is evaluated as an argument.
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            return null;
        }

        foreach (string candidate in EnumerateCandidates())
        {
            if (IsGameFolder(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        // Steam is a 32 bit app, so its keys land in the WOW6432Node redirect on
        // 64 bit Windows. The plain path is tried too, which covers 32 bit
        // systems and installs that registered themselves natively.
        string fromUninstall =
            ReadRegistryString(Registry.LocalMachine, $@"SOFTWARE\WOW6432Node\{UninstallKeyPath}", "InstallLocation")
            ?? ReadRegistryString(Registry.LocalMachine, UninstallKeyPath, "InstallLocation");

        if (!string.IsNullOrEmpty(fromUninstall))
        {
            yield return fromUninstall;
        }

        string steamPath =
            ReadRegistryString(Registry.CurrentUser, $@"SOFTWARE\WOW6432Node\{SteamKeyPath}", "SteamPath")
            ?? ReadRegistryString(Registry.CurrentUser, SteamKeyPath, "SteamPath");

        if (!string.IsNullOrEmpty(steamPath))
        {
            yield return Path.Combine(steamPath, "steamapps", "common", GameFolderName);
        }

        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        if (!string.IsNullOrEmpty(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "Steam", "steamapps", "common", GameFolderName);
        }

        if (!string.IsNullOrEmpty(programFiles))
        {
            yield return Path.Combine(programFiles, "Steam", "steamapps", "common", GameFolderName);
        }
    }
}