namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Displays a manufacturer list for a dealer user to whitelist specific
    /// options. Manufacturers that the dealer selects will appear as options
    /// in the designer portion of the app. Admins can override which
    /// manufacturer options are available to dealers on a per-user basis.
    /// </summary>
    public class ManufacturerWhitelistUI : Menu
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Take the users currently whitelisted manufacturers and display
        ///     it as the selection in the UI. Some manufacturers are forced to
        ///     be on (currently just the blank-name/generic group).
        /// </summary>
        public void SetSelectionFromProfile()
        {
            m_MandatoryManufacturerIds.Clear();
            m_SelectedManufacturerIds.Clear();
            m_ManufacturerListEntries.Clear();

            List<ManufacturerInfo> allManufactures = ProFitGlobals.manufacturers;
            List<int> currentWhitelistIds = ProFitGlobals.userProfile.dealerSelectedManufacturerIds;

            // Find mandatory options that are used for backsplash, bartop,
            // etc. and place into one list. (They are identified by having no
            // name.) Add the rest of the options to a different list
            for (int manufacturerIndex = 0; manufacturerIndex < allManufactures.Count; ++manufacturerIndex)
            {
                if (string.IsNullOrEmpty(allManufactures[manufacturerIndex].name))
                {
                    // If it's a blank name, it's mandatory
                    m_MandatoryManufacturerIds.Add(allManufactures[manufacturerIndex].id);
                }
            }

            // Setup the selected ID list using the current whitelist IDs.
            // Filter out the mandatory IDs
            foreach (int whitelistId in currentWhitelistIds)
            {
                if (!m_MandatoryManufacturerIds.Contains(whitelistId))
                {
                    m_SelectedManufacturerIds.Add(whitelistId);
                }
            }

            IEnumerable<ManufacturerInfo> availableManufacturers =
                allManufactures.Where(x => !string.IsNullOrEmpty(x.name));

            if (!ProFitGlobals.appRules.IncludesStollManufacturer)
            {
                // Find the manufacturer ID of Stoll as it may be filtered out from
                // the list of all manufacturers in the next step.
                if (ProFitGlobals.catalogData.FindManufacturerByName("Stoll", out ManufacturerInfo stoll))
                {
                    availableManufacturers =
                        availableManufacturers.Where(x => x.id != stoll.id);
                }
                else
                {
                    MelcherLog.LogError($"Could not find manufacturer info for Stoll, therefore it could not be filtered from the manufacturer list.");
                }
            }

            // Construct the list that is used by the RecyclableList to track
            // what each row looks like.

            IEnumerable<IGrouping<ProductCategory?, ManufacturerInfo>> groupedManufacturers =
                availableManufacturers
                    .GroupBy(x => x.productCategory)
                    .OrderBy(group => group.Key);

            foreach (IGrouping<ProductCategory?, ManufacturerInfo> manufacturerGroup in groupedManufacturers)
            {
                if (manufacturerGroup.Key.HasValue)
                {
                    // Start each product category with a header.
                    m_ManufacturerListEntries.Add(new RowEntry(manufacturerGroup.Key.Value));
                }

                foreach (ManufacturerInfo manufacturer in manufacturerGroup)
                {
                    m_ManufacturerListEntries.Add(new RowEntry(manufacturer));
                }
            }

            // Cache current manufacturer price markups

            m_ManufacturerIdToMarkupPercentages.Clear();

            foreach (ManufacturerInfo manufacturer in ProFitGlobals.manufacturers)
            {
                m_ManufacturerIdToMarkupPercentages[manufacturer.id] =
                    ProFitGlobals.userProfile.GetMarkupPercentageForManufacturerId(manufacturer.id);
            }

            m_SetMarkupForAllButton.gameObject.SetActive(ProFitGlobals.appOptions.allowDealerMarkupPercentChanges);

            // We need to refresh the layout to ensure the accordion fills
            // the space left by the Set Markup For All button.
            for (Transform t = m_ManufacturerListAccordion.contentLayout.transform; t && t != m_Canvas.transform; t = t.parent)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(t as RectTransform);
            }

            m_ManufacturerList.RowCount = m_ManufacturerListEntries.Count;
            m_ManufacturerList.Refresh();

            bool isUsingWhitelist = m_SelectedManufacturerIds.Count > 0;
            m_AllowAllManufacturersToggle.SetIsOnWithoutNotify(!isUsingWhitelist);
            m_ManufacturerListAccordion.Snap(expand: isUsingWhitelist);
        }

        protected override void DoShow()
        {
            base.DoShow();

            //// Ensure the accordion's size is synchronized to the activeness of
            //// the Set Markup For All button.
            //m_ManufacturerListAccordion.Snap(m_ManufacturerListAccordion.isExpanded);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            ManufacturerWhitelistRow.onValueChanged += OnWhitelistChanged;
            ManufacturerWhitelistRow.onPriceMarkupButtonClicked += OnPriceMarkupButtonClicked;

            m_AllowAllManufacturersToggle.onValueChanged += OnChangeAllowAllManufacturers;
            m_ManufacturerList.ItemCallback += BindItem;
            m_SetMarkupForAllButton.onClick += OnSetPriceMarkupForAll;
            m_ApplyButton.onClick += ApplySelection;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            ManufacturerWhitelistRow.onValueChanged -= OnWhitelistChanged;
            ManufacturerWhitelistRow.onPriceMarkupButtonClicked -= OnPriceMarkupButtonClicked;

            m_AllowAllManufacturersToggle.onValueChanged += OnChangeAllowAllManufacturers;
            m_ManufacturerList.ItemCallback -= BindItem;
            m_SetMarkupForAllButton.onClick -= OnSetPriceMarkupForAll;
            m_ApplyButton.onClick -= ApplySelection;
        }

        /// <summary>
        /// If the user is using all manufacturers, disable the manufacturer
        /// selection list. Otherwise, enable the selection list.
        /// </summary>
        protected void OnChangeAllowAllManufacturers(IMelcherToggle toggle)
        {
            bool isUsingWhitelist = !toggle.isOn;
            m_ManufacturerListContentGroup.interactable = isUsingWhitelist;
            m_ManufacturerListAccordion.TweenUsingDuration(expand: isUsingWhitelist, duration: 0.6f);

            EventBridge.Menu.setMenuHasPendingChanges(this, hasPendingChanges: true);
        }

        protected void BindItem(RecyclingListViewItem item, int itemIndex)
        {
            if (!item.TryGetComponent(out ManufacturerWhitelistRow row))
            {
                return;
            }

            RowEntry rowData = m_ManufacturerListEntries[itemIndex];

            if (rowData.rowType == RowType.Header)
            {
                row.SetManufacturerGroupHeadingFields(rowData.manufacturerProductCategory);
                return;
            }

            int manufacturerId = rowData.manufacturerId;

            IReadOnlyList<int> adminSelectedManufacturerIds =
                ProFitGlobals.userProfile.adminSelectedManufacturerIds;

            bool isRowInteractable = true;

            if (adminSelectedManufacturerIds != null &&
                adminSelectedManufacturerIds.Count > 0 &&
                !adminSelectedManufacturerIds.Contains(manufacturerId))
            {
                isRowInteractable = false;
            }

            string manufacturerName = "<unknown>";
            if (ProFitGlobals.catalogData.FindManufacturerById(manufacturerId, out ManufacturerInfo manufacturer))
            {
                manufacturerName = manufacturer.name;
            }

            decimal markupPercentage = 0.0m;
            m_ManufacturerIdToMarkupPercentages.TryGetValue(manufacturer.id, out markupPercentage);

            row.SetManufacturerCheckboxFields(
                manufacturerName,
                markupPercentage,
                itemIndex,
                m_SelectedManufacturerIds.Contains(manufacturerId),
                isRowInteractable
            );
        }

        protected void OnWhitelistChanged(ManufacturerWhitelistRow row)
        {
            int manufacturerId =
                m_ManufacturerListEntries[row.index].manufacturerId;

            if (row.isOn)
            {
                if (!m_SelectedManufacturerIds.Contains(manufacturerId))
                {
                    m_SelectedManufacturerIds.Add(manufacturerId);
                }
            }
            else
            {
                m_SelectedManufacturerIds.Remove(manufacturerId);
            }

            EventBridge.Menu.setMenuHasPendingChanges(this, hasPendingChanges: true);
        }

        protected void OnPriceMarkupButtonClicked(ManufacturerWhitelistRow row)
        {
            int manufacturerId =
                m_ManufacturerListEntries[row.index].manufacturerId;

            ProFitGlobals.catalogData.FindManufacturerById(manufacturerId, out ManufacturerInfo manufacturerInfo);

            decimal markupPercentage = 0.0m;
            m_ManufacturerIdToMarkupPercentages.TryGetValue(manufacturerId, out markupPercentage);

            PromptForMarkupPercentage(
                manufacturerInfo.name,
                markupPercentage,
                newMarkupPercentage =>
                {
                    m_ManufacturerIdToMarkupPercentages[manufacturerId] = newMarkupPercentage;

                    m_ManufacturerList.Refresh(row.GetComponent<RecyclingListViewItem>());

                    EventBridge.Menu.setMenuHasPendingChanges(this, hasPendingChanges: true);
                }
            );
        }

        protected void OnSetPriceMarkupForAll(MelcherButton caller)
        {
            PromptForMarkupPercentage(
                "Set All Manufactures",
                markupPercentage: 0.0m,
                newMarkupPercentage =>
                {
                    foreach (ManufacturerInfo manufacturer in ProFitGlobals.manufacturers)
                    {
                        m_ManufacturerIdToMarkupPercentages[manufacturer.id] = newMarkupPercentage;
                    }

                    m_ManufacturerList.Refresh();

                    EventBridge.Menu.setMenuHasPendingChanges(this, hasPendingChanges: true);
                }
            );
        }

        protected void PromptForMarkupPercentage(string title, decimal markupPercentage, System.Action<decimal> onAccept)
        {
            EventBridge.Menu.showEditUserProfileUi.Invoke(
                title,
                header1: $"Price Markup %",
                value1: markupPercentage.ToString(),
                contentType: TMPro.TMP_InputField.ContentType.DecimalNumber,
                keyboardType: TouchScreenKeyboardType.NumberPad,
                onAccept: (value1, value2) =>
                {
                    const decimal MaxPriceMarkupPercent = 50.0m;

                    if (!decimal.TryParse(value1, out decimal newMarkupPercent) ||
                        newMarkupPercent < -MaxPriceMarkupPercent ||
                        newMarkupPercent > MaxPriceMarkupPercent)
                    {
                        EventBridge.Menu.setEditUserProfileError($"Please enter a number between -{MaxPriceMarkupPercent} and +{MaxPriceMarkupPercent}.");
                        return;
                    }

                    // Limit inputs to increments of 0.05.

                    const decimal PercentStep = 0.05m;

                    if ((newMarkupPercent / PercentStep) % 1.0m != 0.0m)
                    {
                        // Input is too precise.
                        EventBridge.Menu.setEditUserProfileError($"Please enter a number that is an increment of {PercentStep}.");
                        return;
                    }

                    m_EditUserProfileUi.Hide();

                    onAccept.Invoke(newMarkupPercent);
                }
            );
        }

        protected void ApplySelection(MelcherButton caller)
        {
            EventBridge.Menu.showFullScreenSpinner?.Invoke(true);
            m_CanvasGroup.interactable = false;
            m_MessageBox.Clear();

            // Copy the current profile but override the manufacturer whitelist
            UserProfile profileCopy = ProFitGlobals.userProfile;
            profileCopy.dealerSelectedManufacturerIds = new List<int>();

            // If a user selects no toggles we will treat that as allowing all
            // of them. This behaviour occurs elsewhere in the app (ToolbarUI)
            if (!m_AllowAllManufacturersToggle.isOn && m_SelectedManufacturerIds.Count > 0)
            {
                List<ManufacturerInfo> allManufactures = ProFitGlobals.manufacturers;

                // First, add all the mandatory IDs
                foreach (int id in m_MandatoryManufacturerIds)
                {
                    if (!profileCopy.dealerSelectedManufacturerIds.Contains(id))
                    {
                        profileCopy.dealerSelectedManufacturerIds.Add(id);
                    }
                }

                // Then add all the other selected ids. There are two layers
                // of indirection here (m_SelectedAvailableManufactures indexes
                // m_AvailableManufacturerIndices which in turn indexes
                // allManufactures)
                foreach (int id in m_SelectedManufacturerIds)
                {
                    if (!profileCopy.dealerSelectedManufacturerIds.Contains(id))
                    {
                        profileCopy.dealerSelectedManufacturerIds.Add(id);
                    }
                }
            }

            foreach (int manufacturerId in m_ManufacturerIdToMarkupPercentages.Keys)
            {
                profileCopy.SetMarkupPercentageForManufacturerId(
                    manufacturerId,
                    m_ManufacturerIdToMarkupPercentages[manufacturerId]
                );
            }

            ProFitWeb.UpdateProfile(
                ProFitGlobals.user,
                profileCopy,
                password: null,
                confirmPassword: null,
                onSuccess: (newUserProfile) =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
                    m_CanvasGroup.interactable = true;

                    ProFitGlobals.userProfile = newUserProfile;

                    User user = ProFitGlobals.user;
                    user.userType = newUserProfile.userType;
                    ProFitGlobals.user = user;

                    EventBridge.Menu.setMenuHasPendingChanges(this, hasPendingChanges: false);

                    EventBridge.Menu.navigateBackward.Invoke();
                },
                onError: (builder, error, jsonValue) =>
                {
                    EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
                    m_CanvasGroup.interactable = true;

                    m_MessageBox.ErrorFromWebRequest(builder, error, jsonValue);
                }
            );
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(ManufacturerWhitelistUI))]

        [SerializeField] protected RectTransform m_MainContentLayoutTransform = null;
        [SerializeField] protected MelcherToggle m_AllowAllManufacturersToggle = null;
        [SerializeField] protected Accordion m_ManufacturerListAccordion = null;
        [SerializeField] protected RecyclingListView m_ManufacturerList = null;
        [SerializeField] protected CanvasGroup m_ManufacturerListContentGroup = null;
        [SerializeField] protected MelcherButton m_SetMarkupForAllButton = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_ApplyButton = null;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected EditUserProfileUI m_EditUserProfileUi = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected List<int> m_MandatoryManufacturerIds = new List<int>();

        protected List<int> m_SelectedManufacturerIds = new List<int>();

        /// <summary>
        ///     This defines the rows in the RecyclableList. A row can be
        ///     either a header (a label for a product category that will be
        ///     followed up by one or more items belonging to that category) or
        ///     a toggle for a specific manufacturer. Each toggle represents a
        ///     manufacturer that the user can choose to filter.
        ///     This is the list the recyclable ListView is bound to.
        /// </summary>
        protected List<RowEntry> m_ManufacturerListEntries = new List<RowEntry>();

        protected Dictionary<int, decimal> m_ManufacturerIdToMarkupPercentages = new ();

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        ///     Represents a row in the RecyclableList used for the manufacturer
        ///     white list. This is how we remember what to show in a given row.
        ///     I.e., whether to show a toggle or a header.
        /// </summary>
        protected readonly struct RowEntry
        {
            public RowEntry(ProductCategory productCategory)
            {
                rowType = RowType.Header;
                manufacturerProductCategory = productCategory;
                manufacturerId = -1;
            }

            public RowEntry(ManufacturerInfo manufacturer)
            {
                rowType = RowType.ManufacturerToggle;
                manufacturerProductCategory = default;
                manufacturerId = manufacturer.id;
            }

            public readonly RowType rowType;

            /// <summary>
            ///     The product category this header represents. This is only
            ///     valid when <see cref="rowType"/> is <see
            ///     cref="RowType.Header"/>.
            /// </summary>
            public readonly ProductCategory manufacturerProductCategory;

            /// <summary>
            ///     The index of the manufacturer. This is only valid when <see
            ///     cref="rowType"/> is <see
            ///     cref="RowType.ManufacturerToggle"/>.
            /// </summary>
            public readonly int manufacturerId;

        }   // struct RowEntry

        protected enum RowType
        {
            Header,
            ManufacturerToggle

        }   // enum RowType

    }   // class ClientListUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-19
/// ===========================================================================
