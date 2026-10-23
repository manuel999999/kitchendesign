# 05 — The screens (area 5 of the refactor)

Planned 2026-10-23. Area 5 of `03-refactor.md` (read its "The recipe for each area", "Checks" and
the area 1–3 sections, then `04-kitchen-model.md`, first). The user left the design and the details
to Claude: write this plan, then build it step by step like areas 1–4, each step checked and
committed on its own, no behaviour change.

## The old repo first (checked 2026-10-23)
Every commit in the old repo since the copy's source commit `7700e16a8c` that touches
`Scripts/UI/`, `Scripts/Scenes/`, `EventBridge.cs` or `ProFitGlobals.cs` was already in the
2026-10-22 audit (`03-refactor.md`, "Old-repo code audit") and the area 4 check: all in the copy
under other hashes, except the rear wall (`77f53840ee`, `DesignerToolbarUI` +87 lines), which
still waits for the user. The only commits since the area 4 check (2026-10-21 to 2026-10-22: AMG
products, the Wolf dry storage inset, the user's notes) change no `.cs` file.

**Nothing to bring over before the split.** Where the rear wall's `DesignerToolbarUI` part lands
after this area is added to the map at the end of `04-kitchen-model.md` when B5.4 is done.

## Today (read 2026-10-23)
- **85 files in `AssetsMelcher/Scripts/UI/` (17,135 lines)** and the five scene scripts in
  `Scripts/Scenes/` (1,941 lines): `SplashScene`, `PlatformSetupScene`, `MenuScene`,
  `DesignerScene`, `IntermediateDesignerToMenuScene`. `DesignerToolbarUI` alone is 2,525 lines.
- **Screens are `MelcherUI`s** (core package; `Menu` and `BasePromptUI` are our own bases on top).
  `Show`/`Hide` enable the screen's `Canvas`, `CanvasGroup` and raycaster; the GameObject stays
  active. So every screen's `Awake` and `OnEnable` run at scene load, before
  `SceneInitializer.Initialize` (called from `sceneLoaded`, before `Start`).
- **The scene scripts are `MelcherStateMachineState`s**, the state machine's only users. The core
  package's `MelcherGlobalStateMachine` pushes the first state of each fully loaded scene and calls
  its `OnStateAdded` (the scene's start-up) from `MelcherSceneManager.sceneLoaded`, before our
  `AppInitializer` hands the scene its systems. It also forwards `Update` and `LateUpdate` to the
  state, from a `[DefaultExecutionOrder(-10)]` component: `DesignerScene` uses them for the 3D
  selection click and `Kitchen.LateTick`.
- **`MenuScene` is the menu's navigator.** It holds every menu screen (fields filled in the editor
  by `MelcherFindComponent`), the history stack and the "discard pending changes?" prompt, the
  screen orientation, and handles the `EventBridge.Menu` commands. `DesignerScene` holds the
  setup-wizard screens, the toolbar, the navigation bar and the loading screen; it filters the
  cabinets offered when swapping one, picks the loading image, applies the product overrides on
  three events (area 3), and does the selection click.
- **`EventBridge.Menu`** (about 40 delegates): commands, each with one handler (`MenuScene`, or a
  prompt screen for the four prompts that answer through a callback). Raised by screens and by app
  systems: `SessionStatus` (spinners, home, welcome, log in, trial countdown, kicked prompt),
  `CatalogRequests` (22 status messages) and `ClientRequests` (2).
- **`EventBridge.Designer`** (about 65 delegates), four kinds:
  - commands to the kitchen, one handler each (`Kitchen` or its parts): set building method, brand
    catalog, layout, template, countertop type, depth, thickness, overhang; create kitchen; try
    add / clone / destroy a product, section or backing; reposition, reorder, center; toggle toe
    kicks; finish changed; highlight an object; "give me the layout";
  - notifications about the kitchen with several listeners, many awaited
    (`SequentiallyAwaitedDelegate`) and carrying model types (`Cabinet`, `Occupant`,
    `CabinetLayout`, the highlighted `GameObject`): restart (6 listeners), building method and
    brand catalog changed, kitchen ready, layout created, highlight changed (4), cabinet added /
    destroyed / reordered / resized (4–5), occupant added / instantiated / removed / resized /
    nudged / dragged, toe kick type, door handle style, cabinet swap start / cancel;
  - requests to the designer's screens: show a wizard screen, a toast, a notification, a confirm
    or save prompt, the loading screen and its message, the about-product prompt, the set-width
    prompt, product search;
  - from app systems: show/hide the trial countdown, load the menu scene.
- **Hubs in the screens and scene scripts:** `ProFitGlobals` 330 uses in 50 files (`userProfile`
  62, `appOptions` 34, `userClients` 34, `user` 31, `server` 28, `catalogData` 22, …);
  `Kitchen.instance` 27 uses in 9 files (`DesignerToolbarUI` 13); building-method checks 39
  (`DesignerToolbarUI` 20, `DesignerScene` 4).
- **Eight views apply the app's skin** (`ButtonView`, `ToggleView`, `InputFieldView`,
  `FooterToggleView`, `BackgroundView`, `InlineMessageBox`, `MainUI`, `HomeUI`): they read
  `appOptions.appSkin` in `Awake`/`OnEnable`, also on list rows instantiated at runtime.
