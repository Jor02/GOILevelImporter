using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    /// <summary>
    /// Builds the screen that fades in while a level loads.
    /// </summary>
    internal static class LevelTransitionView
    {
        public static GameObject Build()
        {
            GameObject root = new GameObject("LevelTransition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = 15;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            RectTransform transition = UiFactory.Stretch("Transition", rootRect);
            transition.anchoredPosition = new Vector2(0.017578125f, -0.0029296875f);
            transition.sizeDelta = new Vector2(-0.6098633f, -0.39990234f);
            UiFactory.Panel(transition, Color.black);
            UiFactory.Group(transition);

            RectTransform info = UiFactory.Node("LevelInfo", transition, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one / 2f, new Vector2(0.0000019073f, 0f), new Vector2(907.9f, 637f));
            UiFactory.Column(info, TextAnchor.MiddleCenter, 0f, true, true, false, false);

            BuildThumbnail(info);
            BuildName(info);

            return root;
        }

        private static void BuildThumbnail(RectTransform info)
        {
            RectTransform thumbnail = UiFactory.Point("Thumbnail", info);
            thumbnail.sizeDelta = new Vector2(907.9f, 485.74f);

            RectTransform image = UiFactory.Stretch("ThumbnailImage", thumbnail);
            image.anchoredPosition = new Vector2(0.0018310547f, 0.24786377f);
            image.sizeDelta = new Vector2(-0.0018310547f, -0.4954834f);
            UiFactory.AddImage(image, UiAssets.MissingThumb, Color.white, Image.Type.Simple, false, false);
        }

        private static void BuildName(RectTransform info)
        {
            RectTransform name = UiFactory.Point("Name", info);
            name.sizeDelta = new Vector2(907.9f, 151.26f);

            RectTransform title = UiFactory.Node("Title", name, new Vector2(0.0515553f, 0.06789622f), new Vector2(0.9480001f, 0.21944812f), Vector2.one / 2f, new Vector2(-0.0019836426f, 69.93433f), new Vector2(-1.3018799f, 72.61496f));
            UiFactory.Label(title, "Level Name", UiFactory.TransitionTitle);

            RectTransform author = UiFactory.Node("Author", name, new Vector2(0.18400002f, 0f), new Vector2(0.95153373f, 0.08962026f), Vector2.one / 2f, new Vector2(-0.10482788f, 21.616066f), new Vector2(-0.4017334f, 43.232338f));
            UiFactory.Label(author, "By Someone", UiFactory.TransitionAuthor);
        }
    }
}
