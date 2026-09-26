using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    internal static class LevelSelectView
    {
        public static GameObject Build(Transform parent)
        {
            RectTransform root = UiFactory.Node("LevelSelect", parent, Vector2.zero, Vector2.one, Vector2.one / 2f, Vector2.zero, new Vector2(-100f, -100f));
            UiFactory.AddImage(root, null, new Color(0f, 0f, 0f, 0.4f), Image.Type.Sliced);

            RectTransform topArea = UiFactory.Stretch("TopArea", root);
            topArea.anchoredPosition = new Vector2(0f, 51.425003f);
            topArea.sizeDelta = new Vector2(0f, -102.850006f);
            UiFactory.Row(topArea, TextAnchor.UpperCenter, 0f, false, true, true, true);
            BuildCloseButton(root);
            BuildLevelArea(topArea);
            RectTransform sidebar = BuildSidebar(topArea);

            // The sidebar only appears once the player picks a level.
            sidebar.gameObject.SetActive(false);

            return root.gameObject;
        }

        private static RectTransform BuildLevelArea(RectTransform topArea)
        {
            RectTransform levelArea = UiFactory.Point("Level Area", topArea);
            UiFactory.Sizing(levelArea, flexibleWidth: 1f);
            UiFactory.Column(levelArea, TextAnchor.UpperLeft, 0f, true, false, true, true);

            BuildButtonArea(levelArea);
            BuildScrollArea(levelArea);

            return levelArea;
        }

        private static void BuildButtonArea(RectTransform levelArea)
        {
            RectTransform buttonArea = UiFactory.Point("ButtonArea", levelArea);
            UiFactory.Panel(buttonArea, new Color(0f, 0f, 0f, 0.209f));
            UiFactory.Sizing(buttonArea, preferredHeight: 60f);

            RectTransform heading = UiFactory.Node("Text", buttonArea, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.one / 2f, new Vector2(172f, -30f), new Vector2(343.9f, 60f));
            UiFactory.Label(heading, "Level select", UiFactory.Heading);

            RectTransform refresh = UiFactory.Node("Refresh", buttonArea, RightEdge, RightEdge, RightEdgePivot, new Vector2(-16f, 0f), RefreshSize);
            UiFactory.SlicedPanel(refresh, new Color(0f, 0f, 0f, 0.866f));
            UiFactory.Button(refresh, UiFactory.ColorBlockFor(DarkButtonTint, UiFactory.HighlightedTint, UiFactory.PressedTint, UiFactory.HighlightedTint));

            RectTransform icon = UiFactory.Stretch("Image", refresh);
            icon.sizeDelta = new Vector2(-9.86f, -9.86f);
            UiFactory.AddImage(icon, UiAssets.RefreshIcon, Color.white, Image.Type.Simple);
        }

        private static void BuildScrollArea(RectTransform levelArea)
        {
            RectTransform scrollArea = UiFactory.Point("Scroll Area", levelArea);
            UiFactory.Panel(scrollArea, new Color(0f, 0f, 0f, 0.11f));
            UiFactory.Sizing(scrollArea, flexibleHeight: 1f);

            RectTransform viewport = BuildScrollViewport(scrollArea);
            Scrollbar scrollbar = BuildScrollbar(scrollArea);

            ScrollRect scrollRect = scrollArea.gameObject.AddComponent<ScrollRect>();
            scrollRect.content = (RectTransform)viewport.Find("Content");
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 100f;
            scrollRect.viewport = viewport;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -3f;
        }

        private static RectTransform BuildScrollViewport(RectTransform scrollArea)
        {
            RectTransform viewport = UiFactory.Node("Viewport", scrollArea, Vector2.zero, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            UiFactory.SlicedPanel(viewport, Color.white);
            UiFactory.Clip(viewport, false);

            BuildErrorScreen(viewport);
            BuildLevelGrid(viewport);

            return viewport;
        }

        private static void BuildLevelGrid(RectTransform viewport)
        {
            RectTransform content = UiFactory.Node("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);

            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(13, 13, 13, 0);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.cellSize = new Vector2(CardCellWidth, CardCellHeight);
            grid.spacing = new Vector2(6.75f, 6.75f);

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildLevelButtonTemplate(content);
        }

        private static void BuildLevelButtonTemplate(RectTransform content)
        {
            RectTransform button = UiFactory.Point("LevelSection", content);
            UiFactory.SlicedPanel(button, CardTint);
            UiFactory.Button(button, CardColorBlock(false));

            float thumbnailWidth = CardCellWidth - (CardPadding * 2f);
            float thumbnailHeight = thumbnailWidth / ThumbnailAspect;

            RectTransform thumbnail = UiFactory.Stretch("Thumbnail", button);
            thumbnail.anchorMax = new Vector2(1f, 1f);
            thumbnail.pivot = new Vector2(0.5f, 1f);
            thumbnail.anchoredPosition = new Vector2(0f, -CardPadding);
            thumbnail.sizeDelta = new Vector2(-(CardPadding * 2f), -(CardPadding * 2f + thumbnailHeight));
            UiFactory.AddImage(thumbnail, UiAssets.MissingThumb, Color.white, Image.Type.Simple, false, false);
            UiFactory.Sizing(thumbnail, preferredWidth: 500f, preferredHeight: 278.95f);

            RectTransform textArea = UiFactory.Stretch("TextArea", button);
            textArea.anchorMax = new Vector2(1f, 1f);
            textArea.pivot = new Vector2(0.5f, 1f);
            textArea.anchoredPosition = new Vector2(0f, -(CardPadding * 2f + thumbnailHeight));
            textArea.sizeDelta = new Vector2(-(CardPadding * 2f), -((CardPadding * 2f + thumbnailHeight) * 2f));
            button.gameObject.SetActive(false);
        }

        public static void ConfigureLevelNameLabel(TextMeshProUGUI label)
        {
            label.alpha = 1f;

            label.alignment = TextAlignmentOptions.TopLeft;
            label.horizontalAlignment = HorizontalAlignmentOptions.Left;
            label.verticalAlignment = VerticalAlignmentOptions.Top;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
        }

        public static ColorBlock CardColorBlock(bool isSelected)
        {
            return UiFactory.ColorBlockFor(
                isSelected ? SelectedTint : CardTint,
                isSelected ? SelectedHoverTint : CardHoverTint,
                isSelected ? SelectedPressedTint : CardPressedTint,
                isSelected ? SelectedFocusTint : CardFocusTint);
        }

        private static void BuildErrorScreen(RectTransform viewport)
        {
            RectTransform errorScreen = UiFactory.Stretch("ErrorScreen", viewport);
            errorScreen.sizeDelta = new Vector2(-200f, -200f);
            UiFactory.Panel(errorScreen, new Color(0f, 0f, 0f, 0.472f));

            RectTransform message = UiFactory.Stretch("Text", errorScreen);
            message.sizeDelta = new Vector2(-42.254997f, -42.255f);
            UiFactory.Label(message, "Error Message", UiFactory.ErrorMessage);

            BuildButton(errorScreen, "OK", new Vector2(-0.6999512f, 0.50006104f), new Vector2(0.30004883f, 0.7000122f), RefreshAnchorMin, RefreshAnchorMax, DarkButtonTint, UiFactory.ButtonLabel);
        }

        private static Scrollbar BuildScrollbar(RectTransform scrollArea)
        {
            RectTransform scrollbarRect = UiFactory.Node("Scrollbar Vertical", scrollArea, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(ScrollbarWidth, 0f));
            UiFactory.SlicedPanel(scrollbarRect, new Color(0f, 0f, 0f, 0.35f));

            RectTransform slidingArea = UiFactory.Stretch("Sliding Area", scrollbarRect);
            slidingArea.sizeDelta = new Vector2(-ScrollbarInset, -ScrollbarInset);

            RectTransform handle = UiFactory.Stretch("Handle", slidingArea);
            handle.sizeDelta = Vector2.zero;
            UiFactory.SlicedPanel(handle, new Color(1f, 1f, 1f, 0.55f));

            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Graphic>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.transition = Selectable.Transition.ColorTint;
            scrollbar.colors = UiFactory.ColorBlockFor(new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.75f), new Color(1f, 1f, 1f, 0.9f), new Color(1f, 1f, 1f, 0.75f));
            scrollbar.size = 0.25f;

            return scrollbar;
        }

        private static RectTransform BuildSidebar(RectTransform topArea)
        {
            RectTransform sidebar = UiFactory.Point("Description", topArea);
            UiFactory.Panel(sidebar, new Color(0f, 0f, 0f, 0.234f));
            UiFactory.Clip(sidebar, true);
            UiFactory.Sizing(sidebar, preferredWidth: 500f);
            UiFactory.Column(sidebar, TextAnchor.UpperCenter, 10f, false, false, true, true);

            RectTransform thumbnail = UiFactory.Point("Thumbnail", sidebar);
            UiFactory.AddImage(thumbnail, UiAssets.MissingThumb, Color.white, Image.Type.Simple, false, false);
            UiFactory.Sizing(thumbnail, preferredWidth: 500f, preferredHeight: 278.95f);

            BuildCredits(sidebar);
            BuildSidebarBody(sidebar);
            BuildButton(sidebar, "CLOSE", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, DarkButtonTint, UiFactory.ButtonLabel);

            return sidebar;
        }

        private static void BuildCredits(RectTransform sidebar)
        {
            RectTransform credits = UiFactory.Point("Credits", sidebar);
            UiFactory.Sizing(credits, preferredWidth: 462.4f, preferredHeight: 85f);

            RectTransform title = UiFactory.Node("Title", credits, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.one / 2f, new Vector2(0f, -27.5f), new Vector2(0f, 54.30005f));
            UiFactory.Label(title, "Level Name", UiFactory.SidebarHeading);

            RectTransform author = UiFactory.Node("Author", credits, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.one / 2f, new Vector2(18.699963f, 22.649967f), new Vector2(-34.9f, 32.100037f));
            UiFactory.Label(author, "By Someone", UiFactory.SidebarSubheading);
        }

        private static void BuildSidebarBody(RectTransform sidebar)
        {
            RectTransform body = UiFactory.Point("Description", sidebar);
            UiFactory.Panel(body, new Color(0f, 0f, 0f, 0.234f));
            UiFactory.Sizing(body, preferredWidth: 462.4f, flexibleHeight: 1f);

            RectTransform text = UiFactory.Stretch("Text", body);
            text.anchoredPosition = new Vector2(0f, WarningHeight / 2f);
            text.sizeDelta = new Vector2(-20.26f, -(20.26f * 2f + WarningHeight));
            UiFactory.Label(text, string.Empty, UiFactory.SidebarBody);

            BuildWarning(body);
        }

        private static void BuildWarning(RectTransform body)
        {
            RectTransform warning = UiFactory.Stretch("Warning", body);
            warning.pivot = new Vector2(0.5f, 0f);
            warning.anchoredPosition = Vector2.zero;
            warning.sizeDelta = new Vector2(0f, WarningHeight);
            UiFactory.Panel(warning, new Color(0f, 0f, 0f, 0.428f));

            RectTransform symbol = UiFactory.Node("WarningSymbol", warning, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(WarningHeight, WarningHeight));
            UiFactory.AddImage(symbol, UiAssets.WarningIcon, Color.white, Image.Type.Simple, true);

            RectTransform text = UiFactory.Stretch("WarningText", warning);
            text.anchoredPosition = new Vector2(WarningHeight / 2f + 8f, 0f);
            text.sizeDelta = new Vector2(-(WarningHeight + 24f), 0f);
            UiFactory.Label(text, "This is a warning", UiFactory.Warning);

            warning.gameObject.SetActive(false);
        }

        private static void BuildButton(RectTransform parent, string caption, Vector2 position, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax, Color tint, UiFactory.TextStyle labelStyle)
        {
            RectTransform button = UiFactory.Node("OK", parent, anchorMin, anchorMax, Vector2.one / 2f, position, sizeDelta);
            UiFactory.SlicedPanel(button, tint);
            UiFactory.Button(button, UiFactory.ColorBlockFor(tint, UiFactory.HighlightedTint, UiFactory.PressedTint, UiFactory.HighlightedTint));
            UiFactory.Sizing(button, preferredWidth: 500f, preferredHeight: 75.05005f);

            RectTransform label = UiFactory.Stretch("Text", button);
            UiFactory.Label(label, caption, labelStyle);
        }

        private static RectTransform BuildCloseButton(RectTransform root)
        {
            RectTransform button = UiFactory.Node("OK", root, new Vector2(0.4146143f, 0.014285714f), new Vector2(0.58615726f, 0.091000006f), Vector2.one / 2f, new Vector2(-0.5f, -0.07751465f), new Vector2(-4.9000244f, 1.7999878f));
            UiFactory.SlicedPanel(button, new Color(1f, 1f, 1f, 0.722f));
            UiFactory.Button(button, UiFactory.ColorBlockFor(new Color(1f, 1f, 1f, 0.72156864f), UiFactory.HighlightedTint, UiFactory.PressedTint, UiFactory.HighlightedTint));
            UiFactory.Sizing(button, preferredWidth: 500f, preferredHeight: 75.05005f);

            RectTransform label = UiFactory.Stretch("Text", button);
            UiFactory.Label(label, "OK", UiFactory.DarkButtonLabel);

            return button;
        }

        private static readonly Vector2 RefreshAnchorMin = new Vector2(0.41772184f, 0.031000001f);
        private static readonly Vector2 RefreshAnchorMax = new Vector2(0.6141655f, 0.14447679f);
        private static readonly Color DarkButtonTint = new Color(0f, 0f, 0f, 0.866f);

        private static readonly Vector2 RightEdge = new Vector2(1f, 0.5f);
        private static readonly Vector2 RightEdgePivot = new Vector2(1f, 0.5f);
        private static readonly Vector2 RefreshSize = new Vector2(53.191f, 53.191f);
        private const float WarningHeight = 60.5f;
        private const float ScrollbarWidth = 22f;
        private const float ScrollbarInset = 5f;

        private const float CardCellWidth = 551.7f;
        private const float CardPadding = 16f;
        private const float ThumbnailAspect = 500f / 278.95f;
        private const float CardNameStrip = 96f;

        private static readonly float CardCellHeight = (CardPadding * 2f) + ((CardCellWidth - (CardPadding * 2f)) / ThumbnailAspect) + CardNameStrip;

        public static readonly Color CardTint = new Color(1f, 1f, 1f, 0.2f);
        public static readonly Color CardHoverTint = new Color(1f, 1f, 1f, 0.3f);
        public static readonly Color CardPressedTint = new Color(1f, 1f, 1f, 0.14f);
        public static readonly Color CardFocusTint = new Color(1f, 1f, 1f, 0.42f);

        public static readonly Color SelectedTint = new Color(0.36f, 0.78f, 0.42f, 0.85f);
        public static readonly Color SelectedHoverTint = new Color(0.45f, 0.86f, 0.5f, 0.92f);
        public static readonly Color SelectedPressedTint = new Color(0.28f, 0.66f, 0.33f, 0.8f);
        public static readonly Color SelectedFocusTint = new Color(0.55f, 0.92f, 0.6f, 0.95f);
    }
}
