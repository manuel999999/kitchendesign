namespace KitchenDesign
{
    using Melcher;
    using DG.Tweening;
    using UnityEngine;

    /// <summary>
    ///     This manages a little pop-up / tooltip-like hint that helps
    ///     tutorialize the UI. It was added to help explain the toolbar's
    ///     breadcrumb buttons (that they can be pressed to return to a
    ///     previous menu).
    /// </summary>
    public class QuickHint : MelcherMonoBehaviour
    {
        // ====================================================================
        // Events
        // ====================================================================

        public event System.Action<QuickHint> onManuallyClosed;

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     Check if this hint is already displayed. We use the close
        ///     button's interactable field to determine this. It is
        ///     interactable while visible and non-interactive when
        ///     closed/closing.
        /// </summary>
        public bool isVisible => m_CloseButton.interactable;

        // ====================================================================
        // Methods
        // ====================================================================

        public void Show(float delayDuration)
        {
            m_CloseButton.interactable = true;

            m_ScaleTween.Reset(
                m_Transform.DOScale(endValue: Vector3.one, duration: 0.2f)
                    .SetDelay(delayDuration)
            );
        }

        public void Close()
        {
            m_CloseButton.interactable = false;

            m_ScaleTween.Reset(
                m_Transform.DOScale(endValue: Vector3.zero, duration: 0.2f)
            );
        }

        private void Awake()
        {
            m_Transform.localScale = Vector3.zero;
            m_CloseButton.interactable = false;
        }

        private void OnEnable()
        {
            m_CloseButton.onClick += OnCloseButtonClicked;
        }

        private void OnDisable()
        {
            m_CloseButton.onClick -= OnCloseButtonClicked;
        }

        private void OnCloseButtonClicked(MelcherButton sender)
        {
            Close();

            onManuallyClosed?.Invoke(this);
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        private MelcherButton m_CloseButton;

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        private RecyclableTween<Tween> m_ScaleTween = new ();
    }   // class QuickHint

}   // namespace KitchenDesign

