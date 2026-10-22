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
   until the areas that use it are done.
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
   products, as the old plan listed.
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
