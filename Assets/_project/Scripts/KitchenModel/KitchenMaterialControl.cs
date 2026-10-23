using Cysharp.Threading.Tasks;
using KitchenDesign.Geometry;
using KitchenDesign.Products;
using KitchenDesign.Scriptables;
using KitchenDesign.Serializables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using KitchenDesign.Cabinets;
using KitchenDesign.Occupancy;

namespace KitchenDesign.KitchenModel
{
    public sealed class KitchenMaterialControl
    {
        private readonly AppProfileSO _appProfile;
        private readonly ProductVisualContentStatus _productContent;
        private readonly Kitchen _kitchen;
        private readonly KitchenInteractionState _interaction;
        private readonly KitchenSelections _selections;
        private readonly Transform _ground;
        private readonly MeshRenderer _groundWood;
        private readonly MeshRenderer _groundStone;
        private readonly ProductVisualDatabaseSO _productVisualDatabase;
        private KitchenWidgetsControl _widgets;

        internal KitchenMaterialControl(
            Kitchen kitchen,
            KitchenInteractionState interaction,
            KitchenSelections selections,
            Transform ground,
            MeshRenderer groundWood,
            MeshRenderer groundStone,
            ProductVisualDatabaseSO productVisualDatabase,
            AppProfileSO appProfile,
            ProductVisualContentStatus productContent)
        {
            this._appProfile = appProfile;
            this._productContent = productContent;
            this._kitchen = kitchen;
            this._interaction = interaction;
            this._selections = selections;
            this._ground = ground;
            this._groundWood = groundWood;
            this._groundStone = groundStone;
            this._productVisualDatabase = productVisualDatabase;
            this._primaryHighlightMaterial = this._appProfile.appOptions.appSkin.primarySelectionMaterial;
            this._secondaryHighlightMaterial = this._appProfile.appOptions.appSkin.secondarySelectionMaterial;
        }

        internal void Connect(KitchenWidgetsControl widgets)
        {
            this._widgets = widgets;
        }

        internal async UniTask TryHighlightObject(GameObject highlightObject)
        {
            bool isNewHighlight = highlightObject != this._interaction.highlightObject;

            if (this._kitchen.buildingRules.ReclickingProductSelectsItsVolume)
            {
                if (!isNewHighlight &&
                    highlightObject &&
                    highlightObject.TryGetComponent(out Occupant highlightedOccupant))
                {
                    highlightObject = highlightedOccupant.occupiedVolume.gameObject;
                    isNewHighlight = true;
                }
            }

            if (this._interaction.productBeingDragged || this._interaction.volumeBeingResized != null)
            {
                return;
            }

            if (this._interaction.highlightObject && isNewHighlight)
            {
                await UpdateHighlightMaterial(
                    this._interaction.highlightObject,
                    primaryHighlightMaterial: null,
                    secondaryHighlightMaterial: null
                );

                this._interaction.highlightObject.TryGetComponent(out Occupant lastHighlightedProduct);

                if (lastHighlightedProduct)
                {
                    if (this._interaction.highlightObject.TryGetComponent(out IOccupantHighlightChangedHandler occupantHighlightChanged))
                    {
                        await occupantHighlightChanged.OnOccupantHighlightChanged(
                            lastHighlightedProduct.cabinet,
                            highlight: null
                        );
                    }
                }
            }

            this._interaction.highlightObject = highlightObject;

            if (this._interaction.highlightObject)
            {
                await UpdateHighlightMaterial(
                    this._interaction.highlightObject,
                    this._primaryHighlightMaterial,
                    this._secondaryHighlightMaterial
                );

                this._interaction.highlightObject.TryGetComponent(out Occupant currentHighlightedProduct);

                if (currentHighlightedProduct &&
                    this._interaction.highlightObject.TryGetComponent(out IOccupantHighlightChangedHandler occupantHighlightChanged))
                {
                    await occupantHighlightChanged.OnOccupantHighlightChanged(
                        currentHighlightedProduct.cabinet,
                        this._primaryHighlightMaterial
                    );
                }
            }

            if (isNewHighlight)
            {
                await EventBridge.Designer.onHighlightChanged.InvokeIfNonEmpty(this._interaction.highlightObject);
            }
        }

