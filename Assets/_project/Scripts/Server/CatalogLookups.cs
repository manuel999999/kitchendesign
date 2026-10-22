using System;
using System.Collections.Generic;

namespace KitchenDesign.Server
{
    public static class CatalogLookups
    {
        public static bool FindManufacturerByName(IReadOnlyList<ManufacturerInfo> manufacturers, string manufacturerNameToFind, out ManufacturerInfo manufacturer)
        {
            int manufacturerCount = manufacturers.Count;

            for (int i = 0; i < manufacturerCount; ++i)
            {
                if (string.Equals(manufacturers[i].name, manufacturerNameToFind, StringComparison.OrdinalIgnoreCase))
                {
                    manufacturer = manufacturers[i];
                    return true;
                }
            }

            manufacturer = default;
            return false;
        }

        public static bool FindProductGroupByName(IReadOnlyList<ProductGroupInfo> productGroups, string groupNameToFind, out ProductGroupInfo group)
        {
            IReadOnlyList<ProductGroupInfo> productGroupList = productGroups ?? Array.Empty<ProductGroupInfo>();

            int productGroupCount = productGroupList.Count;
            for (int i = 0; i < productGroupCount; ++i)
            {
                if (string.Equals(productGroupList[i].name, groupNameToFind, StringComparison.OrdinalIgnoreCase))
                {
                    group = productGroupList[i];
                    return true;
                }
            }

            group = default;
            return false;
        }

        public static bool FindCabinetSectionCategoryByName(
            IReadOnlyList<CabinetSectionCategoryInfo> categoryList,
            string categoryNameToFind,
            out CabinetSectionCategoryInfo cabinetSectionCategory)
        {
            int categoryCount = categoryList.Count;

            for (int i = 0; i < categoryCount; ++i)
            {
                if (string.Equals(categoryList[i].name, categoryNameToFind, StringComparison.OrdinalIgnoreCase))
                {
                    cabinetSectionCategory = categoryList[i];
                    return true;
                }
            }

            cabinetSectionCategory = default;
            return false;
        }

        public static bool FindCabinetSectionCategoryById(
            IReadOnlyList<CabinetSectionCategoryInfo> categoryList,
            int categoryIdToFind,
            out CabinetSectionCategoryInfo cabinetSectionCategory)
        {
            int categoryCount = categoryList.Count;
            for (int i = 0; i < categoryCount; ++i)
            {
                if (categoryList[i].id == categoryIdToFind)
                {
                    cabinetSectionCategory = categoryList[i];
                    return true;
                }
            }

            cabinetSectionCategory = default;
            return false;
        }
    }
}
