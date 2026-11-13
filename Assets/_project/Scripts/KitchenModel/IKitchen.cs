namespace KitchenDesign.KitchenModel
{
    using KitchenDesign.Session;
    using KitchenDesign.CatalogData;
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Server;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using KitchenDesign.Geometry;
    using KitchenDesign.Shared.Interfaces;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Products;

    /// <summary>
    /// This interface is used by cabinets to request information from the
    /// kitchen they belong to.
    /// </summary>
    public interface IKitchen
    {
        KitchenEvents events { get; }

        KitchenBuildingMethod selectedBuildingMethod { get; }

        IBuildingMethodRules buildingRules { get; }

        BrandCatalogSO selectedBrandCatalog { get; }

        KitchenTemplateSO selectedKitchenTemplate { get; }

        CountertopType selectedCountertopType { get; }

        CabinetToeKickType selectedToeKickType { get; }

        string selectedDoorStyleName { get; }
        string selectedDoorHandleStyleName { get; }

        CabinetLayout layout { get; }

        float cabinetHeightInMeters { get; }
        float countertopThicknessInMeters { get; }
        float baseCabinetDepthInMeters { get; }
        CuboidPadding baseCountertopOverhangInMeters { get; }
        float backPanelDepthInMeters { get; }

        ProductVisualDatabaseSO productVisualDatabase { get;  }

        IAppRules appRules { get; }

        CatalogDataStatus catalogData { get; }

        ProductVisualContentStatus productContent { get; }

        SessionStatus session { get; }

        /// <summary>
        ///     Return <see langword="true"/> if cabinets can display columns
        ///     at their corners. This is intended to be true when Sections
        ///     building method is enabled, i.e., in the Stoll app version.
        /// </summary>
        bool doCabinetsHaveColumns { get; }

        List<KitchenExtraFeature> selectedExtraFeatures { get; }

        bool AreProductsForcedToCenterOfCabinets(Cabinet cabinetForRuleset);

        UniTask<(Material cabinetMaterial, Material countertopMaterial)> GetFinishMaterials(bool allowHighlightToReturn);
        UniTask<Occupant> InstantiateOccupant(ProductInfo productInfo, ProductVisualSO productVisual, ProductVisualContent productVisualPrefab);


        UniTask TryHighlightObject(GameObject highlightObject);

        void OnOccupantDragBegin(Occupant draggedOccupant, PointerEventData eventData);
        void OnOccupantDrag(Occupant draggedOccupant, PointerEventData eventData);
        UniTask OnOccupantDragEnd((Occupant draggedOccupant, PointerEventData eventData) args);

        UniTask OnOccupantInstantiated((Occupant newOccupant, bool userInitiated) args);
        UniTask OnOccupantRemovedFromCabinet(Occupant occupant);

    }   // interface IKitchen

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-01
/// ===========================================================================
