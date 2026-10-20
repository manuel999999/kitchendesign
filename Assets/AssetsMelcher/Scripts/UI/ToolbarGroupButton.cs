namespace Melcher
{
    using System;
    using UnityEngine;

    [RequireComponent(typeof(MelcherButton))]
    public class ToolbarGroupButton : ToolbarButton
    {
        // =====================================================================
        // Events
        // =====================================================================

        public static event Action<ToolbarGroupButton> onClick;

        // =====================================================================
        // Properties
        // =====================================================================

        public IdType targetIdType
        {
            get => m_TargetIdType;
            set => m_TargetIdType = value;
        }

        public int targetId
        {
            get => m_TargetId;
            set => m_TargetId = value;
        }

        public CabinetSpan targetCabinetSpan
        {
            get => m_TargetCabinetSpan;
            set => m_TargetCabinetSpan = value;
        }

        public Sprite icon => m_Icon.sprite;

        protected override ToolbarButton highlightedButton
        {
            get => s_HighlightedManufacturerButton;
            set => s_HighlightedManufacturerButton = value;
        }

        // =====================================================================
        // Static functions
        // =====================================================================

        internal static void ForgetHighlight()
        {
            s_HighlightedManufacturerButton = null;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetIcon(Sprite sprite, Color? color = null)
        {
            m_Icon.sprite = sprite;
            m_Icon.color = color.GetValueOrDefault(Color.white);
            m_Icon.gameObject.SetActive(sprite);

            m_Label.textMesh.verticalAlignment = sprite ?
                TMPro.VerticalAlignmentOptions.Bottom :
                TMPro.VerticalAlignmentOptions.Middle;
        }

        protected override void DoEmitClickEvent()
        {
            onClick?.Invoke(this);
        }

        protected override void DoSetPhrase(string phrase)
        {
            base.DoSetPhrase(phrase);

            bool shiftIconUp = !string.IsNullOrEmpty(phrase);
            m_Icon.rectTransform.offsetMin = new Vector2(0.0f, shiftIconUp ? 32.0f : 0.0f);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ToolbarGroupButton))]

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected IdType m_TargetIdType;

        protected int m_TargetId;

        protected CabinetSpan m_TargetCabinetSpan;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static ToolbarButton s_HighlightedManufacturerButton;

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum IdType
        {
            /// <summary>
            /// The ID corresponds to a manufacturer's ID.
            /// </summary>
            Manufacturer,

            /// <summary>
            /// The ID corresponds to a product group's ID.
            /// </summary>
            ProductGroup,

            /// <summary>
            /// The button will reset the cabinet finish to its default.
            /// </summary>
            ResetCabinet,

            /// <summary>
            /// The button will reset the countertop finish to its default.
            /// </summary>
            ResetCountertop,

            /// <summary>
            /// The button will reset the kitchen flooring to its default.
            /// </summary>
            ResetFlooring,

            /// <summary>
            ///     The ID corresponds to the index of a door style in the
            ///     currently selected <see cref="BrandCatalog"/>'s door styles
            ///     list.
            /// </summary>
            CabinetSectionType,

            /// <summary>
            ///     The ID corresponds to the index of a door style in the
            ///     currently selected <see cref="BrandCatalog"/>'s door styles
            ///     list.
            /// </summary>
            CabinetDoorStyle,

            /// <summary>
            ///     The ID corresponds to the index of a door handle style in the
            ///     currently selected <see cref="BrandCatalog"/>'s door styles
            ///     list.
            /// </summary>
            CabinetDoorHandleStyle,

            /// <summary>
            ///     The button will toggle Challenger's premium finishes on/off.
            /// </summary>
            ChallengerPremiumCabinetFinish,

            /// <summary>
            ///     The ID corresponds to which end panel to be added
            ///     0 is to left, 1 is to right.
            /// </summary>
            CabinetEndPanels,
            CabinetBackPanels,

        }   // enum IdType

    }   // class ToolbarGroupButton

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
