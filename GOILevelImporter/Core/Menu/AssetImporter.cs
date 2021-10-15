using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GOILevelImporter.Utils;
using I2.Loc;

namespace GOILevelImporter.Core.Menu
{
    static class AssetImporter
    {
        public static AssetBundle embededBundle;

        public static LevelSelectScreen createLevelSelect(GameObject templateText, Transform parent)
        {
            if (embededBundle == null)
                embededBundle = AssetBundle.LoadFromMemory(Properties.Resources.levelselect);

            GameObject levelSelectScreen = GameObject.Instantiate(embededBundle.LoadAsset<GameObject>("LevelSelect"));
            Transform textArea = levelSelectScreen.transform.Find("TopArea/Scroll Area/Viewport/Content/LevelSection/TextArea");
            levelSelectScreen.transform.SetParent(parent, false);

            GameObject levelText = GameObject.Instantiate(templateText, textArea);
            GameObject.Destroy(levelText.GetComponent<Localize>());

            TextMeshProUGUI levelTextMesh = levelText.GetComponent<TextMeshProUGUI>();
            levelTextMesh.alpha = 1;
            levelTextMesh.alignment = TextAlignmentOptions.Center;

            RectTransform levelTextRect = levelText.GetComponent<RectTransform>();
            levelTextRect.SetStretchAnchor();
            levelTextRect.SetRect(Rect.zero);

            levelSelectScreen.transform.Find("TopArea/Scroll Area/Viewport/Content/LevelSection").gameObject.SetActive(false);
            levelSelectScreen.transform.Find("TopArea/Description").gameObject.SetActive(true);
            levelSelectScreen.transform.Find("TopArea/Scroll Area/Viewport/ErrorScreen").gameObject.AddComponent<LoadingError>().Init(levelSelectScreen.transform.Find("TopArea/Scroll Area/Viewport/Content").gameObject);

            return levelSelectScreen.AddComponent<LevelSelectScreen>();
        }
    }
}
