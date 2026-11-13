namespace KitchenDesign.UI.Views
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;

    public class InputFieldView : MelcherMonoBehaviour
    {
        protected void Awake()
        {
            AppSkinSO currentSkin = AppProfileStore.Skin;

            ButtonStyleSO buttonStyle =
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
        protected ButtonStyleName m_ButtonStyleName = default;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected TMP_InputField m_InputField = default;

    }   // class InputFieldView

}   // namespace KitchenDesign.UI.Views

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-08-01
/// ===========================================================================
