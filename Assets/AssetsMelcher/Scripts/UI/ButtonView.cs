namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;
    using TMPro;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Handles the visuals of a <see cref="MelcherButton"/>.
    /// </summary>
    public class ButtonView : MelcherMonoBehaviour
    {
        protected void Awake()
        {
            IEnumerable<Graphic> allGraphics =
                m_ForegroundGraphics.Concat(m_BackgroundGraphics);

            foreach (Graphic graphic in allGraphics)
            {
                if (graphic && graphic is TMP_Text text)
                {
                    m_DefaultFontStylesInGraphics[text] = text.fontStyle;
                }
            }
        }

        protected void OnEnable()
        {
            HandleAppearanceTransition(m_Button.GetAppearance(), instant: true);
            m_Button.onAppearanceChanged += OnAppearanceChanged;
        }

        protected void OnDisable()
        {
            m_Button.onAppearanceChanged -= OnAppearanceChanged;
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

            FontStyleOverride fontStyleOverride =
                buttonStyle.FontStyleOverrideForAppearance(appearance);

            (Color foregroundColor, Color backgroundColor) =
                buttonStyle.ColorsForAppearance(appearance);

            if (instant)
            {
                m_Sequence.KillIfActive();

                foreach (Graphic graphic in m_ForegroundGraphics)
                {
                    if (!graphic)
                    {
                        continue;
                    }

                    graphic.color = foregroundColor;

                    if (graphic is TMP_Text text)
                    {
                        ApplyFontStyleOverride(fontStyleOverride, text);
                    }
                }

                foreach (Graphic graphic in m_BackgroundGraphics)
                {
                    if (!graphic)
                    {
                        continue;
                    }

                    graphic.color = backgroundColor;

                    if (graphic is TMP_Text text)
                    {
                        ApplyFontStyleOverride(fontStyleOverride, text);
                    }
                }

                return;
            }

            m_Sequence.Reset(
                DOTween.Sequence()
                    .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable)
            );

            foreach (Graphic graphic in m_ForegroundGraphics)
            {
                if (!graphic)
                {
                    continue;
                }

                m_Sequence.value.Join(
                    graphic.DOColor(foregroundColor, m_TweenDuration)
                        .SetEase(Ease.Linear)
                );

                if (graphic is TMP_Text text)
                {
                    ApplyFontStyleOverride(fontStyleOverride, text);
                }
            }

            foreach (Graphic graphic in m_BackgroundGraphics)
            {
                if (!graphic)
                {
                    continue;
                }

                m_Sequence.value.Join(
                    graphic.DOColor(backgroundColor, m_TweenDuration)
                        .SetEase(Ease.Linear)
                );

                if (graphic is TMP_Text text)
                {
                    ApplyFontStyleOverride(fontStyleOverride, text);
                }
            }
        }

        protected void ApplyFontStyleOverride(FontStyleOverride styleOverride, TMP_Text text)
        {
            if (m_DefaultFontStylesInGraphics.TryGetValue(text, out FontStyles defaultFontStyle))
            {
                text.fontStyle = styleOverride.Apply(defaultFontStyle);
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] protected float m_TweenDuration = 0.1f;
        [SerializeField] protected AppSkin.ButtonStyleName m_ButtonStyleName = default;
        [SerializeField] protected Graphic[] m_ForegroundGraphics = null;
        [SerializeField] protected Graphic[] m_BackgroundGraphics = null;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Up)]
        protected MelcherButton m_Button = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected RecyclableTween<Sequence> m_Sequence = new RecyclableTween<Sequence>();

        /// <summary>
        ///     Pairs <see cref="TMP_Text"/> components found in <see
        ///     cref="m_ForegroundGraphics"/> and <see
        ///     cref="m_BackgroundGraphics"/> to their font style that was
        ///     active during Awake. These values are then used when changing
        ///     the font style according to the button's appearance style.
        /// </summary>
        protected Dictionary<TMP_Text, FontStyles> m_DefaultFontStylesInGraphics = new();

    }   // class ButtonView

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-24
/// ===========================================================================
