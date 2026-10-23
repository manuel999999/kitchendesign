using KitchenDesign.BuildingRules;
using KitchenDesign.CatalogData;
using KitchenDesign.Geometry;
using KitchenDesign.KitchenModel;
using KitchenDesign.Pricing;
using KitchenDesign.Scriptables;
using KitchenDesign.Serializables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;
using Melcher;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using KitchenDesign.Session;
using KitchenDesign.Cabinets;
using KitchenDesign.Occupancy;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;
using JsonList = System.Collections.Generic.IList<object>;

namespace KitchenDesign.Save
{
    public sealed class KitchenSaveWriter
    {
        private readonly SessionStatus _session;
        private readonly ClientListStatus _clientList;
        private readonly IAppRules _appRules;
        private readonly CatalogDataStatus _catalogData;
        private readonly KitchenSelections _selections;
        private readonly ProductVisualDatabaseSO _productVisualDatabase;

        public KitchenSaveWriter(KitchenSelections selections, ProductVisualDatabaseSO productVisualDatabase, SessionStatus session, ClientListStatus clientList, IAppRules appRules, CatalogDataStatus catalogData)
        {
            this._session = session;
            this._clientList = clientList;
            this._appRules = appRules;
            this._catalogData = catalogData;
            this._selections = selections;
            this._productVisualDatabase = productVisualDatabase;
        }

        public JsonDict Write(CabinetLayout layout)
        {
            IBuildingMethodRules buildingRules = new BuildingMethodRules(this._selections.buildingMethod);

            List<string> dimensions = new List<string>();

            void AddDimensions(List<string> dimensions, Vector3 sizeInMeters, bool showFraction)
            {
                string length, height, depth;

                if (this._session.UserProfile.activeUnits == MeasurementUnit.Centimeters)
                {
                    length = Utility.MeasurementFormat.FormatMetersToCentimeters(sizeInMeters.x) + " ";
                    height = Utility.MeasurementFormat.FormatMetersToCentimeters(sizeInMeters.y) + " ";
                    depth = Utility.MeasurementFormat.FormatMetersToCentimeters(sizeInMeters.z) + " ";
                }
                else
                {
                    length = Utility.MeasurementFormat.FormatMetersToImperial(sizeInMeters.x, showFraction: showFraction);
                    height = Utility.MeasurementFormat.FormatMetersToImperial(sizeInMeters.y, showFeet: false, showFraction: showFraction);
                    depth = Utility.MeasurementFormat.FormatMetersToImperial(sizeInMeters.z, showFeet: false, showFraction: showFraction);
                }

                dimensions.Add($"{length} L x {height} H x {depth} D");
            }

            bool showFraction = false;

            if(this._appRules.MeasurementFractionsFollowBrandCatalog && this._selections.brandCatalog != null)
            {
                showFraction = this._selections.brandCatalog.showFractionInMeasurements;
            }

            foreach (CabinetSpan span in layout.spans)
            {
                if (!span.IsSpace())
                {
                    Cuboid spanOutsideCuboid =
                        span.GetOutsideCuboidInSpanSpace(includeCountertop: false);

                    AddDimensions(dimensions, spanOutsideCuboid.size, showFraction);
                }
            }

            string cabinetFinishProductSku      = "";
            string countertopFinishProductSku   = "";
            string paverFinishProductSku        = "";
            string deckFinishProductSku         = "";

            if (this._selections.cabinetFinish != null)
            {
                cabinetFinishProductSku = this._selections.cabinetFinish.sku;
            }

            if (this._selections.countertopFinish != null)
            {
                countertopFinishProductSku = this._selections.countertopFinish.sku;
            }

            if (this._selections.flooringFinish != null)
            {
                ProductVisualSO flooringVisual =
                    this._productVisualDatabase.FindRuntimeVisualByID(
                        this._selections.flooringFinish.visualId
                    );

                if (flooringVisual)
                {
                    if (flooringVisual.info.flooringType == FlooringType.Stone)
                    {
                        paverFinishProductSku = this._selections.flooringFinish.sku;
                    }
                    else
                    {
                        deckFinishProductSku = this._selections.flooringFinish.sku;
                    }
                }
            }

            JsonList modelSaveDataJson = new List<object>();
            int cabinetSide = 0;
            foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (!cabinet.canHaveProducts)
                {
                    continue;
                }

                AddLegacyOccupantEntries(cabinet, cabinetSide, modelSaveDataJson);

                ++cabinetSide;
            }

