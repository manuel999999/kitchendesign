# Products and visuals

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Products and visuals** (area 3 of the refactor, 2026-10-22; plan: `.claude/prompts/03-refactor.md`,
  "Area 3").
  - Enums (B3.1) in `_projectShared/Enums/`, values as before: `ProductCategory` (the server's
    product type, mapped from its strings by `CatalogJson`; it decides the menu a product is in),
    `ProductCategorySpecific` (a finer, fixed category set on the visual, so runtime visuals can
    be adjusted before any product that uses them is known; fridges, smokers and ice boxes have
    their own values here), `ProductAnchor`, `FlooringType` (which floor mesh a floor finish
    uses), `CabinetModelType` (procedural 27-slice cube or authored model),
    `CabinetSectionFootingType` (authored Catalog cabinets: on the ground, on visible legs,
    floating, or floating with the air below occupiable), `CabinetCutOutMode` (was nested in
    `ProductVisualInfo`): `StollFixedSmokerCabinet` keeps the front countertop as a bridge in
    front of the smoker and lowers the cabinet cut-out (sturdier in the real cabinet), `NoCutOuts`
    is Challenger's ceramic cabinets with an open shelf under the countertop for a smoker,
    `NoSmokerCutOuts` the same for smokers only. All of these are stored in assets as
    `MelcherStableEnum` (by name), so renaming a value breaks assets while reordering doesn't;
    keep both stable anyway. Their pure helpers (`IsOnTop`, `IsFinish`, `IsBacking`,
    `ToProductCategory`, …) are in `_projectShared/Common/`.
  - `ProductVisualDatabaseEntry.uxml` names `ProductAnchor` as text
    (`KitchenDesign.Shared.Enums.ProductAnchor, KitchenDesign.Shared`).
  - The data (B3.2). `ProductInfo` (`_project/Scripts/Server/Data/`, `KitchenDesign.Server`):
    the server's product record. `id` is the server's insert id and the reliable key; `sku` is
    human-readable, was the key in old saves and can change on the dashboard (hence
    `ProductIdRemapDatabaseSO`). `baseProductId` makes a product a variant (the toolbar shows a
    family as one button). `productSizeInMeters` is used by backings, preset-size authored
    cabinets and products with `doLayoutUsingProductSize` (a few special cases: Fire Magic's
    pizza oven, Blaze's griddle), which occupy their product size instead of their cut-out.
    `doesExistInServerDashboard` is false for the editor's fake products. The copy constructor
    makes the runtime copies, and an editor-only static constructor asserts the field count (26)
    so a new field can't be left out of it.
  - The visual assets, `_project/Scripts/Scriptables/` (`KitchenDesign.Scriptables`):
    `ProductVisualSO` (an info plus the address of its content prefab), `ProductVisualInfoSO`
    (the data that stays loaded: id, thumbnail, anchor, section flags), `ProductVisualDatabaseSO`
    (the source list; it makes a runtime clone of every visual and info, in `Awake` in builds and
    on entering play mode in the editor, so building methods and brand catalogs can change the
    clones and still read the originals). The asset files keep their old names
    (`ProductVisual_<id>^.asset` for the visual, without `^` for the info and the content prefab);
    the door lookup builds names from them (`…@<door style>^`), so those names are data. The
    database's own note: editing prefabs from editor scripts can leave loaded prefab references
    stale (reporting null) until reloaded, so Domain Reload stays on in play mode.
    `ProductVisualLookups.FindById` (`_project/Scripts/Products/`) finds a visual in a given list.
  - **Content loading (B3.3)**, `_project/Scripts/Products/` (`KitchenDesign.Products`).
    `ProductVisualContentStatus` (plain class, constructed by `AppInitializer`) loads a visual's content prefab through Addressables
    once per address, keeps the handle, makes a runtime copy of the prefab
    (`LoadedProductContent`: source and runtime prefab), and raises `ContentLoaded` once per
    loaded content. `LoadContent` returns the runtime copy for a runtime visual and the source
    prefab for a source visual. `Purge` releases every handle; it runs when the designer scene
    goes (`DesignerSceneInitializer.OnDestroy`) and when `Kitchen` builds a new layout. The
    runtime copies (in `DontDestroyOnLoad`) are not destroyed by a purge, as before. Why
    `ContentLoaded` is a C# event and not an `Event<Domain>`: its payload is the content prefab, a
    `KitchenDesign` type that can't move to Shared. Checked with
    `Tools/Baseline/unity6/contentprobe.sh` (27755: 18 contents loaded, 18 events, 0 after
    returning to the menu; 32569: 8).
  - `RuntimeProductOverrides` (static): `RebuildRuntimeProducts` (on a building method change:
    new runtime copies of every source product, then the app rules, or in non-Catalog methods the
    visual's inset reset to its source value; Catalog without app overrides changes nothing),
    `ApplySmokerCutOutHeight` (when content loads, in Catalog or when the app rules ask: a smoker
    that plunges from the top gets a 14" cut-out, its model moved to match),
    `ApplyBrandCatalogPadding` (on a brand catalog change: products' side padding per catalog;
    Challenger's fridges ¼", its other products except smokers half the Challenger occupant
    padding; fridges at least 1/128" against z-fighting next to open shelves; shelves 0; heated
    and burner products 2¼" for Agora (smokers 0) and 3" for Toppin, Endura, WeatherStrong,
    WeatherStrong Select and NatureKast). `DesignerScene` keeps its event subscriptions and calls
    these, so they run in the same order among the other listeners as before. The brand-specific
    numbers are brand rules and move onto `BrandCatalog` in area 4.
  - Addressables: `AssetAddress<T>` and `AssetAddressData` (`_project/Scripts/Serializables/`) hold
    an address and, in the editor, the asset; unlike `AssetReferenceT` they own nothing, so
    passing one around never raises the question of who releases the load. Addresses are asset
    paths. `IAssetAddress` (for the drawer) and `AddressablesForEditor` (editor-only, find or
    make an entry, create groups packed separately) are in `_project/Scripts/AddressableContent/`
    (`KitchenDesign.AddressableContent`: a namespace ending in `.Addressables` would hide Unity's
    `Addressables` class in our code).
  - Editor tools (B3.4), `KitchenDesign.Products.Editor`: the database window
    (`ProductVisualDatabaseManager` and its two UXML, which it gets through its script's default
    references, so they move with the `.meta`), the database inspector, the user's Cloder tools for
    it (`ProductVisualDatabaseSetsManager`, `…StatesManager`, `…EntryFile`; their `EditorPrefs`
    key keeps its old `Melcher.` text, or the set-aside list is lost), the visual tools (dump,
    mismatched models, reserialize, insulated-jacket fix, `ProductVisualEditorHelp`), the
    `AssetAddress` drawer, `AddressableGroupMenuCommand` (was `ProFitAddressableGroupMenuCommand`;
    menu paths unchanged), `FilteredObjectSelector`, and `ProductEditorOptionsSO` (was
    `ProFitEditorOptions`; the asset `_project/Editor/Tools/ProFitEditorOptions.asset` is found by
    type). Comments in these files stay: they were moved, not refactored. **The FBX import
    postprocessor is `ProductModelFbxImporter` in `KitchenDesign.Tools.Editor`** (was
    `ProFitProductModelFbxImporter`). Unity counts the model postprocessors' type names among
    each model's import dependencies. Measured in B8.6 (2026-11-13): moving its file to another
    folder (same GUID, class, namespace and assembly) reimported nothing; renaming the class and
    its namespace reimported every model once (1,159 actual imports, 20 s on the thinned copy),
    with no `.fbx.meta` changed. Rename or re-namespace it again only when that reimport is
    acceptable on the full catalog.
  - Checked with `Tools/Baseline/unity6/visualscensus.cs` (edit mode): every source visual's
    content address and every public value of its info, written to
    `GeminiBaseline/visualscensus-latest.txt` with a hash; it finds the types by name, so it
    works across the renames. Reference before area 3: 901 entries, hash `D72B3E9CB18746DF`
    (output in Unicode NFC, see Gotchas).
