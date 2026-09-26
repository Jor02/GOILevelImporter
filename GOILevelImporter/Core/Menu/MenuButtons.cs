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

        public void Init(Transform templateSource)
        {
            template = templateSource.gameObject;
        }

        public Transform AddButton(string name, UnityEngine.Events.UnityAction uAction)
        {
            GameObject tmpButton = Instantiate(template);
            tmpButton.name = name;

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
