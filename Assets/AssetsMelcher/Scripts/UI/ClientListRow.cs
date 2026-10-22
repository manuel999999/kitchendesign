namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;
    using TMPro;

    public class ClientListRow : MelcherMonoBehaviour
    {
        // =====================================================================
        // Events
        // =====================================================================

        public static event Action<ClientListRow> onInfo;
        public static event Action<ClientListRow> onSelect;

        // =====================================================================
        // Properties
        // =====================================================================

        public int rowIndex => m_RowIndex;

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetFields(UserClient client, int rowIndex)
        {
            m_ClientNameLabel.text = string.Join(", ", client.lastName, client.firstName);
            m_RowIndex = rowIndex;
        }

        protected void OnEnable()
        {
            m_InfoButton.onClick += EmitInfoEvent;
            m_SelectButton.onClick += EmitSelectEvent;
        }

        protected void OnDisable()
        {
            m_InfoButton.onClick -= EmitInfoEvent;
            m_SelectButton.onClick -= EmitSelectEvent;
        }

        protected void EmitInfoEvent(MelcherButton caller)
        {
            onInfo?.Invoke(this);
        }

        protected void EmitSelectEvent(MelcherButton caller)
        {
            onSelect?.Invoke(this);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Tooltip("A button that shows the client's profile info.")]
        [SerializeField] protected MelcherButton m_InfoButton = null;

        [SerializeField] protected TMP_Text m_ClientNameLabel = null;

        [Tooltip("A button that selects the client.")]
        [SerializeField] protected MelcherButton m_SelectButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected int m_RowIndex;

    }   // class ClientListRow

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
