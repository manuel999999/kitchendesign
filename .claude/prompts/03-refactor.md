# 03 — Refactor the old code into the house layout and namespaces

Agreed 2026-10-22 (decisions below). Step 6 of `01-copy-and-upgrade.md`. The user asked for the
`Melcher` namespace to be replaced as part of it. Read this whole file before changing any code.

## Why
- The old code is a few huge files that all depend on each other (numbers below). Each later step
  of the rebuild (box cabinets, the door-style shader, the new database, WebGL memory) needs to
  change one area without touching everything else.
- The house rules (sections 2–4) apply to refactored code: one type per file, filed by what it is,
  constructed by an initializer, no singletons, events through `Event<Domain>`, app differences
  behind one app-rules layer.
- The baseline (`Tools/Baseline/unity6/runall.sh`) shows the app saves and measures the same as
  the old one, so each step can be checked to change nothing.

## What the code looks like (measured 2026-10-22)
- **240 app files, about 74,000 lines, all in `namespace Melcher`, all in `Assembly-CSharp`** (no
  asmdefs): `AssetsMelcher/Scripts` (191, of which 85 in `UI/`), `AssetsMelcher/Editor` (21),
  `Cloder` (8, of which 7 editor), `AssetsMelcher/Miscellaneous` (3), `AssetsMelcher/Plugins/WebGL`
  (1), `Procedural Assets/Layers.cs` (1).
- **The embedded package `ca.melcher.unity.core` also uses `namespace Melcher`** (186 files, its own
  asmdefs, plus UniTask, MEC, MiniJSON). 168 of the 240 app files use its types: `MelcherLog`,
  `MelcherButton`, `MelcherMonoBehaviour` (51 files derive from it), `MelcherUI`, and others.
- **One knot of 53 files, about 37,000 lines, all depend on each other** (a type-reference graph
  of the 240 files; the script is in "How the map was made"). In it: `Kitchen` (7,564 lines),
  `ProFitWeb` (4,299), `Cabinet` (3,040), `OccupiableVolume` (2,899), `CabinetLayout` (1,991),
  `ProFitUtility` (1,429), `ProFitGlobals` (1,352), `CabinetSpan` (1,218), `Occupant` (1,029),
  `ProductVisualContent` (+ its `.Editor` partial), `BrandCatalog`, `EventBridge`, the
  `ProductVisual*` types, `AppOptions`, `AppProfile`. Taking out `ProFitGlobals`, `EventBridge`
  and `ProFitUtility` still leaves 39 files tied together; the kitchen model itself (`Kitchen`,
  `Cabinet`, `CabinetSpan`, `Occupant`, `OccupiableVolume`, `CabinetShelf`, `ProductVisualContent`)
  points both ways by design.
- Outside the knot the code layers cleanly: 68 files depend on nothing else in the app (enums,
  value types, small helpers), 41 more only on those, and the rest (mostly UI, the two scene
  scripts, the photo manager, editor tools) sit on top. Four editor files form their own small
  knot (`ProductVisualDatabaseManager` and the three `Cloder` database managers).
- **The hubs:** `ProFitGlobals` (42 static members used 473 times, in 63 files), `EventBridge`
  (97 static delegates, 109 distinct ones used, in 58 files), `ProFitUtility` (44 files),
  `Kitchen.instance` (31 uses). 410 static fields and properties in total. 7 `Find…OfType` calls.
- **App checks:** `isAppOptionGemini/ProFit/Stoll` and `userFilter ==` 28 times in 10 files,
  `appOptions.<field>` 82 times in 35 files (25 distinct fields), `Const.Stoll`/`Const.Gemini`
  22 times.
- **Nothing serialized names an app type or assembly.** No `[SerializeReference]`; no scene,
  prefab or asset contains `Melcher.` or `Assembly-CSharp`; no `MelcherSignal` asset stores an
  argument type name; no reflection looks for `Assembly-CSharp`. Scripts bind by the GUID in their
  `.meta`, so renaming namespaces and moving files between assemblies keeps every reference. The
  one string that names the namespace is an `EditorPrefs` key in
  `ProductVisualDatabaseSetsManager` (`"Melcher.ProductVisualDatabaseSetsManager.SetAsideGuids"`):
  leave the string as it is, or the set-aside list in the editor is lost.
- **Third-party code without an asmdef that app code uses:** DOTween's `Modules/` (the UI
  shortcuts like `DOFade`, `DOColor`) and `UnityRecyclingListView` (2 files). An asmdef can't
  reference `Assembly-CSharp`, so both need an asmdef before app code can move. Other plain
  third-party folders (ProceduralUIImage, UIGradient, WebGLInput, ConsolePro…) are only used from
  prefabs, which don't care which assembly a script is in.
- **No runtime file uses an `Editor/` folder type.** Editor-only code in runtime files is behind
  `#if UNITY_EDITOR` (`ProFitUtility.Editor`, the `AppOptions.Editor` and
  `ProductVisualContent.Editor` partials).
- **The team is still changing the old code.** Since the copy's commit `7700e16a8c`, branches in
  the old repo touch `.cs` files (for example `origin/pro-fit-45-degree-corners` and
  `origin/fix/add-occupant-anywhere-function`, 20 files each). After an area here is renamed and
  split, those changes no longer apply as patches; they have to be ported by hand.

## The approach
Two phases. **Phase A moves the old code into our assemblies and namespace without changing what
it does.** It is mechanical and quick, and afterwards the "old code can call new code but not the
reverse" limit is gone, so phase B can take areas in any order. **Phase B refactors one area at a
time** inside the `KitchenDesign` assembly. Cycles inside one assembly compile, so the 53-file knot
is untied gradually instead of all at once.

