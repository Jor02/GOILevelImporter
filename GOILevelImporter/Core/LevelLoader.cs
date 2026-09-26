using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using GOILevelImporter.Utils;

namespace GOILevelImporter.Core
{
    /// <summary>
    /// Owns the currently loaded asset bundle and loads a level's scene into "Mian" once one has been picked.
    /// </summary>
    class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }
        public static bool Playing { get; private set; }
        public static bool Legacy { get; private set; }
        public static bool Async { get; set; }
        public static bool Loading { get; set; }
        public static bool HasCustomSpline { get; set; }
        public static AssetBundle currectBundle { get; private set; }
        public static string currentBundlePath { get; private set; }

        /// <summary>
        /// The "Levels" folder.
        /// </summary>
        public static string TargetPath { get; private set; }

        public LevelLoader()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TargetPath = ResolveLevelsPath();
        }

        private static string ResolveLevelsPath()
        {
            string path = Application.dataPath;

            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                    path += "/../../";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.LinuxPlayer:
                    path += "/../";
                    break;
            }

            return path + "Levels/";
        }

        /// <summary>
        /// Unloads the currently loaded level.
        /// </summary>
        public void Reset()
        {
            if (currectBundle)
            {
                currectBundle.Unload(true);
            }

            Playing = false;
            Loading = false;
            HasCustomSpline = false;
        }

        public void BeginLoadLevel(string path, bool legacy, ulong headerSize)
        {
            if (Playing) return;

            Playing = true;
            Legacy = legacy;
            currentBundlePath = path;
            currectBundle = AssetBundle.LoadFromFile(path, 0, headerSize);
        }

        /// <summary>
        /// Waits for an AsyncOperation to finish, then loads the level.
        /// </summary>
        public void LoadLevelAsync(AsyncOperation loadingOperation) => StartCoroutine(LoadLevelAsync_(loadingOperation));
        private IEnumerator LoadLevelAsync_(AsyncOperation loadingOperation)
        {
            Async = true;
            while (!loadingOperation.isDone) yield return null;
            Async = false;
            StartCoroutine(LoadLevel());
        }

        /// <summary>
        /// Restarts the current level, optionally keeping the sub-scene a
        /// SwitchScene trigger moved us to instead of going back to the entry scene.
        /// </summary>
        /// <param name="keepCurrentScene">True to resume in the saved sub-scene</param>
        public void Reload(bool keepCurrentScene = false)
        {
            if (Loading) return;

            if (!keepCurrentScene)
            {
                LevelSelectionState.ClearPendingScene();
            }

            Menu.LevelTransitionScreen.Instance.FadeOut();
            Loading = true;
            Time.timeScale = 0;
            Physics2D.simulationMode = SimulationMode2D.Script;

            PlayerPrefs.DeleteKey("NumSaves");
            PlayerPrefs.DeleteKey("SaveGame0");
            PlayerPrefs.DeleteKey("SaveGame1");
            PlayerPrefs.Save();

            LoadLevelAsync(SceneManager.LoadSceneAsync("Mian"));
        }

        public IEnumerator LoadLevel()
        {
            Loading = true;
            yield return new WaitForEndOfFrame();
            Physics2D.simulationMode = SimulationMode2D.Script;
            Time.timeScale = 0;

            var settings = LevelSelectionState.Metadata;

            // Creates the helper component custom level objects look up at runtime.
            new GameObject("ComponentHelper", typeof(Components.ComponentHelper));

            LevelSceneEffects.Apply(settings);

            // Fixes an index-out-of-range in PoseControl on levels with nothing wired up to it.
            Resources.FindObjectsOfTypeAll<PoseControl>()[0].SetPrivateFieldValue("interestingItems", new Transform[0]);

            // A SwitchScene trigger may have moved us to a sub-scene of this level.
            // Falls back to the bundle's entry scene on the first load.
            string targetScene = LevelSelectionState.GetPendingScene(currentBundlePath);
            string scenePath = string.IsNullOrWhiteSpace(targetScene) ? currectBundle.GetAllScenePaths()[0] : targetScene;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            while (!asyncLoad.isDone) yield return null;
            yield return new WaitForEndOfFrame();

            // Bundles built by older versions reference the old mod components, we have to replace these.
            if (Legacy) ReplaceLegacyComponents();

            // Levels can bring their own camera path. When one is present the game
            // scripts follow it and the fallback patches stand down.
            HasCustomSpline = Spline.SplineInstaller.TryInstall(
                Object.FindObjectOfType<Components.LevelCameraPath>());

            Menu.LevelTransitionScreen.Instance.FadeIn();
            Time.timeScale = 1;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            Loading = false;
        }

        private static void ReplaceLegacyComponents()
        {
            GameObject pos = GameObject.Find("startPos");
            if (pos != null)
            {
                pos.AddComponent<Components.PlayerStart>();
            }
        }
    }
}
