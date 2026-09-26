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
                currectBundle = null;
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

            if (currectBundle == null)
            {
                ReportBundleFailure(path, headerSize);

                // Nothing to load, so drop back to "no level started" and let the
                // game continue into its own scene instead of a botched fade.
                Playing = false;
                currentBundlePath = string.Empty;
            }
        }

        /// <summary>
        /// Explains a bundle the runtime refused to open. Unity only loads bundles
        /// built by the same Unity version line as the player, and the bundle
        /// header records the version that built it, so that gets read back here
        /// instead of leaving the bare "Unable to read header from archive file"
        /// in the log.
        /// </summary>
        private static void ReportBundleFailure(string path, ulong headerSize)
        {
            string builtWith = LevelFileScanner.ReadBundleUnityVersion(path, (long)headerSize);

            Debug.LogError("Could not load level '" + path + "'. Unity " + Application.unityVersion +
                           " could not read the bundle header" +
                           (string.IsNullOrEmpty(builtWith)
                               ? string.Empty
                               : ", and the bundle says it was built with Unity " + builtWith) +
                           ". Rebuild the level with Unity " + Application.unityVersion + " to load it.");
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
            // A failed bundle is reported and cleared by BeginLoadLevel, and a
            // bundle without scenes has nothing to show. Both would fault further
            // down, so stop here and leave the game on its own scene.
            if (currectBundle == null)
            {
                Debug.LogWarning("Skipping level load: no bundle is loaded.");
                Loading = false;
                yield break;
            }

            string[] scenePaths = currectBundle.GetAllScenePaths();

            if (scenePaths.Length == 0)
            {
                Debug.LogError("Level bundle '" + currentBundlePath + "' holds no scenes.");
                Loading = false;
                yield break;
            }

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
            string scenePath = string.IsNullOrWhiteSpace(targetScene) ? scenePaths[0] : targetScene;

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
