namespace KitchenDesign
{
    using Melcher;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Entry = ProductVisualDatabaseEntryFile.Entry;

    public class ProductVisualDatabaseSetsManager : EditorWindow
    {
        private const string WINDOW_TITLE = "Product Visual Database Sets";
        private const string MENU_PATH = "Tools/Melcher Studios/Pro-Fit/Product Visual Database Sets";
        private const string SETS_FOLDER_NAME = "ProductVisualDatabaseSets";

        private const string COLUMN_POSITION = "Position";
        private const string COLUMN_NAME = "Name";
        private const string COLUMN_ID = "Id";

        private const string STATUS_TO_APPEND = "To append";
        private const string STATUS_IN_DATABASE = "In database";
        private const string STATUS_NOT_IN_PROJECT = "Not in project";

        private const string SET_ASIDE_SESSION_KEY = "Melcher.ProductVisualDatabaseSetsManager.SetAsideGuids";

        [MenuItem(MENU_PATH)]
        private static void ShowWindow()
        {
            ShowDatabase(null);
        }

        public static void ShowDatabase(ProductVisualDatabase database)
        {
            ProductVisualDatabaseSetsManager window =
                GetWindow<ProductVisualDatabaseSetsManager>(WINDOW_TITLE);

            window.minSize = new Vector2(1000.0f, 350.0f);

            if (database)
            {
                window.SetDatabase(database);
            }

            window.Show();
        }

        public void SetDatabase(ProductVisualDatabase database)
        {
            m_Database = database;
            m_DatabaseField?.SetValueWithoutNotify(database);

            Refresh();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Refresh;

            m_SetAsideGuids.Clear();
            m_SetAsideGuids.UnionWith(
                SessionState.GetString(SET_ASIDE_SESSION_KEY, "")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            );
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Refresh;
        }

