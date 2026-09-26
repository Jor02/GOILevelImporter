using System.Globalization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using System.Reflection;
using System;
using GOILevelImporter.Utils;

namespace GOILevelImporter.Core
{
    class LevelLoader : MonoBehaviour
    {
        #region Fields/Properties
        public static LevelLoader Instance { get; private set; }
        public static bool Playing { get; private set; } = false;
        public static bool Legacy { get; private set; } = false;
        public static bool Async { get; set; } = false;
        public static bool Loading { get; set; } = false;
        public static bool HasCustomSpline { get; set; } = false;
        public long HeaderSize { get; private set; } = 0;
        public static AssetBundle currectBundle { get; private set; }
        public static string currentBundlePath { get; private set; }

        public static string targetPath { get; private set; }
        private readonly byte[] header = new byte[] {0x47, 0x4F, 0x49, 0x4C, 0x46};

        #endregion

        public LevelLoader()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GetLevelPath();
        }

        #region Methods
        /// <summary>
        /// Gets path to level directory.
        /// Usually "../GOI/Levels/"
        /// </summary>
        private void GetLevelPath()
        {
            targetPath = Application.dataPath;

            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                    targetPath += "/../../";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.LinuxPlayer:
                    targetPath += "/../";
                    break;
            }

            targetPath += "Levels/";
        }

        /// <summary>
        /// Unloads current loaded level
        /// </summary>
        public void Reset()
        {
            if (currectBundle)
                currectBundle.Unload(true);
            Playing = false;
            Loading = false;
            HasCustomSpline = false;
        }

        #region Loading
        /// <summary>
        /// Loads the level
        /// </summary>
        /// <param name="path">Path to level file</param>
        /// <param name="HeaderSize">Byte size of the header (to skip)</param>
        /// <returns></returns>
        public void BeginLoadLevel(string path, bool legacy, ulong HeaderSize)
        {
            if (!Playing)
            {
                Playing = true;
                Legacy = legacy;
                currentBundlePath = path;
                currectBundle = AssetBundle.LoadFromFile(path, 0, HeaderSize);
            }
        }

        /// <summary>
        /// Waits for AsyncOperation to complete then loads the level
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
                Base.ClearPendingScene();
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

            var settings = Base.metadata;

            if (settings.ReplacementMap)
            {
                foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (!requiredGameObject(gameObject))
                    {
                        Destroy(gameObject);
                    }
                }
            }

            //Creates helper class for custom components
            new GameObject("ComponentHelper", typeof(Components.ComponentHelper));

            //Disable fog controllers
            GameObject bgCam = GameObject.Find("/Main Camera/BGCamera");
            Destroy(bgCam.GetComponent<FogControl>());
            Destroy(bgCam.GetComponent<FogVolumeRenderer>());

            if (settings.ReplaceSky)
            {
                Destroy(GameObject.Find("CloudSystems"));
                Destroy(GameObject.Find("SkySphere"));

                var sky = bgCam.transform.Find("Sky");
                if (sky != null) Destroy(sky.gameObject);

                var starnest = bgCam.transform.Find("Starnest");
                if (starnest != null) Destroy(starnest.gameObject);

                var bgCamera = bgCam.GetComponent<Camera>();
                bgCamera.clearFlags = CameraClearFlags.Skybox;
                bgCamera.backgroundColor = Color.black;
            }
            else
            {
                //Without a replacement sky the background camera would draw nothing behind the level.
                bgCam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            }

            if (settings.HideShadow)
            {
                var shadow = GameObject.Find("/Player/Hub/Slider/Handle/PoleMiddle/climbinghammer_remap/RetopoGroup1/Shadow");
                if (shadow != null) shadow.SetActive(false);
            }

            if (settings.FixHammerMaterial)
            {
                var hammerMesh = GameObject.Find("/Player/handle/Mesh");
                if (hammerMesh != null)
                {
                    var renderer = hammerMesh.GetComponent<SkinnedMeshRenderer>();
                    if (renderer != null) renderer.material.shader = Shader.Find("Standard");
                }
            }

            if (settings.ReplaceLighting)
            {
                ApplyLighting();
            }

            ApplyFog(settings.Fog);

            //Fixes error in PoseControl
            Resources.FindObjectsOfTypeAll<PoseControl>()[0].SetPrivateFieldValue("interestingItems", new Transform[0]);

            //A SwitchScene trigger may have moved us to a sub-scene of this level.
            //Falls back to the bundle's entry scene on the first load.
            string targetScene = Base.GetPendingScene(currentBundlePath);
            string scenePath = string.IsNullOrWhiteSpace(targetScene) ? currectBundle.GetAllScenePaths()[0] : targetScene;

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
                yield return null;
            yield return new WaitForEndOfFrame();

            //Bundles built by older versions reference the old mod components, we have to replace these.
            if (Legacy) LegacyComponents();

            //Levels can bring their own camera path. When one is present the game
            //scripts follow it and the fallback patches stand down.
            HasCustomSpline = Spline.SplineInstaller.TryInstall(
                UnityEngine.Object.FindObjectOfType<Components.LevelCameraPath>());

            Menu.LevelTransitionScreen.Instance.FadeIn();
            Time.timeScale = 1;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            Loading = false;
        }

        /// <summary>
        /// Flattens the ambient lighting. Levels that build their own lighting would otherwise not look very good.
        /// </summary>
        private static void ApplyLighting()
        {
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.3f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.ambientLight = Color.white;
            RenderSettings.ambientSkyColor = Color.black;
        }

        /// <summary>
        /// Applies a level's fog color. The value is hex without a leading #,
        /// optionally followed by two more digits for density. Anything that
        /// fails to parse leaves the fog off entirely.
        /// </summary>
        private static void ApplyFog(string fog)
        {
            RenderSettings.fog = false;

            if (string.IsNullOrWhiteSpace(fog))
            {
                return;
            }

            try
            {
                string hex = fog.TrimStart('#');

                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

                float density = 0.013f;
                if (hex.Length > 6)
                {
                    density = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) / 5100f;
                }

                RenderSettings.fogColor = new Color(r / 255f, g / 255f, b / 255f, 1f);
                RenderSettings.fogDensity = density;
                RenderSettings.fog = true;
            }
            catch (Exception)
            {
                //A malformed value should just mean no fog, not a failed level load.
                RenderSettings.fog = false;
            }
        }


        /// <summary>
        /// Checks is gameobject is required for the game to work
        /// </summary>
        private bool requiredGameObject(GameObject target)
        {
            switch (target.name)
            {
                case "Player":
                case "Canvas":
                case "EventSystem":
                case "Main Camera":
                case "HitSounds":
                case "ImpactSprites":
                case "Cursor":
                case "Force Camera Ratios":
                case "Rewired Input Manager":
                    return true;
                default:
                    return false;
            }
        }

        void LegacyComponents()
        {
            GameObject pos = GameObject.Find("startPos");

            if (pos != null)
                pos.AddComponent<Components.PlayerStart>();
        }
        #endregion

        #region Fetching
        public bool ParseResponses(LevelLoader.Response[] responses, out LevelLoader.Response[] SuccesfulResponses)
        {
            Menu.LoadingError error = Menu.LoadingError.Instance;
            SuccesfulResponses = null;

            List<Response> succesfulResponses = new List<Response>();

            foreach (LevelLoader.Response response in responses)
            {
                switch (response.Message)
                {
                    case LevelLoader.Response.ResponseType.succes:
                        succesfulResponses.Add(response);
                        break;
                    case LevelLoader.Response.ResponseType.directoryNotFound:
                        error.AddError("Level directory created, Please restart the game to load maps", true);
                        break;
                    case LevelLoader.Response.ResponseType.metadataNotFound:
                    default:
                        error.AddError("An error occured: " + response.Message, true);
                        break;
                }
            }

            if (succesfulResponses.Count > 0)
            {
                SuccesfulResponses = succesfulResponses.ToArray();
                return true;
            }

            SuccesfulResponses = new Response[0];
            return true;
        }

        public Response[] FetchLevels()
        {
            List<Response> responses = new List<Response>();

            if (Directory.Exists(targetPath))
            {
                foreach(string path in Directory.GetFiles(targetPath).Where(name => !name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                {
                    LevelMetadata metadata;
                    long HeaderSize = 0;

                    if (path.EndsWith(".scene"))
                    {
                        /* Legacy map */
                        #region Legacy Map
                        string levelName = Path.GetFileNameWithoutExtension(path);
                        string author = string.Empty;
                        string description = levelName + " (Legacy Mode)";
                        var props = new Dictionary<string, string>();

                        if (File.Exists(Path.ChangeExtension(path, "txt")) || File.Exists(Path.ChangeExtension(path, "mdata")))
                        {
                            bool txtExtension = File.Exists(Path.ChangeExtension(path, "txt"));

                            string[] LevelData = File.ReadAllLines(Path.ChangeExtension(path, txtExtension ? "txt" : "mdata"));

                            foreach (string line in LevelData)
                            {
                                //A line without an = is malformed. Skipping it keeps one
                                //bad line from taking the whole level down.
                                string[] pair = line.Split('=');
                                if (pair.Length < 2) continue;

                                props[pair[0].Trim()] = pair[1].Trim();
                            }
                        }

                        //Legacy sidecars use lower case names for the same fields.
                        if (props.TryGetValue("credit", out var credit)) author = credit;
                        if (props.TryGetValue("description", out var legacyDescription)) description = legacyDescription;

                        metadata = new LevelMetadata(levelName, author, description, true, false, null, 0, props);
                        #endregion
                    }
                    else if (path.EndsWith(".glf"))
                    {
                        /* Normal map */
                        #region Normal Map
                        using (Stream stream = new FileStream(path, FileMode.Open))
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            if (!reader.ReadBytes(5).SequenceEqual(header))
                            {
                                responses.Add(new Response(Response.ResponseType.wrongFileType));
                                continue;
                            }

                            int metaDataLength = reader.ReadInt32();
                            byte[] compressedMetaData = new byte[metaDataLength];

                            reader.Read(compressedMetaData, 0, metaDataLength);
                            HeaderSize = stream.Position;

                            byte[] decompressedMetaData = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(compressedMetaData);

                            using (MemoryStream memStream = new MemoryStream(decompressedMetaData))
                            using (BinaryReader memReader = new BinaryReader(memStream))
                            {
                                var props = new Dictionary<string, string>();

                                int propertyCount = memReader.ReadInt32();
                                for (int i = 0; i < propertyCount; i++)
                                {
                                    string key = memReader.ReadString();
                                    string value = memReader.ReadString();
                                    props[key] = value;
                                }

                                string LevelName = props.TryGetValue("LevelName", out var n) ? n : "Untitled";
                                string Author = props.TryGetValue("Author", out var a) ? a : "Unknown";
                                string Description = props.TryGetValue("Description", out var d) ? d : "";
                                bool hasThumbnail = props.TryGetValue("HasThumbnail", out var ht) && bool.TryParse(ht, out var hasThumb) && hasThumb;
                                byte ThumbnailFormat = props.TryGetValue("ThumbnailFormat", out var tf) && byte.TryParse(tf, out var format) ? format : (byte)0;

                                //The thumbnail is the last thing in the stream, so whatever is left after the properties is the image.
                                byte[] Thumbnail = new byte[0];
                                if (hasThumbnail && memStream.Position < memStream.Length)
                                {
                                    Thumbnail = memReader.ReadBytes((int)(memStream.Length - memStream.Position));
                                }

                                metadata = new LevelMetadata(LevelName, Author, Description, false, hasThumbnail, Thumbnail, ThumbnailFormat, props);
                            }
                        }
                        #endregion
                    }
                    else continue;

                    responses.Add(new Response(Response.ResponseType.succes, metadata.LegacyMap, path, metadata, metadata.GetThumbnail(), HeaderSize));
                    continue;
                }
            } else
            {
                Directory.CreateDirectory(targetPath);
                responses.Add(new Response(Response.ResponseType.directoryNotFound));
            }

            return responses.ToArray();
        }
        #endregion
        
        #endregion

        #region Structs
        /// <summary>
        /// Contains necessary info for levels
        /// </summary>
        public struct Response
        {
            public ResponseType Message;
            public string LevelName;
            public string Author;
            public string Description;
            public long HeaderSize;
            public string LevelPath;
            public bool Legacy;
            public Texture2D Thumbnail;
            public LevelMetadata Metadata;

            public Response(ResponseType message)
            {
                Message = message;
                LevelName = "";
                LevelPath = "";
                Author = "";
                Description = "";
                Legacy = false;
                Thumbnail = null;
                HeaderSize = 0;
                Metadata = default;
            }

            public Response(ResponseType message, bool legacy, string levelPath, LevelMetadata metadata, Texture2D thumbnail, long headerSize)
            {
                Message = message;

                LevelName = metadata.LevelName;
                Author = metadata.Author;
                Description = metadata.Description;
                Metadata = metadata;

                LevelPath = levelPath;
                Legacy = legacy;
                Thumbnail = thumbnail;
                HeaderSize = headerSize;
            }

            public enum ResponseType
            {
                succes,
                metadataNotFound,
                directoryNotFound,
                wrongFileType
            }
        }
        #endregion
    }
}