# Kitchen model

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Kitchen model** (area 4 of the refactor, from 2026-10-23; plan:
  `.claude/prompts/04-kitchen-model.md`).
  - Enums (B4.1) in `_projectShared/Enums/`, values as before: `CabinetType`,
    `KitchenBuildingMethod` (flags, because cabinet prefabs store sets of methods:
    `m_AllowedFitToProductWidthBuildingMethods`, `m_ForceProductsToCenterBuildingMethods`),
    `CountertopType`, `CabinetToeKickType`, `KitchenStyle`, `KitchenExtraFeature`, `Extent`,
    `MeasurementAxis`, `CabinetLocalDirection`, `BrandCatalogRuleset`, `OccupantCuboidStatus`, and
    the ones that were nested in their class, renamed so they stand on their own:
    `CabinetResizeAxis` (`Cabinet.ResizeAxis`), `RiserMaterialMode`, `CutOutCubeFace`,
    `ShelfBottomShape`, `VolumeOverlap` (`OccupiableVolume.IsOverlap`), `MeasureDisplayFlag`,
    `MeasureMode`, `SlicedMeshSkinScaleMode`, `SlicedMeshAxisScaling`,
    `ProductVisualEditorSetupStatus`. **The design JSON stores `cabinet_type`,
    `kitchen_layout_style`, `kitchen_building_method`, `countertop_type`, `toe_kick_type` and
    `extra_features` by value name**, so those names are frozen (R-DATA-3) as well as the numbers.
    Assets store the model's enums as `MelcherStableEnum` (by name) or as plain ints, so the type
    names were free to change. Their pure helpers (`CabinetTypeExtensions`, `KitchenStyleExtensions`,
    `CabinetLocalDirectionExtensions`) are in `_projectShared/Common/`.
  - Data and value types (B4.2). ScriptableObjects in `_project/Scripts/Scriptables/`:
    `BrandCatalogSO`, `KitchenTemplateSO`, `CabinetSectionDoorStyleSO`,
    `CabinetSectionDoorHandleStyleSO`, `CabinetSectionLegStyleSO`, `CabinetSectionDoorEditorMappingSO`
    (moved with their `.meta`; 61 assets, names unchanged). The editor mapping is loaded by path
    (`Resources/CabinetSectionDoorEditorMapping.asset`); the template's create menu keeps the default
    file name `KitchenTemplate`. `BrandCatalogSO.catalogTag` (`m_CatalogTag`, empty means the
    catalog name; team commit `c186aad175`, ported 2026-11-12) is what a door visual's
    `brandCatalogName` is matched against when the thumbnail creator looks up its catalog, so
    several catalogs can share one set of door visuals; no catalog sets a tag yet. Serializables in
    `_project/Scripts/Serializables/`:
    `BackPanelInfo(s)`, `EndPanelInfo(s)`, `CabinetSectionDoorData` (cabinet id → door prefab name),
    `CabinetSectionDoorHandleData` (cabinet id → handle count), `Price` (a price per currency; only
    the catalogs' panel prices live in the app, everything else is priced by the server),
    `DoorStyleInfo`, `FridgePanelInfo` (despite the name, any panel at the end of a fridge or a
    cabinet, with its price-sheet price), `BuilderSpecificDistance` (a distance that differs between
    ProFit and Stoll), `SlicedMeshSlice` (its `scaleAlongX/Y/Z` were replaced by `scalingX/Y/Z`;
    `SlicedMesh` migrates old data when it validates). They bind by field name, so taking them out
    of their classes changes no asset. Geometry in `_project/Scripts/Geometry/`
    (`KitchenDesign.Geometry`): `Cuboid`, `CuboidPadding`, `OrientedBounds`, `LineSegment`,
    `Vector3T`, `VectorUtility`, `CabinetLocalDirectionUtil` (comments kept: moved, not
    refactored). Checked with `kitchendatacensus.cs` (every serialized value of the 61 assets).
  - `KitchenSelections` (B4.3, `_project/Scripts/KitchenModel/`, `KitchenDesign.KitchenModel`): the
    user's choices for the open design (building method, brand catalog, template, layout prefab,
    door and handle style, end and back panel sizes, countertop type, the three finishes, base
    cabinet depth, countertop thickness and overhang, toe kick, extra features, analytics key,
    session start). Owned by `Kitchen`, whose `selected…` properties forward to it, and handed to
    the parts that read the selections. Public fields, because the load passes several of them as
    `out` arguments. The back panel depth is the extra depth added to the back of every cabinet when
    back panels are on (Gemini), kept so it can be removed again and so authored corners' overhang
    can be corrected. The overhang works because the countertop's 27-slice mesh has a 1" overhang
    beyond its size on every side but the top; X and Z set the left/right and front/back overhangs.
    The end panel setters on `Kitchen` toggle: setting the current width again sets it to 0.
  - **Save and price inputs** (B4.4). `KitchenSaveWriter` (`_project/Scripts/Save/`,
    `KitchenDesign.Save`) builds the design JSON exactly as `Kitchen.SaveToJson` did (that method now
    calls it). It writes two independent halves: what the server reads to build the quote PDF
    (`islandType`, `islandDimensions`, the finish SKUs, the legacy `modelSaveData`, the top and
    front measurements, `cabinet_list`, `manual_price_sheet_entries`, `price_changes`,
    `countertop_area`, the disclaimer…) and what the app reads to reload the design (`cabinets`,
    `cabinet_spans`, `kitchen_layout_style`, …). Some logic is duplicated between them (custom
    cut-out sizes). An occupant's cut-out size is saved only when it differs from the product's
    default, so changing a product's cut-out on the dashboard updates old designs. The JSON key
    strings are the frozen format (R-DATA-3). Areas: ProFit and Stoll count the cabinet finish area
    without the hidden faces where spans meet; occupants on the front or back subtract their cut-out
    from it; the countertop area no longer counts backings or plunges, nor subtracts cut-outs
    (client feedback: the customer pays for the cut-out area too). WeatherStrong fridge and ice box
    cabinets count only the back for the toe kick.
  - `KitchenSaveReader` (B4.5, `_project/Scripts/Save/`): loads a design (`LoadFromJson`, the
    occupants and shelves, the backing by SKU, and the requested save from the client list, which
    is how Edit Design opens a design). `Kitchen.LoadFromJson` and `TryLoadRequestedSave` forward to
    it. It still drives `Kitchen`'s building methods (layout, depth, thickness, finishes, widgets,
    cameras, ground), so it holds the `Kitchen`; that back-reference goes when layout building
    leaves `Kitchen`. It gets the layout prefabs, backing sprites and backing prefabs from
    `Kitchen`'s serialized fields, which stay there so the scene doesn't change.
  - Interaction parts (B4.6, `_project/Scripts/KitchenModel/`): `KitchenInteractionState` (the
    highlighted object, the volume being resized, the product being dragged; a resize or drag
    doesn't start while another is running, and the highlight ignores clicks during either),
    `KitchenWidgetsControl` (cabinet and shelf resize widgets, the size button, the dimension
    tickers, the top info label), `KitchenMaterialControl` (highlight materials and finishes,
    including the ground's wood or stone mesh; one class because the highlight reads the finish
    materials and a finish change re-applies the highlight), `OccupantDragControl` (dragging a
    product between cabinets and shelves, and the snapping). Constructed in `Kitchen.Awake` with the
    serialized references, which stay on `Kitchen` so the scene doesn't change, then connected to
    the widgets (materials and drag call the widgets; the widgets call nothing). They call back into
    `Kitchen` for the camera, the ground, the cabinet height transitions, fitting a cabinet to a
    product, rotating the camera to a cabinet, and the centring rule; those go when areas 5 and 6
    take the cameras and the editing commands. `Kitchen.OnEnable` still makes every
    `EventBridge.Designer` subscription in the old order, pointing at the parts (plan, decision 2).
  - Price inputs, `_project/Scripts/Pricing/` (`KitchenDesign.Pricing`): `PriceSheetInputs` (the
    manual price-sheet lines and the per-product price factors), `IPriceSheetExtras` with
    `StollPriceSheet` and `GeminiCatalogPriceSheet`, picked by the app rule `PriceSheetExtras`.
    The prices and SKUs in them are business rules agreed with the clients (sources in the old
    code's comments, Basecamp threads):
    - Stoll: a setup fee on every custom kitchen; countertops priced per sq. ft. of overall length
      × depth (contiguous spans share the corner; the client's own example divided by 35 where it
      meant 144); cabinet finishes by the summed outside length of the spans in feet; custom
      cabinets at their price; a corbel per pair of visible back column halves on high bar tops; a
      backsplash line per foot.
    - Gemini catalog: Challenger's premium finish package per cabinet; Shaker doors +5% and Deco
      Grain finishes +25% on cabinet prices (as price factors); handles per door handle; fillers
      added twice (front and back, "-Melony"); two 45° transition pieces per Napoleon corner (two
      45s make a 90); Agora corners priced as their own SKU and Agora kamado grill cabinets forced
      to get end panels on the sides not next to an end cabinet; runtime end and back panels;
      fridge panels (not on Agora or Toppin; fridge back panels not on American Outdoor Cabinets
      or Endura); cabinet end panels when the catalog has a SKU; Challenger and Agora Berlin
      Gardens countertops by linear length at the countertop finish's customer price, when the
      finish's manufacturer is the catalog's cabinet manufacturer (corners counted once;
      backsplashes and high bar tops per linear inch, brushed stainless dearer). Challenger used
      fixed prices by depth band until the team's `dbd1a82fdb` (2026-11-05), ported 2026-11-12;
      toe kicks (the first
      few follow the client's table, then one per 96"; WeatherStrong: front + back + 54" for the
      ends, divided by 96, rounded up); WeatherStrong end vents; no toe kick line for Agora.
  - **Building-method rules** (B4.7): `IBuildingMethodRules` (`KitchenDesign.Shared.Interfaces`)
    and `BuildingMethodRules` (`_project/Scripts/BuildingRules/`, `KitchenDesign.BuildingRules`): one
    class constructed with the method, each rule written as the expression it replaced, so it is
    exact for every value, including the unset 0 the kitchen holds before `Restart` (every old
    comparison was false then). Model code asks `kitchen.buildingRules` (on `IKitchen`; `Kitchen`
    keeps one instance per selected method); the save writer and `RuntimeProductOverrides`
    construct one. **No model code compares the building method any more**; the screens (area 5)
    and the photo manager (area 6) still do. `CabinetTypeExtensions.IsBlindCorner` /
    `IsMidSpanCabinetType` still take the method: they are Shared helpers the screens use too, and
    they encode the same rule as `HasBlindCorners`. `CabinetLayout.CabinetHasMidSpanCabinetNeighbour`
    lost its building-method branch: both branches computed the same thing. The rules
    (Standard / Sections / Catalog):
    - Catalog only: `UsesBrandCatalog` (the catalog sets cabinet height and depth, the default toe
      kick, legs under height transitions, fridge back panels and the procedural corner height; the
      brand catalog screen comes first; the Deco Grain notice; the catalog-adjusted product inset and
      smoker cut-out height are kept), `CanToggleToeKicks`, `FridgeCabinetsResizableOnlyWhenEmpty`
      (Gemini's fridge panels), `AppliesMidRunEndPanelPadding`, `HasBlindCorners`,
      `ClonedSectionsCopyTheirCabinet`.
    - Sections only (Stoll): `HasCabinetColumns` (columns, column-based size limits, corner shims),
      `LimitsCabinetSizeToPreventBowing`, `LabelsCabinetsByProductName` (others show the SKU),
      `BackingRiserUsesCabinetMaterial`, `UsesStollBuilderDistances`, `ShelvesFitWidthToProduct`
      (plunges sized to their burner and locked while full), `BackingSpansTakeOnlyStandardHeightCabinets`
      (36"), `ReportsCabinetFinishLinearLength`, `CabinetProductIsOptional`; and
      `BacksplashesHaveCountertop` false; `DefaultCountertopOverhangInMeters` ½" (else 1").
    - Standard only (ProFit): `PadsCornersForAdjacentSpans`, `SizesStraightCabinetsToOutsideEdge`
      (15" outside edge on a new kitchen); and `ReclickingProductSelectsItsVolume` false.
    - Sections and Catalog: `CabinetsHaveTheirOwnDepth`, `CabinetsAreProducts` (cabinets come from
      cabinet-section products and a load rebuilds them), `ProductsCenteredInCabinets`.
    - Standard and Sections: `PricesCabinetFinishByArea`.
    Checked with `Tools/Baseline/unity6/buildingrulescheck.cs` (every rule against the old
    expression for the four values: 96 checks).
    **The screens ask the same rules since B5.6 (2026-11-13)**; no screen compares the building
    method any more (`BuildingMethodUI` only names the three values its toggles select). Eight
    rules were added for them: `HasShelfProducts` (not Catalog: no plunge / shelf category),
    `UserChoosesCabinetDepth` (Standard: the depth control on the sizes screen and the set-width
    prompt), `HasSingleCabinetDepth` (Sections: 29" only; the others offer 24" to 36"),
    `HasSecondCountertopThickness` (Sections: Stoll's second thickness control and its own
    thickness options), `ProductsMoveUpAndDown` (not Sections), `CabinetSectionManufacturerName`
    ("Stoll" in Sections, else null: the finishes offered are limited to it; Catalog limits them
    to the catalog's name), `UsesStollHighBarTopOverhang` (Sections: the toolbar sets the high bar
    top prefab's back overhang to 10", for a 14" deep top with the 1" front overhang and 3" riser;
    never restored, which can't matter while an app has one method), `SelectsWholeKitchen`
    (Standard: a click on a cabinet selects the kitchen, and every cabinet but a `Space` can be
    clicked). The screens reuse the existing ones where the meaning is the same: `UsesBrandCatalog`
    (brand catalog screen in the wizard, catalog-driven toolbar categories and finishes, the
    catalog's cabinet depth), `CabinetsAreProducts` (the Add Cabinets category), `CanToggleToeKicks`,
    `HasCabinetColumns` (a corner's ticker adds the neighbouring span's columns),
    `CabinetProductIsOptional` (Stoll's procedural section, id 0), `HasBlindCorners` (the click
    picks the blind cabinet for a procedural corner), `SizesStraightCabinetsToOutsideEdge` (the
    set-width prompt shows the outside length) and `DefaultCountertopOverhangInMeters`. The two
    `OnBuildingMethodChanged` handlers read `kitchen.buildingRules` instead of their argument: the
    kitchen raises the event with its own selection, so they are the same. The check now makes
    128 comparisons. The photo manager's five checks wait for area 6.
  - **Brand rules** (B4.8): `BrandRules` (`_project/Scripts/Brands/`, `KitchenDesign.Brands`), a
    readonly struct built from a catalog's cabinet manufacturer id (`brandCatalog.rules`; a struct so
    asking costs nothing and a runtime clone of a catalog can't hold a stale copy). It replaces every
    `Const.Gemini.ManufactersIds` comparison (`Const.Gemini` is gone). Pricing: `PricesToeKicks` (not
    Agora Berlin Gardens), `PricesFridgeBackPanels` (not American Outdoor Cabinets or Endura),
    `PricesFridgeEndPanels` (not Agora or Toppin), `PricesCountertopsFromCatalog`,
    `PricesCornersAsOwnProduct`, `ForcesKamadoGrillEndPanels` (GADA/GADB cabinets),
    `FridgePanelSkuIncludesCabinetSize` (all four Agora), `PricesAmericanOutdoorBaseEnds`,
    `PricesBaseEnds` (not Endura or Agora), `UsesEnduraToeKick`. Products:
    `HeatedOrBurnerProductSidePaddingInMeters(isSmoker)` (Agora 2¼", smokers 0; Toppin, Endura,
    WeatherStrong, WeatherStrong Select, NatureKast 3"; otherwise unchanged) and the Challenger
    occupant padding constant (4"). The `BrandCatalogRuleset` checks (`Challenger`, `Napoleon`,
    `WeatherStrong`) stay as they are: the ruleset is the catalog's own serialized rule key, which
    the original author designed for this; the manufacturer ids were the workaround, used because
    American Outdoor, Endura and Agora catalogs share the WeatherStrong ruleset. Deco Grain is a
    finish manufacturer (140), not a cabinet brand, and stays where it is. The values are code, not
    asset data (plan, decision 6). Checked with `Tools/Baseline/unity6/brandrulescheck.cs` (every
    rule against the old expression for every id in the old constants and every catalog asset).
  - `DoorStyleLookup` (B4.8, `_project/Scripts/CatalogData/`): the door-style lookup that was the
    static `CabinetSectionDoorUtility`, now an instance owned by `CatalogDataStatus` (`DoorStyles`);
    the brand catalog screen hands it to `BrandCatalogSO.PopulateRuntimeData`, other code reaches
    it through the catalog data it was handed. It maps a cabinet section id to the visual id whose doors
    it uses, from every catalog's `CabinetSectionDoorStyleSO`, and finds a door visual by name
    (`ProductVisual_<cabinet>@<door style>^`, falling back to the catalog's default style). Kept
    exactly as it was, two quirks included: a catalog is never recorded as initialized, so its
    mapping is re-added on every call (an overwrite with the same values), and the lookups are null
    until the first catalog initializes them.
  - **`Kitchen.instance` and the app systems** (B4.9). No model or photo code uses `Kitchen.instance`
    any more: `CabinetShelf.isResizable` reaches the kitchen through its occupant's cabinet, the
    shelf's cuboid callback through the volume testing it (walking up to the cabinet),
    `CabinetBacking.OnCabinetMaterialsChanged` takes the kitchen from its callers (the span, the
    material control, the photo manager), and the photo manager passes its kitchen down. **The static
    is gone since B5.4c (2026-11-12):** `DesignerScreensControl.Initialize` hands the kitchen to the
    toolbar, the setup wizard's shape, template and sizes screens, the white-label client info, the
    product search and the about-product prompt; `KitchenWidgetsControl` hands it to each cabinet
    ticker it creates; `BrandCatalogUI` reads the product visual database through its own
    serialized reference to the same asset (its use runs in `Awake`, before any initializer). Tool
    snippets find the kitchen with `FindFirstObjectByType`. The classes area 4 created
    (the three controls, the save writer and reader, the Gemini price sheet) take the app systems
    they read (session, client list, catalog data, app profile, app rules, product content) in their
    constructors; `Kitchen.Initialize` hands them over from the `AppSystems` it is given (B8.1).
    Model objects ask their kitchen (B8.2): `IKitchen` has `appRules`, `catalogData`,
    `productContent` and `session`; `Cabinet`, `CabinetLayout`, `CabinetSpan` and `Occupant` read
    them there, `CabinetSectionDoorHandle.InstantiateHandle` takes the door-style lookup,
    `KitchenWidgetsControl` gives each `ResizerWidget` the session and each `CabinetTickerWidget`
    the session and the rules. `EndPanelInfo` and `FridgePanelInfo` have `GetDealerPrice(currency)`
    (the price sheet passes the user's currency), and `BrandCatalogSO.info` is gone: the toolbar
    looks the server's catalog record up in `CatalogDataStatus`. The model's profile, options and
    skin reads (default materials, highlight materials, measure colours) go to `AppProfileStore`
    (B8.4): a cabinet made in edit mode by the thumbnail creator has no kitchen to ask.
  - **Where the model's files are** (B4.10, `_project/Scripts/`, namespaces follow the folders):
    `KitchenModel/` (`Kitchen`, `IKitchen`, `KitchenCutoutClipping`, `KitchenSelections`,
    `KitchenInteractionState`, the three controls), `Cabinets/` (`Cabinet`, `CabinetSpan`,
    `CabinetLayout`, `CabinetBacking`, `CabinetShelf`, `CabinetCutOut`, `ICabinet`,
    `CabinetSectionDoorHandle`, `CabinetSectionInfo`, `CabinetSectionLeg`), `Occupancy/`
    (`Occupant`, `OccupiableVolume`, `VolumeOccupantResult`, `IOccupantListener` and the three handler
    interfaces, the two cuboid visualisations), `Measures/` (`Measure`, `MeasurementOverlayManager`),
    `Widgets/` (`KitchenWidget`, `ResizerWidget`, `ButtonWidget`), `Products/`
    (`ProductVisualContent` and its editor partial), `Geometry/` (`SlicedMesh` with the structs),
    `Save/`, `Pricing/`, `BuildingRules/`, `Brands/`, `CatalogData/DoorStyleLookup`. Moved with their
    `.meta` (scenes and prefabs bind by GUID, so no asset changed); comments kept in files that only
    moved.