        private void OnDestroy()
        {
            SessionState.EraseString(SET_ASIDE_SESSION_KEY);
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void CreateGUI()
        {
            if (!m_Database)
            {
                m_Database = ProductVisualDatabaseEntryFile.FindDefaultDatabase();
            }

            VisualElement root = rootVisualElement;

            m_DatabaseField = new ObjectField("Database");
            m_DatabaseField.objectType = typeof(ProductVisualDatabase);
            m_DatabaseField.allowSceneObjects = false;
            m_DatabaseField.SetValueWithoutNotify(m_Database);
            m_DatabaseField.RegisterValueChangedCallback(OnDatabaseChanged);
            m_DatabaseField.style.marginBottom = 4.0f;
            root.Add(m_DatabaseField);

            TwoPaneSplitView splitView =
                new TwoPaneSplitView(1, 480.0f, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            VisualElement entriesPane = new VisualElement();
            splitView.Add(entriesPane);

            m_SearchField = new ToolbarSearchField();
            m_SearchField.style.width = StyleKeyword.Auto;
            m_SearchField.style.flexShrink = 1.0f;
            m_SearchField.tooltip = "Matches the visual's name or ID. Every word must match. Start a word with a hyphen to exclude it. * matches any characters and ? one character, for example CabinetSection_*_BerlinGardens.";
            m_SearchField.RegisterValueChangedCallback(OnSearchChanged);
            entriesPane.Add(m_SearchField);

            m_EntriesLabel = MakeLabel();
            entriesPane.Add(m_EntriesLabel);

            m_EntriesView = new MultiColumnListView();
            m_EntriesView.itemsSource = m_DisplayedRows;
            m_EntriesView.selectionType = SelectionType.Multiple;
            m_EntriesView.fixedItemHeight = 20.0f;
            m_EntriesView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            m_EntriesView.sortingEnabled = true;
            m_EntriesView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            m_EntriesView.columns.Add(MakeColumn(COLUMN_POSITION, "#", 60.0f, false, index => m_DisplayedRows[index].position.ToString()));
            m_EntriesView.columns.Add(MakeColumn(COLUMN_NAME, "Product Visual", 320.0f, true, index => m_DisplayedRows[index].name));
            m_EntriesView.columns.Add(MakeColumn(COLUMN_ID, "ID", 160.0f, true, index => m_DisplayedRows[index].id));
            m_EntriesView.columnSortingChanged += RebuildDisplayedRows;
            m_EntriesView.selectionChanged += OnEntrySelectionChanged;
            m_EntriesView.itemsChosen += OnEntriesChosen;
            m_EntriesView.style.flexGrow = 1.0f;
            entriesPane.Add(m_EntriesView);

            VisualElement selectionButtonRow = MakeButtonRow();
            entriesPane.Add(selectionButtonRow);

            m_SelectAllShownButton = MakeButton("Select All Shown", "Select every entry the search shows.", SelectAllShown);
            selectionButtonRow.Add(m_SelectAllShownButton);

            m_SetAsideButton = MakeButton("Set Aside Selected", "Add the selected entries to the set-aside list, so they stay picked when the search changes.", SetAsideSelected);
            selectionButtonRow.Add(m_SetAsideButton);

            m_SetAsideLabel = MakeLabel();
            entriesPane.Add(m_SetAsideLabel);

            m_SetAsideView = new MultiColumnListView();
            m_SetAsideView.itemsSource = m_SetAsideRows;
            m_SetAsideView.selectionType = SelectionType.Multiple;
            m_SetAsideView.fixedItemHeight = 20.0f;
            m_SetAsideView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            m_SetAsideView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            m_SetAsideView.columns.Add(MakeColumn(COLUMN_POSITION, "#", 60.0f, false, index => m_SetAsideRows[index].position.ToString()));
            m_SetAsideView.columns.Add(MakeColumn(COLUMN_NAME, "Product Visual", 320.0f, true, index => m_SetAsideRows[index].name));
            m_SetAsideView.selectionChanged += OnSetAsideSelectionChanged;
            m_SetAsideView.itemsChosen += OnSetAsideChosen;
            m_SetAsideView.style.height = 110.0f;
            m_SetAsideView.style.flexShrink = 0.0f;
            entriesPane.Add(m_SetAsideView);

            VisualElement setAsideButtonRow = MakeButtonRow();
            entriesPane.Add(setAsideButtonRow);

            m_TakeOutButton = MakeButton("Take Out", "Remove the entries selected in the set-aside list from it. The database is not changed.", TakeOutOfSetAside);
            setAsideButtonRow.Add(m_TakeOutButton);

            m_ClearSetAsideButton = MakeButton("Clear", "Empty the set-aside list. The database is not changed.", ClearSetAside);
            setAsideButtonRow.Add(m_ClearSetAsideButton);

            m_NewSetNameField = new TextField("New Set Name");
            m_NewSetNameField.style.marginTop = 4.0f;
            entriesPane.Add(m_NewSetNameField);

            m_ActionTargetLabel = MakeLabel();
            entriesPane.Add(m_ActionTargetLabel);

            VisualElement entryButtonRow = MakeButtonRow();
            entriesPane.Add(entryButtonRow);

            m_SaveSetButton = MakeButton("Save Selected As Set", "Save the set-aside and selected entries as a new set. The database is not changed.", SaveSelectedAsSet);
            entryButtonRow.Add(m_SaveSetButton);

            m_RemoveButton = MakeButton("Remove Selected", "Remove the set-aside and selected entries from the database. Their files stay in the project.", RemoveSelected);
            entryButtonRow.Add(m_RemoveButton);

            m_SaveSetAndRemoveButton = MakeButton("Save Selected As Set And Remove", "Save the set-aside and selected entries as a new set, then remove them from the database.", SaveSelectedAsSetAndRemove);
            entryButtonRow.Add(m_SaveSetAndRemoveButton);

            VisualElement setsPane = new VisualElement();
            splitView.Add(setsPane);

            m_SetsView = new MultiColumnListView();
            m_SetsView.itemsSource = m_Sets;
            m_SetsView.selectionType = SelectionType.Single;
            m_SetsView.fixedItemHeight = 20.0f;
            m_SetsView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            m_SetsView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            m_SetsView.columns.Add(MakeColumn("Set", "Set", 180.0f, true, index => m_Sets[index].name));
            m_SetsView.columns.Add(MakeColumn("Entries", "Entries", 60.0f, false, index => m_Sets[index].entries.Count.ToString("N0")));
            m_SetsView.columns.Add(MakeColumn("InDatabase", "In Database", 90.0f, false, index => m_Sets[index].inDatabaseCount.ToString("N0")));
            m_SetsView.selectionChanged += OnSetSelectionChanged;
            m_SetsView.style.height = 130.0f;
            m_SetsView.style.flexShrink = 0.0f;
            setsPane.Add(m_SetsView);

            m_SetSummaryLabel = MakeLabel();
            m_SetSummaryLabel.style.whiteSpace = WhiteSpace.Normal;
            setsPane.Add(m_SetSummaryLabel);

            m_SetEntriesView = new MultiColumnListView();
            m_SetEntriesView.itemsSource = m_SetEntryRows;
            m_SetEntriesView.selectionType = SelectionType.None;
            m_SetEntriesView.fixedItemHeight = 20.0f;
            m_SetEntriesView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            m_SetEntriesView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            m_SetEntriesView.columns.Add(MakeColumn("Status", "Status", 100.0f, false, index => m_SetEntryRows[index].status));
            m_SetEntriesView.columns.Add(MakeColumn("Name", "Product Visual", 300.0f, true, index => m_SetEntryRows[index].name));
            m_SetEntriesView.style.flexGrow = 1.0f;
            setsPane.Add(m_SetEntriesView);

            VisualElement setButtonRow = MakeButtonRow();
            setsPane.Add(setButtonRow);

            m_AppendButton = MakeButton("Append To Database", "Add the set's entries that are not in the database yet to the end of the database.", AppendSelectedSet);
            setButtonRow.Add(m_AppendButton);

            m_RemoveSetEntriesButton = MakeButton("Remove From Database", "Remove the set's entries from the database. The set and the files stay.", RemoveSelectedSetFromDatabase);
            setButtonRow.Add(m_RemoveSetEntriesButton);

            VisualElement setButtonSpacer = new VisualElement();
            setButtonSpacer.style.flexGrow = 1.0f;
            setButtonRow.Add(setButtonSpacer);

            m_RevealSetButton = MakeButton("Show File", null, RevealSelectedSet);
            setButtonRow.Add(m_RevealSetButton);

            m_DeleteSetButton = MakeButton("Delete Set", "Delete the set's file. The database is not changed.", DeleteSelectedSet);
            setButtonRow.Add(m_DeleteSetButton);

            Refresh();
        }

        private static Label MakeLabel()
        {
            Label label = new Label();
            label.style.marginLeft = 4.0f;
            label.style.marginTop = 4.0f;
            label.style.marginBottom = 4.0f;

            return label;
        }

        private static VisualElement MakeButtonRow()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.flexShrink = 0.0f;
            row.style.marginTop = 4.0f;
            row.style.marginBottom = 4.0f;

            return row;
        }

        private static Button MakeButton(string text, string tooltip, Action onClicked)
        {
            Button button = new Button(onClicked);
            button.text = text;
            button.tooltip = tooltip;

            return button;
        }

        private static Column MakeColumn(string name, string title, float width, bool stretchable, Func<int, string> getText)
        {
            Column column = new Column();
            column.name = name;
            column.title = title;
            column.width = width;
            column.stretchable = stretchable;
            column.makeCell = static () =>
            {
                Label label = new Label();
                label.style.flexGrow = 1.0f;
                label.style.paddingLeft = 4.0f;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                return label;
            };
            column.bindCell = (element, index) => ((Label)element).text = getText(index);

            return column;
        }

        private void OnDatabaseChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            m_Database = changeEvent.newValue as ProductVisualDatabase;

            Refresh();
        }

