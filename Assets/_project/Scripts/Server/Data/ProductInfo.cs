namespace KitchenDesign.Server
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using System.Collections.Generic;
    using UnityEngine;
    using KitchenDesign.Geometry;

    public class ProductInfo
    {
        public ProductInfo()
        {
        }

        public ProductInfo(in ProductInfo toClone)
        {
            id                      = toClone.id;
            sortOrder               = toClone.sortOrder;
            sku                     = toClone.sku;
            manufacturerId          = toClone.manufacturerId;
            groupId                 = toClone.groupId;
            baseProductId           = toClone.baseProductId;
            name                    = toClone.name;
            description             = toClone.description;
            category                = toClone.category;
            priceDealer             = toClone.priceDealer;
            priceCustomer           = toClone.priceCustomer;
            cutOutSizeInMeters      = toClone.cutOutSizeInMeters;
            productSizeInMeters     = toClone.productSizeInMeters;
            anchorOffsetInMeters    = toClone.anchorOffsetInMeters;
            storeLink               = toClone.storeLink;
            visualId                = toClone.visualId;
            hasErrors               = toClone.hasErrors;
            productPaddingInMeters  = toClone.productPaddingInMeters;
            minWidthInMeters        = toClone.minWidthInMeters;
            maxWidthInMeters        = toClone.maxWidthInMeters;
            cabinetSectionCategoryId= toClone.cabinetSectionCategoryId;
            compatibilityId         = toClone.compatibilityId;
            doLayoutUsingProductSize = toClone.doLayoutUsingProductSize;
            productTagIds           = new (toClone.productTagIds);
            doesExistInServerDashboard = toClone.doesExistInServerDashboard;

            Debug.Assert(toClone.sourceProductInfo == null, "Not expecting to clone a clone.");
            sourceProductInfo       = toClone;
        }

        public int id;

        public int sortOrder;

        public string sku;

        public int manufacturerId;

        public int groupId;

        public int baseProductId;

        public string name;

        public string description;

        public ProductCategory category;

        public float priceDealer;

        public float priceCustomer;

        public Vector3 cutOutSizeInMeters;

        public Vector3 productSizeInMeters;

        public float anchorOffsetInMeters;

        public string storeLink;

        public string visualId;

        public bool hasErrors;

        public CuboidPadding productPaddingInMeters;

        public float? minWidthInMeters;

        public float? maxWidthInMeters;

        public int cabinetSectionCategoryId;

        public int compatibilityId;

        public bool doLayoutUsingProductSize;

        public HashSet<int> productTagIds = new();

        public bool doesExistInServerDashboard;

        public ProductInfo sourceProductInfo;

#if UNITY_EDITOR

        static ProductInfo()
        {
            const int ExpectedFieldCount = 26;

            int actualFieldCount =
                typeof(ProductInfo).GetFields().Length;

            MelcherLog.Assert(
                actualFieldCount == ExpectedFieldCount,
                $"Ensure {MelcherFormat.ConsoleTypeName<ProductInfo>()}'s " +
                $"copy constructor copies all fields and then update " +
                $"{MelcherFormat.ConsoleSymbolName(nameof(ExpectedFieldCount))} " +
                $"with the correct field count. " +
                $"Expected {MelcherFormat.ConsoleNumericValue(ExpectedFieldCount)} " +
                $"but found {MelcherFormat.ConsoleNumericValue(actualFieldCount)}.",
                onFalse: static () => Debug.Break()
            );
        }
#endif
    }
}
