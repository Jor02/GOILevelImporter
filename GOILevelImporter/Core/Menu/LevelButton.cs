using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    class LevelButton : MonoBehaviour
    {
        public string levelName;
        public string levelPath;
        public string description;
        public string author;
        public bool legacy;
        public int id;
        public ulong headerSize;
        public bool hasThumbnail;
        public bool incompatible;
        public string builtWithVersion;
        public Sprite thumbnail;
        public Core.LevelMetadata metadata;

        public LevelButton Init(string levelPath, string levelName, string author, string description, int id, bool legacy, Texture2D thumbnail, Action<int> onClickEvent, long headerSize, Core.LevelMetadata metadata, bool incompatible = false, string builtWithVersion = null)
        {
            TextMeshProUGUI levelLabel = transform.Find("TextArea/Label").GetComponent<TextMeshProUGUI>();
            levelLabel.text = levelName;

            this.incompatible = incompatible;
            this.builtWithVersion = builtWithVersion;
            SetErrorBadge(incompatible);

            hasThumbnail = (thumbnail != null);
            if (!legacy) {
                this.thumbnail = (thumbnail != null) ? Sprite.Create(thumbnail, new Rect(0.0f, 0.0f, thumbnail.width, thumbnail.height), Vector2.one / 2) : UiAssets.MissingThumb;
                transform.Find("Thumbnail").GetComponent<Image>().sprite = this.thumbnail;
            } else
            {
                this.thumbnail = UiAssets.LegacyThumb;
                transform.Find("Thumbnail").GetComponent<Image>().sprite = this.thumbnail;
            }

            this.levelPath = levelPath;
            this.levelName = levelName;
            name = levelName;
            this.id = id;
            this.legacy = legacy;
            this.author = author;
            this.description = description;
            this.headerSize = (ulong)headerSize;
            this.metadata = metadata;

            Button button = GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => { onClickEvent.Invoke(this.id); });

            SetSelected(false);

            gameObject.SetActive(true);

            return this;
        }

        public void SetSelected(bool isSelected)
        {
            Button button = GetComponent<Button>();
            button.colors = LevelSelectView.CardColorBlock(isSelected);
        }

        /// <summary>
        /// Shows or hides the red badge on the card.
        /// </summary>
        private void SetErrorBadge(bool show)
        {
            Transform badge = transform.Find("TextArea/ErrorIcon");
            if (badge != null) badge.gameObject.SetActive(show);
        }

        public string IncompatibleReason =>
            "'" + levelName + "' was built with Unity " +
            (string.IsNullOrEmpty(builtWithVersion) ? "a different version" : builtWithVersion) +
            ", but this game runs Unity " + Application.unityVersion +
            ".\n\nRebuild the level with Unity " + Application.unityVersion + " to play it.";
    }
}
