namespace KitchenDesign.ProductsEditor
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.AddressableContent;
    using KitchenDesign.ProductsEditor.Scriptables;
    using MEC;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;
    using UnityEngine.UIElements;
    using System.Threading.Tasks;
    /// <summary>
    ///     The <see cref="ProductVisualDatabaseManager"/> is an editor window
    ///     for managing the kitchen models and visual product data in the
    ///     project.
    /// </summary>
    /// <remarks>
    ///     Features:<br />
    ///         - Displays Product Visuals in a detailed list<br />
    ///         - Automatically detects issues and checks for model updates<br />
    ///         - Each entry has a status icon to quickly indicate issues<br />
    ///         - Allows new Product Visuals to be created from a model; model
    ///           selection window is filtered (by default) to models that
    ///           haven't been used yet
    /// </remarks>
    public class ProductVisualDatabaseManager : EditorWindow
    {
        // =====================================================================
        // Static functions
        // =====================================================================

        /// <summary>
        ///     Open the product visual database manager
        /// </summary>
        /// <param name="database">
        ///     The database to display.
        /// </param>
        public static void ShowDatabase(ProductVisualDatabaseSO database)
        {
            ProductVisualDatabaseManager window =
                GetWindow<ProductVisualDatabaseManager>("Product Visual Database");

            window.SetDatabase(database);
            window.Show();
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Reset the window to display the database.
        /// </summary>
        public void SetDatabase(ProductVisualDatabaseSO database)
        {
            //// These two lines seems to have fixed an issue where database
            //// entries would break - point to invalid data - after their model
            //// was reimported (e.g., when changing the model's default material
            //// assignments).
            //// REVIEW: No they have not. Still having this issue when remapping
            ////         the model's materials.
            //using SerializedObject serializedDatabase = new SerializedObject(database);
            //serializedDatabase.Update();

            m_Database = database;

            ListView listView = rootVisualElement.Q<ListView>("Table");
            listView.itemsSource = m_Database ? (IList)m_Database.sourceEntries : null;

            // Display products with any status by default.
            m_SearchStatusFilter = (ProductVisualEditorSetupStatus)~0;

            RefreshSearch();
        }

        private void OnEnable()
        {
            // Create/load icons to display next to product models. These offer
            // a quick way to visualize which product models have issues
            m_StatusGood = new GUIContent(text: "", image: null, tooltip: "Good!");

            m_StatusOrange = EditorGUIUtility.IconContent("orangeLight");
            m_StatusOrange.tooltip = "Model hash has changed. Verify the product visual is good and click Approve Model";

            m_StatusRed = EditorGUIUtility.IconContent("redLight");
            m_StatusRed.tooltip = "Incomplete. Please finish setting up the product visual";
        }

        private void CreateGUI()
        {
            VisualElement document = m_DocumentAsset.Instantiate();
            document.StretchToParentSize();
            rootVisualElement.Add(document);

            ToolbarSearchField searchField = GetSearchField();
            searchField.delegatesFocus = false;
            searchField.RegisterCallback<ChangeEvent<string>>(OnSearchChange);

            // Create the table's headers. We instantiate two table rows to act
            // as the header. The first row has all its elements replaced with
            // label elements. The second row is left default but made
            // invisible. The invisible row informs the size of the label row's fields
            {
                VisualElement tableHeaders = document.Q("TableHeaders");

                VisualElement labelHeader = m_TableRowAsset.Instantiate();
                tableHeaders.Add(labelHeader);

                VisualElement hiddenHeader = m_TableRowAsset.Instantiate();
                hiddenHeader.visible = false;
                hiddenHeader.style.height = 0.0f;
                tableHeaders.Add(hiddenHeader);

                VisualElement labelHeaderLayout = labelHeader.Q("HorizontalLayout");
                VisualElement hiddenHeaderLayout = hiddenHeader.Q("HorizontalLayout");
                labelHeader.userData = hiddenHeaderLayout;
                labelHeaderLayout.RegisterCallback<GeometryChangedEvent>(UpdateHeaderSize);

                // For each field in the row, replace it with a label
                int childCount = labelHeaderLayout.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    VisualElement original = labelHeaderLayout.ElementAt(i);

                    // Use the names and size of previous row controls
                    Label headerLabel =
                        new Label(
                            original is BindableElement && original is not Button ?
                                original.name :
                                ""
                            );

                    headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    headerLabel.style.flexGrow = original.resolvedStyle.flexGrow;
                    headerLabel.style.display = original.resolvedStyle.display;

                    labelHeaderLayout.RemoveAt(i);
                    labelHeaderLayout.Insert(i, headerLabel);
                }
            }

            RebuildSearchStatusFilterMenu();

            ListView listView = GetTable();

            Button addOccupantButton = document.Q<Button>("AddOccupantEntry");
            addOccupantButton.clicked += CreateAndAddOccupantProductVisualPrefab;

            Button addSectionButton = document.Q<Button>("AddSectionEntry");
            addSectionButton.clicked += CreateAndAddSectionProductVisualPrefab;

            Button addMultipleOccupantButton = document.Q<Button>("AddMultipleOccupantEntry");
            addMultipleOccupantButton.clicked += CreateAndAddMultipleOccupantProductVisualPrefab;

            Button addMultipleSectionButton = document.Q<Button>("AddMultipleSectionEntry");
            addMultipleSectionButton.clicked += CreateAndAddMultipleSectionProductVisualPrefab;

            Button addFinishButton = document.Q<Button>("AddFinishEntry");
            addFinishButton.clicked += CreateAndAddFinishProductVisualPrefab;

            Button addMultipleFinishButton = document.Q<Button>("AddMultipleFinishEntry");
            addMultipleFinishButton.clicked += CreateAndAddMultipleFinishProductVisualPrefab;

            Button addExistingButton = document.Q<Button>("AddExistingEntry");
            addExistingButton.clicked += AddExistingProductVisualPrefab;

            Button addMultipleExistingProductsButton = document.Q<Button>("AddMultipleExistingProductEntry");
            addMultipleExistingProductsButton.clicked += AddMultipleExistingProductProductVisualPrefab;

            Button addMultipleExistingCabinetsButton = document.Q<Button>("AddMultipleExistingCabinetEntry");
            addMultipleExistingCabinetsButton.clicked += AddMultipleExistingCabinetProductVisualPrefab;

            ToolbarMenu toolsMenu = document.Q<ToolbarMenu>("ToolsMenu");
            toolsMenu.menu.AppendAction("Duplicate selected visual...", DuplicateSelectedVisual);
            toolsMenu.menu.AppendAction("Delete orphaned thumbnails...", DeleteOrphanedThumbnails);
            toolsMenu.menu.AppendAction("Delete visuals without underlying assets...", DeleteProductsWithoutModels);
            toolsMenu.menu.AppendAction("Remove displayed visuals...", RemoveDisplayedVisuals);
            toolsMenu.menu.AppendAction("Remove selected visual...", RemoveSelectedVisual);
            toolsMenu.menu.AppendAction("Strip trailing \" Variant\" from visual names", StripVariantWordFromVisualNames);
            toolsMenu.menu.AppendAction("Fix cabinets with no occupiable height", FixCabinetsWithNoOccupiableHeight);
            toolsMenu.menu.AppendAction("Load or save database states...", ShowDatabaseStates);
            toolsMenu.menu.AppendAction("Remove entries or append sets...", ShowDatabaseSets);

            Button approveModelsButton = document.Q<Button>("ApproveModels");
            approveModelsButton.clicked += ApproveModelsOfFilteredResults;

            Button renderThumbnailsButton = document.Q<Button>("RenderThumbnails");
            renderThumbnailsButton.clicked += RenderThumbnailsOfFilteredResults;

            // Method to create a row for the table. The scroll view will create
            // only as many rows as it needs to fill its viewport and then they
            // are virtualized (reused when content scrolls on/off-screen)
            Func<VisualElement> makeItem = () =>
            {
                VisualElement row = m_TableRowAsset.Instantiate();
                row.RegisterCallback<ClickEvent>(OnRowClicked);

                VisualElement status = row.Q("Status");
                status.RegisterCallback<ClickEvent>(OnRowStatusClicked);

                ObjectField visualPrefabField = row.Q<ObjectField>("Visual");
                visualPrefabField.RegisterCallback<ClickEvent>(OnRowVisualClicked);
                visualPrefabField.RegisterValueChangedCallback(OnRowVisualChanged);

                TextField idField = row.Q<TextField>("Id");
                idField.RegisterCallback<ChangeEvent<string>>(OnRowIdChanged);

                TextField visualPrefabNameField = row.Q<TextField>("VisualName");
                visualPrefabNameField.RegisterValueChangedCallback(OnRowVisualNameChanged);

                ObjectField visualContentPrefab = row.Q<ObjectField>("Prefab");
                //modelPrefabField.RegisterValueChangedCallback(OnRowModelPrefabChanged);
                visualContentPrefab.RegisterCallback<ClickEvent>(OnRowVisualContentPrefabClicked);

                ObjectField lightweightData = row.Q<ObjectField>("Lightweight");
                //modelPrefabField.RegisterValueChangedCallback(OnRowModelPrefabChanged);
                visualContentPrefab.RegisterCallback<ClickEvent>(OnRowLightweightDataClicked);

                ObjectField modelPrefabField = row.Q<ObjectField>("UnderlyingAsset");
                //modelPrefabField.RegisterValueChangedCallback(OnRowModelPrefabChanged);
                modelPrefabField.RegisterCallback<ClickEvent>(OnRowUnderlyingAssetClicked);

                EnumField anchorField = row.Q<EnumField>("Anchor");
                anchorField.RegisterCallback<ChangeEvent<Enum>>(OnRowAnchorChanged);

                VisualElement thumbnailImage = row.Q("Thumbnail");
                thumbnailImage.RegisterCallback<ClickEvent>(OnRowThumbnailClicked);

                Button renderThumbnailButton = row.Q<Button>("RenderThumbnail");
                renderThumbnailButton.RegisterCallback<ClickEvent>(OnRowRenderThumbnailClicked);

                Button approveModelButton = row.Q<Button>("ApproveModel");
                approveModelButton.RegisterCallback<ClickEvent>(OnRowApproveModelPrefabClicked);

                return row;
            };

            // Update a previously instantiated table row. Assign data at index
            // to the UI fields (i.e. virtualize it)
            Action<VisualElement, int> bindItem = (row, i) =>
            {
                row.userData = i;

                ProductVisualEditorSetupStatus rowStatus = ProductVisualEditorSetupStatus.MissingFields;
                string              rowId = "";
                UnityEngine.Object  rowVisualPrefab = null;
                string              rowVisualPrefabName = "";
                ProductAnchor       rowAnchor = ProductAnchor.Unknown;
                UnityEngine.Object  rowLightweightData = null;
                UnityEngine.Object  rowVisualContentPrefab = null;
                UnityEngine.Object  rowUnderlyingAsset = null;
                Sprite              rowThumbnail = null;

                bool isModelApproved = false;
                bool isLastOpenedRow = false;

                // Status
                if (m_DisplayedData != null && m_DisplayedData[i])
                {
                    ProductVisualSO v = m_DisplayedData[i];
                    ProductVisualContent content = v.contentAddress.editorOnlyCachedAsset;

                    rowVisualPrefab         = v;
                    rowStatus               = content.editorSetupStatus;
                    rowId                   = v.info.id;
                    rowVisualPrefabName     = content.name;
                    rowVisualContentPrefab  = content;
                    rowLightweightData      = v.info;
                    rowAnchor               = v.info.anchor;
                    rowUnderlyingAsset      = content.underlyingAsset;
                    rowThumbnail            = v.info.thumbnail;
                    isModelApproved         = content.currentHash == content.approvedHash;
                    isLastOpenedRow         = v == m_LastVisualPrefabSelected;
                }

                GUIContent statusContent = null;
                switch (rowStatus)
                {
                    case ProductVisualEditorSetupStatus.MissingFields:
                        statusContent = m_StatusRed;
                        break;

                    case ProductVisualEditorSetupStatus.HashHasChanged:
                        statusContent = m_StatusOrange;
                        break;

                    default:
                        statusContent = m_StatusGood;
                        break;
                }

                // Highlight the row if it was the last one opened
                VisualElement rowRoot = row.Q<VisualElement>("HorizontalLayout");

                // A green colour similar to the highlight applied in the
                // project window after a prefab is opened.
                Color HighlightColor = new Color32(0, 255, 0, 50);

                rowRoot.style.backgroundColor =
                    isLastOpenedRow ? HighlightColor : new StyleColor(StyleKeyword.Auto);

                // Update the status indicator to make issues easier to spot at
                // a glance
                VisualElement statusWidget = row.Q<VisualElement>("Status");
                statusWidget.style.backgroundImage = (Texture2D)statusContent.image;
                statusWidget.tooltip = statusContent.tooltip;

                // If the model is out-of-date, visualize it with a button color
                // change
                Button approveModelButton = row.Q<Button>("ApproveModel");
                approveModelButton.style.backgroundColor = isModelApproved ? Color.grey : Color.red;

                // Set values for UI without triggering callbacks
                row.Q<ObjectField>("Visual").SetValueWithoutNotify(rowVisualPrefab);
                row.Q<TextField>("VisualName").SetValueWithoutNotify(rowVisualPrefabName);
                row.Q<TextField>("Id").SetValueWithoutNotify(rowId);
                row.Q<EnumField>("Anchor").SetValueWithoutNotify(rowAnchor);
                ObjectField underlyingAssetField = row.Q<ObjectField>("UnderlyingAsset");
                underlyingAssetField.SetValueWithoutNotify(rowUnderlyingAsset);
                underlyingAssetField.SetEnabled(false); // make read-only
                ObjectField visualContentPrefabField = row.Q<ObjectField>("Prefab");
                visualContentPrefabField.SetValueWithoutNotify(rowVisualContentPrefab);
                visualContentPrefabField.SetEnabled(false); // make read-only
                ObjectField lightweightData = row.Q<ObjectField>("Lightweight");
                lightweightData.SetValueWithoutNotify(rowLightweightData);
                lightweightData.SetEnabled(false); // make read-only

                row.Q("Thumbnail").style.backgroundImage = new StyleBackground(rowThumbnail);
            };

            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = null;
        }

        private ToolbarSearchField GetSearchField()
        {
            return rootVisualElement.Q<ToolbarSearchField>("SearchField");
        }

        private ListView GetTable()
        {
            return rootVisualElement.Q<ListView>("Table");
        }

        private void RefreshSearch()
        {
            m_FilteredData.Clear();

            string searchInput = GetSearchField().value;

            bool hasSearchInput = !string.IsNullOrWhiteSpace(searchInput);

            if (hasSearchInput)
            {
                // Use regex to handle word splitting
                string[] searchWords = Regex.Split(searchInput, @"\s+")
                    .Where(word => !string.IsNullOrWhiteSpace(word))
                    .ToArray();

                const StringComparison ComparisonMode = StringComparison.OrdinalIgnoreCase;

                bool MustContain(string haystack, string needle)
                {
                    return haystack != null && haystack.Contains(needle, ComparisonMode);
                }

                bool MustNotContain(string haystack, string needle)
                {
                    return haystack != null && !haystack.Contains(needle, ComparisonMode);
                }

                foreach (ProductVisualSO entry in m_Database.sourceEntries)
                {
                    if (!entry)
                    {
                        continue;
                    }

                    ProductVisualContent content =
                        entry.contentAddress.editorOnlyCachedAsset;

                    int matchCount = 0;

                    for (int wordIndex = 0; wordIndex < searchWords.Length; wordIndex++)
                    {
                        string word = searchWords[wordIndex];

                        // A word starting with a hyphen is treated as an
                        // exclusive search. Exclude any result that contains
                        // that word.
                        if (word.StartsWith('-') && word.Length > 1)
                        {
                            word = word.Substring(1);   // remove the leading hyphen

                            if (MustNotContain(entry.info.id, word) &&
                                MustNotContain(content.name, word) &&
                                (content.productModelPrefab == null || MustNotContain(content.productModelPrefab.name, word)))
                            {
                                ++matchCount;
                            }
                            else
                            {
                                // Skip remanining words because this one matched
                                // and excludes it
                                break;
                            }
                        }
                        else if (MustContain(entry.info.id, word) ||
                            MustContain(content.name, word) ||
                            (content.productModelPrefab != null && MustContain(content.productModelPrefab.name, word)))
                        {
                            ++matchCount;
                        }
                        else
                        {
                            // Optimization: Early exit if word doesn't match
                            break;
                        }
                    }

                    // Every search word should match (excluding negative words)
                    if (matchCount == searchWords.Length &&
                        m_SearchStatusFilter.HasFlag(content.editorSetupStatus))
                    {
                        m_FilteredData.Add(entry);
                    }
                }

                m_DisplayedData = m_FilteredData;
            }
            else
            {
                // Filter out products that are good (i.e. have no issues)
                foreach (ProductVisualSO entry in m_Database.sourceEntries)
                {
                    if (entry && m_SearchStatusFilter.HasFlag(entry.contentAddress.editorOnlyCachedAsset.editorSetupStatus))
                    {
                        m_FilteredData.Add(entry);
                    }
                }

                m_DisplayedData = m_FilteredData;
            }

            ListView listView = rootVisualElement.Q<ListView>("Table");
            listView.itemsSource = (IList)m_DisplayedData;

            // Unity tries to be smart and re-select the previous table item
            // when it returns in view. The problem is it does this whenever
            // the table refreshes too and doesn't offer a way to clear this
            // memory. Whenever the list refreshes, e.g. using the search bar,
            // focus jumps to the previous table row and its contents get
            // stomped by the user's next typed characters. To avoid this,
            // we manually re-select the search bar after the refresh occurs.

            // Tried a bunch of alternative solutions: clearing the selection,
            // clearing the selection through reflection, etc.

            Focusable focusedPreRefresh = listView.focusController.focusedElement;
            ToolbarSearchField searchField = focusedPreRefresh as ToolbarSearchField;
            TextField searchTextField = searchField?.Q<TextField>();
            int cursorIndex = 0;
            if (searchTextField != null)
            {
                cursorIndex = searchTextField.cursorIndex;
            }

            // Refresh the list. This re-selects the previous row control!
            listView.RefreshItems();

            if (focusedPreRefresh != null)
            {
                if (searchTextField != null)
                {
                    // Solution from: https://forum.unity.com/threads/focus-doesnt-seem-to-work.901130/#post-5917760
                    rootVisualElement.schedule.Execute(() => {
                        searchTextField.ElementAt(0).Focus();
                        searchTextField.SelectRange(cursorIndex, cursorIndex);
                    });
                }
                else
                {
                    focusedPreRefresh.Focus();
                }
            }

            if (m_LastVisualPrefabSelected)
            {
                // Try to keep the previously clicked item in view.
                int index =
                    MelcherCommon.IndexOf(
                        m_DisplayedData,
                        m_LastVisualPrefabSelected
                    );

                if (index >= 0)
                {
                    listView.ScrollToItem(index);
                }
            }

            string statusMessage = "No database loaded";

            int sourceItemCount = 0;
            if (m_Database && m_Database.sourceEntries != null)
            {
                sourceItemCount = m_Database.sourceEntries.Count;
                statusMessage = $"{sourceItemCount} items";
            }

            if (m_SearchStatusFilter != (ProductVisualEditorSetupStatus)~0 || hasSearchInput)
            {
                statusMessage = $"Matched {m_DisplayedData.Count} of {sourceItemCount} items";
            }

            rootVisualElement.Q<Label>("StatusMessage").text = statusMessage;
        }

        private void OnSearchChange(ChangeEvent<string> changeEvent)
        {
            RefreshSearch();
        }

        private void UpdateHeaderSize(GeometryChangedEvent geometryChangedEvent)
        {
            VisualElement headerLabelsLayout = (VisualElement)geometryChangedEvent.target;
            VisualElement headerHiddenLayout = (VisualElement)headerLabelsLayout.FindAncestorUserData();

            // For each field in the header, copy the size from its invisible sibling
            int childCount = headerLabelsLayout.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                VisualElement label = headerLabelsLayout.ElementAt(i);
                VisualElement original = headerHiddenLayout.ElementAt(i);

                if (original.style.flexGrow.value <= 0.0f)
                {
                    float width = original.resolvedStyle.width + original.resolvedStyle.paddingLeft + original.resolvedStyle.paddingRight;
                    label.style.width = width;
                    label.style.minWidth = width;
                }
            }
        }

        private ProductVisualSO RowEventToRowData(EventBase eventBase, out VisualElement caller, out int dataIndex)
        {
            ProductVisualSO productVisual = null;

            caller = (VisualElement)eventBase.currentTarget;

            dataIndex = (caller != null) ? (int)caller.FindAncestorUserData() : -1;

            if (caller != null && dataIndex >= 0 && dataIndex < m_DisplayedData.Count)
            {
                productVisual = m_DisplayedData[dataIndex];
            }

            return productVisual;
        }

        private void GetObjectAssetUsingPicker(string pickerTitle, string projectRelativeFolderPath, string assetSearchFilter, GameObjectPickedEventHandler onFinished, Func<(string assetPath, UnityEngine.Object asset), bool> assetFilter)
        {
            // TODO: Optimize? This will be slow the first time because we are
            //       loading all models.
            string progressTitle = "Loading ALL requested object assets. Will be slow...";
            List<UnityEngine.Object> allObjects = new ();
            try
            {
                string[] guids =
                    AssetDatabase.FindAssets(
                        filter: assetSearchFilter,
                        searchInFolders: new[] { projectRelativeFolderPath }
                    );

                for (int i = 0; i < guids.Length; ++i)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                    string progressInfo = $"({i + 1}/{guids.Length}) {assetPath}";

                    bool progressCancel = EditorUtility.DisplayCancelableProgressBar(
                        progressTitle,
                        progressInfo,
                        progress: (i + 1) / (float)guids.Length
                    );

                    if (progressCancel)
                    {
                        return;
                    }

                    UnityEngine.Object asset =
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

                    if (assetFilter != null && !assetFilter.Invoke((assetPath, asset)))
                    {
                        continue;
                    }

                    allObjects.Add(asset);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            allObjects.Sort(
                static (a, b) =>
                {
                    return EditorUtility.NaturalCompare(a.name, b.name);
                }
            );

            HashSet<UnityEngine.Object> objectsInUse = new ();

            foreach (ProductVisualSO entry in m_Database.sourceEntries)
            {
                ProductVisualContent content = entry.contentAddress.editorOnlyCachedAsset;

                TryAddObject(objectsInUse, entry);

                TryAddObject(objectsInUse, content.gameObject);
                TryAddObject(objectsInUse, content.productModelPrefab);
                TryAddObject(objectsInUse, content.sectionModelPrefab);
                TryAddObject(objectsInUse, content.finishMaterial);

                static void TryAddObject(HashSet<UnityEngine.Object> all, UnityEngine.Object toAdd)
                {
                    if (toAdd)
                    {
                        all.Add(toAdd);
                    }
                }
            }

            FilteredObjectSelector.ShowPicker(
                pickerTitle,
                allObjects,
                getRowName: index => allObjects[index].name,
                getRowStatus: null,
                (selection) =>
                {
                    bool madeSelection = selection != null;
                    UnityEngine.Object selected = null;
                    if (selection.Count > 0)
                    {
                        selected = allObjects[selection[0]];
                    }
                    onFinished?.Invoke(madeSelection, selected);
                },
                extraFilter: index =>
                {
                    UnityEngine.Object gameObjectAtIndex = allObjects[index];
                    return !objectsInUse.Contains(gameObjectAtIndex);
                },
                extraFilterText: "Hide Used",
                extraFilterTooltip: "Hide objects that are already used",
                extraFilterDefaultOn: true
            );
        }

        private void GetMultipleObjectAssetUsingPicker(string pickerTitle, string projectRelativeFolderPath, string assetSearchFilter, Func<UnityEngine.Object, bool> addObject, Func<(string assetPath, UnityEngine.Object asset), bool> assetFilter, Func<string, bool> assetPathFilter = null)
        {
            // TODO: Optimize? This will be slow the first time because we are
            //       loading all models.
            string progressTitle = "Loading ALL requested object assets. Will be slow...";
            List<UnityEngine.Object> allObjects = new ();
            try
            {
                string[] guids =
                    AssetDatabase.FindAssets(
                        filter: assetSearchFilter,
                        searchInFolders: new[] { projectRelativeFolderPath }
                    );

                for (int i = 0; i < guids.Length; ++i)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                    if (assetPathFilter != null && !assetPathFilter.Invoke(assetPath))
                    {
                        continue;
                    }

                    string progressInfo = $"({i + 1}/{guids.Length}) {assetPath}";

                    bool progressCancel = EditorUtility.DisplayCancelableProgressBar(
                        progressTitle,
                        progressInfo,
                        progress: (i + 1) / (float)guids.Length
                    );

                    if (progressCancel)
                    {
                        return;
                    }

                    UnityEngine.Object asset =
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

                    if (assetFilter != null && !assetFilter.Invoke((assetPath, asset)))
                    {
                        continue;
                    }

                    allObjects.Add(asset);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            allObjects.Sort(
                static (a, b) =>
                {
                    return EditorUtility.NaturalCompare(a.name, b.name);
                }
            );

            HashSet<UnityEngine.Object> objectsInUse = new ();

            foreach (ProductVisualSO entry in m_Database.sourceEntries)
            {
                ProductVisualContent content = entry.contentAddress.editorOnlyCachedAsset;

                TryAddObject(objectsInUse, entry);

                TryAddObject(objectsInUse, content.gameObject);
                TryAddObject(objectsInUse, content.productModelPrefab);
                TryAddObject(objectsInUse, content.sectionModelPrefab);
                TryAddObject(objectsInUse, content.finishMaterial);

                static void TryAddObject(HashSet<UnityEngine.Object> all, UnityEngine.Object toAdd)
                {
                    if (toAdd)
                    {
                        all.Add(toAdd);
                    }
                }
            }

            FilteredObjectSelector.ShowPicker(
                pickerTitle,
                allObjects,
                getRowName: index => allObjects[index].name,
                getRowStatus: null,
                (selection) =>
                {
                    if (selection != null && selection.Count > 0)
                    {
                        List<UnityEngine.Object> selectedObjects =
                            selection.Select(index => allObjects[index]).ToList();

                        Timing.RunCoroutine(
                            AddMultipleObjectsRoutine(selectedObjects, addObject),
                            Segment.EditorUpdate
                        );
                    }
                },
                extraFilter: index =>
                {
                    UnityEngine.Object gameObjectAtIndex = allObjects[index];
                    return !objectsInUse.Contains(gameObjectAtIndex);
                },
                extraFilterText: "Hide Used",
                extraFilterTooltip: "Hide objects that are already used",
                extraFilterDefaultOn: true,
                allowMultipleSelection: true
            );
        }

        protected IEnumerator<float> AddMultipleObjectsRoutine(List<UnityEngine.Object> objects, Func<UnityEngine.Object, bool> addObject)
        {
            const string ProgressTitle = "Adding product visuals";

            yield return Timing.WaitForOneFrame;

            using var hideProgressDiaog =
                MelcherDeferred.Defer(
                    static () => EditorUtility.ClearProgressBar()
                );

            int addedCount = 0;

            for (int i = 0; i < objects.Count; i++)
            {
                UnityEngine.Object toAdd = objects[objects.Count - 1 - i];

                bool didCancel =
                    EditorUtility.DisplayCancelableProgressBar(
                        title: ProgressTitle,
                        info: $"({i + 1}/{objects.Count}) {toAdd.name}",
                        progress: (float)i / objects.Count
                    );

                if (didCancel)
                {
                    break;
                }

                try
                {
                    if (addObject.Invoke(toAdd))
                    {
                        ++addedCount;
                    }
                }
                catch (Exception e)
                {
                    MelcherLog.LogError($"Could not add: {toAdd.name}\nException: {e}", toAdd);
                }

                yield return Timing.WaitForOneFrame;
            }

            EditorUtility.DisplayProgressBar(ProgressTitle, "Saving assets", 1.0f);

            AssetDatabase.SaveAssets();

            MelcherLog.Log($"Added {addedCount} of {objects.Count} product visuals to {m_Database.name}", m_Database);

            if (addedCount <= 0 || !this)
            {
                yield break;
            }

            GetSearchField().SetValueWithoutNotify("");
            RefreshSearch();

            IReadOnlyList<ProductVisualSO> sourceEntries = m_Database.sourceEntries;

            int selectIndex =
                MelcherCommon.IndexOf(
                    m_DisplayedData,
                    sourceEntries[sourceEntries.Count - 1]
                );

            if (selectIndex >= 0)
            {
                ListView listView = GetTable();
                listView.ScrollToItem(selectIndex);
                listView.SetSelection(selectIndex);
            }
        }

        /// <summary>
        ///     Remember the last row that was clicked such that a developer
        ///     can search for a product, clear their search, and still have
        ///     the previously selected object highlighted.
        /// </summary>
        private void OnRowClicked(ClickEvent clickEvent)
        {
            VisualElement caller = (VisualElement)clickEvent.currentTarget;

            int dataIndex = -1;
            if (caller.userData != null)
            {
                dataIndex = (int)caller.userData;
            }

            if (dataIndex < 0 || dataIndex >= m_DisplayedData.Count)
            {
                return;
            }

            m_LastVisualPrefabSelected = m_DisplayedData[dataIndex];

            // Refresh the whole table (to hide the old highlight and show the
            // new)
            GetTable().RefreshItems();
        }

        /// <summary>
        /// Clicking the status icon shows a model picker window that can be
        /// used to swap out the prefab's current model.
        /// </summary>
        /// <param name="clickEvent"></param>
        private void OnRowStatusClicked(ClickEvent clickEvent)
        {
            //ProductVisual target = RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);
            //GetModelPrefabUsingPicker(
            //    onFinished: (madeSelection, selection) =>
            //    {
            //        if (madeSelection)
            //        {
            //            using SerializedObject serializedObject = new SerializedObject(target);
            //            using SerializedProperty modelPrefabProperty = serializedObject.FindProperty(ProductVisual.ModelPrefabMemberName);
            //            modelPrefabProperty.objectReferenceValue = selection;
            //            serializedObject.ApplyModifiedProperties();

            //            AssetDatabase.SaveAssets();

            //            GetTable().RefreshItem(dataIndex);
            //        }
            //    }
            //);
        }

        private void OnRowVisualNameChanged(ChangeEvent<string> changeEvent)
        {
            ProductVisualSO target =
                RowEventToRowData(changeEvent, out VisualElement caller, out int dataIndex);

            if (!target)
            {
                return;
            }

            Ref<string> error = new (null);

            using var errorCleanup =
                MelcherDeferred.Defer(
                    toCapture: (target, changeEvent, error),
                    onDispose: static captures =>
                    {
                        MelcherLog.LogError(
                            tag: nameof(ProductVisualDatabaseManager),
                            message: captures.error.value,
                            context: captures.target
                        );

                        // Restore original value
                        TextField textField = (TextField)captures.changeEvent.target;
                        textField.SetValueWithoutNotify(captures.changeEvent.previousValue);
                    }
                );

            char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
            if (string.IsNullOrWhiteSpace(changeEvent.newValue) ||
                changeEvent.newValue.IndexOfAny(invalidFileNameChars) >= 0)
            {
                error.value = "Invalid visual prefab name. Must not be blank or contain any of: " + invalidFileNameChars;
                return;
            }

            AssetDatabase.SaveAssetIfDirty(target);
            string contentPath = AssetDatabase.GetAssetPath(target.contentAddress.editorOnlyCachedAsset);
            error.value = AssetDatabase.RenameAsset(contentPath, changeEvent.newValue);

            if (!string.IsNullOrWhiteSpace(error.value))
            {
                return;
            }

            // The address path should match the asset's name.
            target.contentAddress.editorOnlyCachedAsset.UpdateAddressableGroup();

            if (target.info.thumbnail)
            {
                // Try to rename the thumbnail to match the new name,
                // but only if the thumbnail already matches the
                // prefab's file name.

                string thumbnailPath = AssetDatabase.GetAssetPath(target.info.thumbnail);

                string thumbnailFileName = System.IO.Path.GetFileNameWithoutExtension(thumbnailPath);
                string assetFileName = System.IO.Path.GetFileNameWithoutExtension(contentPath);

                if (string.Equals(thumbnailFileName, assetFileName, StringComparison.OrdinalIgnoreCase))
                {
                    string newThumbnailPath =
                        System.IO.Path.ChangeExtension(changeEvent.newValue, System.IO.Path.GetExtension(thumbnailPath));

                    error.value = AssetDatabase.RenameAsset(thumbnailPath, newThumbnailPath);

                    if (!string.IsNullOrWhiteSpace(error.value))
                    {
                        return;
                    }
                }
            }

            // Rename the visual file.
            string visualPath = AssetDatabase.GetAssetPath(target);
            string visualName = changeEvent.newValue + "^";
            error.value = AssetDatabase.RenameAsset(visualPath, visualName);

            if (!string.IsNullOrWhiteSpace(error.value))
            {
                return;
            }

            errorCleanup.Dismiss();

            MelcherLog.Log("Renamed: " + changeEvent.previousValue + " => " + changeEvent.newValue);

            AssetDatabase.SaveAssets();

            RefreshSearch();
        }

        private void OnRowIdChanged(ChangeEvent<string> changeEvent)
        {
            ProductVisualSO target =
                RowEventToRowData(changeEvent, out VisualElement caller, out int dataIndex);

            if (target)
            {
                ProductVisualContent content =
                    target.contentAddress.editorOnlyCachedAsset;

                string error = null;
                object context = this;

                // Force a unique ID. Allow the ID to be empty
                if (!string.IsNullOrEmpty(changeEvent.newValue) &&
                    !IsProductVisualIdUnique(changeEvent.newValue, target, out ProductVisualSO collidesWith))
                {
                    error = "ID collides with " + collidesWith.name;
                    context = collidesWith;
                }
                else
                {
                    using SerializedObject serializedObject = new SerializedObject(target.info);
                    using SerializedProperty idProperty = serializedObject.FindProperty(ProductVisualInfoSO.PropertyName.Id);
                    idProperty.stringValue = changeEvent.newValue;
                    serializedObject.ApplyModifiedProperties();

                    RefreshSearch();
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    MelcherLog.LogError(
                        tag: nameof(ProductVisualDatabaseManager),
                        message: error,
                        context: target
                    );

                    // Restore original value
                    TextField textField = (TextField)changeEvent.target;
                    textField.SetValueWithoutNotify(changeEvent.previousValue);
                }

                AssetDatabase.SaveAssets();
            }
        }

        private bool IsProductVisualIdUnique(string id, ProductVisualSO toIgnore, out ProductVisualSO collidesWith)
        {
            foreach (ProductVisualSO entry in m_Database.sourceEntries)
            {
                if (!entry || entry == toIgnore)
                {
                    continue;
                }

                if (id.Equals(entry.info.id, StringComparison.OrdinalIgnoreCase))
                {
                    collidesWith = entry;
                    return false;
                }
            }

            collidesWith = null;
            return true;
        }

        //private void OnRowModelPrefabChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        //{
        //    ProductVisual target = RowEventToRowData(changeEvent, out VisualElement caller, out int dataIndex);
        //    if (target)
        //    {
        //        MelcherLog.Log(
        //            message: "ChangeRowPrefab to " + changeEvent.newValue,
        //            context: changeEvent.newValue
        //        );

        //        {
        //            using SerializedObject serializedObject = new SerializedObject(target);
        //            using SerializedProperty modelPrefabProperty = serializedObject.FindProperty(ProductVisual.PropertyName.ProductModelPrefab);
        //            modelPrefabProperty.objectReferenceValue = changeEvent.newValue;
        //            serializedObject.ApplyModifiedProperties();
        //        }

        //        // This is necessary to update the prefab stage's values
        //        AssetDatabase.SaveAssets();

        //        RefreshSearch();
        //    }
        //}

        private void OnRowVisualClicked(ClickEvent clickEvent)
        {
            ProductVisualSO target = RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);
            if (target)
            {
                MelcherEditorUtility.PingObject(target);
            }
        }

        private void OnRowVisualChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            MelcherLog.LogWarningEditorOnly($"The {nameof(ProductVisualContent)} field is read-only. It's shown so the asset can be pinged and opened via double click. If you must change it, access the asset directly by clicking here.", m_Database);

            // Revert the field's value
            (changeEvent.currentTarget as ObjectField).SetValueWithoutNotify(changeEvent.previousValue);
        }

        private void OnRowUnderlyingAssetClicked(ClickEvent clickEvent)
        {
            ProductVisualSO target = RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);
            if (target)
            {
                UnityEngine.Object asset =
                    target.contentAddress.editorOnlyCachedAsset.underlyingAsset;

                MelcherEditorUtility.PingObject(asset);
            }
        }

        private void OnRowVisualContentPrefabClicked(ClickEvent clickEvent)
        {
            ProductVisualSO target = RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);
            if (target)
            {
                UnityEngine.Object asset =
                    target.contentAddress.editorOnlyCachedAsset;

                MelcherEditorUtility.PingObject(asset);
            }
        }

        private void OnRowLightweightDataClicked(ClickEvent clickEvent)
        {
            ProductVisualSO target = RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);
            if (target)
            {
                UnityEngine.Object asset =
                    target.info;

                MelcherEditorUtility.PingObject(asset);
            }
        }

        private void OnRowApproveModelPrefabClicked(ClickEvent clickEvent)
        {
            ProductVisualSO target =
                RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);

            if (target)
            {
                ApproveModel(target);

                AssetDatabase.SaveAssets();

                GetTable().RefreshItem(dataIndex);
            }
        }

        private void ApproveModel(ProductVisualSO target)
        {
            ProductVisualContent productContent = target.contentAddress.editorOnlyCachedAsset;

            using SerializedObject serializedObject = new SerializedObject(productContent);
            using SerializedProperty recordedModelHashProperty = serializedObject.FindProperty(ProductVisualContent.PropertyName.ApprovedHash);
            recordedModelHashProperty.hash128Value = productContent.currentHash;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnRowAnchorChanged(ChangeEvent<Enum> changeEvent)
        {
            ProductVisualSO target =
                RowEventToRowData(changeEvent, out VisualElement caller, out int dataIndex);

            if (target)
            {
                ProductVisualInfoSO productVisualInfo = target.info;

                {
                    using SerializedObject serializedObject = new SerializedObject(productVisualInfo);
                    using SerializedProperty anchorProperty = serializedObject.FindProperty(ProductVisualInfoSO.PropertyName.Anchor);
                    anchorProperty.enumValueIndex = Convert.ToInt32(changeEvent.newValue);
                    serializedObject.ApplyModifiedProperties();
                }

                AssetDatabase.SaveAssets();

                GetTable().RefreshItem(dataIndex);
            }
        }

        private void OnRowThumbnailClicked(ClickEvent clickEvent)
        {
            ProductVisualSO target =
                RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);

            if (target && target.info.thumbnail)
            {
                MelcherEditorUtility.OpenPropertyEditor(target.info.thumbnail);
            }
        }


        /// <summary>
        /// Called when the user clicks a "Render Thumbnail" button on a row.
        /// This function renders a screenshot of the product.
        /// </summary>
        private void OnRowRenderThumbnailClicked(ClickEvent clickEvent)
        {
            if (ProductVisualThumbnailCreator.PromptLoadScreenshotScene())
            {
                ProductVisualSO target =
                    RowEventToRowData(clickEvent, out VisualElement caller, out int dataIndex);

                ProductVisualThumbnailCreator.RenderScreenshot(
                    target.info,
                    target.contentAddress.editorOnlyCachedAsset,
                    forceRender: false
                ).OnDestroy(() => GetTable().RefreshItem(dataIndex));
            }
        }

        private void CreateAndAddOccupantProductVisualPrefab()
        {
            CreateAndAddModelProductVisualPrefab(
                "Product",
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeProductModelsFolder,
                static (GameObject modelToUse, ProductVisualContent newVisual, SerializedObject serializedObject) =>
                {
                    using SerializedProperty modelPrefabProperty =
                        serializedObject.FindProperty(
                            ProductVisualContent.PropertyName.ProductModelPrefab
                        );
                    modelPrefabProperty.objectReferenceValue = modelToUse;

                    // Ensure the model is tagged with "Product" so it can
                    // be hidden when necessary (e.g., CAD screenshots)
                    newVisual.AssignProductGameObjectLayer();
                }
            );
        }

        private void CreateAndAddMultipleOccupantProductVisualPrefab()
        {
            CreateAndAddMultipleModelProductVisualPrefab(
                "Product",
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeProductModelsFolder,
                static (GameObject modelToUse, ProductVisualContent newVisual, SerializedObject serializedObject) =>
                {
                    using SerializedProperty modelPrefabProperty =
                        serializedObject.FindProperty(
                            ProductVisualContent.PropertyName.ProductModelPrefab
                        );
                    modelPrefabProperty.objectReferenceValue = modelToUse;

                    // Ensure the model is tagged with "Product" so it can
                    // be hidden when necessary (e.g., CAD screenshots)
                    newVisual.AssignProductGameObjectLayer();
                }
            );
        }

        private void CreateAndAddSectionProductVisualPrefab()
        {
            CreateAndAddModelProductVisualPrefab(
                "Cabinet Section",
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeCabinetSectionModelsFolder,
                static (GameObject modelToUse, ProductVisualContent newVisual, SerializedObject serializedObject) =>
                {
                    using SerializedProperty modelPrefabProperty =
                        serializedObject.FindProperty(
                            ProductVisualContent.PropertyName.SectionModelPrefab
                        );
                    modelPrefabProperty.objectReferenceValue = modelToUse;
                }
            );
        }

        private void CreateAndAddMultipleSectionProductVisualPrefab()
        {
            CreateAndAddMultipleModelProductVisualPrefab(
                "Cabinet Section",
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeCabinetSectionModelsFolder,
                static (GameObject modelToUse, ProductVisualContent newVisual, SerializedObject serializedObject) =>
                {
                    using SerializedProperty modelPrefabProperty =
                        serializedObject.FindProperty(
                            ProductVisualContent.PropertyName.SectionModelPrefab
                        );
                    modelPrefabProperty.objectReferenceValue = modelToUse;
                }
            );
        }

        private void CreateAndAddFinishProductVisualPrefab()
        {
            GetObjectAssetUsingPicker(
                "Select Material",
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeFinishesFolder,
                $"t:{nameof(Material)}",
                HandleSelection,
                assetFilter: (args) =>
                {
                    // Treat double underscores as templates. We don't want to
                    // see these as valid options.
                    return !args.assetPath.Contains("__");
                }
            );

            void HandleSelection(bool madeSelection, UnityEngine.Object selection)
            {
                if (!madeSelection || !selection)
                {
                    return;
                }

                CreateAndAddProductVisualPrefab("Finish", selection, modelProductSetup: null);
            }
        }

        private void CreateAndAddMultipleFinishProductVisualPrefab()
        {
            GetMultipleObjectAssetUsingPicker(
                "Select Materials",
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeFinishesFolder,
                $"t:{nameof(Material)}",
                HandleSelection,
                assetFilter: (args) =>
                {
                    // Treat double underscores as templates. We don't want to
                    // see these as valid options.
                    return !args.assetPath.Contains("__");
                }
            );

            bool HandleSelection(UnityEngine.Object selection)
            {
                if (selection is not Material)
                {
                    return false;
                }

                CreateAndAddProductVisualPrefabWithoutSaving("Finish", selection, modelProductSetup: null);

                return true;
            }
        }

        private const string CABINET_SECTION_VISUAL_NAME_PART = "_CabinetSection_";

        private void AddMultipleExistingProductProductVisualPrefab()
        {
            AddMultipleExistingProductVisualPrefab(
                "Select Product Visuals",
                assetPath => !System.IO.Path.GetFileName(assetPath).Contains(CABINET_SECTION_VISUAL_NAME_PART)
            );
        }

        private void AddMultipleExistingCabinetProductVisualPrefab()
        {
            AddMultipleExistingProductVisualPrefab(
                "Select Cabinet Visuals",
                assetPath => System.IO.Path.GetFileName(assetPath).Contains(CABINET_SECTION_VISUAL_NAME_PART)
            );
        }

        private void AddMultipleExistingProductVisualPrefab(string pickerTitle, Func<string, bool> assetPathFilter)
        {
            // Prompt the user for a model
            GetMultipleObjectAssetUsingPicker(
                pickerTitle,
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeProductVisualsFolder,
                $"t:{nameof(ProductVisualSO)}",
                HandleSelection,
                assetFilter: null,
                assetPathFilter: assetPathFilter
            );

            bool HandleSelection(UnityEngine.Object selection)
            {
                // If nothing was selected, abort adding the entry
                if (!selection)
                {
                    return false;
                }

                if (!(selection is ProductVisualSO productVisual))
                {
                    MelcherLog.LogError($"Selection is not a {nameof(ProductVisualSO)}.", selection);
                    return false;
                }

                {
                    using SerializedObject serializedObject =
                        new SerializedObject(m_Database);

                    using SerializedProperty entriesProperty =
                        serializedObject.FindProperty(
                            ProductVisualDatabaseSO.SourceEntriesMemberName
                        );

                    int arraySize = entriesProperty.arraySize;
                    entriesProperty.InsertArrayElementAtIndex(arraySize);

                    using SerializedProperty entryProperty =
                        entriesProperty.GetArrayElementAtIndex(arraySize);

                    entryProperty.objectReferenceValue = productVisual;

                    serializedObject.ApplyModifiedProperties();
                }

                return true;
            }
        }

        private void AddExistingProductVisualPrefab()
        {
            // Prompt the user for a model
            GetObjectAssetUsingPicker(
                "Select Visual",
                "Assets/" + ProductEditorOptionsSO.Get().dataRelativeProductVisualsFolder,
                $"t:{nameof(ProductVisualSO)}",
                HandleSelection,
                assetFilter: null
            );

            void HandleSelection(bool madeSelection, UnityEngine.Object selection)
            {
                // If nothing was selected, abort adding the entry
                if (!madeSelection || !selection)
                {
                    return;
                }

                if (!(selection is ProductVisualSO productVisual))
                {
                    MelcherLog.LogError($"Selection is not a {nameof(ProductVisualSO)}.", selection);
                    return;
                }

                {
                    using SerializedObject serializedObject =
                        new SerializedObject(m_Database);

                    using SerializedProperty entriesProperty =
                        serializedObject.FindProperty(
                            ProductVisualDatabaseSO.SourceEntriesMemberName
                        );

                    int arraySize = entriesProperty.arraySize;
                    entriesProperty.InsertArrayElementAtIndex(arraySize);

                    using SerializedProperty entryProperty =
                        entriesProperty.GetArrayElementAtIndex(arraySize);

                    entryProperty.objectReferenceValue = productVisual;

                    serializedObject.ApplyModifiedProperties();
                }

                AssetDatabase.SaveAssets();

                // Clear the search, scroll to, and select the new item
                GetSearchField().SetValueWithoutNotify("");
                RefreshSearch();

                int selectIndex = m_Database.sourceEntries.Count;
                ListView listView = GetTable();
                listView.ScrollToItem(selectIndex);
                listView.SetSelection(selectIndex);
            }
        }

        /// <summary>
        ///     Remake the status filter menu in its entirety. This is
        ///     necessary because there is no way to update menu item's status
        ///     (checked/unchecked) after they are created.
        /// </summary>
        private void RebuildSearchStatusFilterMenu()
        {
            ToolbarMenu searchFilterMenu = rootVisualElement.Q<ToolbarMenu>("SearchFilterMenu");

            bool isDefaultFilter = m_SearchStatusFilter == (ProductVisualEditorSetupStatus)~0;

            TextElement toolbarLabel = searchFilterMenu.Q<TextElement>();
            toolbarLabel.style.unityFontStyleAndWeight =
                isDefaultFilter ?
                    FontStyle.Normal :
                    FontStyle.Bold;

            searchFilterMenu.menu.MenuItems().Clear();
            searchFilterMenu.menu.AppendAction("Reset", OnSearchStatusFilterClicked_Reset);
            searchFilterMenu.menu.AppendSeparator();
            searchFilterMenu.menu.AppendAction("Include good", OnSearchStatusFilterClicked_Good, GetSearchFilterItemStatus(ProductVisualEditorSetupStatus.Good));
            searchFilterMenu.menu.AppendAction("Include changed", OnSearchStatusFilterClicked_Changed, GetSearchFilterItemStatus(ProductVisualEditorSetupStatus.HashHasChanged));
            searchFilterMenu.menu.AppendAction("Include incomplete", OnSearchStatusFilterClicked_Incomplete, GetSearchFilterItemStatus(ProductVisualEditorSetupStatus.MissingFields));
        }

        private DropdownMenuAction.Status GetSearchFilterItemStatus(ProductVisualEditorSetupStatus mask)
        {
            return m_SearchStatusFilter.HasFlag(mask) ?
                DropdownMenuAction.Status.Checked :
                DropdownMenuAction.Status.Normal;
        }

        private void OnSearchStatusFilterClicked_Reset(DropdownMenuAction action)
        {
            m_SearchStatusFilter = (ProductVisualEditorSetupStatus)~0;
            RefreshSearch();
            RebuildSearchStatusFilterMenu();
        }

        private void OnSearchStatusFilterClicked_Good(DropdownMenuAction action)
        {
            m_SearchStatusFilter ^= ProductVisualEditorSetupStatus.Good;
            RefreshSearch();
            RebuildSearchStatusFilterMenu();
        }

        private void OnSearchStatusFilterClicked_Changed(DropdownMenuAction action)
        {
            m_SearchStatusFilter ^= ProductVisualEditorSetupStatus.HashHasChanged;
            RefreshSearch();
            RebuildSearchStatusFilterMenu();
        }

        private void OnSearchStatusFilterClicked_Incomplete(DropdownMenuAction action)
        {
            m_SearchStatusFilter ^= ProductVisualEditorSetupStatus.MissingFields;
            RefreshSearch();
            RebuildSearchStatusFilterMenu();
        }

        private void DeleteOrphanedThumbnails(DropdownMenuAction action)
        {
            int dialogResult =
                EditorUtility.DisplayDialogComplex(
                    title: "Delete Orphaned Product Thumbnails",
                    message: "Find all sprites in the Products Visual " +
                        "folder that are not referenced by at least one " +
                        "Product Visual.\n" +
                        "\n" +
                        "Optionally, you may also choose to permanently " +
                        "delete these thumbnails. (By default, those that " +
                        "found are only logged to the console.)",
                    ok: "Find Only",
                    alt: "Find and Permanently Delete",
                    cancel: "Cancel"
                );

            if (dialogResult == MelcherEditorConst.DialogResult.Cancel)
            {
                return;
            }

            bool deleteAssets =
                dialogResult == MelcherEditorConst.DialogResult.Alt;

            string[] searchFolders =
                new[]
                {
                    "Assets/AssetsMelcher/ProductVisuals/"
                };

            List<Texture> unusedThumbnails =
                AssetDatabase.FindAssets("t:texture", searchFolders)
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(assetPath => AssetDatabase.LoadAssetAtPath<Texture>(assetPath))
                    .ToList();

            string[] prefabGuids =
                AssetDatabase.FindAssets("t:prefab ^", searchFolders);

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabAssetPath =
                    AssetDatabase.GUIDToAssetPath(prefabGuid);

                if (string.IsNullOrEmpty(prefabAssetPath))
                {
                    continue;
                }

                ProductVisualSO productVisual =
                    AssetDatabase.LoadAssetAtPath<ProductVisualSO>(prefabAssetPath);

                if (!productVisual)
                {
                    continue;
                }

                if (productVisual.info.thumbnail)
                {
                    unusedThumbnails.Remove(productVisual.info.thumbnail.texture);
                }
            }

            if (unusedThumbnails.Count <= 0)
            {
                MelcherLog.Log("No orphaned thumbnails found.");
                return;
            }

            if (!deleteAssets)
            {
                MelcherLog.Log("Would remove these orphaned thumbnails.");
            }

            foreach (Texture unusedThumbnail in unusedThumbnails)
            {
                string thumbnailAssetPath =
                    AssetDatabase.GetAssetPath(unusedThumbnail);

                MelcherLog.Log(thumbnailAssetPath, unusedThumbnail);

                if (deleteAssets)
                {
                    AssetDatabase.DeleteAsset(thumbnailAssetPath);
                }
            }
        }

        private void DeleteProductsWithoutModels(DropdownMenuAction action)
        {
            int dialogResult =
                EditorUtility.DisplayDialogComplex(
                    title: "Delete Product Visuals",
                    message: "Find all Product Visuals (including those " +
                        "not included in the Product Visual Database) that " +
                        "have no underlying assets. These visuals were " +
                        "likely created with models that have since been " +
                        "or renamed in the project.\n" +
                        "\n" +
                        "If you are sure, you may also choose to permanently " +
                        "delete these visuals. (By default, the found visuals " +
                        "are only logged to the console.)",
                    ok: "Find Only",
                    alt: "Find and Permanently Delete",
                    cancel: "Cancel"
                );

            if (dialogResult == MelcherEditorConst.DialogResult.Cancel)
            {
                return;
            }

            bool deleteAssets =
                dialogResult == MelcherEditorConst.DialogResult.Alt;

            if (deleteAssets)
            {
                bool confirmed =
                    EditorUtility.DisplayDialog(
                        title: "Confirm Deletion",
                        message: "Are you sure you want to permanently delete " +
                            "all product visuals that have no underlying " +
                            "assets?",
                        ok: "Find and Permanently Delete",
                        cancel: "Cancel"
                    );

                if (!confirmed)
                {
                    return;
                }
            }

            string[] searchFolders =
                new[]
                {
                    "Assets/AssetsMelcher/ProductVisuals/"
                };

            string[] prefabGuids =
                AssetDatabase.FindAssets("t:prefab ^", searchFolders);

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabAssetPath =
                    AssetDatabase.GUIDToAssetPath(prefabGuid);

                if (string.IsNullOrEmpty(prefabAssetPath))
                {
                    continue;
                }

                ProductVisualSO productVisual =
                    AssetDatabase.LoadAssetAtPath<ProductVisualSO>(prefabAssetPath);

                if (!productVisual)
                {
                    continue;
                }

                if (productVisual.contentAddress.editorOnlyCachedAsset.underlyingAsset)
                {
                    // Has an asset, all is good.
                    continue;
                }

                if (productVisual.info.runtimePrefabCategory == ProductCategorySpecific.Unknown)
                {
                    // This is a shelf. It's allowed to not have a model.
                    MelcherLog.Assert(
                        productVisual.info.id == "Shelf:Top" ||
                        productVisual.info.id == "Accessory_Stoll_StainlessSteelBurnerPlungeKit"
                    );
                    continue;
                }

                if (productVisual.info.runtimePrefabCategory == ProductCategorySpecific.CabinetSection &&
                    productVisual.info.sectionModelType == CabinetModelType.Procedural)
                {
                    // Procedural cabinets don't need a model.
                    continue;
                }

                if (deleteAssets)
                {
                    if (productVisual.info.thumbnail)
                    {
                        // Also remove its thumbnail.
                        string thumbnailAssetPath =
                            AssetDatabase.GetAssetPath(productVisual.info.thumbnail);

                        MelcherLog.Log("Deleting " + productVisual.info.thumbnail.name, productVisual);
                        AssetDatabase.DeleteAsset(thumbnailAssetPath);
                    }

                    MelcherLog.Log("Deleting " + productVisual.name, productVisual);
                    AssetDatabase.DeleteAsset(prefabAssetPath);
                }
                else
                {
                    MelcherLog.Log("Missing underlying asset: " + productVisual.name, productVisual);
                }
            }
        }

        protected void DuplicateSelectedVisual(DropdownMenuAction action)
        {
            if (!m_LastVisualPrefabSelected)
            {
                return;
            }

            // Ensure the Addressables settings asset exists because it is
            // needed to mark the duplicated assets as addressable.

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.Settings;

            if (!settings)
            {
                throw new InvalidOperationException("The default Addressables settings asset must be created.");
            }

            AddressableAssetGroup addressableGroup = settings.DefaultGroup;

            string originalVisualPath =
                AssetDatabase.GetAssetPath(m_LastVisualPrefabSelected);

            string duplicateVisualPath =
                AssetDatabase.GenerateUniqueAssetPath(originalVisualPath);

            //
            // Duplicate the selected product visual info
            //

            ProductVisualInfoSO duplicateInfo =
                Instantiate(m_LastVisualPrefabSelected.info);

            string duplicateInfoPath = duplicateVisualPath.Replace("^", "");

            // Assign a unique ID to the duplicated visual
            string fileName = System.IO.Path.GetFileNameWithoutExtension(duplicateInfoPath);

            string id = fileName;

            const string ProductVisualAssetPrefix = "ProductVisual_";
            if (id.StartsWith(ProductVisualAssetPrefix))
            {
                id = id.Substring(ProductVisualAssetPrefix.Length);
            }
            duplicateInfo.EditorOnlySetId(id);

            AssetDatabase.CreateAsset(duplicateInfo, duplicateInfoPath);

            //
            // Duplicate the selected product visual content
            //

            string duplicateContentPath =
                System.IO.Path.ChangeExtension(duplicateInfoPath, ".prefab");

            GameObject duplicateContentAsset =
                PrefabUtility.SaveAsPrefabAsset(
                    m_LastVisualPrefabSelected.contentAddress.editorOnlyCachedAsset.gameObject,
                    duplicateContentPath
                );

            ProductVisualContent duplicateContent =
                duplicateContentAsset.GetComponent<ProductVisualContent>();

            if (!duplicateContent)
            {
                MelcherLog.LogError($"Failed to retrieve the {MelcherFormat.ConsoleTypeName<ProductVisualContent>()} from the duplicated product visual.", duplicateContentAsset);
                return;
            }

            // Mark the new content prefab as addressable

            string duplicateContentGuid = AssetDatabase.AssetPathToGUID(duplicateContentPath);

            AddressableAssetEntry duplicateContentEntry =
                settings.CreateOrMoveEntry(duplicateContentGuid, addressableGroup);

            //
            // Duplicate the selected product visual asset
            //

            ProductVisualSO duplicateVisual =
                Instantiate(m_LastVisualPrefabSelected);

            duplicateVisual.EditorOnlySetInfo(duplicateInfo);
            duplicateVisual.EditorOnlySetContentAddress(duplicateContent);

            AssetDatabase.CreateAsset(duplicateVisual, duplicateVisualPath);

            //if (!string.IsNullOrEmpty(duplicateVisual.brandCatalogName))
            //{
            //    duplicateVisual.SetBrandCatalogName("ChallengerByEmpire");
            //}

            //
            // Duplicate the thumbnail (if present)
            //

            if (m_LastVisualPrefabSelected.info.thumbnail)
            {
                string originalThumbnailPath =
                    AssetDatabase.GetAssetPath(m_LastVisualPrefabSelected.info.thumbnail);

                string duplicateThumbnailPath =
                    System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(duplicateVisualPath),
                        fileName + ".png"
                    );

                MelcherLog.Log(
                    $"Duplicating thumbnail.\n" +
                    $"Original: {MelcherFormat.ConsoleString(originalThumbnailPath)}\n" +
                    $"Duplicate:{MelcherFormat.ConsoleString(duplicateThumbnailPath)}"
                );

                if (AssetDatabase.CopyAsset(originalThumbnailPath, duplicateThumbnailPath))
                {
                    duplicateVisual.info.EditorOnlySetThumbnail(
                        AssetDatabase.LoadAssetAtPath<Sprite>(duplicateThumbnailPath)
                    );
                }
                else
                {
                    MelcherLog.LogError(
                        $"Could not duplicate the visual's thumbnail to " +
                        $"{MelcherFormat.ConsoleString(duplicateThumbnailPath)}",
                        context: m_LastVisualPrefabSelected.info.thumbnail
                    );
                }
            }

            EditorUtility.SetDirty(duplicateContent);

            // Create the product visual asset now that it is referencing its
            // info and content.
            AssetDatabase.CreateAsset(duplicateVisual, duplicateVisualPath);

            m_Database.mutableSourceEntries.Add(duplicateVisual);

            if (m_DisplayedData == m_FilteredData)
            {
                // Show the duplicated item in the list, even if the filter
                // settings wouldn't normally allow it.
                m_FilteredData.Add(duplicateVisual);
                GetTable().RefreshItems();
            }


            MelcherLog.Log($"Made duplicate: {MelcherFormat.ConsoleString(duplicateVisualPath)}", duplicateContent);
        }

        /// <summary>
        ///     Removes all the product visuals that are currently displayed
        ///     in the filter view.
        /// </summary>
        /// <param name="action"></param>
        protected void RemoveDisplayedVisuals(DropdownMenuAction action)
        {
            if (m_DisplayedData != m_FilteredData)
            {
                MelcherLog.LogWarning("A filter/search is required (to avoid erasing everything).");
                return;
            }

            int numberToRemove = m_DisplayedData.Count;

            if (numberToRemove <= 0)
            {
                return;
            }

            bool willRemove =
                EditorUtility.DisplayDialog(
                    title: "Remove Displayed Product Visuals",
                    message: $"Remove {numberToRemove} visuals?\n" +
                        "\n" +
                        "When removed, the prefab will remain in its current " +
                        "folder and can be re-added to the database using the " +
                        "Add Existing button in the Database Manager.",
                    ok: "Remove",
                    cancel: "Cancel"
                );

            if (!willRemove)
            {
                return;
            }

            using SerializedObject serializedObject =
                new SerializedObject(m_Database);

            using SerializedProperty entriesProperty =
                serializedObject.FindProperty(
                    ProductVisualDatabaseSO.SourceEntriesMemberName
                );

            int numberRemoved = 0;

            List<int> visualIndicesToRemove = new List<int>();
            foreach (ProductVisualSO visualToRemove in m_FilteredData)
            {
                if (!visualToRemove)
                {
                    continue;
                }

                int productVisualSourceIndex =
                    MelcherCommon.IndexOf(m_Database.sourceEntries, visualToRemove);

                if (productVisualSourceIndex < 0)
                {
                    MelcherLog.LogError($"Product Visual not found in database: {visualToRemove.name}", visualToRemove);
                    continue;
                }

                visualIndicesToRemove.Add(productVisualSourceIndex);
            }

            visualIndicesToRemove.Sort();
            visualIndicesToRemove.Reverse();

            // Remove entries in the reverse order they appear to ensure the
            // earlier indices are not invalidated by each removal.
            foreach (int i in visualIndicesToRemove)
            {
                entriesProperty.DeleteArrayElementAtIndex(i);
                ++numberRemoved;
            }

            serializedObject.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();

            MelcherLog.Log($"Removed {numberRemoved} from {m_Database.name}", m_Database);

            RefreshSearch();

            // Refresh the database window.
            ListView listView = GetTable();
            listView.RefreshItems();
        }

        protected void ShowDatabaseStates(DropdownMenuAction action)
        {
            ProductVisualDatabaseStatesManager.ShowDatabase(m_Database);
        }

        protected void ShowDatabaseSets(DropdownMenuAction action)
        {
            ProductVisualDatabaseSetsManager.ShowDatabase(m_Database);
        }

        protected void RemoveSelectedVisual(DropdownMenuAction action)
        {
            if (!m_LastVisualPrefabSelected)
            {
                return;
            }

            bool willRemove =
                EditorUtility.DisplayDialog(
                    title: "Remove Selected Product Visual",
                    message: $"Remove {m_LastVisualPrefabSelected.name}?\n" +
                        "\n" +
                        "When removed, the prefab will remain in its current " +
                        "folder and can be re-added to the database using the " +
                        "Add Existing button in the Database Manager.",
                    ok: "Remove",
                    cancel: "Cancel"
                );

            if (willRemove)
            {
                int productVisualSourceIndex =
                    MelcherCommon.IndexOf(m_Database.sourceEntries, m_LastVisualPrefabSelected);

                int productVisualDisplayIndex =
                    MelcherCommon.IndexOf(m_DisplayedData, m_LastVisualPrefabSelected);

                if (productVisualSourceIndex < 0)
                {
                    MelcherLog.LogError($"Product Visual not found in database: {m_LastVisualPrefabSelected.name}", m_LastVisualPrefabSelected);
                    return;
                }

                {
                    using SerializedObject serializedObject =
                        new SerializedObject(m_Database);

                    using SerializedProperty entriesProperty =
                        serializedObject.FindProperty(
                            ProductVisualDatabaseSO.SourceEntriesMemberName
                        );

                    entriesProperty.DeleteArrayElementAtIndex(productVisualSourceIndex);

                    serializedObject.ApplyModifiedProperties();
                }

                AssetDatabase.SaveAssets();

                MelcherLog.Log($"Removed {m_LastVisualPrefabSelected.name} from {m_Database.name}", m_LastVisualPrefabSelected);

                RefreshSearch();

                // Scroll to the next item in the list
                ListView listView = GetTable();
                listView.RefreshItems();
                if (productVisualDisplayIndex >= listView.itemsSource.Count)
                {
                    productVisualDisplayIndex = listView.itemsSource.Count - 1;
                }
                if (productVisualDisplayIndex >= 0)
                {
                    listView.ScrollToItem(productVisualDisplayIndex);
                    listView.SetSelection(productVisualDisplayIndex);
                }
            }
        }

        protected void StripVariantWordFromVisualNames(DropdownMenuAction action)
        {
            const string ToStrip = " Variant";

            foreach (ProductVisualSO visual in m_DisplayedData)
            {
                if (!visual)
                {
                    continue;
                }

                string visualName = visual.name;
                if (visualName.EndsWith(ToStrip))
                {
                    visual.name = StripSuffix(visualName);
                }

                string assetPath = AssetDatabase.GetAssetPath(visual);
                string assetFileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (assetFileName.EndsWith(ToStrip))
                {
                    assetFileName = StripSuffix(assetFileName);
                    AssetDatabase.RenameAsset(assetPath, assetFileName);
                }
            }

            ListView listView = rootVisualElement.Q<ListView>("Table");
            listView.RefreshItems();

            AssetDatabase.SaveAssets();

            static string StripSuffix(string text)
            {
                return text.Substring(0, text.Length - ToStrip.Length);
            }
        }

        protected void FixCabinetsWithNoOccupiableHeight(DropdownMenuAction action)
        {
            foreach (ProductVisualSO visual in m_Database.sourceEntries)
            {
                if (!visual)
                {
                    continue;
                }

                if (visual.info.runtimePrefabCategory != ProductCategorySpecific.CabinetSection ||
                    visual.info.sectionModelType != CabinetModelType.Authored)
                {
                    continue;
                }

                float cabinetHeight =
                    visual.contentAddress.editorOnlyCachedAsset.modelCutOutExtentMax.position.y -
                    visual.contentAddress.editorOnlyCachedAsset.modelCutOutExtentMin.position.y;

                if (visual.info.sectionOccupiableCuboidPaddingInMeters.bottom >= cabinetHeight - 1.0f * Units.Inches2Meters)
                {
                    MelcherLog.Log("Cabinet has no occupiable height: " + visual, visual);
                    CuboidPadding padding = visual.info.sectionOccupiableCuboidPaddingInInches;
                    padding.bottom = (cabinetHeight - (2.0f * Units.Inches2Meters)) * Units.Meters2Inches;
                    visual.info.EditorOnlySetSectionOccupiableCuboidPaddingInInches(padding);
                    EditorUtility.SetDirty(visual);
                }
            }

            AssetDatabase.SaveAssets();
        }

        protected delegate void SetupNewProductVisualHandler(GameObject modelToUse, ProductVisualContent newVisual, SerializedObject newVisualSerializedObject);

        protected void CreateAndAddModelProductVisualPrefab(string entryTypeName, string productRelativeModelFolderPath, SetupNewProductVisualHandler doSetup)
        {
            // Prompt the user for a model
            GetObjectAssetUsingPicker(
                "Select Models",
                productRelativeModelFolderPath,
                "t:model",
                HandleSelection,
                assetFilter: null
            );

            void HandleSelection(bool madeSelection, UnityEngine.Object selection)
            {
                // If no model was selected, abort creating the new entry
                if (!madeSelection || !selection)
                {
                    return;
                }

                if (selection is not GameObject selectionAsGameObject)
                {
                    return;
                }

                CreateAndAddProductVisualPrefab(entryTypeName, selection, doSetup);
            }
        }

        protected void CreateAndAddMultipleModelProductVisualPrefab(string entryTypeName, string productRelativeModelFolderPath, SetupNewProductVisualHandler doSetup)
        {
            // Prompt the user for a model
            GetMultipleObjectAssetUsingPicker(
                "Select Models",
                productRelativeModelFolderPath,
                "t:model",
                HandleSelection,
                assetFilter: null
            );

            bool HandleSelection(UnityEngine.Object selection)
            {
                // If no model was selected, abort creating the new entry
                if (!selection)
                {
                    return false;
                }

                if (selection is not GameObject selectionAsGameObject)
                {
                    return false;
                }

                CreateAndAddProductVisualPrefabWithoutSaving(entryTypeName, selection, doSetup);

                return true;
            }
        }

        protected void CreateAndAddProductVisualPrefabWithoutSaving(string entryTypeName, UnityEngine.Object selection, SetupNewProductVisualHandler modelProductSetup)
        {
            string undoName = $"Add Product Visual ({entryTypeName})";

            string outputPath = "Assets/AssetsMelcher/ProductVisuals/";

            string entryName = "ProductVisual_" + selection.name;

            //
            // Create the product visual info object.
            //

            ProductVisualInfoSO productVisualInfo =
                ScriptableObject.CreateInstance<ProductVisualInfoSO>();

            productVisualInfo.name = entryName;
            productVisualInfo.EditorOnlySetId(selection.name);

            AssetDatabase.CreateAsset(productVisualInfo, outputPath + productVisualInfo.name + ".asset");

            //
            // Create a soon-to-be prefab variant using the template
            //

            ProductVisualContent productVisualContent =
                (ProductVisualContent)PrefabUtility.InstantiatePrefab(
                    m_NewProductVisualPrefab
                );

            using var cleanUpInMemoryPrefab =
                MelcherDeferred.Defer(
                    toCapture: productVisualContent,
                    onDispose: memoryInstance => DestroyImmediate(productVisualContent.gameObject)
                );

            productVisualContent.name = entryName;

            using SerializedObject visualContentSerializedObject =
                new SerializedObject(productVisualContent);

            using SerializedProperty editorOnlyProductVisualInfoProperty =
                visualContentSerializedObject.FindProperty(
                    ProductVisualContent.PropertyName.EditorOnlyVisualInfo
                );
            editorOnlyProductVisualInfoProperty.objectReferenceValue = productVisualInfo;

            // Add the selected model to the prefab
            if (selection is GameObject selectionAsGameObject)
            {
                PrefabUtility.InstantiatePrefab(selection, productVisualContent.scaleOrigin);

                modelProductSetup?.Invoke(selectionAsGameObject, productVisualContent, visualContentSerializedObject);
            }
            else if (selection is Material)
            {
                using SerializedProperty finishMaterialProperty =
                    visualContentSerializedObject.FindProperty(ProductVisualContent.PropertyName.FinishMaterial);

                finishMaterialProperty.objectReferenceValue = selection;
            }

            visualContentSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            // Save it out as a prefab variant
            string contentPath = outputPath + productVisualContent.name + ".prefab";
            contentPath = AssetDatabase.GenerateUniqueAssetPath(contentPath);
            GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(productVisualContent.gameObject, contentPath);
            Undo.RegisterCreatedObjectUndo(newPrefab, undoName);

            string contentGuid = AssetDatabase.AssetPathToGUID(contentPath);
            AddressablesForEditor.MakeAddressableWithGroupName(contentGuid, "ProductVisuals");

            //
            // Create the ProductVisual which binds the product
            // visual's content to its info.
            //

            ProductVisualSO productVisual =
                ScriptableObject.CreateInstance<ProductVisualSO>();

            productVisual.name = entryName + "^";
            productVisual.EditorOnlySetInfo(productVisualInfo);
            productVisual.EditorOnlySetContentAddress(newPrefab.GetComponent<ProductVisualContent>());

            AssetDatabase.CreateAsset(productVisual, outputPath + productVisual.name + ".asset");

            //
            // Link the new product visual to the database.
            //

            {
                using SerializedObject serializedObject =
                    new SerializedObject(m_Database);

                using SerializedProperty entriesProperty =
                    serializedObject.FindProperty(
                        ProductVisualDatabaseSO.SourceEntriesMemberName
                    );

                int arraySize = entriesProperty.arraySize;
                entriesProperty.InsertArrayElementAtIndex(arraySize);

                using SerializedProperty entryProperty =
                    entriesProperty.GetArrayElementAtIndex(arraySize);

                entryProperty.objectReferenceValue = productVisual;

                serializedObject.ApplyModifiedProperties();
            }
        }

        protected void CreateAndAddProductVisualPrefab(string entryTypeName, UnityEngine.Object selection, SetupNewProductVisualHandler modelProductSetup)
        {
            CreateAndAddProductVisualPrefabWithoutSaving(entryTypeName, selection, modelProductSetup);

            AssetDatabase.SaveAssets();

            // Clear the search and scroll to / select the new item
            GetSearchField().SetValueWithoutNotify("");
            RefreshSearch();

            int selectIndex = m_Database.sourceEntries.Count;
            ListView listView = GetTable();
            listView.ScrollToItem(selectIndex);
            listView.SetSelection(selectIndex);
        }

        /// <summary>
        /// Batch-approve the models used by all visible product visuals. This
        /// respects the search field and the "exclude good entries" toggle.
        ///
        /// Basically, this saves a person needing to click tens or hundreds
        /// of buttons if they have a ton of models that were updated.
        /// </summary>
        protected void ApproveModelsOfFilteredResults()
        {
            foreach (ProductVisualSO visual in m_DisplayedData)
            {
                if (visual)
                {
                    ApproveModel(visual);
                }
            }

            AssetDatabase.SaveAssets();
            GetTable().RefreshItems();
        }

        /// <summary>
        /// Batch-render the thumbnail for every product visual that is
        /// currently displayed. This respects the search field and the
        /// "exclude good entries" toggle.
        ///
        /// This is convenient for re-rendering many thumbnails at once but it
        /// does take a while.
        /// </summary>
        protected void RenderThumbnailsOfFilteredResults()
        {
            Timing.RunCoroutine(RenderThumbnailsOfFilteredResultsRoutine(), Segment.EditorUpdate);
        }

        protected IEnumerator<float> RenderThumbnailsOfFilteredResultsRoutine()
        {
            if (ProductVisualThumbnailCreator.PromptLoadScreenshotScene())
            {
                using var hideProgressDiaog =
                    MelcherDeferred.Defer(
                        static () => EditorUtility.ClearProgressBar()
                    );

                for (int i = 0; i < m_DisplayedData.Count; i++)
                {
                    ProductVisualSO visual = m_DisplayedData[i];
                    if (visual)
                    {
                        string productVisualAssetPath =
                            AssetDatabase.GetAssetPath(visual);

                        bool didCancel =
                            EditorUtility.DisplayCancelableProgressBar(
                                title: "Rendering thumbnails",
                                info: productVisualAssetPath,
                                progress: (float)i / m_DisplayedData.Count
                            );

                        if (didCancel)
                        {
                            break;
                        }

                        yield return
                            Timing.WaitUntilDone(
                                ProductVisualThumbnailCreator.RenderScreenshot(
                                    visual.info,
                                    visual.contentAddress.editorOnlyCachedAsset,
                                    forceRender: false
                                )
                            );
                    }
                }

                GetTable().RefreshItems();
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherAssetField] protected VisualTreeAsset m_DocumentAsset = default;
        [SerializeField, MelcherAssetField] protected VisualTreeAsset m_TableRowAsset = default;
        [SerializeField, MelcherAssetField] protected ProductVisualContent m_NewProductVisualPrefab = default;

        private GUIContent m_StatusGood;
        private GUIContent m_StatusOrange;
        private GUIContent m_StatusRed;

        [SerializeField]
        private ProductVisualDatabaseSO m_Database = default;

        /// <summary>
        ///     Keeps track of the last entry the player selected. This
        ///     prefab's row will be highlighted so it's easier to keep track
        ///     of the last prefab that was selected/modified. This is
        ///     especially useful for finding a group of products by first
        ///     searching for the product by name, selecting it, and then
        ///     clearing the selection.
        /// </summary>
        private ProductVisualSO m_LastVisualPrefabSelected;

        /// <summary>
        /// The database's entries with the search field and "exclude good
        /// entries" filters applied.
        /// </summary>
        private List<ProductVisualSO> m_FilteredData = new ();

        /// <summary>
        /// The database entries that are actively being displayed. This is
        /// either <see cref="m_Database"/>'s <see cref="ProductVisualDatabase.sourceEntries"/>
        /// or <see cref="m_FilteredData"/>.
        /// </summary>
        private IReadOnlyList<ProductVisualSO> m_DisplayedData;

        private ProductVisualEditorSetupStatus m_SearchStatusFilter = (ProductVisualEditorSetupStatus)~0;

        // ====================================================================
        // Nested types
        // ====================================================================

        public delegate void GameObjectPickedEventHandler(bool wasSelectionmade, UnityEngine.Object selection);

    }   // class ProductVisualDatabaseManager

}   // namespace KitchenDesign

/// ============================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-13
/// ===========================================================================
