namespace Melcher
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A recyclable item for the manufacturer whitelist screen.
    /// </summary>
    public class ManufacturerWhitelistRow : MelcherMonoBehaviour
    {
        // =====================================================================
        // Events
        // =====================================================================

        public static event Action<ManufacturerWhitelistRow> onValueChanged;

        public static event Action<ManufacturerWhitelistRow> onPriceMarkupButtonClicked;

        // =====================================================================
        // Properties
        // =====================================================================Info

        public int index => m_Index;
        public bool isHeading => m_IsHeading;
        public bool isOn
        {
            get
            {
                if (isHeading)
                {
                    throw new InvalidOperationException(
                        $"Cannot call {nameof(isOn)} when {nameof(isHeading)} is true. Cannot return a meaningful value."
                    );
                }

                return m_Toggle.isOn;
            }
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Make this row a heading for a product category. This allows the
        ///     RecyclableList to group manufacturers according to the main
        ///     product type built by the manufacturer.
        /// </summary>
        public void SetManufacturerGroupHeadingFields(ProductCategory category)
        {
            ResetFields();

            // TODO: Use a prettier name.
            m_HeaderLabel.phrase = category.ToString();
            m_HeaderLabel.gameObject.SetActive(true);
        }

        public void SetManufacturerCheckboxFields(string manufacturerName, decimal priceMarkupPercent, int index, bool isOn, bool isInteractable)
        {
            ResetFields();

            m_Index = index;

            m_Toggle.isOn = isOn;
            m_Toggle.interactable = isInteractable;
            m_Toggle.enabled = true;

            // Hide the checkbox if the manufacturer is overridden in the
            // dashboard and therefore not allowed to be changed by the dealer.
            m_ToggleCheckboxRoot.GetComponent<Graphic>().enabled = isInteractable;
            m_ToggleCheckboxRoot.gameObject.SetActive(true);

            if (isInteractable)
            {
                m_ToggleLabel.textMesh.fontStyle &= ~TMPro.FontStyles.Strikethrough;
            }
            else
            {
                m_ToggleLabel.textMesh.fontStyle |= TMPro.FontStyles.Strikethrough;
                // Use rich text tags to extend the strikethrough further out
                // the sides of the label.
                manufacturerName = $"<indent=-40px> </indent><indent=0px>{manufacturerName}</indent><space=40px /> ";
                m_ToggleLabel.textMesh.textWrappingMode = TMPro.TextWrappingModes.PreserveWhitespace;
            }
            m_ToggleLabel.phrase = manufacturerName;
            m_ToggleLabel.gameObject.SetActive(true);

            if (ProFitGlobals.appOptions.allowDealerMarkupPercentChanges)
            {
                m_PriceMarkupButton.gameObject.SetActive(true);
                if (m_PriceMarkupButton.TryGetComponent(out MelcherLocalizedText priceMarkupLabel))
                {
                    priceMarkupLabel.phrase = $"{priceMarkupPercent:+0.##;-0.##;+0}% $(pen)";
                }
            }
        }

        protected void OnEnable()
        {
            m_Toggle.onValueChanged += EmitToggleChanged;

            m_PriceMarkupButton.onClick += OnPriceMarkupButtonClicked;
        }

        protected void OnDisable()
        {
            m_Toggle.onValueChanged -= EmitToggleChanged;

            m_PriceMarkupButton.onClick -= OnPriceMarkupButtonClicked;
        }

        protected void EmitToggleChanged(IMelcherToggle toggle)
        {
            onValueChanged?.Invoke(this);
        }

        protected void OnPriceMarkupButtonClicked(MelcherButton sender)
        {
            onPriceMarkupButtonClicked?.Invoke(this);
        }

        protected void ResetFields()
        {
            m_Index = -1;

            m_HeaderLabel.gameObject.SetActive(false);

            m_Toggle.enabled = false;
            m_ToggleCheckboxRoot.gameObject.SetActive(false);
            m_ToggleLabel.gameObject.SetActive(false);

            m_PriceMarkupButton.gameObject.SetActive(false);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Tooltip("The label to display when the row represents the heading " +
            "for a group of manufacturers.")]
        [SerializeField] protected MelcherLocalizedText m_HeaderLabel = null;

        [Space]

        [SerializeField] protected MelcherToggle m_Toggle = null;

        [SerializeField] protected RectTransform m_ToggleCheckboxRoot = default;

        [Tooltip("The label to display when the row represents a specific " +
            "manufacturer as a toggle option.")]
        [SerializeField] protected MelcherLocalizedText m_ToggleLabel = null;

        [Space]

        [Tooltip("The button that allows this manufacturer's price markup " +
            "to be changed.")]
        [SerializeField] protected MelcherButton m_PriceMarkupButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected int m_Index;

        protected bool m_IsHeading;

    }   // class ManufacturerWhitelistRow

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
