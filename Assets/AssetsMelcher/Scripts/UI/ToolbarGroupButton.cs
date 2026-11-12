namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Shared.Enums;

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

        public ToolbarGroupIdType targetIdType
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

        protected ToolbarGroupIdType m_TargetIdType;

        protected int m_TargetId;

        protected CabinetSpan m_TargetCabinetSpan;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static ToolbarButton s_HighlightedManufacturerButton;

    }   // class ToolbarGroupButton

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
