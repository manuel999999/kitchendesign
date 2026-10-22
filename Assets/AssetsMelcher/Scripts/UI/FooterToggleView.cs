namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;

    /// <summary>
    ///     Handles the transition effects of a toggle that appears on the main
    ///     menu's footer bar.
    /// </summary>
    public class FooterToggleView : MelcherMonoBehaviour
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected void OnEnable()
        {
            HandleAppearanceTransition(m_Toggle.GetAppearance(), instant: true);
            m_Toggle.onValueChanged += OnValueChanged;
            m_Toggle.onAppearanceChanged += OnAppearanceChanged;
        }

        protected void OnDisable()
        {
            m_Toggle.onValueChanged -= OnValueChanged;
            m_Toggle.onAppearanceChanged -= OnAppearanceChanged;
        }

        protected void OnValueChanged(IMelcherToggle toggle)
        {
            HandleAppearanceTransition(m_Toggle.GetAppearance(), instant: false);
        }

        protected void OnAppearanceChanged(MelcherSelectable selectable, MelcherSelectable.Appearance appearance, bool instant)
        {
            HandleAppearanceTransition(appearance, instant);
        }

        protected void HandleAppearanceTransition(MelcherSelectable.Appearance appearance, bool instant)
        {
            AppSkin currentSkin = ProFitGlobals.appOptions.appSkin;

            ButtonStyle buttonStyle =
                currentSkin.GetButtonStyleFromName(m_ButtonStyleName);

            (Color foreground, Color background) =
                buttonStyle.ColorsForAppearance(appearance);

            if (m_Label)
            {
                if (instant)
                {
                    m_Tween.KillIfActive();
                    m_Label.color = background;
                }
                else
                {
                    m_Tween.Reset(
                        m_Label.DOColor(background, m_TweenDuration)
                    );
                }
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected float m_TweenDuration = 0.1f;

        [SerializeField] protected Graphic m_Label = null;

        [SerializeField] protected AppSkin.ButtonStyleName m_ButtonStyleName = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected MelcherToggle m_Toggle;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected RecyclableTween<Tween> m_Tween = new RecyclableTween<Tween>();

    }   // class FooterToggleView

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-24
/// ===========================================================================
