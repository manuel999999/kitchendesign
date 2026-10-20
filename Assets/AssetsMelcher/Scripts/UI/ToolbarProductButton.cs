namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(MelcherButton))]
    public class ToolbarProductButton : ToolbarButton
    {
        // =====================================================================
        // Events
        // =====================================================================

        public static event Action<ToolbarProductButton> onClick;

        // =====================================================================
        // Properties
        // =====================================================================

        public Sprite overlaySprite
        {
            get => m_OverlayIcon.sprite;

            set
            {
                m_OverlayIcon.sprite = value;
                m_OverlayIcon.gameObject.SetActive(value);
            }
        }

        public ProductInfo productInfo
        {
            get => m_ProductInfo;
            set
            {
                m_ProductInfo = value;
                m_Label.phrase = m_ProductInfo.name;

                // Update the toolbar button's variant text label. This label
                // displays how many product variants belong to the product
                // variant chain and displays the index of the currently
                // displayed product. E.g., "2 of 10".

                string variantLabelText = "";

                m_ProductVariantIndex = -1;

                List<int> productVariants =
                    ProFitUtility.GetProductVariantList(m_ProductInfo);

                if (productVariants != null && productVariants.Count > 0)
                {
                    m_ProductVariantIndex =
                        productVariants.IndexOf(m_ProductInfo.id);

                    MelcherLog.Assert(
                        m_ProductVariantIndex >= 0,
                        $"A product belongs to a product variant chain but its ID is not in the product variant chain. Product is {m_ProductInfo.name} (id: {m_ProductInfo.id}, baseProductId: {m_ProductInfo.baseProductId})"
                    );

                    int displayIndex = m_ProductVariantIndex + 1;

                    variantLabelText = $"{displayIndex} of {productVariants.Count}";
                }

                bool useVariantsLabel = !string.IsNullOrEmpty(variantLabelText);

                m_VariantsIndexLabel.phrase = variantLabelText;
                m_VariantsIndexBackground.gameObject.SetActive(useVariantsLabel);
            }
        }

        public int productVariantIndex => m_ProductVariantIndex;

        public bool belongsToProductVariantChain => m_VariantsIndexBackground.gameObject.activeSelf;

        public bool isErrorHighlightVisible
        {
            get => m_ErrorHighlight.gameObject.activeSelf;
        }

        protected override ToolbarButton highlightedButton
        {
            get => s_HighlightedProductButton;
            set => s_HighlightedProductButton = value;
        }
        // =====================================================================
        // Static functions
        // =====================================================================

        internal static void ForgetHighlight()
        {
            s_HighlightedProductButton = null;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetErrorHighlightVisible(bool visible)
        {
            m_ErrorHighlight.gameObject.SetActive(visible);
        }

        /// <summary>
        ///     This shows or hides an icon intended to indicate whether the
        ///     target product has product variants.
        /// </summary>
        public void ShowHasVariantsIcon(bool hasVariants)
        {
        }

        protected override void Awake()
        {
            base.Awake();

            // Refresh the visibility of the overlay sprite
            overlaySprite = m_OverlayIcon.sprite;

            m_ErrorHighlight.gameObject.SetActive(false);

            // Hide the variant label by default
            m_VariantsIndexLabel.phrase = "";
            m_VariantsIndexBackground.gameObject.SetActive(false);
        }

        protected override void DoEmitClickEvent()
        {
            onClick?.Invoke(this);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ToolbarProductButton))]

        [SerializeField] protected Image m_OverlayIcon = null;

        [SerializeField] protected Image m_ErrorHighlight = null;

        [SerializeField] protected Image m_VariantsIndexBackground = null;

        [SerializeField] protected MelcherLocalizedText m_VariantsIndexLabel = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected ProductInfo m_ProductInfo;

        /// <summary>
        ///     Stores which index the target product represents in its product
        ///     variant list. A value of 0 means "the base product" and anything
        ///     greater than this is within the base product's list stored at
        ///     <see cref="ProFitGlobals.baseProductIdToProductVariantIds"/>.
        /// </summary>
        protected int m_ProductVariantIndex;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static ToolbarButton s_HighlightedProductButton;

    }   // class ToolbarProductButton

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