Each step is checked on its own and committed by the user before the next, so a break has one
cause (the old plan's rule).

### Phase A — move in (no behaviour change)
- **A1. Asmdefs for the two third-party folders the app uses.** `DOTween.Modules` (in
  `AssetsThirdParty/DOTween/Modules/`, referencing DOTween, UI, TMP as its `#if`s need) and
  `RecyclingListView`. Check: compiles, no console errors.
- **A2. Rename the namespace `Melcher` → `KitchenDesign` in the 240 app files.** A script over the
  files: the `namespace` line, `Melcher.` qualifiers that name app types, and `using Melcher;`
  added to every file (the package's types stay in `Melcher`). The `EditorPrefs` key string stays.
  Nothing moves and no assembly changes. Check: compiles; `runall.sh` on the 8 designs
  (`resave.json` identical, only the known camera differences); the menu smoke check (below).
- **A3. Put the old code into our assemblies without moving files.** An assembly reference file
  (`.asmref`, pointing at `KitchenDesign`) in `AssetsMelcher/Scripts`,
  `AssetsMelcher/Miscellaneous`, `AssetsMelcher/Plugins/WebGL`, `Procedural Assets` and the
  `Cloder` root. One editor-only asmdef for `AssetsMelcher/Editor` and `Cloder/Editor` (the
  four-file editor knot spans both), `KitchenDesign.Tools.Editor`, namespace stays `KitchenDesign`
  for now (R-ASM-3 applies when it is split). `KitchenDesign.asmdef` gains the references the old
  code needs (the core package, UniTask, MEC, MiniJSON, `DOTween.Modules`, `RecyclingListView`,
  TMP, uGUI, Addressables, URP, Input System as needed), found by compiling. Check as in A2, plus
  opening each editor window the tools add (product visual database, sets, states, addressable
  group commands).
- After A3 the project's own code is all in `KitchenDesign*` assemblies; `Assembly-CSharp` holds
  only third-party code.

**Phase A done 2026-10-22** (staged/unstaged for the user to commit; see "Commits" below).
- **A1:** `RecyclingListView.asmdef` (refs `UnityEngine.UI`). **DOTween's asmdef is DOTween's
  own**: a hand-written `DOTween.Modules.asmdef` was deleted by DOTween the next time the editor
  opened (`ASMDEFManager.ApplyASMDEFSettings`, run by its asset postprocessor, removes it while
  `DOTweenSettings.createASMDEF` is 0). Now `createASMDEF: 1` is set in `DOTweenSettings.asset`
  (through `SerializedObject`) and DOTween wrote the file (`{"name": "DOTween.Modules"}`, no
  references; the UI module still compiles).
- **A2:** 239 files (the 240th, Weaver's generated `Layers.cs`, is `namespace Weaver`). Each got
  `namespace KitchenDesign` and `using Melcher;` as the first line inside the namespace (all files
  keep their usings inside it), plus the closing `// namespace` comment; byte-order marks and line
  endings kept; every file changed by 2–3 lines. `using Melcher;` goes in every file, not only the
  168 that name a package type, because package extension methods are used without naming their
  class. The Baseline (`unity6/`) and Cutouts snippets had their `Melcher.<app type>` names moved
  to `KitchenDesign.`; `Tools/Baseline/old2022/` stays `Melcher` (the old project). The script was
  a one-off in the session scratchpad.
- **A3:** `.asmref` → `KitchenDesign` in `AssetsMelcher/Scripts`, `/Miscellaneous`,
  `/Plugins/WebGL`, `Procedural Assets`, `Cloder`; `KitchenDesign.Tools.Editor.asmdef` in
  `AssetsMelcher/Editor` and an `.asmref` to it in `Cloder/Editor`. References were trimmed to what
  the compiled assemblies use (checked with `GetReferencedAssemblies`), plus `NativeGallery.Runtime`
  (the `com.yasirkula.nativegallery` package, used by the hamburger menu's save-to-gallery) and, in
  the editor assembly, `Unity.Timeline.Editor` for an unused `using UnityEditor.Timeline.Actions;`
  in `ProductVisualDatabaseManager` (kept rather than editing code in phase A). No app code has
  platform-only (`#if UNITY_WEBGL/IOS/ANDROID`) package use, so the editor compile covers the
  references. `Assembly-CSharp` is down to 140 third-party types, none of ours.
  **`ProductVisualDatabaseEntry.uxml` named a type as text** (`<uie:EnumField
  type="Melcher.ProductAnchor, Assembly-CSharp">`), which the earlier search of scenes, prefabs and
  `.asset` files missed; the database window threw `TypeLoadException` until it was changed to
  `KitchenDesign.ProductAnchor, KitchenDesign`. A search of every text asset type (uxml, uss, json,
  xml, txt, mat, shadergraph, asmdef, …) found no other.
- **Checks, A2 and A3 each:** compiles with no errors (editor, and for A3 also
  `PlayerBuildInterface.CompilePlayerScripts` for macOS); `runall.sh`: `resave.json` and every
  non-camera measurement identical to the last run before the rename
  (`GeminiBaseline/unity6-after-stencil-removal-2026-10-21/`), save photos within 0.09/255
  (A2's run kept in `GeminiBaseline/unity6-after-rename-2026-10-22/`); `menucheck.sh` passed with
  the same console error counts (21 / 21 / 1,268). A3 also: each editor window of the tools
  (product visual database, sets, states, fallback JSON entries, finish materials, bulk remap)
  opens and closes with no new console error and no file change.
  The first A2 run had three bad cover photos (cabinets drawn with a white noisy texture, plain
  black countertops, 2.4–19.5/255); re-captured on the warm editor they matched (≤0.14). A
  fresh-editor first-load artefact, not the rename (see CLAUDE.md, Gotchas).

**Commits** (the user commits): four groups with disjoint paths, in order. (1) forced product
occupancy (`Cabinet.cs`, `ProductVisualInfo.cs`, staged as hunks before the rename touched the same
files); (2) A1, `Assets/AssetsThirdParty/DOTween` and `…/UnityRecyclingListView`; (3) A2, the
renamed `.cs` files under `Assets/AssetsMelcher` and `Assets/Cloder` and the `Tools/` snippets;
(4) A3, the `.asmref`/`.asmdef` files, `KitchenDesign.asmdef`, `ProductVisualDatabaseEntry.uxml`,
the menu check and the notes.

### Phase B — areas
Proposed order. The hubs come first because every other area uses them.

1. **App foundation.** A boot scene with `AppInitializer` (first in the build list, then today's
   splash and platform-setup flow), which constructs the app-wide systems once and passes them on.
   `ProFitGlobals` is split into those systems (the signed-in user and session, the downloaded
   catalog data, the app profile) and deleted. The app-rules interface in
   `KitchenDesign.Shared.Interfaces` with one implementation per app; the 28 app checks, the 82
   `appOptions` uses where they are rules, and `Const.Stoll`/`Const.Gemini` move behind it. `Const`
   and `ProFitUtility` are split by owner (helpers into `Utility/`, the rest to the type that uses
   them). `MenuSceneInitializer` and `DesignerSceneInitializer` start as thin shells that today's
   `MenuScene`/`DesignerScene` hand off to, and grow as areas move into them. `EventBridge` stays
   until the areas that use it are done. **Detailed in "Area 1 — App foundation" below.**
2. **Server and offline data.** `ProFitWeb` (4,299 lines) becomes a server client constructed by
   the `AppInitializer`; `ProFitWebTypes` become `Serializables/` with their JSON names frozen
   (R-DATA-3); the offline fallback JSON loading; `GroupDatabase`, `ProductIdRemapDatabase`,
   `BrandCatalog` data.
3. **Products and visuals.** `ProductInfo`, `ProductVisual*`, `ProductVisualDatabase`,
   `ProductVisualContentLoader`, `AssetAddress` and the Addressables helpers, and their editor
   tools (split into `KitchenDesign.ProductsEditor` etc. per R-ASM-3).
4. **The kitchen model.** `Kitchen` (7,564 lines), `CabinetLayout`, `CabinetSpan`, `Cabinet`,
   `CabinetBacking`, `CabinetShelf`, `Occupant`, `OccupiableVolume`, widgets, measures, cutouts.
   The largest area; it gets its own plan (`04-…`) after area 3, splitting `Kitchen` into the
   systems it really is (layout building, selection, save/load, pricing inputs, highlight…) and
   replacing `Kitchen.instance`. Inside it: spans and layouts, then cabinets, then occupancy and
   products, as the old plan listed. **Planned and done in `04-kitchen-model.md` (B4.0–B4.10,
   2026-10-23).**
5. **UI.** The menu screens and the designer screens (85 files, `DesignerToolbarUI` 2,521 lines).
   Still uGUI (project override), but constructed by their scene initializer and talking through
   events. Most of `EventBridge.Menu` goes here.
6. **Photos.** `KitchenScreenshotManager` (2,707 lines), `ProductVisualThumbnailCreator`, the
   cameras. The orbit-camera difference (open in the handover) can be fixed here if it hasn't been
   by then.
7. **Leftovers.** `EventBridge` deleted once empty; the `.asmref` files deleted once their folders
   are empty; `AssetsMelcher/` then holds only assets.

### The recipe for each area
1. Write the area's section in this file (or its own plan if large): files, the new types and
   names, which statics and `EventBridge` delegates go, which app checks move. Agree it with the
   user before editing.
2. Bring over the team's later changes to these files from the old repo first, while they still
   apply as patches (which branches count is an open question below).
3. Split to one type per file and file them by what they are (section 2): `git mv` the `.cs` and
   `.cs.meta` together (R-CODE-12), namespace following the folder (`KitchenDesign`,
   `KitchenDesign.Serializables`, `KitchenDesign.<System>`, …). Enums go to
   `_projectShared/Enums/` with explicit values **equal to their current implicit ones** (they are
   saved as numbers).
4. Replace the area's statics, `Kitchen.instance` uses and `Find…` calls with references passed
   through `Initialize(...)` from the initializer, with the order's why written in section 7. Its
   `EventBridge` groups become `Event<Domain>` holders in `KitchenDesign.Events`; payloads that
   events carry become `<Thing>Info` types in `KitchenDesign.Shared` (R-ASM-2).
5. House conventions on the code touched (section 3); names per the project overrides
   (`<Name>Status`/`<Name>Control`, kitchen terms kept, `ProFit…` prefixes dropped).
6. Check: clean compile and console; `runall.sh` on the 8 designs; the menu smoke check; the
   conformance list (section 6). Update CLAUDE.md ("Key systems", "The Initialize() chain",
   "Events", "Scene and prefab map"). Stage; the user commits.

### Checks
- **Designer:** `Tools/Baseline/unity6/runall.sh`, the 8 designs: `resave.json` identical, every
  measurement the same apart from the known orbit-camera differences, photos within the noise of
  the last run. It swaps in the committed Gemini `AppProfile.asset` for the run and restores the
  user's files after.
- **Menu smoke check (new, built in A2):** the baseline doesn't touch the menu screens. A script in
  `Tools/Baseline/` that drives the menu through the CLI: sign in (Gemini quick sign-in), client
  list, a client's quotes, open a design, back to the menu, user profile, sign out, counting
  console errors. Read-only: **it never creates, edits or deletes clients or designs, and never
  saves**. The screens that write (create/delete client, sign up, edit profile, pay) are only
  opened and closed; the user checks those by hand when an area changes them.
- **Not covered:** end panels (no usable baseline design, see the handover), WebGL and phones
  (modules not installed), the three app profiles other than Gemini.

## Area 1 — App foundation (agreed 2026-10-22)
The user agreed the approach, including keeping `ProFitGlobals` as forwards, and left the names
and the refactoring details to Claude.

### How it works today (read 2026-10-22)
- **Start.** `Main.RuntimeInit` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`) sets the
  culture to en-US, the log level and `ProFitGlobals.isVerbose` (editor, debug build, `?verbose`,
  `--verbose`), quality level 0, turns off the render debug UI, initialises DOTween, and calls
  `ProFitGlobals.Init()` (in the editor only when the open scene is in the build list). In the
  editor it also polls F1 to kick the user.
- **`ProFitGlobals.Init()`** is a MEC coroutine: reset every field, read the fallback JSON from
  `Resources/<App>/FallbackJSON` (manufacturers, product groups, cabinet section categories, brand
  catalogs, tags, compatibilities, products, cabinets; the catalog ones only when
  `cabinetDataAvailability` allows), apply the product overrides, build the variant lists, probe
  the server (switching http/https once if it fails), download brand catalogs (then
  `PopulateRuntimeBrandCatalogs`), manufacturers, product groups, tags and compatibilities, add the
  editor-only fake products for unused visuals, then run the `ExecuteWhenReady` queue.
- **Scenes** (build list): `SplashScene` → `PlatformSetupScene` (waits for the globals; on WebGL
  and in the editor checks white label mode; loads `MenuScene` or, for white label,
  `DesignerScene`) → `MenuScene` ↔ `DesignerScene` (back through
  `IntermediateDesignerToMenuScene`). Splash loads `buildIndex + 1`; the others load by name.
- Each scene script is a `MelcherStateMachineState`. The core package's `MelcherGlobalStateMachine`
  calls `OnStateAdded` from `MelcherSceneManager.sceneLoaded`: after `Awake`, before `Start`. It is
  created at `BeforeSceneLoad`, so it is subscribed before anything a boot scene could add, and
  `OnStateAdded` always runs before an app-level `sceneLoaded` handler.
- `runall.sh` and `menucheck.sh` press play from `MenuScene`, so Splash and PlatformSetup don't
  run in the checks.
- **Each app has exactly one building method** (`m_AllowBuildingMethods: 0` in all three
  `AppOptions`): ProFit Standard, Stoll Sections, Gemini Catalog. Many "Stoll" behaviours are
  gated on Sections, not on the app (the bowing limit, the 0.5" overhang, `Const.Stoll`'s bowing
  and depth values), and the `Const.Gemini.ManufactersIds` checks are per cabinet brand. Neither
  is an app check, so neither moves in area 1: building-method rules go with the kitchen model
  (area 4), brand rules onto `BrandCatalog` / `BrandCatalogRuleset` (area 4).

### The main decision: `ProFitGlobals` keeps only forwards, and is deleted last
Its state and logic all move out in area 1, into systems the `AppInitializer` constructs. What
remains of `ProFitGlobals` is a list of static properties that forward to those systems: no state
of its own except the references the `AppInitializer` hands it, and no logic. Each later area
replaces its files' uses with references passed through `Initialize(...)` and deletes the forwards
nobody uses any more. The class goes when the list is empty (area 7 at the latest).

Why: most of the 63 files that use it are the kitchen model (area 4) and the screens (area 5).
Nothing constructs them through an initializer yet, so they have nowhere to receive a reference
from. Removing every use now would mean either a new static under another name (the same problem)
or touching each of those files twice. The forward list keeps the remaining dependency visible and
countable. The old code reaches the app rules the same way (`ProFitGlobals.appRules`) until its
area passes them in.

The forwards are the one app-wide static store this area adds (project override for R-DATA-8).
They reset at `SubsystemRegistration`. `appProfile` and `appOptions` still work in edit mode,
because editor tools and `OnValidate` read them (`ProFitBuildProcessor`,
`ProFitAddressableGroupMenuCommand`, `ProductIdRemapDatabase`, `ProductVisualContent`,
`ProductVisualThumbnailCreator`). With no `AppInitializer` running, they load the profile from
`Resources` once and load it again if it was destroyed, instead of once per access.

### Steps
Each step is checked and committed on its own, as in phase A.

**B1.1 — App rules.**
- `KitchenDesign.Shared.Interfaces.IAppRules` (`_projectShared/Interfaces/IAppRules.cs`). Members
  use only primitives and Shared types, since Shared references nothing.
- `ProFitAppRules`, `StollAppRules`, `GeminiAppRules` (`_project/Scripts/AppRules/`, namespace
  `KitchenDesign.AppRules`): plain classes. `AppRulesFactory.Create(UserFilter)` picks one. Until
  B1.3 the forward `ProFitGlobals.appRules` creates it from the profile. From B1.3 the
  `AppInitializer` creates it once.
- New enum `PriceSheetExtras` (`_projectShared/Enums/`: `None = 0`, `StollCustomKitchen = 1`,
  `GeminiCatalog = 2`).
- Every app check, and the rule that replaces it:

  | Where (today) | What it does | Rule |
  |---|---|---|
  | `ProFitWeb` 2276, 3012 | product and cabinet side padding: Stoll 0, others 5" | `OccupantMinimumHorizontalPaddingInMeters` |
  | `CabinetLayout` 1615 | column width: Stoll 6", others 3" | `CabinetColumnWidthInMeters` |
  | `CabinetSpan` 252; `DesignerScene` `ModifyProductInfoForStollBuild` (`Backing:BarTop`) | Stoll high bar top riser 3" deep | `HighBarTopRiserDepthInMeters` (`null` = use the product size) |
  | `CabinetSpan` 769; `Kitchen` 2850 (re-apply after load) | Stoll high bar top countertop 2" thick | `HighBarTopCountertopThicknessInMeters` (`null` = kitchen value; the re-apply runs when it has a value) |
  | `CabinetSpan` 805 | Stoll high bar top countertop 12" (14" drawn) | `HighBarTopCountertopDepthInMeters` |
  | `DesignerScene` `ModifyProductInfoForStollBuild` | fridge side padding 0; bottom-anchored product inset 4" | `FridgeHorizontalPaddingInMeters`, `BottomAnchoredProductInsetInMeters` (`null` = unchanged) |
  | `DesignerScene` 536 (`IsStollBuild`) | smoker cut-out height applied on content load (Stoll, or Catalog method) | `AdjustsSmokerCutOutHeight` |
  | `Kitchen` 923, 1130 | the Stoll and Gemini price sheet blocks | `PriceSheetExtras` (the blocks themselves move into per-app price sheet code in area 4) |
  | `Kitchen` 2031 | Stoll disclaimer when not in Catalog | `NonCatalogQuoteDisclaimer` (`null` = none) |
  | `Kitchen` 299 | measurement fractions follow the brand catalog (Gemini) | `MeasurementFractionsFollowBrandCatalog` |
  | `CabinetTickerWidget` 129 | fractions in the ticker (Gemini) | `ShowsTickerFractions` |
  | `Kitchen` 5913; `DesignerScene` 450; `ProductSearchUI` 99 | a brand catalog is required: no "catalog changed" while it's null, loading image from the catalog, search limited to the catalog's sections | `RequiresBrandCatalog` |
  | `ProductSearchUI` 75 | no storage in product search (Gemini) | `ShowsStorageInProductSearch` |
  | `DesignerToolbarUI` 131 | plunge button (Stoll) | `ShowsPlungeCategory` |
  | `ManufacturerWhitelistUI` 60; `DesignerToolbarUI` 376 | Stoll's products only in Stoll | `IncludesStollManufacturer` |
  | `DesignerToolbarUI` 689 | manufacturer 137 only in ProFit | `IsManufacturerIdAvailable(int)` |

  `Const.StollOccupantMinimumHorizontalPaddingInMeters` and `Const.StollCabinetColumnWidthInMeters`
  move into `StollAppRules`; the non-Stoll values stay in `Const` (the kitchen uses them
  elsewhere). Not app checks, so unchanged: `LogInUI` 79, `SignUpUI` 181, `QuickLogInWidget` 21.
  They compare or send the app's `userFilter` itself (its identity on the server).
  The manufacturer check was first one rule, `IsManufacturerAvailable(id, name)`. It became two
  because the whitelist screen hides only Stoll, never 137: one rule would have started hiding
  137 there.
- `ProFitGlobals.isAppOptionGemini/ProFit/Stoll` are deleted.
- Check: compile; `runall.sh` and `menucheck.sh` (Gemini). The baseline only covers Gemini, so
  there is also a per-app table: an editor snippet evaluates every old expression and every new
  rule for each of the three `AppOptions` assets, and they must be equal.
- **Done 2026-10-22.** Compiles with no errors. `rulescheck.cs`: 60 checks, 0 mismatches.
  `runall.sh` against the A3 run (`GeminiBaseline/unity6-after-A3-2026-10-22/`, kept for this):
  `resave.json` identical in all 8 designs, measurements identical apart from camera angles within
  0.0002°, save photos within 0.09/255. Kept as `unity6-after-B1.1-2026-10-22/`. `menucheck.sh`:
  21 / 21 / 1,268, as before. User files backed up, restored and hash-checked; no Addressables
  change. New checks for the refactor: `compare_runs.py`, `photodiff.sh`, `coldstart.sh`,
  `rulescheck.cs` (`Tools/Baseline/README.md`). `ProFitGlobals.appRules` creates the rules once
  per domain and resets at `SubsystemRegistration`. In edit mode it keeps the first app it saw
  until the next domain reload; play mode always reloads the domain in this project.
- **Found while checking:** `SplashScene` is switched off in the build list (the old project's
  too), so the app starts at `PlatformSetupScene` and B1.2's "build index 1" after the boot scene
  is PlatformSetup.

**B1.2 — Boot scene.**
- `_projectAssets/Scenes/BootScene.unity`, build index 0, with one object holding
  `AppInitializer` (`DontDestroyOnLoad`). `[SerializeField] private AppProfile appProfile` points
  at the same `Resources/AppProfile.asset`, so switching apps works as it does today.
- `AppInitializer.Awake` does what `Main.RuntimeInit` did, in the same order (DOTween's capacity
  first, R-ARC-4), then `ProFitGlobals.Init()` unchanged. `Start` loads build index 1: today
  PlatformSetup, since Splash is switched off. It has to be `Start`: a load started in the
  first scene's `Awake` runs before that scene counts as loaded, so `MelcherSceneManager` doesn't
  register it for unloading and logs "The scene BootScene appears to have been unloaded from
  outside of MelcherSceneManager". If Splash is ever switched on again, it is index 1
  and its `buildIndex + 1` still leads to PlatformSetup. `Main.cs` is deleted. The F1 kick
  becomes a menu item with an F1 shortcut in `_projectDebugging` (`Tools/Kitchen Design/Kick
  signed-in user`), instead of a coroutine that polls input.
- **Playing from any scene in the editor:** `AppInitializer` is a prefab
  (`_projectAssets/Prefabs/AppInitializer.prefab`); `BootScene` holds an instance.
  `StartAppForEditor` (`#if UNITY_EDITOR`, `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`)
  instantiates the prefab when play starts in any other build-list scene. That is the moment
  `Main.RuntimeInit` ran, so pressing play in `MenuScene` behaves as before and the checks don't
  change. `AppInitializer` loads build index 1 only when it sits in the boot scene (build index
  0). A scene outside the build list (a test scene) starts as itself with no app. Today such a
  scene still gets `Main`'s culture and log level, so that is a small difference.
  First built with `EditorSceneManager.playModeStartScene` (play always starts in `BootScene`,
  which then loads the scene that was open). Dropped: with a start scene different from the open
  scene, 6.6's Hierarchy window logs 4 `Assertion failed on expression: 'manager != NULL'`
  (`HierarchyWindow.SetViewState`) during every play. That would add editor noise to every check
  and every play.
- **Scene initializers:** an abstract `SceneInitializer : MonoBehaviour` with `Initialize(...)`
  taking the app systems as arguments, and `MenuSceneInitializer` / `DesignerSceneInitializer` on a
  root object of each scene. The `AppInitializer` listens to `SceneManager.sceneLoaded`, takes the
  loaded scene's `SceneInitializer` from its root objects (`GetRootGameObjects` +
  `TryGetComponent`, not a `Find…`), calls `Initialize`, then raises `EventApp.OnSceneReady`
  (`_projectEvents/Events/EventApp.cs`, Doge `ActionEvent`). In area 1 the two only keep what they
  receive. Because `OnStateAdded` runs first (see above), today's scene scripts can't use it there;
  they keep using the forwards until area 5 moves their start-up into the initializer and the
  scene stops being a `MelcherStateMachineState` (decision 1).
- Check: as B1.1, plus `coldstart.sh` from `PlatformSetupScene` and from `BootScene` (both must
  give `… PlatformSetupScene MenuScene`, the Welcome screen and the reference console counts).
  The Remember Me path (a saved token) isn't covered: the quick sign-in doesn't save one, and
  saving one would mean writing the user's PlayerPrefs.
- **Done 2026-10-22.** Compiles with no errors. `coldstart.sh` from `PlatformSetupScene` and from
  `BootScene`: both `PlatformSetupScene MenuScene`, Welcome, 13 errors, 4 warnings (the B1.1
  reference). In play from `MenuScene`: one `AppInitializer` (in `DontDestroyOnLoad`), the menu's
  initializer holds the profile and `GeminiAppRules`, culture en-US. `runall.sh` against the B1.1
  run: `resave.json` identical in all 8, camera noise only, photos within 0.14/255. Kept as
  `unity6-after-B1.2-2026-10-22/`. `menucheck.sh`: 21 / 21 / 1,268 errors (warnings at the end
  vary by a couple between runs: 8,694 / 8,696 / 8,694). Saving `MenuScene` and `DesignerScene`
  in the editor also wrote edit-time values the app recomputes at runtime: TMP's default font
  (LiberationSans SDF) on three icon texts, the localizer's FontAwesome tag, one
  `MelcherSafeAreaConformerDock` size; `DesignerScene` dropped the orphan `m_RendererData`.
  The baseline shows no effect.

**B1.3 — Split `ProFitGlobals` into app-wide systems.** Plain classes, created and owned by the
`AppInitializer`; MEC coroutines keep working without a MonoBehaviour.

| New | Folder, namespace | Takes over |
|---|---|---|
| `LaunchOptions` | `_project/Scripts/Game/`, `KitchenDesign` | `isVerbose`, debug mode; `GetWebDomainOfApp`, `GetDistributorUrlParameter` (how and where the app was started) |
| `AppProfile` (the asset) | stays where it is | `appProfile`, `appOptions`; gains `GetFallbackJsonPath` and `GetAppResourcesName` from `ProFitUtility` (they depend only on the profile) |
| `CatalogDataStatus` | `_project/Scripts/CatalogData/`, `KitchenDesign.CatalogData` | `manufacturers`, `productGroups`, `productTags`, `cabinetCompatibilities`, `cabinetSectionCategories`, `serverBrandCatalogs`, `runtimeBrandCatalogs`, `sourceProducts`, `runtimeProducts` and their lookups, `baseProductIdToProductVariantIds`; `Init`'s loading (fallback, then server), `LoadFallbackWebResponse`, `LoadAndApplyProductOverrides`, `BuildProductVariantLists`, `PopulateRuntimeBrandCatalogs`, `ExecuteWhenReady` (as `ExecuteWhenLoaded`), the editor fake products; from `ProFitUtility` the `Find…` lookups for manufacturers, groups, categories, brand catalog infos, compatibilities and products, and `GetProductVariantList` |
| `ClientListStatus` | `_project/Scripts/Session/`, `KitchenDesign.Session` | `userClients`, `activeClientId`, `activeQuoteId`, `activeClientWantsToBeContacted`, `TryGetClient…`, `TryGetClientCombinedName` |
| `SessionStatus` | `_project/Scripts/Session/`, `KitchenDesign.Session` | `user` (with the kiosk preference it reads), `userProfile`, `isKioskModeEnabled`, `useWhiteLabelMode`, `logInCoroutineState`, `LogOut`, `ForgetRememberMeData`, the auto-kick coroutine, `KickUser`; from `ProFitUtility` `FinishLogInCoroutine`, `ShouldUseWhiteLabelMode`, `GetWhiteLabelAuthorizedDomain`; `Const.PlayerPrefs_Token` |

- `ProFitGlobals.LogInCoroutineState` becomes `LogInState` in `_projectShared/Enums/`
  (`NotStarted = 0`, `Running = 1`, `Finished = 2`; never saved). `AppOptions.CabinetDataAvailability`
  moves there too (`None = 0`, `ClientOnly = 1`, `Server = 2`). It is a `MelcherStableEnum`, which
  stores names, so the assets don't change.
- The editor menu item `DumpSinksWithZeroDepth` moves to `_projectDebugging`.
- Inside the systems, collaborators are passed in: `SessionStatus` gets the profile, the client
  list and the catalog data in its constructor; `CatalogDataStatus` gets the profile. Every caller
  in the old code goes through `ProFitGlobals`, including `PlatformSetupScene`, `MenuScene` and
  `DesignerScene`. Their start-up runs in `OnStateAdded`, before a scene initializer can hand them
  anything (see B1.2).
- The `ProFitUtility` functions that moved are called through the system they now belong to:
  `ProFitGlobals.catalogData.FindManufacturerById(…)`, `ProFitGlobals.session.FinishLogInCoroutine(…)`,
  `ProFitGlobals.launchOptions.GetWebDomainOfApp()`, `ProFitGlobals.appProfile.GetFallbackJsonPath(…)`.
  The two category lookups and the list overload of `FindManufacturerByName` take their list as
  an argument and stay static (`CatalogData.CatalogDataStatus.…`). 63 call sites in 19 files,
  rewritten by a one-off script (session scratchpad).
- Without an `AppInitializer` (edit mode, a test scene outside the build list) the forwards
  create default instances on first use, so editor tools and `OnValidate` that read
  `ProFitGlobals` work as before (empty lists, the profile from `Resources`).
- **Done 2026-10-22.** Compiles with no errors. `rulescheck.cs` 60/0. `coldstart.sh` from
  PlatformSetup and from Boot: the reference (13 errors, 4 warnings). `runall.sh` against the
  B1.2 run: `resave.json` identical in all 8, camera noise only, photos within 0.14/255; kept as
  `unity6-after-B1.3-2026-10-22/`. `menucheck.sh`: one run 22 / 22 / 1,269, the next 21 / 21 /
  1,268. The app's own log lines were the same in both, so the extra error was the editor's.
  Removed from `ProFitGlobals` with no forward, because nothing outside the new systems used
  them: `Init`, `isInitialized`, `productTags`, `cabinetCompatibilities`,
  `LoadFallbackWebResponse`, `LoadAndApplyProductOverrides`, `BuildProductVariantLists`,
  `PopulateRuntimeBrandCatalogs`, `FindRuntimeProductById/BySku`, `StartAutoKickCoroutine`,
  `TryGetClientQuoteIndex`, the `LogInCoroutineState` enum (now `LogInState`). The editor menu
  item moved to `Tools/Kitchen Design/Dump sinks with zero depth`.
- **Who uses whom:** `SessionStatus` → `ClientListStatus`, `CatalogDataStatus` (log out clears
  both; the login flow fills both). Neither of those uses the session. No cycles.
- **`EventBridge` in area 1: no delegate is removed.** The systems call the same delegates the
  old code called (`Menu.showFullScreenSpinner`, `showBackgroundSpinner`,
  `setSpinnerStatusMessage`, `showHomeUi`, `showWelcomeUi`, `pushLogInUi`,
  `show/hideTrialCountdownUi` (menu and designer), `showUserKickedPrompt`,
  `Designer.loadMenuScene`). Each is a command to a screen, and the screens are area 5. The only
  new event is `EventApp.OnSceneReady`.
- Check: as B1.2.

**B1.4 — The generic helpers.** Mechanical: the class name changes at each call site, nothing
else.
- `Const`'s unit conversions (`Inches2Meters`, `Meters2Inches`, `Meters2Centimeters`,
  `Centimeters2Meters`, `InchesPerFoot`; about 175 uses in 40 files) → `KitchenDesign.Shared.Common.Units`.
- From `ProFitUtility`, into `Utility/` (`KitchenDesign.Utility`), one class per concern:
  `MeasurementFormat` (`FormatMetersToCentimeters`, `FormatMetersToImperial…`,
  `TryParseImperialString`), `UrlUtility` (`GetBaseDomain`, `GetDomain`), `RuntimePrefabs`
  (`InstantiatePrefabAsRuntimePrefab`, `IsPrefab`), `RendererUtility` (`SetAllRendererMaterials`,
  `SetGameObjectLayerRecursively`), `RenderQuality` (`TrySetRenderQuality`), `MemoryUtility`
  (`GarbageCollectOnLowMemoryDevices`), `RandomNames` (`RandomName`, `RandomPlace`).
- What stays, until its area: in `Const`, the kitchen's paddings and limits and `Const.Gemini`
  (area 4), `ErrorColor` (5), `DialogResult_*` and `Tag` (editor tools, 3). In `ProFitUtility`,
  the drag planes, cuboid tests, `IsDistanceAlmostZero`, the corner wedge length,
  `DoesCategoryUseSizes` and `Vector3ToJson` (4), `FindProductVisual` and
  `FindDoorStyleVisual` (3), the URLs the menu opens (`OpenProductUrl`,
  `OpenSubscriptionLandingPage`, `ShowTermsAndConditions`; 5), and `ProFitUtility.Editor` (3).
- Check: compile, `runall.sh`, `menucheck.sh`.
- **Done 2026-10-22.** 168 unit constants and 101 helper calls rewritten in 45 files by a one-off
  script (session scratchpad); comments in the moved code dropped, their whys in CLAUDE.md
  ("Generic helpers"). The editor-only random names became `RandomNamesForEditor`, and the URL
  self-tests a debug menu (`_projectDebugging/Menus/UrlDebugMenu.cs`). `KitchenDesign.Tools.Editor`
  gained a reference to `KitchenDesign.Shared`. Compiles in the editor and as macOS player
  scripts. `rulescheck.cs` 60/0; `coldstart.sh` both the reference; `runall.sh` against B1.3:
  `resave.json` identical, camera noise only, photos within 0.09/255 (kept as
  `unity6-after-B1.4-2026-10-22/`); `menucheck.sh` 21 / 21 / 1,268.
- **Area 1 is done.**

### The `Initialize()` chain after area 1
1. `LaunchOptions`: first, because the log level and DOTween's debug mode depend on it.
2. Culture, quality level, render debug UI, DOTween capacity and init: before anything formats
   numbers or tweens.
3. `AppProfile` (serialized), then the `IAppRules` for its `userFilter`: before any data is
   parsed, because parsing products reads the padding rule.
4. Construct `CatalogDataStatus`, `ClientListStatus`, `SessionStatus`, then hand them and the
   rules to the `ProFitGlobals` forwards. The forwards must work before step 5, because the loading
   code (`ProFitWeb`, area 2) still reads them.
5. `SessionStatus.Initialize()`, `ClientListStatus.Initialize()`, then
   `CatalogDataStatus.Initialize()`, which starts loading (fallback, then server). The order is
   the order the old `Init` reset its fields in; the session's reset writes the kiosk preference
   of user 0, as before.
6. `Start`, boot scene only: load build index 1 (`PlatformSetupScene`). In the editor, any other
   build-list scene plays as itself (`StartAppForEditor`). On each scene load: that scene's
   `SceneInitializer.Initialize(...)`, then `EventApp.OnSceneReady`.

These whys go into CLAUDE.md's "The Initialize() chain" when the code lands (`file-only` mode).

### Found while reading, kept as they are
Area 1 doesn't change behaviour. The user decides about these.
1. **Kiosk mode for white label sessions.** `PlatformSetupScene` reads
   `"User/{pendingUser.Value.id}/isKioskModeEnabled"`: no `$`, so it is that literal text, and not
   the `User.<id>.isKioskModeEnabled` form used everywhere else. It then assigns
   `isKioskModeEnabled`, whose setter writes under the *current* user's key, which at that moment
   is the default user (id 0). So a white label session always starts with kiosk mode off and
   writes "off" for user 0.
2. **`MenuScene.OnGameDataReady` sets `appOptions.simulateWhiteLabelMode = false`** in the
   editor. That changes the `AppOptions` asset in memory, and on disk the next time the editor
   saves assets.
3. **`ModifyProductInfoForStollBuild`'s bottom inset** was 4" for Stoll and for non-Stoll
   products, so its manufacturer test changed nothing. Removed in B1.1, together with the Stoll
   manufacturer lookup that fed it (`GetStollManufacturer`; `FindManufacturerByName` has no side
   effects, so nothing observable changes). The method is now `ModifyProductInfoForAppRules`.

The user left these and the naming to Claude (2026-10-22). Items 1 and 2 stay as they are until
the screens are refactored (area 5), so area 1 can be checked as "no behaviour change".

### Not in area 1
`ProFitWeb` and `ProFitWebTypes` (area 2; `UserFilter`, `User` and `UserProfile` stay there, and
the http/https probe stays a `ProFitWeb` call inside `CatalogDataStatus`'s loading). Building-method
and brand rules (area 4). The screens' use of the systems, including white label and kiosk
(area 5).

### Runs for each step
`runall.sh` and `menucheck.sh` use the committed Gemini `AppProfile.asset`. Before them: hash and
back up the user's `AppProfile.asset` and every `Resources/*/FallbackJSON` folder (signing in
rewrites them), and put in the committed profile. After: restore from that backup (never from
git), check the hashes, `AssetDatabase.Refresh()`, and check `git status` for Addressables group
changes (CLAUDE.md, Gotchas).

## Area 2 — Server and offline data (planned 2026-10-22)
The user left the design to Claude ("feel free to proceed any time"); same rules as area 1: no
behaviour change, each step checked (the run-against-run checks in `Tools/Baseline/README.md`) and
committed on its own.

### Today (read 2026-10-22)
- `ProFitWeb` (4,285 lines) is a static class: about 30 requests (`MelcherWebRequestBuilder`
  coroutines with a local `ParseResponse`), about 25 `TryParse…` functions that turn the server's
  JSON into the types in `ProFitWebTypes`, the fallback file names, the server address
  (`domain`/`Host` from the profile, the mutable `UseHttps`), and helpers
  (`ValidateJsonResponse`, `CombineParserErrorsIntoException`, the auth header, and the
  editor-only `SaveFallbackJsonResource` that rewrites `Resources/<App>/FallbackJSON` whenever a
  catalog request succeeds in the editor).
- About 70 uses in 25 files: the screens (sign-in, sign-up, profile, clients, quotes), the new
  `CatalogDataStatus` and `SessionStatus`, `ProductIdRemapDatabase` (editor, reads the fallback
  JSON) and `Kitchen` (one error helper).
- `ProFitWebTypes` (505 lines): 5 enums, 10 structs and classes, an extension class. None is
  `[Serializable]`; the parsers read the JSON by key strings. So the C# names and namespaces can
  change, and **the key strings in the parsers are what R-DATA-3 freezes**. Of the enums, only
  `ServerType` (`AppProfile.m_TargetServerType`), `UserFilter` (`AppOptions.m_UserFilter`) and
  `MeasurementUnit` (`AppOptions.m_SimulatedPreferredUnits`, editor) are serialized, as ints.
- Name-as-text traps for renames: `Resources.Load<AppProfile>(nameof(AppProfile))` (the asset is
  called `AppProfile`), `AppProfile.GetAppResourcesName` strips `nameof(AppOptions) + "_"` from
  the options asset's name, `ProFitBuildProcessor` searches `t:{nameof(AppOptions)}` (that one
  follows a rename correctly). No scene, prefab, asset or UXML names any of these types.

### Steps
**B2.1 — Server data types.** `ProFitWebTypes.cs` split one type per file:
- Enums to `_projectShared/Enums/` with explicit values equal to today's: `ServerType`
  (Production 0, Test 1), `UserType` (None 0, Trial 1, Dealer 2, Offline 3), `UserFilter` (None 0,
  ProFit 1, Stoll 2, Gemini 3), `Currency` (USD 0, CAD 1), `MeasurementUnit` (None 0,
  FeetAndInches 1, Centimeters 2).
- The data types to `_project/Scripts/Server/Data/`, namespace `KitchenDesign.Server`: `User`,
  `UserProfile`, `UserClient`, `UserClientQuote`, `ProductTagInfo`, `CabinetCompatibilityInfo`,
  `ManufacturerInfo`, `ManufacturerDoorStyleInfo`, `ProductGroupInfo`, `BrandCatalogInfo`,
  `CabinetSectionCategoryInfo`; `ResponseCode` and `MeasurementUnitExtensions` to
  `_project/Scripts/Server/`. `ProFitWebTypes.cs` is deleted (its `.meta` with it; nothing binds
  to its GUID, it holds no MonoBehaviour).
- Every file that names one of them gets `using KitchenDesign.Server;` /
  `using KitchenDesign.Shared.Enums;` (script).
- **Done 2026-10-22.** 13 files in `Server/` and `Server/Data/`, 5 enums; 51 files got the
  usings (script in the session scratchpad); `rulescheck.cs` now names
  `KitchenDesign.Shared.Enums.UserFilter`. Checks as area 1, all the reference; `runall.sh` against
  B1.4 identical (kept as `unity6-after-B2.1-2026-10-22/`). The fallback JSON the run's sign-in
  wrote is kept in the session scratchpad (`fallback-ref-b21/`) as B2.2's reference.
  `menucheck.sh` now lists the errors after sign-in; the occasional extra error (22 instead of 21,
  seen in B1.3 and here) is the CLI's own "Failed to handle /api/exec request: Main thread
  operation timed out after 5000ms", logged when a polling snippet lands on a heavy frame.

**B2.2 — The server client.** `ProFitWeb` is split by what each part is, in `_project/Scripts/Server/`
(`KitchenDesign.Server`):
- `ServerConnection` (class): the address (`Domain`, `Host`, `UseHttps`, `IsTestServer`,
  `WhitelistedEmbedDomains`) from the profile, the http/https probe that `CatalogDataStatus` does
  today (`ProbeCoroutine`), and the editor-only fallback writer.
- `ServerJson` (static, pure): `ValidateJsonResponse`, `CombineParserErrorsIntoException`, the
  auth header.
- Requests, one class per server resource, each constructed with the `ServerConnection`:
  `AccountRequests` (sign-up, log-in, white label check, password reset, profile get/update,
  delete account), `ClientRequests` (client list/get/create/update/delete, find by e-mail),
  `QuoteRequests` (create/update/delete/get quotes, idempotency token), `CatalogRequests`
  (manufacturers, categories, groups, products, brand catalogs, cabinets, tags,
  compatibilities), `AnalyticsRequests`.
- Parsers, static and pure, next to their requests: `AccountJson`, `ClientJson`, `QuoteJson`,
  `CatalogJson`. The product and cabinet parsers take the `IAppRules` as an argument (the padding
  rule) instead of reading `ProFitGlobals`.
- `FallbackJsonFiles` (constants): the fallback file names.
- `ServerClient`: owns the connection and the request groups; constructed by `AppInitializer`
  before the catalog data, which (with the session) now receives it in its constructor. Old
  callers reach it through `ProFitGlobals.server` until their area.
- `ProFitWeb.cs` is deleted.
- **Done 2026-10-22.** 20 files in `Server/`, split by a one-off script (session scratchpad);
  comments dropped, whys in CLAUDE.md ("Server client"). 24 files' calls rewritten
  (`ProFitGlobals.server.<Group>.<Request>`, `Server.<Parser>.TryParse…`). The offline branches
  of `GetProductList`/`GetCabinetList` now use the lists passed in instead of
  `ProFitGlobals.manufacturers`/`cabinetSectionCategories`; every caller passes those same
  lists. Checks: `rulescheck.cs` 60/0, `coldstart.sh` both the reference, `runall.sh` against B2.1
  identical (photos within 0.09/255; kept as `unity6-after-B2.2-2026-10-22/`), the 8 fallback
  JSON files the sign-in wrote byte-identical to B2.1's, `menucheck.sh` 21 / 21 / 1,268. Log
  lines from the parsers now carry the new class names (`[CatalogJson] Error parsing cabinet…`
  instead of `[ProFitWeb] …`).

**B2.3 — The data assets, by what they are.** `GroupDatabase`, `ProductIdRemapDatabase`,
`AppProfile` and `AppOptions` are ScriptableObjects: renamed with the `SO` suffix (R-CODE-8),
`git mv` with their `.meta` into `_project/Scripts/Scriptables/` (`KitchenDesign.Scriptables`),
nested types into their own files. The two text uses of the names stay literal (`"AppProfile"`,
`"AppOptions_"`). `BrandCatalog` stays for area 4: it carries the brand rules that
`Const.Gemini` and `Kitchen` will move onto.
- **Done 2026-10-22.** `git mv` of the five files with their `.meta`; renamed by a script that
  leaves string literals alone (the code inside interpolated `{…}` excepted), which kept the paths
  `Resources/AppProfile.asset` and `AppOptions/…` intact; then fixed by hand: three properties the
  rename had also renamed (`AppProfile`), `Resources.Load<AppProfileSO>("AppProfile")` and
  `"AppOptions_"`. 15 files got the usings. Checked: editor and macOS player compile; every
  asset of the four types loads as the new class with its data (8 group databases with their
  entries, the remap database's 1,761 entries); no asset file changed; `rulescheck.cs` 60/0;
  `coldstart.sh` both the reference. The first `runall.sh` after the editor was reopened (the user
  paused the work for a release of the old app) differed in three cover photos (2.3–16.9/255, the
  known fresh-editor texture artefact) and in 27755's live orbit camera (yaw 89° instead of 45°;
  the save-photo cameras were identical); the repeat on the warm editor was identical to B2.2
  (photos within 0.07/255; kept as `unity6-after-B2.3-2026-10-22/`, the first run as
  `unity6-B2.3-fresh-editor-2026-10-22/`). `menucheck.sh` 21 / 21 / 1,268.
- **Area 2 is done.**

### Checks
As area 1 (`rulescheck.cs`, `coldstart.sh` ×2, `runall.sh` + `compare_runs.py` + `photodiff.sh`,
`menucheck.sh`), plus for B2.2: the fallback JSON a sign-in writes must be byte-identical to the
one the run before wrote (it goes through the same parse/serialize path), compared before the
backup is restored.

## Area 3 — Products and visuals (planned 2026-10-22)
The user left the design to Claude ("write its section in the plan, then build it step by step
like areas 1 and 2"); same rules: no behaviour change, each step checked and committed on its own.

### Today (read 2026-10-22)
- **`ProductInfo`** (287 lines): the server's product record, a plain class (not
  `[Serializable]`; parsed by key in `CatalogJson`). Its copy constructor makes the runtime copies;
  an editor-only static constructor asserts 26 fields so a new field isn't forgotten there. 504
  uses in 32 files.
- **The visual assets**, all ScriptableObjects: `ProductVisual` (an info plus the Addressables
  address of its content; `MakeRuntimeClone`), `ProductVisualInfo` (the always-loaded data: id,
  thumbnail, anchor, the section flags; a nested enum `CabinetCutOutMode`), `ProductVisualDatabase`
  (the list of source visuals; it makes the runtime clones itself, in `Awake` in builds and on
  entering play mode in the editor, and `FindRuntimeVisualByID`). The database is referenced by
  `DesignerScene`, `Kitchen` and four designer screens, all in `DesignerScene.unity`.
- **`ProductVisualContent`** (940 + 915 editor lines): the root component of every visual prefab.
  Most of it is the placed product's cut-outs, measures, plan labels, highlight materials and
  collider: the kitchen model.
- **Addressables:** `AssetAddress<T>` (a `[Serializable]` struct holding an address, plus an
  editor-only cached asset), `IAssetAddress` (for its drawer), `AssetAddressUtility` (unused),
  `Internal.AssetAddressData`; `AddressableEditorUtility` and `ProFitAddressableEditorUtility`
  (editor-only, in the runtime assembly). Addresses are asset paths.
- **`ProductVisualContentLoader`**: a `MelcherMonoBehaviourRegistry` on an object in
  `DesignerScene`, found 20 times with `FirstFromActiveScene()` (Kitchen, Cabinet, CabinetLayout,
  two screens, DesignerScene). It loads a visual's content through Addressables once, keeps the
  handle, makes a runtime copy of the prefab, raises `onProductVisualContentLoaded`, and releases
  every handle when the designer scene is destroyed.
- **Product rules in `DesignerScene`**: `OnBuildingMethodChanged` rebuilds the runtime products
  from the source products and applies the app rules (`ModifyProductInfoForAppRules`; the Catalog
  branch calls an empty method); `OnProductVisualContentLoaded` sets the smoker cut-out height;
  `OnBrandCatalogChanged` sets the product side padding per brand catalog.
- **Enums** used by the product and visual types: `ProductCategory`, `ProductCategorySpecific`,
  `ProductAnchor`, `FlooringType`, `CabinetModelType`, `CabinetSectionFootingType`,
  `ProductVisualInfo.CabinetCutOutMode` (`ProductVisualContent` has a second, unused copy). All are
  stored in assets as `MelcherStableEnum` (by name), never as plain enum fields; the server's
  category strings are mapped by `CatalogJson`. **One UXML names a type as text:**
  `ProductVisualDatabaseEntry.uxml`, `KitchenDesign.ProductAnchor, KitchenDesign`.
- **`ProFitUtility` leftovers for this area:** `FindProductVisual` (2 callers),
  `FindDoorStyleVisual` (unused; the door lookup in use is `CabinetSectionDoorUtility`'s),
  `ProFitUtility.Editor` (`OverwriteSpecificLogFile`, 3 callers; `FindAllAssetGuidsInFolder`, 2;
  `FindAllCabinetSectionModelGuids`, unused).
- **Editor tools** (in `KitchenDesign.Tools.Editor`): the database window
  `ProductVisualDatabaseManager` (2,453 lines, 2 UXML) and the database inspector, the Cloder
  database sets/states managers and their entry file, the visual tools (dump, mismatched models,
  reserialize, insulated-jacket fix, editor help), the `AssetAddress` drawer, the Addressables
  group commands, `FilteredObjectSelector`.

### Not in area 3
`ProductVisualContent` stays as it is for area 4: what it holds is the placed product (cut-outs,
measures, materials), and area 4 splits it along with `Occupant` and `CabinetCutOut`. Its name
stays (the old CLAUDE.md uses it). `CabinetType` and its extensions stay for area 4 (they need
`KitchenBuildingMethod`). The door-style lookup (`CabinetSectionDoorUtility`) and the brand
padding's manufacturer ids are brand rules (area 4). `ProductVisualThumbnailCreator` is area 6.

### Steps
**B3.1 — Product enums to Shared.** `ProductCategory`, `ProductCategorySpecific`,
`ProductAnchor`, `FlooringType`, `CabinetModelType`, `CabinetSectionFootingType` and
`CabinetCutOutMode` (out of `ProductVisualInfo`) to `_projectShared/Enums/` with explicit values
equal to today's; the unused copy in `ProductVisualContent` deleted. Their extension classes
(`ProductAnchorExtensions`, `ProductCategoryExtensions`, `ProductCategorySpecificExtensions`;
pure, enum-only) to `_projectShared/Common/`. `git mv` with `.meta`. The UXML's type string
becomes `KitchenDesign.Shared.Enums.ProductAnchor, KitchenDesign.Shared`. Usings added by script.
Check: compile; every visual info asset still loads its stable enums (a count of each value
across the database, before and after); the database window opens; then the usual runs.
- **Done 2026-10-22.** 7 enums (values as listed by the script: e.g. `ProductCategory` 0–21,
  `CabinetCutOutMode` 0–3), 3 extension classes; 27 files got usings (script in the session
  scratchpad, with a small C# lexer that tells code from comments and strings). Compiles with no
  errors. New check `visualscensus.cs`: identical before and after (901 entries, hash
  `E3B8044CE6426AA5`; `D72B3E9CB18746DF` since its output is Unicode-normalised, B3.2). The database, states and sets windows open and close with no console
  error; the UXML type string resolves. `rulescheck.cs` 60/0; `coldstart.sh` both the
  reference; `runall.sh` against B2.3: `resave.json` identical, camera noise only, photos within
  0.09/255 (kept as `unity6-after-B3.1-2026-10-22/`); `menucheck.sh` 21 / 21 / 1,268. The sign-in
  wrote a Gemini `products.json` that differs from git: the server has new products (Delta Heat);
  this run's files are B3.2's reference.

**B3.2 — The product and visual types by what they are.**
- `ProductInfo` → `_project/Scripts/Server/Data/` (`KitchenDesign.Server`), next to the other
  server records.
- `ProductVisual` → `ProductVisualSO`, `ProductVisualInfo` → `ProductVisualInfoSO`,
  `ProductVisualDatabase` → `ProductVisualDatabaseSO` (R-CODE-8), `git mv` with `.meta` into
  `_project/Scripts/Scriptables/` (`KitchenDesign.Scriptables`). Asset file names and the
  `ProductVisual_` / `^` / `@` naming they rely on don't change: those are asset names, which the
  rename script leaves alone (string literals, including literals inside interpolation holes).
- `AssetAddress<T>` and `AssetAddressData` → `_project/Scripts/Serializables/`
  (`KitchenDesign.Serializables`). `IAssetAddress` and the editor helpers (merged into one
  `AddressablesForEditor`, R-ASM-4) → `_project/Scripts/AddressableContent/`
  (`KitchenDesign.AddressableContent`; not `…Addressables`, which would hide Unity's
  `Addressables` class inside our namespace). `AssetAddressUtility` deleted (unused).
- `FindProductVisual` → `ProductVisualLookups.FindById` (`_project/Scripts/Products/`,
  `KitchenDesign.Products`). `ProFitUtility.FindDoorStyleVisual` and
  `FindAllCabinetSectionModelGuids` deleted (unused); `OverwriteSpecificLogFile` →
  `Utility.LogFilesForEditor`; `FindAllAssetGuidsInFolder` (a one-line wrapper) inlined.
- Check: compile (editor and macOS player); every visual, info and database asset loads as the new
  class with the same data (counts, ids, content addresses); no asset file changes; the usual runs.
- **Done 2026-10-22.** Script in the session scratchpad (moves with `.meta`, renames in code only,
  comments of the moved files dropped, usings added in 35 files); `ProFitUtility` and the
  database window's two folder searches edited by hand. Compiles in the editor and as macOS player
  scripts. All 901 visuals, 901 infos and the database load as `…SO` (main asset type checked);
  no asset file changed. The census differed only in two content addresses with an "è": the asset
  files store it composed, the Addressables entries decomposed, and `AssetAddress.OnBeforeSerialize`
  swaps the in-memory value (an old quirk, logged at every editor start; CLAUDE.md, Gotchas).
  The census now normalises its output and is identical. Database windows and a visual's inspector
  open with no error. `rulescheck.cs` 60/0; `coldstart.sh` both the reference (the first showed 6
  warnings: those 2 address warnings, once, because the renames reloaded the assets); `runall.sh`
  against B3.1: identical, photos within 0.02/255 (kept as `unity6-after-B3.2-2026-10-22/`); the
  8 fallback JSON files byte-identical to B3.1's; `menucheck.sh` 22 / 22 / 1,269, the extra one the
  CLI's own timeout error.
- **Slip during the checks:** a second `menucheck.sh` was started by mistake after the user's files
  had been restored, so it ran with the user's ProFit profile and rewrote
  `ProFit/FallbackJSON/products.json` (the other ProFit files came out identical). It was stopped,
  and the user's file put back from the copy taken before the restore (same diff against git as
  at the start of the session). The backup helper now keeps its backup after a restore.

**B3.3 — The content loader as a constructed system, the product rules out of the scene.**
- `ProductVisualContentStatus` (`_project/Scripts/Products/`): a plain class that does what the
  loader component does (`LoadContent`, `IsLoadingOrLoaded`, `Purge`, the `ContentLoaded` C#
  event); `LoadedProductContent` (the source/runtime prefab pair) its own file. **Constructed by
  `AppInitializer`**, because the designer's start-up (`DesignerScene.OnEnable` subscribes,
  `OnStateAdded` runs) happens before a scene initializer can hand anything over (B1.2). It is
  given to `DesignerSceneInitializer`, which purges it in `OnDestroy`: the moment the old component
  released its handles. Old code reaches it as `ProFitGlobals.productContent` until its area. The
  event stays a C# event, not an `Event<Domain>`: its payload is the content prefab, a
  `KitchenDesign` type that can't move to Shared. The component is removed from `DesignerScene`
  through the editor and the script deleted.
- `RuntimeProductOverrides` (`_project/Scripts/Products/`, static): `RebuildRuntimeProducts`
  (the building-method rebuild with the app rules), `ApplySmokerCutOutHeight`,
  `ApplyBrandCatalogPadding`, taking the catalog data, the visual database, the rules and the
  content as arguments. `DesignerScene` keeps its subscriptions and calls them, so they run in the
  same order relative to the other listeners. The empty Catalog method goes; its branch keeps
  doing nothing (no inset reset in Catalog without app overrides), exactly as now.
- Check: compile; a play-mode probe that content loads, the event fires once per visual and the
  handles are released when going back to the menu (count before/after); the usual runs.
- **Done 2026-10-22.** The loader's object (`Product Visual Content Loader`, a root of
  `DesignerScene` with only that component) removed through the editor first, so the scene diff
  is exactly that object; then the script (session scratchpad). `RuntimeProductOverrides` was
  generated from `DesignerScene`'s own methods, so the logic is the same text; a dry run on a
  copy was read before it was applied. `SceneInitializer.Initialize` takes the content status as
  a third argument (the menu keeps it unused) rather than per-scene signatures. Compiles in the
  editor and as macOS player scripts. New `contentprobe.sh`: 27755 18 contents / 18 events,
  32569 8 / 8, and 0 states back in the menu each time. `rulescheck.cs` 60/0; `coldstart.sh`
  both the reference; `runall.sh` against B3.2: identical, photos within 0.05/255 (kept as
  `unity6-after-B3.3-2026-10-22/`); `menucheck.sh` 22 / 22 / 1,269 (the CLI timeout again). The
  sign-in's Gemini `products.json` differed from B3.1's only in two Kalamazoo descriptions and
  links edited on the dashboard in between; this run's files are B3.4's reference.

**B3.4 — The editor tools into `KitchenDesign.Products.Editor`.** A new editor-only asmdef in
`_project/Editor/Products/`, namespace `KitchenDesign.ProductsEditor` (R-ASM-3), with the database
window and inspector, the Cloder sets/states managers and entry file, the visual tools, the
`AssetAddress` drawer, the Addressables group commands and `FilteredObjectSelector` (with their
UXML). `KitchenDesign.Tools.Editor` references it where it still uses one of them. The
`EditorPrefs` key string stays. One type per file. Check: compile; each window and menu command of
the moved tools opens and closes with no new console error and no file change; the usual runs.
- **Done 2026-10-22.** 14 scripts and 3 UXML moved with `.meta` (the UXML reach their windows
  through the scripts' default references, kept by the `.meta`); `ProFitEditorOptions` became
  `ProductEditorOptionsSO` (in `Scriptables/`, the database window needs it) and
  `ProFitAddressableGroupMenuCommand` `AddressableGroupMenuCommand`. **The FBX import postprocessor
  stays in `KitchenDesign.Tools.Editor`:** moving or renaming a model postprocessor is expected to
  reimport every model (CLAUDE.md). Comments in the moved tools stay (moved, not refactored).
  The database window's unused `using UnityEditor.Timeline.Actions;` went, and with it the Timeline
  reference. The new asmdef needed MEC and MiniJSON (found by compiling). Checks: compiles; new
  `menuitems.cs`: 29 menu items, all registered, the 10 of the moved files the same as their
  `[MenuItem]`s at HEAD (the "before" list wasn't captured: the first version of the snippet called
  a `Menu.MenuItemExists` that isn't public in 6.6); the database, states and sets windows, the
  database and visual inspectors and `FilteredObjectSelector` open and close with 0 console
  errors; the options asset loads as the new class; census identical; no asset file changed and
  no model was reimported (906 visual prefabs were reimported after a compile, no file changed).
  `rulescheck.cs` 60/0; `coldstart.sh` both the reference; `runall.sh` against B3.3 identical,
  photos within 0.05/255 (kept as `unity6-after-B3.4-2026-10-22/`); `menucheck.sh` 21 / 21 /
  1,268. The sign-in's `products.json` differed from B3.3's only in one more Kalamazoo link and
  description edited on the dashboard.
- **Area 3 is done.**

### Checks
As area 2 (`rulescheck.cs`, `coldstart.sh` ×2, `runall.sh` + `compare_runs.py` + `photodiff.sh`,
`menucheck.sh`, the fallback JSON a sign-in writes byte-identical to the run before), with the
user's `AppProfile.asset` and every `Resources/*/FallbackJSON` folder backed up and restored
around the play-mode runs.

## Decisions (user, 2026-10-22)
1. **The core package keeps `namespace Melcher` for now.** App code imports it with
   `using Melcher;`. Later we make our own library to replace it, so phase B should not build new
   code on top of `MelcherMonoBehaviour`, `MelcherUI` and the like where a plain Unity type does.
2. **Comments go when a file is refactored.** Any why worth keeping moves into that system's part
   of CLAUDE.md; the old repo keeps the originals.
3. **All code still missing from the old repo is brought over before the refactor starts.** The
   audit is below. After that, later team changes are ported by hand into the refactored code.
4. **Phase A in one session**, A1–A3 back to back, each checked and committed separately.

## Old-repo code audit (2026-10-22)
Every branch and stash in the old repo was checked for `.cs` changes that are not in the copy
(source commit `7700e16a8c`). Each commit's code diff was test-applied to the copy's tree, forwards
and in reverse, and matched by title against the copy's history.
- **Already in the copy:** the corner-wedge / 45° corner work and the `CanAddOccupantAnywhere` fix
  (`origin/feature/pro-fit-45-degree-corners`, `origin/fix/add-occupant-anywhere-function`; the
  same commits were rebased into `feature/model-integration` under other hashes), the product
  lookup dictionary, the Stoll compatible brand, the Agora countertop pricing, the WeatherStrong
  padding, the door handle fallback, and stashes 2, 3 and 5.
- **Brought over 2026-10-22:** `1e29fe3e9b` "Add forced product occupancy in cabinet" (branch
  `feature/naturekast`): `ProductVisualInfo` gains `m_ProductManufacturersAllowedInSection` and
  `m_ForcedCabinetCuboidDepthInMeters`, and `Cabinet` uses them. With both left at their defaults
  nothing changes; only the NatureKast Evo cabinet visuals (`19c71f9825`, left behind with
  NatureKast) set them. Applied cleanly, compiles.
- **Not brought over: the rear wall waits until the user finishes it in the old project** (user,
  2026-10-22). `77f53840ee` "Add rear wall for cabinet spans"
  (`____localXXX/WORK-IN-PROGRESS`, the user's own test version: new `CabinetWall`, changes to
  `CabinetSpan`, `CabinetLayout`, `Kitchen`, `BrandCatalog`, `DesignerToolbarUI`, and a
  `RearWallButton` in `DesignerScene`). The commit is incomplete: it uses
  `ProductCategory.CabinetRearWall`, which no branch or stash defines, and leaves out
  `BrandCatalog_Toppin.asset`'s `m_HasRearWall`. Its notes list save/load, pricing, dragging,
  selection and visuals as not done, and a `TEMP` block adding 48" to the high bar top riser.
  The code applies cleanly to this project; the scene button would be rebuilt in the editor.
- **Left out:** stash 6 (an earlier version of stash 5's countertop change) and stash 8's
  `ProductSearchToolbar.cs` (April 2026, never committed; product search shipped as
  `ProductSearchUI`).
- **Content, not code:** `feature/model-integration` and `____localXXX/MAIN` have new product
  visuals, models, materials and logos since the copy (AMG, Alfresco, Coyote, Thor). Content comes
  over separately, a few products at a time (see "Open questions" in CLAUDE.md).

## How the map was made
A Python script (kept in the session scratchpad, not committed) read the 240 files with comments
and strings stripped, took each top-level type declared, counted a file as using another when one
of its identifiers (not after a `.`) names a type declared there, and found the strongly connected
parts and levels. It over-counts a little (a local variable named like a type counts) and can miss
uses that never name the type (`var` from a method call, extension methods), so the numbers are
close, not exact. Rebuild it the same way if the numbers are needed again.