        private void OnSearchChanged(ChangeEvent<string> changeEvent)
        {
            RebuildDisplayedRows();
        }

        private void OnEntrySelectionChanged(IEnumerable<object> selection)
        {
            RefreshEntryControls();
        }

        private void OnEntriesChosen(IEnumerable<object> chosen)
        {
            List<Row> rows = GetSelectedRows();

            if (rows.Count > 0)
            {
                EditorGUIUtility.PingObject(ProductVisualDatabaseEntryFile.LoadVisual(rows[0].guid));
            }
        }

        private void OnSetAsideSelectionChanged(IEnumerable<object> selection)
        {
            m_TakeOutButton.SetEnabled(m_SetAsideView.selectedIndices.Any());
        }

        private void OnSetAsideChosen(IEnumerable<object> chosen)
        {
            foreach (object item in chosen)
            {
                EditorGUIUtility.PingObject(ProductVisualDatabaseEntryFile.LoadVisual(((Row)item).guid));
                return;
            }
        }

        private void OnSetSelectionChanged(IEnumerable<object> selection)
        {
            RefreshSetDetail();
        }

        private void Refresh()
        {
            if (m_EntriesView == null)
            {
                return;
            }

            string selectedSetName = GetSelectedSet()?.name;

            ReadRows();
            ReadSets();

            RebuildSetAsideRows();
            RebuildDisplayedRows();

            m_SetsView.RefreshItems();

            int selectedSetIndex = m_Sets.FindIndex(set => set.name == selectedSetName);
            m_SetsView.SetSelectionWithoutNotify(
                selectedSetIndex >= 0 ? new[] { selectedSetIndex } : Array.Empty<int>()
            );

            RefreshSetDetail();
        }

