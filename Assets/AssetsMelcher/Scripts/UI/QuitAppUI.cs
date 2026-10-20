namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// The prompt that shows up to confirm the user wants to quit the app.
    /// </summary>
    public class QuitAppUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CancelButton.onClick  += OnClickCancel;
            m_QuitButton.onClick    += OnClickQuit;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_CancelButton.onClick  -= OnClickCancel;
            m_QuitButton.onClick    -= OnClickQuit;
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            Hide();
        }

        protected void OnClickQuit(MelcherButton caller)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif  // if UNITY_EDITOR
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DeleteClientUI))]

        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_QuitButton = null;

    }   // class QuitAppUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-20
/// ===========================================================================
