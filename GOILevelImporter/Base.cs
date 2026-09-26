using BepInEx;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using GOILevelImporter.Utils;
using GOILevelImporter.Core.Menu;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Configuration;

namespace GOILevelImporter
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Base : BaseUnityPlugin
    {
        private static ConfigEntry<string> configSelectedLevel { get; set; }
        public static ConfigEntry<string> configTargetSceneLevel { get; set; }
        public static ConfigEntry<string> configTargetScene { get; set; }

        void Awake()
        {
            new GameObject("Level Loader", typeof(Core.LevelLoader));

            #region Configs
            configSelectedLevel = Config.Bind(
                "General",
                "currentLevel",
                string.Empty,
                "The current selected level"
            );

            configTargetSceneLevel = Config.Bind(
                "General",
                "targetSceneLevel",
                string.Empty,
                "Path of the level whose target scene is saved, so a resumed sub-scene is only used for the level that set it"
            );

            configTargetScene = Config.Bind(
                "General",
                "targetScene",
                string.Empty,
                "The scene to load after the level entry scene, set by a SwitchScene trigger"
            );
            #endregion

            SceneManager.sceneLoaded += OnSceneLoaded;

            //Patch
            Harmony harmony = new Harmony("com.jor02.goilevelimporter");
            harmony.PatchAll();

        }

        private void OnSceneLoaded(Scene target, LoadSceneMode mode)
        {
            if (target.name == "Loader") // If we loaded the main menu
            {
                Core.LevelLoader.Instance.Reset();
                StartCoroutine(SetupMenu());
            } else if (!Core.LevelLoader.Async && target.name == "Mian" && mode != LoadSceneMode.Additive && !isDefault) // If we restarted
            {
                StartCoroutine(Core.LevelLoader.Instance.LoadLevel());
            }
        }


        private LevelButton[] levelButtons { get; set; }
        private LevelSelectScreen levelScreen { get; set; }
        private IEnumerator SetupMenu()
        {
            yield return null;

            Transform UI = GameObject.Find("/Canvas").transform;
            Transform Column = UI.Find("Column");
            GameObject templateButton = UI.Find("Column/Quit").gameObject;

            //Include Level mod version in VERSION
            TextMeshProUGUI GOIVersion = UI.Find("Version").GetComponent<TextMeshProUGUI>();
            GOIVersion.overflowMode = TextOverflowModes.Overflow;
            GOIVersion.alignment = TextAlignmentOptions.TopRight;
            GOIVersion.enableAutoSizing = true;
            GOIVersion.text += "<br>Level Mod " + PluginInfo.FULLVERSION;

            #region Setup Custom Button
            //Create button generator
            MenuButtons menuButtonGenerator = new MenuButtons();
            menuButtonGenerator.Init(templateButton.transform, null); //null should be changed to level menu grid!
            #endregion

            #region Setup Level Screen
            levelScreen = AssetImporter.createLevelSelect(templateButton.transform.GetChild(0).gameObject, UI);
            levelScreen.okButton.onClick.AddListener(() => { 
                StartCoroutine(MenuTransitions.OnLevelSelectCloseRoutine());
                //levelScreen.sidebar.SetActive(false);
            });
            levelScreen.levelScreen.SetActive(false);
            #endregion

            #region Setup Level Transition
            if (LevelTransitionScreen.Instance == null)
                AssetImporter.createLevelTransition(UI);
            #endregion

            #region Setup Level Select Button
            //Main Level Select Button
            Transform SelectLevelButton = menuButtonGenerator.AddButton("Select Level", () =>
            {
                StartCoroutine(MenuTransitions.OnLevelSelectClickRoutine(Column.gameObject, levelScreen.levelScreen));
            });
            SelectLevelButton.SetParent(Column, false);
            SelectLevelButton.SetSiblingIndex(0);
            #endregion

            #region Load Levels
            LevelButton defaultMap = Instantiate(levelScreen.templateLevelButton, levelScreen.content).GetComponent<LevelButton>().Init(
                string.Empty,
                "Default Map",
                "Bennett Foddy",
                "Don't load any custom maps.",
                0,
                false,
                UiAssets.DefaultThumbTexture,
                UpdateSelectedLevel,
                0,
                new Core.LevelMetadata("Default Map", "Bennett Foddy", "Don't load any custom maps.", false, false, null, 0)
            );

            Core.LevelLoader.Response[] responses = Core.LevelLoader.Instance.FetchLevels();
            Core.LevelLoader.Response[] succesfulResponses;

            if (!Core.LevelLoader.Instance.ParseResponses(responses, out succesfulResponses)) yield break;

            levelButtons = new LevelButton[succesfulResponses.Length + 1];
            levelButtons[0] = defaultMap;

            int selectedMap = 0;
            for (int i = 0; i < succesfulResponses.Length; i++)
            {
                //Get response
                Core.LevelLoader.Response response = succesfulResponses[i];

                //Create button
                levelButtons[i+1] = Instantiate(levelScreen.templateLevelButton, levelScreen.content).GetComponent<LevelButton>().Init(response.LevelPath, response.LevelName, response.Author, response.Description, i+1, response.Legacy, response.Thumbnail, UpdateSelectedLevel, response.HeaderSize, response.Metadata);

                if (succesfulResponses[i].LevelPath == configSelectedLevel.GetSerializedValue()) selectedMap = i+1;
            }
            
            levelButtons[selectedMap].GetComponent<Button>().onClick.Invoke();
            #endregion
        }
        
        public static string levelPath { get; private set; }
        public static bool legacy { get; private set; }
        public static ulong levelHeaderSize { get; private set; }
        public static bool isDefault { get; private set; }

        /// <summary>
        /// Settings of the level currently selected in the menu. Read by the
        /// loader and the camera patches.
        /// </summary>
        public static Core.LevelMetadata metadata { get; private set; }

        public static string GetPendingScene(string bundlePath)
        {
            if (string.IsNullOrWhiteSpace(configTargetScene.Value) || configTargetSceneLevel.Value != bundlePath)
            {
                return string.Empty;
            }
            return configTargetScene.Value;
        }

        public static void ClearPendingScene()
        {
            configTargetScene.SetSerializedValue(string.Empty);
            configTargetSceneLevel.SetSerializedValue(string.Empty);
        }

        private void UpdateSelectedLevel(int id)
        {
            foreach (LevelButton b in levelButtons)
            {
                // Swap the whole colour block rather than the Image colour: a Selectable
                // rewrites its target graphic's colour on every state change, so a colour set
                // directly on the Image is discarded as soon as the card is focused or hovered.
                b.SetSelected(b.id == id);
            }

            LevelButton curButton = levelButtons[id];

            levelScreen.sidebarThumbnail.sprite = curButton.thumbnail;
            levelScreen.sidebarText.text = curButton.description;
            levelScreen.sidebarName.text = curButton.levelName;
            levelScreen.sidebarAuthor.text = string.IsNullOrWhiteSpace(curButton.author) ? "" : "By " + curButton.author;

            LevelTransitionScreen.Instance.Name.text = curButton.levelName;
            LevelTransitionScreen.Instance.Author.text = levelScreen.sidebarAuthor.text;

            LevelTransitionScreen.Instance.ThumbnailObject.SetActive(curButton.hasThumbnail);
            if (curButton.hasThumbnail)
                LevelTransitionScreen.Instance.Thumbnail.sprite = curButton.thumbnail;

            levelPath = curButton.levelPath;
            levelHeaderSize = curButton.headerSize;
            metadata = curButton.metadata;

            configSelectedLevel.SetSerializedValue(curButton.levelPath);

            isDefault = (id == 0);
            legacy = curButton.legacy;
            levelScreen.sidebarWarning.SetActive(curButton.legacy);
            if (curButton.legacy) levelScreen.sidebarWarningText.text = "Custom components don't currently\nwork on legacy maps.";
        }
    }
}
