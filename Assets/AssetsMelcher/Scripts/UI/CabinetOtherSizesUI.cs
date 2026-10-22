namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using System.Linq;
    using UnityEngine;

    public class CabinetOtherSizesUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetMode(Mode mode, CabinetLayout layout)
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
            KitchenBuildingMethod buildingMethod = GetKitchenBuildingMethod();

            bool displayCabinetDepthOption =
                buildingMethod == KitchenBuildingMethod.Standard &&
                (mode == Mode.InitialCabinetSetup || cornerCabinetCount < 1);

            // Hide the cabinet depth control when Section mode is enabled.
            // This was requested by Stoll.
            m_CabinetDepthControl.gameObject.SetActive(displayCabinetDepthOption);

            m_CurrentKitchen = null;

            if (m_Mode == Mode.ModifyExistingCabinet)
            {
                // Make the sliders display the kitchen's current size.

                m_CurrentKitchen = Kitchen.instance;

                m_CurrentCabinetDepthInMeters = m_CurrentKitchen.baseCabinetDepthInMeters;
                m_CurrentCountertopThicknessInMeters = m_CurrentKitchen.countertopThicknessInMeters;
                m_CurrentCountertopType = m_CurrentKitchen.selectedCountertopType;

                CabinetOtherSizeControl.SelectOptionMatchingCurrentSize(
                    m_CabinetDepthControl,
                    m_CurrentKitchen.baseCabinetDepthInMeters
                );

                if (m_CurrentKitchen.selectedCountertopType == CountertopType.Default)
                {
                    CabinetOtherSizeControl.SelectOptionMatchingCurrentSize(
                        m_CountertopThicknessControl,
                        m_CurrentKitchen.countertopThicknessInMeters
                    );
                }
                else
                {
                    CabinetOtherSizeControl.SelectOptionMatchingCurrentSize(
                        m_StollCountertopThicknessControl,
                        m_CurrentKitchen.countertopThicknessInMeters
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

            m_StollCountertopThicknessControl.SelectOption(m_DefaultStollCountertopThicknessOptionIndex, notify: false);
            m_CountertopThicknessControl.SelectOption(m_DefaultCountertopThicknessOptionIndex, notify: false);
            m_CabinetDepthControl.SelectOption(m_DefaultCabinetDepthOptionIndex, notify: false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_BackButton.onClick += NavigateBackward;
            m_NextButton.onClick += GoNext;

            EventBridge.Designer.onBuildingMethodChanged += OnBuildingMethodChanged;
            m_StollCountertopThicknessControl.isEnabledToggle.toggleGroup.onToggleChanged += OnCountertopTypeChanged;
            m_CountertopThicknessControl.onValueChanged += OnCountertopThicknessChanged;
            m_StollCountertopThicknessControl.onValueChanged += OnCountertopThicknessChanged;
            m_CabinetDepthControl.onValueChanged += OnCabinetDepthChanged;
        }

        protected override void DoShow()
        {
            if (m_Mode == Mode.InitialCabinetSetup)
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

            EventBridge.Designer.onBuildingMethodChanged -= OnBuildingMethodChanged;
            m_StollCountertopThicknessControl.isEnabledToggle.toggleGroup.onToggleChanged -= OnCountertopTypeChanged;
            m_CountertopThicknessControl.onValueChanged -= OnCountertopThicknessChanged;
            m_StollCountertopThicknessControl.onValueChanged -= OnCountertopThicknessChanged;
            m_CabinetDepthControl.onValueChanged -= OnCabinetDepthChanged;
        }

        protected void NavigateBackward(MelcherButton button)
        {
            Hide();

            if (m_Mode == Mode.InitialCabinetSetup)
            {
                EventBridge.Designer.showCabinetShapeUi.Invoke();
            }
            else
            {
                // Restore the original kitchen size.

                m_CurrentKitchen.SetBaseCabinetDepth(m_CurrentCabinetDepthInMeters);
                m_CurrentKitchen.SetCountertopThickness(m_CurrentCountertopThicknessInMeters);
                EventBridge.Designer.setCountertopType?.Invoke(m_CurrentCountertopType);
            }
        }

        protected void GoNext(MelcherButton caller)
        {
            Hide();

            if (m_Mode == Mode.InitialCabinetSetup)
            {
                float countertopThicknessInMeters =
                    m_CountertopThicknessControl.value.sizeInMeters;

                if (GetKitchenBuildingMethod() == KitchenBuildingMethod.Sections &&
                    m_StollCountertopThicknessControl.isEnabledToggle.isOn)
                {
                    countertopThicknessInMeters = m_StollCountertopThicknessControl.value.sizeInMeters;
                }

                CuboidPadding countertopOverhangInInches = CuboidPadding.one;

                if (GetKitchenBuildingMethod() == KitchenBuildingMethod.Sections)
                {
                    countertopOverhangInInches *= 0.5f;
                }

                float baseCabinetDepthInMeters = m_CabinetDepthControl.value.sizeInMeters;
                if (Kitchen.instance.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
                {
                    baseCabinetDepthInMeters = Kitchen.instance.selectedBrandCatalog.standardCabinetDepthInMeters;
                }

                EventBridge.Designer.setBaseCabinetDepth.Invoke(baseCabinetDepthInMeters);
                EventBridge.Designer.setCountertopThickness.Invoke(countertopThicknessInMeters);
                EventBridge.Designer.setCountertopOverhang.Invoke(countertopOverhangInInches * Units.Inches2Meters);
                EventBridge.Designer.createKitchen.Invoke().Forget();
            }
            else
            {
                m_CurrentKitchen.RefreshTickerLabels();

                bool hasCorner =
                    m_CurrentKitchen.layout.EnumerateCabinets()
                        .Any(x => x.element.cabinetType == CabinetType.Corner);

                if (hasCorner && !MelcherCommon.IsAlmostEqual(m_CurrentKitchen.baseCabinetDepthInMeters, m_CurrentCabinetDepthInMeters, tolerance: 0.001f))
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

                    foreach (CabinetSpan span in m_CurrentKitchen.layout.spans)
                    {
                        float widthInMeters =
                            span.GetOutsideCuboidInSpanSpace().width;

                        string widthAsText = null;

                        if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
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

                    EventBridge.Designer.showNotification?.Invoke(
                        title: "Depth Changed",
                        message: message,
                        onDismiss: null
                    );
                }
            }
        }

        protected void OnBuildingMethodChanged(KitchenBuildingMethod newBuildingMethod)
        {
            bool isSectionsMethod =
                newBuildingMethod == KitchenBuildingMethod.Sections;

            // Stoll has a second countertop thickness control.
            m_StollCountertopThicknessControl.gameObject.SetActive(isSectionsMethod);

            m_CountertopThicknessControl.useIsEnabledToggle = isSectionsMethod;
            m_StollCountertopThicknessControl.useIsEnabledToggle = isSectionsMethod;

            if (isSectionsMethod)
            {
                // Custom countertop thicknesses when in Sections method (Stoll).
                m_DefaultCountertopThicknessOptionIndex =
                    m_CountertopThicknessControl.SetOptions(
                        defaultIndex: 3,
                        new CabinetOtherSizeControl.Option(0.25f),
                        new CabinetOtherSizeControl.Option(0.50f),
                        new CabinetOtherSizeControl.Option(0.75f),
                        new CabinetOtherSizeControl.Option(1.00f),
                        new CabinetOtherSizeControl.Option(1.25f),
                        new CabinetOtherSizeControl.Option(1.50f),
                        new CabinetOtherSizeControl.Option(1.75f),
                        new CabinetOtherSizeControl.Option(2.00f),
                        new CabinetOtherSizeControl.Option(3.25f),
                        new CabinetOtherSizeControl.Option(3.50f),
                        new CabinetOtherSizeControl.Option(3.75f),
                        new CabinetOtherSizeControl.Option(4.00f)
                    );
            }
            else
            {
                m_DefaultCountertopThicknessOptionIndex =
                    m_CountertopThicknessControl.SetOptions(
                        defaultIndex: 3,
                        new CabinetOtherSizeControl.Option(0.5f),
                        new CabinetOtherSizeControl.Option(0.75f),
                        new CabinetOtherSizeControl.Option(1.0f),
                        new CabinetOtherSizeControl.Option(1.25f),
                        new CabinetOtherSizeControl.Option(1.5f),
                        new CabinetOtherSizeControl.Option(1.75f),
                        new CabinetOtherSizeControl.Option(2.0f)
                    );
            }

            m_DefaultCabinetDepthOptionIndex =
                CabinetOtherSizeControl.SetupCabinetDepthSizes(m_CabinetDepthControl, newBuildingMethod);

            // The Stoll countertop thickness always uses these values.
            // However, this control will be completely hidden unless Sections
            // method is used.
            m_DefaultStollCountertopThicknessOptionIndex =
                m_StollCountertopThicknessControl.SetOptions(
                    defaultIndex: 3,
                    new CabinetOtherSizeControl.Option(1.25f),
                    new CabinetOtherSizeControl.Option(2.0f),
                    new CabinetOtherSizeControl.Option(3.0f),
                    new CabinetOtherSizeControl.Option(4.0f)
                );

            ClearForm();
        }

        protected KitchenBuildingMethod GetKitchenBuildingMethod()
        {
            return Kitchen.instance.selectedBuildingMethod;
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

            EventBridge.Designer.setCountertopType?.Invoke(countertopType);
            EventBridge.Designer.setCountertopThickness(countertopThicknessInMeters);
        }

        protected void OnCountertopThicknessChanged(CabinetOtherSizeControl.Option selectedOption)
        {
            if (m_Mode == Mode.ModifyExistingCabinet)
            {
                MelcherLog.Breadcrumb("Cabinet countertop thickness changed to: " + selectedOption.displayLabel);

                m_CurrentKitchen.SetCountertopThickness(selectedOption.sizeInMeters);
            }
        }

        protected void OnCabinetDepthChanged(CabinetOtherSizeControl.Option selectedOption)
        {
            if (m_Mode == Mode.ModifyExistingCabinet)
            {
                MelcherLog.Log("Cabinet depth changed to: " + selectedOption.displayLabel);

                // Test if the new size works.
                foreach (CabinetSpan span in m_CurrentKitchen.layout.spans)
                {
                    if (!span.CanResizeBaseCabinetDepth(selectedOption.sizeInMeters))
                    {
                        return;
                    }
                }

                // Clear the previous toast in case it was the message saying
                // the cabinet depth size cannot be used.
                EventBridge.Designer.showToast?.Invoke("", holdDuration: 0.0f);
                m_CurrentKitchen.SetBaseCabinetDepth(selectedOption.sizeInMeters);
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

        protected Mode m_Mode;

        protected Kitchen m_CurrentKitchen;

        /// <summary>
        ///     The depth of the cabinet when this UI was displayed. Only valid
        ///     when the UI was displayed with <see cref="Mode.ModifyExistingCabinet"/>.
        /// </summary>
        protected float m_CurrentCabinetDepthInMeters;

        /// <summary>
        ///     The thickness of the countertop when this UI was displayed. Only
        ///     valid when the UI was displayed with <see cref="Mode.ModifyExistingCabinet"/>.
        /// </summary>
        protected float m_CurrentCountertopThicknessInMeters;

        protected CountertopType m_CurrentCountertopType;

        protected int m_DefaultCabinetDepthOptionIndex;

        protected int m_DefaultCountertopThicknessOptionIndex;

        protected int m_DefaultStollCountertopThicknessOptionIndex;

        // =====================================================================
        // Nested types
        // =====================================================================

        public enum Mode
        {
            InitialCabinetSetup,

            ModifyExistingCabinet,

        }   // enum Mode

    }   // class CabinetOtherSizesUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-30
/// ===========================================================================
