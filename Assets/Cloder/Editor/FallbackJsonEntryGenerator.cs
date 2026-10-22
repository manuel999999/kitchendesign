namespace KitchenDesign
{
    using Melcher;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class FallbackJsonEntryGenerator : EditorWindow
    {
        private const string WINDOW_TITLE = "Fallback JSON Entry Generator";
        private const string MENU_PATH = "Tools/Melcher Studios/Pro-Fit/Fallback JSON Entry Generator";
        private const string RESOURCES_FOLDER = "Assets/AssetsMelcher/Resources";
        private const string FALLBACK_FOLDER_NAME = "FallbackJSON";

        private static readonly FileKind[] s_FileKinds =
        {
            new FileKind
            {
                label = "Components (products.json)",
                fileName = Server.FallbackJsonFiles.Products + ".json",
                listKey = "components",
                modelIdKey = "model_id",
                typeKey = "type",
                sizeKeys = new[] { "width", "height", "depth", "cutout_width", "cutout_height", "cutout_depth" },
                isAlwaysFromServer = true
            },
            new FileKind
            {
                label = "Cabinet sections (cabinets.json)",
                fileName = Server.FallbackJsonFiles.Cabinets + ".json",
                listKey = "cabinet_sections",
                modelIdKey = "cabinet_model_id",
                typeKey = "cabinet_section_type",
                sizeKeys = new[] { "width", "height", "depth", "minimum_width", "maximum_width" },
                isAlwaysFromServer = false
            }
        };

        [MenuItem(MENU_PATH)]
        private static void ShowWindow()
        {
            FallbackJsonEntryGenerator window = GetWindow<FallbackJsonEntryGenerator>(WINDOW_TITLE);
            window.minSize = new Vector2(900.0f, 450.0f);
            window.Show();
        }

        private void OnFocus()
        {
            if (m_EntriesView != null && File.GetLastWriteTimeUtc(GetFilePath()) != m_LoadedWriteTime)
            {
                LoadFile();
            }
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement fileRow = new VisualElement();
            fileRow.style.flexDirection = FlexDirection.Row;
            fileRow.style.flexShrink = 0.0f;
            root.Add(fileRow);

            m_AppField = new DropdownField("App");
            m_AppField.style.width = 300.0f;
            m_AppField.RegisterValueChangedCallback(OnAppChanged);
            fileRow.Add(m_AppField);

            m_FileField = new DropdownField("File");
            m_FileField.style.flexGrow = 1.0f;
            m_FileField.RegisterValueChangedCallback(OnFileChanged);
            fileRow.Add(m_FileField);

            m_ServerWarning = new HelpBox(
                "Signing in while the editor is running replaces this file with the server's list, which removes appended entries.",
                HelpBoxMessageType.Warning
            );
            m_ServerWarning.style.flexShrink = 0.0f;
            root.Add(m_ServerWarning);

            TwoPaneSplitView splitView =
                new TwoPaneSplitView(0, 520.0f, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            VisualElement entriesPane = new VisualElement();
            splitView.Add(entriesPane);

            m_SearchField = new ToolbarSearchField();
            m_SearchField.style.width = StyleKeyword.Auto;
            m_SearchField.style.flexShrink = 1.0f;
            m_SearchField.tooltip = "Matches the id, SKU, name, type, manufacturer or model id. Every word must match. Start a word with a hyphen to exclude it.";
            m_SearchField.RegisterValueChangedCallback(_ => RebuildDisplayedRows());
            entriesPane.Add(m_SearchField);

            m_EntriesLabel = MakeLabel();
            entriesPane.Add(m_EntriesLabel);

            m_EntriesView = new MultiColumnListView();
            m_EntriesView.itemsSource = m_DisplayedRows;
            m_EntriesView.selectionType = SelectionType.Single;
            m_EntriesView.fixedItemHeight = 20.0f;
            m_EntriesView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            m_EntriesView.columns.stretchMode = Columns.StretchMode.GrowAndFill;
            m_EntriesView.columns.Add(MakeColumn("Id", 60.0f, false, index => m_DisplayedRows[index].id));
            m_EntriesView.columns.Add(MakeColumn("Type", 120.0f, false, index => m_DisplayedRows[index].type));
            m_EntriesView.columns.Add(MakeColumn("Manufacturer", 120.0f, false, index => m_DisplayedRows[index].manufacturer));
            m_EntriesView.columns.Add(MakeColumn("Name", 200.0f, true, index => m_DisplayedRows[index].name));
            m_EntriesView.columns.Add(MakeColumn("SKU", 110.0f, true, index => m_DisplayedRows[index].sku));
            m_EntriesView.columns.Add(MakeColumn("Model Id", 200.0f, true, index => m_DisplayedRows[index].modelId));
            m_EntriesView.selectionChanged += _ => RefreshTemplate();
            m_EntriesView.style.flexGrow = 1.0f;
            entriesPane.Add(m_EntriesView);

            VisualElement generatorPane = new VisualElement();
            generatorPane.style.paddingLeft = 4.0f;
            generatorPane.style.paddingRight = 4.0f;
            splitView.Add(generatorPane);

            m_TemplateLabel = MakeLabel();
            m_TemplateLabel.style.whiteSpace = WhiteSpace.Normal;
            m_TemplateLabel.style.flexShrink = 0.0f;
            generatorPane.Add(m_TemplateLabel);

            m_CopySizesToggle = new Toggle("Copy sizes");
            m_CopySizesToggle.tooltip = "Copy the template's sizes. Otherwise they are left empty.";
            m_CopySizesToggle.SetValueWithoutNotify(m_CopySizes);
            m_CopySizesToggle.RegisterValueChangedCallback(changeEvent => m_CopySizes = changeEvent.newValue);
            m_CopySizesToggle.style.flexShrink = 0.0f;
            generatorPane.Add(m_CopySizesToggle);

            Label modelIdsLabel = MakeLabel();
            modelIdsLabel.text = "Model ids, one per line. Each line makes one entry.";
            generatorPane.Add(modelIdsLabel);

            m_ModelIdsField = MakeMultilineField(m_ModelIdsText);
            m_ModelIdsField.style.height = 120.0f;
            m_ModelIdsField.style.flexShrink = 0.0f;
            m_ModelIdsField.RegisterValueChangedCallback(changeEvent => { m_ModelIdsText = changeEvent.newValue; RefreshButtons(); });
            generatorPane.Add(m_ModelIdsField);

            VisualElement generateRow = MakeButtonRow();
            generatorPane.Add(generateRow);

            m_GenerateButton = MakeButton("Generate", "Write one entry per model id into the text below. The file is not changed.", Generate);
            generateRow.Add(m_GenerateButton);

            Label generatedLabel = MakeLabel();
            generatedLabel.text = "Entries to append. Edit them here before appending.";
            generatorPane.Add(generatedLabel);

            m_GeneratedField = MakeMultilineField(m_GeneratedText);
            m_GeneratedField.style.flexGrow = 1.0f;
            m_GeneratedField.style.flexShrink = 1.0f;
            m_GeneratedField.RegisterValueChangedCallback(changeEvent => { m_GeneratedText = changeEvent.newValue; RefreshButtons(); });
            generatorPane.Add(m_GeneratedField);

            VisualElement appendRow = MakeButtonRow();
            generatorPane.Add(appendRow);

            m_AppendButton = MakeButton("Append To File", "Add the entries above to the end of the file's list.", Append);
            appendRow.Add(m_AppendButton);

            m_ClearButton = MakeButton("Clear", "Clear the entries above.", () => m_GeneratedField.value = "");
            appendRow.Add(m_ClearButton);

            RefreshAppChoices();
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

        private static TextField MakeMultilineField(string value)
        {
            TextField field = new TextField();
            field.multiline = true;
            field.SetVerticalScrollerVisibility(ScrollerVisibility.Auto);
            field.SetValueWithoutNotify(value ?? "");

            return field;
        }

        private static Column MakeColumn(string title, float width, bool stretchable, Func<int, string> getText)
        {
            Column column = new Column();
            column.name = title;
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

        private void OnAppChanged(ChangeEvent<string> changeEvent)
        {
            m_AppName = changeEvent.newValue;
            m_EntriesView.SetSelectionWithoutNotify(Array.Empty<int>());

            RefreshFileChoices();
        }

        private void OnFileChanged(ChangeEvent<string> changeEvent)
        {
            m_FileKindLabel = changeEvent.newValue;
            m_EntriesView.SetSelectionWithoutNotify(Array.Empty<int>());

            LoadFile();
        }

        private void RefreshAppChoices()
        {
            List<string> appNames =
                Directory.GetDirectories(RESOURCES_FOLDER)
                    .Where(folder => s_FileKinds.Any(kind => File.Exists(Path.Combine(folder, FALLBACK_FOLDER_NAME, kind.fileName))))
                    .Select(Path.GetFileName)
                    .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

            m_AppField.choices = appNames;

            if (!appNames.Contains(m_AppName))
            {
                m_AppName = appNames.FirstOrDefault() ?? "";
            }

            m_AppField.SetValueWithoutNotify(m_AppName);

            RefreshFileChoices();
        }

        private void RefreshFileChoices()
        {
            List<string> fileLabels =
                s_FileKinds
                    .Where(kind => File.Exists(GetFilePath(m_AppName, kind)))
                    .Select(static kind => kind.label)
                    .ToList();

            m_FileField.choices = fileLabels;

            if (!fileLabels.Contains(m_FileKindLabel))
            {
                m_FileKindLabel = fileLabels.FirstOrDefault() ?? "";
            }

            m_FileField.SetValueWithoutNotify(m_FileKindLabel);

            LoadFile();
        }

        private FileKind GetFileKind()
        {
            return s_FileKinds.FirstOrDefault(kind => kind.label == m_FileKindLabel) ?? s_FileKinds[0];
        }

        private string GetFilePath()
        {
            return GetFilePath(m_AppName, GetFileKind());
        }

        private static string GetFilePath(string appName, FileKind kind)
        {
            return Path.Combine(RESOURCES_FOLDER, appName ?? "", FALLBACK_FOLDER_NAME, kind.fileName);
        }

        private void LoadFile()
        {
            m_Rows.Clear();
            m_LoadError = null;

            FileKind kind = GetFileKind();
            string filePath = GetFilePath();

            m_ServerWarning.style.display =
                kind.isAlwaysFromServer || !m_AppName.StartsWith("Stoll", StringComparison.OrdinalIgnoreCase)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            if (File.Exists(filePath))
            {
                m_LoadedWriteTime = File.GetLastWriteTimeUtc(filePath);

                if (TryReadList(File.ReadAllText(filePath), kind, out List<object> entries, out m_LoadError))
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (entries[i] is not Dictionary<string, object> entry)
                        {
                            continue;
                        }

                        m_Rows.Add(
                            new Row
                            {
                                position = i,
                                entry = entry,
                                id = GetText(entry, "id"),
                                sku = GetText(entry, "product_id"),
                                name = GetText(entry, "name"),
                                type = GetText(entry, kind.typeKey),
                                manufacturer = GetText(entry, "manufacturer"),
                                modelId = GetText(entry, kind.modelIdKey)
                            }
                        );
                    }
                }
            }
            else
            {
                m_LoadedWriteTime = default;
                m_LoadError = "There is no file at " + filePath;
            }

            RebuildDisplayedRows();
        }

        private static bool TryReadList(string json, FileKind kind, out List<object> entries, out string error)
        {
            entries = null;
            error = null;

            if (MiniJSON.Json.Deserialize(json) is not Dictionary<string, object> root)
            {
                error = "The file is not a JSON object.";
                return false;
            }

            if (!root.TryGetValue(kind.listKey, out object list) || list is not List<object> listEntries)
            {
                error = $"The file has no \"{kind.listKey}\" list.";
                return false;
            }

            entries = listEntries;
            return true;
        }

        private static string GetText(Dictionary<string, object> entry, string key)
        {
            return entry.TryGetValue(key, out object value) && value != null ? value.ToString().Trim() : "";
        }

        private void RebuildDisplayedRows()
        {
            int selectedPosition = GetTemplateRow()?.position ?? -1;

            string[] searchWords =
                Regex.Split(m_SearchField.value ?? "", @"\s+")
                    .Where(static word => !string.IsNullOrWhiteSpace(word))
                    .ToArray();

            m_DisplayedRows.Clear();
            m_DisplayedRows.AddRange(m_Rows.Where(row => MatchesSearch(row, searchWords)));

            m_EntriesView.RefreshItems();

            int selectedIndex = m_DisplayedRows.FindIndex(row => row.position == selectedPosition);
            m_EntriesView.SetSelectionWithoutNotify(selectedIndex >= 0 ? new[] { selectedIndex } : Array.Empty<int>());

            m_EntriesLabel.text =
                m_LoadError ?? $"{m_DisplayedRows.Count:N0} of {m_Rows.Count:N0} entries shown. Select one as the template.";

            RefreshTemplate();
        }

        private static bool MatchesSearch(Row row, string[] searchWords)
        {
            foreach (string searchWord in searchWords)
            {
                bool isExclusion = searchWord.Length > 1 && searchWord[0] == '-';
                string word = isExclusion ? searchWord.Substring(1) : searchWord;

                bool contains =
                    row.id.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    row.sku.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    row.name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    row.type.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    row.manufacturer.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    row.modelId.Contains(word, StringComparison.OrdinalIgnoreCase);

                if (contains == isExclusion)
                {
                    return false;
                }
            }

            return true;
        }

        private Row GetTemplateRow()
        {
            int index = m_EntriesView?.selectedIndex ?? -1;

            return index >= 0 && index < m_DisplayedRows.Count ? m_DisplayedRows[index] : null;
        }

        private void RefreshTemplate()
        {
            Row template = GetTemplateRow();
            FileKind kind = GetFileKind();

            if (template == null)
            {
                m_TemplateLabel.text = "Template: none. Select an entry on the left.";
            }
            else
            {
                string sizes =
                    string.Join(", ", kind.sizeKeys.Select(key => $"{key} {FormatValue(template.entry, key)}"));

                m_TemplateLabel.text =
                    $"Template: {template.name}\n" +
                    $"{kind.typeKey}: {FormatValue(template.entry, kind.typeKey)}\n" +
                    $"manufacturer: {FormatValue(template.entry, "manufacturer")}\n" +
                    $"Sizes: {sizes}";
            }

            RefreshButtons();
        }

        private static string FormatValue(Dictionary<string, object> entry, string key)
        {
            if (!entry.TryGetValue(key, out object value))
            {
                return "(missing)";
            }

            return value == null ? "null" : $"\"{value}\"";
        }

        private void RefreshButtons()
        {
            if (m_GenerateButton == null)
            {
                return;
            }

            m_GenerateButton.SetEnabled(GetTemplateRow() != null && GetModelIds().Count > 0);
            m_AppendButton.SetEnabled(m_LoadError == null && !string.IsNullOrWhiteSpace(m_GeneratedText));
            m_ClearButton.SetEnabled(!string.IsNullOrEmpty(m_GeneratedText));
        }

        private List<string> GetModelIds()
        {
            return (m_ModelIdsText ?? "")
                .Split('\n')
                .Select(static line => line.Trim())
                .Where(static line => line.Length > 0)
                .ToList();
        }

        private void Generate()
        {
            Row template = GetTemplateRow();

            if (template == null)
            {
                return;
            }

            FileKind kind = GetFileKind();
            List<string> modelIds = GetModelIds();
            List<object> entries = new ();

            for (int i = 0; i < modelIds.Count; i++)
            {
                entries.Add(MakeEntry(template.entry, kind, -(i + 1), modelIds[i], m_CopySizes));
            }

            m_GeneratedField.value = MiniJSON.Json.Serialize(entries, prettyPrint: true).Replace("\r\n", "\n");
        }

        private static Dictionary<string, object> MakeEntry(
            Dictionary<string, object> template,
            FileKind kind,
            long id,
            string modelId,
            bool copySizes)
        {
            Dictionary<string, object> entry = new ();

            foreach (KeyValuePair<string, object> field in template)
            {
                bool isCopied =
                    field.Key == "manufacturer" ||
                    field.Key == kind.typeKey ||
                    (copySizes && kind.sizeKeys.Contains(field.Key));

                entry[field.Key] = isCopied ? field.Value : GetBlankValue(field.Value);
            }

            entry["id"] = id;
            entry["name"] = modelId;
            entry["product_id"] = modelId;
            entry[kind.modelIdKey] = modelId;
            entry["manufacturer"] = template.TryGetValue("manufacturer", out object manufacturer) ? manufacturer : "";
            entry[kind.typeKey] = template.TryGetValue(kind.typeKey, out object type) ? type : "";

            return entry;
        }

        private static object GetBlankValue(object value)
        {
            return value switch
            {
                string => "",
                long or double => 0L,
                bool => false,
                IList => new List<object>(),
                _ => null
            };
        }

        private void Append()
        {
            FileKind kind = GetFileKind();
            string filePath = GetFilePath();

            List<Dictionary<string, object>> newEntries = ParseGeneratedEntries();

            if (newEntries == null)
            {
                EditorUtility.DisplayDialog(WINDOW_TITLE, "The text is not a JSON list of entries, or a single entry. Check it for typos.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    WINDOW_TITLE,
                    $"Append {newEntries.Count} {(newEntries.Count == 1 ? "entry" : "entries")} to {m_AppName}/{kind.fileName}?",
                    "Append",
                    "Cancel"))
            {
                return;
            }

            string fileText = File.ReadAllText(filePath);

            if (!TryAppendEntries(fileText, kind, newEntries, out string newFileText, out string error))
            {
                EditorUtility.DisplayDialog(WINDOW_TITLE, "The file was not changed. " + error, "OK");
                return;
            }

            File.WriteAllText(filePath, newFileText);
            AssetDatabase.ImportAsset(filePath);

            Debug.Log($"Appended {newEntries.Count} entries to {filePath}");

            m_GeneratedField.value = "";
            m_SearchField.value = "";

            LoadFile();

            if (m_DisplayedRows.Count > 0)
            {
                m_EntriesView.ScrollToItem(m_DisplayedRows.Count - 1);
            }
        }

        private List<Dictionary<string, object>> ParseGeneratedEntries()
        {
            object parsed = MiniJSON.Json.Deserialize(m_GeneratedText ?? "");

            if (parsed is Dictionary<string, object> singleEntry)
            {
                return new List<Dictionary<string, object>> { singleEntry };
            }

            if (parsed is List<object> list && list.Count > 0 && list.All(static item => item is Dictionary<string, object>))
            {
                return list.Cast<Dictionary<string, object>>().ToList();
            }

            return null;
        }

        private static bool TryAppendEntries(
            string fileText,
            FileKind kind,
            List<Dictionary<string, object>> newEntries,
            out string newFileText,
            out string error)
        {
            newFileText = null;

            if (!TryReadList(fileText, kind, out List<object> oldEntries, out error))
            {
                return false;
            }

            int rootClose = LastNonWhitespaceIndex(fileText, fileText.Length);
            int listClose = LastNonWhitespaceIndex(fileText, rootClose);
            int lastItemEnd = LastNonWhitespaceIndex(fileText, listClose);

            if (rootClose < 0 || fileText[rootClose] != '}' ||
                listClose < 0 || fileText[listClose] != ']' ||
                lastItemEnd < 0)
            {
                error = $"The file does not end with the \"{kind.listKey}\" list.";
                return false;
            }

            string newline = fileText.Contains("\r\n") ? "\r\n" : "\n";

            bool isEmptyList = fileText[lastItemEnd] == '[';

            string closeIndent = GetLineIndent(fileText, listClose);
            string itemIndent = isEmptyList ? null : GetLineIndent(fileText, lastItemEnd);

            if (itemIndent == null || itemIndent.Length <= closeIndent.Length)
            {
                itemIndent = closeIndent + new string(' ', MiniJSON.Json.PrettyPrintIndentSize);
            }

            List<string> itemTexts = new ();
            int defaultIndentSize = MiniJSON.Json.PrettyPrintIndentSize;

            try
            {
                MiniJSON.Json.PrettyPrintIndentSize = itemIndent.Length - closeIndent.Length;

                foreach (Dictionary<string, object> entry in newEntries)
                {
                    itemTexts.Add(
                        MiniJSON.Json.Serialize(entry, prettyPrint: true)
                            .Replace("\r\n", "\n")
                            .Replace("\n", newline + itemIndent)
                    );
                }
            }
            finally
            {
                MiniJSON.Json.PrettyPrintIndentSize = defaultIndentSize;
            }

            newFileText =
                fileText.Substring(0, lastItemEnd + 1) +
                (isEmptyList ? "" : ",") +
                newline + itemIndent +
                string.Join("," + newline + itemIndent, itemTexts) +
                newline + closeIndent +
                fileText.Substring(listClose);

            if (!TryReadList(newFileText, kind, out List<object> checkedEntries, out error) ||
                checkedEntries.Count != oldEntries.Count + newEntries.Count)
            {
                error = $"The result could not be checked: {error ?? "the entry count does not match"}.";
                newFileText = null;
                return false;
            }

            return true;
        }

        private static string GetLineIndent(string text, int index)
        {
            int lineStart = text.LastIndexOf('\n', index) + 1;
            string indent = text.Substring(lineStart, index - lineStart);

            return string.IsNullOrWhiteSpace(indent) ? indent : "";
        }

        private static int LastNonWhitespaceIndex(string text, int before)
        {
            for (int i = before - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [SerializeField] private string m_AppName = "";
        [SerializeField] private string m_FileKindLabel = "";
        [SerializeField] private bool m_CopySizes;
        [SerializeField] private string m_ModelIdsText = "";
        [SerializeField] private string m_GeneratedText = "";

        private readonly List<Row> m_Rows = new ();
        private readonly List<Row> m_DisplayedRows = new ();
        private DateTime m_LoadedWriteTime;
        private string m_LoadError;

        private DropdownField m_AppField;
        private DropdownField m_FileField;
        private HelpBox m_ServerWarning;
        private ToolbarSearchField m_SearchField;
        private Label m_EntriesLabel;
        private MultiColumnListView m_EntriesView;
        private Label m_TemplateLabel;
        private Toggle m_CopySizesToggle;
        private TextField m_ModelIdsField;
        private Button m_GenerateButton;
        private TextField m_GeneratedField;
        private Button m_AppendButton;
        private Button m_ClearButton;

        private class Row
        {
            public int position;
            public Dictionary<string, object> entry;
            public string id;
            public string sku;
            public string name;
            public string type;
            public string manufacturer;
            public string modelId;
        }

        private class FileKind
        {
            public string label;
            public string fileName;
            public string listKey;
            public string modelIdKey;
            public string typeKey;
            public string[] sizeKeys;
            public bool isAlwaysFromServer;
        }
    }
}
