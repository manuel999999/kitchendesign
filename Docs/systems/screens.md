# Screens

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Screens** (area 5 of the refactor, from 2026-11-12; plan: `.claude/prompts/05-screens.md`).
  All screens are still uGUI (`MelcherUI`); none is on UI Toolkit yet.
  - Small types (B5.1), enums in `_projectShared/Enums/` with their old values: `AccordionEdge`
    (its order is how Unity indexes a `RectOffset` as a `Vector4`: left = x, right = y, top = z,
    bottom = w, so `Accordion` indexes padding vectors with it), `ButtonStyleName` (serialized as
    an int in prefabs and scenes, `m_ButtonStyleName`: append only), `CabinetSizesMode` (the
    cabinet depth and countertop thickness screen opened for a new kitchen or for an existing
    cabinet), `FontFlagOverride`, `WhitelistRowType`, `ToolbarContentVisibility` (`Collapsed`
    hidden, `InForeground` the active toolbar, `InBackground` dimmed behind it),
    `ToolbarContentDuration`, `ToolbarGroupIdType` (what a toolbar group button's id is: a
    manufacturer id, a product group id, an index into the selected catalog's door styles or door
    handle styles, the end panel side (0 left, 1 right); or the button resets the cabinet,
    countertop or floor finish, or toggles Challenger's premium finishes), `DesignerLoadingMode`,
    `RepositionSnapMode` (flags). `AppSkinSO` (one per app: the `ButtonStyleSO`s and colours that
    make its colour theme) and `ButtonStyleSO` are in `Scriptables/` (moved with their `.meta`;
    their create menus keep the default file names `AppSkin` and `ButtonStyle`).
    `FontStyleOverride` (`Serializables/`) lets a button style force a font style flag on or off,
    or leave it as the text has it; its property drawer was commented out in the old code and was
    not carried over. Value types next to their screens: `CabinetSizeOption` (a size and its
    imperial label), `WhitelistRowEntry` (a whitelist row: a product category header or a
    manufacturer toggle), `ActiveMeasure` (a measure and its label pivot, (0.5, 0.5) unless shifted
    to keep labels from overlapping, independent of the screen resolution). No asset file changed
    (scripts bind by GUID, the enums are stored as ints).
  - **Menu screens get their systems** (B5.3c2): `MenuNavigationControl.Initialize` first hands
    every screen it holds only the systems it reads (session, client list, catalog data, server,
    options, rules) plus its prompts, then runs the menu's start-up; `PlatformSetupControl` uses
    the `AppSystems` it is given. On `AppProfileStore` in the menu, on purpose: reads in
    `Awake`/`OnEnable` (the skin in `MainUI`, `HomeUI`, `WelcomeUI` and the views: they run before
    any initializer; they go with the UI Toolkit rewrite), `ManufacturerWhitelistRow` (rows made
    at runtime), `QuickLogInWidget` (editor sign-in helper, not held by the navigation) and
    `PayNowUI`'s message post-processing (the app's display name): the label calls it
    from `OnEnable`. `PayNowUI`, `UnauthorizedDomainUI` and `TrialCountdownUI` get the session
    and the options for the subscription link (B8.4). A handler that a screen's own `Awake`/`OnEnable` triggers must not read a
    handed-in field (found by the cold start on 2026-11-13).
  - **Designer screens get their systems** (B5.4d, 2026-11-13): `DesignerScreensControl.Initialize`
    hands each designer screen the systems it reads next to the kitchen: the toolbar (which passes
    the session on to its highlighted-product content), the navigation screen (which passes
    everything on to its hamburger menu), the building method, brand catalog, shape and sizes
    screens, the white-label client info, the product search, the about-product prompt, the
    set-width prompt and the dev screen (the last two are new serialized references). It
    subscribes to `ProductVisualContentStatus.ContentLoaded` in `Initialize` and unsubscribes in
    `OnDestroy` (it was `OnEnable`/`OnDisable` on the static forward). Four lambdas lost `static`
    because they now read a field.
  - **No designer screen reads a system from `ProFitGlobals`** (B8.3, 2026-11-13). What ran in
    `Awake` on the static now runs in the screen's `Initialize`, in the same frame:
    `BrandCatalogUI` queues its catalog population there (see "The Initialize() chain" in `CLAUDE.md`, step 5,
    for the order), `DesignerDevUI` its product list, and `WhiteLabelClientInfoUI` shows or
    hides the "email me the file" checkbox there (Napoleon in white label mode only). The
    highlighted-product content gets the launch options from the toolbar and sets its product
    link button from both `Awake` and `Initialize`, whichever runs last (the button shows in the
    editor, debug builds and verbose mode). A `ToolbarProductButton` no longer looks its variants
    up: `SetProduct(product, variants)` replaces the `productInfo` setter, and the toolbar and
    the product search pass `CatalogDataStatus.GetProductVariantList(product)`. The sizes screen
    and the set-width prompt hand their `CabinetOtherSizeControl`s the session (the label needs
    the user's units on every slider change). `TrialCountdownUI` gets the session from
    `MenuNavigationControl` and `DesignerScreensControl`. `VersionLabel` reads the server type
    from the profile instead of through the server client. What the
    screens read from `AppProfileStore` is profile, options and skin only
    (`DesignerScreensControl.Awake`'s skybox, `CabinetShapeUI.OnEnable`, `BrandCatalogToggle`,
    `VersionLabel`, the menu's views). The toolbar's highlighted-product content and the
    about-product prompt get the server client for the product's dashboard link (B8.4).
  - **Where the screens' files are** (B5.7, 2026-11-13; moved with their `.meta`, comments kept
    in files that only moved): `_project/Scripts/UI/MenuScreens/` (`KitchenDesign.UI.MenuScreens`:
    the menu's screens, prompts and list rows, `ListController` and `ListControllerSortMode`),
    `UI/DesignerScreens/` (`KitchenDesign.UI.DesignerScreens`: the setup wizard, the toolbar and
    its buttons and contents, the navigation screen and hamburger menu, the designer's prompts,
    toast, loading and recording screens, the measure overlay, the cabinet tickers) and
    `UI/Views/` (`KitchenDesign.UI.Views`: what both scenes share: `Menu` and `BasePromptUI` (our
    bases on `MelcherUI`), `MenuTransition`, the skin views, `Accordion`, `Spinner`, `QuickHint`,
    `InlineMessageBox` and its prompt, `TrialCountdownUI`, `VersionLabel`, the WebGL input
    fixes). The scene controls (`MenuNavigationControl`, `DesignerScreensControl`,
    `PlatformSetupControl`, `SplashVideoControl`, `MemoryFlushControl`, `MenuPreferenceKeys`) are
    in `_project/Scripts/Game/` beside their initializers, namespace `KitchenDesign`.
    `PhoneNumber` (formatting and checking phone numbers) is in `Utility/`, and the TMP input
    validator is `PhoneNumberInputFieldValidatorSO` in `Scriptables/` (a ScriptableObject; its
    asset keeps the name `PhoneNumberInputFieldValidator.asset`). Why the folders aren't `Menu/`
    and `Designer/`: a namespace must not contain a type's name, and `Menu` is a class. No scene,
    prefab or asset names these types as text, so nothing but the scripts changed.
    `AssetsMelcher/Scripts/UI` and `/Scenes` are gone.
  - `MenuPreferenceKeys` (B5.3b): the `PlayerPrefs` key for "always landscape on mobile",
    written as the literal `"MainUI.AlwaysLandscapeOnMobile"` (it was `nameof(MainUI) + …`):
    devices already store it under that name, so it must not follow a class rename.
