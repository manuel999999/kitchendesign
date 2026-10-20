namespace Melcher
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    ///
    /// </summary>
    // Original implementation by `DeloitteCam`:
    // https://forum.unity.com/threads/solved-how-to-make-grid-layout-group-cell-size-x-auto-expand.448534/#post-6099369
    [ExecuteInEditMode]
    [RequireComponent(typeof(GridLayoutGroup))]
    public class MelcherGridLayoutCellSizeAdjuster : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public void UpdateCellSize()
        {
            if (!enabled)
            {
                return;
            }

            if (!m_RectTransform || !m_GridLayoutGroup)
            {
                return;
            }

            int count = m_GridLayoutGroup.constraintCount;

            if (m_ExpandAxis == Axis.X)
            {
                float spacing = (count - 1) * m_GridLayoutGroup.spacing.x;
                float contentSize = m_RectTransform.rect.width - m_GridLayoutGroup.padding.left - m_GridLayoutGroup.padding.right - spacing;
                float sizePerCell = contentSize / count;

                m_GridLayoutGroup.cellSize =
                    new Vector2(
                        sizePerCell,
                        m_PreserveCellRatio ?
                            sizePerCell * m_WidthToHeightRatio :
                            m_GridLayoutGroup.cellSize.y
                    );
            }
            else //if (expand == Axis.Y)
            {
                float spacing = (count - 1) * m_GridLayoutGroup.spacing.y;
                float contentSize = m_RectTransform.rect.height - m_GridLayoutGroup.padding.top - m_GridLayoutGroup.padding.bottom - spacing;
                float sizePerCell = contentSize / count;

                m_GridLayoutGroup.cellSize =
                    new Vector2(
                        m_PreserveCellRatio ?
                            sizePerCell * m_WidthToHeightRatio :
                            m_GridLayoutGroup.cellSize.x,
                        sizePerCell
                    );
            }
        }

        protected void Start()
        {
            UpdateCellSize();
        }

        protected void OnRectTransformDimensionsChange()
        {
            UpdateCellSize();
        }

        // ====================================================================
        // Editor-only
        // ====================================================================
#if UNITY_EDITOR

        [ExecuteAlways]
        protected void Update()
        {
            UpdateCellSize();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            UpdateCellSize();
        }

        protected void Reset()
        {
            Vector2 cellSize = GetComponent<GridLayoutGroup>().cellSize;

            float cellRatio = cellSize.x / cellSize.y;

            if (!float.IsNaN(cellRatio) && !float.IsInfinity(cellRatio))
            {
                m_WidthToHeightRatio = cellRatio;
            }
        }
#endif  // if UNITY_EDITOR

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected Axis m_ExpandAxis;

        [SerializeField]
        protected bool m_PreserveCellRatio;

        [SerializeField]
        [MelcherShowOnValue(typeof(MelcherGridLayoutCellSizeAdjuster), nameof(m_PreserveCellRatio), value: true)]
        [Tooltip("Used when cell ratio is preserved. The ratio is the cell's width divided by height.")]
        protected float m_WidthToHeightRatio = 1;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected RectTransform m_RectTransform = default;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected GridLayoutGroup m_GridLayoutGroup;

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum Axis
        {
            X,
            Y
        }   // enum Axis

    }   // class

}   // namespace Melcher