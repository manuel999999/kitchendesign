namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;

    /// <summary>
    /// Handles the visuals of a <see cref="MelcherToggle"/>.
    /// </summary>
    public class ToggleView : MelcherMonoBehaviour
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

            if (m_Checkmark)
            {
                // The checkmark has a (mostly) constant color but its alpha is
                // adjusted between to hide or show its graphic. When it is
                // disabled, its colour is fully desaturated.

                Color.RGBToHSV(
                    currentSkin.toggleOnColor,
                    out float hue,
                    out float saturation,
                    out float value
                );

                if (!m_Toggle.IsInteractable())
                {
                    saturation = 0.0f;
                    value *= 0.5f;
                }

                m_Checkmark.color =
                    Color.HSVToRGB(hue, saturation, value)
                        .WithAlpha(m_Checkmark.color.a);
            }

            ButtonStyle buttonStyle =
                currentSkin.GetButtonStyleFromName(m_ButtonStyleName);

            (Color foregroundColor, Color backgroundColor) =
                buttonStyle.ColorsForAppearance(appearance);

            Color toggleColor =
                m_Toggle.isOn ? currentSkin.toggleOnColor : currentSkin.toggleOffColor;

            // This is the toggle colour but it will match the selectable's
            // highlight and pressed color when relevant.
            Color reactiveToggleColor = toggleColor;

            if (appearance != MelcherSelectable.Appearance.Normal)
            {
                reactiveToggleColor = foregroundColor;
            }

            if (m_UseNormalColorOnToggleGraphics)
            {
                if (m_Toggle.isOn)
                {
                    (Color fore, Color back) =
                        buttonStyle.ColorsForAppearance(MelcherSelectable.Appearance.Hovered);
                    reactiveToggleColor = fore;
                }
                else
                {
                    reactiveToggleColor = foregroundColor;
                }
            }

            float alpha = m_Toggle.isOn ? 1.0f : 0.0f;

            if (instant)
            {
                m_Sequence.KillIfActive();

                if (m_Checkmark)
                {
                    m_Checkmark.color = m_Checkmark.color.WithAlpha(alpha);
                }

                foreach (Graphic graphic in m_ToggleGraphics)
                {
                    if (graphic)
                    {
                        graphic.color = reactiveToggleColor;
                    }
                }

                foreach (Graphic graphic in m_ForegroundGraphics)
                {
                    if (graphic)
                    {
                        graphic.color = foregroundColor;
                    }
                }

                foreach (Graphic graphic in m_BackgroundGraphics)
                {
                    if (graphic)
                    {
                        graphic.color = backgroundColor;
                    }
                }

                return;
            }

            m_Sequence.Reset(
                DOTween.Sequence()
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
            );

            if (m_Checkmark)
            {
                m_Sequence.value.Join(
                    m_Checkmark.DOFade(alpha, m_TweenDuration)
                        .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
                        .SetEase(Ease.Linear)
                );
            }

            foreach (Graphic graphic in m_ToggleGraphics)
            {
                if (graphic != null)
                {
                    m_Sequence.value.Join(
                        graphic.DOColor(reactiveToggleColor, m_TweenDuration)
                            .SetEase(Ease.Linear)
                    );
                }
            }

            foreach (Graphic graphic in m_ForegroundGraphics)
            {
                if (graphic != null)
                {
                    m_Sequence.value.Join(
                        graphic.DOColor(foregroundColor, m_TweenDuration)
                            .SetEase(Ease.Linear)
                    );
                }
            }

            foreach (Graphic graphic in m_BackgroundGraphics)
            {
                if (graphic != null)
                {
                    m_Sequence.value.Join(
                        graphic.DOColor(backgroundColor, m_TweenDuration)
                            .SetEase(Ease.Linear)
                    );
                }
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected float m_TweenDuration = 0.1f;
        [SerializeField] protected Graphic m_Checkmark = null;

        [Tooltip("Extra canvas items that will colour match with the" +
            "checkmark's state, but won't become transparent like the " +
            "checkmark when it is unchecked.")]
        [SerializeField] protected Graphic[] m_ToggleGraphics = null;

        [SerializeField] protected Graphic[] m_ForegroundGraphics = null;
        [SerializeField] protected Graphic[] m_BackgroundGraphics = null;

        [SerializeField] protected AppSkin.ButtonStyleName m_ButtonStyleName = default;

        [Space]

        [SerializeField]
        protected bool m_UseNormalColorOnToggleGraphics = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected MelcherToggle m_Toggle;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected RecyclableTween<Sequence> m_Sequence = new RecyclableTween<Sequence>();

    }   // class ToggleView

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-24
/// ===========================================================================