- **Types not filed by what they are:** `AppSkin` and `ButtonStyle` (ScriptableObjects without
  `SO`), `ColorTheme`, `FontStyleOverride` (a serializable struct with a nested enum and a property
  drawer in the same file); nested enums `Accordion.Edge`, `AppSkin.ButtonStyleName`,
  `CabinetOtherSizesUI.Mode`, `FontStyleOverride.FlagOverride`, `ManufacturerWhitelistUI.RowType`,
  `ToolbarContent.Visibility` / `Duration`, `ToolbarGroupButton.IdType`,
  `EventBridge.Designer.LoadingUIMode` / `RepositionSnapMode`; nested structs
  `CabinetOtherSizeControl.Option`, `ManufacturerWhitelistUI.RowEntry`,
  `MeasureOverlayUI.ActiveMeasure`; `MenuScene.PlayerPref`. Helpers only the screens use:
  `ListControllerSortMode`, `PhoneNumber`, `PhoneNumberInputFieldValidator`.
- **The checks call `EventBridge` and `Kitchen.instance` directly** (`menu.tmpl`,
  `editstep.tmpl`, `load.tmpl`, `tomenu.cs`; `kstatus.cs`, `measure.tmpl`, `editsig.tmpl`,
  `photos.cs`), so they test the code behind a button, not the button. Area 5 changes exactly the
  wiring between the two.

## Decisions
1. **Screens stay uGUI on `MelcherUI`** (project override "uGUI first"). The UI Toolkit rewrite
   replaces them screen by screen later, so no new code is built on `MelcherUI` (03, decision 1)
   and its base classes are not reworked. **Their names keep the `…UI` suffix:** it marks a uGUI
   screen still to be rewritten, and a rewritten screen becomes a `<Name>Control`
   (`BaseDocumentControl`, R-UI-1). Names starting with `ProFit` drop it, as before.
2. **Scene start-up moves into the scene initializers, and the scene scripts stop being state
   machine states.** Every build-list scene gets a `SceneInitializer` (`PlatformSetup`, `Splash`
   and `IntermediateDesignerToMenu` too). What each script does that isn't start-up becomes a
   component named for its job, renamed in place (`git mv`, same GUID, so the scene keeps its
   serialized references): `MenuScene` → `MenuNavigationControl` (screens, history, prompts),
   `DesignerScene` → `DesignerScreensControl` (wizard screens, loading screen, cabinet swap),
   with the selection click in its own component. The `OnStateAdded` code runs from
   `Initialize` instead: the same frame and the same `sceneLoaded` call, a little later. The
   `Update`/`LateUpdate` work keeps `[DefaultExecutionOrder(-10)]`, so it still runs before
   ordinary scripts.
