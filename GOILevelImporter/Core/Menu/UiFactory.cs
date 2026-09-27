using System.Collections.Generic;
using TMPro;
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
        /// A nine-sliced rectangle. Defaults to the shared flat skin so every plain panel and
        /// button shares one square-cornered texture rather than picking its own.
        /// </summary>
        public static Image SlicedPanel(RectTransform rect, Color color, Sprite sprite = null)
        {
            return AddImage(rect, sprite ?? Skin, color, Image.Type.Sliced);
        }

        /// <summary>
        /// A nine-sliced rectangle with a flat diagonal cut taken out of the bottom-left
        /// and top-right corners, for the angular accent look. <paramref name="notchSize"/>
        /// is how far the cut reaches into the corner, in UI units.
        /// </summary>
        public static Image CutCornerPanel(RectTransform rect, Color color, float notchSize)
        {
            return AddImage(rect, NotchedSkin(notchSize), color, Image.Type.Sliced);
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
        
        public static TextMeshProUGUI Label(RectTransform rect, string content, TextStyle style)
        {
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font;
            if (Font != null) label.fontSharedMaterial = Font.material;
            label.fontSize = style.size;
            label.fontStyle = style.fontStyle;
            label.enableAutoSizing = true;
            label.fontSizeMin = style.minSize;
            label.fontSizeMax = style.maxSize;
            label.alignment = style.alignment;
            label.richText = style.richText;
            label.color = style.color;
            label.raycastTarget = style.raycastTarget;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Truncate;
            label.text = content;
            return label;
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

        /// <summary>
        /// The TMP font asset every label is built with. Set once, from a font asset the
        /// game's own menu is already using, before building any screen: there's no
        /// guarantee this game configured a default TMP font asset to fall back on.
        /// </summary>
        public static void SetFont(TMP_FontAsset value)
        {
            font = value;
        }

        public static TMP_FontAsset Font
        {
            get
            {
                if (font != null) return font;

                font = TMP_Settings.defaultFontAsset;
                if (font == null) Debug.LogError("[GOI Level Importer] No TextMeshPro font asset available, so level select labels will not render.");
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

        public static Sprite NotchedSkin(float notchSize)
        {
            int notch = Mathf.Max(4, Mathf.RoundToInt(notchSize));
            if (notchedSkins.TryGetValue(notch, out Sprite cached)) return cached;

            // A flat sliver between the cut and the sprite's own edge keeps the diagonal
            // from touching the seam where the corner tile meets the stretched middle.
            const int margin = 6;
            int border = notch + margin;
            int size = border * 2 + 8;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GOILevelImporter.NotchedSkin" + notch,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte alpha = (byte)(255f * CornerCoverage(x, y, size, notch));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            const float pixelsPerUnit = 100f;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                Vector2.one / 2f,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));

            notchedSkins[notch] = sprite;
            return sprite;
        }

        private static float CornerCoverage(int x, int y, int size, int notch)
        {
            float bottomLeftCut = x + y - notch;
            float topRightCut = (size - 1 - x) + (size - 1 - y) - notch;

            float distanceFromCut = Mathf.Min(bottomLeftCut, topRightCut);
            return Mathf.Clamp01(distanceFromCut + 0.5f);
        }

        public struct TextStyle
        {
            public TextStyle(float size, FontStyles fontStyle, float minSize, float maxSize, TextAlignmentOptions alignment, bool richText = true, Color? color = null, bool raycastTarget = true)
            {
                this.size = size;
                this.fontStyle = fontStyle;
                this.minSize = minSize;
                this.maxSize = maxSize;
                this.alignment = alignment;
                this.richText = richText;
                this.color = color ?? Color.white;
                this.raycastTarget = raycastTarget;
            }

            public float size;
            public FontStyles fontStyle;
            public float minSize;
            public float maxSize;
            public TextAlignmentOptions alignment;
            public bool richText;
            public Color color;
            public bool raycastTarget;
        }

        public static readonly TextStyle Heading = new TextStyle(44f, FontStyles.Bold, 16f, 48f, TextAlignmentOptions.Center);
        public static readonly TextStyle ButtonLabel = new TextStyle(30f, FontStyles.Bold, 10f, 34f, TextAlignmentOptions.Center);
        public static readonly TextStyle DarkButtonLabel = new TextStyle(30f, FontStyles.Bold, 10f, 34f, TextAlignmentOptions.Center, true, Color.black);
        public static readonly TextStyle CardTitle = new TextStyle(26f, FontStyles.Bold, 12f, 30f, TextAlignmentOptions.Center);
        public static readonly TextStyle SidebarHeading = new TextStyle(34f, FontStyles.Bold, 14f, 38f, TextAlignmentOptions.TopLeft);
        public static readonly TextStyle SidebarSubheading = new TextStyle(24f, FontStyles.Normal, 12f, 28f, TextAlignmentOptions.TopLeft, true, new Color(1f, 1f, 1f, 0.7f));
        public static readonly TextStyle SidebarBody = new TextStyle(22f, FontStyles.Normal, 12f, 24f, TextAlignmentOptions.TopLeft);
        public static readonly TextStyle Warning = new TextStyle(24f, FontStyles.Bold, 12f, 28f, TextAlignmentOptions.Center, false, new Color(1f, 0.82f, 0.4f));
        public static readonly TextStyle ErrorMessage = new TextStyle(30f, FontStyles.Bold, 14f, 40f, TextAlignmentOptions.Center);
        public static readonly TextStyle TransitionTitle = new TextStyle(34f, FontStyles.Bold, 14f, 38f, TextAlignmentOptions.Top, true, Color.white, false);
        public static readonly TextStyle TransitionAuthor = new TextStyle(24f, FontStyles.Normal, 12f, 28f, TextAlignmentOptions.Top, true, new Color(1f, 1f, 1f, 0.75f), false);

        public static readonly Color DisabledTint = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
        public static readonly Color PressedTint = new Color(0.33823532f, 0.33823532f, 0.33823532f, 0.816f);
        public static readonly Color HighlightedTint = new Color(0.61764705f, 0.61764705f, 0.61764705f, 0.741f);

        private static TMP_FontAsset font;
        private static Sprite skinSprite;
        private static readonly Dictionary<int, Sprite> notchedSkins = new Dictionary<int, Sprite>();
    }
}
