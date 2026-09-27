using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    static class AssetImporter
    {
        public static LevelSelectScreen createLevelSelect(GameObject templateText, Transform parent)
        {
            UiAssets.Load();

            // Borrows the font the game's own menu is already using for its buttons,
            // since nothing guarantees this game has a default TMP font asset set.
            TextMeshProUGUI templateLabel = templateText.GetComponent<TextMeshProUGUI>();
            if (templateLabel != null) UiFactory.SetFont(templateLabel.font);

            GameObject levelSelectScreen = LevelSelectView.Build(parent);

            // The level list's width comes from a HorizontalLayoutGroup dividing
            // space between it and the sidebar, which Unity would normally only
            // resolve in its own end-of-frame pass. Forcing it now, before any
            // level buttons get created, means LevelGridLayout sees the level
            // list's real width on its very first calculation instead of
            // whatever default it started life with.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)levelSelectScreen.transform);

            levelSelectScreen.transform.Find("TopArea/Description").gameObject.SetActive(true);
            levelSelectScreen.transform.Find("TopArea/Level Area/Scroll Area/Viewport/ErrorScreen").gameObject.AddComponent<LoadingError>().Init(levelSelectScreen.transform.Find("TopArea/Level Area/Scroll Area/Viewport/Content").gameObject);

            return levelSelectScreen.AddComponent<LevelSelectScreen>();
        }

        public static LevelTransitionScreen createLevelTransition(Transform parent)
        {
            UiAssets.Load();

            return LevelTransitionView.Build().AddComponent<LevelTransitionScreen>();
        }
    }
}
