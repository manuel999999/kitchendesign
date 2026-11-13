namespace KitchenDesign.ProductsEditor
{
    using Melcher;
    using KitchenDesign.Scriptables;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Entry = ProductVisualDatabaseEntryFile.Entry;

    public class ProductVisualDatabaseStatesManager : EditorWindow
    {
        private const string WINDOW_TITLE = "Product Visual Database States";
        private const string MENU_PATH = "Tools/Melcher Studios/Pro-Fit/Product Visual Database States";
        private const string STATES_FOLDER_NAME = "ProductVisualDatabaseStates";

        private const string CHANGE_ADDED = "Added";
        private const string CHANGE_REMOVED = "Removed";
        private const string CHANGE_NOT_IN_PROJECT = "Not in project";

        [MenuItem(MENU_PATH)]
        private static void ShowWindow()
        {
            ShowDatabase(null);
        }

        public static void ShowDatabase(ProductVisualDatabaseSO database)
        {
            ProductVisualDatabaseStatesManager window =
                GetWindow<ProductVisualDatabaseStatesManager>(WINDOW_TITLE);

            window.minSize = new Vector2(700.0f, 300.0f);

            if (database)
            {
                window.SetDatabase(database);
            }

            window.Show();
        }

        public void SetDatabase(ProductVisualDatabaseSO database)
        {
            m_Database = database;
            m_DatabaseField?.SetValueWithoutNotify(database);

            Refresh();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Refresh;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Refresh;
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
            m_DatabaseField.objectType = typeof(ProductVisualDatabaseSO);
            m_DatabaseField.allowSceneObjects = false;
            m_DatabaseField.SetValueWithoutNotify(m_Database);
            m_DatabaseField.RegisterValueChangedCallback(OnDatabaseChanged);
            root.Add(m_DatabaseField);

            _currentLabel = new Label();
            _currentLabel.style.marginLeft = 4.0f;
            _currentLabel.style.marginTop = 2.0f;
            _currentLabel.style.marginBottom = 4.0f;
            root.Add(_currentLabel);

            VisualElement saveRow = new VisualElement();
            saveRow.style.flexDirection = FlexDirection.Row;
            saveRow.style.marginBottom = 4.0f;
            root.Add(saveRow);

            _newStateNameField = new TextField("New State Name");
            _newStateNameField.style.flexGrow = 1.0f;
            saveRow.Add(_newStateNameField);

            _saveNewButton = new Button(SaveCurrentAsNewState);
            _saveNewButton.text = "Save Current Entries";
            _saveNewButton.tooltip = "Save the database's current entries as a new state.";
            saveRow.Add(_saveNewButton);

            TwoPaneSplitView splitView =
                new TwoPaneSplitView(0, 440.0f, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            _statesView = new MultiColumnListView();
            _statesView.itemsSource = _states;
            _statesView.selectionType = SelectionType.Single;
            _statesView.fixedItemHeight = 20.0f;
            _statesView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            _statesView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            _statesView.columns.Add(MakeColumn("State", 150.0f, true, index => _states[index].name));
            _statesView.columns.Add(MakeColumn("Entries", 60.0f, false, index => _states[index].entries.Count.ToString("N0")));
            _statesView.columns.Add(MakeColumn("Saved", 115.0f, false, index => _states[index].savedAt.ToString("yyyy-MM-dd HH:mm")));
            _statesView.columns.Add(MakeColumn("Loading It", 115.0f, false, index => DescribeLoad(_states[index])));
            _statesView.selectionChanged += OnStateSelectionChanged;
            splitView.Add(_statesView);

            VisualElement detailPane = new VisualElement();
            splitView.Add(detailPane);

            _summaryLabel = new Label();
            _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            _summaryLabel.style.marginLeft = 4.0f;
            _summaryLabel.style.marginTop = 4.0f;
            _summaryLabel.style.marginBottom = 4.0f;
            detailPane.Add(_summaryLabel);

            _differenceSearchField = new ToolbarSearchField();
            _differenceSearchField.style.width = StyleKeyword.Auto;
            _differenceSearchField.style.flexShrink = 1.0f;
            _differenceSearchField.RegisterValueChangedCallback(OnDifferenceSearchChanged);
            detailPane.Add(_differenceSearchField);

            _differencesView = new MultiColumnListView();
            _differencesView.itemsSource = _displayedDifferences;
            _differencesView.selectionType = SelectionType.None;
            _differencesView.fixedItemHeight = 20.0f;
            _differencesView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            _differencesView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            _differencesView.columns.Add(MakeColumn("When Loaded", 100.0f, false, index => _displayedDifferences[index].change));
            _differencesView.columns.Add(MakeColumn("Product Visual", 400.0f, true, index => _displayedDifferences[index].name));
            _differencesView.style.flexGrow = 1.0f;
            detailPane.Add(_differencesView);

            VisualElement buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.flexShrink = 0.0f;
            buttonRow.style.marginTop = 4.0f;
            buttonRow.style.marginBottom = 4.0f;
            detailPane.Add(buttonRow);

            _loadButton = new Button(LoadSelectedState);
            _loadButton.text = "Load";
            _loadButton.tooltip = "Replace the database's entries with the selected state's entries.";
            buttonRow.Add(_loadButton);

            _overwriteButton = new Button(OverwriteSelectedState);
            _overwriteButton.text = "Overwrite With Current";
            _overwriteButton.tooltip = "Save the database's current entries into the selected state.";
            buttonRow.Add(_overwriteButton);

            VisualElement buttonSpacer = new VisualElement();
            buttonSpacer.style.flexGrow = 1.0f;
            buttonRow.Add(buttonSpacer);

            _revealButton = new Button(RevealSelectedState);
            _revealButton.text = "Show File";
            buttonRow.Add(_revealButton);

            m_DeleteButton = new Button(DeleteSelectedState);
            m_DeleteButton.text = "Delete";
            buttonRow.Add(m_DeleteButton);

            Refresh();
        }

        private static Column MakeColumn(string title, float width, bool stretchable, Func<int, string> getText)
        {
            Column column = new Column();
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

        private static string DescribeLoad(State state)
        {
            if (state.matchesDatabase)
            {
                return "Same as database";
            }

            if (state.addedCount == 0 && state.removedCount == 0)
            {
                return "Reorders only";
            }

            return $"+{state.addedCount:N0}  -{state.removedCount:N0}";
        }

        private void OnDatabaseChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            m_Database = changeEvent.newValue as ProductVisualDatabaseSO;

            Refresh();
        }

        private void OnStateSelectionChanged(IEnumerable<object> selection)
        {
            RefreshDetail();
        }

        private void OnDifferenceSearchChanged(ChangeEvent<string> changeEvent)
        {
            RefreshDisplayedDifferences();
        }

        private State GetSelectedState()
        {
            int index = _statesView.selectedIndex;

            return index >= 0 && index < _states.Count ? _states[index] : null;
        }

        private void Refresh()
        {
            if (_statesView == null)
            {
                return;
            }

            string selectedName = GetSelectedState()?.name;

            ReadCurrentEntries();
            ReadStates();

            _statesView.RefreshItems();

            int selectedIndex = _states.FindIndex(state => state.name == selectedName);
            _statesView.SetSelectionWithoutNotify(
                selectedIndex >= 0 ? new[] { selectedIndex } : Array.Empty<int>()
            );

            _currentLabel.text = DescribeDatabase();
            _saveNewButton.SetEnabled(m_Database);

            RefreshDetail();
        }

        private string DescribeDatabase()
        {
            if (!m_Database)
            {
                return "No database selected.";
            }

            string description = $"The database holds {m_CurrentEntries.Count:N0} entries";

            if (_currentNullEntryCount > 0)
            {
                description += $" and {_currentNullEntryCount:N0} null entries";
            }

            State matchingState = _states.Find(static state => state.matchesDatabase);

            return matchingState != null ?
                $"{description}, the same as the state \"{matchingState.name}\"." :
                $"{description}. They are not saved as a state.";
        }

        private void ReadCurrentEntries()
        {
            m_CurrentEntries.Clear();
            m_CurrentGuids.Clear();
            m_GuidIsInProject.Clear();
            _currentNullEntryCount = 0;

            if (!m_Database)
            {
                return;
            }

            foreach (ProductVisualSO visual in m_Database.sourceEntries)
            {
                if (!ProductVisualDatabaseEntryFile.TryGetEntry(visual, out Entry entry))
                {
                    ++_currentNullEntryCount;
                    continue;
                }

                m_CurrentEntries.Add(entry);
                m_CurrentGuids.Add(entry.guid);
            }
        }

        private void ReadStates()
        {
            _states.Clear();

            foreach (string filePath in ProductVisualDatabaseEntryFile.GetFilePaths(STATES_FOLDER_NAME))
            {
                State state = new State();
                state.name = Path.GetFileNameWithoutExtension(filePath);
                state.filePath = filePath;
                state.savedAt = File.GetLastWriteTime(filePath);
                state.entries.AddRange(ProductVisualDatabaseEntryFile.Read(filePath));

                CompareWithDatabase(state);

                _states.Add(state);
            }
        }

        private void CompareWithDatabase(State state)
        {
            HashSet<string> stateGuids = new ();
            List<string> loadableGuids = new ();

            foreach (Entry entry in state.entries)
            {
                stateGuids.Add(entry.guid);

                if (m_CurrentGuids.Contains(entry.guid))
                {
                    loadableGuids.Add(entry.guid);
                }
                else if (IsInProject(entry.guid))
                {
                    loadableGuids.Add(entry.guid);
                    ++state.addedCount;
                }
                else
                {
                    ++state.notInProjectCount;
                }
            }

            state.removedCount = m_CurrentEntries.Count(entry => !stateGuids.Contains(entry.guid));

            state.matchesDatabase =
                m_Database &&
                state.addedCount == 0 &&
                state.removedCount == 0 &&
                loadableGuids.SequenceEqual(m_CurrentEntries.Select(static entry => entry.guid));
        }

        private bool IsInProject(string guid)
        {
            if (m_GuidIsInProject.TryGetValue(guid, out bool isInProject))
            {
                return isInProject;
            }

            isInProject = ProductVisualDatabaseEntryFile.IsInProject(guid);

            m_GuidIsInProject[guid] = isInProject;

            return isInProject;
        }

        private void RefreshDetail()
        {
            State state = GetSelectedState();

            _differences.Clear();

            if (state != null)
            {
                HashSet<string> stateGuids = new ();

                foreach (Entry entry in state.entries)
                {
                    stateGuids.Add(entry.guid);

                    if (m_CurrentGuids.Contains(entry.guid))
                    {
                        continue;
                    }

                    _differences.Add(
                        new Difference
                        {
                            change = IsInProject(entry.guid) ? CHANGE_ADDED : CHANGE_NOT_IN_PROJECT,
                            name = entry.name
                        }
                    );
                }

                foreach (Entry entry in m_CurrentEntries)
                {
                    if (!stateGuids.Contains(entry.guid))
                    {
                        _differences.Add(new Difference { change = CHANGE_REMOVED, name = entry.name });
                    }
                }

                _differences.Sort(
                    static (a, b) =>
                    {
                        int order = string.CompareOrdinal(a.change, b.change);
                        return order != 0 ? order : EditorUtility.NaturalCompare(a.name, b.name);
                    }
                );
            }

            _summaryLabel.text = DescribeSelection(state);

            _loadButton.SetEnabled(state != null && m_Database && !state.matchesDatabase);
            _overwriteButton.SetEnabled(state != null && m_Database && !state.matchesDatabase);
            _revealButton.SetEnabled(state != null);
            m_DeleteButton.SetEnabled(state != null);

            RefreshDisplayedDifferences();
        }

        private string DescribeSelection(State state)
        {
            if (state == null)
            {
                return _states.Count > 0 ?
                    "Select a state to see what loading it would change." :
                    "No states saved yet. Name one above and save the current entries.";
            }

            string description;

            if (state.matchesDatabase)
            {
                description = $"\"{state.name}\" holds the same entries as the database.";
            }
            else if (state.addedCount == 0 && state.removedCount == 0)
            {
                description = $"Loading \"{state.name}\" keeps the same entries and changes their order.";
            }
            else
            {
                description = $"Loading \"{state.name}\" adds {state.addedCount:N0} and removes {state.removedCount:N0} entries.";
            }

            if (state.notInProjectCount > 0)
            {
                description += state.notInProjectCount == 1 ?
                    " 1 of its entries is not in this project and is skipped." :
                    $" {state.notInProjectCount:N0} of its entries are not in this project and are skipped.";
            }

            return description;
        }

        private void RefreshDisplayedDifferences()
        {
            string search = _differenceSearchField.value;

            _displayedDifferences.Clear();

            foreach (Difference difference in _differences)
            {
                if (string.IsNullOrWhiteSpace(search) ||
                    difference.name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _displayedDifferences.Add(difference);
                }
            }

            _differencesView.RefreshItems();
        }

        private void SaveCurrentAsNewState()
        {
            if (!m_Database)
            {
                return;
            }

            string stateName = _newStateNameField.value.Trim();

            if (!ProductVisualDatabaseEntryFile.IsValidFileName(stateName))
            {
                EditorUtility.DisplayDialog(
                    title: "Save State",
                    message: "Enter a state name that can be used as a file name.",
                    ok: "OK"
                );

                return;
            }

            string filePath =
                Path.Combine(
                    ProductVisualDatabaseEntryFile.GetFolder(STATES_FOLDER_NAME),
                    stateName + ProductVisualDatabaseEntryFile.FILE_EXTENSION
                );

            if (File.Exists(filePath))
            {
                bool willOverwrite =
                    EditorUtility.DisplayDialog(
                        title: "Save State",
                        message: $"A state named \"{stateName}\" already exists. Overwrite it with the database's current entries?",
                        ok: "Overwrite",
                        cancel: "Cancel"
                    );

                if (!willOverwrite)
                {
                    return;
                }
            }

            WriteState(filePath);

            _newStateNameField.SetValueWithoutNotify("");

            Refresh();
            SelectState(stateName);
        }

        private void OverwriteSelectedState()
        {
            State state = GetSelectedState();

            if (state == null || !m_Database)
            {
                return;
            }

            bool willOverwrite =
                EditorUtility.DisplayDialog(
                    title: "Overwrite State",
                    message: $"Replace the {state.entries.Count:N0} entries saved in \"{state.name}\" with the database's current {m_CurrentEntries.Count:N0} entries?",
                    ok: "Overwrite",
                    cancel: "Cancel"
                );

            if (!willOverwrite)
            {
                return;
            }

            WriteState(state.filePath);

            Refresh();
        }

        private void WriteState(string filePath)
        {
            ReadCurrentEntries();

            ProductVisualDatabaseEntryFile.Write(filePath, m_CurrentEntries);
        }

        private void LoadSelectedState()
        {
            State state = GetSelectedState();

            if (state == null || !m_Database)
            {
                return;
            }

            if (!_states.Exists(static x => x.matchesDatabase))
            {
                bool willLoad =
                    EditorUtility.DisplayDialog(
                        title: "Load State",
                        message: $"The database's current {m_CurrentEntries.Count:N0} entries are not saved as a state.\n" +
                            "\n" +
                            $"Replace them with the entries of \"{state.name}\"?",
                        ok: "Load",
                        cancel: "Cancel"
                    );

                if (!willLoad)
                {
                    return;
                }
            }

            List<ProductVisualSO> visuals = new ();
            List<string> skippedNames = new ();

            foreach (Entry entry in state.entries)
            {
                ProductVisualSO visual = ProductVisualDatabaseEntryFile.LoadVisual(entry.guid);

                if (visual)
                {
                    visuals.Add(visual);
                }
                else
                {
                    skippedNames.Add(entry.name);
                }
            }

            Undo.RecordObject(m_Database, "Load Product Visual Database State");
            m_Database.mutableSourceEntries.Clear();
            m_Database.mutableSourceEntries.AddRange(visuals);
            EditorUtility.SetDirty(m_Database);
            AssetDatabase.SaveAssetIfDirty(m_Database);

            if (skippedNames.Count > 0)
            {
                Debug.LogWarning(
                    $"Loaded the state \"{state.name}\" without {skippedNames.Count} entries that are not in this project:\n" +
                        string.Join("\n", skippedNames),
                    m_Database
                );
            }

            ProductVisualDatabaseEntryFile.RefreshOpenDatabaseManagers(m_Database);

            Refresh();
        }

        private void RevealSelectedState()
        {
            State state = GetSelectedState();

            if (state != null)
            {
                EditorUtility.RevealInFinder(state.filePath);
            }
        }

        private void DeleteSelectedState()
        {
            State state = GetSelectedState();

            if (state == null)
            {
                return;
            }

            bool willDelete =
                EditorUtility.DisplayDialog(
                    title: "Delete State",
                    message: $"Delete the state \"{state.name}\"? This deletes its file and does not change the database.",
                    ok: "Delete",
                    cancel: "Cancel"
                );

            if (!willDelete)
            {
                return;
            }

            File.Delete(state.filePath);

            Refresh();
        }

        private void SelectState(string stateName)
        {
            int index = _states.FindIndex(state => state.name == stateName);

            if (index >= 0)
            {
                _statesView.SetSelection(index);
                _statesView.ScrollToItem(index);
            }
        }

        [SerializeField] private ProductVisualDatabaseSO m_Database = default;

        private readonly List<Entry> m_CurrentEntries = new ();
        private readonly HashSet<string> m_CurrentGuids = new ();
        private readonly Dictionary<string, bool> m_GuidIsInProject = new ();
        private readonly List<State> _states = new ();
        private readonly List<Difference> _differences = new ();
        private readonly List<Difference> _displayedDifferences = new ();
        private int _currentNullEntryCount;

        private ObjectField m_DatabaseField;
        private Label _currentLabel;
        private TextField _newStateNameField;
        private Button _saveNewButton;
        private MultiColumnListView _statesView;
        private Label _summaryLabel;
        private ToolbarSearchField _differenceSearchField;
        private MultiColumnListView _differencesView;
        private Button _loadButton;
        private Button _overwriteButton;
        private Button _revealButton;
        private Button m_DeleteButton;

        private struct Difference
        {
            public string change;
            public string name;
        }

        private class State
        {
            public string name;
            public string filePath;
            public DateTime savedAt;
            public readonly List<Entry> entries = new ();
            public int addedCount;
            public int removedCount;
            public int notInProjectCount;
            public bool matchesDatabase;
        }
    }
}
