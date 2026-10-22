namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class InputFieldView : MelcherMonoBehaviour
    {
        protected void Awake()
        {
            AppSkin currentSkin = ProFitGlobals.appOptions.appSkin;

            ButtonStyle buttonStyle =
                currentSkin.GetButtonStyleFromName(m_ButtonStyleName);

            ColorBlock controlColors = m_InputField.colors;
            {
                controlColors.normalColor       = buttonStyle.normalBackgroundColor;
                controlColors.highlightedColor  = controlColors.normalColor;
                controlColors.pressedColor      = buttonStyle.pressedBackgroundColor;
                controlColors.selectedColor     = buttonStyle.hoveredBackgroundColor;
                controlColors.disabledColor     = buttonStyle.nonInteractableBackgroundColor;
            }
            m_InputField.colors = controlColors;

            m_InputField.caretColor         = buttonStyle.normalBackgroundColor;
            m_InputField.selectionColor     = buttonStyle.pressedBackgroundColor;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected AppSkin.ButtonStyleName m_ButtonStyleName = default;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected TMP_InputField m_InputField = default;

    }   // class InputFieldView

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-08-01
/// ===========================================================================