        internal async UniTask UpdateHighlightMaterial(
            GameObject  highlightObject,
            Material    primaryHighlightMaterial,
            Material    secondaryHighlightMaterial)
        {
            (Material cabinetMaterial, Material countertopMaterial) =
                await GetFinishMaterials(allowHighlightToReturn: false);

            Material defaultKitchenMaterial =
               await this._appProfile.GetDefaultKitchenMaterial();

            if (highlightObject.TryGetComponent(out Occupant occupant))
            {
                MelcherLog.Log(
                    "Highlighting product: " + occupant,
                    occupant,
                    severity: MelcherLogSeverity.Verbose
                );

                await occupant.productVisualContentInstance.SetModelHighlight(
                    primaryHighlightMaterial,
                    primaryHighlightMaterial ? primaryHighlightMaterial : cabinetMaterial,
                    countertopMaterial,
                    defaultKitchenMaterial
                );

                CabinetShelf shelf = null;
                if (primaryHighlightMaterial)
                {
                    shelf = highlightObject.GetComponent<CabinetShelf>();
                }

                this._widgets.SetCabinetShelfResizeWidgetsVisible(shelf);

                return;
            }
            else if (highlightObject.TryGetComponent(out CabinetBacking backing))
            {
                MelcherLog.Log(
                    "Highlighting backing: " + backing,
                    backing,
                    severity: MelcherLogSeverity.Verbose
                );

                Material activeCabinetMaterial =
                    primaryHighlightMaterial ? primaryHighlightMaterial : cabinetMaterial;

                Material activeCountertopMaterial =
                    primaryHighlightMaterial ? primaryHighlightMaterial : countertopMaterial;

                backing.OnCabinetMaterialsChanged(this._kitchen,
                    activeCabinetMaterial,
                    activeCountertopMaterial
                );

                if (!backing.hasRiserAndRiserCountertop)
                {
                    CabinetSpan backingSpan = null;

                    foreach (CabinetSpan span in this._kitchen.layout.spans)
                    {
                        if (span.backing == backing)
                        {
                            backingSpan = span;
                            break;
                        }
                    }

                    if (backingSpan == null)
                    {
                        MelcherLog.LogError($"Could not find a cabinet span that has the backing, {backing.name}", backing);
                        return;
                    }

                    foreach (ElementIndex<Cabinet> i in backingSpan.cabinets)
                    {
                        Cabinet cabinet = i.element;

                        await cabinet.SetCabinetMaterials(
                           cabinetMaterial,
                           activeCountertopMaterial,
                           defaultKitchenMaterial
                        );
                    }
                }

                return;
            }

            Material unfocusedCabinetMaterial       = cabinetMaterial;
            Material unfocusedCountertopMaterial    = countertopMaterial;

            bool isHighlighting = primaryHighlightMaterial;

            if (isHighlighting)
            {
                cabinetMaterial = primaryHighlightMaterial;
                countertopMaterial = primaryHighlightMaterial;

                unfocusedCabinetMaterial = secondaryHighlightMaterial;
                unfocusedCountertopMaterial = secondaryHighlightMaterial;
            }

            MelcherLog.LogEditorOnly($"{(isHighlighting ? "Highlighting" : "Unhighlighting")} kitchen");

            this._kitchen.CenterTheGround();

            this._ground.gameObject.SetActive(!isHighlighting);

            highlightObject.TryGetComponent(out Cabinet individualCabinet);

            this._widgets.SetCabinetResizeWidgetsVisible(isHighlighting);

            foreach (CabinetSpan span in this._kitchen.layout.spans)
            {
                foreach (ElementIndex<Cabinet> i in span.cabinets)
                {
                    Cabinet cabinet = i.element;

                    if (!individualCabinet || individualCabinet == cabinet)
                    {
                        await cabinet.SetCabinetMaterials(
                            cabinetMaterial,
                            countertopMaterial,
                            defaultKitchenMaterial
                        );
                    }
                    else
                    {
                        await cabinet.SetCabinetMaterials(
                            unfocusedCabinetMaterial,
                            unfocusedCountertopMaterial,
                            defaultKitchenMaterial
                        );
                    }
                }

                if (span.backing)
                {
                    if (!individualCabinet)
                    {
                        span.backing.OnCabinetMaterialsChanged(this._kitchen,
                            cabinetMaterial,
                            countertopMaterial
                        );
                    }
                    else
                    {
                        span.backing.OnCabinetMaterialsChanged(this._kitchen,
                            unfocusedCabinetMaterial,
                            unfocusedCountertopMaterial
                        );
                    }
                }
            }
        }
        internal async UniTask<(Material cabinetMaterial, Material countertopMaterial)> GetFinishMaterials(bool allowHighlightToReturn)
        {
            Material cabinetMaterial =
                await GetSelectedOrDefaultFinishMaterial(this._selections.cabinetFinish);

            Material countertopMaterial =
                await GetSelectedOrDefaultFinishMaterial(this._selections.countertopFinish);

            if (allowHighlightToReturn && this._interaction.highlightObject)
            {
                if (this._interaction.highlightObject == this._kitchen.gameObject)
                {
                    cabinetMaterial = this._primaryHighlightMaterial;
                    countertopMaterial = this._primaryHighlightMaterial;
                }
                else if (this._interaction.highlightObject.TryGetComponent(out Cabinet cabinet))
                {
                    cabinetMaterial = this._secondaryHighlightMaterial;
                    countertopMaterial = this._secondaryHighlightMaterial;
                }
            }

            return (cabinetMaterial, countertopMaterial);
        }