3. **How each `EventBridge` delegate is replaced** (one rule per kind):
   - **A command with one handler becomes a method call on that handler.** Screens get the handler
     through `Initialize`; model code reaches the kitchen through `IKitchen`, as it already does.
   - **A notification about the kitchen becomes an instance event on `KitchenEvents`**, a plain
     class `Kitchen` owns (`SequentiallyAwaitedDelegate` where it is awaited today, a C# event
     otherwise). Why not Doge: its events can't be awaited and can't carry `Cabinet` or
     `Occupant` (`KitchenDesign.Events` sees only Shared). Listeners that exist at scene load are
     subscribed by `DesignerSceneInitializer` **in today's order** (measured in B5.0), so the
     order that several handlers depend on is written down in one place (the orbit start-yaw race
     is one of them). Listeners created later (layouts, tickers, door handles) subscribe when they
     are created, as today.
   - **A request to the current scene's screens with a plain payload becomes a Doge event**:
     `EventMenu` (navigation: show home, push client info, …), `EventDesigner` (toast,
     notification, loading screen, wizard screens, …), `EventSession` (from the app systems:
     spinners, status messages, trial countdown, kicked user). Payloads are `<Thing>Info` structs
     in `KitchenDesign.Shared` (R-ASM-2). The scene's control listens, subscribing in `Initialize`
     and unsubscribing in `OnDestroy`. Why events and not calls: the app systems outlive the scene
     and can't hold its screens, and screens calling the navigator that holds them would be a
     cycle (R-ARC-3). Doge drops a `null` payload (CLAUDE.md, Doge API), so no payload is a bare
     `string` that can be `null`.
   - **A prompt that answers its caller through a callback** (edit a profile field, multiple
     choice, kiosk PIN, sort mode, confirm, save, set a cabinet's width) **is handed to the screens
     that open it** through `Initialize`, and they call it. These payloads (TMP content types,
     callbacks with model types) can't be Shared types, and there is one caller per use.
4. **App systems reach screens through one `AppSystems` object** that `AppInitializer` builds and
   hands to every scene initializer (`Initialize(AppSystems)` replaces today's three arguments):
   profile, options, rules, launch options, server, catalog data, client list, session, product
   content. The scene initializer hands each screen only what it uses. Old code keeps the
   `ProFitGlobals` forwards until its own area.
5. **Building-method checks in the screens become rules** on `IBuildingMethodRules`, written as the
   expression they replace (the B4.7 pattern); `buildingrulescheck.cs` gains them.
6. **What stays on `ProFitGlobals` after this area,** each listed in CLAUDE.md with its reason:
   the eight skin views (they run in `Awake`/`OnEnable` on prefabs and runtime list rows, before
   anything can hand them a reference, and go with the UI Toolkit rewrite); edit-mode tools and
   `OnValidate`; `Kitchen`'s and the model's own reads that no initializer path reaches yet; area 6
   (the photo manager, the thumbnail creator, the cameras).
7. **Comments:** code moved into a new class drops its comments and the whys go into CLAUDE.md;
   files that are only moved keep theirs (as in area 4).
8. **No behaviour change, bugs included.** What is found on the way is listed for the user, not
   fixed: the orbit start-yaw race (handover), the white-label kiosk key and the
   `simulateWhiteLabelMode` reset (03, "Found while reading"), and anything new.
9. **Names:** a namespace never contains a type's name (`Menu` is a class, so the folders are
   `MenuScreens/` and `DesignerScreens/`). Screens go to `_project/Scripts/UI/`
   (`KitchenDesign.UI.…`); no code here writes an unqualified `UI.X`, so the namespace can't hide
   `UnityEngine.UI` (checked 2026-10-23).

## Steps
Each step: compile (editor, and macOS player scripts when `#if` code moves), the checks below,
CLAUDE.md updated, one commit. A step that turns out too large to check as one is split, and the
split is written here.

**B5.0 — A screen probe and a listener-order dump.** Area 5 changes the wiring between buttons and
the code behind them, which no current check exercises.
- `screenprobe.sh`: plays from `MenuScene` and drives the app **through the screens' own
  controls**: a pointer click sent through the `EventSystem` handler interfaces (`ExecuteEvents`)
  to the button or toggle found by its path, so a button whose handler lost its wiring fails the
  same way it would for a user. Menu: welcome, log in, quick sign-in, the footer tabs, a client,
  its quotes, client info and back, quote info and close, profile, manufacturer whitelist and
  back, the log-out prompt and cancel, open a design. Designer: wait for the kitchen, toolbar
  categories, add a product, the highlighted-product buttons, the hamburger menu, restart into
  the setup wizard (brand catalog, shape, template, sizes, create), back to the menu (no save).
  After each step it waits until things settle and writes a snapshot: the active scene, every
  visible screen by path, the visible toast or notification text, and the kitchen's signature
  (`editsig`). Read-only on the server: it never saves, creates, edits or deletes.
- `listenerorder.cs`: for every multicast delegate of `EventBridge` (and, from B5.4, of
  `KitchenEvents` and the Doge events), the subscribers in invocation order (`Type.Method`), in the
  menu and in the designer after a design is open. Recorded now; every later step must list the
  same subscribers in the same order under the new names (a rename table in the script).
- The probe snippets find what they need by type (`FindFirstObjectByType<Kitchen>()`), not through
  `Kitchen.instance` or `EventBridge`, so they keep working across the area. The existing
  checks are updated as the API they call changes (as the area 3 and 4 snippets were).
- Two runs on unchanged code must be identical before the probe is used. Reference recorded on
  the commit before B5.1.

**B5.1 — Small types by what they are.** The nested enums to `_projectShared/Enums/` with explicit
values equal to today's (several are serialized as ints in scenes and prefabs): `AccordionEdge`,
`ButtonStyleName`, `CabinetSizesMode` (`CabinetOtherSizesUI.Mode`), `FontFlagOverride`,
`WhitelistRowType`, `ToolbarContentVisibility`, `ToolbarContentDuration`, `ToolbarGroupIdType`,
`DesignerLoadingMode` (`LoadingUIMode`), `RepositionSnapMode`, `ListControllerSortMode`.
`AppSkin` → `AppSkinSO`, `ButtonStyle` → `ButtonStyleSO` (`Scriptables/`); `FontStyleOverride`
(`Serializables/`) with its drawer in an editor assembly; the nested structs into their own files.
Check: as B4.1/B4.2 (no asset file changes; a census of the skin and button style assets).

**B5.2 — `AppSystems` and the app systems' requests as events.** `AppSystems` and
`SceneInitializer.Initialize(AppSystems)`. `EventSession` (Doge) for what `SessionStatus`,
`CatalogRequests`, `ClientRequests` and `PlatformSetupScene` ask of whichever scene is loaded
(spinners, status message, home, welcome, log in, trial countdown, kicked user); the menu's and
designer's scene scripts listen instead of subscribing to those `EventBridge` delegates.
`EventBridge.Menu`/`Designer` keep their other delegates for B5.3–B5.5.

**B5.3 — The menu.** `SplashScene`, `PlatformSetupScene`, `IntermediateDesignerToMenuScene` and
`MenuScene` start-up into their scene initializers; `MenuScene` → `MenuNavigationControl`.
`EventBridge.Menu` → `EventMenu` requests and prompts handed over (decision 3). Every menu screen
gets an `Initialize` with what it uses and stops reading `ProFitGlobals` (decision 6 aside).
`EventBridge.Menu` is deleted.

**B5.4 — The designer's start-up and screens get their references.** `DesignerScene` start-up into
`DesignerSceneInitializer`, which initializes `Kitchen` with the app systems and then the designer
screens with the kitchen and what else they use; `DesignerScene` → `DesignerScreensControl` and the
selection component. **`Kitchen.instance` is deleted** (the probes find the kitchen by type).

**B5.5 — `EventBridge.Designer` goes.** `KitchenEvents` (notifications), kitchen methods
(commands), `EventDesigner` (screen requests), prompts handed over, per decision 3; the model,
the screens, the cameras and the photo manager move to them. `EventBridge.cs` is deleted; the
listener-order dump checks every multicast list against B5.0's.

**B5.6 — The screens' building-method checks become rules** (decision 5).

**B5.7 — File the screens.** `git mv` with `.meta` into `_project/Scripts/UI/`: `MenuScreens/`,
`DesignerScreens/` (with the toolbar), `Views/` (the shared views and widgets); the scene
controls next to their initializers in `Game/`; the helpers by what they are. Namespaces follow the
folders. One type per file.

## Checks for every step
As area 4, plus the new probe:
- `rulescheck.cs` (60/0), `buildingrulescheck.cs` and `brandrulescheck.cs` (0 mismatches),
  `coldstart.sh` from `PlatformSetupScene` and from `BootScene` (13 errors, 4 warnings),
  `runall.sh` then `compare_runs.py` and `photodiff.sh` against the previous step's run, the
  fallback JSON a sign-in writes byte-identical to the previous run's, `menucheck.sh` (21 / 21 /
  1,268), `editprobe.sh` then `compare_probe.py` against the previous probe, and from B5.0
  `screenprobe.sh` and `listenerorder.cs` identical to the previous step's.
- Around every play-mode run, the user's `AppProfile.asset` and every `Resources/*/FallbackJSON`
  folder are backed up (hashes kept) and restored afterwards from the backup, never from git; the
  backup is kept until the step is committed. Those files are never staged.
- Not covered by any check, so listed for the user to try by hand when a step changes them: the
  screens that write to the server (sign up, create / edit / delete client and quote, edit
  profile, pay, delete account, save a design), white-label mode, kiosk mode, Remember Me, the
  other two apps' screens.
