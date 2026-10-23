using KitchenDesign.Brands;
using KitchenDesign.Geometry;
using KitchenDesign.KitchenModel;
using KitchenDesign.Scriptables;
using KitchenDesign.Serializables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using KitchenDesign.Session;
using KitchenDesign.CatalogData;
using KitchenDesign.Cabinets;
using KitchenDesign.Occupancy;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;
using JsonList = System.Collections.Generic.IList<object>;

namespace KitchenDesign.Pricing
{
    public sealed class GeminiCatalogPriceSheet : IPriceSheetExtras
    {
        private readonly SessionStatus _session;
        private readonly CatalogDataStatus _catalogData;
        private readonly KitchenSelections _selections;

        public GeminiCatalogPriceSheet(KitchenSelections selections, SessionStatus session, CatalogDataStatus catalogData)
        {
            this._session = session;
            this._catalogData = catalogData;
            this._selections = selections;
        }

        private BrandRules brandRules => this._selections.brandCatalog.rules;

        public void AddTo(PriceSheetInputs inputs, CabinetLayout layout, float toeKickLinearMeters)
        {
            JsonList manualPriceSheetEntries = inputs.manualPriceSheetEntries;
            Dictionary<int, float> productIdToPriceChangeFactor = inputs.productIdToPriceChangeFactor;

            static JsonDict CreateManualPriceSheetEntry(string productSku, string productName, float price)
            {
                return PriceSheetInputs.CreateManualPriceSheetEntry(productSku, productName, price);
            }

            JsonDict premiumFinishPackageEntry =
                this._selections.extraFeatures.Contains(KitchenExtraFeature.ChallengerPremiumCabinetFinish) ?
                    CreateManualPriceSheetEntry(
                        productSku: "CPFP",
                        productName: "Challenger Premium Finish Package",
                        price: 463.00f
                    ) :
                    null;

            bool isShakerDoorSelected = this._selections.doorStyleName == "Shaker";
            bool isDecoGrain = this._selections.cabinetFinish != null && this._selections.cabinetFinish.manufacturerId == 140;

            int cabinetEndPanelCount = 0;

            CabinetSectionDoorHandleStyleSO style = this._selections.brandCatalog.cabinetSectionDoorHandleStyle;
            float handleBasePrice = 0f;

            if(style != null)
            {
                handleBasePrice = style.baseHandlePrice;
            }

            foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (cabinet.cabinetType.IsEnd())
                {
                    ++cabinetEndPanelCount;
                }

                if(!string.IsNullOrEmpty(this._selections.doorHandleStyleName))
                {
                    CabinetSectionDoorHandle handle = cabinet.GetComponentInChildren<CabinetSectionDoorHandle>();

                    if(handle != null)
                    {
                        for(int item = 0; item < handle.hanndleCount; item++)
                        {
                             manualPriceSheetEntries.Add(
                                CreateManualPriceSheetEntry(
                                    productSku: "HARDWARE",
                                    productName: this._selections.doorHandleStyleName,
                                    price: handleBasePrice
                                )
                            );
                        }
                    }
                }

                if (isShakerDoorSelected &&
                    cabinet.cabinetSectionProductInfo != null &&
                    cabinet.cabinetSectionVisual.info.expectingCabinetDoors &&
                    !productIdToPriceChangeFactor.ContainsKey(cabinet.cabinetSectionProductInfo.id))
                {
                    productIdToPriceChangeFactor.Add(
                        cabinet.cabinetSectionProductInfo.id,
                        1.05f
                    );
                }

                if(isDecoGrain &&
                    cabinet.cabinetSectionProductInfo != null &&
                    !productIdToPriceChangeFactor.ContainsKey(cabinet.cabinetSectionProductInfo.id))
                {
                    productIdToPriceChangeFactor.Add(
                        cabinet.cabinetSectionProductInfo.id,
                        1.25f
                    );
                }

                if (premiumFinishPackageEntry != null &&
                    cabinet.cabinetType != CabinetType.Space &&
                    !cabinet.cabinetType.IsEnd(includeTransitions: true))
                {
                    manualPriceSheetEntries.Add(premiumFinishPackageEntry);
                }

                const int FillerBaseCabinetCategoryId = 30;

                if (cabinet.cabinetSectionProductInfo != null)
                {
                    if (cabinet.cabinetSectionProductInfo.cabinetSectionCategoryId == FillerBaseCabinetCategoryId)
                    {
                        JsonDict fillerEntry =
                            CreateManualPriceSheetEntry(
                                productSku: cabinet.cabinetSectionProductInfo.sku,
                                productName: cabinet.cabinetSectionProductInfo.name,
                                price: cabinet.cabinetSectionProductInfo.priceCustomer
                            );

                        manualPriceSheetEntries.Add(fillerEntry);
                        manualPriceSheetEntries.Add(fillerEntry);
                    }

                    if (this._selections.brandCatalog.ruleset == BrandCatalogRuleset.Napoleon &&
                        cabinet.Is(CabinetType.Corner))
                    {
                        JsonDict transitionPiece =
                            CreateManualPriceSheetEntry(
                                productSku: "IM-45T-CN",
                                productName: "OASIS\u2122 45\u00b0 Transition Piece",
                                price: this._session.UserProfile.currency == Currency.CAD ?
                                    99.0f :
                                    109.0f
                            );
                        manualPriceSheetEntries.Add(transitionPiece);
                        manualPriceSheetEntries.Add(transitionPiece);
                    }

                    if (brandRules.PricesCornersAsOwnProduct &&
                        cabinet.Is(CabinetType.Corner))
                    {
                        JsonDict transitionPiece =
                            CreateManualPriceSheetEntry(
                                productSku: cabinet.cabinetSectionProductInfo.sku,
                                productName: cabinet.cabinetSectionProductInfo.name,
                                price: cabinet.cabinetSectionProductInfo.priceCustomer
                            );
                        manualPriceSheetEntries.Add(transitionPiece);
                    }
                }

                if(brandRules.ForcesKamadoGrillEndPanels &&
                    cabinet.cabinetSectionProductInfo != null &&
                    cabinet.cabinetSectionProductInfo.sku != null &&
                    (cabinet.cabinetSectionProductInfo.sku is "GADB" or "GADA"))
                {
                     layout.GetCabinetNeighbours(
                        cabinet,
                        out Cabinet leftNeighbour,
                        out Cabinet rightNeighbour
                    );

                    bool leftIsEnd  = leftNeighbour != null && leftNeighbour.cabinetType.IsEnd();
                    bool rightIsEnd = rightNeighbour != null && rightNeighbour.cabinetType.IsEnd();

                    bool isBesideACabinet = true;

                    EndPanelInfo endPanelInfo =
                            this._selections.brandCatalog.
                            cabinetRuntimeEndPanelInfo.
                            GetEndPanelInfoByTargetDepthOrDefault(cabinet.sizeInMeters.z - this._selections.backPanelDepthInMeters);

                    if(leftIsEnd && rightIsEnd == false)
                    {
                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: endPanelInfo.productId + "-R",
                                productName: endPanelInfo.productName + "-R",
                                price: endPanelInfo.dealerPrice
                            )
                        );

                        isBesideACabinet = false;
                    }

