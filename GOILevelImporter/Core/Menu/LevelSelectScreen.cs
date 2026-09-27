using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    class LevelSelectScreen : MonoBehaviour
    {
        public GameObject levelScreen;
        public LevelButton templateLevelButton;
        public Transform content;
        public Button okButton;

        public GameObject sidebar;
        public Image sidebarThumbnail;
        public TextMeshProUGUI sidebarName;
        public TextMeshProUGUI sidebarAuthor;
        public TextMeshProUGUI sidebarText;

        public GameObject sidebarWarning;
        public TextMeshProUGUI sidebarWarningText;


        void Awake()
        {
            levelScreen = gameObject;

            templateLevelButton = transform.Find("TopArea/Level Area/Scroll Area/Viewport/Content/LevelSection").gameObject.AddComponent<LevelButton>();
            content = transform.Find("TopArea/Level Area/Scroll Area/Viewport/Content");
            sidebar = transform.Find("TopArea/Description").gameObject;
            sidebarThumbnail = transform.Find("TopArea/Description/Thumbnail").GetComponent<Image>(); ;
            sidebarText = transform.Find("TopArea/Description/Description/Text").GetComponent<TextMeshProUGUI>();
            sidebarName = transform.Find("TopArea/Description/Credits/Title").GetComponent<TextMeshProUGUI>();
            sidebarAuthor = transform.Find("TopArea/Description/Credits/Author").GetComponent<TextMeshProUGUI>();

            sidebarWarning = transform.Find("TopArea/Description/Description/Warning").gameObject;
            sidebarWarningText = transform.Find("TopArea/Description/Description/Warning/WarningText").GetComponent<TextMeshProUGUI>();

            Button closeSide = transform.Find("TopArea/Description/OK").GetComponent<Button>();
            closeSide.onClick = new Button.ButtonClickedEvent();
            closeSide.onClick.AddListener(() => sidebar.SetActive(false));
            closeSide.gameObject.SetActive(false);

            okButton = transform.Find("OK").GetComponent<Button>();
        }
    }
}
