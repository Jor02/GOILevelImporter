using BepInEx.Configuration;

namespace GOILevelImporter.Core
{
    /// <summary>
    /// Which level is picked in the menu, and where a reload should resume.
    /// </summary>
    static class LevelSelectionState
    {
        public static string LevelPath { get; private set; } = string.Empty;
        public static bool Legacy { get; private set; }
        public static ulong LevelHeaderSize { get; private set; }
        public static bool IsDefault { get; private set; } = true;
        public static LevelMetadata Metadata { get; private set; }

        private static ConfigEntry<string> selectedLevelConfig;
        private static ConfigEntry<string> pendingSceneConfig;
        private static ConfigEntry<string> pendingSceneLevelConfig;

        /// <summary>
        /// Registers the backing config entries. Called once from the plugin's Awake.
        /// </summary>
        public static void BindConfig(ConfigFile config)
        {
            selectedLevelConfig = config.Bind(
                "General",
                "currentLevel",
                string.Empty,
                "The current selected level"
            );

            pendingSceneLevelConfig = config.Bind(
                "General",
                "targetSceneLevel",
                string.Empty,
                "Path of the level whose target scene is saved, so a resumed sub-scene is only used for the level that set it"
            );

            pendingSceneConfig = config.Bind(
                "General",
                "targetScene",
                string.Empty,
                "The scene to load after the level entry scene, set by a SwitchScene trigger"
            );
        }

        /// <summary>
        /// Level path saved from a previous session, used to re-highlight the right card when the menu builds its level list.
        /// </summary>
        public static string SavedLevelPath => selectedLevelConfig.GetSerializedValue();

        public static void Select(bool isDefault, string levelPath, bool legacy, ulong headerSize, LevelMetadata metadata)
        {
            IsDefault = isDefault;
            LevelPath = levelPath;
            Legacy = legacy;
            LevelHeaderSize = headerSize;
            Metadata = metadata;

            selectedLevelConfig.SetSerializedValue(levelPath);
        }

        /// <summary>
        /// Called by a SwitchScene trigger so a reload resumes in the sub-scene instead of going back to the level's entry scene.
        /// </summary>
        public static void SetPendingScene(string sceneName, string bundlePath)
        {
            pendingSceneConfig.SetSerializedValue(sceneName);
            pendingSceneLevelConfig.SetSerializedValue(bundlePath);
        }

        public static string GetPendingScene(string bundlePath)
        {
            if (string.IsNullOrWhiteSpace(pendingSceneConfig.Value) || pendingSceneLevelConfig.Value != bundlePath)
            {
                return string.Empty;
            }
            return pendingSceneConfig.Value;
        }

        public static void ClearPendingScene()
        {
            pendingSceneConfig.SetSerializedValue(string.Empty);
            pendingSceneLevelConfig.SetSerializedValue(string.Empty);
        }
    }
}
