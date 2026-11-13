# 08 — Emptying `ProFitGlobals`, and the cleanup after the refactor

Planned 2026-11-13, after areas 1–7 of `03-refactor.md` and their checks. Order agreed with the
user the same day: (a) empty `ProFitGlobals`, (b) split the measure placement out of
`KitchenScreenshotControl`, (c) move `AssetsMelcher/Editor` with a planned model reimport,
(d) house conventions inside the files that were only moved.

## Settled first (2026-11-13)
- **Save photos.** `photodiff.sh final-base/unity6 final-tip/unity6-run2`: every photo of the 8
  designs within 0.08/255 of `3d25da55` (covers 0.00–0.08, the rest 0.00–0.02). The three covers
  that differed on the tip's first run were the "freshly opened editor" gotcha. No bisect needed.
- **The 14th cold-start error** is the Unity CLI's own
  `Failed to handle /api/exec request: Thread was being aborted.`: a CLI request cut off by the
  domain reload when play mode starts. 10 cold starts from `BootScene` on HEAD gave 13 or 14
  (14 in about 2 of 5); the CLI's captured log list has the same 13 errors in both, and
  `Logs/Editor.log` shows the extra line right after "Begin MonoManager ReloadAssembly" only in a
  14 run. Not an app error; it can hit any check that enters play mode.

## Today (read 2026-11-13)
`ProFitGlobals` (`Game/`) is forwards only. About 150 reads in 50 files:
1. **`Kitchen`** (22): its `Awake` builds the parts (widgets, camera, materials, drag, save
   writer and reader) with systems from the static, because `Awake` runs before
   `DesignerSceneInitializer.Initialize`. 16 more reads in its methods.
2. **Model objects** made by the kitchen (about 25): `Cabinet`, `CabinetLayout`, `CabinetSpan`,
   `CabinetShelf`, `CabinetSectionDoorHandle`, `Occupant`, `ProductVisualContent`, `Measure`,
   `MeasurementOverlayManager`, `ResizerWidget`, `KitchenWidget`, `CabinetTickerWidget`.
3. **Designer screens** that read in `Awake`/`OnEnable` or are made at runtime (about 20):
   `BrandCatalogUI`, `WhiteLabelClientInfoUI`, `DesignerDevUI`, `CabinetShapeUI`,
   `BrandCatalogToggle`, `ToolbarProductButton`, `CabinetOtherSizeControl`, the highlighted
   product content, `DesignerScreensControl.Awake` (skybox).
4. **Skin and options reads in views** (about 20): `ButtonView`, `ToggleView`, `InputFieldView`,
   `BackgroundView`, `FooterToggleView`, `InlineMessageBox`, `VersionLabel`, `TrialCountdownUI`,
   `MainUI`, `HomeUI`, `WelcomeUI`, `PayNowUI`, `ManufacturerWhitelistRow`, `QuickLogInWidget`.
   On many prefabs, running in `Awake`/`OnEnable`, before any initializer.
