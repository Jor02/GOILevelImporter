using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    /// <summary>
    /// Adds the "Select Level" button and level-select screen to the vmain menu.
    /// </summary>
    class MainMenuController
    {
        public static MainMenuController Instance { get; } = new MainMenuController();

        private LevelButton[] levelButtons;
        private LevelSelectScreen levelScreen;

        private MainMenuController() { }

        public IEnumerator Setup()
        {
            yield return null;

            Transform ui = GameObject.Find("/Canvas").transform;
            Transform column = ui.Find("Column");
            GameObject templateButton = ui.Find("Column/Quit").gameObject;

            ShowModVersion(ui);

            MenuButtons menuButtonGenerator = new MenuButtons();
            menuButtonGenerator.Init(templateButton.transform);

            levelScreen = AssetImporter.createLevelSelect(templateButton.transform.GetChild(0).gameObject, ui);
            levelScreen.okButton.onClick.AddListener(() =>
            {
                Base.Instance.StartCoroutine(MenuTransitions.OnLevelSelectCloseRoutine());
            });
            levelScreen.levelScreen.SetActive(false);

            EnsureTransitionScreen(ui);

            AddSelectLevelButton(menuButtonGenerator, column, levelScreen.levelScreen);

            PopulateLevelList(levelScreen);
        }

        /// <summary>
        /// Creates the transition overlay used when a level starts. Split out
        /// so the --test-level path can skip the full menu (level list scan,
        /// thumbnails, buttons) and only build what the loader patch needs.
        /// </summary>
        public static void EnsureTransitionScreen(Transform ui)
        {
            if (LevelTransitionScreen.Instance == null)
            {
                AssetImporter.createLevelTransition(ui);
            }
        }

        private static void ShowModVersion(Transform ui)
        {
            TextMeshProUGUI versionLabel = ui.Find("Version").GetComponent<TextMeshProUGUI>();
            versionLabel.overflowMode = TextOverflowModes.Overflow;
            versionLabel.alignment = TextAlignmentOptions.TopRight;
            versionLabel.enableAutoSizing = true;
            versionLabel.text += "<br>Level Mod " + PluginInfo.FULLVERSION;
        }

        private void AddSelectLevelButton(MenuButtons menuButtonGenerator, Transform column, GameObject levelSelectScreen)
        {
            Transform selectLevelButton = menuButtonGenerator.AddButton("Select Level", () =>
            {
                Base.Instance.StartCoroutine(MenuTransitions.OnLevelSelectClickRoutine(column.gameObject, levelSelectScreen));
            });
            selectLevelButton.SetParent(column, false);
            selectLevelButton.SetSiblingIndex(0);
        }

        private void PopulateLevelList(LevelSelectScreen screen)
        {
            LevelButton defaultMap = Object.Instantiate(screen.templateLevelButton, screen.content).GetComponent<LevelButton>().Init(
                string.Empty,
                "Default Map",
                "Bennett Foddy",
                "The normal game.",
                0,
                false,
                UiAssets.DefaultThumbTexture,
                SelectLevel,
                0,
                new LevelMetadata("Default Map", "Bennett Foddy", "The normal game.", false, false, null, 0)
            );

            LevelFileScanner.Response[] responses = LevelFileScanner.Scan(LevelLoader.TargetPath);
            if (!LevelFileScanner.TrySplitResults(responses, out var successfulResponses)) return;

            levelButtons = new LevelButton[successfulResponses.Length + 1];
            levelButtons[0] = defaultMap;

            int selectedMap = 0;
            for (int i = 0; i < successfulResponses.Length; i++)
            {
                LevelFileScanner.Response response = successfulResponses[i];

                levelButtons[i + 1] = Object.Instantiate(screen.templateLevelButton, screen.content).GetComponent<LevelButton>().Init(
                    response.LevelPath, response.LevelName, response.Author, response.Description,
                    i + 1, response.Legacy, response.Thumbnail, SelectLevel, response.HeaderSize, response.Metadata);

                if (response.LevelPath == LevelSelectionState.SavedLevelPath) selectedMap = i + 1;
            }

            levelButtons[selectedMap].GetComponent<Button>().onClick.Invoke();
        }

        private void SelectLevel(int id)
        {
            foreach (LevelButton button in levelButtons)
            {
                button.SetSelected(button.id == id);
            }

            LevelButton picked = levelButtons[id];

            levelScreen.sidebarThumbnail.sprite = picked.thumbnail;
            levelScreen.sidebarText.text = picked.description;
            levelScreen.sidebarName.text = picked.levelName;
            levelScreen.sidebarAuthor.text = string.IsNullOrWhiteSpace(picked.author) ? "" : "By " + picked.author;

            LevelTransitionScreen.Instance.Name.text = picked.levelName;
            LevelTransitionScreen.Instance.Author.text = levelScreen.sidebarAuthor.text;

            LevelTransitionScreen.Instance.ThumbnailObject.SetActive(picked.hasThumbnail);
            if (picked.hasThumbnail)
            {
                LevelTransitionScreen.Instance.Thumbnail.sprite = picked.thumbnail;
            }

            LevelSelectionState.Select(id == 0, picked.levelPath, picked.legacy, picked.headerSize, picked.metadata);

            levelScreen.sidebarWarning.SetActive(picked.legacy);
            if (picked.legacy)
            {
                levelScreen.sidebarWarningText.text = "Custom components don't currently\nwork on legacy maps.";
            }
        }
    }
}
