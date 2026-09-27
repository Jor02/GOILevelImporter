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

            CommandLine.Capture();
            new GameObject("Level Loader", typeof(LevelLoader));
            LevelSelectionState.BindConfig(Config);

            SceneManager.sceneLoaded += OnSceneLoaded;

            new Harmony(PluginInfo.GUID).PatchAll();
        }

        private System.Collections.IEnumerator SetupMenuWithTestLaunch()
        {
            if (CommandLine.HasTestLevel)
            {
                yield return null;

                Transform ui = GameObject.Find("/Canvas").transform;
                MainMenuController.EnsureTransitionScreen(ui);
                yield return CommandLine.AutoStartTestLevel();
                yield break;
            }

            yield return MainMenuController.Instance.Setup();
        }

        private void OnSceneLoaded(Scene target, LoadSceneMode mode)
        {
            if (target.name == "Loader")
            {
                LevelLoader.Instance.Reset();
                StartCoroutine(SetupMenuWithTestLaunch());
            }
            else if (!LevelLoader.LoadingMian && target.name == "Mian" && mode != LoadSceneMode.Additive && !LevelSelectionState.IsDefault)
            {
                StartCoroutine(LevelLoader.Instance.LoadLevel());
            }
        }
    }
}
