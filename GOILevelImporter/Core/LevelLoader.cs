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
        public enum LoadState
        {
            Idle,
            BundleReady,
            LoadingMian,
            LoadingLevel,
            Playing
        }

        public static LevelLoader Instance { get; private set; }
        public static LoadState State { get; private set; } = LoadState.Idle;
        public static bool HasCustomSpline { get; private set; }

        /// <summary>
        /// A level session is open.
        /// </summary>
        public static bool Playing => State != LoadState.Idle;

        /// <summary>
        /// A scene swap is in flight.
        /// </summary>
        public static bool IsBusy => State == LoadState.LoadingMian || State == LoadState.LoadingLevel;

        /// <summary>
        /// We are reloading "Mian" ourselves.
        /// </summary>
        public static bool LoadingMian => State == LoadState.LoadingMian;

        public static AssetBundle currentBundle { get; private set; }
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
            if (currentBundle)
            {
                currentBundle.Unload(true);
                currentBundle = null;
            }

            currentBundlePath = string.Empty;
            State = LoadState.Idle;
            HasCustomSpline = false;
        }

        /// <summary>
        /// Opens the selected level's bundle.
        /// </summary>
        public void BeginLoadLevel()
        {
            if (State != LoadState.Idle) return;

            string path = LevelSelectionState.LevelPath;
            ulong headerSize = LevelSelectionState.LevelHeaderSize;
            currentBundlePath = path;
            currentBundle = AssetBundle.LoadFromFile(path, 0, headerSize);

            if (currentBundle == null)
            {
                ReportBundleFailure(path, headerSize);

                // Nothing to load, so drop back to "no level started" and let the
                // game continue into its own scene instead of a botched fade.
                currentBundlePath = string.Empty;
                return;
            }

            State = LoadState.BundleReady;
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
        /// Deletes the active save slots.
        /// </summary>
        public static void WipeSaves()
        {
            PlayerPrefs.DeleteKey("NumSaves");
            PlayerPrefs.DeleteKey("SaveGame0");
            PlayerPrefs.DeleteKey("SaveGame1");
            PlayerPrefs.Save();
        }

        private static void FreezeWorld()
        {
            Time.timeScale = 0;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        private static void UnfreezeWorld()
        {
            Time.timeScale = 1;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        }

        /// <summary>
        /// Restarts the current level, optionally keeping the sub-scene a
        /// SwitchScene trigger moved us to instead of going back to the entry scene.
        /// </summary>
        /// <param name="keepCurrentScene">True to resume in the saved sub-scene</param>
        public void Reload(bool keepCurrentScene = false)
        {
            if (IsBusy) return;

            if (!keepCurrentScene)
            {
                LevelSelectionState.ClearPendingScene();
            }

            Menu.LevelTransitionScreen.Instance.FadeOut();
            State = LoadState.LoadingMian;
            FreezeWorld();
            WipeSaves();

            var mianLoad = SceneManager.LoadSceneAsync("Mian");
            StartCoroutine(WaitForBaseScene(mianLoad));
        }

        private IEnumerator WaitForBaseScene(AsyncOperation loadingOperation)
        {
            while (!loadingOperation.isDone) yield return null;
            StartCoroutine(LoadLevel());
        }

        /// <summary>
        /// Loads the level's scene additively on top of "Mian" once the base scene is up.
        /// </summary>
        public IEnumerator LoadLevel()
        {
            // A failed bundle is reported and cleared by BeginLoadLevel, and a
            // bundle without scenes has nothing to show. Both would fault further
            // down, so drop the level session and leave the game on its own scene.
            if (currentBundle == null)
            {
                Debug.LogWarning("Skipping level load: no bundle is loaded.");
                Reset();
                UnfreezeWorld();
                yield break;
            }

            string[] scenePaths = currentBundle.GetAllScenePaths();

            if (scenePaths.Length == 0)
            {
                Debug.LogError("Level bundle '" + currentBundlePath + "' holds no scenes.");
                Reset();
                UnfreezeWorld();
                yield break;
            }

            State = LoadState.LoadingLevel;
            yield return new WaitForEndOfFrame();
            FreezeWorld();

            var settings = LevelSelectionState.Metadata;

            // Deletes all unneeded gameobjects.
            LevelSceneEffects.Apply(settings);

            // Creates the helper component custom level objects look up at runtime.
            new GameObject("ComponentHelper", typeof(Components.ComponentHelper));

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
            if (LevelSelectionState.Legacy) ReplaceLegacyComponents();

            // Levels can bring their own camera path. When one is present the game
            // scripts follow it and the fallback patches stand down.
            HasCustomSpline = Spline.SplineInstaller.TryInstall(
                Object.FindObjectOfType<Components.LevelCameraPath>());

            Menu.LevelTransitionScreen.Instance.FadeIn();
            UnfreezeWorld();
            State = LoadState.Playing;
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
