namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    public static class Const
    {
        // =====================================================================
        // Measurements
        // =====================================================================

        public const float Inches2Meters = 0.0254f;
        public const float Meters2Inches = 1.0f / Inches2Meters;

        public const float Meters2Centimeters = 100.0f;
        public const float Centimeters2Meters = 1.0f / Meters2Centimeters;

        public const float InchesPerFoot = 12.0f;

        /// <summary>
        /// The minimum *horizontal* spacing between products. All products
        /// have their cabinet-space cuboid inflated by this amount along their
        /// width. The product model is shifted half this amount into their
        /// cabinet- space cuboid to keep it centered within the space it
        /// occupies.
        /// </summary>
        public const float OccupantMinimumHorizontalPaddingInMeters = 5.0f * Const.Inches2Meters;

        /// <summary>
        /// The minimum *vertical* spacing between products. This behaves
        /// identically to <see cref="OccupantMinimumHorizontalPaddingInMeters"/>
        /// by applies to the product's height.
        /// </summary>
        // TODO: Implement proper vertical padding. We need to shift products
        //       up because we measure product's position from the min-extent.
        public const float OccupantMinimumVerticalPaddingInMeters = 1.5f * Const.Inches2Meters;

        /// <summary>
        ///     The amount to shrink a cabinet's occupiable volume if it has
        ///     a corner. This minimum padding in corners is intended to be 6"
        ///     but we must compensate for the occupant's minimum padding.
        ///     Therefore this distance is 6" - (one half of an occupant's
        ///     padding).
        /// </summary>
        public const float CabinetCornerOccupiablePaddingInMeters = (6.0f * Inches2Meters) - (OccupantMinimumHorizontalPaddingInMeters * 0.5f);

        /// <summary>
        ///     The width of a cabinet column as a whole. In practice, the app
        ///     renders cabinet supports in halves, so two adjacent cabinets'
        ///     supports are used to render one whole.
        /// </summary>
        public const float CabinetColumnWidthInMeters = 3.0f * Const.Inches2Meters;

        public const float SectionBuildingMethodMinCabinetLengthInMeters = 18.0f * Const.Inches2Meters;

        public const float SectionBuildingMethodMaxCabinetLengthInMeters = 60.0f * Const.Inches2Meters;

        public const float MinMetersFromEdgeAfterNudge = 2.0f * Const.Inches2Meters;

        /// <summary>
        ///     Within Sections building method, we limit the number of
        ///     straight cabinets that can appear side-by-side for a given
        ///     span. This ensures spans do not become too long for the PDF
        ///     screenshots.
        /// </summary>
        public const int MaxStraightCabinetsPerSpan = 10;

        /// <summary>
        ///     The distance an inset toe kick is shifted inward from the front
        ///     of the cabinet.
        /// </summary>
        public const float ToeKickInsetInMeters = 1.5f * Const.Inches2Meters;

        // =====================================================================
        // Styling
        // =====================================================================

        public static readonly Color ErrorColor = new Color32(0xDD, 0x2C, 0x00, 0xFF);

        // ====================================================================
        // PlayerPrefs
        // ====================================================================

        public const string PlayerPrefs_Token = "Token";

        // ====================================================================
        // Unity
        // ====================================================================

        /// <summary>
        /// Class to contain symbolic names for tags. Nicer than using string
        /// literals.
        /// </summary>
        public static class Tag
        {
            public const string EditorOnly = "EditorOnly";
        }

        // ====================================================================
        // Stoll
        // ====================================================================

        public static class Stoll
        {
            /// <summary>
            ///     This is the trigger threshold for occupants that require
            ///     the cabinet's size to be limited. When an occupant's width
            ///     is larger than this, it can cause bowing on cabinets if
            ///     they are much longer than the occupant itself.
            /// </summary>
            public const float MinimumOccupantWidthInMetersCausingBowing = 15.0f * Const.Inches2Meters;

            /// <summary>
            ///     This is the maximum distance from an occupant the left and
            ///     right edge of a cabinet/shelf may be. This is to prevent
            ///     bowing.
            /// </summary>
            public const float MaximumUnoccupiedPaddingInMeters = 8.0f * Const.Inches2Meters;

            public const float DefaultCabinetDepth = 29.0f;

            public const float FixedSmokerCabinetCutOutMetersToKeepBelowCountertop = 2.0f * Const.Inches2Meters;

        }

        // ====================================================================
        // Gemini
        // ====================================================================

        public static class Gemini
        {
            public const float FridgePanelWidthInMeters = 1.375f * Const.Inches2Meters;   // A fridge panel is 1 3/8"

            public static class Challenger
            {

                /// <summary>
                ///     An override for Challenger. They require more padding
                ///     than other products.
                ///     <br /><br />
                ///
                ///     <inheritdoc cref="Const.OccupantMinimumHorizontalPaddingInMeters"/>
                /// </summary>
                public const float OccupantMinimumHorizontalPaddingInMeters = 4.0f * Const.Inches2Meters;
            }

            public static class ManufactersIds
            {
                public const int AgoraBerlinGardens = 109;
                public const int Endura = 100;
                public const int AmericanOutdoorCabinets = 112;
                public const int Toppin = 114;
                public const int WeatherStrong = 92;
                public const int WeatherStrongSelect = 141;
                public const int Naturekast = 134;
            }

        }

        // ====================================================================
        // Unity Editor
        // ====================================================================
        #region Unity Editor
        public const int DialogResult_OK = 0;
        public const int DialogResult_Cancel = 1;
        public const int DialogResult_Alt = 2;
        #endregion  // Unity Editor


    }   // class Const

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
