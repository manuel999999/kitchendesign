# App systems and boot

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **App-wide systems** (B1.3 of the refactor, 2026-10-22). Plain classes, constructed and owned
  by `AppInitializer`; collaborators come in through the constructor.
  - `AppSystems` (B5.2, `_project/Scripts/Game/`): the profile (and its options), the rules, the
    launch options and the systems below, as one object every `SceneInitializer` gets. Nothing else
    reaches a system: there is no static forward any more (B8.4).
  - `LaunchOptions` (`_project/Scripts/Game/`): debug mode and verbose (editor, debug build,
    `?verbose` URL parameter, `--verbose` argument), the WebGL page's domain and `distributor`
    parameter (white label). In the editor, `AppOptions.simulateWhiteLabelMode` replaces the last
    two with the simulated values.
  - `CatalogDataStatus` (`_project/Scripts/CatalogData/`): the server's data (manufacturers,
    product groups, tags, compatibilities, cabinet section categories, brand catalogs, products
    and cabinets) and its lookups. `Initialize` loads the fallback JSON from
    `Resources/<App>/FallbackJSON` first, so the app works offline, then applies the product
    overrides (products the dashboard can't describe yet; allowed to fail), builds the variant
    lists, probes the server (switching http/https once if the first try fails) and downloads
    fresh brand catalogs, manufacturers, groups, tags and compatibilities; `ExecuteWhenLoaded`
    queues work until then. Products and cabinets are downloaded at sign-in instead
    (`SessionStatus.FinishLogInCoroutine`). Why the variant list starts with the base product's
    id: the toolbar treats the whole family as one list. Why the runtime products are copies of
    the source products: `DesignerScene` rewrites some values per app and building method (bar top
    sizes, padding), and the copies can be rebuilt from the originals; their lookup key includes
    "is a cabinet section", because cabinet sections and products can share a server id.
    `PopulateRuntimeBrandCatalogs` uses a preset catalog when the server's id matches, or a clone
    of the preset with the same cabinet manufacturer; don't call it while a kitchen is open, or the
    kitchen keeps a catalog that is no longer in the list. In the editor the load adds fake
    products for visuals no product uses, so every model can be checked on a cabinet (written to
    `Logs/<App>/issues-unused-product-visuals.txt`). `CabinetDataAvailability`
    (`_projectShared/Enums/`): `None`, `ClientOnly` (Stoll: the dashboard has no cabinet data, so
    it ships as fallback JSON), `Server` (Gemini).
  - `ClientListStatus` (`_project/Scripts/Session/`): the signed-in user's clients, the active
    client and quote (how the menu tells the designer which design to open), and whether the
    active client wants to be contacted (kept from a Napoleon requirement that no longer applies).
  - `SessionStatus` (`_project/Scripts/Session/`): the user (setting it reads that user's kiosk
    preference), profile, sign-in state (`LogInState`, `_projectShared/Enums/`; the menu can be
    used while products still download), kiosk mode (a PIN in front of the client list and the
    profile, kept per device in `PlayerPrefs` under `User.<id>.isKioskModeEnabled`; anyone with
    access to the device's preferences can turn it off), white label mode, Remember Me (the token
    in `PlayerPrefs` under `Token`), log out, the auto-kick that checks the subscription every 20 s
    and kicks expired trials and subscriptions, and the sign-in flow. Uses `ClientListStatus` and
    `CatalogDataStatus` (log out clears both, sign-in fills both); neither uses the session.
  - **`AppProfileStore`** (B8.4, 2026-11-13, `_project/Scripts/Game/`) is the one app-wide static
    store: `Profile`, `Options`, `Skin`. `ProFitGlobals` is deleted. Why this one static stays:
    the profile, its options and the skin are one immutable asset, read by views on dozens of
    prefabs in `Awake`/`OnEnable` (before any initializer), by components made at runtime, by
    the model for default and highlight materials, and by edit-mode tools where nothing is
    constructed. `AppInitializer` binds its serialized profile; with no `AppInitializer` (edit
    mode) the first read loads `Resources/AppProfile`; reset at `SubsystemRegistration`. A
    screen that has an `Initialize` is handed the options instead. **No system (session, server,
    catalog data, client list, rules, content, launch options) is reachable through a static.**
    Editor-only code that needs the running systems (the two debug menus through
    `_projectDebugging/Menus/DebugSystems.TryGet`, and the check snippets under `Tools/`) finds
    the `AppInitializer` and reads its editor-only `SystemsForEditor`. Why on `AppInitializer`
    and not on each `SceneInitializer` (the plan's first idea): it is one object that exists in
    every scene, and the scene initializers stay untouched.
    `ProductIdRemapDatabaseSO`'s editor button (`CaptureCurrentIds`) builds the rules from the
    profile and reads the product groups from the fallback JSON itself; the cabinet parser gets
    the products it has just parsed as "products already loaded". Before, in edit mode, both
    lists came empty from a default catalog-data instance.

- **Boot and scene initializers** (B1.2 of the refactor, 2026-10-22): `AppInitializer`,
  `SceneInitializer` (abstract, `Initialize(AppProfile, IAppRules)`), `MenuSceneInitializer`,
  `DesignerSceneInitializer` (they only keep what they get, until areas move into them),
  `StartAppForEditor` (all in `_project/Scripts/Game/`). The old `Main.RuntimeInit` is gone; its
  F1 "kick the signed-in user" is now a menu item, `Tools/Kitchen Design/Kick signed-in user`
  (F1, play mode only; `_projectDebugging/Menus/SessionDebugMenu.cs`). Order and whys: "The
  Initialize() chain". Checked with `coldstart.sh` from `PlatformSetupScene` and from
  `BootScene` (both the B1.1 reference: PlatformSetup → Menu, Welcome, 13 errors, 4 warnings),
  plus the baseline and menu check.

- **App rules** (B1.1 of the refactor, 2026-10-22): `KitchenDesign.Shared.Interfaces.IAppRules`
  with `ProFitAppRules`, `StollAppRules`, `GeminiAppRules` (`_project/Scripts/AppRules/`),
  chosen by `AppRulesFactory.Create(appOptions.userFilter)`. Every place the old code checked
  which app it was now asks a rule; the table of old check → rule is in
  `.claude/prompts/03-refactor.md`, "Area 1". **No code outside `AppRules/` may test
  `userFilter` to change behaviour.** The three places that still read it (`LogInUI`, `SignUpUI`,
  `QuickLogInWidget`) use it as the app's identity on the server, not as a rule. Code gets
  the rules handed in (`AppSystems.Rules`, or `IKitchen.appRules` in the model). Why rules return `null` for "leave the value alone" (the high bar top
  sizes, fridge padding, bottom inset): only Stoll overrides them, and the caller keeps its own
  value otherwise. Each app has exactly one building method today (ProFit Standard, Stoll
  Sections, Gemini Catalog), but app and building method stay separate axes: what the old code
  tested by building method (the Sections bowing limit, the 0.5" overhang) stays a building-method
  rule, and the per-brand checks (`Const.Gemini.ManufactersIds`) belong on `BrandCatalog` (area 4).
  Why two manufacturer rules (`IncludesStollManufacturer`, `IsManufacturerIdAvailable`): the
  whitelist screen hides only Stoll, the toolbar also hides 137. Checked with
  `Tools/Baseline/unity6/rulescheck.cs` (all three apps) and the Gemini baseline.

- **App and data assets** (B2.3 of the refactor, 2026-10-22): `AppProfileSO`, `AppOptionsSO`
  (+ its `.Editor` partial), `GroupDatabaseSO`, `ProductIdRemapDatabaseSO` in
  `_project/Scripts/Scriptables/` (`KitchenDesign.Scriptables`), moved with their `.meta` so every
  asset still binds; their nested serializable types are `GroupInfo`, `ProductIdRemapEntry` and the
  editor-only `AndroidProjectSettings` in `_project/Scripts/Serializables/` (they bind by field name).
  The assets keep their names: `Resources/AppProfile.asset` (one per project; which
  `AppOptions_<App>` it points at decides the app), `AppOptions_ProFit/Stoll/Gemini`. Two places
  use those names as text and must stay literal: `Resources.Load<AppProfileSO>("AppProfile")` and
  `AppProfileSO.GetAppResourcesName`, which strips `"AppOptions_"` from the options asset's name to
  get the `Resources/<App>` folder (plus `_Test` for the test server). The group databases map a
  server id (manufacturer, product group, brand catalog) to a toolbar sprite, a sort order and an
  optional local display name; the remap database maps product ids to every SKU they have had, so
  designs saved with an old SKU still load.