            void AddLegacyOccupantEntries(OccupiableVolume volume, int cabinetSide, JsonList modelSaveDataJson)
            {
                foreach (Occupant occupant in volume.occupants)
                {
                    JsonDict productPositionJson = new Dictionary<string, object>()
                    {
                        { "x", occupant.metersFromVolumeMinX },
                        { "y", 0.0f },
                        { "z", 0.0f },
                    };

                    JsonDict modelDataEntryJson = new Dictionary<string, object>()
                    {
                        { "productID", occupant.productInfo.sku },
                        { "productPosition", productPositionJson },
                        { "cabinetSide", cabinetSide }
                    };

                    Vector3 occupantCutOutSize = occupant.currentCutOutSizeInMeters;
                    Vector3 defaultCutOutSize = occupant.productInfo.cutOutSizeInMeters;
                    if (occupantCutOutSize != defaultCutOutSize)
                    {
                        string w, h, d;

                        if (this._session.UserProfile.activeUnits == MeasurementUnit.Centimeters)
                        {
                            w = Utility.MeasurementFormat.FormatMetersToCentimeters(occupantCutOutSize.x);
                            h = Utility.MeasurementFormat.FormatMetersToCentimeters(occupantCutOutSize.y);
                            d = Utility.MeasurementFormat.FormatMetersToCentimeters(occupantCutOutSize.z);
                        }
                        else
                        {
                            w = ImperialUnit.FromTotalMeters(occupantCutOutSize.x).ToString(showFeet: false);
                            h = ImperialUnit.FromTotalMeters(occupantCutOutSize.y).ToString(showFeet: false);
                            d = ImperialUnit.FromTotalMeters(occupantCutOutSize.z).ToString(showFeet: false);
                        }

                        string customCutOutSize =
                            $"{w} (W) x {h} (H) x {d} (D)";

                        modelDataEntryJson.Add("cutOutSize", customCutOutSize);
                    }

                    modelSaveDataJson.Add(modelDataEntryJson);

                    if (occupant.TryGetComponent(out CabinetShelf nestedVolume))
                    {
                        AddLegacyOccupantEntries(nestedVolume, cabinetSide, modelSaveDataJson);
                    }
                }
            }

            string counterTopHeight;

            if (this._session.UserProfile.activeUnits == MeasurementUnit.Centimeters)
            {
                counterTopHeight = Utility.MeasurementFormat.FormatMetersToCentimeters(this._selections.countertopThicknessInMeters);
            }
            else
            {
                counterTopHeight = Utility.MeasurementFormat.FormatMetersToImperial(this._selections.countertopThicknessInMeters);
            }

            JsonList topMeasurements = new List<object>();
            JsonList frontMeasurements = new List<object>();

            cabinetSide = 0;
            foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (!cabinet.canHaveProducts)
                {
                    continue;
                }

                JsonList cabinetTopMeasurementEntries = new List<object>();
                JsonDict cabinetTopMeasurements = new Dictionary<string, object>()
                {
                    { "section", cabinetSide },
                    { "entries", cabinetTopMeasurementEntries }
                };
                GetLegacyMeasurementsForCabinetSave(cabinet, cabinetTopMeasurementEntries, findTop: true);
                topMeasurements.Add(cabinetTopMeasurements);

                JsonList cabinetFrontMeasurementEntries = new List<object>();
                JsonDict cabinetFrontMeasurements = new Dictionary<string, object>()
                {
                    { "section", cabinetSide },
                    { "entries", cabinetFrontMeasurementEntries }
                };
                GetLegacyMeasurementsForCabinetSave(cabinet, cabinetFrontMeasurementEntries, findTop: false);
                frontMeasurements.Add(cabinetFrontMeasurements);

                ++cabinetSide;
            }

            List<int> cabinetIdList = new();