        internal async UniTask SetTemporaryFinishMaterials(Material cabinetMaterial, Material countertopMaterial, Material cutOutMaterial)
        {
            foreach (CabinetSpan span in this._kitchen.layout.spans)
            {
                if (span.backing)
                {
                    span.backing.OnCabinetMaterialsChanged(this._kitchen,
                        cabinetMaterial,
                        countertopMaterial
                    );
                }
            }

            await this._kitchen.layout.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                await this._appProfile.GetDefaultKitchenMaterial(),
                cutOutMaterial
            );
        }
        private Material _primaryHighlightMaterial = null;
        private Material _secondaryHighlightMaterial = null;
        internal async UniTask ChangeFinish(ProductInfo finishProduct, ProductCategory fallbackFinishCategory)
        {
            if (!fallbackFinishCategory.IsFinish())
            {
                MelcherLog.LogError(
                    $"Received {fallbackFinishCategory} but require a finish category."
                );
            }

            ProductCategory finishCategory = fallbackFinishCategory;
            ProductVisualSO finishProductVisual = null;

            Material finishMaterial = await this._appProfile.GetDefaultKitchenMaterial();
            FlooringType flooringType = FlooringType.Wood;

            if (finishProduct != null)
            {
                finishCategory = finishProduct.category;

                finishProductVisual =
                    this._productVisualDatabase.FindRuntimeVisualByID(
                        finishProduct.visualId
                    );

                if (!finishProductVisual)
                {
                    MelcherLog.LogError(
                        $"No product visual found for finish '{finishProduct.name}' (id: {finishProduct.id}) with visual id '{finishProduct.visualId}'"
                    );
                }
                else
                {
                    ProductVisualContent finishVisualContent =
                        await this._productContent
                            .LoadContent(finishProductVisual);

                    finishMaterial = finishVisualContent.finishMaterial;
                    flooringType = finishProductVisual.info.flooringType;
                }
            }

            if (finishCategory == ProductCategory.FinishFloor && !finishProductVisual)
            {
                finishMaterial = this._appProfile.appOptions.appSkin.kitchenDefaultFlooringMaterial;
            }

            await this._kitchen.UpdateCabinetHeightTransitions();

            switch (finishCategory)
            {
                case ProductCategory.FinishFloor:
                    this._selections.flooringFinish = finishProduct;

                    bool isStone = flooringType == FlooringType.Stone;
                    this._groundStone.gameObject.SetActive(isStone);
                    this._groundWood.gameObject.SetActive(!isStone);

                    this._groundStone.sharedMaterial = finishMaterial;
                    this._groundWood.sharedMaterial = finishMaterial;

                    return;

                case ProductCategory.FinishCabinet:
                    this._selections.cabinetFinish = finishProduct;

                    if (this._kitchen.buildingRules.UsesBrandCatalog && this._selections.cabinetFinish.manufacturerId == 140)
                    {
                        EventBridge.Designer.showNotification?.Invoke(
                            title: "Deco Grain",
                            message: "Please note that the Deco Grain finish adds a 25% premium to your cabinet choices.",
                            onDismiss: null
                        );
                    }
                    break;

                case ProductCategory.FinishCountertop:
                    this._selections.countertopFinish = finishProduct;
                    break;

                default:
                    MelcherLog.LogError(
                        $"Unhandled finish category '{finishCategory}'",
                        finishProductVisual
                    );

                    return;
            }

            if (this._interaction.highlightObject != this._kitchen.gameObject)
            {
                (Material cabinetMaterial, Material countertopMaterial) =
                    await GetFinishMaterials(allowHighlightToReturn: false);

                foreach (CabinetSpan span in this._kitchen.layout.spans)
                {
                    await span.SetCabinetMaterials(
                        this._interaction.highlightObject,
                        cabinetMaterial,
                        countertopMaterial,
                        await this._appProfile.GetDefaultKitchenMaterial()
                    );
                }
            }
        }
        internal void OnToolbarFinishProductSelected(ProductInfo finishProduct, ProductCategory fallbackFinishCategory)
        {
            ChangeFinish(finishProduct, fallbackFinishCategory).Forget();
        }
        internal async UniTask<Material> GetSelectedOrDefaultFinishMaterial(ProductInfo selectedFinish)
        {
            Material finishMaterial =
                await this._appProfile.GetDefaultKitchenMaterial();

            if (selectedFinish != null)
            {
                ProductVisualSO finishVisual =
                    this._productVisualDatabase.FindRuntimeVisualByID(
                        selectedFinish.visualId
                    );

                if (!finishVisual)
                {
                    MelcherLog.LogError(
                        $"No product visual found for finish '{selectedFinish.name}' (id: {selectedFinish.id}) with visual id '{selectedFinish.visualId}'"
                    );
                }
                else
                {
                    ProductVisualContent finishVisualPrefab =
                        await this._productContent
                            .LoadContent(finishVisual);

                    if (finishVisualPrefab != null && finishVisualPrefab.finishMaterial != null)
                    {
                        finishMaterial = finishVisualPrefab.finishMaterial;
                    }
                }
            }

            return finishMaterial;
        }
    }
}
