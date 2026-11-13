namespace KitchenDesign.UI.DesignerScreens
{
    using KitchenDesign.Events;
    using KitchenDesign.Session;
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using System.Linq;
    using UnityEngine;
    using KitchenDesign.Geometry;
    using KitchenDesign.Cabinets;
    using KitchenDesign.KitchenModel;

    public class CabinetOtherSizesUI : MelcherUI
    {
        private KitchenEvents _events;

        public void ListenTo(KitchenEvents events)
        {
            if (this._events != null && this.isActiveAndEnabled)
            {
                this.RemoveKitchenListeners();
            }

            this._events = events;

            if (this._events != null && this.isActiveAndEnabled)
            {
                this.AddKitchenListeners();
            }
        }

        private void AddKitchenListeners()
        {
            this._events.onBuildingMethodChanged += OnBuildingMethodChanged;
        }

        private void RemoveKitchenListeners()
        {
            this._events.onBuildingMethodChanged -= OnBuildingMethodChanged;
        }

        private SessionStatus _session;

        private Kitchen _kitchen;

        public void Initialize(Kitchen kitchen, SessionStatus session)
        {
            this._session = session;
            this._kitchen = kitchen;

            m_StollCountertopThicknessControl.Initialize(session);
            m_CountertopThicknessControl.Initialize(session);
            m_CabinetDepthControl.Initialize(session);
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void SetMode(CabinetSizesMode mode, CabinetLayout layout)
        {
            if (layout == null)
            {
                // We need to know the kitchen layout as it influences whether
                // the cabinet depth option is visible.
                throw new System.ArgumentNullException(nameof(layout));
            }

            m_Mode = mode;


            int cornerCabinetCount = 0;

            if (layout)
            {
                cornerCabinetCount =
                    layout.EnumerateCabinets()
                        .Count(static x => x.element.Is(CabinetType.Corner));
            }

            // The cabinet depth option is restricted to Pro-Fit kitchens.
            // The other styles of kitchens have predetermined depths.
            // Additionally, we do not show the depth option for existing
            // kitchens if they have more than one span. This is because
            // kitchens in Pro-Fit now support different cabinet depths per
            // span.
            bool displayCabinetDepthOption =
                this._kitchen.buildingRules.UserChoosesCabinetDepth &&
                (mode == CabinetSizesMode.InitialCabinetSetup || cornerCabinetCount < 1);

            // Hide the cabinet depth control when Section mode is enabled.
            // This was requested by Stoll.
            m_CabinetDepthControl.gameObject.SetActive(displayCabinetDepthOption);

            _currentKitchen = null;

            if (m_Mode == CabinetSizesMode.ModifyExistingCabinet)
            {
                // Make the sliders display the kitchen's current size.

                _currentKitchen = this._kitchen;

                _currentCabinetDepthInMeters = _currentKitchen.baseCabinetDepthInMeters;
                _currentCountertopThicknessInMeters = _currentKitchen.countertopThicknessInMeters;
                _currentCountertopType = _currentKitchen.selectedCountertopType;

                CabinetOtherSizeControl.SelectOptionMatchingCurrentSize(
                    m_CabinetDepthControl,
                    _currentKitchen.baseCabinetDepthInMeters
                );

                if (_currentKitchen.selectedCountertopType == CountertopType.Default)
                {
                    CabinetOtherSizeControl.SelectOptionMatchingCurrentSize(
                        m_CountertopThicknessControl,
                        _currentKitchen.countertopThicknessInMeters
                    );
                }
                else
                {
                    CabinetOtherSizeControl.SelectOptionMatchingCurrentSize(
                        m_StollCountertopThicknessControl,
                        _currentKitchen.countertopThicknessInMeters
                    );
                }
            }
        }

        public void ClearForm()
        {
            // Default to using Stoll countertops. This is ignored
            // when Sections building method is not active because the
            // `useIsEnabledToggle` will be false.
            MelcherToggleGroup countertopEnabledToggleGroup =
                m_StollCountertopThicknessControl.isEnabledToggle.toggleGroup;

            countertopEnabledToggleGroup.SetAllTogglesOff(instant: true, sendCallback: false);
            m_StollCountertopThicknessControl.isEnabledToggle.isOn = true;

            m_StollCountertopThicknessControl.SelectOption(_defaultStollCountertopThicknessOptionIndex, notify: false);
            m_CountertopThicknessControl.SelectOption(_defaultCountertopThicknessOptionIndex, notify: false);
            m_CabinetDepthControl.SelectOption(_defaultCabinetDepthOptionIndex, notify: false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_BackButton.onClick += NavigateBackward;
            m_NextButton.onClick += GoNext;

            if (this._events != null)
            {
                this.AddKitchenListeners();
            }
            m_StollCountertopThicknessControl.isEnabledToggle.toggleGroup.onToggleChanged += OnCountertopTypeChanged;
            m_CountertopThicknessControl.onValueChanged += OnCountertopThicknessChanged;
            m_StollCountertopThicknessControl.onValueChanged += OnCountertopThicknessChanged;
            m_CabinetDepthControl.onValueChanged += OnCabinetDepthChanged;
        }

        protected override void DoShow()
        {
            if (m_Mode == CabinetSizesMode.InitialCabinetSetup)
            {
                // It was requested that the other sizes UI (cabinet depth and
                // countertop thickness) be skipped during initial setup.
                // Therefore, we automatically accept the default values. We
                // recently added the ability to adjust the cabinet depth and
                // countertop thickness at any point, so no features are missing.
                GoNext(null);
                return;
            }

            base.DoShow();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_BackButton.onClick -= NavigateBackward;
            m_NextButton.onClick -= GoNext;

            if (this._events != null)
            {
                this.RemoveKitchenListeners();
            }
            m_StollCountertopThicknessControl.isEnabledToggle.toggleGroup.onToggleChanged -= OnCountertopTypeChanged;
            m_CountertopThicknessControl.onValueChanged -= OnCountertopThicknessChanged;
            m_StollCountertopThicknessControl.onValueChanged -= OnCountertopThicknessChanged;
            m_CabinetDepthControl.onValueChanged -= OnCabinetDepthChanged;
        }

        protected void NavigateBackward(MelcherButton button)
        {
            Hide();

            if (m_Mode == CabinetSizesMode.InitialCabinetSetup)
            {
                EventDesigner.OnCabinetShapesRequested.Raise();
            }
            else
            {
                // Restore the original kitchen size.

                _currentKitchen.SetBaseCabinetDepth(_currentCabinetDepthInMeters);
                _currentKitchen.SetCountertopThickness(_currentCountertopThicknessInMeters);
                this._kitchen.SetCountertopType(_currentCountertopType);
            }
        }

        protected void GoNext(MelcherButton caller)
        {
            Hide();

            if (m_Mode == CabinetSizesMode.InitialCabinetSetup)
            {
                float countertopThicknessInMeters =
                    m_CountertopThicknessControl.value.sizeInMeters;

                if (this._kitchen.buildingRules.HasSecondCountertopThickness &&
                    m_StollCountertopThicknessControl.isEnabledToggle.isOn)
                {
                    countertopThicknessInMeters = m_StollCountertopThicknessControl.value.sizeInMeters;
                }

                CuboidPadding countertopOverhangInMeters =
                    CuboidPadding.one * this._kitchen.buildingRules.DefaultCountertopOverhangInMeters;

                float baseCabinetDepthInMeters = m_CabinetDepthControl.value.sizeInMeters;
                if (this._kitchen.buildingRules.UsesBrandCatalog)
                {
                    baseCabinetDepthInMeters = this._kitchen.selectedBrandCatalog.standardCabinetDepthInMeters;
                }

                this._kitchen.SetBaseCabinetDepth(baseCabinetDepthInMeters);
                this._kitchen.SetCountertopThickness(countertopThicknessInMeters);
                this._kitchen.SetCountertopOverhang(countertopOverhangInMeters);
                this._kitchen.CreateKitchen().Forget();
            }
            else
            {
                _currentKitchen.RefreshTickerLabels();

                bool hasCorner =
                    _currentKitchen.layout.EnumerateCabinets()
                        .Any(x => x.element.cabinetType == CabinetType.Corner);

                if (hasCorner && !MelcherCommon.IsAlmostEqual(_currentKitchen.baseCabinetDepthInMeters, _currentCabinetDepthInMeters, tolerance: 0.001f))
                {
                    // Display a message that informs the user of the new
                    // cabinet lengths. This is intended to draw attention to
                    // the fact that altering the kitchen's depth has the
                    // side-effect of changing the outside measurement.

                    // The message only needs to be shown if the kitchen has
                    // at least one visible corner. Otherwise, there's no
                    // spans that are joined in a way where the depth of one
                    // affects another.

                    string message =
                        "Please note that changing the cabinet depth " +
                        "also affects the outside measurement of the " +
                        "cabinets. The new widths are:";

                    foreach (CabinetSpan span in _currentKitchen.layout.spans)
                    {
                        float widthInMeters =
                            span.GetOutsideCuboidInSpanSpace().width;

                        string widthAsText = null;

                        if (this._session.UserProfile.activeUnits == MeasurementUnit.Centimeters)
                        {
                            widthAsText =
                                Utility.MeasurementFormat.FormatMetersToCentimeters(widthInMeters);
                        }
                        else
                        {
                            widthAsText =
                                Utility.MeasurementFormat.FormatMetersToImperial(widthInMeters);
                        }

                        message += $"\n<b>{widthAsText}</b>";
                    }

                    EventDesigner.OnNotificationRequested.Raise(new NotificationInfo(
                        title: "Depth Changed",
                        message: message,
                        onDismiss: null
                    ));
                }
            }
        }

        protected void OnBuildingMethodChanged(KitchenBuildingMethod newBuildingMethod)
        {
            bool isSectionsMethod =
                this._kitchen.buildingRules.HasSecondCountertopThickness;

            // Stoll has a second countertop thickness control.
            m_StollCountertopThicknessControl.gameObject.SetActive(isSectionsMethod);

            m_CountertopThicknessControl.useIsEnabledToggle = isSectionsMethod;
            m_StollCountertopThicknessControl.useIsEnabledToggle = isSectionsMethod;

            if (isSectionsMethod)
            {
                // Custom countertop thicknesses when in Sections method (Stoll).
                _defaultCountertopThicknessOptionIndex =
                    m_CountertopThicknessControl.SetOptions(
                        defaultIndex: 3,
                        new CabinetSizeOption(0.25f),
                        new CabinetSizeOption(0.50f),
                        new CabinetSizeOption(0.75f),
                        new CabinetSizeOption(1.00f),
                        new CabinetSizeOption(1.25f),
                        new CabinetSizeOption(1.50f),
                        new CabinetSizeOption(1.75f),
                        new CabinetSizeOption(2.00f),
                        new CabinetSizeOption(3.25f),
                        new CabinetSizeOption(3.50f),
                        new CabinetSizeOption(3.75f),
                        new CabinetSizeOption(4.00f)
                    );
            }
            else
            {
                _defaultCountertopThicknessOptionIndex =
                    m_CountertopThicknessControl.SetOptions(
                        defaultIndex: 3,
                        new CabinetSizeOption(0.5f),
                        new CabinetSizeOption(0.75f),
                        new CabinetSizeOption(1.0f),
                        new CabinetSizeOption(1.25f),
                        new CabinetSizeOption(1.5f),
                        new CabinetSizeOption(1.75f),
                        new CabinetSizeOption(2.0f)
                    );
            }

            _defaultCabinetDepthOptionIndex =
                CabinetOtherSizeControl.SetupCabinetDepthSizes(m_CabinetDepthControl, this._kitchen.buildingRules);

            // The Stoll countertop thickness always uses these values.
            // However, this control will be completely hidden unless Sections
            // method is used.
            _defaultStollCountertopThicknessOptionIndex =
                m_StollCountertopThicknessControl.SetOptions(
                    defaultIndex: 3,
                    new CabinetSizeOption(1.25f),
                    new CabinetSizeOption(2.0f),
                    new CabinetSizeOption(3.0f),
                    new CabinetSizeOption(4.0f)
                );

            ClearForm();
        }

        protected KitchenBuildingMethod GetKitchenBuildingMethod()
        {
            return this._kitchen.selectedBuildingMethod;
        }

        protected void OnCountertopTypeChanged(MelcherToggleGroup group, IMelcherToggle toggle)
        {
            CountertopType countertopType = CountertopType.Default;

            float countertopThicknessInMeters =
                m_CountertopThicknessControl.value.sizeInMeters;

            if (m_StollCountertopThicknessControl.useIsEnabledToggle &&
                m_StollCountertopThicknessControl.isEnabledToggle.isOn)
            {
                countertopType = CountertopType.Stoll;

                countertopThicknessInMeters =
                    m_StollCountertopThicknessControl.value.sizeInMeters;
            }

            this._kitchen.SetCountertopType(countertopType);
            this._kitchen.SetCountertopThickness(countertopThicknessInMeters);
        }

        protected void OnCountertopThicknessChanged(CabinetSizeOption selectedOption)
        {
            if (m_Mode == CabinetSizesMode.ModifyExistingCabinet)
            {
                MelcherLog.Breadcrumb("Cabinet countertop thickness changed to: " + selectedOption.displayLabel);

                _currentKitchen.SetCountertopThickness(selectedOption.sizeInMeters);
            }
        }

        protected void OnCabinetDepthChanged(CabinetSizeOption selectedOption)
        {
            if (m_Mode == CabinetSizesMode.ModifyExistingCabinet)
            {
                MelcherLog.Log("Cabinet depth changed to: " + selectedOption.displayLabel);

                // Test if the new size works.
                foreach (CabinetSpan span in _currentKitchen.layout.spans)
                {
                    if (!span.CanResizeBaseCabinetDepth(selectedOption.sizeInMeters))
                    {
                        return;
                    }
                }

                // Clear the previous toast in case it was the message saying
                // the cabinet depth size cannot be used.
                EventDesigner.OnToastRequested.Raise(new ToastInfo("", holdDuration: 0.0f));
                _currentKitchen.SetBaseCabinetDepth(selectedOption.sizeInMeters);
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(CabinetOtherSizesUI))]

        [SerializeField] protected CabinetOtherSizeControl m_StollCountertopThicknessControl = null;
        [SerializeField] protected CabinetOtherSizeControl m_CountertopThicknessControl = null;
        [SerializeField] protected CabinetOtherSizeControl m_CabinetDepthControl = null;

        [Space]
        [SerializeField] protected MelcherButton m_BackButton = null;
        [SerializeField] protected MelcherButton m_NextButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected CabinetSizesMode m_Mode;

        protected Kitchen _currentKitchen;

        /// <summary>
        ///     The depth of the cabinet when this UI was displayed. Only valid
        ///     when the UI was displayed with <see cref="CabinetSizesMode.ModifyExistingCabinet"/>.
        /// </summary>
        protected float _currentCabinetDepthInMeters;

        /// <summary>
        ///     The thickness of the countertop when this UI was displayed. Only
        ///     valid when the UI was displayed with <see cref="CabinetSizesMode.ModifyExistingCabinet"/>.
        /// </summary>
        protected float _currentCountertopThicknessInMeters;

        protected CountertopType _currentCountertopType;

        protected int _defaultCabinetDepthOptionIndex;

        protected int _defaultCountertopThicknessOptionIndex;

        protected int _defaultStollCountertopThicknessOptionIndex;

    }   // class CabinetOtherSizesUI

}   // namespace KitchenDesign.UI.DesignerScreens

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
