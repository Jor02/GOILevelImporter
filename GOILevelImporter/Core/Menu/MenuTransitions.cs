using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GOILevelImporter.Core.Menu
{
    public static class MenuTransitions
    {
        private static Transform rock { get; set; }
        private static Vector3 rockStartPos { get; set; }
        private static RectTransform titleMask { get; set; }
        private static Vector3 titleStartPos { get; set; }
        private static GameObject menu { get; set; }
        private static GameObject levelMenu { get; set; }
        private static Loader loader { get; set; }
        private static bool isLevelSelectOpen { get; set; } = false;

        public static IEnumerator OnLevelSelectClickRoutine(GameObject menu, GameObject levelMenu)
        {
            if (isLevelSelectOpen) yield break;

            loader = Resources.FindObjectsOfTypeAll<Loader>()[0];

            loader.hammerAnim.Play("HammerDown");

            rock = loader.rock;
            rockStartPos = rock.position;

            titleMask = loader.titleMask;
            titleStartPos = titleMask.position;

            menu.SetActive(false);

            TextMeshProUGUI[] items = loader.menu.GetComponentsInChildren<TextMeshProUGUI>();
            for (float t = 0f; t <= 1.0001f; t += 0.05f)
            {
                titleMask.position = titleStartPos + new Vector3(Mathf.SmoothStep(0f, -67.5f, t), 0f, 0f);//new Vector3(Mathf.SmoothStep(0f, -9f, t), 0f, 0f);
                rock.position = rockStartPos + new Vector3(Mathf.SmoothStep(0f, -15f, t), 0f, 0f); //new Vector3(Mathf.SmoothStep(0f, -2f, t), 0f, 0f);
                
                /* Currently broken for some reason
                for (int i = 0; i < items.Length; i++)
                {
                    //For some reason the color of the text doesn't seem to change.
                    Color color = items[i].color;
                    color.a = Mathf.Clamp01(1f - t);
                    color.a *= color.a;
                    items[i].color = color;
                }*/
                yield return null;
            }
            levelMenu.SetActive(true);
            MenuTransitions.menu = menu;
            MenuTransitions.levelMenu = levelMenu;
            isLevelSelectOpen = true;
        }

        public static IEnumerator OnLevelSelectCloseRoutine()
        {
            if (!isLevelSelectOpen) yield break;


            titleMask.sizeDelta = new Vector2(0f, 216f);
            levelMenu.gameObject.SetActive(false);

            loader.hammerAnim.Play("HammerUp");

            for (float t = 1f; t >= -0.0001f; t -= 0.05f)
            {
                titleMask.position = titleStartPos + new Vector3(Mathf.SmoothStep(0f, -900f, t), 0f, 0f);
                rock.position = rockStartPos + new Vector3(Mathf.SmoothStep(0f, -15f, t), 0f, 0f);
                
                yield return null;
            }

            TextMeshProUGUI[] items = menu.GetComponentsInChildren<TextMeshProUGUI>();
            for (int i = 0; i < items.Length; i++)
            {
                Color c = items[i].color;
                c.a = 0;
                items[i].color = c;
            }
            menu.gameObject.SetActive(true);

            isLevelSelectOpen = false;
            yield break;
        }
    }
}