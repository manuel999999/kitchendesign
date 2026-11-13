# Utility and editor tools

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Generic helpers** (B1.4 of the refactor, 2026-10-22). `KitchenDesign.Shared.Common.Units`
  (`_projectShared/Common/`): inch/metre/centimetre/foot conversions, the old `Const.Inches2Meters`
  and friends. `_project/Scripts/Utility/` (`KitchenDesign.Utility`, called as
  `Utility.<Class>.<Method>` from old code):
  - `MeasurementFormat`: metres to centimetres or feet/inches text, and parsing typed imperial
    sizes. Why the quote tables: phones and pasted text produce typographic quotes (‘ ’ “ ”), which
    are normalised to ' and " before parsing.
  - `UrlUtility`: `GetDomain` (host of a URL, a scheme added if missing) and `GetBaseDomain`
    (the last two labels, or the IP). Self-tests: `Tools/Kitchen Design/Tests/…`
    (`_projectDebugging/Menus/UrlDebugMenu.cs`).
  - `RuntimePrefabs`: a "runtime prefab" is an inactive copy in `DontDestroyOnLoad` that stands
    in for a prefab asset at runtime; `IsPrefab` treats those and real prefab assets (including the
    prefab stage) alike. `DontDestroyOnLoad` must come after `SetActive(false)`, or some components
    of the copy stay active (the product visuals' editor-only lights did).
  - `RendererUtility`, `RenderQuality` (switch to the quality level that uses a given URP asset),
    `MemoryUtility` (a GC on iOS only, where low-memory devices otherwise run out).
  - `RandomNamesForEditor` (editor only): random names and places to fill the sign-up and client
    forms while testing.
  `KitchenDesign.Tools.Editor` now references `KitchenDesign.Shared` (for `Units`).
  `Const` and `ProFitUtility` were split by owner in area 7 (see "Leftovers" below).

- **Leftovers** (area 7 of the refactor, 2026-11-13; plan: `.claude/prompts/07-leftovers.md`).
  **No runtime code is left in `AssetsMelcher`**; its `.asmref` files are gone. What remains
  outside the house layout: the user's `Cloder` folder and Weaver's `Procedural Assets/Layers.cs` (each with an `.asmref`).
  - **`_project/Editor/Tools/`** (B8.6, 2026-11-13): the whole old `AssetsMelcher/Editor` folder,
    moved in one piece with its `.meta` files: the `KitchenDesign.Tools.Editor` asmdef, the
    scripts (now `namespace KitchenDesign.ToolsEditor`; `AppBuildProcessor` was
    `ProFitBuildProcessor`, `KitchenFinishTools` was `ProFitKitchenFinishTools`,
    `ProductModelFbxImporter` was `ProFitProductModelFbxImporter`; menu paths unchanged) and
    the assets they use (asset icons, `ProFitEditorOptions.asset`, the random name lists, the
    UXML/USS, `ToolScenes/`). Why the assets stay beside the scripts: three are loaded by path
    (`ScreencastKeysOverlay.uxml`, and `RandomNames.txt` / `RandomPlaces.txt` from
    `Utility/RandomNamesForEditor`), and `ButtonStyleEditor` finds
    `TransparencyBackground.png` relative to its own script. `AppBuildProcessor` still has
    `Assets/AssetsMelcher/Resources` and `…/InactiveResources` as literal paths (it moves the
    other apps' resources out for a build); they change when those folders move.
  - `_project/Scripts/Utility/` (`KitchenDesign.Utility`) gained: `ElementIndex<T>` (an element
    with its index, what the layout's enumerations yield), `ComponentsInChildrenEnumerator<T>`,
    `EnumInfo<T>` (cached enum count and values), `ImperialUnit` (feet, inches and fractions),
    `Ref<T>`, `RecyclableTween<T>` (one reused DOTween per animated thing), `DOTweenExtensions`,
    `DOTweenInit`, `DebugDraw`, `CoroutineUtility` (was `ProFitCoroutineUtility`),
    `UnityExtensions` (was `ProFitUnityExtensions`), `SequentiallyAwaitedDelegate` (see
    "Events"), `MelcherWebGL` (the C# side of `AssetsMelcher/Plugins/WebGL/MelcherWebGL.jslib`,
    which stays there as an asset) and `AppLinks` (opens the subscription page, the terms page
    and, in the editor and development builds only so the address can't leak, a product's
    dashboard page; the caller passes the server's host, or the options and the user's type).
  - `Behaviors/`: `HighlightTarget`, `PCMBandaidFix`, `TransformHelper`, and the cutout
    authoring helpers `ComponentCutoutHelper`, `StorageCutoutHelper` (its direction enum is
    `CutoutDirection` in `_projectShared/Enums/`) and `GeminiCutoutPrefabs` (editor-only menu
    items that drop the helper prefabs of `AssetsMelcher/Miscellaneous` into the scene).
  - **`Const` and `ProFitUtility` are gone, split by owner** (B7.2):
    `KitchenModel/KitchenSizes` (the minimum spacing between products: every product's
    cabinet-space cuboid is widened by 5" and its model shifted half of that to stay centred,
    1.5" vertically; a cabinet with a corner loses 6" of occupiable width minus half the product
    padding; a cabinet column is 3" as a whole and drawn as two halves by neighbouring cabinets;
    Sections cabinets are 18" to 60" long; a nudged product stays 2" from the edge; at most 10
    straight cabinets per span in Sections, so a span still fits the PDF photos; an inset toe kick
    sits 1.5" back), `KitchenModel/StollSizes` (products wider than 15" can bow a cabinet much
    longer than they are, so the cabinet may extend at most 8" past them; the fixed smoker
    cabinet keeps 2" below the countertop; `DefaultCabinetDepth`),
    `KitchenModel/OccupantDragMath` (the pointer's position on the drag plane in camera-local
    space, so the camera may move during a drag; the plane is the cabinet's top when the camera
    looks more than 65° away from its front, else its front), `Geometry/CuboidChecks` (a size
    under 1/16" counts as "not entered on the dashboard"; containment with a 1e-8 tolerance for
    cuboids transformed between spaces), `Save/JsonVectors`, `Cabinets/CabinetCornerWedgeMath`,
    `_projectShared/Common/ProductCategorySizes` (finishes have no cut-out sizes, so missing
    ones aren't parse errors), `UI/Views/UiColors.Error`. The Remember Me key is
    `SessionStatus.RememberMeTokenKey`.
