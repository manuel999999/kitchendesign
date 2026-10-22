using System.Collections.Generic;

namespace KitchenDesign.Server
{
    public struct ManufacturerInfo
    {
        public int id;
        public string name;

        public int order;

        public ProductCategory? productCategory;

        public List<ManufacturerDoorStyleInfo> doorStyles;
    }
}
