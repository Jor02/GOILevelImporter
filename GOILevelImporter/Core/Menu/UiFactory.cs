using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    /// <summary>
    /// Helpers for assembling UI out of code. The layout numbers in
    /// <see cref="LevelSelectView"/> and <see cref="LevelTransitionView"/> are transcribed
    /// from an old Unity design scene, so they read oddly precise on purpose.
    /// </summary>
    internal static class UiFactory
    {
        public static RectTransform Stretch(string name, Transform parent)
        {
            return Node(name, parent, Vector2.zero, Vector2.one, Vector2.one / 2f, Vector2.zero, Vector2.zero);
        }

        public static RectTransform Point(string name, Transform parent)
        {
            return Node(name, parent, Vector2.zero, Vector2.zero, Vector2.one / 2f, Vector2.zero, Vector2.zero);
        }

        public static RectTransform Node(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        public static Image Panel(RectTransform rect, Color color)
        {
            return AddImage(rect, null, color, Image.Type.Simple);
        }

        /// <summary>
        /// A nine-sliced rectangle. Defaults to the shared flat skin so every panel and
        /// button shares one square-cornered texture rather than picking its own.
        /// </summary>
        public static Image SlicedPanel(RectTransform rect, Color color, Sprite sprite = null)
        {
            return AddImage(rect, sprite ?? Skin, color, Image.Type.Sliced);
        }

        public static Image AddImage(RectTransform rect, Sprite sprite, Color color, Image.Type type, bool preserveAspect = false, bool raycastTarget = true)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static Text Label(RectTransform rect, string content, TextStyle style)
        {
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = style.size;
            text.fontStyle = style.fontStyle;
            text.resizeTextForBestFit = style.bestFit;
            text.resizeTextMinSize = style.minSize;
            text.resizeTextMaxSize = style.maxSize;
            text.alignment = style.alignment;
            text.supportRichText = style.richText;
            text.color = style.color;
            text.raycastTarget = style.raycastTarget;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1f;
            text.text = content;
            return text;
        }

        public static Button Button(RectTransform rect, ColorBlock colors)
        {
            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;
            button.targetGraphic = rect.GetComponent<Graphic>();
            return button;
        }
        
        public static ColorBlock ColorBlockFor(Color normal, Color highlighted, Color pressed, Color selected, bool disabled = true)
        {
            ColorBlock block = new ColorBlock();
            block.normalColor = normal;
            block.highlightedColor = highlighted;
            block.pressedColor = pressed;
            block.selectedColor = selected;
            block.disabledColor = DisabledTint;
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.1f;
            return block;
        }

        public static LayoutElement Sizing(RectTransform rect, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        public static VerticalLayoutGroup Column(RectTransform rect, TextAnchor alignment, float spacing, bool expandWidth, bool expandHeight, bool controlWidth, bool controlHeight)
        {
            VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.childForceExpandWidth = expandWidth;
            group.childForceExpandHeight = expandHeight;
            group.childControlWidth = controlWidth;
            group.childControlHeight = controlHeight;
            return group;
        }

        public static HorizontalLayoutGroup Row(RectTransform rect, TextAnchor alignment, float spacing, bool expandWidth, bool expandHeight, bool controlWidth, bool controlHeight)
        {
            HorizontalLayoutGroup group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.childForceExpandWidth = expandWidth;
            group.childForceExpandHeight = expandHeight;
            group.childControlWidth = controlWidth;
            group.childControlHeight = controlHeight;
            return group;
        }

        public static Mask Clip(RectTransform rect, bool showMaskGraphic)
        {
            Mask mask = rect.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = showMaskGraphic;
            return mask;
        }

        public static CanvasGroup Group(RectTransform rect)
        {
            return rect.gameObject.AddComponent<CanvasGroup>();
        }

        public static Font Font
        {
            get
            {
                if (fontTried) return font;

                fontTried = true;
                font = Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) Debug.LogError("[GOI Level Importer] Could not load Unity's built-in font, so level select labels will not render.");
                return font;
            }
        }

        public static Sprite Skin => BuildSkinSprite();
        private static Sprite BuildSkinSprite()
        {
            if (skinSprite != null) return skinSprite;

            const int size = 16;
            const float border = 4f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GOILevelImporter.SkinSprite",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();

            skinSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                Vector2.one / 2f,
                size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));

            return skinSprite;
        }

        public struct TextStyle
        {
            public TextStyle(int size, FontStyle fontStyle, bool bestFit, int minSize, int maxSize, TextAnchor alignment, bool richText = true, Color? color = null, bool raycastTarget = true)
            {
                this.size = size;
                this.fontStyle = fontStyle;
                this.bestFit = bestFit;
                this.minSize = minSize;
                this.maxSize = maxSize;
                this.alignment = alignment;
                this.richText = richText;
                this.color = color ?? Color.white;
                this.raycastTarget = raycastTarget;
            }

            public int size;
            public FontStyle fontStyle;
            public bool bestFit;
            public int minSize;
            public int maxSize;
            public TextAnchor alignment;
            public bool richText;
            public Color color;
            public bool raycastTarget;
        }

        public static readonly TextStyle Heading = new TextStyle(50, FontStyle.Bold, false, 8, 154, TextAnchor.MiddleCenter);
        public static readonly TextStyle ButtonLabel = new TextStyle(65, FontStyle.Bold, true, 10, 78, TextAnchor.MiddleCenter);
        public static readonly TextStyle DarkButtonLabel = new TextStyle(65, FontStyle.Bold, true, 10, 78, TextAnchor.MiddleCenter, true, Color.black);
        public static readonly TextStyle SidebarHeading = new TextStyle(50, FontStyle.Bold, true, 10, 100, TextAnchor.UpperLeft);
        public static readonly TextStyle SidebarSubheading = new TextStyle(50, FontStyle.Normal, true, 10, 100, TextAnchor.UpperLeft);
        public static readonly TextStyle SidebarBody = new TextStyle(28, FontStyle.Normal, true, 0, 35, TextAnchor.UpperLeft);
        public static readonly TextStyle Warning = new TextStyle(67, FontStyle.Bold, true, 0, 67, TextAnchor.MiddleCenter, false, Color.yellow);
        public static readonly TextStyle ErrorMessage = new TextStyle(80, FontStyle.Bold, false, 8, 154, TextAnchor.MiddleCenter);
        public static readonly TextStyle TransitionTitle = new TextStyle(50, FontStyle.Bold, true, 10, 100, TextAnchor.UpperCenter, true, Color.white, false);
        public static readonly TextStyle TransitionAuthor = new TextStyle(50, FontStyle.Normal, true, 10, 100, TextAnchor.UpperCenter, true, Color.white, false);

        public static readonly Color DisabledTint = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
        public static readonly Color PressedTint = new Color(0.33823532f, 0.33823532f, 0.33823532f, 0.816f);
        public static readonly Color HighlightedTint = new Color(0.61764705f, 0.61764705f, 0.61764705f, 0.741f);

        private static Font font;
        private static bool fontTried;
        private static Sprite skinSprite;
    }
}
