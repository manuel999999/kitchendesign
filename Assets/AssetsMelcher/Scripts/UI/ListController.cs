namespace KitchenDesign
{
    using Melcher;
    using System;
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// Provides access to the controls (search, sort, refresh, create, empty
    /// label) to
    /// a list
    /// </summary>
    public class ListController : MelcherMonoBehaviour
    {
        public event Action<string> onSearchChanged;

        public event Action<ListControllerSortMode> onSortModeChanged;

        public event Action<MelcherButton> onRefresh
        {
            add { m_RefreshButton.onClick += value; }
            remove { m_RefreshButton.onClick -= value; }
        }

        public event Action<MelcherButton> onCreate
        {
            add { m_CreateButton.onClick += value; }
            remove { m_CreateButton.onClick -= value; }
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public RecyclingListView recyclingList => m_ListView;

        public string searchInput
        {
            get => m_SearchField.text;
            set => m_SearchField.text = value;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetSortModeWithoutNotify(ListControllerSortMode sortMode)
        {
            m_SortButton.GetComponentInChildren<MelcherLocalizedText>().phrase = sortMode.phrase;
        }

        public void ScrollToTop()
        {
            m_ListView.VerticalNormalizedPosition = 0.0f;
        }

        public void SetRowCount(int count)
        {
            if (count == m_ListView.RowCount)
            {
                // Force a refresh even if the row count hasn't changed
                m_ListView.Refresh();
            }
            else
            {
                // Row count changes will automatically refresh the list
                m_ListView.RowCount = count;
            }

            m_EmptyListLabel.gameObject.SetActive(count <= 0);
            m_EmptyListLabel.phrase = m_SearchField.text.Length > 0 ? "No matches" : "No entries";
        }

        protected void Awake()
        {
            // Default to showing the 'no entries' label
            m_EmptyListLabel.gameObject.SetActive(true);
        }

        protected void OnEnable()
        {
            m_SearchField.onValueChanged.AddListener(EmitSearchChangedEvent);
            m_SortButton.onClick += ShowSortPrompt;
        }

        protected void OnDisable()
        {
            m_SearchField.onValueChanged.RemoveListener(EmitSearchChangedEvent);
            m_SortButton.onClick -= ShowSortPrompt;
        }

        protected void EmitSearchChangedEvent(string searchInput)
        {
            onSearchChanged?.Invoke(searchInput);
        }

        protected void ShowSortPrompt(MelcherButton caller)
        {
            EventBridge.Menu.showSortModeUi.Invoke(EmitSortChangedEvent);
        }

        protected void EmitSortChangedEvent(ListControllerSortMode sortMode)
        {
            // Match the button text to the selected sort mode
            m_SortButton.GetComponentInChildren<MelcherLocalizedText>().phrase = sortMode.phrase;

            // Notify listeners that the sort mode has changed
            onSortModeChanged?.Invoke(sortMode);
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected RecyclingListView m_ListView = null;
        [SerializeField] protected ListControllerSortMode m_SortMode = ListControllerSortMode.NameAscending;

        [Space]
        [SerializeField] protected TMP_InputField m_SearchField = null;
        [SerializeField] protected MelcherButton m_SortButton = null;
        [SerializeField] protected MelcherButton m_RefreshButton = null;
        [SerializeField] protected MelcherButton m_CreateButton = null;

        [Space]
        [SerializeField] protected MelcherLocalizedText m_EmptyListLabel  = null;

    }   // class ListController

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
