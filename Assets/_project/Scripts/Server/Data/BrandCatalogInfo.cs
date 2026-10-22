using System.Collections.Generic;

namespace KitchenDesign.Server
{
    public struct BrandCatalogInfo
    {
        public int id;
        public string name;
        public int cabinetManufacturerId;
        public List<int> availableManufacturerIds;
        public List<int> availableGroupIds;
        public List<int> startingCabinetSectionServerIds;
        public List<int> allowedCabinetSectionServerIds;
    }
}
