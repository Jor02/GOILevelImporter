using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    class LoadingError : MonoBehaviour
    {
        public static LoadingError Instance { get; private set; }
        private Text errorText;
        private Transform okButton;

        private GameObject contentObject;

        private List<Error> errors = new List<Error>();

        public LoadingError()
        {
            Instance = this;
        }

        public void Init(GameObject content)
        {
            contentObject = content;

            gameObject.SetActive(false);

            errorText = transform.Find("Text").GetComponent<Text>();

            //Error button
            okButton = transform.Find("OK");

            Button OK = okButton.GetComponent<Button>();
            OK.onClick.AddListener(OKClick);

            if (errors.Count > 0)
                OKClick();
        }

        public void OKClick()
        {
            errors.RemoveAt(0);

            gameObject.SetActive(false);
            contentObject.SetActive(true);
            okButton.gameObject.SetActive(false);
            errorText.gameObject.SetActive(false);

            ShowNext();
        }

        public void ShowNext()
        {
            if (errors.Count > 0)
            {
                ShowError(errors[0].message, errors[0].showOk);
            }
        }

        public void AddError(string message, bool showOk)
        {
            errors.Add(new Error(message, showOk));
            if (errors.Count == 1) ShowNext();
        }

        public void ShowError(string message, bool showOk)
        {
            gameObject.SetActive(true);
            contentObject.SetActive(false);

            errorText.text = message;
            errorText.gameObject.SetActive(true);
            okButton.gameObject.SetActive(showOk);
        }

        struct Error
        {
            public string message;
            public bool showOk;

            public Error(string Message, bool ShowOk)
            {
                message = Message;
                showOk = ShowOk;
            }
        }
    }
}