            JsonList cabinetList = new List<object>();
            foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (cabinet.cabinetSectionProductInfo != null &&
                    !string.IsNullOrEmpty(cabinet.cabinetSectionProductInfo.sku))
                {
                    if (cabinet.cabinetSectionVisual == null ||
                        cabinet.cabinetSectionVisual.info.sectionAppearsInDetailSheet)
                    {
                        cabinetIdList.Add(cabinet.cabinetSectionProductInfo.id);
                    }
                }

                string cabinetBackingSpriteName =
                    cabinet.backingSprite ? cabinet.backingSprite.name : null;

                JsonDict cabinetEntry = new Dictionary<string, object>()
                {
                    { "cabinet_type", cabinet.cabinetType.ToString() },
                    { "normalized_anchor_to_previous_section", ProFitUtility.Vector3ToJson(cabinet.normalizedAnchorToPreviousSection) },
                    { "local_euler_angles", ProFitUtility.Vector3ToJson(cabinet.GetTransform().localEulerAngles) },
                    { "min_size_in_meters", ProFitUtility.Vector3ToJson(cabinet.minSizeInMeters) },
                    { "max_size_in_meters", ProFitUtility.Vector3ToJson(cabinet.maxSizeInMeters) },
                    { "backing_sprite", cabinetBackingSpriteName }
                };

                if (cabinet.Is(CabinetType.CornerWedge))
                {
                    int indexOfCabinetBeingWedged =
                        layout.FindCabinetIndex(cabinet.cornerCabinetBeingWedged);

                    cabinetEntry.Add("index_of_cabinet_being_wedged", indexOfCabinetBeingWedged);
                }

                if (cabinet.cabinetSectionProductInfo != null)
                {
                    cabinetEntry.Add("cabinet_server_id", cabinet.cabinetSectionProductInfo.id);
                    cabinetEntry.Add("cabinet_id", cabinet.cabinetSectionProductInfo.sku);
                }

                float lengthInMeters = cabinet.sizeInMetersWithoutAdditive.x;
                if (cabinet.cabinetType == CabinetType.Corner)
                {
                    lengthInMeters = this._selections.baseCabinetDepthInMeters;
                }

                cabinetEntry.Add("size_in_meters_x", lengthInMeters);

                AddOccupantsToJson(cabinetEntry, cabinet.occupants);

