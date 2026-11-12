namespace KitchenDesign
{
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Enums;

    public readonly struct WhitelistRowEntry
    {
        public WhitelistRowEntry(ProductCategory productCategory)
        {
            rowType = WhitelistRowType.Header;
            manufacturerProductCategory = productCategory;
            manufacturerId = -1;
        }

        public WhitelistRowEntry(ManufacturerInfo manufacturer)
        {
            rowType = WhitelistRowType.ManufacturerToggle;
            manufacturerProductCategory = default;
            manufacturerId = manufacturer.id;
        }

        public readonly WhitelistRowType rowType;
        public readonly ProductCategory manufacturerProductCategory;
        public readonly int manufacturerId;
    }
}
