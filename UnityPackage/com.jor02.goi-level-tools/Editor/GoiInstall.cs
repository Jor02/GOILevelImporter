using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    private const string UnityPlayerFileName = "UnityPlayer.dll";

    private const string UninstallKeyPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + SteamAppNumber;

    private const string SteamKeyPath = @"SOFTWARE\Valve\Steam";

    private const string UninstallKeyPath32 =
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + SteamAppNumber;

    private const string SteamKeyPath32 = @"SOFTWARE\WOW6432Node\Valve\Steam";

    private const string LocalMachineHive = "LocalMachine";
    private const string CurrentUserHive = "CurrentUser";

    private static readonly Type RegistryType =
        Type.GetType("Microsoft.Win32.Registry, Microsoft.Win32.Registry", throwOnError: false)
        ?? Type.GetType("Microsoft.Win32.Registry, mscorlib", throwOnError: false);

    private static string cachedGameDirectory;
    private static bool hasSearched;

    private static string cachedUnityVersion;
    private static bool hasLookedUpUnityVersion;

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

    public const string GameExeName = "GettingOverIt.exe";

    public static string GameExePath
    {
        get
        {
            string gameDirectory = GameDirectory;
            return string.IsNullOrEmpty(gameDirectory) ? null : Path.Combine(gameDirectory, GameExeName);
        }
    }

    public static bool IsInstalled => !string.IsNullOrEmpty(GameDirectory);

    public static string GameUnityVersion
    {
        get
        {
            if (!hasLookedUpUnityVersion)
            {
                cachedUnityVersion = DetectUnityVersion();
                hasLookedUpUnityVersion = true;
            }

            return cachedUnityVersion;
        }
    }

    /// <summary>
    /// Forgets the cached result so a freshly installed or moved game is picked
    /// up without restarting the editor.
    /// </summary>
    public static void ClearCache()
    {
        hasSearched = false;
        cachedGameDirectory = null;

        hasLookedUpUnityVersion = false;
        cachedUnityVersion = null;
    }

    /// <summary>
    /// Reads the player version off UnityPlayer.dll, which carries the full
    /// build string ("2020.3.25.10195328"). The last part is the build number,
    /// so it is dropped to leave a version that compares against
    /// Application.unityVersion.
    /// </summary>
    private static string DetectUnityVersion()
    {
        string gameDirectory = GameDirectory;

        if (string.IsNullOrEmpty(gameDirectory))
        {
            return null;
        }

        try
        {
            string playerPath = Path.Combine(gameDirectory, UnityPlayerFileName);

            if (!File.Exists(playerPath))
            {
                return null;
            }

            string[] parts = System.Diagnostics.FileVersionInfo.GetVersionInfo(playerPath).FileVersion?.Split('.');

            return parts != null && parts.Length >= 3
                ? parts[0] + "." + parts[1] + "." + parts[2]
                : null;
        }
        catch (Exception)
        {
            // A missing or locked player only costs the version check.
            return null;
        }
    }

    /// <summary>
    /// Reads a registry string value, returning null when the hive, the key, or
    /// the value is missing. A machine without Steam has none of them, so
    /// failures are swallowed rather than logged.
    /// </summary>
    private static string ReadRegistryString(string hive, string keyPath, string valueName)
    {
        object key = null;

        try
        {
            // OpenSubKey and GetValue sit on Microsoft.Win32.RegistryKey, which is
            // not available at compile time either, so they are pulled off the
            // runtime types.
            key = InvokeStringMethod(RegistryRoot(hive), "OpenSubKey", keyPath);

            return InvokeStringMethod(key, "GetValue", valueName) as string;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            (key as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// The hive key object, or null when the registry cannot be reached. Mono
    /// exposes the hives as static fields and the .NET Framework as static
    /// properties, so both are checked.
    /// </summary>
    private static object RegistryRoot(string hive)
    {
        if (RegistryType == null)
        {
            return null;
        }

        MemberInfo[] members = RegistryType.GetMember(hive, BindingFlags.Public | BindingFlags.Static);

        if (members.Length == 0)
        {
            return null;
        }

        FieldInfo field = members[0] as FieldInfo;

        if (field != null)
        {
            return field.GetValue(null);
        }

        PropertyInfo property = members[0] as PropertyInfo;

        return property == null ? null : property.GetValue(null);
    }

    private static object InvokeStringMethod(object target, string methodName, string argument)
    {
        MethodInfo method = target?.GetType().GetMethod(methodName, new[] { typeof(string) });

        return method == null ? null : method.Invoke(target, new object[] { argument });
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
        // The registry only exists on Windows, and the folder guesses below are
        // Windows paths too. Unity runs on macOS and Linux as well, where there is
        // nothing to look up.
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
        string fromUninstall =
            ReadRegistryString(LocalMachineHive, UninstallKeyPath, "InstallLocation")
            ?? ReadRegistryString(LocalMachineHive, UninstallKeyPath32, "InstallLocation");

        if (!string.IsNullOrEmpty(fromUninstall))
        {
            yield return fromUninstall;
        }

        string steamPath =
            ReadRegistryString(CurrentUserHive, SteamKeyPath, "SteamPath")
            ?? ReadRegistryString(CurrentUserHive, SteamKeyPath32, "SteamPath");

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