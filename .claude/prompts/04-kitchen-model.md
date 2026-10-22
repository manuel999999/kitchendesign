# 04 — The kitchen model (area 4 of the refactor)

Planned 2026-10-23. Area 4 of `03-refactor.md` (read its "The recipe for each area", "Checks" and
the area 1–3 sections first). The user left the design and the details to Claude: write this plan,
then build it step by step like areas 1–3, each step checked and committed on its own, no
behaviour change.

## The old repo first (checked 2026-10-23)
Every commit in the old repo (`/Users/emmanuel/Documents/Unity/Gemini`, all branches and stashes)
since the copy's source commit `7700e16a8c` that touches `Kitchen`, `Cabinet`, `CabinetSpan`,
`CabinetLayout`, `Occupant` or `OccupiableVolume` was test-applied to the copy's tree, forwards and
in reverse, and matched by title and `git patch-id` against the copy's history. Then the same for
every later `.cs` commit, since area 4 also takes `CabinetShelf`, `CabinetBacking`, `BrandCatalog`
and the measures.
- **Already in the copy:** the eight 45° corner commits and the `CanAddOccupantAnywhere` fix
  (`origin/feature/pro-fit-45-degree-corners`, `origin/fix/add-occupant-anywhere-function`; the
  same patches under other hashes, identical patch ids), "Fix products not being draggable into
  plunges", "Update agora countertop pricing", "Add countertop thickness refresh on kitchen ready
  for stoll" (`ac8056db10`; in `Kitchen` as the `HighBarTopCountertopThicknessInMeters` re-apply),
  the door handle fallback, the product lookup dictionary, the Stoll compatible brand.
  `9d339931a5` "Update weather strong cabinet padding" doesn't reverse-apply only because the
  surrounding lines moved; its content (WeatherStrong 92 and WeatherStrong Select at 3") is in
  `RuntimeProductOverrides`.
- **Brought over before (area 1):** `1e29fe3e9b` forced product occupancy; both of its `Cabinet`
  hunks are in place.
- **Waiting for the user:** `77f53840ee` "Add rear wall for cabinet spans" (new `CabinetWall`,
  changes to `CabinetSpan`, `CabinetLayout`, `Kitchen`, `BrandCatalog`, `DesignerToolbarUI`). After
  this area it no longer applies as a patch and is ported by hand. Where its `Kitchen` and
  `BrandCatalog` parts land is listed at the end of this file.
- No code commit in the old repo is newer than the 2026-10-22 audit; the newest stash
  (`stash@{0}` "Release-Settings", 2026-10-21) changes only `ProjectSettings.asset`.

**Nothing to bring over before the split.**

## Today (read 2026-10-23)
- **`Kitchen`** (7,565 lines, a `MelcherMonoBehaviour` on the `Kitchen` object of `DesignerScene`)
  is, by the fields each method uses:
  - the user's selections for the open design: building method, brand catalog, template, layout
    prefab, door and handle style, end/back panel sizes, countertop type, cabinet / countertop /
    flooring finish, base cabinet depth, countertop thickness and overhang, toe kick type, extra
    features, analytics key, session start time (19 fields);
  - **save** (`SaveToJson`, 1,838 lines: measurements, the reload data, the cabinet, toe kick and
    countertop areas, then the price inputs: a Stoll block of 205 lines and a Gemini block of 805
    lines with the brand checks; plus `AddOccupantsToJson`, `TryGetCountertopPricing`,
    `CanAddFridgeBackPanelToPriceSheet`, `GetLegacyMeasurementsForCabinetSave`);
  - **load** (`LoadFromJson` 602 lines, `LoadOccupantsFromJson` 203, `LoadBackingFromProductSku`
    79, `TryLoadRequestedSave`);
  - **layout building** (`CreateKitchen`, `CreateCabinetLayoutFromShape` 414 lines, depth /
    thickness / overhang setters, `UpdateCabinetHeightTransitions`, `ApplyDeltaToAllCabinetDepths`,
    `FindCabinetPrefab`, the ground and the camera target groups);
  - **selection and finishes** (`TryHighlightObject`, `UpdateHighlightMaterial`, `ChangeFinish`,
    the finish material getters);
  - **widgets** (cabinet and shelf resize widgets, size buttons, the dimension tickers, the top
    info label);
  - **dragging** (`OnOccupantDragBegin/Drag/DragEnd` 450 lines, the snapping helpers);
  - **editing commands** (add / clone / destroy product, section and backing, reorder, reposition,
    center, toe kicks; the occupant added/removed handlers with the Sections bowing limit).
  It subscribes to 33 `EventBridge.Designer` delegates in `OnEnable`.
- **`Kitchen.instance`** (a static set in `Awake`): 31 uses. 26 in screens (area 5), 1 in
  `KitchenScreenshotManager` (area 6), 3 in the model (`CabinetShelf` ×2, `CabinetBacking`). The rest
  of the model already gets the kitchen as `IKitchen` (`Cabinet.SetKitchen`, `CabinetLayout`,
  `CabinetSpan`'s constructor).
- **Building-method checks** (`KitchenBuildingMethod.Standard/Sections/Catalog`): about 120 in 22
  files: `Kitchen` 38, `CabinetLayout` 14, `Cabinet` 12, `KitchenScreenshotManager` 6, `CabinetShelf`
  2, `CabinetBacking` 2, `CabinetSpan`, `ProductVisualContent`, `MeasurementOverlayManager`,
  `CabinetTypeExtensions` 1 each; the rest in screens (`DesignerToolbarUI` 22).
- **Brand checks** (`Const.Gemini.ManufactersIds`, `isAgoraBerlinGardens`, `isEndura`, `isToppin`,
  `canHaveToeKickEntry`, `ruleset == Napoleon`): about 20 in `Kitchen`'s price inputs, 7 in
  `RuntimeProductOverrides`. `Const.Stoll` (bowing, smoker cut-out) in `Kitchen`, `Occupant`,
  `KitchenScreenshotManager`, `DesignerToolbarUI`.
- **Static state in the model:** `Kitchen.s_KitchenInstance`; `CabinetSectionDoorUtility`'s door
  mapping (built from every brand catalog's `CabinetSectionDoorStyle`, never reset). The other
  statics are scratch lists reused to avoid allocations (`Cabinet.s_OccupantsToCenter`,
  `Occupant.s_OccupantListeners`, …), which stay.
- **Enums of the area**, all with implicit values except `KitchenBuildingMethod` (flags) and
  `Measure.DisplayFlag`: `CabinetType`, `KitchenBuildingMethod`, `CountertopType` (in
  `CountertopStyle.cs`), `CabinetToeKickType`, `KitchenStyle`, `KitchenExtraFeature`, `Extent`,
  `MeasurementAxis`, `CabinetLocalDirection`, `BrandCatalogRuleset`, `OccupantCuboidStatus` (in
  `VolumeOccupantResult.cs`), and nested: `Cabinet.ResizeAxis`, `CabinetBacking.RiserMaterialMode`,
  `CabinetCutOut.CubeFace`, `CabinetShelf.BottomShape`, `OccupiableVolume.IsOverlap`,
  `Measure.DisplayFlag`, `MeasurementOverlayManager.MeasureMode`, `SlicedMesh.SkinScaleMode` and
  `AxisScaling`, `ProductVisualContent.EditorSetupStatus`. Assets store them as `MelcherStableEnum`
  (by value name: `m_EnumNames: Napoleon`) or as plain ints; **the design JSON stores
  `cabinet_type`, `kitchen_layout_style`, `kitchen_building_method`, `countertop_type`,
  `toe_kick_type` and `extra_features` by name** (`ToString()` / `Enum.Parse`), so the value names
  of those six are frozen (R-DATA-3), and every value keeps its number.
- **Data assets and serializables:** `BrandCatalog` (with nested `DoorStyleInfo`,
  `FridgePanelInfo`), `KitchenTemplate`, `CabinetSectionDoorStyle`, `CabinetSectionDoorHandleStyle`,
  `CabinetSectionLegStyle`, `CabinetSectionDoorEditorMapping` (ScriptableObjects without the `SO`
  suffix); `BackPanelInfo(s)`, `EndPanelInfo(s)`, `CabinetSectionDoorData`,
  `CabinetSectionDoorHandleData`, `CabinetSectionInfo`, `CabinetSectionLeg`,
  `CabinetBacking.BuilderSpecificDistance`, `SlicedMesh.Slice`, the geometry structs (`Cuboid`,
  `CuboidPadding`, `OrientedBounds`, `LineSegment`, `Vector3T`).

## Decisions
1. **`Kitchen` stays the model's root in the scene for this area, and its parts become plain
   classes it constructs in `Awake`**, getting the serialized references they need from it. Why:
   the designer's start-up (`DesignerScene.OnStateAdded` → `restart` → `Kitchen.Restart`) runs
   before `DesignerSceneInitializer.Initialize` (03-refactor, B1.2), so a part constructed by the
   initializer would not exist yet. When area 5 moves the scene start-up into the initializer, the
   initializer takes the construction over. Plain classes also keep the scene file unchanged (no
   serialized reference moves).
2. **The `EventBridge.Designer` subscriptions stay in `Kitchen.OnEnable/OnDisable`, in the same
   order, pointing at the parts' methods.** Why: several delegates have more than one listener
   (`onTryHighlightObject`, `onCabinetResized`, `onBrandCatalogChanged`, …), and a multicast
   delegate calls its listeners in subscription order. Keeping the subscription points keeps the
   order, so the split can't change behaviour through it.
3. **`EventBridge.Designer` itself stays for area 5.** Most of its delegates are awaited
   (`SequentiallyAwaitedDelegate`, a Doge event can't be awaited), their payloads are model types
   (`Cabinet`, `Occupant`) that can't move to `KitchenDesign.Shared`, and the other side is mostly
   screens. The model-internal ones (`onOccupantInstantiated`, `onOccupantDragBegin/Drag/DragEnd`,
   `onOccupantRemovedFromCabinet`, `onPreCabinetDestroyedByUser`) are listed for area 5.
4. **`Kitchen.instance` leaves the model** (`CabinetShelf`, `CabinetBacking` use the `IKitchen`
   they can already reach) and the photo manager (it is handed the kitchen already). The 26 screen
   uses stay until area 5 constructs the screens; the static stays as their one forward, like
   `ProFitGlobals`, and is listed in CLAUDE.md.
5. **Building-method rules** go behind `IBuildingMethodRules` (`KitchenDesign.Shared.Interfaces`)
   with `StandardBuildingRules`, `SectionsBuildingRules`, `CatalogBuildingRules`
   (`_project/Scripts/BuildingRules/`), picked from the kitchen's selected method. The same pattern
   as `IAppRules`: code asks the rule, never compares the method. App and building method stay two
   axes (each app has one method today). Model code only; the screens' checks move in area 5.
6. **Brand rules are code keyed by the catalog's cabinet manufacturer id**, in one `BrandRules`
   class reached from the catalog (`BrandCatalogSO.rules`). Not serialized asset data yet: the
   server can list catalogs that only get a clone of a preset with the same manufacturer, some ids
   (WeatherStrong Select, NatureKast) have no catalog asset in this copy, and an asset change can't
   be checked as "no behaviour change". Moving the values into the assets is a later content
   decision for the user. `Const.Gemini` goes into `BrandRules`.
7. **Comments:** code moved into a new class drops its comments (decision 2 of 03-refactor), and
   the whys worth keeping go into CLAUDE.md under that system. The price-sheet sources (Basecamp
   links, client quotes) are summarised there. Files only moved to their folder (`git mv`, namespace)
   keep their comments, as in B3.4.
8. **Names:** kitchen terms stay (`Kitchen`, `Cabinet`, `CabinetSpan`, `CabinetLayout`,
   `CabinetBacking`, `CabinetShelf`, `CabinetCutOut`, `Occupant`, `OccupiableVolume`,
   `ProductVisualContent`), so the old CLAUDE.md still reads correctly. New parts are named for what
   they are. A namespace never has the name of a type in it (`KitchenDesign.Kitchen` would hide the
   `Kitchen` class): the model's folder is `KitchenModel/`.

## Steps
Each step: compile (editor, and macOS player scripts when `#if UNITY_EDITOR` code moves), the
checks below, CLAUDE.md updated, one commit.

**B4.0 — An edit probe.** The baseline only loads, saves and photographs designs. Area 4 moves
the code behind adding, dragging, resizing and deleting, so a new play-mode check drives those
edits the way the toolbar does (`EventBridge.Designer` calls, the resize widget's own events through
reflection, drags with pointer positions projected from the camera), on design 28316 (Catalog: two
products, a low and a high bar top) and then on a new Standard L kitchen built in the same session
(free placement, drags and the resize widgets, which Catalog refuses). After each edit it waits
until the kitchen is stable and writes a snapshot (selections, spans and backings, every cabinet's
type, SKU, size, position and materials, every product's position, visible widgets); at the end
`measured.json` and `resave.json` (built, never sent). Read-only on the server. Two runs on the same
code must be identical before it is used as a reference. `Tools/Baseline/unity6/editprobe.sh`.
- **Done 2026-10-23.** Two runs on the unchanged code: every snapshot, both `resave.json` and the
  log identical; `measured.json` differs only in cameras (the idle cover-photo vcam keeps its last
  pose, several degrees apart between runs), so `compare_probe.py` ignores `cameras`. The first
  version raised the resize widget's `onResized` through reflection, which changed nothing: the
  widget resizes the cabinet in its own `OnDrag` and only then tells the kitchen, so the probe drives
  `OnBeginDrag/OnDrag/OnEndDrag` with projected pointer positions (the right cabinet grows 3"). In
  the Catalog phase the app refuses the nudge, the clone ("No Room Available") and the widget resize,
  as it does for a user; the Standard phase clones, drags the clone to another cabinet and resizes.
  Reference: `GeminiBaseline/editprobe-HEAD-2026-10-23`. Committed with this plan.

**B4.1 — The model's enums to Shared.** The enums listed above to `_projectShared/Enums/` with
explicit values equal to today's, nested ones taken out of their class under names that stand on
their own: `CabinetResizeAxis`, `RiserMaterialMode`, `CutOutCubeFace`, `ShelfBottomShape`,
`VolumeOverlap`, `MeasureDisplayFlag`, `MeasureMode`, `SlicedMeshSkinScaleMode`,
`SlicedMeshAxisScaling`, `ProductVisualEditorSetupStatus`. Not in this area: `ScreenshotAngle`
(area 6), `EventBridge`'s two (area 5). The pure helpers `CabinetTypeExtensions`,
`KitchenStyleExtensions`, `CabinetLocalDirectionExtensions` to `_projectShared/Common/`.
(`CabinetLocalDirectionUtil` uses the core package, so it stays with the geometry, B4.2.) Check: as
B3.1 (no asset file changes; every `MelcherStableEnum` stores names; plain enum fields keep their
ints).
- **Done 2026-10-23.** 21 enums (11 moved files, 10 new), 3 helpers moved and stripped of comments
  (script in the session scratchpad: a C# lexer that tells code from comments and strings, renames
  in code only). One rename the script missed: `nameof(SkinScaleMode)` inside an interpolated
  string (the lexer treated the whole string as text); fixed by hand, and the lexer now treats
  interpolation holes as code. Compiles in the editor and as macOS player scripts. Checks against
  B3.4: `rulescheck.cs` 60/0; `coldstart.sh` both 13 errors / 4 warnings; `runall.sh` identical
  (photos within 0.03/255; kept as `unity6-after-B4.1`); fallback JSON identical to the probe's
  sign-in; `menucheck.sh` 21 / 21 / 1,268; edit probe identical (`editprobe-after-B4.1`). No asset
  file changed.

**B4.2 — The model's data and value types by what they are.** ScriptableObjects renamed with `SO`
and moved with their `.meta` to `_project/Scripts/Scriptables/`: `BrandCatalogSO`,
`KitchenTemplateSO`, `CabinetSectionDoorStyleSO`, `CabinetSectionDoorHandleStyleSO`,
`CabinetSectionLegStyleSO`, `CabinetSectionDoorEditorMappingSO`. `[Serializable]` types to
`_project/Scripts/Serializables/` (one per file, nested ones taken out). The geometry structs and
helpers to `_project/Scripts/Geometry/` (`KitchenDesign.Geometry`). Check: as B3.2 (each asset of
those types loads as the new class with the same data, through a census snippet; no asset file
changes).
- **Done 2026-10-23.** 6 ScriptableObjects renamed and moved, 11 serializable types (4 taken out
  of `BrandCatalog`, `CabinetBacking` and `SlicedMesh`; `SlicedMesh.Slice` became
  `SlicedMeshSlice`), 7 geometry files; 70 files changed by a script (session scratchpad) that
  renames in code only, interpolation holes included. Compiles in the editor and as macOS player
  scripts. `kitchendatacensus.cs`: identical before and after (61 assets, 215,254 values, hash
  `43965C8199303F1E`), every asset loads as its `…SO` class. Checks against B4.1: `rulescheck.cs`
  60/0; `coldstart.sh` both the reference; `menucheck.sh` 21 / 21 / 1,268; edit probe identical.
  `runall.sh` (kept as `unity6-after-B4.2`): 7 designs identical, but 36449 (the only Toppin
  design) had its live orbit camera at yaw 0° instead of 45° and its product 3" left of the stored
  position (photos up to 5.8/255). A second run (`unity6-after-B4.2b`): `resave.json` identical in
  all 8 and photos within 0.14/255, only 36449's live camera at 0° again. The code diff normalises
  to renames, usings and namespaces only (no extension methods moved, no type name used as text).
  B4.1's code rerun the same day gave 45°, and an instrumented B4.2 run of all 8 designs gave 45°
  too, so the result is intermittent. The instrumentation found the restart-order race described in
  CLAUDE.md (handover, orbit view). The server dropped 40 Kalamazoo products between the B4.1 and
  B4.2 sign-ins, so the fallback JSON differed only in those.

**B4.3 — The kitchen's selections in one object.** `KitchenSelections` (plain class,
`_project/Scripts/KitchenModel/`) holds the 19 selection fields; `Kitchen`'s public properties
forward to it, so no caller changes. Why first: every later part (save, load, finishes, drag)
reads the selections, and one object to hand them is what lets them leave `Kitchen`.
- **Done 2026-10-23.** 20 fields (the 19 plus the analytics key) became public fields of
  `KitchenSelections`, public because `LoadFromJson` passes several as `out` arguments; `Kitchen`'s
  references rewritten to `this._selections.…` by a script, its properties unchanged. Checks against
  B4.2: `rulescheck.cs` 60/0; `coldstart.sh` both the reference; `runall.sh` `resave.json`
  identical in all 8, photos within 0.07/255, only 36449's live orbit camera back at 45° (the
  intermittent case); `menucheck.sh` 21 / 21 / 1,268; edit probe identical. The server dropped 7
  more Kalamazoo products in between.

**B4.4 — Save and price inputs out of `Kitchen`.** `KitchenSaveWriter` (`_project/Scripts/Save/`,
`KitchenDesign.Save`) builds the JSON exactly as `SaveToJson` does; `Kitchen.SaveToJson` calls it.
The price inputs become `_project/Scripts/Pricing/` (`KitchenDesign.Pricing`): `PriceSheetInputs`
(the manual entries, the price change factors), `StollPriceSheet` and `GeminiCatalogPriceSheet` (the
two blocks, picked by the app rule `PriceSheetExtras`, as area 1 planned), `CountertopPricing`
(`TryGetCountertopPricing` and the Challenger depth tables). The JSON key strings stay where the
writer builds them (frozen format). Check: `resave.json` byte-identical (it carries every price
input) in the 8 designs and in both probe phases.
- **Done 2026-10-23.** Generated by a script (session scratchpad) from `Kitchen`'s own text, then
  three fixes the compile found: a local `analyticsKey` that the property remap had also renamed,
  `TryGetCountertopPricing` reading `Kitchen.layout` (now a parameter), and the toe-kick brand check
  the writer uses after the price blocks. A normalised text comparison of both price blocks and the
  helpers against the old code: identical apart from the intended remaps (the baseline can't run
  the Stoll block). The unused `ManualPriceSheetEntry` struct went. `Kitchen.cs` 7,450 → 5,413
  lines. Checks against B4.3: `rulescheck.cs` 60/0; `coldstart.sh` both the reference; `runall.sh`
  `resave.json` identical in all 8 and every measurement within the noise (photos within
  0.05/255); fallback JSON identical; `menucheck.sh` 21 / 21 / 1,268; edit probe identical (both
  `resave.json`, so the Standard path's price inputs too).

**B4.5 — Load out of `Kitchen`.** `KitchenSaveReader` (`KitchenDesign.Save`): `LoadFromJson`,
the occupants, the backing by SKU, the requested-save lookup. Check: the baseline (every design is
a load) and the probe.
- **Done 2026-10-23.** Generated by a script from `Kitchen`'s text. **Trap found on the way:**
  `LoadFromJson` declares locals `baseCabinetDepthInMeters` and `countertopThicknessInMeters`
  (`out float`), which shadow `Kitchen` properties of the same names. A remap of the properties also
  rewrote the locals' uses, which would have compiled and read the property instead; only the two
  declarations failed. Fixed by leaving those names alone in that method, and every moved method
  of B4.4–B4.6 was then checked for locals, parameters and lambda parameters named like a remapped
  member (only B4.4's `analyticsKey`, already restored exactly). A normalised comparison of the
  four methods with the old text shows only the intended remaps. `Kitchen.cs` 5,413 → 4,512 lines.
  Checks against B4.4: `rulescheck.cs` 60/0; `coldstart.sh` both the reference; `runall.sh`
  identical (photos within 0.07/255); fallback JSON identical; `menucheck.sh` 21 / 21 / 1,268; edit
  probe identical.

**B4.6 — Selection, finishes, widgets and dragging out of `Kitchen`.** `KitchenSelectionControl`
(highlight and its materials), `KitchenFinishControl` (finish changes, the ground materials, the
finish material getters), `KitchenWidgetsControl` (resize widgets, size buttons, tickers, the top
info label), `OccupantDragControl` (drag and snapping), in `KitchenModel/`. Check: the probe above
all (highlight, drag and resize are only covered there), then the baseline.
- **Done 2026-10-23.** Selection and finishes became one class, `KitchenMaterialControl`, instead
  of two: the highlight reads the finish materials and a finish change re-applies the highlight, so
  two classes would have called each other. The three shared fields (highlighted object, volume being
  resized, product being dragged) are `KitchenInteractionState`. Generated by a script from
  `Kitchen`'s text; before applying, every moved method was checked for locals, parameters or lambda
  parameters named like a `Kitchen` member (none), and afterwards a token-level diff against the old
  methods listed every substitution (only field, state and `Kitchen`-member remaps, plus
  `Object.Destroy`/`Object.Instantiate`). Two compile fixes: `CreateKitchenWidget` had no access
  modifier, and the reader calls `ChangeFinish` and `RecreateResizeWidgets`, which `Kitchen` now
  forwards (internal). `Kitchen.cs` 4,512 → about 3,030 lines. Checks against B4.5: `rulescheck.cs`
  60/0; `coldstart.sh` both the reference; `runall.sh` identical (photos within 0.14/255); fallback
  JSON identical; `menucheck.sh` 21 / 21 / 1,268; edit probe identical in both phases.

**B4.7 — Building-method rules.** As decision 5, with a table check like `rulescheck.cs`
(`buildingrulescheck.cs`: every old expression against the new rule for the three methods and,
where a cabinet takes part, each cabinet prefab).

**B4.8 — Brand rules and the door-style lookup.** `BrandRules` as decision 6 (the price inputs'
brand checks, `RuntimeProductOverrides`' padding numbers, `Const.Gemini`), checked by a table
snippet over every manufacturer id in `Const.Gemini` and every brand catalog asset. The door
mapping (`CabinetSectionDoorUtility`) becomes `DoorStyleLookup`, an instance owned by
`CatalogDataStatus` and filled when it populates the runtime brand catalogs (today the first
`PopulateRuntimeData` fills the static); old code reaches it through `ProFitGlobals.catalogData`.

**B4.9 — `Kitchen.instance` and `ProFitGlobals` out of the model.** Decision 4, and the model's
`ProFitGlobals` reads (product content, app rules, user profile units) become references the kitchen
hands down where a path exists (`IKitchen`). Whatever is left is listed with its reason.

**B4.10 — File the model.** `git mv` with `.meta` into `_project/Scripts/`: `KitchenModel/`
(`Kitchen`, `IKitchen`, the parts, the cutout clipping), `Cabinets/` (`Cabinet`, `CabinetSpan`,
`CabinetLayout`, `CabinetBacking`, `CabinetShelf`, `CabinetCutOut`, `ICabinet`, the door, handle
and leg components), `Occupancy/` (`Occupant`, `OccupiableVolume`, `VolumeOccupantResult`,
`IOccupantListener` and the handler interfaces, the cuboid visualisations), `Measures/`
(`Measure`, `MeasurementOverlayManager`), `Widgets/` (`KitchenWidget`, `ResizerWidget`,
`ButtonWidget`), `Products/` (`ProductVisualContent` and its editor partial). Namespaces follow the
folders; usings by script; the `Tools/Baseline` and `Tools/Cutouts` snippets follow the new names.
Comments stay in files that only moved (decision 7). Check: as B3.2 plus the probe; scenes and
prefabs unchanged (scripts bind by GUID).

## Checks for every step
As area 3, plus the probe:
- `rulescheck.cs` (60/0), `coldstart.sh` from `PlatformSetupScene` and from `BootScene` (13 errors,
  4 warnings), `runall.sh` then `compare_runs.py` and `photodiff.sh` against the previous step's
  run, the fallback JSON a sign-in writes byte-identical to the previous run's, `menucheck.sh`
  (21 / 21 / 1,268; 22 when the CLI logs its own timeout), and `editprobe.sh` identical to the
  previous step's probe.
- Around every play-mode run, the user's `AppProfile.asset` and every
  `Resources/*/FallbackJSON` folder are backed up (hashes kept) and restored afterwards from the
  backup, never from git; the backup is kept until the step is committed. Those files are never
  staged.

## Where the rear wall will land (for porting `77f53840ee` by hand)
Filled in as the steps land.
