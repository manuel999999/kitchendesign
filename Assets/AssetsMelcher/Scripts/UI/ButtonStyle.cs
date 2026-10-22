namespace KitchenDesign
{
    using Melcher;
    using System;
    using TMPro;
    using UnityEngine;

    [CreateAssetMenu(fileName = "ButtonStyle", menuName = "Melcher Studios/Pro-Fit/Button Style")]
    public class ButtonStyle : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public Color normalForegroundColor => m_NormalForegroundColor.value;
        public Color normalBackgroundColor => m_NormalBackgroundColor.value;
        public FontStyleOverride normalFontStyle => m_NormalFontStyle;

        public Color hoveredForegroundColor => m_HoveredForegroundColor.value;
        public Color hoveredBackgroundColor => m_HoveredBackgroundColor.value;
        public FontStyleOverride hoveredFontStyle => m_HoveredFontStyle;

        public Color pressedForegroundColor => m_PressedForegroundColor.value;
        public Color pressedBackgroundColor => m_PressedBackgroundColor.value;
        public FontStyleOverride pressedFontStyle => m_PressedFontStyle;

        public Color nonInteractableForegroundColor => m_NonInteractableForegroundColor.value;
        public Color nonInteractableBackgroundColor => m_NonInteractableBackgroundColor.value;
        public FontStyleOverride nonInteractableFontStyle => m_NonInteractableFontStyle;

        // ====================================================================
        // Methods
        // ====================================================================

        public (Color foreground, Color background) ColorsForAppearance(MelcherSelectable.Appearance appearance)
        {
            switch (appearance)
            {
                case MelcherSelectable.Appearance.Normal:
                    return (normalForegroundColor, normalBackgroundColor);

                case MelcherSelectable.Appearance.Hovered:
                    return (hoveredForegroundColor, hoveredBackgroundColor);

                case MelcherSelectable.Appearance.Pressed:
                    return (pressedForegroundColor, pressedBackgroundColor);

                case MelcherSelectable.Appearance.NonInteractable:
                    return (nonInteractableForegroundColor, nonInteractableBackgroundColor);

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(appearance), $"Unhandled {nameof(appearance)} value: {appearance}");
            }
        }

        public FontStyleOverride FontStyleOverrideForAppearance(MelcherSelectable.Appearance appearance)
        {
            switch (appearance)
            {
                case MelcherSelectable.Appearance.Normal:
                    return normalFontStyle;

                case MelcherSelectable.Appearance.Hovered:
                    return hoveredFontStyle;

                case MelcherSelectable.Appearance.Pressed:
                    return pressedFontStyle;

                case MelcherSelectable.Appearance.NonInteractable:
                    return nonInteractableFontStyle;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(appearance), $"Unhandled {nameof(appearance)} value: {appearance}");
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Space]
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_NormalForegroundColor = default;
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_NormalBackgroundColor = default;
        [SerializeField] protected FontStyleOverride m_NormalFontStyle = default;

        [Space]
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_HoveredForegroundColor = default;
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_HoveredBackgroundColor = default;
        [SerializeField] protected FontStyleOverride m_HoveredFontStyle = default;

        [Space]
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_PressedForegroundColor = default;
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_PressedBackgroundColor = default;
        [SerializeField] protected FontStyleOverride m_PressedFontStyle = default;

        [Space]
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_NonInteractableForegroundColor = default;
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_NonInteractableBackgroundColor = default;
        [SerializeField] protected FontStyleOverride m_NonInteractableFontStyle = default;

    }   // class ButtonStyle

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-24
/// ===========================================================================
