namespace Melcher
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Linq;
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// Handles displaying the list of client entries.
    /// </summary>
    public class ClientListUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void Refresh()
        {
            m_ClientList.SetSortModeWithoutNotify(m_SortMode);

            FilterList();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_ClientList.onRefresh += FetchList;
            m_ClientList.onCreate += AddClient;
            m_ClientList.recyclingList.ItemCallback += BindItem;
            m_ClientList.onSearchChanged += SearchChanged;
            m_ClientList.onSortModeChanged += SortChanged;

            ClientListRow.onInfo += ShowClientInfo;
            ClientListRow.onSelect += ShowClientQuotes;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ClientList.onRefresh -= FetchList;
            m_ClientList.onCreate -= AddClient;
            m_ClientList.recyclingList.ItemCallback -= BindItem;

            ClientListRow.onInfo -= ShowClientInfo;
            ClientListRow.onSelect -= ShowClientQuotes;
        }

        protected override void DoShow()
        {
            base.DoShow();
        }

        protected void FetchList(MelcherButton caller)
        {
            EventBridge.Menu.showFullScreenSpinner.Invoke(true);

            ProFitWeb.GetClientList(
                ProFitGlobals.user,
                onSuccess: OnSuccess,
                onError: (request, error, jsonValue) =>
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                }
            );

            void OnSuccess(List<UserClient> clientList)
            {
                EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                ProFitGlobals.userClients = clientList;

                FilterList();

                m_ClientList.ScrollToTop();
            }
        }

        protected void SearchChanged(string searchInput)
        {
            FilterList();
        }

        protected void SortChanged(ListControllerSortMode sortMode)
        {
            m_SortMode = sortMode;
            FilterList();
        }

        protected void FilterList()
        {
            m_FilteredClientList.Clear();

            string searchInput = m_ClientList.searchInput;

            // Use regex to handle word splitting. (Empty matches are removed.)
            string[] searchWords = Regex.Split(searchInput, @"\s+")
                .Where(word => !string.IsNullOrEmpty(word))
                .ToArray();

            List<UserClient> allClients = ProFitGlobals.userClients;

            bool HasSubstring(string haystack, string needle)
            {
                return haystack != null && haystack.Contains(needle, System.StringComparison.OrdinalIgnoreCase);
            }

            // For each client, check if they match all the search terms
            for (int i = 0; i < allClients.Count; ++i)
            {
                bool allWordsMatched = true;

                foreach (string word in searchWords)
                {
                    if (!HasSubstring(allClients[i].firstName, word) &&
                        !HasSubstring(allClients[i].lastName, word) &&
                        !HasSubstring(allClients[i].phoneNumber, word))
                    {
                        // If any word does not match, do not display this client
                        allWordsMatched = false;
                        break;
                    }
                }

                if (allWordsMatched)
                {
                    m_FilteredClientList.Add(i);
                }
            }

            // Now apply the sort mode to the filtered clients
            if (m_SortMode == ListControllerSortMode.NameDescending)
            {
                m_FilteredClientList.Sort((a, b) => -AscendingNameSort(a, b));
            }
            else if (m_SortMode == ListControllerSortMode.DateAscending)
            {
                m_FilteredClientList.Sort(AscendingDateSort);
            }
            else if (m_SortMode == ListControllerSortMode.DateDescending)
            {
                m_FilteredClientList.Sort((a, b) => -AscendingDateSort(a, b));
            }
            else if (m_SortMode == ListControllerSortMode.NameAscending)
            {
                m_FilteredClientList.Sort(AscendingNameSort);
            }
            else
            {
                MelcherLog.LogWarningEditorOnly("Unhandled sort mode: " + m_SortMode);
            }

            m_ClientList.SetRowCount(m_FilteredClientList.Count);
        }

        protected int AscendingNameSort(int index1, int index2)
        {
            int compare = string.Compare(
                ProFitGlobals.userClients[index1].lastName,
                ProFitGlobals.userClients[index2].lastName,
                System.StringComparison.OrdinalIgnoreCase
            );
            if (compare != 0)
            {
                return compare;
            }

            compare = string.Compare(
                ProFitGlobals.userClients[index1].firstName,
                ProFitGlobals.userClients[index2].firstName,
                System.StringComparison.OrdinalIgnoreCase
            );
            if (compare != 0)
            {
                return compare;
            }

            return string.Compare(
                ProFitGlobals.userClients[index1].email,
                ProFitGlobals.userClients[index2].email,
                System.StringComparison.OrdinalIgnoreCase
            );
        }

        protected int AscendingDateSort(int index1, int index2)
        {
            int id1 = ProFitGlobals.userClients[index1].id;
            int id2 = ProFitGlobals.userClients[index2].id;
            return id1.CompareTo(id2);
        }

        /// <summary>
        /// Called when the user clicks the Add Client button. This switches
        /// menu contexts to <see cref="CreateClientUI"/> so a new user can be
        /// setup.
        /// </summary>
        protected void AddClient(MelcherButton caller)
        {
            EventBridge.Menu.pushCreateClientUi.Invoke();
        }

        protected void BindItem(RecyclingListViewItem item, int itemIndex)
        {
            if (item.TryGetComponent(out ClientListRow row))
            {
                int clientIndex = m_FilteredClientList[itemIndex];
                row.SetFields(ProFitGlobals.userClients[clientIndex], itemIndex);
            }
        }

        protected void ShowClientInfo(ClientListRow row)
        {
            // Lookup client index
            if (row.rowIndex >= 0)
            {
                int clientIndex = m_FilteredClientList[row.rowIndex];
                int clientId = ProFitGlobals.userClients[clientIndex].id;
                EventBridge.Menu.pushClientInfoUi.Invoke(clientId);
            }
        }

        protected void ShowClientQuotes(ClientListRow row)
        {
            if (row.rowIndex >= 0)
            {
                int clientIndex = m_FilteredClientList[row.rowIndex];
                int clientId = ProFitGlobals.userClients[clientIndex].id;
                ProFitGlobals.activeClientId = clientId;
                EventBridge.Menu.pushClientQuoteListUi.Invoke();
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ClientListUI))]

        [SerializeField] protected TMP_Text m_Title = null;
        [SerializeField] protected ListController m_ClientList = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        /// List of indices into <see cref="ProFitGlobals.userClients"/>
        /// </summary>
        protected List<int> m_FilteredClientList = new List<int>();
        protected ListControllerSortMode m_SortMode = ListControllerSortMode.NameAscending;

        // =====================================================================
        // Nested types
        // =====================================================================


    }   // class ClientListUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
