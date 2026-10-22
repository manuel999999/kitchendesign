namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// TODO: Use this as a base class for all toolbar buttons. It would
    /// help to have the buttons stay highlighted when clicked so it's easier
    /// to return back to browsing.
    /// </summary>
    public abstract class ToolbarButton : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public Sprite sprite
        {
            get => m_Icon.sprite;
            set => m_Icon.sprite = value;
        }

        public string phrase
        {
            get => m_Label.phrase;
            set
            {
                DoSetPhrase(value);
            }
        }

        protected abstract ToolbarButton highlightedButton
        {
            get;
            set;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Set whether the button is greyed out. Greyed out buttons use
        ///     a different material (to render their icon in a flat grey
        ///     color) and they do not emit their onClick events. Buttons are
        ///     not greyed-out by default.
        /// </summary>
        /// <param name="isGreyed"></param>
        /// <remarks>
        ///     Not currently used. It was developed for a feature that morphed
        ///     into something else. It is kept here in case it is ever useful
        ///     in the future.
        /// </remarks>
        public void SetIsGreyedOut(bool isGreyed)
        {
            m_IsGreyedOut = isGreyed;

            m_Icon.material = m_IsGreyedOut ? m_GreyedOutSpriteMaterial : null;// m_Icon.defaultMaterial;
        }

        protected virtual void Awake()
        {
            m_PersistentHighlight.gameObject.SetActive(false);
        }

        protected virtual void OnEnable()
        {
            m_Button.onClick += OnClicked;
        }

        protected virtual void OnDisable()
        {
            m_Button.onClick -= OnClicked;
        }

        protected virtual void OnClicked(MelcherButton caller)
        {
            if (!m_IsGreyedOut)
            {
                Highlight();
                DoEmitClickEvent();
            }
        }

        protected virtual void Highlight()
        {
            if (m_UsePersistentHighlight)
            {
                // Disable the previous button's highlight
                ToolbarButton previousHighlightedButton = highlightedButton;
                if (previousHighlightedButton)
                {
                    previousHighlightedButton.m_PersistentHighlight.gameObject.SetActive(false);
                }

                // Activate our highlight
                highlightedButton = this;
                m_PersistentHighlight.gameObject.SetActive(highlightedButton == this);
            }
        }

        protected virtual void DoSetPhrase(string phrase)
        {
            m_Label.phrase = phrase;
        }

        protected abstract void DoEmitClickEvent();

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ToolbarButton))]

        [SerializeField] protected MelcherLocalizedText m_Label = null;
        [SerializeField] protected Image m_Icon = null;
        [SerializeField] protected Image m_Background = null;
        [SerializeField] protected Image m_PersistentHighlight = null;

        [Tooltip("When true, this button will remaing highlighted after it " +
            "is clicked. This is to make it easier to see which option was " +
            "last chosen in the toolbar scroll list but it isn't ideal for " +
            "the breadcrumb buttons. If this is false, the button should not " +
            "remain highlighted after it is clicked.")]
        [SerializeField] protected bool m_UsePersistentHighlight = true;

        [SerializeField]
        [Tooltip("The material to use when the sprite is greyed out.")]
        protected Material m_GreyedOutSpriteMaterial = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private MelcherButton m_Button = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     Whether the button is greyed out or not. Set in <see
        ///     cref="SetIsGreyedOut(bool)"/> and used in <see
        ///     cref="OnClicked(MelcherButton)"/>.
        /// </summary>
        protected bool m_IsGreyedOut;

    }   // class ToolbarButton

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-22
/// ===========================================================================
