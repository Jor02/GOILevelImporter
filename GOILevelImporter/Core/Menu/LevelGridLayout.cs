using UnityEngine;
using UnityEngine.UI;

namespace GOILevelImporter.Core.Menu
{
    [RequireComponent(typeof(GridLayoutGroup))]
    internal class LevelGridLayout : MonoBehaviour
    {
        private int columns = 4;


        private float cardPadding = 16f;
        private float thumbnailAspect = 500f / 278.95f;
        private float nameStripHeight = 96f;

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

        public void Configure(int columnCount, float cardPadding, float thumbnailAspect, float nameStripHeight)
        {
            columns = columnCount;
            this.cardPadding = cardPadding;
            this.thumbnailAspect = thumbnailAspect;
            this.nameStripHeight = nameStripHeight;

            appliedWidth = -1f;
            Recalculate();
        }

        private void Recalculate()
        {
            if (grid == null) return; // can fire before Awake on the very first pass

            float width = rect.rect.width;
            if (width <= 0f || Mathf.Approximately(width, appliedWidth)) return;
            appliedWidth = width;

            int columnCount = Mathf.Max(1, columns);
            grid.constraintCount = columnCount;

            float usableWidth = width - grid.padding.left - grid.padding.right - grid.spacing.x * (columnCount - 1);
            float cellWidth = usableWidth / columnCount;

            float thumbnailHeight = (cellWidth - cardPadding * 2f) / thumbnailAspect;
            float cellHeight = cardPadding * 2f + thumbnailHeight + nameStripHeight;

            grid.cellSize = new Vector2(cellWidth, cellHeight);
        }
    }
}
