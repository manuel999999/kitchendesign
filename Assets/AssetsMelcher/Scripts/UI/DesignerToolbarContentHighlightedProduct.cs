namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Manages the UI for the highlighted product in the toolbar.
    /// </summary>
    public class DesignerToolbarContentHighlightedProduct : MelcherMonoBehaviour
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Update the content shown in the selected object toolbar.
        /// </summary>
        /// <param name="selectedObject">
        ///     The GameObject of the thing the player selected in the kitchen
        ///     scene: E.g., the kitchen's root, a cabinet or an occupant. When
        ///     this is <see langword="null"/>, the selected object is cleared.
        /// </param>
        public void SetSelectedObject(GameObject selectedObject)
        {
            m_SelectedObject = selectedObject;

            if (m_SelectedObject)
            {
                string title = "";
                string subtitle = "";
                bool showRemoveButton = true;
                bool showUpAndDownButtons = false;
                bool showProductUrlButton = false;
                bool showReorderCabinetButtons = false;
                bool showSwapCabinetButton = false;

                Kitchen kitchen = null;

                Occupant occupant = null;
                CabinetBacking backing = null;
                Cabinet cabinet = null;

                if (m_SelectedObject.TryGetComponent(out occupant))
                {
                    // The selected object is an occupant.

                    kitchen = (Kitchen)occupant.cabinet.kitchen;

                    title = occupant.productInfo.name;
                    subtitle = occupant.productInfo.sku;
                    showRemoveButton = !occupant.isLockedToCabinet;
                    showUpAndDownButtons = occupant.productVisual.info.allowUpAndDownMovement;
                    showProductUrlButton = !string.IsNullOrWhiteSpace(occupant.productInfo.storeLink);
                }
                else if (m_SelectedObject.TryGetComponent(out backing))
                {
                    // The selected object is a cabinet backing.

                    title =
                        backing.info.category switch
                        {
                            ProductCategory.Backsplash => "Backsplash",
                            ProductCategory.BarTopHigh => "Bar Top",
                            ProductCategory.BarTopLow => "Low Bar Top",
                            _ => backing.info.category.ToString()
                        };

                    subtitle = "";
                }
                else if (m_SelectedObject.TryGetComponent(out cabinet))
                {
                    // The selected object is a cabinet (or cabinet section).
                    string cabinetName = "Cabinet";

                    if (cabinet.cabinetSectionProductInfo != null)
                    {
                        cabinetName = cabinet.cabinetSectionProductInfo.name;
                    }

                    title = cabinetName;

                    string w;
                    string h;
                    string d;

                    Vector3 cabinetSizeInMeters = cabinet.sizeInMeters;

                    if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                    {
                        w = Utility.MeasurementFormat.FormatMetersToCentimeters(cabinetSizeInMeters.x);
                        h = Utility.MeasurementFormat.FormatMetersToCentimeters(cabinetSizeInMeters.y);
                        d = Utility.MeasurementFormat.FormatMetersToCentimeters(cabinetSizeInMeters.z);
                    }
                    else
                    {
                        w = Utility.MeasurementFormat.FormatMetersToImperial(cabinetSizeInMeters.x);
                        h = Utility.MeasurementFormat.FormatMetersToImperial(cabinetSizeInMeters.y);
                        d = Utility.MeasurementFormat.FormatMetersToImperial(cabinetSizeInMeters.z);
                    }

                    subtitle = $"{w} x {h} x {d}";

                    showReorderCabinetButtons = true;

                    kitchen = (Kitchen)cabinet.kitchen;

                    int cabinetSiblingIndex =
                        cabinet.GetTransform().GetSiblingIndex();

                    bool enableMoveLeftButton = false;
                    bool enableMoveRightButton = false;

                    if (kitchen.layout.CabinetHasMidSpanCabinetNeighbour(cabinet))
                    {
                        enableMoveLeftButton =
                            kitchen.layout.FindInsertIndexForCabinet(
                                cabinetToInsert: cabinet,
                                cabinetSectionProductInfo: cabinet.cabinetSectionProductInfo,
                                startIndex: cabinetSiblingIndex,
                                reverse: true
                            ).HasValue;

                        enableMoveRightButton =
                            kitchen.layout.FindInsertIndexForCabinet(
                                cabinetToInsert: cabinet,
                                cabinetSectionProductInfo: cabinet.cabinetSectionProductInfo,
                                startIndex: cabinetSiblingIndex,
                                reverse: false
                            ).HasValue;
                    }

                    m_ReorderCabinetLeftButton.interactable = enableMoveLeftButton;
                    m_ReorderCabinetRightButton.interactable = enableMoveRightButton;

                    if (cabinet.cabinetSectionProductInfo != null)
                    {
                        showSwapCabinetButton = true;
                        showProductUrlButton = !string.IsNullOrWhiteSpace(cabinet.cabinetSectionProductInfo.storeLink);
                    }
                }

                if (kitchen && kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections)
                {
                    // No up-and-down movement in Sections/Stoll mode.
                    showUpAndDownButtons = false;
                }

                m_ProductNameLabel.phrase = title;
                m_ProductIdLabel.phrase = subtitle;
                m_ProductUrlButton.gameObject.SetActive(showProductUrlButton);

                m_AboutProductButton.gameObject.SetActive(occupant || (cabinet && cabinet.cabinetSectionProductInfo != null));

                m_RemoveProductButton.gameObject.SetActive(showRemoveButton);

                m_MoveProductUpButton.gameObject.SetActive(showUpAndDownButtons);
                m_MoveProductDownButton.gameObject.SetActive(showUpAndDownButtons);

                m_ReorderCabinetLeftButton.gameObject.SetActive(showReorderCabinetButtons);
                m_ReorderCabinetRightButton.gameObject.SetActive(showReorderCabinetButtons);

                m_SwapCabinetButton.gameObject.SetActive(showSwapCabinetButton);

                m_CenterProductButton.gameObject.SetActive(occupant);
            }
        }

        protected void Awake()
        {
            bool showProductButton = Application.isEditor || Debug.isDebugBuild || ProFitGlobals.isVerbose;

            m_ProductServerLinkButton.gameObject.SetActive(showProductButton);

            bool showClipboardButton = !MelcherWebGL.IsActiveBuildTarget();

            // Pro-Fit requested the clipboard button is removed. Therefore
            // it is always hidden.
            showClipboardButton = false;

            m_CopyProductIdButton.gameObject.SetActive(showClipboardButton);

            // Ensure the toolbar mask is enabled. It is disabled during
            // edit-time to reveal all the buttons for visual editing.
            m_ScrollAreaMask.enabled = true;
        }

        protected void OnEnable()
        {
            m_AboutProductButton.onClick += OnClickAbout;
            m_CloneProductButton.onClick += OnClickClone;
            m_RemoveProductButton.onClick += OnClickRemove;
            m_MoveProductUpButton.onClick += OnClickMoveProductUp;
            m_MoveProductDownButton.onClick += OnClickMoveProductDown;
            m_ReorderCabinetLeftButton.onClick += OnClickReorderCabinetLeft;
            m_ReorderCabinetRightButton.onClick += OnClickReorderCabinetRight;
            m_SwapCabinetButton.onClick += OnClickSwapCabinet;
            m_CenterProductButton.onClick += OnClickCenterProduct;

            m_ProductUrlButton.onClick += OnClickProductUrl;
            m_CopyProductIdButton.onClick += OnClickCopyProductIdToClipboard;
            m_ProductServerLinkButton.onClick += OnClickServerLink;
        }

        protected void OnDisable()
        {
            m_AboutProductButton.onClick -= OnClickAbout;
            m_CloneProductButton.onClick -= OnClickClone;
            m_RemoveProductButton.onClick -= OnClickRemove;
            m_MoveProductUpButton.onClick -= OnClickMoveProductUp;
            m_MoveProductDownButton.onClick -= OnClickMoveProductDown;
            m_ReorderCabinetLeftButton.onClick -= OnClickReorderCabinetLeft;
            m_ReorderCabinetRightButton.onClick -= OnClickReorderCabinetRight;
            m_SwapCabinetButton.onClick -= OnClickSwapCabinet;
            m_CenterProductButton.onClick -= OnClickCenterProduct;

            m_ProductUrlButton.onClick -= OnClickProductUrl;
            m_CopyProductIdButton.onClick -= OnClickCopyProductIdToClipboard;
            m_ProductServerLinkButton.onClick -= OnClickServerLink;
        }

        protected void Update()
        {
            // TODO: Move this elsewhere
            if (Input.GetKeyDown(KeyCode.Delete) && m_SelectedObject)
            {
                if (m_SelectedObject.TryGetComponent(out Occupant occupant))
                {
                    EventBridge.Designer.onTryDestroyOccupant.InvokeIfNonEmpty(occupant).Forget();
                }
                else if (m_SelectedObject.TryGetComponent(out Cabinet cabinet))
                {
                    EventBridge.Designer.onTryDestroySection?.Invoke(cabinet);
                }
            }
        }

        protected void OnClickAbout(MelcherButton caller)
        {
            if (!m_SelectedObject)
            {
                return;
            }

            ProductInfo productInfo = null;
            ProductVisual productVisual = null;

            if (m_SelectedObject.TryGetComponent(out Occupant selectedOccupant))
            {
                productInfo = selectedOccupant.productInfo;
                productVisual = selectedOccupant.productVisual;
            }
            else if (m_SelectedObject.TryGetComponent(out Cabinet cabinet))
            {
                productInfo = cabinet.cabinetSectionProductInfo;
                productVisual = cabinet.cabinetSectionVisual;
            }

            if (!productVisual)
            {
                MelcherLog.LogError("Not something that contains a product visual.", m_SelectedObject);
                return;
            }

            EventBridge.Designer.onAboutProduct.Invoke(productInfo, productVisual);
        }

        protected void OnClickClone(MelcherButton caller)
        {
            if (!m_SelectedObject)
            {
                return;
            }

            if (m_SelectedObject.TryGetComponent(out Occupant selectedOccupant))
            {
                EventBridge.Designer.onTryCloneProduct.Invoke(selectedOccupant.productInfo, onComplete: null);
            }
            else if (m_SelectedObject.TryGetComponent(out Cabinet cabinet))
            {
                EventBridge.Designer.onTryCloneSection.Invoke(cabinet);
            }

            // Refresh the action buttons to allow a selected cabinet to be
            // refresh whether it can be reordered after it is cloned.
            SetSelectedObject(m_SelectedObject);
        }

        protected void OnClickRemove(MelcherButton caller)
        {
            if (!m_SelectedObject)
            {
                return;
            }

            if (m_SelectedObject.TryGetComponent(out Occupant selectedOccupant))
            {
                EventBridge.Designer.onTryDestroyOccupant.Invoke(selectedOccupant).Forget();
            }
            else if (m_SelectedObject.TryGetComponent(out CabinetBacking selectedBacking))
            {
                EventBridge.Designer.onTryDestroyCabinetBacking.Invoke(selectedBacking).Forget();
            }
            else if (m_SelectedObject.TryGetComponent(out Cabinet cabinet))
            {
                EventBridge.Designer.onTryDestroySection.Invoke(cabinet);
            }
        }

        protected void OnClickMoveProductUp(MelcherButton caller)
        {
            if (!m_SelectedObject || !m_SelectedObject.TryGetComponent(out Occupant occupant))
            {
                return;
            }

            // Determine how far up this product can be nudged.
            float maxYInMeters =
                occupant.occupiedVolume.resizerCuboid.height - Const.MinMetersFromEdgeAfterNudge;

            if (occupant.productVisual.info.anchor.IsAtBottom())
            {
                Cuboid cutOutCuboid = occupant.GetCutOutCuboidInVolumeSpace();
                maxYInMeters -= cutOutCuboid.height;
            }

            Vector3 nudgeAmountInMeters = Vector3.up * Units.Inches2Meters;
            Vector3 newPosition = occupant.productVisualContentInstance.GetTransform().localPosition;
            newPosition += nudgeAmountInMeters;

            // Prevent nudging the product too high
            newPosition.y = Mathf.Min(newPosition.y, maxYInMeters);

            bool didMove = false;

            EventBridge.Designer.onTryRepositionOccupant?.Invoke(
                occupantToMove: occupant,
                newPosition: newPosition,
                snapMode: EventBridge.Designer.RepositionSnapMode.None,
                ref didMove
            );

            if (didMove)
            {
                EventBridge.Designer.onOccupantNudged?.Invoke(occupant, nudgeAmountInMeters);
            }
            else
            {
                EventBridge.Designer.showToast?.Invoke("Can't move up here");
            }
        }

        protected void OnClickMoveProductDown(MelcherButton caller)
        {
            if (!m_SelectedObject || !m_SelectedObject.TryGetComponent(out Occupant occupant))
            {
                return;
            }

            // Determine how far down this product can be nudged.
            float minYInMeters = Const.MinMetersFromEdgeAfterNudge;

            if (!occupant.productVisual.info.anchor.IsAtBottom())
            {
                Cuboid cutOutCuboid = occupant.GetCutOutCuboidInVolumeSpace();
                minYInMeters += cutOutCuboid.height;
            }

            Vector3 nudgeAmountInMeters = Vector3.down * Units.Inches2Meters;
            Vector3 newPosition = occupant.productVisualContentInstance.GetTransform().localPosition;
            newPosition += nudgeAmountInMeters;

            // Prevent nudging the product too low
            newPosition.y = Mathf.Max(newPosition.y, minYInMeters);

            bool didMove = false;

            EventBridge.Designer.onTryRepositionOccupant?.Invoke(
                occupantToMove: occupant,
                newPosition: newPosition,
                snapMode: EventBridge.Designer.RepositionSnapMode.None,
                ref didMove
            );

            if (didMove)
            {
                EventBridge.Designer.onOccupantNudged?.Invoke(occupant, nudgeAmountInMeters);
            }
            else
            {
                EventBridge.Designer.showToast?.Invoke("Can't move down here");
            }
        }

        protected void OnClickReorderCabinetLeft(MelcherSelectable caller)
        {
            OnClickReorderCabinetLeftAsync().Forget();

            async UniTask OnClickReorderCabinetLeftAsync()
            {
                if (m_SelectedObject.TryGetComponent(out Cabinet cabinet))
                {
                    await EventBridge.Designer.onTryReorderCabinet.InvokeIfNonEmpty((cabinet, Extent.Min));

                    // Refresh the UI to sync the reorder left/right buttons
                    // according to the new state.
                    SetSelectedObject(m_SelectedObject);
                }
            }
        }

        protected void OnClickReorderCabinetRight(MelcherSelectable caller)
        {
            OnClickReorderCabinetRightAsync().Forget();

            async UniTask OnClickReorderCabinetRightAsync()
            {
                if (m_SelectedObject.TryGetComponent(out Cabinet cabinet))
                {
                    await EventBridge.Designer.onTryReorderCabinet.InvokeIfNonEmpty((cabinet, Extent.Max));

                    // Refresh the UI to sync the reorder left/right buttons
                    // according to the new state.
                    SetSelectedObject(m_SelectedObject);
                }
            }
        }

        protected void OnClickSwapCabinet(MelcherSelectable caller)
        {
            if (m_SelectedObject.TryGetComponent(out Cabinet cabinet))
            {
                EventBridge.Designer.onCabinetSwapStart?.Invoke(cabinet);
            }
        }

        protected void OnClickCenterProduct(MelcherSelectable caller)
        {
            if (m_SelectedObject.TryGetComponent(out Occupant occupant))
            {
                EventBridge.Designer.centerOccupantInVolume?.Invoke(occupant);
            }
        }

        /// <summary>
        ///     Open the product's store link as entered in the dashboard.
        /// </summary>
        /// <param name="caller"></param>
        protected void OnClickProductUrl(MelcherButton caller)
        {
            if (m_SelectedObject)
            {
                if (m_SelectedObject.TryGetComponent(out Occupant occupant))
                {
                    MelcherWebGL.OpenNewTab(occupant.productInfo.storeLink);
                }
                else if (m_SelectedObject.TryGetComponent(out Cabinet cabinet) &&
                    cabinet.cabinetSectionProductInfo != null)
                {
                    MelcherWebGL.OpenNewTab(cabinet.cabinetSectionProductInfo.storeLink);
                }
            }
        }

        protected void OnClickCopyProductIdToClipboard(MelcherButton caller)
        {
            // Copy the product ID to the clipboard
            GUIUtility.systemCopyBuffer = m_ProductIdLabel.textMesh.GetParsedText();
            EventBridge.Designer.showToast?.Invoke("Copied product ID to clipboard");
        }

        protected void OnClickServerLink(MelcherButton caller)
        {
            if (m_SelectedObject && m_SelectedObject.TryGetComponent(out Occupant occupant))
            {
                ProFitUtility.OpenProductUrl(occupant.productInfo.id);
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header("Info Panel")]
        [SerializeField] protected MelcherLocalizedText m_ProductNameLabel = null;
        [SerializeField] protected MelcherLocalizedText m_ProductIdLabel = null;
        [SerializeField] protected MelcherButton m_ProductUrlButton = null;
        [SerializeField] protected MelcherButton m_CopyProductIdButton = null;

        [Tooltip("An editor-only button to quickly view the product in the " +
            "dashboard.")]
        [SerializeField] protected MelcherButton m_ProductServerLinkButton = null;

        [Header("Action Panel")]
        [SerializeField] protected Mask m_ScrollAreaMask = null;
        [SerializeField] protected MelcherButton m_AboutProductButton = null;
        [SerializeField] protected MelcherButton m_CloneProductButton = null;
        [SerializeField] protected MelcherButton m_RemoveProductButton = null;
        [SerializeField] protected MelcherButton m_MoveProductUpButton = null;
        [SerializeField] protected MelcherButton m_MoveProductDownButton = null;
        [SerializeField] protected MelcherButton m_ReorderCabinetLeftButton = null;
        [SerializeField] protected MelcherButton m_ReorderCabinetRightButton = null;
        [SerializeField] protected MelcherButton m_SwapCabinetButton = null;
        [SerializeField] protected MelcherButton m_CenterProductButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected GameObject m_SelectedObject;

    }   // class DesignerToolbarContentHighlightedProduct

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-03
/// ===========================================================================
