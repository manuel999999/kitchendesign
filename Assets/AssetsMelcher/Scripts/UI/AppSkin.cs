namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     Collects many different <see cref="ButtonStyle"/>s into one
    ///     "skin" which can then be swapped out to have different app
    ///     appearances (color themes).
    /// </summary>
    [CreateAssetMenu(fileName = "AppSkin", menuName = "Melcher Studios/Pro-Fit/App Skin")]
    public class AppSkin : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public Sprite menuHeaderLogo => m_MenuHeaderLogo;

        public float menuHeaderLogoHeight => m_MenuHeaderLogoHeight;

        public Color primaryColor => m_PrimaryColor.value;

        public Color secondaryColor => m_SecondaryColor.value;

        public Color dangerColor => m_DangerColor.value;

        public Color designerMeasureLineColor => m_DesignerMeasureLineColor.value;

        public Color toggleOnColor => m_ToggleOnColor.value;

        public Color toggleOffColor => m_ToggleOffColor.value;

        public Color resizerWidgetForegroundColor => m_ResizerWidgetForegroundColor;

        public Color resizerWidgetBackgroundColor => m_ResizerWidgetBackgroundColor;

        public Material primarySelectionMaterial => m_PrimaryKitchenSelectionMaterial;

        public Material secondarySelectionMaterial => m_SecondaryKitchenSelectionMaterial;

        public Material designerSceneSkyboxMaterial => m_DesignerSceneSkyboxMaterial;

        public Material kitchenDefaultFlooringMaterial => m_KitchenDefaultFlooringMaterial;


        // ====================================================================
        // Methods
        // ====================================================================

        public ButtonStyle GetButtonStyleFromName(ButtonStyleName mode)
        {
            switch (mode)
            {
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(mode), $"Unhandled mode: {mode}");

                case ButtonStyleName.BlackOnWhite:
                    return m_BlackOnWhite;

                case ButtonStyleName.PrimaryOnWhite:
                    return m_PrimaryOnWhite;

                case ButtonStyleName.PrimaryOnTransparent:
                    return m_PrimaryOnTransparent;

                case ButtonStyleName.WhiteOnDanger:
                    return m_WhiteOnDanger;

                case ButtonStyleName.WhiteOnPrimary:
                    return m_WhiteOnPrimary;

                case ButtonStyleName.WhiteOnSecondary:
                    return m_WhiteOnSecondary;

                case ButtonStyleName.InputField:
                    return m_InputField;

                case ButtonStyleName.MainMenuFooterButton:
                    return m_MainMenuFooterButton;

                case ButtonStyleName.ToolbarButton:
                    return m_ToolbarButton;

                case ButtonStyleName.KitchenSetupToggle:
                    return m_KitchenSetupToggle;

                case ButtonStyleName.DesignerZoomButton:
                    return m_DesignerZoomButton;

                case ButtonStyleName.DesignerToolbarButtonBorder:
                    return m_DesignerToolbarButtonBorder;

                case ButtonStyleName.DesignerBrandCatalogToggle:
                    return m_DesignerBrandCatalogToggle;

                case ButtonStyleName.DesignerCabinetDetailImage:
                    return m_DesignerCabinetDetailImage;
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected Sprite m_MenuHeaderLogo = default;

        [Tooltip("The height of the menu header logo.")]
        [SerializeField]
        protected float m_MenuHeaderLogoHeight = 151.0f;

        [Tooltip("This material is applied to the main object the player " +
            "highlights. For example, the whole kitchen (Standard building" +
            "method), an individual cabinet (Sections building method), a " +
            "single product, etc.")]
        [SerializeField]
        protected Material m_PrimaryKitchenSelectionMaterial = default;

        [Tooltip("This material is applied to the rest of the kitchen when " +
            "a single cabinet is selected. The purpose this material is to " +
            "hide any applied kitchen finishes and avoid visual texture " +
            "sliding when the kitchen is resized. This is only used in" +
            "Sections Building Method because it allows individual cabinets " +
            "to be selected.")]
        [SerializeField]
        protected Material m_SecondaryKitchenSelectionMaterial = default;

        [Tooltip("The skybox to use in the designer scene.")]
        [SerializeField]

        protected Material m_DesignerSceneSkyboxMaterial = default;

        [Tooltip("The material to use as the default kitchen flooring in " +
            "the designer scene.")]
        [SerializeField]
        protected Material m_KitchenDefaultFlooringMaterial = default;


        [Space]

        [SerializeField]
        protected MelcherColorAppValue m_PrimaryColor = default;

        [SerializeField]
        protected MelcherColorAppValue m_SecondaryColor = default;

        [SerializeField]
        protected MelcherColorAppValue m_DangerColor = default;

        [Space]

        [SerializeField]
        protected MelcherColorAppValue m_DesignerMeasureLineColor = default;

        [Space]

        [Tooltip("The colour of the checkmark's \"on\" graphic. The Stoll " +
            "skin uses its primary colour whereas Pro-Fit uses its " +
            "secondary colour.")]
        [SerializeField]
        protected MelcherColorAppValue m_ToggleOnColor = default;

        [Tooltip("If a toggle has extra content they want to colour-match " +
            "to the checkbox, this is the colour that content will appear " +
            "in when the checkbox is unchecked. ")]
        [SerializeField]
        protected MelcherColorAppValue m_ToggleOffColor = default;

        [Space]

        [Tooltip("The colour that resizer widgets in the designer scene use " +
            "to draw their arrow.")]
        [SerializeField]
        protected MelcherColorAppValue m_ResizerWidgetForegroundColor = default;

        [Tooltip("The colour that resizer widgets in the designer scene use " +
            "to draw the background / tear-shaped sprite behind the arrows.")]
        [SerializeField]
        protected MelcherColorAppValue m_ResizerWidgetBackgroundColor = default;

        [Space]

        [SerializeField]
        protected ButtonStyle m_BlackOnWhite = default;

        [Space]

        [SerializeField]
        protected ButtonStyle m_PrimaryOnTransparent = default;

        [SerializeField]
        protected ButtonStyle m_PrimaryOnWhite = default;

        [Space]

        [SerializeField]
        protected ButtonStyle m_WhiteOnDanger = default;

        [SerializeField]
        protected ButtonStyle m_WhiteOnPrimary = default;

        [SerializeField]
        protected ButtonStyle m_WhiteOnSecondary = default;

        [Space]

        [SerializeField]
        protected ButtonStyle m_InputField = default;

        [SerializeField]
        protected ButtonStyle m_MainMenuFooterButton = default;

        [SerializeField]
        protected ButtonStyle m_ToolbarButton = default;

        [SerializeField]
        protected ButtonStyle m_KitchenSetupToggle = default;

        [SerializeField]
        protected ButtonStyle m_DesignerZoomButton = default;

        [SerializeField]
        protected ButtonStyle m_DesignerToolbarButtonBorder = default;

        [SerializeField]
        protected ButtonStyle m_DesignerBrandCatalogToggle = default;

        [SerializeField]
        protected ButtonStyle m_DesignerCabinetDetailImage = default;

        // ====================================================================
        // Types
        // ====================================================================

        // TODO: Replace all serialized instances of this enum with a
        //       MelcherStableEnum. Otherwise, add new entries to the end of
        //       the enum.
        public enum ButtonStyleName
        {
            None,

            PrimaryOnWhite,
            PrimaryOnTransparent,

            BlackOnWhite,

            WhiteOnDanger,
            WhiteOnPrimary,
            WhiteOnSecondary,

            InputField,
            ToolbarButton,

            /// <summary>
            ///     A big radio button that appears on the pop-ups when a new
            ///     kitchen is created.
            ///     <br /><br />
            ///
            ///     Kitchen layout: straight, L, U, etc.
            ///     Building method: standard or sections.
            /// </summary>
            KitchenSetupToggle,

            MainMenuFooterButton,

            /// <summary>
            ///     The +/- zoom buttons used in the designer scene.
            /// </summary>
            DesignerZoomButton,

            DesignerToolbarButtonBorder,

            DesignerBrandCatalogToggle,

            /// <summary>
            ///     The images that appear during the cabinet setup phase. This
            ///     colour is used to tint the lines that appear within the
            ///     images that depict the cabinet depth, countertop thickness,
            ///     etc.
            /// </summary>
            DesignerCabinetDetailImage,

        }   // enum ButtonStyleName

    }   // class AppSkin

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-07-26
/// ===========================================================================
