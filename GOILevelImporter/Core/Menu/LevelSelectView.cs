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
            UiFactory.CutCornerPanel(root, RootScrim, RootNotch);

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
            UiFactory.CutCornerPanel(buttonArea, PanelTint, HeaderNotch);
            UiFactory.Sizing(buttonArea, preferredHeight: 60f);

            RectTransform heading = UiFactory.Node("Text", buttonArea, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.one / 2f, new Vector2(172f, -30f), new Vector2(343.9f, 60f));
            UiFactory.Label(heading, "Level select", UiFactory.Heading);

            RectTransform refresh = UiFactory.Node("Refresh", buttonArea, RightEdge, RightEdge, RightEdgePivot, new Vector2(-16f, 0f), RefreshSize);
            UiFactory.CutCornerPanel(refresh, DarkButtonTint, ButtonNotch);
            UiFactory.Button(refresh, UiFactory.ColorBlockFor(DarkButtonTint, UiFactory.HighlightedTint, UiFactory.PressedTint, UiFactory.HighlightedTint));

            RectTransform icon = UiFactory.Stretch("Image", refresh);
            icon.sizeDelta = new Vector2(-9.86f, -9.86f);
            UiFactory.AddImage(icon, UiAssets.RefreshIcon, Color.white, Image.Type.Simple);
        }

        private static void BuildScrollArea(RectTransform levelArea)
        {
            RectTransform scrollArea = UiFactory.Point("Scroll Area", levelArea);
            UiFactory.Panel(scrollArea, PanelWash);
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
            // Plain rectangle on purpose: this carries the scroll Mask, and a notched
            // mask would clip the cards near its edges into the same angled corners.
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
            // Was UpperCenter, which centered each row in whatever width was left
            // over after fitting as many cards as would fit - the reported bug
            // where cards drift to the middle instead of hugging the left edge.
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.spacing = new Vector2(CardSpacing, CardSpacing);

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Cell size used to be a fixed literal sized for one specific screen
            // width. This locks the row to Columns cards and keeps recalculating
            // the cell size to fill whatever width the level list actually has.
            LevelGridLayout columns = content.gameObject.AddComponent<LevelGridLayout>();
            columns.Configure(Columns, CardPadding, ThumbnailAspect, CardNameStrip);

            BuildLevelButtonTemplate(content);
        }

        private static void BuildLevelButtonTemplate(RectTransform content)
        {
            RectTransform button = UiFactory.Point("LevelSection", content);
            UiFactory.CutCornerPanel(button, CardTint, CardNotch);
            UiFactory.Button(button, CardColorBlock(false));

            // Stacks the thumbnail and the name strip top to bottom, sized to
            // whatever width the grid currently hands this card - nothing here is
            // computed from a fixed pixel width anymore.
            VerticalLayoutGroup layout = button.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)CardPadding, (int)CardPadding, (int)CardPadding, (int)CardPadding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            // The thumbnail keeps the height its AspectRatioFitter gives it, so it's the
            // name strip that takes up whatever height is left over in the card.
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform thumbnail = UiFactory.Point("Thumbnail", button);
            UiFactory.AddImage(thumbnail, UiAssets.MissingThumb, Color.white, Image.Type.Simple, false, false);

            // Recomputes the thumbnail's height from whatever width the layout
            // group above gives it, so the image never gets stretched off its
            // aspect ratio the way the old fixed-height box used to.
            UiFactory.Sizing(thumbnail, flexibleHeight: 0f);

            AspectRatioFitter fitter = thumbnail.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = ThumbnailAspect;

            RectTransform textArea = UiFactory.Point("TextArea", button);
            textArea.sizeDelta = new Vector2(0f, CardNameStrip);

            // Whatever height the thumbnail doesn't claim goes to the name strip. Without
            // that the remainder pools at the bottom of the card, leaving the title high in
            // a gap rather than centered under the thumbnail.
            UiFactory.Sizing(textArea, preferredHeight: CardNameStrip, flexibleHeight: 1f);

            RectTransform label = UiFactory.Stretch("Label", textArea);
            TextMeshProUGUI levelLabel = UiFactory.Label(label, string.Empty, UiFactory.CardTitle);
            levelLabel.overflowMode = TextOverflowModes.Overflow;

            button.gameObject.SetActive(false);
            button.gameObject.AddComponent<CardTitleCentering>();
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
            UiFactory.CutCornerPanel(errorScreen, PanelTint, SidebarNotch);

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
            UiFactory.CutCornerPanel(sidebar, PanelTint, SidebarNotch);
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
            UiFactory.CutCornerPanel(body, PanelWash, BodyNotch);
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
            UiFactory.CutCornerPanel(button, tint, ButtonNotch);
            UiFactory.Button(button, UiFactory.ColorBlockFor(tint, UiFactory.HighlightedTint, UiFactory.PressedTint, UiFactory.HighlightedTint));
            UiFactory.Sizing(button, preferredWidth: 500f, preferredHeight: 75.05005f);

            RectTransform label = UiFactory.Stretch("Text", button);
            UiFactory.Label(label, caption, labelStyle);
        }

        private static RectTransform BuildCloseButton(RectTransform root)
        {
            RectTransform button = UiFactory.Node("OK", root, new Vector2(0.4146143f, 0.014285714f), new Vector2(0.58615726f, 0.091000006f), Vector2.one / 2f, new Vector2(-0.5f, -0.07751465f), new Vector2(-4.9000244f, 1.7999878f));
            UiFactory.CutCornerPanel(button, CloseButtonTint, ButtonNotch);
            UiFactory.Button(button, UiFactory.ColorBlockFor(CloseButtonTint, UiFactory.HighlightedTint, UiFactory.PressedTint, UiFactory.HighlightedTint));
            UiFactory.Sizing(button, preferredWidth: 500f, preferredHeight: 75.05005f);

            RectTransform label = UiFactory.Stretch("Text", button);
            UiFactory.Label(label, "OK", UiFactory.DarkButtonLabel);

            return button;
        }

        private static readonly Vector2 RefreshAnchorMin = new Vector2(0.41772184f, 0.031000001f);
        private static readonly Vector2 RefreshAnchorMax = new Vector2(0.6141655f, 0.14447679f);

        private static readonly Vector2 RightEdge = new Vector2(1f, 0.5f);
        private static readonly Vector2 RightEdgePivot = new Vector2(1f, 0.5f);
        private static readonly Vector2 RefreshSize = new Vector2(53.191f, 53.191f);
        private const float WarningHeight = 60.5f;
        private const float ScrollbarWidth = 22f;
        private const float ScrollbarInset = 5f;

        private const float CardPadding = 16f;
        private const float ThumbnailAspect = 500f / 278.95f;
        private const float CardNameStrip = 56f;
        private const float CardSpacing = 6.75f;

        // How many level cards fit side by side. The grid recomputes each card's
        // width - and, from that, its height, to keep the thumbnail's aspect ratio -
        // to fill the level list at exactly this many columns, however wide the
        // level list area turns out to be.
        private const int Columns = 4;

        // How far the cut corner reaches in, in UI units. Bigger panels get a bigger cut
        // so the accent stays proportionate; small buttons get a subtle one.
        private const float RootNotch = 40f;
        private const float HeaderNotch = 14f;
        private const float SidebarNotch = 28f;
        private const float BodyNotch = 20f;
        private const float CardNotch = 16f;
        private const float ButtonNotch = 14f;

        // Slate-and-copper palette: dark, low-saturation panels with a warm accent
        // reserved for the selected card, instead of the old plain black/white tints.
        private static readonly Color RootScrim = new Color(0.02f, 0.02f, 0.03f, 0.6f);
        private static readonly Color PanelTint = new Color(0.07f, 0.08f, 0.1f, 0.78f);
        private static readonly Color PanelWash = new Color(0.07f, 0.08f, 0.1f, 0.4f);
        private static readonly Color DarkButtonTint = new Color(0.05f, 0.06f, 0.07f, 0.9f);
        private static readonly Color CloseButtonTint = new Color(0.9f, 0.91f, 0.93f, 0.85f);

        public static readonly Color CardTint = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color CardHoverTint = new Color(1f, 1f, 1f, 0.16f);
        public static readonly Color CardPressedTint = new Color(1f, 1f, 1f, 0.04f);
        public static readonly Color CardFocusTint = new Color(1f, 1f, 1f, 0.24f);

        public static readonly Color SelectedTint = new Color(0.86f, 0.58f, 0.24f, 0.85f);
        public static readonly Color SelectedHoverTint = new Color(0.93f, 0.68f, 0.32f, 0.92f);
        public static readonly Color SelectedPressedTint = new Color(0.7f, 0.46f, 0.16f, 0.85f);
        public static readonly Color SelectedFocusTint = new Color(0.97f, 0.75f, 0.4f, 0.95f);
    }
}