        private void ReadRows()
        {
            m_Rows.Clear();
            m_CurrentGuids.Clear();
            m_GuidIsInProject.Clear();
            m_NullEntryCount = 0;

            if (!m_Database)
            {
                return;
            }

            IReadOnlyList<ProductVisual> visuals = m_Database.sourceEntries;

            for (int i = 0; i < visuals.Count; i++)
            {
                ProductVisual visual = visuals[i];

                if (!ProductVisualDatabaseEntryFile.TryGetEntry(visual, out Entry entry))
                {
                    ++m_NullEntryCount;
                    continue;
                }

                m_Rows.Add(
                    new Row
                    {
                        position = i,
                        guid = entry.guid,
                        name = entry.name,
                        id = visual.info ? visual.info.id ?? "" : ""
                    }
                );

                m_CurrentGuids.Add(entry.guid);
            }
        }

        private void ReadSets()
        {
            m_Sets.Clear();

            foreach (string filePath in ProductVisualDatabaseEntryFile.GetFilePaths(SETS_FOLDER_NAME))
            {
                Set set = new Set();
                set.name = Path.GetFileNameWithoutExtension(filePath);
                set.filePath = filePath;
                set.entries.AddRange(ProductVisualDatabaseEntryFile.Read(filePath));

                foreach (Entry entry in set.entries)
                {
                    string status = GetStatus(entry.guid);

                    if (status == STATUS_IN_DATABASE)
                    {
                        ++set.inDatabaseCount;
                    }
                    else if (status == STATUS_TO_APPEND)
                    {
                        ++set.toAppendCount;
                    }
                    else
                    {
                        ++set.notInProjectCount;
                    }
                }

                m_Sets.Add(set);
            }
        }

        private string GetStatus(string guid)
        {
            if (m_CurrentGuids.Contains(guid))
            {
                return STATUS_IN_DATABASE;
            }

            if (!m_GuidIsInProject.TryGetValue(guid, out bool isInProject))
            {
                isInProject = ProductVisualDatabaseEntryFile.IsInProject(guid);
                m_GuidIsInProject[guid] = isInProject;
            }

            return isInProject ? STATUS_TO_APPEND : STATUS_NOT_IN_PROJECT;
        }

        private void RebuildDisplayedRows()
        {
            HashSet<string> selectedGuids = new (GetSelectedRows().Select(static row => row.guid));

            SearchTerm[] searchTerms =
                Regex.Split(m_SearchField.value ?? "", @"\s+")
                    .Where(static word => !string.IsNullOrWhiteSpace(word))
                    .Select(SearchTerm.Parse)
                    .ToArray();

            m_DisplayedRows.Clear();

            foreach (Row row in m_Rows)
            {
                if (MatchesSearch(row, searchTerms))
                {
                    m_DisplayedRows.Add(row);
                }
            }

            SortColumnDescription sorting = m_EntriesView.sortedColumns.FirstOrDefault();

            if (sorting != null)
            {
                int sign = sorting.direction == SortDirection.Descending ? -1 : 1;

                m_DisplayedRows.Sort(
                    (a, b) =>
                    {
                        int order = 0;

                        if (sorting.columnName == COLUMN_NAME)
                        {
                            order = EditorUtility.NaturalCompare(a.name, b.name);
                        }
                        else if (sorting.columnName == COLUMN_ID)
                        {
                            order = EditorUtility.NaturalCompare(a.id, b.id);
                        }

                        return sign * (order != 0 ? order : a.position.CompareTo(b.position));
                    }
                );
            }

            m_EntriesView.RefreshItems();

            List<int> selectedIndices = new ();

            for (int i = 0; i < m_DisplayedRows.Count; i++)
            {
                if (selectedGuids.Contains(m_DisplayedRows[i].guid))
                {
                    selectedIndices.Add(i);
                }
            }

            m_EntriesView.SetSelectionWithoutNotify(selectedIndices);

            RefreshEntryControls();
        }