                cabinetList.Add(cabinetEntry);
            }

            JsonList cabinetSpanList = new List<object>();
            foreach (CabinetSpan span in layout.spans)
            {
                string backingProductSku = span.backing?.info.sku;

                JsonDict cabinetSpanEntry = new Dictionary<string, object>()
                {
                    { "index", span.index },
                    { "backing_product_id", backingProductSku },
                    { "depth_in_meters", span.baseCabinetDepthInMeters }
                };

                cabinetSpanList.Add(cabinetSpanEntry);
            }

            const float InchesToFeet = 1.0f / Units.InchesPerFoot;

            float cabinetLinearMeters = 0.0f;
            float cabinetSquareMeters = 0.0f;

            if (buildingRules.PricesCabinetFinishByArea)
            {
                bool isSpanConnectedToPreviousSpan = false;

                float cabinetHeight =
                    layout.spans[0].cabinets[0].element.sizeInMeters.y;

                float hiddenFaceLinearMeters =
                    this._selections.baseCabinetDepthInMeters;

                float hiddenFaceSquareMeters =
                    hiddenFaceLinearMeters * cabinetHeight;

                foreach (CabinetSpan span in layout.spans)
                {
                    if (span.IsSpace())
                    {
                        cabinetLinearMeters += hiddenFaceLinearMeters;
                        cabinetLinearMeters += hiddenFaceLinearMeters;

                        cabinetSquareMeters += hiddenFaceSquareMeters;
                        cabinetSquareMeters += hiddenFaceSquareMeters;

                        isSpanConnectedToPreviousSpan = false;
                        continue;
                    }

                    if (span.backing &&
                        span.backing.hasRiserAndRiserCountertop &&
                        span.backing.riserMaterialMode == RiserMaterialMode.UseCabinetMaterial)
                    {
                        Cuboid riserCuboid = span.backing.riserCuboid;

                        cabinetLinearMeters += (riserCuboid.width + riserCuboid.depth) * 2.0f;

                        cabinetSquareMeters +=
                            riserCuboid.width * riserCuboid.height * 2.0f +
                            riserCuboid.depth * riserCuboid.height * 2.0f;
                    }

                    Cuboid spanOuterCuboid =
                        span.GetOutsideCuboidInSpanSpace(includeCountertop: false);

                    cabinetLinearMeters +=
                        (spanOuterCuboid.width * 2.0f) + spanOuterCuboid.depth;

                    cabinetSquareMeters +=
                        spanOuterCuboid.width * spanOuterCuboid.height * 2.0f +
                        spanOuterCuboid.depth * spanOuterCuboid.height;

                    cabinetLinearMeters -= hiddenFaceLinearMeters;
                    cabinetSquareMeters -= hiddenFaceSquareMeters;

                    if (isSpanConnectedToPreviousSpan)
                    {
                        cabinetLinearMeters -= hiddenFaceLinearMeters;
                        cabinetSquareMeters -= hiddenFaceSquareMeters;
                    }
                }

                cabinetLinearMeters += hiddenFaceLinearMeters;
                cabinetLinearMeters += hiddenFaceLinearMeters;

                cabinetSquareMeters += hiddenFaceSquareMeters;
                cabinetSquareMeters += hiddenFaceSquareMeters;

                foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    Cuboid occupiableCuboid = cabinet.occupiableCuboid;

                    foreach (Occupant occupant in cabinet.occupants)
                    {
                        Cuboid cutOutCuboid = occupant.GetCutOutCuboidInVolumeSpace();

                        if (ProFitUtility.IsDistanceAlmostZero(occupiableCuboid.zMin - cutOutCuboid.zMin) ||
                            ProFitUtility.IsDistanceAlmostZero(occupiableCuboid.zMax - cutOutCuboid.zMax))
                        {
                            cabinetSquareMeters -=
                                cutOutCuboid.width * cutOutCuboid.height;
                        }
                    }
                }
            }

            float toeKickLinearMeters = 0.0f;

            if (this._selections.toeKickType != CabinetToeKickType.None)
            {
                bool isToeKickInset =
                    this._selections.toeKickType == CabinetToeKickType.Inset;

                float? previousCabinetDepth = null;

                foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.canHaveLegs ||
                        (cabinet.cabinetSectionVisualPrefab &&
                        cabinet.cabinetSectionVisual.info.cabinetSectionFootingType == CabinetSectionFootingType.OnGround))
                    {
                        previousCabinetDepth = null;
                        continue;
                    }

                    if (cabinet.cabinetType == CabinetType.Space)
                    {
                        continue;
                    }
                    else if (cabinet.cabinetType.IsMidSpanCabinetType(this._selections.buildingMethod))
                    {
                        int numberOfSides = 2;

                        if (buildingRules.UsesBrandCatalog &&
                            this._selections.brandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong &&
                            cabinet.cabinetSectionVisual &&
                            cabinet.cabinetSectionVisual.info.productCategoriesAllowedInSection.Any(x => x == ProductCategorySpecific.Fridge || x == ProductCategorySpecific.IceBox))
                        {
                            numberOfSides = 1;
                        }

                        toeKickLinearMeters += cabinet.sizeInMeters.x * numberOfSides;
                    }
                    else if (cabinet.cabinetType == CabinetType.Corner)
                    {
                        toeKickLinearMeters += cabinet.sizeInMeters.x;
                        toeKickLinearMeters += cabinet.sizeInMeters.z;

                        if (isToeKickInset)
                        {
                            toeKickLinearMeters += Const.ToeKickInsetInMeters * 2.0f;
                        }
                    }
                    else if (cabinet.cabinetType.IsEnd())
                    {
                        toeKickLinearMeters += cabinet.sizeInMeters.x * 2.0f;
                        toeKickLinearMeters += cabinet.sizeInMeters.z;

                        if (isToeKickInset)
                        {
                            toeKickLinearMeters -= Const.ToeKickInsetInMeters;
                        }
                    }
                    else
                    {
                        MelcherLog.LogError("Unhandled cabinet type: " + cabinet.cabinetType, cabinet);
                    }

                    if (!cabinet.cabinetType.IsEnd() && previousCabinetDepth.HasValue)
                    {
                        float cabinetDepthDifference =
                            Mathf.Abs(cabinet.sizeInMeters.z - previousCabinetDepth.Value);

                        toeKickLinearMeters += cabinetDepthDifference;
                    }

                    previousCabinetDepth =
                        (cabinet.cabinetType == CabinetType.Corner) ?
                            cabinet.sizeInMeters.x :
                            cabinet.sizeInMeters.z;
                }
            }

            float countertopAreaInSquareMeters = 0.0f;
            float countertopLinearLengthInMeters = 0.0f;

            foreach (CabinetSpan cabinetSpan in layout.spans)
            {
                if (cabinetSpan.backing && cabinetSpan.backing.hasRiserAndRiserCountertop)
                {
                    if (cabinetSpan.backing.isRiserCountertopVisible)
                    {
                        Cuboid riserCountertopCuboid =
                            cabinetSpan.backing.GetCountertopCuboidInSpanSpace(default);

                        countertopAreaInSquareMeters += riserCountertopCuboid.width * riserCountertopCuboid.depth;
                    }
                }

                foreach (ElementIndex<Cabinet> i in cabinetSpan.cabinets)
                {
                    Cabinet cabinet = i.element;

                    Cuboid countertopCuboid = cabinet.countertopCuboid;

                    countertopAreaInSquareMeters +=
                        countertopCuboid.width * countertopCuboid.depth;

                    countertopLinearLengthInMeters += countertopCuboid.width;
                }
            }

            float countertopAreaInSquareFeet =
                countertopAreaInSquareMeters * (Units.Meters2Inches * Units.Meters2Inches) * (InchesToFeet * InchesToFeet);

            string countertopArea =
                this._session.UserProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                    countertopAreaInSquareFeet + " sq. ft." :
                    countertopAreaInSquareMeters + " sq. m.";

            float countertopLinearLengthInFeet =
                countertopLinearLengthInMeters * Units.Meters2Inches * InchesToFeet;

            PriceSheetInputs priceSheetInputs = new PriceSheetInputs();
            JsonList manualPriceSheetEntries = priceSheetInputs.manualPriceSheetEntries;
            Dictionary<int, float> productIdToPriceChangeFactor = priceSheetInputs.productIdToPriceChangeFactor;

            IPriceSheetExtras priceSheetExtras = this._appRules.PriceSheetExtras switch
            {
                PriceSheetExtras.StollCustomKitchen => new StollPriceSheet(this._selections),
                PriceSheetExtras.GeminiCatalog => new GeminiCatalogPriceSheet(this._selections, this._session, this._catalogData),
                _ => null
            };

            priceSheetExtras?.AddTo(priceSheetInputs, layout, toeKickLinearMeters);

            List<string> extraFeatures = new List<string>();
            foreach (KitchenExtraFeature feature in this._selections.extraFeatures)
            {
                extraFeatures.Add(feature.ToString());
            }

            List<object> priceChanges = new List<object>();
            foreach ((int productId, float rate) in productIdToPriceChangeFactor)
            {
                priceChanges.Add(
                    new Dictionary<string, object>()
                    {
                        { "product_id", productId },
                        { "rate", rate }
                    }
                );
            }

            IDictionary<string, object> saveJson = new Dictionary<string, object>()
            {
                { "islandType", layout.displayName },
                { "islandDimensions", string.Join("\n", dimensions) },
                { "cabinetFinish", cabinetFinishProductSku },
                { "counterTopFinish", countertopFinishProductSku },
                { "deckFinish", deckFinishProductSku },
                { "paverFinish", paverFinishProductSku },
                { "modelSaveData", modelSaveDataJson },
                { "counterTopHeight", counterTopHeight },

                { "top_measurements", topMeasurements },
                { "front_measurements", frontMeasurements },
                { "cabinet_list", cabinetIdList },
                { "manual_price_sheet_entries", manualPriceSheetEntries },
                { "countertop_area", countertopArea },

                { "price_changes", priceChanges },

                { "kitchen_layout_style", layout.style.ToString() },
                { "kitchen_building_method", this._selections.buildingMethod.ToString() },
                { "countertop_type", this._selections.countertopType.ToString() },
                { "toe_kick_type", this._selections.toeKickType.ToString() },
                { "cabinets", cabinetList },
                { "cabinet_spans", cabinetSpanList },
                { "base_cabinet_depth_in_meters", this._selections.baseCabinetDepthInMeters },
                { "countertop_thickness_in_meters", this._selections.countertopThicknessInMeters },
                { "extra_features", extraFeatures },

                { "islandEnum", 0 },
                { "backplashLeftActive", false },
                { "backsplashUpperActive", false },
                { "backsplashRightActive", false },
                { "barTopLeftActive", false },
                { "barTopUpperActive", false },
                { "barTopRightActive", false },
                { "leftLength", false },
                { "upperLength", false },
                { "rightLength", false },
                { "cabinetDepthLeft", 0 },
                { "cabinetDepthUpper", 0 },
                { "cabinetDepthRight", 0 },
                { "cabinetDepthInitial", 0 },
            };

            if (this._selections.analyticsKey != null)
            {
                string analyticsKey = string.IsNullOrEmpty(this._selections.analyticsKey) ? null : this._selections.analyticsKey;
                saveJson.Add("analytics_key", analyticsKey);
            }

            if (this._clientList.ActiveClientWantsToBeContacted.HasValue)
            {
                saveJson.Add("wants_to_be_contacted", this._clientList.ActiveClientWantsToBeContacted.Value);
            }

            string disclaimer = null;

            if (buildingRules.UsesBrandCatalog)
            {
                if (this._selections.brandCatalog.ruleset == BrandCatalogRuleset.Napoleon)
                {
                    disclaimer = "Countertop not included on this final quote.";
                }
            }
            else
            {
                disclaimer = this._appRules.NonCatalogQuoteDisclaimer;
            }

            if (!string.IsNullOrWhiteSpace(disclaimer))
            {
                saveJson.Add("disclaimer", disclaimer);
            }

            if (buildingRules.ReportsCabinetFinishLinearLength)
            {
                string finishLinearDistance =
                    this._session.UserProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                        cabinetLinearMeters * Units.Meters2Inches * InchesToFeet + " ft" :
                        cabinetLinearMeters + " m";

                saveJson["cabinetFinishArea"] = finishLinearDistance;
            }
            else if (cabinetSquareMeters > 0.0f)
            {
                string finishSquareArea =
                    this._session.UserProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                        cabinetSquareMeters * (Units.Meters2Inches * Units.Meters2Inches) * (InchesToFeet * InchesToFeet) + " ft�" :
                        cabinetSquareMeters + " m�";

                saveJson["cabinetFinishArea"] = finishSquareArea;
            }

            if (toeKickLinearMeters > 0.0f
                && this._selections.brandCatalog.rules.PricesToeKicks)
            {
                string toeKickLinearDistance =
                    this._session.UserProfile.activeUnits == MeasurementUnit.FeetAndInches ?
                        Utility.MeasurementFormat.FormatMetersToImperial(toeKickLinearMeters) :
                        Utility.MeasurementFormat.FormatMetersToCentimeters(toeKickLinearMeters);

                saveJson.Add("toe_kick_linear_distance", toeKickLinearDistance);
            }

            if (this._selections.brandCatalog)
            {
                saveJson.Add("brand_catalog", this._selections.brandCatalog.catalogName);
                saveJson.Add("brand_catalog_id", this._selections.brandCatalog.catalogId);
                saveJson.Add("door_style_name", this._selections.doorStyleName);
                saveJson.Add("door_handle_style_name", this._selections.doorHandleStyleName);
                saveJson.Add("end_panel_width_left_in_meters", this._selections.endPanelWidthInMetersLeft);
                saveJson.Add("end_panel_width_right_in_meters", this._selections.endPanelWidthInMetersRight);
                saveJson.Add("back_panel_depth_in_meters", this._selections.backPanelDepthInMeters);
            }

            return saveJson;
        }

        private static void AddOccupantsToJson(JsonDict volumeEntry, ReadOnlyCollection<Occupant> occupants)
        {
            JsonList volumeOccupants = new List<object>();
            volumeEntry.Add("occupants", volumeOccupants);

            foreach (Occupant occupant in occupants)
            {
                Vector3 occupantLocalPosition = occupant.GetLocalPositionInVolume();
                JsonDict localPosition = new Dictionary<string, object>()
                {
                    { "x", occupantLocalPosition.x },
                    { "y", occupantLocalPosition.y },
                    { "z", occupantLocalPosition.z }
                };

                JsonDict occupantEntry = new Dictionary<string, object>()
                {
                    { "product_server_id", occupant.productInfo.id },
                    { "product_id", occupant.productInfo.sku },
                    { "local_position", localPosition }
                };

                if (occupant.isLockedToCabinet)
                {
                    occupantEntry.Add("is_locked_to_cabinet", true);
                }

                Vector3 occupantCutOutSize = occupant.currentCutOutSizeInMeters;
                Vector3 defaultCutOutSize = occupant.productInfo.cutOutSizeInMeters;
                if (occupantCutOutSize != defaultCutOutSize)
                {
                    JsonDict cutOutSize = new Dictionary<string, object>()
                    {
                        { "x", occupantCutOutSize.x },
                        { "y", occupantCutOutSize.y },
                        { "z", occupantCutOutSize.z }
                    };

                    occupantEntry.Add("cut_out_size_in_meters", cutOutSize);
                }

                if (occupant.TryGetComponent(out OccupiableVolume nestedVolume) &&
                    nestedVolume.occupants.Count > 0)
                {
                    AddOccupantsToJson(occupantEntry, nestedVolume.occupants);
                }

                volumeOccupants.Add(occupantEntry);
            }
        }

        private static void GetLegacyMeasurementsForCabinetSave(Cabinet cabinet, JsonList measurementEntries, bool findTop)
        {
            float lastCutOutEndMetersFromCabinetMinX = 0.0f;
            foreach (Occupant occupant in cabinet.occupants)
            {
                if (occupant.productVisualContentInstance.TryGetComponent(out CabinetBacking _))
                {
                    continue;
                }

                bool isOnTop =
                    occupant.productVisual.info.anchor == ProductAnchor.OnTopAtFront ||
                    occupant.productVisual.info.anchor == ProductAnchor.OnTopAtBack;

                if (isOnTop != findTop)
                {
                    continue;
                }

                float cutOutStartMetersFromVolumeMinX = occupant.metersFromVolumeMinX + occupant.productVisualContentInstance.cabinetCutOut.transformHelper.offsetInMeters.x;
                float cutOutEndMetersFromVolumeMinX = cutOutStartMetersFromVolumeMinX + occupant.productVisualContentInstance.cabinetCutOut.transformHelper.sizeInMeters.x;

                float leadingSpanInMeters = lastCutOutEndMetersFromCabinetMinX - cutOutStartMetersFromVolumeMinX;
                if (leadingSpanInMeters > 0.0f)
                {
                    JsonDict gapEntry = new Dictionary<string, object>()
                    {
                        { "inches", leadingSpanInMeters * Units.Meters2Inches },
                        { "product_id", "" }
                    };
                    measurementEntries.Add(gapEntry);
                }

                JsonDict productEntry = new Dictionary<string, object>()
                {
                    { "inches", occupant.productVisualContentInstance.cabinetCutOut.transformHelper.sizeInInches.x },
                    { "product_id", occupant.productInfo.sku }
                };
                measurementEntries.Add(productEntry);

                lastCutOutEndMetersFromCabinetMinX = cutOutEndMetersFromVolumeMinX;
            }

            float trailingSpaceInMeters = cabinet.sizeInMeters.x - lastCutOutEndMetersFromCabinetMinX;
            if (trailingSpaceInMeters > 0.0f)
            {
                JsonDict tailEntry = new Dictionary<string, object>()
                {
                    { "inches", trailingSpaceInMeters * Units.Meters2Inches },
                    { "product_id", "" }
                };
                measurementEntries.Add(tailEntry);
            }
        }
    }
}
