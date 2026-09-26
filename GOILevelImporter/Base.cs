using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using GOILevelImporter.Core;
using GOILevelImporter.Core.Menu;

namespace GOILevelImporter
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Base : BaseUnityPlugin
    {
        public static Base Instance { get; private set; }

        void Awake()
        {
            Instance = this;

            new GameObject("Level Loader", typeof(LevelLoader));
            LevelSelectionState.BindConfig(Config);

            SceneManager.sceneLoaded += OnSceneLoaded;

            new Harmony(PluginInfo.GUID).PatchAll();
        }

        private void OnSceneLoaded(Scene target, LoadSceneMode mode)
        {
            if (target.name == "Loader")
            {
                LevelLoader.Instance.Reset();
                StartCoroutine(MainMenuController.Instance.Setup());
            }
            else if (!LevelLoader.Async && target.name == "Mian" && mode != LoadSceneMode.Additive && !LevelSelectionState.IsDefault)
            {
                // A restart (not our own additive load) lands back on "Mian"
                // with no scene queued, so kick off the normal level load.
                StartCoroutine(LevelLoader.Instance.LoadLevel());
            }
        }
    }
}
