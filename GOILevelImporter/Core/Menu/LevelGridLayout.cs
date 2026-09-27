using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    [RequireComponent(typeof(GridLayoutGroup))]
    internal class LevelGridLayout : MonoBehaviour
    {
        public int Columns = 4;

        public float CardPadding = 16f;
        public float ThumbnailAspect = 500f / 278.95f;
        public float NameStripHeight = 96f;

        private GridLayoutGroup grid;
        private RectTransform rect;
        private float appliedWidth = -1f;

        void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            rect = (RectTransform)transform;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        }

        void OnEnable()
        {
            Recalculate();
        }

        // Fires whenever this RectTransform's resolved width changes, including from a
        // parent resizing (a screen resize, or the sidebar opening next to this grid).
        void OnRectTransformDimensionsChange()
        {
            Recalculate();
        }

        private void Recalculate()
        {
            if (grid == null) return; // can fire before Awake on the very first pass

            float width = rect.rect.width;
            if (width <= 0f || Mathf.Approximately(width, appliedWidth)) return;
            appliedWidth = width;

            int columns = Mathf.Max(1, Columns);
            grid.constraintCount = columns;

            float usableWidth = width - grid.padding.left - grid.padding.right - grid.spacing.x * (columns - 1);
            float cellWidth = usableWidth / columns;

            float thumbnailHeight = (cellWidth - CardPadding * 2f) / ThumbnailAspect;
            float cellHeight = CardPadding * 2f + thumbnailHeight + NameStripHeight;

            grid.cellSize = new Vector2(cellWidth, cellHeight);
        }
    }
}
