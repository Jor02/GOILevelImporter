using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GOILevelImporter.Utils;
using I2.Loc;

namespace GOILevelImporter.Core.Menu
{
    static class AssetImporter
    {
        private const float CardTextPadding = 14f;

        public static LevelSelectScreen createLevelSelect(GameObject templateText, Transform parent)
        {
            UiAssets.Load();

            GameObject levelSelectScreen = LevelSelectView.Build(parent);

            Transform textArea = levelSelectScreen.transform.Find("TopArea/Level Area/Scroll Area/Viewport/Content/LevelSection/TextArea");
            GameObject levelText = GameObject.Instantiate(templateText, textArea);
            GameObject.Destroy(levelText.GetComponent<Localize>());

            TextMeshProUGUI levelTextMesh = levelText.GetComponent<TextMeshProUGUI>();
            LevelSelectView.ConfigureLevelNameLabel(levelTextMesh);

            RectTransform levelTextRect = levelText.GetComponent<RectTransform>();
            levelTextRect.SetStretchAnchor();
            levelTextRect.SetRect(new Rect(CardTextPadding, CardTextPadding, CardTextPadding, CardTextPadding));

            levelSelectScreen.transform.Find("TopArea/Level Area/Scroll Area/Viewport/Content/LevelSection").gameObject.SetActive(false);
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