        private static bool MatchesSearch(Row row, SearchTerm[] searchTerms)
        {
            foreach (SearchTerm searchTerm in searchTerms)
            {
                if (searchTerm.Matches(row.name) || searchTerm.Matches(row.id))
                {
                    if (searchTerm.isExclusion)
                    {
                        return false;
                    }
                }
                else if (!searchTerm.isExclusion)
                {
                    return false;
                }
            }

            return true;
        }

        private readonly struct SearchTerm
        {
            public readonly bool isExclusion;
            private readonly string m_Word;
            private readonly Regex m_Pattern;

            private SearchTerm(bool isExclusion, string word, Regex pattern)
            {
                this.isExclusion = isExclusion;
                m_Word = word;
                m_Pattern = pattern;
            }

            public static SearchTerm Parse(string searchWord)
            {
                bool isExclusion = searchWord.Length > 1 && searchWord[0] == '-';
                string word = isExclusion ? searchWord.Substring(1) : searchWord;

                if (word.IndexOfAny(new[] { '*', '?' }) < 0)
                {
                    return new SearchTerm(isExclusion, word, null);
                }

                string pattern = Regex.Escape(word).Replace(@"\*", ".*").Replace(@"\?", ".");

                return new SearchTerm(
                    isExclusion,
                    word,
                    new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                );
            }

            public bool Matches(string text)
            {
                return m_Pattern != null
                    ? m_Pattern.IsMatch(text)
                    : text.Contains(m_Word, StringComparison.OrdinalIgnoreCase);
            }
        }

        private List<Row> GetSelectedRows()
        {
            List<Row> rows = new ();

            foreach (int index in m_EntriesView.selectedIndices)
            {
                if (index >= 0 && index < m_DisplayedRows.Count)
                {
                    rows.Add(m_DisplayedRows[index]);
                }
            }

            rows.Sort(static (a, b) => a.position.CompareTo(b.position));

            return rows;
        }

        private List<Row> GetActionRows()
        {
            List<Row> rows = new (m_SetAsideRows);

            foreach (Row row in GetSelectedRows())
            {
                if (!m_SetAsideGuids.Contains(row.guid))
                {
                    rows.Add(row);
                }
            }

            rows.Sort(static (a, b) => a.position.CompareTo(b.position));

            return rows;
        }

        private void RebuildSetAsideRows()
        {
            if (m_Database)
            {
                m_SetAsideGuids.IntersectWith(m_CurrentGuids);
                SaveSetAsideGuids();
            }

            m_SetAsideRows.Clear();
            m_SetAsideRows.AddRange(m_Rows.Where(row => m_SetAsideGuids.Contains(row.guid)));

            m_SetAsideView.ClearSelection();
            m_SetAsideView.RefreshItems();
        }

        private void SaveSetAsideGuids()
        {
            SessionState.SetString(SET_ASIDE_SESSION_KEY, string.Join("\n", m_SetAsideGuids));
        }

        private void SetAsideSelected()
        {
            m_SetAsideGuids.UnionWith(GetSelectedRows().Select(static row => row.guid));
            SaveSetAsideGuids();

            RebuildSetAsideRows();
            RefreshEntryControls();
        }

        private void TakeOutOfSetAside()
        {
            foreach (int index in m_SetAsideView.selectedIndices)
            {
                if (index >= 0 && index < m_SetAsideRows.Count)
                {
                    m_SetAsideGuids.Remove(m_SetAsideRows[index].guid);
                }
            }

            SaveSetAsideGuids();

            RebuildSetAsideRows();
            RefreshEntryControls();
        }