5. **Data types**: `EndPanelInfo.dealerPrice` and `FridgePanelInfo.dealerPrice` (the user's
   currency), `BrandCatalogSO.info` (the server's catalog list).
6. **Edit-mode and editor code** (about 35, `appProfile` almost only): `ProFitBuildProcessor`,
   `AddressableGroupMenuCommand`, `ProductVisualThumbnailCreator`, `ProductIdRemapDatabaseSO`'s
   editor functions, the two debug menus.
7. `AppLinks` (static; server host, subscription URLs).

## Decisions
1. **The kitchen is initialized, not self-started.** `Kitchen.Initialize(AppSystems)` does what
   `Awake` did and is the first call in `DesignerSceneInitializer.Initialize`, before any
   `ListenTo`. `Kitchen.Awake` goes. Between the scene's `Awake`/`OnEnable` pass and the
   initializer (same frame, before any `Start` or render) the kitchen has its selections and
   events but no parts; nothing may call into the parts from `Awake`/`OnEnable`.
   `Kitchen.OnEnable` hooks the shelf resize widgets only once they exist.
2. **Model objects ask their kitchen.** `IKitchen` gains the systems the model reads
   (`appProfile`, `appRules`, `catalogData`, `productContent`, `session`); every model object
   already has, or is given, its kitchen. Widgets and tickers get what they read from
   `KitchenWidgetsControl`, which makes them.
3. **Screens:** a read in `Awake`/`OnEnable` moves into the screen's `Initialize` when nothing
   needs it earlier; rows made at runtime get their values from the screen that makes them.
4. **Data types don't read systems.** `dealerPrice` becomes `GetDealerPrice(Currency)`;
   `BrandCatalogSO.info` becomes a lookup the caller makes on `CatalogDataStatus`.
5. **One static store stays: the app profile.** The skin, the options and the profile are one
   immutable asset, read by views on dozens of prefabs before any initializer, and by edit-mode
   tools where nothing is constructed. That is app-wide config, which the project overrides
   allow as a static store. `ProFitGlobals` is replaced by `AppProfileStore`
   (`_project/Scripts/Game/`): `Profile`, `Options`, `Skin`; bound by `AppInitializer`, loaded
   from `Resources/AppProfile` on first use otherwise, reset at `SubsystemRegistration`. No
   system (session, server, catalog data, client list, rules, content, launch options) is
   reachable through a static afterwards. The views lose it when they are rewritten in UI
   Toolkit.
6. **Editor code** reads `AppProfileStore`. The debug menus (play mode only) take the systems
   from the scene's `SceneInitializer.Systems`, found in the open scene: editor-only debugging
   code, not a system lookup in the app. `ProductIdRemapDatabaseSO`'s editor functions build the
   rules from the profile (`AppRulesFactory`) and read the fallback JSON themselves.
7. `AppLinks` becomes methods that take the host / the options and the user type.
8. **Checks around each step that changes start-up order** (B8.1 for certain): the full run
   (`GeminiBaseline/area5-map/fullrun.sh <tag>`) compared with `final-tip` (`3a8fa33b`):
   `resave.json`, measurements, photos (`photodiff.sh`), edit probe, menu check, listener order,
   cold starts (13 errors, or 14 with the CLI's own), rule checks. Steps that only reroute a
   read compile (editor and WebGL player scripts) and are checked together at the end of (a).

## Steps
- **B8.1 — `Kitchen.Initialize`** (decision 1) and `Kitchen`'s own reads. Full check.
- **B8.2 — model objects** (decision 2) and the data types (decision 4).
- **B8.3 — designer screens** (decision 3).
- **B8.4 — `AppProfileStore`** (decisions 5–7); `ProFitGlobals` deleted. Full check.
- **B8.5 — measure placement** out of `KitchenScreenshotControl` into its own class in
  `Photos/` (`PlanMeasurePlacement`): the control keeps the cameras and renders. Photos and
  measurements checked.
- **B8.6 — `AssetsMelcher/Editor` → `_project/Editor/Tools/`**, the FBX postprocessor with it.
  Expected to reimport every model: measure the import time, then compare the model `.meta`
  files (the material remap must come out the same), the visuals census, and the photos.
  Ask the user before starting: it is the one step that touches every model.
- **B8.7 — house conventions in the files that were only moved** (braces, explicit `private`,
  field naming where no asset binds the name, comments into CLAUDE.md, logs editor-wrapped), one
  folder per commit. Serialized field names stay unless `FormerlySerializedAs` is added.

## Results
- **B8.1 done 2026-11-13.** `Kitchen.Awake` is `Kitchen.Initialize(AppSystems)`, the first call
  in `DesignerSceneInitializer.Initialize`; `Kitchen` keeps the systems it reads as fields and no
  longer reads `ProFitGlobals`. `OnEnable`/`OnDisable` hook the shelf resize widgets only when
  the parts exist; `Initialize` hooks them once it has made them. Full run `final-b81` against
  `final-tip`: `resave.json` identical in all 8 designs, measurements differ only in camera noise
  (≤0.05°), photos ≤0.08/255, edit probe identical (cameras aside), menu check, listener order
  and both cold starts the same (one cold start with the CLI's own 14th error).
- **B8.2 done 2026-11-13.** `IKitchen` has `appRules`, `catalogData`, `productContent` and
  `session`; `Cabinet`, `CabinetLayout`, `CabinetSpan` and `Occupant` read them there.
  `InstantiateHandle` takes the door-style lookup. `KitchenWidgetsControl` gives each
  `ResizerWidget` the session and each `CabinetTickerWidget` the session and the rules.
  `MeasurementOverlayManager.Initialize(session)` is called from `DesignerSceneInitializer`.
  `dealerPrice` is `GetDealerPrice(currency)`; `BrandCatalogSO.info` is gone (the toolbar looks
  the record up). Profile, options and skin reads in the model stay on `ProFitGlobals` until
  B8.4 moves them to the store (a cabinet made in edit mode has no kitchen to ask).
  Full run `final-b82` against `final-tip`: `resave.json` identical, measurements camera noise
  only, photos ≤0.08/255, edit probe identical, menu listeners the same, **but the designer's
  listener order showed `MeasurementOverlayManager` gone from every event**: its `Awake` calls
  its restart handler, which read the session before `Initialize` had set it, and the exception
  in `Awake` disabled the component. Fixed (the handler applies the units only once it has the
  session, and `Initialize` applies them); listener order re-run (`b82b`): the same as
  `final-tip`, no `NullReferenceException`. The save photos and the edit probe did not show this;
  the listener-order check did. Compiles as WebGL player scripts.
- **B8.3 done 2026-11-13.** `BrandCatalogUI`, `DesignerDevUI` and `WhiteLabelClientInfoUI` do in
  `Initialize` what they did in `Awake` on the static; `DesignerScreensControl.Initialize`
  initializes the brand catalog screen first, then the dev screen, then the toolbar, so the
  `ExecuteWhenLoaded` callbacks keep their order (catalog population, dev product list, toolbar
  set-up, hamburger menu set-up, `restart`). `ToolbarProductButton.SetProduct(product, variants)`
  replaces the `productInfo` setter; `CabinetOtherSizeControl` and `TrialCountdownUI` get the
  session; the highlighted-product content gets the launch options; `VersionLabel` reads the
  server type from the profile. Found on the way: `DesignerDevUI.OnGlobalDataReady` already read
  its handed-in catalog data while still being queued from `Awake`, which ran it before
  `Initialize` whenever the catalog was loaded (every start from the menu); queued from
  `Initialize` now. Compiles in the editor and as WebGL player scripts.
  Full run `final-b83` against `final-tip`: `resave.json` identical in all 8 designs,
  measurements camera noise only (≤0.02°), edit probe identical (cameras aside), menu and
  designer listener order the same, cold starts 13 and 14 (the CLI's own), no
  `NullReferenceException` in `Logs/Editor.log` after the change, the menu check's console ends
  with 1245 errors instead of 1247. Photos, both captures (`unity6` and `unity6-run2`), within
  0.14/255 of `final-tip/unity6-run2`. **Compare photos with `final-tip/unity6-run2`, not
  `final-tip/unity6`**: the latter is the capture with the three untextured covers (28316,
  27763, 27764), and comparing against it looks like a regression in exactly those three.
- **B8.4 done 2026-11-13.** `ProFitGlobals` is deleted. `AppProfileStore` (`Profile`, `Options`,
  `Skin`) is bound by `AppInitializer` and loads `Resources/AppProfile` on first use otherwise.
  `AppLinks` takes the server host, or the options and the user type; `PayNowUI`,
  `UnauthorizedDomainUI`, `TrialCountdownUI`, the highlighted-product content and the
  about-product prompt are handed what they pass on. Changed from decision 6: the editor-only
  way to the running systems is `AppInitializer.SystemsForEditor` (one object in every scene),
  not a property on each `SceneInitializer`; the debug menus use it through
  `DebugSystems.TryGet`, the check snippets under `Tools/` directly, so those snippets don't run
  on older commits. `ProductIdRemapDatabaseSO.CaptureCurrentIds` reads the product groups from
  the fallback JSON and passes the parsed products to the cabinet parser (button not run).
  Full run `final-b84` against `final-tip`: `resave.json` identical in all 8 designs,
  measurements camera noise only, photos within 0.05/255 of `final-tip/unity6-run2`, edit probe
  identical (cameras aside), menu check the same as `final-b83`, listener order the same in the
  menu and the designer, both cold starts 13 errors. Compiles as WebGL player scripts.
  **Part (a) is done.**
- **B8.5 done 2026-11-13.** `PlanMeasurePlacement` (`Photos/`, plain class) holds
  `PopulateMeasuresForScreenshots`, `AddMeasureBetweenCabinetPoints`, `AxisToColor` and the
  static `SetupOccupantCutOutMeasurements`, moved unchanged apart from field names and access.
  `KitchenScreenshotControl` (2,137 → 874 lines) still makes the pool of measures in `Awake` and
  builds the placement in `Initialize` with the pool, the three axis colours and the session.
  Two captures (`final-b85/unity6-1`, `unity6-2`) against `final-tip`: `resave.json` identical
  in all 8 designs, measurements camera noise only, every photo (plans included) within
  0.14/255 of `final-tip/unity6-run2`. Compiles as WebGL player scripts.
- **B8.6 done 2026-11-13** (the user said to go ahead). `AssetsMelcher/Editor` is
  `_project/Editor/Tools/`, moved whole with its `.meta` files; scripts in
  `namespace KitchenDesign.ToolsEditor`; `ProductModelFbxImporter`, `AppBuildProcessor`,
  `KitchenFinishTools` lost the `ProFit` prefix; three path strings updated. Measured: the
  folder move alone reimported nothing; the rename and namespace change reimported every model
  once (2,096 imports, 1,159 actual, 20 s). No `.fbx.meta` changed, nothing in
  `AddressableAssetsData` changed. Visuals census: 901 entries, hash `D72B3E9CB18746DF` (the
  reference). Two captures (`final-b86/unity6-1`, `unity6-2`): `resave.json` identical,
  measurements camera noise only, photos within 0.08/255 of `final-tip/unity6-run2`.
- **B8.7, mechanical part, 2026-11-13.** Braces (IDE0011) and accessibility modifiers (IDE0040)
  with `dotnet format style <project>.csproj --no-restore --diagnostics IDE0011 IDE0040
  --severity warn --include Assets/_project/ …` and a temporary `.editorconfig` (Unity writes the
  `.csproj` files on `CodeEditor.CurrentEditor.SyncAll()`; they are gitignored): 9 files. Six
  runtime log calls editor-wrapped. 220 non-serialized fields renamed (guards in CLAUDE.md's
  handover). Not done: serialized names, 85 fields the guards skipped, and the comments, which
  wait for the user's answer on where the whys go.
  Full run `final-b87` against `final-tip`: `resave.json` identical, measurements camera noise
  only, photos within 0.08/255 of `final-tip/unity6-run2`, menu check and both cold starts (13)
  the same as `final-b84`, listener order the same, no null-reference exception in the log.
  The edit probe's snapshots were identical, but in the first run its drag steps logged screen
  positions 30 to 100 pixels off and `MainCamera` ended with FOV 14.39 instead of 21.37; a
  second run of the probe gave the usual values (449.71, 339.17; FOV 21.37). Not reproduced,
  cause not found; the camera code's change is renames only.
- **Notes split, 2026-11-13 (user's decision; the plan's "comments into CLAUDE.md" is dropped).**
  `CLAUDE.md` 2,222 → about 1,230 lines: "Key systems" moved as it was into nine files under
  `Docs/systems/`, the two upgrade sections into `Docs/upgrade-to-unity-6.md`. The old code's
  comments stay in the code. **Plan 08 is finished.**
