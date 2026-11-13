# 07 — Leftovers (area 7 of the refactor)

Planned 2026-11-13. Area 7 of `03-refactor.md`. Same rules as area 6: no behaviour checks between
steps, each step compiles (editor and WebGL player scripts) and is committed; every check runs once
at the end, against `3d25da55`.

## Today (read 2026-11-13)
After areas 1–6, the runtime code left in the old folders is 22 files:
- `AssetsMelcher/Scripts` (18): helpers (`ComponentsInChildrenEnumerator`, `DOTweenExtensions`,
  `DOTweenInit`, `DebugDraw`, `ElementIndex`, `EnumInfo`, `ImperialUnit`, `ProFitCoroutineUtility`,
  `ProFitUnityExtensions`, `RecyclableTween`, `Ref`, `SequentiallyAwaitedDelegate`), three
  components (`HighlightTarget`, `PCMBandaidFix`, `TransformHelper`), and the three hubs area 1
  left for "their area": `Const` (136 lines), `ProFitUtility` (340) and `ProFitGlobals` (204,
  forwards only).
- `AssetsMelcher/Miscellaneous` (3): the cutout authoring helpers (`ComponentCutoutHelper`,
  `StorageCutoutHelper` with a nested enum, `GeminiCutoutPrefabs`, editor-only menu items).
- `AssetsMelcher/Plugins/WebGL/MelcherWebGL.cs`: the C# side of `MelcherWebGL.jslib`.
Each of the three folders has a `KitchenDesign.asmref`.

Editor code: `AssetsMelcher/Editor` (the `KitchenDesign.Tools.Editor` assembly, 10 files) and the
user's `Cloder` folder (`.asmref` files into `KitchenDesign` and `KitchenDesign.Tools.Editor`);
`Procedural Assets` holds Weaver's generated `Layers.cs`.

## Decisions
1. **Everything runtime leaves `AssetsMelcher`,** and its three `.asmref` files are deleted.
   Helpers to `_project/Scripts/Utility/` (`KitchenDesign.Utility`), components to `Behaviors/`,
   the WebGL bridge to `Utility/`. `ProFit…` prefixes are dropped (`CoroutineUtility`,
   `UnityExtensions`). `ElementIndex` and `SequentiallyAwaitedDelegate` each keep their generic and
   non-generic type of the same name in one file (one concept, two arities).
2. **`Const` and `ProFitUtility` are split by owner** (03, area 1): each constant and method goes
   to a static class named for what it is about, in the folder of the code that uses it.
3. **`ProFitGlobals` stays, in `Game/`.** It is still read by the kitchen model (constructed in
   `Kitchen.Awake`, before any initializer runs), the skin views, runtime-made rows and edit-mode
   tools (05, decision 6). Emptying it means building the kitchen from `DesignerSceneInitializer`,
   which changes start-up order and wants the checks first; listed as open.
4. **`AssetsMelcher/Editor` stays where it is for now.** `ProFitProductModelFbxImporter` is a model
   postprocessor: moving it is expected to reimport every model and rerun its material remap
   (CLAUDE.md, "Products and visuals"). The folder moves in one piece together with a planned
   model reimport. `Cloder` (the user's own tools) and `Procedural Assets` (generated) stay too.

## Steps
- **B7.1 — file the remaining runtime scripts** (decision 1) and delete the three `.asmref`s.
- **B7.2 — split `Const` and `ProFitUtility`** (decision 2).

## Results
- **B7.1 done 2026-11-13.** 20 files moved with their `.meta` by the mover script (now handling a
  byte-order mark and resuming after a stop): 13 to `Utility/` (namespace `KitchenDesign.Utility`,
  `using` lines added to 48 files), 6 components and helpers to `Behaviors/`, `ProFitGlobals` to
  `Game/`. `StorageCutoutHelper.CutoutDirection` is `CutoutDirection` in `_projectShared/Enums/`
  (Auto 0, Up 1, Down 2, as serialized in the helper prefab). The `.asmref`s of `Miscellaneous`
  and `Plugins/WebGL` are deleted (their prefabs and the `.jslib` stay: assets). Compiles in the
  editor and as WebGL player scripts.

- **B7.2 done 2026-11-13.** `Const` → `KitchenSizes`, `StollSizes`, `UiColors.Error`,
  `SessionStatus.RememberMeTokenKey` (it already had the key); the two editor dialog results in
  the thumbnail creator are the literals 0 and 1; `Const.Tag` and `DialogResult_Alt` were unused.
  `ProFitUtility` → `AppLinks`, `OccupantDragMath`, `CuboidChecks`, `JsonVectors`,
  `CabinetCornerWedgeMath`, `ProductCategorySizes`. 26 files rewritten by a script, `using` lines
  added to 9. `AssetsMelcher/Scripts` and its `.asmref` are deleted. Compiles in the editor and
  as WebGL player scripts. **Area 7's steps are done; the checks are owed.**

## Open after this area
- `ProFitGlobals` (decision 3) and the editor tools folder (decision 4).
- Every deferred check of areas 5, 6 and 7.
