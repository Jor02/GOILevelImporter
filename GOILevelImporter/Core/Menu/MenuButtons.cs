using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    class MenuButtons : MonoBehaviour
    {
        private GameObject template { get; set; }
        private Transform menu { get; set; }

        public void Init(Transform Base, Transform parent)
        {
            template = Base.gameObject;

            //template.transform.GetChild(0).GetComponent<TextMeshProUGUI>().enableAutoSizing = true;

            menu = parent;
        }

        public Transform AddButton(string name, UnityEngine.Events.UnityAction uAction)
        {
            GameObject tmpButton = Instantiate(template, menu);
            tmpButton.name = name;

            //tmpButton.GetComponent<Image>().sprite = Sprite.Create(thumb, new Rect(0.0f, 0.0f, thumb.width, thumb.height), new Vector2(0.5f, 0.5f));

            Transform buttonText = tmpButton.transform.GetChild(0);
            Destroy(buttonText.GetComponent<I2.Loc.Localize>());
            buttonText.GetComponent<TextMeshProUGUI>().text = name;
            
            Button btn = tmpButton.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(uAction);
            //btn.onClick.AddListener(() => StartCoroutine(LevelLoader.Instance.BeginLoadLevel(path, (ulong)HeaderSize)));

            return tmpButton.transform;
        }
    }
}
