namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using System;
    using UnityEngine;
    using TMPro;

    public class QuoteListRow : MelcherMonoBehaviour
    {
        // =====================================================================
        // Events
        // =====================================================================

        public static event Action<QuoteListRow> onRequestRename;
        public static event Action<QuoteListRow> onShowPdf;
        public static event Action<QuoteListRow> onLoadDesign;

        // =====================================================================
        // Properties
        // =====================================================================

        public int index => m_Index;

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(UserClientQuote quote, DateTime lastUpdated, int index)
        {
            m_QuoteNameLabel.text = quote.saveName;
            m_LastUpdatedLabel.phrase = lastUpdated.ToString("f");
            m_Index = index;
        }

        protected void OnEnable()
        {
            m_RenameButton.onClick  += EmitRenameEvent;
            m_QuoteButton.onClick   += EmitShowPdfEvent;
            m_OpenButton.onClick    += EmitLoadDesignEvent;
        }

        protected void OnDisable()
        {
            m_RenameButton.onClick  -= EmitRenameEvent;
            m_QuoteButton.onClick   -= EmitShowPdfEvent;
            m_OpenButton.onClick    -= EmitLoadDesignEvent;
        }

        protected void EmitRenameEvent(MelcherButton caller)
        {
            onRequestRename?.Invoke(this);
        }

        protected void EmitShowPdfEvent(MelcherButton caller)
        {
            onShowPdf?.Invoke(this);
        }

        protected void EmitLoadDesignEvent(MelcherButton caller)
        {
            onLoadDesign?.Invoke(this);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected MelcherButton m_RenameButton = null;
        [SerializeField] protected TMP_Text m_QuoteNameLabel = null;
        [SerializeField] protected MelcherLocalizedText m_LastUpdatedLabel = null;
        [SerializeField] protected MelcherButton m_QuoteButton = null;
        [SerializeField] protected MelcherButton m_OpenButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected int m_Index;

    }   // class QuoteListRow

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
