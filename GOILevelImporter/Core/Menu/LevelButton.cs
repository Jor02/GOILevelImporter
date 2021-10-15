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
        public Sprite thumbnail;
        public Image background;

        public LevelButton Init(string levelPath, string levelName, string author, string description, int id, bool legacy, Texture2D thumbnail, Action<int> onClickEvent, long headerSize)
        {
            transform.Find("TextArea").GetChild(0).GetComponent<TextMeshProUGUI>().text = levelName;
            
            if (!legacy) {
                this.thumbnail = (thumbnail != null) ? Sprite.Create(thumbnail, new Rect(0.0f, 0.0f, thumbnail.width, thumbnail.height), Vector2.one / 2) : AssetImporter.embededBundle.LoadAsset<Sprite>("MissingThumb");
                transform.Find("Thumbnail").GetComponent<Image>().sprite = this.thumbnail;
            } else
            {
                this.thumbnail = AssetImporter.embededBundle.LoadAsset<Sprite>("LegacyThumb");
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
            background = GetComponent<Image>();

            Button button = GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => { onClickEvent.Invoke(this.id); });

            gameObject.SetActive(true);

            return this;
        }
    }
}
