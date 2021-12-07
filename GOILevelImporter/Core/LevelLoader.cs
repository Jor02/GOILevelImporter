using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using Newtonsoft.Json;
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
        public static string Scene { get; set; }
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

        public IEnumerator LoadLevel()
        {
            yield return new WaitForEndOfFrame();
            Physics2D.simulationMode = SimulationMode2D.Script;
            Time.timeScale = 0;

            foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!requiredGameObject(gameObject))
                {
                    Destroy(gameObject);
                }
            }

            //Creates helper class for custom components
            new GameObject("ComponentHelper", typeof(Components.ComponentHelper));

            //Disable fog controllers
            GameObject bgCam = GameObject.Find("/Main Camera/BGCamera");
            Destroy(bgCam.GetComponent<FogControl>());
            Destroy(bgCam.GetComponent<FogVolumeRenderer>());

            //Fixes error in PoseControl
            Resources.FindObjectsOfTypeAll<PoseControl>()[0].SetPrivateFieldValue("interestingItems", new Transform[0]);

            AsyncOperation asyncLoad =  SceneManager.LoadSceneAsync(currectBundle.GetAllScenePaths()[0], LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
                yield return null;
            yield return new WaitForEndOfFrame();

            //Add modded components
            if (!Legacy)
                AddComponents();
            else
                LegacyComponents();

            Menu.LevelTransitionScreen.Instance.FadeIn();
            Time.timeScale = 1;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
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

        /// <summary>
        /// Adds all modded components back
        /// </summary>
        void AddComponents()
        {
            LevelSettings levelData;

            using (FileStream stream = new FileStream(currentBundlePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(header.Length, SeekOrigin.Current);
                int metaDataLength = reader.ReadInt32();

                stream.Seek(metaDataLength, SeekOrigin.Current);
                int levelDataLength = reader.ReadInt32();

                byte[] compressedLevelData = reader.ReadBytes(levelDataLength);
                byte[] uncompressedLevelData = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(compressedLevelData);

                string levelDataJson = System.Text.Encoding.UTF8.GetString(uncompressedLevelData);

                levelData = JsonConvert.DeserializeObject<LevelSettings>(levelDataJson);
            }

            foreach (LevelSettings.ScriptedObject targetObject in levelData.objects)
            {
                GameObject targetGameObject = GameObject.Find(targetObject.path);
                if (!targetGameObject) continue;

                foreach (LevelSettings.ObjectComponent component in targetObject.components)
                {
                    Type type = Type.GetType(component.typeName);
                    if (type == null) continue;

                    Component addedComponent = targetGameObject.AddComponent(type);

                    foreach (LevelSettings.Member member in component.members)
                    {
                        FieldInfo field;
                        Type memberType;

                        try
                        {
                            field = type.GetField(member.name);
                            memberType = field.FieldType;
                        }
                        catch
                        {
                            Debug.LogWarning($"{addedComponent.name} doesn't have field {member.name}");
                            continue;
                        }

                        try
                        {
                            if (memberType == typeof(int))
                            {
                                field.SetValue(addedComponent, int.Parse(member.value));
                            }
                            else if (memberType == typeof(float))
                            {
                                field.SetValue(addedComponent, float.Parse(member.value));
                            }
                            else if (memberType == typeof(string))
                            {
                                field.SetValue(addedComponent, member.value);
                            }
                            else if (memberType == typeof(bool))
                            {
                                field.SetValue(addedComponent, bool.Parse(member.value));
                            }
                            else if (memberType.IsEnum)
                            {
                                field.SetValue(addedComponent, Enum.Parse(memberType, member.value));
                            }
                        }
                        catch
                        {
                            Debug.LogWarning($"Failed to parse member {field.Name} of {addedComponent.name}");
                        }
                    }

                    ((Components.ComponentBase)addedComponent).StartComponent();
                }
            }
        }

        void LegacyComponents()
        {
            GameObject pos = GameObject.Find("startPos");

            Debug.LogWarning(pos != null);
            if (pos != null)
                pos.AddComponent<Components.PlayerStart>().StartComponent();
            
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
                        error.AddError("Level directory created, Please restart the game to load maps", false);
                        return false;
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

            error.AddError("No levels found", false);
            return false;
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

                        if (File.Exists(Path.ChangeExtension(path, "txt")) || File.Exists(Path.ChangeExtension(path, "mdata")))
                        {
                            bool txtExtension = File.Exists(Path.ChangeExtension(path, "txt"));

                            string[] LevelData = File.ReadAllLines(Path.ChangeExtension(path, txtExtension ? "txt" : "mdata"));

                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                            dictionary = (from l in LevelData select l.Split('=')).ToDictionary((string[] s) => s[0].Trim(), (string[] s) => s[1].Trim().ToLower());

                            if (dictionary.ContainsKey("credit")) author = dictionary["credit"];
                        }

                        metadata = new LevelMetadata(levelName, author, levelName + " (Legacy Mode)", true, false, null, 0);
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

                            int levelDataLength = reader.ReadInt32();

                            HeaderSize = stream.Position + levelDataLength;

                            byte[] decompressedMetaData = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(compressedMetaData);

                            using (MemoryStream memStream = new MemoryStream(decompressedMetaData))
                            using (BinaryReader memReader = new BinaryReader(memStream))
                            {
                                string LevelName = memReader.ReadString();
                                string Author = memReader.ReadString();
                                string Description = memReader.ReadString();
                                bool hasThumbnail = memReader.ReadBoolean();
                                byte ThumbnailFormat = memReader.ReadByte();
                                byte[] Thumbnail = memReader.ReadBytes((int)(memStream.Length - memStream.Position));
                                metadata = new LevelMetadata(LevelName, Author, Description, false, hasThumbnail, Thumbnail, ThumbnailFormat);
                            }
                        }
                        #endregion
                    }
                    else continue;

                    responses.Add(new Response(Response.ResponseType.succes, metadata.LegacyMap, path, metadata.LevelName, metadata.Author, metadata.Description, metadata.GetThumbnail(), HeaderSize));
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
            }

            public Response(ResponseType message, bool legacy, string levelPath, string levelName, string author, string description, Texture2D thumbnail, long headerSize)
            {
                Message = message;   

                LevelName = levelName;
                Author = author;
                Description = description;

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