namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     The prompt that shows up to confirm the user wishes to ignore their
    ///     changes in the current menu.
    /// </summary>
    public class DiscardPendingChangesUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void OnEnable()
        {
            base.OnEnable();

            m_DiscardButton.onClick += OnClickDiscard;
            m_CancelButton.onClick += OnClickKeep;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_DiscardButton.onClick -= OnClickDiscard;
            m_CancelButton.onClick -= OnClickKeep;
        }

        protected override void DoHide()
        {
            m_Callback = null;
            base.DoHide();
        }

        protected void OnClickDiscard(MelcherButton caller)
        {
            m_Callback?.Invoke(true);
            Hide();
        }

        protected void OnClickKeep(MelcherButton caller)
        {
            m_Callback?.Invoke(false);
            Hide();
        }

        internal void SetCallback(System.Action<bool> callback)
        {
            m_Callback = callback;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DiscardPendingChangesUI))]

        [SerializeField] protected MelcherLocalizedText m_TitleLabel = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_DiscardButton = null;
        [SerializeField] protected MelcherButton m_CancelButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        System.Action<bool> m_Callback;

    }

}   // namespace Melcher

