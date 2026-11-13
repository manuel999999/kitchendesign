namespace KitchenDesign.ProductsEditor
{
    using Melcher;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor;
    using UnityEditor.UIElements;

    /// <summary>
    /// A custom selector window that lets us apply optional filtering.
    /// </summary>
    public class FilteredObjectSelector : EditorWindow
    {
        public delegate string GetRowText(int index);
        public delegate void ResultCallback(IReadOnlyList<int> selectedIndices);
        public delegate bool ExtraFilter(int toCheck);

        public static void ShowPicker(
            string          title,
            IList           content,
            GetRowText      getRowName,
            GetRowText      getRowStatus,
            ResultCallback  resultCallback,
            ExtraFilter     extraFilter = null,
            string          extraFilterText = null,
            string          extraFilterTooltip = null,
            bool            extraFilterDefaultOn = false,
            bool            allowMultipleSelection = false)
        {
            FilteredObjectSelector window =
                GetWindow<FilteredObjectSelector>(title ?? "Object Picker");

            window.SetupAndShow(
                content,
                getRowName,
                getRowStatus,
                resultCallback,
                extraFilter,
                extraFilterText,
                extraFilterTooltip,
                extraFilterDefaultOn,
                allowMultipleSelection
            );
        }

        public void SetupAndShow(
            IList           content,
            GetRowText      getRowName,
            GetRowText      getRowStatus,
            ResultCallback  resultCallback,
            ExtraFilter     extraFilter,
            string          extraFilterText,
            string          extraFilterTooltip,
            bool            extraFilterDefaultOn,
            bool            allowMultipleSelection)
        {
            m_IsVisible = true;

            m_Content = content;
            _getRowName = getRowName;
            _getRowStatus = getRowStatus;
            _resultCallback = resultCallback;
            _selectedIndices.Clear();

            _extraFilter = extraFilter;

            ToolbarToggle extraFilterToggle = GetExtraFilterToggle();
            extraFilterToggle.text = !string.IsNullOrWhiteSpace(extraFilterText) ? extraFilterText : "Apply Extra Filter";
            extraFilterToggle.tooltip = extraFilterTooltip;
            extraFilterToggle.visible = extraFilter != null;
            extraFilterToggle.style.display = extraFilter != null ? DisplayStyle.Flex : DisplayStyle.None;
            extraFilterToggle.value = extraFilter != null && extraFilterDefaultOn;

            ListView listView = GetTable();
            listView.selectionType =
                allowMultipleSelection ?
                    SelectionType.Multiple :
                    SelectionType.Single;

            TextField statusField = GetStatusField();
            statusField.SetValueWithoutNotify("");
            statusField.style.display = _getRowStatus != null ? DisplayStyle.Flex : DisplayStyle.None;

            RefreshSearch();

            Show();
        }

        private void CreateGUI()
        {
            VisualElement document = m_DocumentAsset.Instantiate();
            document.StretchToParentSize();
            rootVisualElement.Add(document);

            ToolbarSearchField searchField = GetSearchField();
            searchField.RegisterCallback<ChangeEvent<string>>(OnSearchChange);

            ToolbarToggle customFilterToggle = GetExtraFilterToggle();
            customFilterToggle.RegisterCallback<ChangeEvent<bool>>(OnExtraFilterToggle);

            Func<VisualElement> makeItem = static () => new Label();

            Action<VisualElement, int> bindItem =
                (row, i) =>
                {
                    Label label = (Label)row;
                    label.userData = i;

                    int actualIndex =
                        m_DisplayedData == m_FilteredData ?
                            m_FilteredData[i] :
                            i;

                    label.text = _getRowName(actualIndex);
                };

            ListView listView = GetTable();
            listView.fixedItemHeight = 20;
            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsChosen += OnItemsChosen;
            listView.selectedIndicesChanged += OnSelectedIndicesChanged;
            listView.itemsSource = null;
        }

        /// <summary>
        /// Act as a pop-up box. Close window when it loses focus.
        /// </summary>
        private void OnLostFocus()
        {
            CloseWithoutCrashing();
        }

        /// <summary>
        /// Called when the editor window is closed. Trigger our callback.
        /// </summary>
        private void OnDestroy()
        {
            ToolbarSearchField searchField = GetSearchField();
            searchField.UnregisterCallback<ChangeEvent<string>>(OnSearchChange);

            ToolbarToggle customFilterToggle = GetExtraFilterToggle();
            customFilterToggle.UnregisterCallback<ChangeEvent<bool>>(OnExtraFilterToggle);

            ListView listView = GetTable();
            listView.makeItem = null;
            listView.bindItem = null;
            listView.itemsChosen -= OnItemsChosen;
            listView.selectedIndicesChanged -= OnSelectedIndicesChanged;
            listView.itemsSource = null;

            m_Content = null;
            m_FilteredData = null;
            m_DisplayedData = null;

            _extraFilter = null;

            _resultCallback?.Invoke(_selectedIndices);
            _resultCallback = null;
        }

        /// <summary>
        /// Ensures that <see cref="Close"/> is only called once per window.
        /// Otherwise, we may crash because <see cref="OnLostFocus"/> and
        /// <see cref="OnItemsChosen"/> both try to close the window.
        /// </summary>
        private void CloseWithoutCrashing()
        {
            if (m_IsVisible)
            {
                m_IsVisible = false;
                Close();
            }
        }

        private void OnItemsChosen(IEnumerable<object> selection)
        {
            ListView listView = GetTable();

            if (m_DisplayedData == m_FilteredData)
            {
                _selectedIndices.AddRange(
                    listView.selectedIndices.Select(x => m_FilteredData[x])
                );
            }
            else
            {
                _selectedIndices.AddRange(listView.selectedIndices);
            }

            CloseWithoutCrashing();
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> indices)
        {
            if (_getRowStatus == null)
            {
                return;
            }

            IEnumerator<int> enumerator = indices.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return;
            }

            int lastIndex = -1;

            // Get the last index.
            do
            {
                lastIndex = enumerator.Current;
            } while (enumerator.MoveNext());

            if (m_DisplayedData == m_FilteredData)
            {
                lastIndex = m_FilteredData[lastIndex];
            }

            TextField statusField = GetStatusField();
            statusField.SetValueWithoutNotify(_getRowStatus(lastIndex));
        }

        private void OnSearchChange(ChangeEvent<string> changeEvent)
        {
            RefreshSearch(changeEvent.newValue);
        }

        private void OnExtraFilterToggle(ChangeEvent<bool> changeEvent)
        {
            RefreshSearch();
        }

        private ToolbarSearchField GetSearchField()
        {
            return rootVisualElement.Q<ToolbarSearchField>("SearchField");
        }

        private ToolbarToggle GetExtraFilterToggle()
        {
            return rootVisualElement.Q<ToolbarToggle>("CustomFilterToggle");
        }

        private ListView GetTable()
        {
            return rootVisualElement.Q<ListView>("Table");
        }

        private TextField GetStatusField()
        {
            return rootVisualElement.Q<TextField>("StatusField");
        }

        private void RefreshSearch()
        {
            RefreshSearch(GetSearchField().value);
        }

        private void RefreshSearch(string searchInput)
        {
            m_FilteredData.Clear();

            bool isExtraFilterApplied =
                _extraFilter != null && GetExtraFilterToggle().value;

            if (!string.IsNullOrWhiteSpace(searchInput))
            {
                // Use regex to handle word splitting. (Empty matches are removed.)
                string[] searchWords =
                    Regex.Split(searchInput, @"\s+")
                        .Where(word => !string.IsNullOrEmpty(word))
                        .ToArray();

                for (int i = 0; i < m_Content.Count; ++i)
                {
                    int matchingSearchWordCount = 0;

                    string name = _getRowName(i) ?? "";

                    foreach (string word in searchWords)
                    {
                        if (name.Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            ++matchingSearchWordCount;
                        }
                    }

                    if (matchingSearchWordCount == searchWords.Length &&
                        (!isExtraFilterApplied || DoesPassExtraFilter(i)))
                    {
                        m_FilteredData.Add(i);
                    }
                }

                m_DisplayedData = m_FilteredData;
            }
            else if (isExtraFilterApplied)
            {
                // Apply extra filter if active
                for (int i = 0; i < m_Content.Count; ++i)
                {
                    if (DoesPassExtraFilter(i))
                    {
                        m_FilteredData.Add(i);
                    }
                }

                m_DisplayedData = m_FilteredData;
            }
            else
            {
                // No filter active; just display the raw content
                m_DisplayedData = m_Content;
            }

            ListView listView = GetTable();
            listView.itemsSource = m_DisplayedData;
            listView.RefreshItems();
        }

        private bool DoesPassExtraFilter(int indexToCheck)
        {
            if (_extraFilter != null)
            {
                return _extraFilter.Invoke(indexToCheck);
            }

            return true;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Space]

        [SerializeField] protected VisualTreeAsset m_DocumentAsset = default;

        private bool m_IsVisible;

        private GetRowText _getRowName;
        private GetRowText _getRowStatus;

        private IList m_Content;

        /// <summary>
        /// This is <see cref="m_Content"/> filtered by the search field and
        /// extra filter (if provided).
        /// </summary>
        private List<int> m_FilteredData = new List<int>();

        /// <summary>
        /// This either points to <see cref="m_Content"/> (if no filter is
        /// applied) or <see cref="m_FilteredData"/> (if there is a search
        /// value or the extra field is enabled).
        /// </summary>
        private IList m_DisplayedData;

        /// <summary>
        /// Optional, extra filter. Can be toggled freely if available. Can be
        /// combined with search filter.
        /// </summary>
        private ExtraFilter _extraFilter;

        private ResultCallback _resultCallback;
        private List<int> _selectedIndices = new();

    }   // class FilteredObjectSelector

}   // namespace KitchenDesign

/// ============================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-13
/// ===========================================================================