        private void ClearSetAside()
        {
            m_SetAsideGuids.Clear();
            SaveSetAsideGuids();

            RebuildSetAsideRows();
            RefreshEntryControls();
        }

        private Set GetSelectedSet()
        {
            int index = m_SetsView.selectedIndex;

            return index >= 0 && index < m_Sets.Count ? m_Sets[index] : null;
        }

        private void RefreshEntryControls()
        {
            int selectedCount = GetSelectedRows().Count;
            int actionCount = GetActionRows().Count;
            int setAsideCount = m_SetAsideRows.Count;
            int selectedOutsideSetAsideCount = actionCount - setAsideCount;

            if (!m_Database)
            {
                m_EntriesLabel.text = "No database selected.";
            }
            else
            {
                string description = $"Showing {m_DisplayedRows.Count:N0} of {m_Rows.Count:N0} entries. {selectedCount:N0} selected.";

                if (m_NullEntryCount > 0)
                {
                    description += $" The database also holds {m_NullEntryCount:N0} null entries.";
                }

                m_EntriesLabel.text = description;
            }

            m_SetAsideLabel.text = $"Set aside: {setAsideCount:N0} entries.";

            m_ActionTargetLabel.text =
                actionCount > 0 ?
                    $"Acts on {actionCount:N0}: {setAsideCount:N0} set aside, {selectedOutsideSetAsideCount:N0} selected." :
                    "Select entries or set some aside to save or remove them.";

            m_SelectAllShownButton.SetEnabled(m_DisplayedRows.Count > 0);
            m_SetAsideButton.SetEnabled(selectedCount > 0);
            m_TakeOutButton.SetEnabled(m_SetAsideView.selectedIndices.Any());
            m_ClearSetAsideButton.SetEnabled(setAsideCount > 0);
            m_SaveSetButton.SetEnabled(actionCount > 0);
            m_RemoveButton.SetEnabled(actionCount > 0);
            m_SaveSetAndRemoveButton.SetEnabled(actionCount > 0);
        }

        private void RefreshSetDetail()
        {
            Set set = GetSelectedSet();

            m_SetEntryRows.Clear();

            if (set != null)
            {
                foreach (Entry entry in set.entries)
                {
                    m_SetEntryRows.Add(new SetEntryRow { status = GetStatus(entry.guid), name = entry.name });
                }

                m_SetEntryRows.Sort(
                    static (a, b) =>
                    {
                        int order = string.CompareOrdinal(b.status, a.status);
                        return order != 0 ? order : EditorUtility.NaturalCompare(a.name, b.name);
                    }
                );

                m_SetSummaryLabel.text =
                    $"\"{set.name}\" holds {set.entries.Count:N0} entries. " +
                    $"To append: {set.toAppendCount:N0}. " +
                    $"Already in the database: {set.inDatabaseCount:N0}. " +
                    $"Not in this project: {set.notInProjectCount:N0}.";
            }
            else
            {
                m_SetSummaryLabel.text =
                    m_Sets.Count > 0 ?
                        "Select a set to see what appending it would add." :
                        "No sets saved yet. Select entries on the left and save them as a set.";
            }

            m_SetEntriesView.RefreshItems();

            m_AppendButton.SetEnabled(set != null && m_Database && set.toAppendCount > 0);
            m_RemoveSetEntriesButton.SetEnabled(set != null && m_Database && set.inDatabaseCount > 0);
            m_RevealSetButton.SetEnabled(set != null);
            m_DeleteSetButton.SetEnabled(set != null);
        }

        private void SelectAllShown()
        {
            m_EntriesView.SetSelection(Enumerable.Range(0, m_DisplayedRows.Count));
        }

        private bool TryGetNewSetFilePath(out string setName, out string filePath)
        {
            setName = m_NewSetNameField.value.Trim();
            filePath = null;

            if (!ProductVisualDatabaseEntryFile.IsValidFileName(setName))
            {
                EditorUtility.DisplayDialog(
                    title: "Save Set",
                    message: "Enter a set name that can be used as a file name.",
                    ok: "OK"
                );

                return false;
            }

            filePath =
                Path.Combine(
                    ProductVisualDatabaseEntryFile.GetFolder(SETS_FOLDER_NAME),
                    setName + ProductVisualDatabaseEntryFile.FILE_EXTENSION
                );

            if (!File.Exists(filePath))
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                title: "Save Set",
                message: $"A set named \"{setName}\" already exists. Overwrite it with the selected entries?",
                ok: "Overwrite",
                cancel: "Cancel"
            );
        }

