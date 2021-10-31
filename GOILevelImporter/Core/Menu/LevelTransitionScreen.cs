using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    class LevelTransitionScreen : MonoBehaviour
    {
        public static LevelTransitionScreen Instance;

        public GameObject ThumbnailObject { get; private set; }
        public Image Thumbnail { get; private set; }
        public Text Name { get; private set; }
        public Text Author { get; private set; }

        public CanvasGroup Group { get; private set; }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Thumbnail = transform.Find("Transition/LevelInfo/Thumbnail/ThumbnailImage").GetComponent<Image>();
            ThumbnailObject = transform.Find("Transition/LevelInfo/Thumbnail").gameObject;

            Name = transform.Find("Transition/LevelInfo/Name/Title").GetComponent<Text>();
            Author = transform.Find("Transition/LevelInfo/Name/Author").GetComponent<Text>();

            Group = transform.Find("Transition/").GetComponent<CanvasGroup>();
            Group.alpha = 0;

            Instance = this;
        }

        public void FadeOut()
        {
            StartCoroutine(FadeOutTransition());
        }

        public void FadeIn()
        {
            StartCoroutine(FadeInTransition());
        }

        private IEnumerator FadeOutTransition()
        {
            for (float f = 0f; f <= 1.001f; f += 0.05f)
            {
                Group.alpha = f;
                yield return null;
            }
        }

        private IEnumerator FadeInTransition()
        {
            for (float f = 1f; f >= -0.001f; f -= 0.05f)
            {
                Group.alpha = f;
                yield return null;
            }
        }
    }
}
