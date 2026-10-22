namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using System.Text;
    using System.Collections.Generic;
    using UnityEngine;
    using Cysharp.Threading.Tasks;

    /// <summary>
    ///     A label that displays the dimensions of a cabinet section. This is
    ///     intended to be displayed in the ticker bar, i.e., the screen area
    ///     at the top of the designer scene.
    /// </summary>
    public class CabinetTickerWidget : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public int targetCabinetSpan
        {
            get => m_TargetCabinetSpan;
            set => m_TargetCabinetSpan = value;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public void RefreshLabel()
        {
            m_StringBuilder.Clear();

            const string Separator = " <color=white>|</color> ";

            m_StringBuilder.Append("<mspace=").Append(m_MonoSpaceSize).Append(">");

            // Pro-Fit measures cabinets from the outside edge (our cabinet
            // class represents the inside edge). We need to find the cabinet's
            // neighbours and add their size to ours

            Kitchen kitchen = Kitchen.instance;

            CuboidPadding cabinetOverhangInMeters =
                kitchen.baseCountertopOverhangInMeters;

            Vector3 sizeInMeters = Vector3.zero;

            CabinetSpan span = kitchen.layout.spans[targetCabinetSpan];

            bool isFirstCabinetInSpan = true;

            foreach (ElementIndex<Cabinet> i in span.cabinets)
            {
                Cabinet cabinet = i.element;

                bool isCornerCabinet =
                    cabinet.cabinetType == CabinetType.Corner;

                if (isFirstCabinetInSpan && isCornerCabinet)
                {
                    // The corner section that starts a span is rotated 90
                    // degrees from the other cabinets in the span.
                    sizeInMeters.x += cabinet.sizeInMeters.z;
                }
                else
                {
                    Vector3 cabinetSizeInMeters = cabinet.sizeInMeters;
                    sizeInMeters.x += cabinetSizeInMeters.x;
                    sizeInMeters.y = cabinetSizeInMeters.y;
                    sizeInMeters.z = cabinetSizeInMeters.z;
                }

                if (isCornerCabinet &&
                    kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections)
                {
                    // The columns on the neighbouring span must be manually
                    // accounted for.
                    sizeInMeters.x += cabinetOverhangInMeters.towardMinZ;
                }

                isFirstCabinetInSpan = false;
            }

            // Display the measurements

            MeasurementUnit units = ProFitGlobals.userProfile.activeUnits;

            AppendMeasurement(m_StringBuilder, sizeInMeters.x, units, allowFeet: true, 'L');
            m_StringBuilder.Append(Separator);
            AppendMeasurement(m_StringBuilder, sizeInMeters.y, units, allowFeet: false, 'H');
            m_StringBuilder.Append(Separator);
            AppendMeasurement(m_StringBuilder, sizeInMeters.z, units, allowFeet: false, 'D');

            m_StringBuilder.Append("</mspace>");

            m_Label.phrase = m_StringBuilder.ToString();

            static void AppendMeasurement(
                StringBuilder stringBuilder,
                float distanceInMeters,
                MeasurementUnit units,
                bool allowFeet,
                char axisSuffix)
            {
                if (units == MeasurementUnit.FeetAndInches)
                {
                    ImperialUnit.FromTotalMeters(distanceInMeters)
                        .GetValuesUsingMaximumInchDivisions(maximumInchDivisions: ImperialUnit.DefaultMaximumInchDivision, out int feet, out int inches, out int numerator, out int denominator);

                    if (allowFeet)
                    {
                        if (feet > 0)
                        {
                            stringBuilder.AppendFormat("{0,2}'", feet);
                        }
                        else
                        {
                            // 3 spaces for: two digits places and a foot marker
                            // (apostrophe)
                            stringBuilder.Append(' ', 3);
                        }
                    }
                    else
                    {
                        inches += feet * Mathf.RoundToInt(Units.InchesPerFoot);
                    }

                    if (numerator > 0 && denominator > 0 && ProFitGlobals.appRules.ShowsTickerFractions)
                    {
                        stringBuilder.AppendFormat("{0,2} {1}/{2}\"", inches, numerator, denominator);
                    }
                    else
                    {
                        stringBuilder.AppendFormat("{0,2}\"", inches);
                    }
                }
                else if (units == MeasurementUnit.Centimeters)
                {
                    string distanceInCentimeters =
                        Utility.MeasurementFormat.FormatMetersToCentimeters(
                            distanceInMeters,
                            leftDecimalPlaces: 3
                        );

                    stringBuilder.Append(distanceInCentimeters + " ");
                }

                stringBuilder.Append(axisSuffix);
            }
        }

        protected void OnEnable()
        {
            EventBridge.Designer.onCabinetResized.OnInvoke += RefreshLabel;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke += RefreshLabel;
            EventBridge.Designer.onCabinetReordered.OnInvoke += OnCabinetReordered;
            EventBridge.Designer.onCabinetDestroyedByUser.OnInvoke += OnCabinetDestroyedByUser;
        }

        protected void OnDisable()
        {
            EventBridge.Designer.onCabinetResized.OnInvoke -= RefreshLabel;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke -= RefreshLabel;
            EventBridge.Designer.onCabinetReordered.OnInvoke -= OnCabinetReordered;
            EventBridge.Designer.onCabinetDestroyedByUser.OnInvoke -= OnCabinetDestroyedByUser;
        }

        protected async UniTask RefreshLabel(Cabinet cabinet)
        {
            // Refresh the label regardless of which cabinet is changed.
            // Now that we are measuring the cabinet's size using the outside
            // edge, the size of the other cabinets influences the displayed
            // measurement of this cabinet
            RefreshLabel();

            await UniTask.CompletedTask;
        }

        protected async UniTask OnCabinetReordered((Cabinet reorderedCabinet, int previousCabinetIndex, List<CabinetSpan> previousCabinetSpans) args)
        {
            RefreshLabel();

            await UniTask.CompletedTask;
        }

        protected async UniTask OnCabinetDestroyedByUser(int cabinetSpanIndex)
        {
            RefreshLabel();

            await UniTask.CompletedTask;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected int m_MonoSpaceSize = 22;
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_TextColor = default;
        [SerializeField] protected MelcherVariable<Color, MelcherColorAppValue> m_SeparatorColor = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        private MelcherLocalizedText m_Label = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        /// <summary>
        ///     The cabinet span number that this ticker is associated with.
        ///     A cabinet span is the group of cabinets that appears before
        ///     a corner.
        /// </summary>
        private int m_TargetCabinetSpan;

        private StringBuilder m_StringBuilder = new StringBuilder();

    }   // class CabinetTickerWidget

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
