namespace KitchenDesign.UI.DesignerScreens
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
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
            get => _highlightedCategoryButton;
            set => _highlightedCategoryButton = value;
        }

        // ====================================================================
        // Static functions
        // ====================================================================

        public static void ForgetHighlight()
        {
            _highlightedCategoryButton = null;
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

        protected static ToolbarButton _highlightedCategoryButton;

    }   // class ToolbarCategoryButton

}   // namespace KitchenDesign.UI.DesignerScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
