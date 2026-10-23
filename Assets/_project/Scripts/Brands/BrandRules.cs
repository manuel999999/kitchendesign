using KitchenDesign.Shared.Common;

namespace KitchenDesign.Brands
{
    public readonly struct BrandRules
    {
        public const float ChallengerOccupantMinimumHorizontalPaddingInMeters = 4.0f * Units.Inches2Meters;

        private const int WeatherStrong = 92;
        private const int Endura = 100;
        private const int AgoraBerlinGardens = 109;
        private const int AmericanOutdoorCabinets = 112;
        private const int Toppin = 114;
        private const int Naturekast = 134;
        private const int WeatherStrongSelect = 141;

        private readonly int _cabinetManufacturerId;

        public BrandRules(int cabinetManufacturerId)
        {
            this._cabinetManufacturerId = cabinetManufacturerId;
        }

        public bool PricesToeKicks => this._cabinetManufacturerId != AgoraBerlinGardens;

        public bool PricesFridgeBackPanels =>
            this._cabinetManufacturerId != AmericanOutdoorCabinets &&
            this._cabinetManufacturerId != Endura;

        public bool PricesFridgeEndPanels =>
            this._cabinetManufacturerId != AgoraBerlinGardens &&
            this._cabinetManufacturerId != Toppin;

        public bool PricesCountertopsFromCatalog => this._cabinetManufacturerId == AgoraBerlinGardens;

        public bool PricesCornersAsOwnProduct => this._cabinetManufacturerId == AgoraBerlinGardens;

        public bool ForcesKamadoGrillEndPanels => this._cabinetManufacturerId == AgoraBerlinGardens;

        public bool FridgePanelSkuIncludesCabinetSize => this._cabinetManufacturerId == AgoraBerlinGardens;

        public bool PricesAmericanOutdoorBaseEnds => this._cabinetManufacturerId == AmericanOutdoorCabinets;

        public bool PricesBaseEnds =>
            this._cabinetManufacturerId != Endura &&
            this._cabinetManufacturerId != AgoraBerlinGardens;

        public bool UsesEnduraToeKick => this._cabinetManufacturerId == Endura;

        public float? HeatedOrBurnerProductSidePaddingInMeters(bool isSmoker)
        {
            switch (this._cabinetManufacturerId)
            {
                case AgoraBerlinGardens:
                    return isSmoker ? 0f : 2.25f * Units.Inches2Meters;
                case Toppin:
                case Endura:
                case WeatherStrong:
                case WeatherStrongSelect:
                case Naturekast:
                    return 3f * Units.Inches2Meters;
                default:
                    return null;
            }
        }
    }
}