        private void WriteSet(string filePath, List<Row> rows)
        {
            ProductVisualDatabaseEntryFile.Write(
                filePath,
                rows.Select(static row => new Entry { guid = row.guid, name = row.name })
            );

            m_NewSetNameField.SetValueWithoutNotify("");
        }

        private void SaveSelectedAsSet()
        {
            List<Row> rows = GetActionRows();

            if (rows.Count <= 0 || !TryGetNewSetFilePath(out string setName, out string filePath))
            {
                return;
            }

            WriteSet(filePath, rows);

            Refresh();
            SelectSet(setName);
        }

        private void RemoveSelected()
        {
            List<Row> rows = GetActionRows();

            if (rows.Count <= 0 || !m_Database)
            {
                return;
            }

            bool willRemove =
                EditorUtility.DisplayDialog(
                    title: "Remove Selected Entries",
                    message: $"Remove {rows.Count:N0} entries from {m_Database.name}?\n" +
                        "\n" +
                        "Their files stay in the project. The entries are not saved as a set.",
                    ok: "Remove",
                    cancel: "Cancel"
                );

            if (!willRemove)
            {
                return;
            }

            RemoveFromDatabase(rows.Select(static row => row.guid));
        }

        private void SaveSelectedAsSetAndRemove()
        {
            List<Row> rows = GetActionRows();

            if (rows.Count <= 0 || !m_Database || !TryGetNewSetFilePath(out string setName, out string filePath))
            {
                return;
            }

            bool willRemove =
                EditorUtility.DisplayDialog(
                    title: "Save As Set And Remove",
                    message: $"Save {rows.Count:N0} entries as the set \"{setName}\" and remove them from {m_Database.name}?\n" +
                        "\n" +
                        "Their files stay in the project.",
                    ok: "Save And Remove",
                    cancel: "Cancel"
                );

            if (!willRemove)
            {
                return;
            }

            WriteSet(filePath, rows);

            RemoveFromDatabase(rows.Select(static row => row.guid));
            SelectSet(setName);
        }

        private void RemoveFromDatabase(IEnumerable<string> guids)
        {
            HashSet<string> guidsToRemove = new (guids);

            Undo.RecordObject(m_Database, "Remove Product Visual Database Entries");

            int removedCount =
                m_Database.mutableSourceEntries.RemoveAll(
                    visual =>
                        ProductVisualDatabaseEntryFile.TryGetEntry(visual, out Entry entry) &&
                        guidsToRemove.Contains(entry.guid)
                );

            EditorUtility.SetDirty(m_Database);
            AssetDatabase.SaveAssetIfDirty(m_Database);

            Debug.Log($"Removed {removedCount} entries from {m_Database.name}.", m_Database);

            ProductVisualDatabaseEntryFile.RefreshOpenDatabaseManagers(m_Database);

            Refresh();
        }

        private void AppendSelectedSet()
        {
            Set set = GetSelectedSet();

            if (set == null || !m_Database)
            {
                return;
            }

            HashSet<string> seenGuids = new (m_CurrentGuids);
            List<ProductVisual> visualsToAppend = new ();
            List<string> notInProjectNames = new ();
            int alreadyInDatabaseCount = 0;

            foreach (Entry entry in set.entries)
            {
                if (!seenGuids.Add(entry.guid))
                {
                    ++alreadyInDatabaseCount;
                    continue;
                }

                ProductVisual visual = ProductVisualDatabaseEntryFile.LoadVisual(entry.guid);

                if (visual)
                {
                    visualsToAppend.Add(visual);
                }
                else
                {
                    notInProjectNames.Add(entry.name);
                }
            }

            Undo.RecordObject(m_Database, "Append Product Visual Database Set");
            m_Database.mutableSourceEntries.AddRange(visualsToAppend);
            EditorUtility.SetDirty(m_Database);
            AssetDatabase.SaveAssetIfDirty(m_Database);

            Debug.Log(
                $"Appended {visualsToAppend.Count} entries of the set \"{set.name}\" to {m_Database.name}. " +
                    $"Skipped {alreadyInDatabaseCount} that were already in the database.",
                m_Database
            );

            if (notInProjectNames.Count > 0)
            {
                Debug.LogWarning(
                    $"Skipped {notInProjectNames.Count} entries of the set \"{set.name}\" that are not in this project:\n" +
                        string.Join("\n", notInProjectNames),
                    m_Database
                );
            }

            ProductVisualDatabaseEntryFile.RefreshOpenDatabaseManagers(m_Database);

            Refresh();
        }

