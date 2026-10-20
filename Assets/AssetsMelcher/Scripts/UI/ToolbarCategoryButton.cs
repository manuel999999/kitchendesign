namespace Melcher
{
    using System;
    using UnityEngine;

    [RequireComponent(typeof(MelcherButton))]
    public class ToolbarCategoryButton : ToolbarButton
    {
        // =====================================================================
        // Events
        // =====================================================================

        public static event Action<ToolbarCategoryButton> onClick;

        // =====================================================================
        // Properties
        // =====================================================================

        public ProductCategory category
        {
            get => m_Category;
            set => m_Category.value = value;
        }

        protected override ToolbarButton highlightedButton
        {
            get => s_HighlightedCategoryButton;
            set => s_HighlightedCategoryButton = value;
        }

        // ====================================================================
        // Static functions
        // ====================================================================

        public static void ForgetHighlight()
        {
            s_HighlightedCategoryButton = null;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        protected override void DoEmitClickEvent()
        {
            onClick?.Invoke(this);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ToolbarCategoryButton))]

        [SerializeField]
        protected MelcherStableEnum<ProductCategory> m_Category = default;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static ToolbarButton s_HighlightedCategoryButton;

    }   // class ToolbarCategoryButton

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
