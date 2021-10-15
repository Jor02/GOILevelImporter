using UnityEngine;

namespace GOILevelImporter.Utils
{
    /// <summary>
    /// Based on http://answers.unity.com/answers/1610964/view.html
    /// </summary>
    public static class RectTransformExtensions
    {
        public static void SetStretchAnchor(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
        }

        public static void SetRect(this RectTransform rt, Rect targetSize)
        {
            rt.SetLeft(targetSize.x);
            rt.SetBottom(targetSize.y);
            rt.SetRight(targetSize.width);
            rt.SetTop(targetSize.height);
        }

        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }
    }

}