        private void RemoveSelectedSetFromDatabase()
        {
            Set set = GetSelectedSet();

            if (set == null || !m_Database)
            {
                return;
            }

            bool willRemove =
                EditorUtility.DisplayDialog(
                    title: "Remove Set From Database",
                    message: $"Remove the {set.inDatabaseCount:N0} entries of \"{set.name}\" that are in {m_Database.name}?\n" +
                        "\n" +
                        "The set and the files stay.",
                    ok: "Remove",
                    cancel: "Cancel"
                );

            if (!willRemove)
            {
                return;
            }

            RemoveFromDatabase(set.entries.Select(static entry => entry.guid));
        }

        private void RevealSelectedSet()
        {
            Set set = GetSelectedSet();

            if (set != null)
            {
                EditorUtility.RevealInFinder(set.filePath);
            }
        }

        private void DeleteSelectedSet()
        {
            Set set = GetSelectedSet();

            if (set == null)
            {
                return;
            }

            bool willDelete =
                EditorUtility.DisplayDialog(
                    title: "Delete Set",
                    message: $"Delete the set \"{set.name}\"? This deletes its file and does not change the database.",
                    ok: "Delete",
                    cancel: "Cancel"
                );

            if (!willDelete)
            {
                return;
            }

            File.Delete(set.filePath);

            Refresh();
        }

        private void SelectSet(string setName)
        {
            int index = m_Sets.FindIndex(set => set.name == setName);

            if (index >= 0)
            {
                m_SetsView.SetSelection(index);
                m_SetsView.ScrollToItem(index);
            }
        }

        [SerializeField] private ProductVisualDatabase m_Database = default;

        private readonly List<Row> m_Rows = new ();
        private readonly List<Row> m_DisplayedRows = new ();
        private readonly HashSet<string> m_SetAsideGuids = new ();
        private readonly List<Row> m_SetAsideRows = new ();
        private readonly HashSet<string> m_CurrentGuids = new ();
        private readonly Dictionary<string, bool> m_GuidIsInProject = new ();
        private readonly List<Set> m_Sets = new ();
        private readonly List<SetEntryRow> m_SetEntryRows = new ();
        private int m_NullEntryCount;

        private ObjectField m_DatabaseField;
        private ToolbarSearchField m_SearchField;
        private Label m_EntriesLabel;
        private MultiColumnListView m_EntriesView;
        private TextField m_NewSetNameField;
        private Button m_SelectAllShownButton;
        private Button m_SetAsideButton;
        private Label m_SetAsideLabel;
        private MultiColumnListView m_SetAsideView;
        private Button m_TakeOutButton;
        private Button m_ClearSetAsideButton;
        private Label m_ActionTargetLabel;
        private Button m_SaveSetButton;
        private Button m_RemoveButton;
        private Button m_SaveSetAndRemoveButton;
        private MultiColumnListView m_SetsView;
        private Label m_SetSummaryLabel;
        private MultiColumnListView m_SetEntriesView;
        private Button m_AppendButton;
        private Button m_RemoveSetEntriesButton;
        private Button m_RevealSetButton;
        private Button m_DeleteSetButton;

        private struct Row
        {
            public int position;
            public string guid;
            public string name;
            public string id;
        }

        private struct SetEntryRow
        {
            public string status;
            public string name;
        }

        private class Set
        {
            public string name;
            public string filePath;
            public readonly List<Entry> entries = new ();
            public int toAppendCount;
            public int inDatabaseCount;
            public int notInProjectCount;
        }
    }
}
