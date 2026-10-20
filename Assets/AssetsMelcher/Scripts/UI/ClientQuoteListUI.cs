namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using TMPro;
    using MEC;

    /// <summary>
    /// Handles displaying the list of client's quotes/designs.
    /// </summary>
    public class ClientQuoteListUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void Refresh()
        {
            m_QuoteList.SetSortModeWithoutNotify(m_SortMode);

            FilterList();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_QuoteList.onRefresh   += FetchList;
            m_QuoteList.onCreate    += CreateNewDesign;
            m_QuoteList.recyclingList.ItemCallback += BindItem;
            m_QuoteList.onSearchChanged     += SearchChanged;
            m_QuoteList.onSortModeChanged   += SortChanged;

            ClientListRow.onInfo += ShowClientInfo;

            QuoteListRow.onRequestRename    += ShowRenamePrompt;
            QuoteListRow.onLoadDesign       += LoadDesign;
            QuoteListRow.onShowPdf          += OpenPdf;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_QuoteList.onRefresh   -= FetchList;
            m_QuoteList.onCreate    -= CreateNewDesign;
            m_QuoteList.recyclingList.ItemCallback -= BindItem;
            m_QuoteList.onSearchChanged     -= SearchChanged;
            m_QuoteList.onSortModeChanged   -= SortChanged;

            ClientListRow.onInfo -= ShowClientInfo;

            QuoteListRow.onRequestRename    -= ShowRenamePrompt;
            QuoteListRow.onLoadDesign       -= LoadDesign;
            QuoteListRow.onShowPdf          -= OpenPdf;
        }

        /// <summary>
        /// Show the UI and setup the client widget at the top so the user
        /// can still the client's details without needing to go back to the
        /// client list.
        ///
        /// The widget at the top is just a clone of one of the row elements
        /// from the client list UI. To prevent it's click callback from
        /// being mistaken for one of the widgets in the client list row, we
        /// assign an invalid row index.
        /// </summary>
        protected override void DoShow()
        {
            base.DoShow();

            int clientId = ProFitGlobals.activeClientId.Value;
            int clientIndex = ProFitGlobals.userClients.FindIndex(
                x => x.id == clientId
            );

            // Use a row index of -1 to avoid the ClientListUI handling our
            // row's onInfoClick events
            m_ClientRow.SetFields(ProFitGlobals.userClients[clientIndex], rowIndex: -1);
        }

        protected void FetchList(MelcherButton caller)
        {
            EventBridge.Menu.showFullScreenSpinner.Invoke(true);

            int clientId = ProFitGlobals.activeClientId.Value;

            // Refetch the client to get their quote list
            ProFitWeb.GetClient(
                ProFitGlobals.user,
                clientId,
                onSuccess: OnSuccess,
                onError: (builder, error, jsonValue) =>
                {
                    EventBridge.Menu.showFullScreenSpinner.Invoke(false);
                }
            );

            void OnSuccess(UserClient client)
            {
                EventBridge.Menu.showFullScreenSpinner.Invoke(false);

                int clientIndex = ProFitGlobals.userClients.FindIndex(x => x.id == clientId);
                if (clientIndex < 0)
                {
                    return;
                }

                ProFitGlobals.userClients[clientIndex] = client;

                FilterList();

                m_QuoteList.ScrollToTop();
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
            m_FilteredQuoteIndices.Clear();

            string searchInput = m_QuoteList.searchInput;

            // Use regex to handle word splitting. (Empty matches are removed.)
            string[] searchWords = Regex.Split(searchInput, @"\s+")
                .Where(word => !string.IsNullOrEmpty(word))
                .ToArray();

            m_ActiveClientQuotes = null;
            m_ActiveClientQuoteDateTimes.Clear();

            int clientId = ProFitGlobals.activeClientId.Value;
            int clientIndex = ProFitGlobals.userClients.FindIndex(
                x => x.id == clientId
            );
            if (clientIndex >= 0)
            {
                m_ActiveClientQuotes = ProFitGlobals.userClients[clientIndex].quotes;
            }

            if (m_ActiveClientQuotes != null)
            {
                const StringComparison CaseInsensitive = StringComparison.OrdinalIgnoreCase;

                // For each quote, check if they match all the search terms
                for (int i = 0; i < m_ActiveClientQuotes.Count; ++i)
                {
                    // Get DateTime from all timestamps for faster compare
                    DateTime time = DateTime.MinValue;
                    if (m_ActiveClientQuotes[i].saveData.TryGetValueFromJson("timeStamp", out string timestamp))
                    {
                        if (!DateTime.TryParse(timestamp, out time))
                        {
                            MelcherLog.LogWarningEditorOnly(
                                $"Failed to parse timestamp \"{timestamp}\" (quote index:{i}, ID:{m_ActiveClientQuotes[i].id})"
                            );
                        }
                    }
                    else
                    {
                        MelcherLog.LogWarningEditorOnly($"No timestamp in quote (index:{i}, ID:{m_ActiveClientQuotes[i].id})");
                    }
                    m_ActiveClientQuoteDateTimes.Add(time);

                    bool allWordsMatched = true;

                    foreach (string word in searchWords)
                    {
                        if (!m_ActiveClientQuotes[i].saveName.Contains(word, CaseInsensitive))
                        {
                            // If any word does not match, do not display this quote
                            allWordsMatched = false;
                            break;
                        }
                    }

                    if (allWordsMatched)
                    {
                        m_FilteredQuoteIndices.Add(i);
                    }
                }

                // Now apply the sort mode to the filtered clients
                if (m_SortMode == ListControllerSortMode.NameDescending)
                {
                    m_FilteredQuoteIndices.Sort((a, b) => -AscendingNameSort(a, b));
                }
                else if (m_SortMode == ListControllerSortMode.DateAscending)
                {
                    MelcherLog.LogEditorOnly("Sorting by date ascending");
                    m_FilteredQuoteIndices.Sort(AscendingDateSort);
                }
                else if (m_SortMode == ListControllerSortMode.DateDescending)
                {
                    MelcherLog.LogEditorOnly("Sorting by date descending");
                    m_FilteredQuoteIndices.Sort((a, b) => -AscendingDateSort(a, b));
                }
                else if (m_SortMode == ListControllerSortMode.NameAscending)
                {
                    m_FilteredQuoteIndices.Sort(AscendingNameSort);
                }
                else
                {
                    MelcherLog.LogWarningEditorOnly("Unhandled sort mode: " + m_SortMode);
                }
            }

            m_QuoteList.SetRowCount(m_FilteredQuoteIndices.Count);
        }

        protected int AscendingNameSort(int index1, int index2)
        {
            int compare = string.Compare(
                m_ActiveClientQuotes[index1].saveName,
                m_ActiveClientQuotes[index2].saveName,
                StringComparison.OrdinalIgnoreCase
            );
            if (compare != 0)
            {
                return compare;
            }

            return m_ActiveClientQuotes[index1].id.CompareTo(
                m_ActiveClientQuotes[index2].id
            );
        }

        protected int AscendingDateSort(int index1, int index2)
        {
            return m_ActiveClientQuoteDateTimes[index1].CompareTo(
                m_ActiveClientQuoteDateTimes[index2]
            );
        }

        protected void CreateNewDesign(MelcherButton caller)
        {
            Timing.RunCoroutine(CreateNewDesignAsync());

            IEnumerator<float> CreateNewDesignAsync()
            {
                EventBridge.Menu.showFullScreenSpinner?.Invoke(true);
                EventBridge.Menu.showBackgroundSpinner?.Invoke(false);

                while (ProFitGlobals.logInCoroutineState != ProFitGlobals.LogInCoroutineState.Finished)
                {
                    yield return Timing.WaitForOneFrame;
                }

                MelcherSceneManager.LoadSceneAsync(
                    MelcherSceneRequest.FromName("DesignerScene"),
                    MelcherSceneLoadMode.Single
                );
            }
        }

        protected void BindItem(RecyclingListViewItem item, int itemIndex)
        {
            if (item.TryGetComponent(out QuoteListRow row))
            {
                int quoteIndex = m_FilteredQuoteIndices[itemIndex];
                row.SetFields(
                    m_ActiveClientQuotes[quoteIndex],
                    m_ActiveClientQuoteDateTimes[quoteIndex],
                    itemIndex
                );
            }
        }

        protected void ShowClientInfo(ClientListRow row)
        {
            if (row == m_ClientRow)
            {
                EventBridge.Menu.pushClientInfoUi.Invoke(ProFitGlobals.activeClientId.Value);
            }
        }

        protected void ShowRenamePrompt(QuoteListRow row)
        {
            int quoteIndex = m_FilteredQuoteIndices[row.index];
            int quoteId = m_ActiveClientQuotes[quoteIndex].id;
            EventBridge.Menu.showQuoteInfoUi?.Invoke(ProFitGlobals.activeClientId.Value, quoteId);
        }

        protected void OpenPdf(QuoteListRow row)
        {
            int quoteIndex = m_FilteredQuoteIndices[row.index];
            string pdfEndpoint = m_ActiveClientQuotes[quoteIndex].pdfEndpoint;
            string pdfUrl = ProFitWeb.Host + pdfEndpoint;
            MelcherLog.LogVerbose(
                $"Opening PDF url: {pdfUrl}\n" +
                $"Host: {ProFitWeb.Host}\n" +
                $"Endpoint: {m_ActiveClientQuotes[quoteIndex].pdfEndpoint}"
            );
            pdfUrl = Uri.EscapeUriString(pdfUrl);
            Application.OpenURL(pdfUrl);
        }

        protected void LoadDesign(QuoteListRow row)
        {
            Timing.RunCoroutine(LoadDesignAsync());

            IEnumerator<float> LoadDesignAsync()
            {
                EventBridge.Menu.showFullScreenSpinner?.Invoke(true);
                EventBridge.Menu.showBackgroundSpinner?.Invoke(false);

                while (ProFitGlobals.logInCoroutineState != ProFitGlobals.LogInCoroutineState.Finished)
                {
                    yield return Timing.WaitForOneFrame;
                }

                int quoteIndex = m_FilteredQuoteIndices[row.index];
                ProFitGlobals.activeQuoteId = m_ActiveClientQuotes[quoteIndex].id;

                MelcherSceneManager.LoadSceneAsync(
                    MelcherSceneRequest.FromName("DesignerScene"),
                    MelcherSceneLoadMode.Single
                );

                EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Loading Design");
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ClientQuoteListUI))]

        [SerializeField] protected TMP_Text m_Title = null;
        [SerializeField] protected ClientListRow m_ClientRow = null;
        [SerializeField] protected ListController m_QuoteList = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected List<int> m_FilteredQuoteIndices = new List<int>();
        protected ListControllerSortMode m_SortMode = ListControllerSortMode.DateDescending;
        protected List<UserClientQuote> m_ActiveClientQuotes;
        protected List<DateTime> m_ActiveClientQuoteDateTimes = new List<DateTime>();

        // =====================================================================
        // Nested types
        // =====================================================================

    }   // class ClientQuoteListUI

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