                    if(rightIsEnd && leftIsEnd == false)
                    {
                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: endPanelInfo.productId + "-L",
                                productName: endPanelInfo.productName + "-L",
                                price: endPanelInfo.dealerPrice
                            )
                        );

                        isBesideACabinet = false;
                    }

                    if(leftIsEnd && rightIsEnd)
                    {
                        isBesideACabinet = false;
                    }

                    if(isBesideACabinet)
                    {
                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: endPanelInfo.productId + "-R",
                                productName: endPanelInfo.productName + "-R",
                                price: endPanelInfo.dealerPrice
                            )
                        );

                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: endPanelInfo.productId + "-L",
                                productName: endPanelInfo.productName + "-L",
                                price: endPanelInfo.dealerPrice
                            )
                        );
                    }
                }

                if(this._selections.brandCatalog.hasEndPanelsOption &&
                    cabinet.cabinetType.IsEnd() &&
                    cabinet.sizeInMeters.x > 0)
                {
                    string suffix = string.Empty;
                    if(cabinet.cabinetType == CabinetType.EndLeft)
                    {
                        suffix = "-L";
                    }
                    else if(cabinet.cabinetType == CabinetType.EndRight)
                    {
                        suffix = "-R";
                    }

                    if(suffix != string.Empty)
                    {
                        EndPanelInfo endPanelInfo =
                            this._selections.brandCatalog.
                            cabinetRuntimeEndPanelInfo.
                            GetEndPanelInfoByTargetDepthOrDefault(cabinet.sizeInMeters.z - this._selections.backPanelDepthInMeters);

                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: endPanelInfo.productId + suffix,
                                productName: endPanelInfo.productName + suffix,
                                price: endPanelInfo.dealerPrice
                            )
                        );
                    }
                }

                if(this._selections.brandCatalog.hasBackPanelsOption &&
                    this._selections.extraFeatures.Contains(KitchenExtraFeature.CabinetBackPanels) &&
                    cabinet.cabinetSectionProductInfo != null &&
                    !cabinet.cabinetSectionVisual.info.IsCategoryAllowedInSection(ProductCategorySpecific.Fridge))
                {
                    ProductInfo cabinetProductInfo = cabinet.cabinetSectionProductInfo;
                    float width = cabinet.sizeInMeters.x;
                    float depth = cabinet.sizeInMeters.z;
                    bool isCornerBackPanel = false;

                    if(cabinet.Is(CabinetType.Corner))
                    {
                        width = cabinet.sizeInMeters.x - this._selections.backPanelDepthInMeters;
                        depth = cabinet.sizeInMeters.z - this._selections.backPanelDepthInMeters;
                        isCornerBackPanel = true;
                    }

                    FridgePanelInfo backPanel;
                    if(isCornerBackPanel)
                    {
                        backPanel = this._selections.brandCatalog.FindFirstCabinetBackPanelByDepth(depth, cabinetProductInfo.cabinetSectionCategoryId);
                    }
                    else
                    {
                        backPanel = this._selections.brandCatalog.FindFirstCabinetBackPanel(width, cabinetProductInfo.cabinetSectionCategoryId);
                    }

                    JsonDict backPanelEntry;

                    if(backPanel != null)
                    {
                        backPanelEntry = CreateManualPriceSheetEntry(
                            productSku: backPanel.productId,
                            productName: backPanel.productName,
                            price: backPanel.dealerPrice
                        );
                    }
                    else
                    {
                        FridgePanelInfo fallbackPanel = this._selections.brandCatalog.cabinetRuntimeBackPanelInfo.fallbackBackPanel;
                        string backPanelProductId = $"{backPanel.productId}-{backPanel.specificPanelWidthInMeters * Units.Meters2Inches}x{backPanel.panelHeight}";
                        string backPanelProductName = $"{backPanel.productName} {backPanel.specificPanelWidthInMeters * Units.Meters2Inches}\"W x {backPanel.panelHeight}\"H";

                        backPanelEntry = CreateManualPriceSheetEntry(
                            productSku: backPanelProductId,
                            productName: backPanelProductName,
                            price: backPanel.dealerPrice
                        );
                    }

                    manualPriceSheetEntries.Add(backPanelEntry);

                    if(isCornerBackPanel)
                    {
                        manualPriceSheetEntries.Add(backPanelEntry);
                    }
                }

                if (cabinet.occupants.Count <= 0 ||
                    cabinet.cabinetSectionVisual == null ||
                    !cabinet.cabinetSectionVisual.info.IsCategoryAllowedInSection(ProductCategorySpecific.Fridge))
                {
                    continue;
                }

                Occupant fridge = cabinet.occupants[0];

                ImperialUnit imperial =
                    ImperialUnit.FromTotalMeters(
                        fridge.currentCutOutSizeInMeters.x
                    );

                layout.GetCabinetNeighbours(
                    cabinet,
                    out Cabinet leftCabinet,
                    out Cabinet rightCabinet
                );

                int fridgeEndPanelCount = 0;

                if (!leftCabinet || leftCabinet.cabinetType.IsEnd())
                {
                    --cabinetEndPanelCount;
                    ++fridgeEndPanelCount;
                }

                if (!rightCabinet || rightCabinet.cabinetType.IsEnd())
                {
                    --cabinetEndPanelCount;
                    ++fridgeEndPanelCount;
                }

                float fridgeWidthInInches = (float)imperial.totalInches;

                {
                    FridgePanelInfo fridgePanel =
                        this._selections.brandCatalog.FindFirstFridgeBackPanel(
                            fridge.productInfo.productSizeInMeters.x
                        );

                    bool canAddFridgeBackPanel = brandRules.PricesFridgeBackPanels;

                    if(this._selections.brandCatalog.ruleset == BrandCatalogRuleset.Napoleon && fridgeEndPanelCount > 0)
                    {
                        canAddFridgeBackPanel = false;
                    }

                    if (fridgePanel != null && canAddFridgeBackPanel)
                    {
                        string productId;
                        string productName;

                        productId = $"{fridgePanel.productId}-{fridgeWidthInInches}x{fridgePanel.panelHeight}";
                        productName = $"{fridgePanel.productName} {fridgeWidthInInches}\"W x {fridgePanel.panelHeight}\"H";

                        if (fridgePanel.specificPanelWidthInMeters > 0.0f)
                        {
                            if(brandRules.FridgePanelSkuIncludesCabinetSize)
                            {
                                fridgeWidthInInches += 1.5f;
                                productId = $"{fridgePanel.productId}x{cabinet.sizeInMeters.z * Units.Meters2Inches}x{cabinet.sizeInMeters.y * Units.Meters2Inches}";
                                productName = fridgePanel.productName;
                            }
                            else
                            {
                                productId = fridgePanel.productId;
                                productName = fridgePanel.productName;
                            }

                            fridgeWidthInInches = fridgePanel.specificPanelWidthInMeters * Units.Meters2Inches;
                        }

                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: productId,
                                productName: productName,
                                price: fridgePanel.dealerPrice
                            )
                        );
                    }
                }

                {
                    if(!brandRules.PricesFridgeEndPanels)
                    {
                        fridgeEndPanelCount = 0;
                    }

                    FridgePanelInfo endPanel =
                        this._selections.brandCatalog.fridgeEndPanelInfo;

                    string productId = $"{endPanel.productId}-{this._selections.brandCatalog.standardCabinetDepthInInches}x{endPanel.panelHeight}";
                    if (this._selections.brandCatalog.ruleset == BrandCatalogRuleset.Challenger)
                    {
                        productId = $"{endPanel.productId}-{this._selections.brandCatalog.standardCabinetDepthInInches}D";
                    }

                    string productName = $"{endPanel.productName} {endPanel.panelHeight}\"H x {this._selections.brandCatalog.standardCabinetDepthInInches}\"D";

                    if (endPanel.specificPanelWidthInMeters > 0.0f)
                    {
                        fridgeWidthInInches = endPanel.specificPanelWidthInMeters * Units.Meters2Inches;

                        productId = endPanel.productId;
                        productName = endPanel.productName;
                    }

                    for (; fridgeEndPanelCount > 0; --fridgeEndPanelCount)
                    {
                        manualPriceSheetEntries.Add(
                            CreateManualPriceSheetEntry(
                                productSku: productId,
                                productName: productName,
                                price: endPanel.dealerPrice
                            )
                        );
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(this._selections.brandCatalog.cabinetEndPanelInfo.productId))
            {
                FridgePanelInfo cabinetEndPanelInfo =
                    this._selections.brandCatalog.cabinetEndPanelInfo;

                JsonDict cabinetEndPanelEntry =
                    CreateManualPriceSheetEntry(
                        productSku: cabinetEndPanelInfo.productId,
                        productName: cabinetEndPanelInfo.productName,
                        price: cabinetEndPanelInfo.dealerPrice
                    );

                for (int i = 0; i < cabinetEndPanelCount; ++i)
                {
                    manualPriceSheetEntries.Add(cabinetEndPanelEntry);
                }
            }

            if(brandRules.PricesCountertopsFromCatalog)
            {
                JsonDict countertopPricing = TryGetCountertopPricing(layout);

                if(countertopPricing.ContainsKey("no_countertop") == false)
                {
                    manualPriceSheetEntries.Add(countertopPricing);
                }
            }

            if (this._selections.brandCatalog.ruleset == BrandCatalogRuleset.Challenger)
            {
                if (this._selections.countertopFinish != null &&
                    this._catalogData.FindManufacturerById(this._selections.countertopFinish.manufacturerId, out ManufacturerInfo countertopFinishManufacturer) &&
                    this._catalogData.FindManufacturerByName(this._selections.brandCatalog.catalogName, out ManufacturerInfo challengerManufacturer) &&
                    countertopFinishManufacturer.id == challengerManufacturer.id)
                {
                    const int ChallengerBrushedStainlessFinishId = 1960;

                    List<ChallengerCountertopInfo> depthToPrice =
                        this._selections.countertopFinish.id == ChallengerBrushedStainlessFinishId ?
                            new List<ChallengerCountertopInfo>()
                            {
                                new ()
                                {
                                    depthInInches = 13.0f,
                                    sku = "SST-13",
                                    description = "Countertop, 13\"D x 1.25\" Thick",
                                    pricePerLinearInch = 42.0f,
                                },
                                new ()
                                {
                                    depthInInches = 18.0f,
                                    sku = "SST-18",
                                    description = "Countertop, 18\"D x 1.25\" Thick",
                                    pricePerLinearInch = 55.0f,
                                },
                                new ()
                                {
                                    depthInInches = 29.0f,
                                    sku = "SST-29",
                                    description = "Countertop, 29\"D x 1.25\" Thick",
                                    pricePerLinearInch = 82.0f,
                                },
                                new ()
                                {
                                    depthInInches = 30.0f,
                                    sku = "SST-30",
                                    description = "Countertop, 30\"D x 1.25\" Thick",
                                    pricePerLinearInch = 86.0f,
                                },
                                new ()
                                {
                                    depthInInches = 33.0f,
                                    sku = "SST-33",
                                    description = "Countertop, 33\"D x 1.25\" Thick",
                                    pricePerLinearInch = 93.0f,
                                },
                                new ()
                                {
                                    depthInInches = 41.0f,
                                    sku = "SST-41",
                                    description = "Countertop, 41\"D x 1.25\" Thick",
                                    pricePerLinearInch = 118.0f,
                                },
                            } :
                            new List<ChallengerCountertopInfo>()
                            {
                                new ()
                                {
                                    depthInInches = 13.0f,
                                    sku = "ACT-13",
                                    description = "Countertop, 13\"D x 1.25\" Thick",
                                    pricePerLinearInch = 19.0f,
                                },
                                new ()
                                {
                                    depthInInches = 18.0f,
                                    sku = "ACT-18",
                                    description = "Countertop, 18\"D x 1.25\" Thick",
                                    pricePerLinearInch = 25.0f,
                                },
                                new ()
                                {
                                    depthInInches = 29.0f,
                                    sku = "ACT-29",
                                    description = "Countertop, 29\"D x 1.25\" Thick",
                                    pricePerLinearInch = 38.0f,
                                },
                                new ()
                                {
                                    depthInInches = 30.0f,
                                    sku = "ACT-30",
                                    description = "Countertop, 30\"D x 1.25\" Thick",
                                    pricePerLinearInch = 41.0f,
                                },
                                new ()
                                {
                                    depthInInches = 33.0f,
                                    sku = "ACT-33",
                                    description = "Countertop, 33\"D x 1.25\" Thick",
                                    pricePerLinearInch = 44.0f,
                                },
                                new ()
                                {
                                    depthInInches = 41.0f,
                                    sku = "ACT-41",
                                    description = "Countertop, 41\"D x 1.25\" Thick",
                                    pricePerLinearInch = 56.0f,
                                },
                            };

                    float backsplashPricePerLinearInch =
                        this._selections.countertopFinish.id == ChallengerBrushedStainlessFinishId ?
                            12.0f : 5.0f;

                    float highBarTopPricePerLinearInch =
                        this._selections.countertopFinish.id == ChallengerBrushedStainlessFinishId ?
                            63.0f : 40.0f;

                    float lastSpanDepthInMeters = 0.0f;
                    float lastSpanBackingDepth = 0.0f;
                    float totalLinearPricing = 0.0f;

                    foreach (CabinetSpan span in layout.spans)
                    {
                        Cuboid outerCuboid =
                            span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                        if (span.IsSpace())
                        {
                            lastSpanDepthInMeters = 0.0f;
                            lastSpanBackingDepth = 0.0f;
                            continue;
                        }

                        float backEdgeLinearLengthInMeters =
                            outerCuboid.width - lastSpanDepthInMeters;

                        lastSpanDepthInMeters = outerCuboid.depth;

                        float lastSpanDepthInInches = lastSpanDepthInMeters * Units.Meters2Inches;

                        const float Smudge = 0.001f;

                        foreach (ChallengerCountertopInfo info in depthToPrice)
                        {
                            if (info.depthInInches >= lastSpanDepthInInches - Smudge)
                            {
                                float inchesWide = backEdgeLinearLengthInMeters * Units.Meters2Inches;

                                totalLinearPricing += inchesWide * info.pricePerLinearInch;

                                break;
                            }
                        }

                        float spanBackingDepth = 0.0f;

                        if (span.backing && span.backing.info.category != ProductCategory.BarTopLow)
                        {
                            float backingPricePerLinearInch =
                                span.backing.info.category == ProductCategory.Backsplash ?
                                    backsplashPricePerLinearInch :
                                    highBarTopPricePerLinearInch;

                            Cuboid backingCuboid =
                                span.backing.GetCountertopCuboidInSpanSpace(default);

                            spanBackingDepth = backingCuboid.depth;

                            totalLinearPricing += (backingCuboid.width - lastSpanBackingDepth) * Units.Meters2Inches * backingPricePerLinearInch;
                        }

                        lastSpanBackingDepth = spanBackingDepth;
                    }

                    JsonDict countertopFinishEntry =
                        CreateManualPriceSheetEntry(
                            this._selections.countertopFinish.sku,
                            this._selections.countertopFinish.name,
                            totalLinearPricing
                        );

                    manualPriceSheetEntries.Add(countertopFinishEntry);
                }

                if (toeKickLinearMeters > 0.0f
                    && brandRules.PricesToeKicks)
                {
                    float toeKickLinearInches = toeKickLinearMeters * Units.Meters2Inches;

                    int numberOfToeKicks =
                        toeKickLinearInches switch
                        {
                            <= 80.0f => 1,
                            <= 182.0f => 2,
                            <= 260.0f => 3,
                            <= 356.0f => 4,

                            _ => 4 + Mathf.CeilToInt((toeKickLinearInches - 356.0f) / 96.0f)
                        };

                    JsonDict toeKickEntry =
                        CreateManualPriceSheetEntry(
                            productSku: $"7TK-94W",
                            productName: $"Toe Kick, 94\" Powder Coated Aluminum",
                            price: 559.0f
                        );

                    for (int i = 0; i < numberOfToeKicks; ++i)
                    {
                        manualPriceSheetEntries.Add(toeKickEntry);
                    }
                }
            }

            if (this._selections.brandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong &&
                this._selections.cabinetFinish != null)
            {
                if(brandRules.PricesAmericanOutdoorBaseEnds)
                {
                    manualPriceSheetEntries.Add(
                        CreateManualPriceSheetEntry(
                            productSku: $"AOBEP-2PK",
                            productName: $"Base Ends - 2-Pack",
                            price: 0.0f
                        )
                    );
                }
                else if(!brandRules.PricesBaseEnds)
                {
                }
                else
                {
                    manualPriceSheetEntries.Add(
                        CreateManualPriceSheetEntry(
                            productSku: $"BEP28.530-2PK",
                            productName: $"28.5\"W x 30\"H Base End w/Vents - 2-Pack",
                            price: 0.0f
                        )
                    );
                }

                const float WeatherStrongToeKickLengthInMeters =
                    96.0f * Units.Inches2Meters;

                int numberOfToeKicks =
                    Mathf.CeilToInt(toeKickLinearMeters / WeatherStrongToeKickLengthInMeters);

                if(!brandRules.PricesToeKicks)
                {
                    numberOfToeKicks = 0;
                }

                JsonDict toeKickEntry =
                    CreateManualPriceSheetEntry(
                        productSku: $"TKF696",
                        productName: $"96\"W x 6\"H Toe Kick/Filler with 8 Clips",
                        price: 483.0f
                    );

                if(brandRules.UsesEnduraToeKick)
                {
                    toeKickEntry = CreateManualPriceSheetEntry(
                        productSku: $"EN-TK96",
                        productName: $"96\"W x 5\"H Toe Kick/Filler",
                        price: 161.0f
                    );
                }

                for (int i = 0; i < numberOfToeKicks; ++i)
                {
                    manualPriceSheetEntries.Add(toeKickEntry);
                }
            }
        }

        private JsonDict TryGetCountertopPricing(CabinetLayout layout)
        {
            if(this._selections.countertopFinish == null)
            {
                return new Dictionary<string, object>()
                {
                    { "no_countertop", string.Empty }
                };
            }

            bool hasFoundFinishManufacturer = this._catalogData.FindManufacturerById(this._selections.countertopFinish.manufacturerId, out ManufacturerInfo countertopFinishManufacturer);

            if (hasFoundFinishManufacturer &&
                countertopFinishManufacturer.id == this._selections.brandCatalog.cabinetManufacturerId)
            {
                float usedPrice = this._selections.countertopFinish.priceCustomer;

                const int ChallengerBrushedStainlessFinishId = 1960;

                float backsplashPricePerLinearInch =
                    this._selections.countertopFinish.id == ChallengerBrushedStainlessFinishId ?
                        12.0f : 5.0f;

                float highBarTopPricePerLinearInch =
                    this._selections.countertopFinish.id == ChallengerBrushedStainlessFinishId ?
                        63.0f : 40.0f;

                float lastSpanDepthInMeters = 0.0f;
                float lastSpanBackingDepth = 0.0f;
                float totalLinearPricing = 0.0f;

                foreach (CabinetSpan span in layout.spans)
                {
                    Cuboid outerCuboid =
                        span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                    if (span.IsSpace())
                    {
                        lastSpanDepthInMeters = 0.0f;
                        lastSpanBackingDepth = 0.0f;
                        continue;
                    }

                    float backEdgeLinearLengthInMeters =
                        outerCuboid.width - lastSpanDepthInMeters;

                    lastSpanDepthInMeters = outerCuboid.depth;

                    float inchesWide = backEdgeLinearLengthInMeters * Units.Meters2Inches;

                    totalLinearPricing += inchesWide * usedPrice;

                    float spanBackingDepth = 0.0f;

                    if (span.backing && span.backing.info.category != ProductCategory.BarTopLow)
                    {
                        float backingPricePerLinearInch =
                            span.backing.info.category == ProductCategory.Backsplash ?
                                backsplashPricePerLinearInch :
                                highBarTopPricePerLinearInch;

                        Cuboid backingCuboid =
                            span.backing.GetCountertopCuboidInSpanSpace(default);

                        spanBackingDepth = backingCuboid.depth;

                        totalLinearPricing += (backingCuboid.width - lastSpanBackingDepth) * Units.Meters2Inches * backingPricePerLinearInch;
                    }

                    lastSpanBackingDepth = spanBackingDepth;
                }

                return new Dictionary<string, object>()
                    {
                        { "product_id", this._selections.countertopFinish.sku },
                        { "name", this._selections.countertopFinish.name },
                        { "price", totalLinearPricing }
                    };
            }
            else
            {
                return new Dictionary<string, object>()
                    {
                        { "no_countertop", string.Empty }
                    };
            }
        }
    }
}
