namespace Melcher
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    ///     Changes a graphic's colour to match the app theme. This is used on
    ///     background images like those on the designer scene's toolbar.
    /// </summary>
    public class BackgroundView : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        protected void Awake()
        {
            AppSkin currentSkin = ProFitGlobals.appOptions.appSkin;

            ButtonStyle backgroundStyle =
                currentSkin.GetButtonStyleFromName(backgroundStyleName);

            m_Graphic.color = backgroundStyle.normalBackgroundColor;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected AppSkin.ButtonStyleName backgroundStyleName = default;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Graphic m_Graphic = default;

    }   // class BackgroundView

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-08-16
/// ===========================================================================
