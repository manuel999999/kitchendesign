namespace KitchenDesign.UI.DesignerScreens
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
            get => _targetIdType;
            set => _targetIdType = value;
        }

        public int targetId
        {
            get => _targetId;
            set => _targetId = value;
        }

        public CabinetSpan targetCabinetSpan
        {
            get => m_TargetCabinetSpan;
            set => m_TargetCabinetSpan = value;
        }

        public Sprite icon => m_Icon.sprite;

        protected override ToolbarButton highlightedButton
        {
            get => _highlightedManufacturerButton;
            set => _highlightedManufacturerButton = value;
        }

        // =====================================================================
        // Static functions
        // =====================================================================

        internal static void ForgetHighlight()
        {
            _highlightedManufacturerButton = null;
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

        protected ToolbarGroupIdType _targetIdType;

        protected int _targetId;

        protected CabinetSpan m_TargetCabinetSpan;

        // =====================================================================
        // Static fields
        // =====================================================================

        protected static ToolbarButton _highlightedManufacturerButton;

    }   // class ToolbarGroupButton

}   // namespace KitchenDesign.UI.DesignerScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-16
/// ===========================================================================
