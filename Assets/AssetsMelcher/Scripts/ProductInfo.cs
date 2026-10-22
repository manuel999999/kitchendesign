namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// This is a product definition as it is stored on the server back-end.
    /// </summary>
    public class ProductInfo
    {
        public ProductInfo()
        {
        }

        /// <summary>
        ///     Deep clones a product. This is intended to clone entries from
        ///     <see cref="ProFitGlobals.sourceProducts"/> into <see
        ///     cref="ProFitGlobals.runtimeProducts"/>, where the clones can be
        ///     safely modified without affecting the originals.
        /// </summary>
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

        /// <summary>
        ///     The server's insert ID of the product. This indicates the order
        ///     products were added in the back-end and is a more reliable way
        ///     of referring to products as it cannot be changed by dashboard
        ///     users. You may also be looking for <see cref="sku"/>.
        /// </summary>
        public int id;

        /// <summary>
        ///     The sort order this item should appear when it is displayed in
        ///     the designer's product toolbar.
        /// </summary>
        public int sortOrder;

        /// <summary>
        ///     A human readable SKU. It was used as the product identifier in
        ///     legacy save files until we learned that it was not guaranteed
        ///     to remain static. This is what the kitchen script would use to
        ///     refer to a specific product.
        /// </summary>
        public string sku;

        /// <summary>
        /// The ID of the manufacturer. Can be used to lookup information in the
        /// Manufacturer Visual Catalog.
        /// <br /><br />
        ///
        /// We avoid holding a string variable of the manufacturer's name
        /// because it leads to wasted memory. The string is likely to be
        /// similar to many other products' manufacturer name, yet each product
        /// would redundantly allocate and hold a reference to a unique string.
        /// </summary>
        public int manufacturerId;

        /// <summary>
        /// The ID of the product group this product belongs to. This is 0 if
        /// the product does not belong to a group.
        /// </summary>
        public int groupId;

        /// <summary>
        ///     If non-zero, this represents the server ID of another product
        ///     which this product is a variant of. Variants are products that
        ///     are mostly similar, varying only by color or some optional
        ///     feature. In the designer toolbar, we group all product variants
        ///     within the icon for their base product. This reduces clutter.
        ///     When this is zero, it means the product is not a variant.
        /// </summary>
        public int baseProductId;

        /// <summary>
        /// Product's name.
        /// </summary>
        public string name;

        /// <summary>
        /// Longer description about the product.
        /// </summary>
        public string description;

        /// <summary>
        /// The product classification. This controls which menu it appears in.
        /// </summary>
        public ProductCategory category;

        /// <summary>
        /// Product's dealer price.
        /// </summary>
        public float priceDealer;

        /// <summary>
        /// Product's customer price.
        /// </summary>
        public float priceCustomer;

        /// <summary>
        /// The default size of the product's cut-out in meters. Individual
        /// <see cref="ProductVisualContent"/>s may override this value if allowed.
        /// E.g., <see cref="CabinetShelf"/>.
        /// </summary>
        public Vector3 cutOutSizeInMeters;

        /// <summary>
        ///     Size of the product. Currently, this is used by cabinet
        ///     backings to determine how large their model is and select
        ///     products that have <see cref="doLayoutUsingProductSize"/> set
        ///     to <see langword="true"/>. It is also used by authored cabinets
        ///     that have preset size.
        /// </summary>
        public Vector3 productSizeInMeters;

        /// <summary>
        /// This offset the product will be shifted from the anchored edge.
        /// E.g. a sink may want to be offset 16" in from the front edge or
        /// a storage unit may want to be 4" from the bottom edge
        /// </summary>
        // TODO: Add this to server backend and use this
        public float anchorOffsetInMeters;

        /// <summary>
        ///     Product link. Displayed in the designer scene's
        ///     "HighlightedProductContent" toolbar.
        /// </summary>
        public string storeLink;

        /// <summary>
        /// The visual ID. This ID corresponds to an entry in the Product Visual
        /// Catalog (either a model or a finish).
        /// </summary>
        // TODO: Replace with an int into the product catalog?
        public string visualId;

        /// <summary>
        /// A temporary debug field. True if this product info encountered
        /// parsing issues, has missing fields (e.g., cut-out size), etc.
        /// </summary>
        public bool hasErrors;

        /// <summary>
        ///     Indicates the amount of padding the product has on each side.
        ///     Some products may require no padding along an edge: e.g.,
        ///     fridges don't have padding on their bottom so they sit flush
        ///     with the floor.
        /// </summary>
        // TODO: Add this to the server backend.
        public CuboidPadding productPaddingInMeters;

        /// <summary>
        ///     The minimum size of the product in meters. Currently, this is
        ///     only used by cabinet sections. The value is optional which
        ///     means if it's not provided, the app will use the default
        ///     cabinet prefab's minimum size.
        /// </summary>
        public float? minWidthInMeters;

        /// <summary>
        ///     Similar to <see cref="minSizeInMeters"/> except for the
        ///     maximum product size.
        /// </summary>
        public float? maxWidthInMeters;

        /// <summary>
        ///     A sub-category used to organize cabinet sections. This field
        ///     is only valid on <see cref="ProductCategory.CabinetSection"/>'s
        ///     which is only used by <see cref="KitchenBuildingMethod.Catalog"/>.
        /// </summary>
        public int cabinetSectionCategoryId;

        /// <summary>
        ///     The ID of the compatibility rules to use with this cabinet. If
        ///     there are none, this is 0. This feature is intended for Gemini
        ///     mode.
        /// </summary>
        public int compatibilityId;

        /// <summary>
        ///     When <see langword="true"/>, the product's occupancy cuboid
        ///     will be calculated using the product's <see
        ///     cref="productSizeInMeters"/>, rather than its <see
        ///     cref="cutOutSizeInMeters"/>.
        ///     <br /><br />
        ///
        ///     This is <see langword="false"/> for almost all products, except
        ///     for select special case products like Fire Magic's Pizza Oven,
        ///     Blaze Griddle, etc.
        /// </summary>
        public bool doLayoutUsingProductSize;

        /// <summary>
        ///     A list of tag IDs that are used to determine a product's
        ///     compatibility with a cabinet. (Gemini)
        /// </summary>
        public HashSet<int> productTagIds = new();

        /// <summary>
        ///     Indicates that this product is defined in the server dashboard.
        ///     This is used to differentiate between real products and "fake"
        ///     products. Fake products are those that the app automatically
        ///     creates at runtime for product visuals that otherwise have no
        ///     products that use it. These fake products allow developers to
        ///     test and verify the appearance of product visuals before any
        ///     product infos are defined in the dashboard. See <see
        ///     cref="DesignerDevUI"/>.
        /// </summary>
        public bool doesExistInServerDashboard;

        /// <summary>
        ///     If this is a runtime clone of an existing <see
        ///     cref="ProductInfo"/>, return the object that was used as the
        ///     source during the clone. Otherwise, return <see
        ///     langword="null"/>.
        /// </summary>
        public ProductInfo sourceProductInfo;

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        /// <summary>
        ///     This is just a quick way to catch the easy-to-make mistake
        ///     of adding new fields to <see cref="ProductInfo"/> but
        ///     forgetting to copy those values in the copy constructor:
        ///     <see cref="ProductInfo(ProductInfo)"/>
        /// </summary>
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
#endif  // if UNITY_EDITOR


    }   // class ProductInfo

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
