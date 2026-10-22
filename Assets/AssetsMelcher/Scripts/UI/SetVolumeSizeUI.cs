namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using TMPro;
    using UnityEngine;

    /// <summary>
    ///     A dialog with a textbox where the user can type a measurement for
    ///     to resize a given <see cref="OccupiableVolume"/>'s width or adjust
    ///     a slider to change its depth.
    /// </summary>
    public class SetVolumeSizeUI : MelcherUI
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void ShowPrompt(OccupiableVolume targetVolume)
        {
            if (targetVolume == null)
            {
                throw new System.ArgumentNullException(nameof(targetVolume));
            }

            m_TargetVolume = targetVolume;
            m_CurrentVolumeDepthInMeters = targetVolume.resizerCuboid.depth;
            m_MessageBox.Clear();

            // Setup width control.

            float displayedWidthInMeters =
                GetDisplayedVolumeWidthInMeters(targetVolume);

            string widthAsText = null;

            if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
            {
                widthAsText = Utility.MeasurementFormat.FormatMetersToCentimeters(displayedWidthInMeters);
            }
            else
            {
                widthAsText =
                    Utility.MeasurementFormat.FormatMetersToImperial(
                        displayedWidthInMeters
                    );
            }

            m_WidthInputField.text = widthAsText;

            m_DisplayedWidthMetersDelta =
                displayedWidthInMeters - targetVolume.resizerCuboid.width;

            // Setup depth control.

            bool showCabinetDepthOption = false;

            if (targetVolume is Cabinet cabinet && !cabinet.Is(CabinetType.CornerWedge))
            {
                IKitchen kitchen = cabinet.kitchen;

                showCabinetDepthOption =
                    kitchen.selectedBuildingMethod == KitchenBuildingMethod.Standard;

                CabinetOtherSizeControl.SetupCabinetDepthSizes(
                    m_CabinetDepthControl,
                    kitchen.selectedBuildingMethod
                );

                CabinetOtherSizeControl.SelectOptionMatchingCurrentSize(
                    m_CabinetDepthControl,
                    cabinet.sizeInMeters.z
                );
            }

            m_CabinetDepthControlColumn.gameObject.SetActive(showCabinetDepthOption);

            Show();
        }

        protected override void Awake()
        {
            m_CabinetDepthControl.useIsEnabledToggle = false;

            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EventBridge.Designer.showSetOccupiableVolumeWidthPrompt += ShowPrompt;

            m_CabinetDepthControl.onValueChanged += OnCabinetDepthChanged;

            m_CancelButton.onClick += OnClickCancel;
            m_ConfirmButton.onClick += OnClickConfirm;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EventBridge.Designer.showSetOccupiableVolumeWidthPrompt -= ShowPrompt;

            m_CabinetDepthControl.onValueChanged -= OnCabinetDepthChanged;

            m_CancelButton.onClick -= OnClickCancel;
            m_ConfirmButton.onClick -= OnClickConfirm;
        }

        protected void OnCabinetDepthChanged(CabinetOtherSizeControl.Option selectedOption)
        {
            if (m_TargetVolume is Cabinet cabinet)
            {
                CabinetSpan cabinetSpan =
                    cabinet.kitchen.layout.FindCabinetSpan(cabinet);

                if (cabinetSpan.CanResizeBaseCabinetDepth(selectedOption.sizeInMeters))
                {
                    EventBridge.Designer.showToast?.Invoke("", holdDuration: 0.0f);
                    cabinetSpan.SetBaseCabinetDepth(selectedOption.sizeInMeters);
                }
            }
        }

        protected void OnClickCancel(MelcherButton caller)
        {
            // Revert to the original cabinet depth when canceled.
            if (m_TargetVolume is Cabinet cabinet)
            {
                CabinetSpan cabinetSpan =
                    cabinet.kitchen.layout.FindCabinetSpan(cabinet);

                if (cabinetSpan.CanResizeBaseCabinetDepth(m_CurrentVolumeDepthInMeters))
                {
                    EventBridge.Designer.showToast?.Invoke("", holdDuration: 0.0f);
                    cabinetSpan.SetBaseCabinetDepth(m_CurrentVolumeDepthInMeters);
                }
            }

            Hide();
        }

        protected void OnClickConfirm(MelcherButton caller)
        {
            m_MeasurementParsingErrors.Clear();
            m_MessageBox.Clear();

            string userInputMeasurementAsText = m_WidthInputField.text.Trim();

            float displayedUserInputWidthInMeters = 0.0f;

            string errorMessage = "Unknown error.";

            if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
            {
                Match extractedNumber =
                    Regex.Match(userInputMeasurementAsText, @"(?:\d*\.?\d*){1,}");

                if (float.TryParse(extractedNumber.Value, out float userInputMeasurementInCentimeters))
                {
                    errorMessage = null;

                    displayedUserInputWidthInMeters =
                        userInputMeasurementInCentimeters * Units.Centimeters2Meters;
                }
            }
            else
            {
                bool didParse =
                    Utility.MeasurementFormat.TryParseImperialString(
                        userInputMeasurementAsText,
                        out displayedUserInputWidthInMeters,
                        "Cabinet Width",
                        m_MeasurementParsingErrors
                    );

                if (didParse)
                {
                    errorMessage = null;
                }
                else if (m_MeasurementParsingErrors.Count > 0)
                {
                    errorMessage = string.Join("\n", m_MeasurementParsingErrors);
                }
            }

            if (errorMessage != null)
            {
                m_MessageBox.Error(errorMessage);
                return;
            }

            const float Tolerance = 0.001f;

            bool isNewSizeEntered =
                !MelcherCommon.IsAlmostEqual(
                    displayedUserInputWidthInMeters,
                    GetDisplayedVolumeWidthInMeters(m_TargetVolume),
                    Tolerance
                );

            if (isNewSizeEntered)
            {
                // Convert the "displayed" length into the actual length of
                // just the volume itself. In Sections building method,
                // the length of adjacent cabinets are included in the
                // "displayed" length.
                float actualUserInputWidthInMeters =
                    displayedUserInputWidthInMeters - m_DisplayedWidthMetersDelta;

                Cabinet targetCabinet = m_TargetVolume as Cabinet;

                if (targetCabinet)
                {
                    if (actualUserInputWidthInMeters < targetCabinet.minSizeInMeters.x)
                    {
                        float minSizeInMeters =
                            targetCabinet.minSizeInMeters.x + m_DisplayedWidthMetersDelta;

                        string minSizeInUserUnits;

                        if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                        {
                            minSizeInUserUnits = Utility.MeasurementFormat.FormatMetersToCentimeters(minSizeInMeters);
                        }
                        else
                        {
                            minSizeInUserUnits = Utility.MeasurementFormat.FormatMetersToImperial(minSizeInMeters);
                        }

                        m_MessageBox.Error(
                            $"Cannot make this cabinet smaller than:\n" +
                            $"{minSizeInUserUnits}"
                        );

                        return;
                    }

                    if (actualUserInputWidthInMeters > targetCabinet.maxSizeInMeters.x)
                    {
                        float maxSizeInMeters =
                            targetCabinet.maxSizeInMeters.x + m_DisplayedWidthMetersDelta;

                        string maxSizeInUserUnits;

                        if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                        {
                            maxSizeInUserUnits = Utility.MeasurementFormat.FormatMetersToCentimeters(maxSizeInMeters);
                        }
                        else
                        {
                            maxSizeInUserUnits = Utility.MeasurementFormat.FormatMetersToImperial(maxSizeInMeters);
                        }

                        m_MessageBox.Error(
                            $"Cannot make this cabinet larger than:\n" +
                            $"{maxSizeInUserUnits}"
                        );

                        return;
                    }
                }

                // Calculate the width change relative to the volume's current
                // width because TryToResizeLengthAsUser wants a value that
                // represents the delta from the volume's current size.
                float widthDeltaInMeters =
                    actualUserInputWidthInMeters - m_TargetVolume.resizerCuboid.width;

                if (!m_TargetVolume.TryToResizeLengthAsUser(ref widthDeltaInMeters, Extent.Max))
                {
                    string requestedSizeInUserUnits;

                    if (ProFitGlobals.userProfile.activeUnits == MeasurementUnit.Centimeters)
                    {
                        requestedSizeInUserUnits = Utility.MeasurementFormat.FormatMetersToCentimeters(displayedUserInputWidthInMeters);
                    }
                    else
                    {
                        requestedSizeInUserUnits = Utility.MeasurementFormat.FormatMetersToImperial(displayedUserInputWidthInMeters);
                    }

                    m_MessageBox.Error(
                        $"Products in the way. Cannot set cabinet size to:\n" +
                        $"{requestedSizeInUserUnits}"
                    );

                    return;
                }

                if (targetCabinet)
                {
                    EventBridge.Designer.onCabinetResized.Invoke(targetCabinet).Forget();
                }
            }

            Hide();
        }

        protected float GetDisplayedVolumeWidthInMeters(OccupiableVolume volume)
        {
            float displayedVolumeWidthInMeters = volume.resizerCuboid.width;

            if (volume is Cabinet cabinet &&
                cabinet.kitchen.selectedBuildingMethod == KitchenBuildingMethod.Standard)
            {
                // In Standard building method, the length displayed must be
                // the full outside measurement which includes any attached
                // corner sections. We can grab this initial size from the
                // cabinet's span.

                MelcherLog.Assert(
                    cabinet.cabinetType != CabinetType.Corner,
                    $"{nameof(SetVolumeSizeUI)} was built with the assumption that it would not affect " +
                    $"corner cabinets. Corner cabinets pose a challenge because they can belong to " +
                    $"multiple cabinet spans."
                );

                CabinetSpan targetCabinetSpan =
                    cabinet.kitchen.layout.FindCabinetSpan(cabinet);

                displayedVolumeWidthInMeters =
                    targetCabinetSpan.GetOutsideCuboidInSpanSpace().width;
            }

            return displayedVolumeWidthInMeters;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [Header(nameof(DesignerSaveUI))]

        [SerializeField] protected TMP_InputField m_WidthInputField = null;
        [SerializeField] protected Transform m_CabinetDepthControlColumn = null;
        [SerializeField] protected CabinetOtherSizeControl m_CabinetDepthControl = null;
        [SerializeField] protected InlineMessageBox m_MessageBox = null;
        [SerializeField] protected MelcherButton m_CancelButton = null;
        [SerializeField] protected MelcherButton m_ConfirmButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected List<string> m_MeasurementParsingErrors = new();

        protected OccupiableVolume m_TargetVolume;

        /// <summary>
        ///     The number of meters that the "display width" differs from the
        ///     actual cuboid width.
        /// </summary>
        protected float m_DisplayedWidthMetersDelta;

        /// <summary>
        ///     The initial depth of the volume when the prompt was shown.
        /// </summary>
        protected float m_CurrentVolumeDepthInMeters;

    }   // class SetVolumeWidthUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-04-17
/// ===========================================================================
