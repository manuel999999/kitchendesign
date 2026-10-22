namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    /// A UI meant for prompts that can be dismissed by clicking outside the
    /// prompt's panel.
    /// </summary>
    public class BasePromptUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Background.onClick += OnClickBackground;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_Background.onClick -= OnClickBackground;
        }

        protected void OnClickBackground(MelcherButton caller)
        {
            Hide();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(BasePromptUI))]

        [SerializeField] protected MelcherButton m_Background = null;


    }   // class BasePromptUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-03
/// ===========================================================================
