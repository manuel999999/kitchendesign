# The upgrade to Unity 6

Finished history, kept for reference: what the upgrade from Unity 2022.3 broke (track A's
throwaway test) and how each break was fixed in this project. The index is in `CLAUDE.md`.

## Upgrade test findings
- Before the copy (2026-10-20): the old `Packages/` embeds `com.unity.render-pipelines.universal`
  and `…core` at **13.1.8** (the manifest says 14.0.12, but an embedded copy wins) and
  `com.unity.shadergraph` 14.0.11. The URP copy has two local edits: `Runtime/ShadowUtils.cs`
  adds 8 stencil bits to the shadow map (so `KitchenShadowRenderPass` can stencil cutouts into
  shadows), and `Shaders/PostProcessing/UberPost.shader` keeps the source alpha through post
  processing (transparent render textures). Clipping cutouts in the shaders removes the need for the
  first edit. The second still needs its own Unity 6 answer.
- The embedded packages can't be diffed against clean copies: packages.unity.com no longer lists
  URP/core 13.x or Shader Graph 14.0.x, and no local cache has them. The old repo's git history
  stands in. URP and core came in with the initial commit (2022-06-13), the ShadowUtils edit
  presumably with them. The UberPost edit is commit `aaad56dc2d` (2022-07-05). Commit `26ef7d2379`
  ("Upgrade to Unity 2022.3.42f1", 2025-02-26) patched three core files so 13.1.8 compiles on
  2022.3 (`RendererUtils.RendererList` → `UnityEngine.Rendering.RendererList`) and embedded
  Shader Graph 14.0.11 whole. Unmarked edits made before the initial commit can't be ruled out.
- Copy made 2026-10-20 to `/Users/emmanuel/Documents/Unity/GeminiUpgradeTest/` (rsync, 53 s;
  3.2 GB Assets, 313 MB Packages). Logs `pass1-unchanged.log` and `pass2-no-embedded-urp.log`
  are in that folder.
- **Compatibility Mode is gone in 6.6 (checked 2026-10-20 in the 6000.6.6f1 URP 17.6 source).**
  `RenderGraphSettings.enableRenderCompatibilityMode` is hard-coded to `false`, and
  `ScriptableRenderPass` has only `RecordRenderGraph`: no `Execute`, `Configure` or
  `OnCameraSetup` to override. `KitchenForwardRenderPass`, `KitchenShadowRenderPass` and the
  Kawase blur (`KawaseBlur.cs`) can't run as they are. Cutouts by clipping is required in 6.6, not
  optional, and the UI blur needs a Render Graph version or a replacement.
- On first open, 6.6 rewrote the copy's manifest by itself: Addressables 1.22.3 → 2.11.2,
  Cinemachine 2.10.3 → 6.6.0, URP 14.0.12 → 17.6.0. TextMesh Pro `4.0.0-pre.2` stays listed.
- Pass 1 (unchanged, 2026-10-20): stopped after 53 s at script compilation; no assets imported.
  The embedded URP 13.1.8 stayed in use. 46 distinct errors in the first assemblies to compile: 37
  in `ca.melcher.unity.core` (almost all `Object.GetInstanceID()`, now a compile error "Use
  GetEntityId instead", in MEC `Timing.cs`; plus `TreeView`/`TreeViewItem`/`TreeViewState`, now
  generic `TreeView<int>`), 6 in `AssetsThirdParty/SerializeInterfaces` (`GetInstanceID`,
  `InstanceIDToObject`, `HierarchyProperty`), 1 in the embedded core (`ProbeVolumeAsset.cs`,
  `[SerializeField]` on a non-field).
- Pass 2 (embedded URP, core and Shader Graph removed, 2026-10-20): 6.6's own URP, core and Shader
  Graph 17.6.0 resolve. Stopped after 32 s at script compilation: 54 distinct errors. The same
  `ca.melcher.unity.core` and SerializeInterfaces errors, 7 in `Assets/KitWright` (not used here),
  and 2 in ALINE (`AlineURPRenderPassFeature` overrides the removed `Execute`/`Configure`).
- Pass 3 (patched, products thinned, 2026-10-20). The copy keeps only the Danver and Napoleon
  files in `ProductVisuals`, `ProductModels`, `ProductMaterials`, `CabinetSectionModels` and
  `Finishes`, plus what they reference by GUID: 1,039 of 59,629 files, Assets 3.2 GB → 472 MB
  (script: `thin_products.py`, which was in the session scratchpad). Throwaway patches, in the
  order the compiler surfaced them:
  - `GetInstanceID()` → `GetHashCode()` (36 calls). `ObjectSelectorWindow` (SerializeInterfaces)
    stubbed. UniTask tracker window deleted. KitWright deleted.
  - ALINE: its old `Configure`/`Execute` removed; it already has a `RecordRenderGraph` path.
  - `Scene.handle` is now `SceneHandle`, not `int` (19 lines in `ca.melcher.unity.core`:
    `MelcherSceneManager`, `MelcherSceneRequest`, `MelcherPrefabPool`,
    `MelcherGlobalStateMachine`). Patched with `.GetHashCode()`; the real fix is
    `GetRawData()`/`FromRawData(ulong)`.
  - `TreeView.DefaultStyles` → `TreeView<int>` (`MelcherPreBuildChecks`).
    `EditorApplication.hierarchyWindowItemOnGUI` is now an error: use
    `hierarchyWindowItemByEntityIdOnGUI` (`MelcherUIExplodeHierarchy`, hook switched off in the copy).
  - Game code (`Assembly-CSharp`), Cinemachine 3: namespace `Cinemachine` → `Unity.Cinemachine`,
    `CinemachineVirtualCamera` → `CinemachineCamera`, `GetCinemachineComponent<T>()` →
    `GetComponent<T>()`, `m_Lens` → `Lens`, `CinemachineCore.Instance.FindPotentialTargetBrain` →
    static, `brain.IsLive(cam)` → `CinemachineCore.IsLive(cam)`, `CinemachineTargetGroup.Target`
    fields `target/weight/radius` → `Object/Weight/Radius`. Files: `DesignerOrbitCamera`,
    `DesignerOverheadCamera`, `Kitchen.cs`, `KitchenScreenshotManager`. Compiling is not the same
    as working: the save-photo cameras still need retuning, as the old plan says.
  - `KitchenForwardRenderPass`, `KitchenShadowRenderPass`, `KawaseBlur`: old methods emptied, so the
    copy has no cutouts, no cutout shadows and no UI blur. Inside them, `ShadowUtils.GetShadowMapByName`,
    `ConfigureTarget`, `cameraColorTarget` and `supportsLightLayers` are gone too.
  - `ProFitBuildProcessor` (editor) uses `UnityEditor.Android.AndroidPlatformIconKind` with no
    platform guard, so it only compiles while the Android module is installed (it isn't for
    6000.6.6f1). Neutralised in the copy.
  - After that, a clean compile and import: **112.5 s** asset refresh, 128 s for the whole run,
    on the thinned copy. Not comparable to a full catalog.
- Import-time errors in pass 3, from the old project's own code, not from 6.6 rendering:
  `ProFitProductModelFbxImporter.OnPostprocessModel` throws NullReferenceException on every model
  (1,078) because `ProFitEditorOptions.Get()` is null during a first import from an empty Library,
  and it calls `AssetDatabase.ImportAsset` inside a postprocessor, which 6.6 rejects (121
  exceptions). `ProductVisualDatabase.ErrorCheck` logs ~3,900 errors for the visuals the thinning
  removed (expected).
- Pink check (probe `Assets/Editor/UpgradeProbe.cs` in the copy, writes `upgrade-probe.txt`): 174
  materials in the copy's Assets, 17 pink, 0 shaders in Assets with compile errors. All 17 are
  unused: 3 ambientCG materials whose Shader Graph is missing in the old project too, and 14
  BetterLit `TestMats` pointing at `.surfshader` dev sources (they need the full Better Shaders
  tool). Shader variants compile when first drawn, so a render of a real kitchen is still the
  final check.
- Play mode, `DesignerScene` as offline guest, in the editor (2026-10-20, `com.unity.pipeline`
  added to the copy only):
  - **TextMesh Pro fonts break.** The old project pins TextMesh Pro `4.0.0-pre.2` (since commit
    `26ef7d2379`). Its font assets point at a built-in script (`m_Script: {fileID: 19001, guid:
    0000000000000000e000000000000000, type: 0}`) that 6.6 doesn't have, so every font asset
    fails to load and all TMP text has no font: ~1,000 "Please assign a Font Asset" warnings per
    start plus NullReferenceExceptions in `TMPro.MaterialReference`. 10 font assets are affected:
    OpenSans ×5, Signika ×2, FontAwesome 6, LiberationSans SDF and its Fallback. Pointing their
    `m_Script` at 6.6's `TMP_FontAsset` (`guid: 71c1514a6bd24e1e882cebbe1904ce04`, fileID
    11500000) makes them load with their glyph tables and atlases, and the warnings go away.
    The editor kept the broken objects in memory until they were unloaded, so after the fix,
    reload (unload unused assets, reopen the scene) before judging. For the rebuild: retarget or
    regenerate these 10 assets when the fonts come over; regenerating from the source fonts is the
    clean route.
  - **TMP Settings get overwritten.** 6.6 sees the old `TMP Settings.asset` version, opens its
    Essential Resources importer, and the package's default settings replace the project's
    (`assetVersion: 2` added). The one setting lost was `m_defaultFontAssetPath`: `Fonts/` became
    `Fonts & Materials/`. Every icon is a `<font="FontAwesome/Font Awesome 6 Free-Solid-900">`
    tag that `MelcherLocalizer` builds from `MelcherUnicodeDictionary.GetRichTextTagFontPath()`,
    which needs that folder, so all icons drew as boxes. Setting it back to `Fonts/` fixed them.
    Check the TMP settings after any TMP or Unity upgrade.
  - **Cinemachine 3 (2026-10-20).** The upgrader (`CinemachineUpgradeManager`, run through
    reflection to skip its dialog) converted the prefabs and scenes with no errors. The orbit view
    now has `CinemachineHardLockToTarget` + `CinemachineRotationComposer` +
    `CinemachineGroupFraming`, the overhead views `CinemachineRotateWithFollowTarget`. But the
    copy's code had been switched to `CinemachineCamera` before the upgrader ran, so five
    serialized fields lost their camera (`Kitchen.m_PlanView`, `Kitchen.m_PhotoCoverView`,
    `DesignerOrbitCamera.m_OrbitCamera`, `DesignerOverheadCamera.m_BlendCamera`,
    `m_OrthoCamera`). They were relinked by name from the old scene file. **Order for the rebuild:**
    Cinemachine 3 still ships the CM2 classes as deprecated (`Runtime/Deprecated/`), so change only
    `using Cinemachine;` → `using Unity.Cinemachine;` first, run the upgrader, then move field types
    and calls to the CM3 API. `CinemachineGroupComposer.m_GroupFramingSize` →
    `CinemachineGroupFraming.FramingSize`. After this the cameras start without errors; the save
    photos were not checked.
  - **The setup wizard didn't show** until the null entries the thinning left in
    `ProductVisualDatabase.m_SourceEntries` were removed (3,903; `FindProductVisual` throws on a
    null entry). A thinning artefact, not an upgrade problem.
  - With that, the designer runs as offline guest: wizard, Straight layout, Add Part menu, all
    text and icons draw. Adding parts showed an empty list: offline guest uses the ProFit profile,
    which has no fallback data for the two kept brands.
  - **Render check.** 32 kept product visuals (8 Danver and 8 Napoleon cabinet sections, 16
    products; 1,825 renderers) placed in a test scene and rendered with the copy's URP asset: 0
    material slots on a missing or broken shader, and no pink in a close-up. Shaders in use: URP
    Lit, URP Unlit and `Scopic/KitchenCutoutStencil` (the cutout stencil boxes, which draw as
    visible boxes while the Kitchen pass is off). Lighting in the test scene is plain, so this
    says nothing about how products look in the designer.
  - DOTween's `DOTween.dll.meta` and `DOTweenEditor.dll.meta` are below the minimum PluginImporter
    version and get warnings; reimporting or updating DOTween clears it.
  - The emptied Kitchen render passes make URP warn every frame that they have no
    `RecordRenderGraph`. `KitchenRendererFeature` was switched off on both renderer assets in the
    copy to stop the flood. Kawase blur isn't on a renderer asset.
  - Not from the upgrade: ProFit has no fallback `cabinetCategories`/`compatibilities` JSON
    (ProFit is the active app profile in the copy), and the ~3,900 `ProductVisualDatabase` "Null
    entry" errors come from the thinning.
  - The designer starts and the skybox renders (not pink).
- **Track A summary (2026-10-20).** The upgrade alone, apart from the parts the rebuild replaces
  anyway, comes down to:
  1. The custom URP and its three render passes can't run (no Compatibility Mode).
  2. A handful of API breaks in `ca.melcher.unity.core` and the game code: instance and scene
     IDs, `TreeView`, Cinemachine 3.
  3. TMP font assets from the 4.0 preview and the overwritten TMP settings.
  4. The Cinemachine scene and prefab upgrade, in the right order.
  Nothing else in the kept content renders pink. Not measured: a full-catalog import, a real
  kitchen in the designer, save photos, WebGL.

## Upgrade fixes in the copy (step 3, 2026-10-20)
Real fixes, not track A's throwaway patches. Batch passes are logged as `Logs/upgrade-pass*.log`.
- **Instance IDs → `EntityId`.** `EntityId` is 64-bit in 6.6 (`ToULong`/`GetRawData`), and the
  docs list no conversion to `int`. Where an ID is only a runtime key, it is now `EntityId`
  (`MelcherPrefabPool` / `MelcherPrefabPoolLink` pool IDs, the SerializeInterfaces picker,
  `MelcherUIExplodeHierarchy` via `hierarchyWindowItemByEntityIdOnGUI`). Compared with
  `.Equals`, because the docs list no `==` operator for it. Where an API needs an `int` (MEC
  `Timing.cs`'s instance "layer" keys, `RecordingUI`'s DOTween id), it is
  `GetEntityId().GetHashCode()`, documented as a 32-bit hash. A collision would merge two
  objects' MEC singleton keys; accepted as very unlikely, and the old `int` keys could already
  collide with user-chosen layer numbers.
- **Scene handles → `SceneHandle`** throughout `ca.melcher.unity.core` (`MelcherSceneRequest`
  gained a `sceneHandle` field and `FromHandle(SceneHandle)`; the scene manager's handle lists are
  `List<SceneHandle>`). `SceneHandle == SceneHandle` compiles (`MelcherSceneSwitcher` uses it).
- **`TreeView` → `TreeView<int>`** in UniTask's tracker window and `MelcherPreBuildChecks`; their IDs
  are row numbers, not objects. `MelcherHierarchySceneSelect` is all `#if false` and untouched.
- **SerializeInterfaces' `ObjectSelectorWindow`** no longer uses `HierarchyProperty`: assets come from
  `AssetDatabase.FindAssets` with the same `t:Type …` filter (then every object at each path that
  passes the filter), scene objects from walking the loaded scenes' roots.
- **ALINE:** its old `Configure`/`Execute` are compiled out on URP 17; it already had a
  `RecordRenderGraph` path.
- **`KitchenForwardRenderPass`, `KitchenShadowRenderPass`, `KawaseBlur`:** everything from the old
  `Configure` through `Execute` and its helpers is inside `#if !UNITY_6000_0_OR_NEWER`, kept as the
  reference for step 5. Until then: no cutouts, no cutout shadows, no UI blur.
- **Cinemachine:** `-accept-apiupdate` already did the "namespace only" step by itself, by fully
  qualifying every name as `Unity.Cinemachine.…` and blanking the `using` line. The `using` lines
  were put back and the qualifiers removed. Then: `CinemachineCore.FindPotentialTargetBrain` and
  `CinemachineCore.IsLive` (static now), and `Kitchen`'s target group uses `Targets` (a list) with
  `new CinemachineTargetGroup.Target { Object, Weight, Radius }`. **`Target` is a class in CM3**, so
  the old `new Target[n]` would hold nulls; track A's patch had that latent bug. The deprecated CM2
  component types (`CinemachineVirtualCamera`, `CinemachineGroupComposer`, `CinemachineFollowZoom`)
  are still in use until the upgrader has converted the scenes and prefabs.
- **Cinemachine scenes and prefabs (2026-10-20).** The upgrader (its five steps run through
  reflection, skipping the dialog) changed only the 6 scenes, and **still lost the camera
  references even with the CM2 field types in place**: `Kitchen.m_PlanView`,
  `Kitchen.m_PhotoCoverView`, `DesignerOverheadCamera.m_BlendCamera`, `m_OrthoCamera` became
  `fileID: 0` and `DesignerOrbitCamera.m_OrbitCamera` pointed at a stray object. So track A's
  "order" theory was wrong. What worked: record every Cinemachine reference by object path before
  upgrading, upgrade, move the fields to `CinemachineCamera` (`GetCinemachineComponent<
  CinemachineGroupComposer>()` → `GetComponent<CinemachineGroupFraming>()`, `m_GroupFramingSize`
  → `FramingSize`, `m_Lens` → `Lens`), then relink by path through `SerializedObject` and save the
  scene. All six references match the pre-upgrade targets.
- **TMP font assets:** their main object loaded as the built-in TextCore `FontAsset`. Setting
  `m_Script` to `TMP_FontAsset` through `SerializedObject` replaces the object in memory; reload it
  and save it (`SetDirty` + `SaveAssetIfDirty`) to write it. All 10 now load as `TMP_FontAsset`
  with the same glyph counts as the old commit (OpenSans-Bold and the Liberation fallback are
  dynamic and empty in the old project too). `TMP Settings` kept `m_defaultFontAssetPath: Fonts/`;
  6.6 moved the project to uGUI 2.6.0 (TMP included) and dropped the 4.0 preview package. Fixing
  the fonts during a session leaves broken objects in memory: the first play mode afterwards threw
  4,004 TMP exceptions, one of which killed the sign-in coroutine (`ShowHome` layout). After an
  editor restart: none.
- **TMP Essentials imported by the user (2026-10-20).** It replaced everything TMP ships in
  `AssetsThirdParty/TextMesh Pro/` with the uGUI 2.6.0 versions: the shaders (plus new SpaceWarp
  and Mobile-2-Pass variants), `LiberationSans.ttf` and its SDF, fallback and materials,
  `EmojiOne.png`, the default style sheet (the package's own styles only, nothing of the
  project's lost) and `TMP Settings`. Checked against the old commit; only `TMP Settings` lost a
  project value, `m_defaultFontAssetPath` (`Fonts/` → `Fonts & Materials/`), which turned every
  icon into a box. Set back to `Fonts/` through `SerializedObject`; FontAwesome loads through it
  again. The rest is kept as the 6.6-matching versions. The project's own fonts (OpenSans,
  Signika, FontAwesome) live elsewhere and were not touched.
- **The cabinets were drawn only by the old kitchen pass.** The renderers' opaque layer mask
  (1,335) left out `Cabinet` and `NestedCabinet` (and the cutout layers); `KitchenForwardRenderPass`
  drew them with the stencil cutouts. With the feature off, cabinets were invisible (legs and
  shadows only). `Cabinet` and `NestedCabinet` were added to the opaque mask of both renderers
  (34,615), and for the cutout prototype `CutOutInterior` and `NestedCutOutInterior` too (51,127).
  With clipping these stay: nothing draws them separately any more. The window layers
  (`CutOutStencil` 6, `NestedCutOutStencil` 13) were removed with the stencil path on 2026-10-21
  (their names cleared; some camera culling masks still have those bits, which is harmless).
- **Visual database lookup in the editor:** `ProductVisualDatabase` builds its runtime lookup on
  entering play mode only for databases already in memory. After a fresh editor start none was,
  so every visual lookup failed and designs loaded partly (27755: 4 of 14 cabinets, then a
  NullReferenceException in `Cabinet.IsYawSimilar`). `InitializeLoadedDatabases` now loads the
  database through the AssetDatabase first. Builds use `Awake` and were not affected.
- **`ProFitBuildProcessor`:** the Android adaptive-icon block is inside `#if UNITY_ANDROID`. It only
  matters when building for Android, which is also the only time the module must be installed.
- **Ambient light (2026-10-20).** The copy rendered every lit surface darker and warmer than the
  old app (floor dark grey instead of light blue-grey, cabinets grey-brown instead of near-white);
  sky, post-processing and lights were the same. Cause: after `DesignerScene` is loaded from the
  menu in play mode, `RenderSettings.ambientProbe` was **all zero**, so the only light was the warm
  0.68 `MainLight`. Neither project's scene references a lighting data asset
  (`m_LightingDataAsset: {fileID: 0}`; the `DesignerScene/LightingData.asset` beside it is unused);
  2022.3 derived the ambient from the skybox on its own, 6.6 does not for a scene loaded at runtime
  (in edit mode, 6.6's bake-on-load still fills it). Fix: `DesignerScene.Awake` calls
  `DynamicGI.UpdateEnvironment()` right after it sets the app's skybox, which recomputes the probe
  from that skybox (up (0.18, 0.26, 0.34), forward (0.31, 0.45, 0.55)). Runtime rather than a
  baked lighting data asset because the skybox is chosen per app (`AppSkin.designerSceneSkyboxMaterial`),
  and a baked probe would only fit one. Measured on design 27755: the four `normal*` save photos
  went from a mean per-channel difference of 32–35/255 against the old app to 0.0–0.5.
  `KitchenScreenshotManager` swaps to the measurement skybox for the plan photos without
  recomputing; those photos match the old app either way, so that is left as it was. Not yet
  checked in a player build (WebGL and mobile modules aren't installed).
- **Save-photo cover camera and Cinemachine 3 (2026-10-20).** The step 4 numbers that read "cover
  camera yaw 88° instead of 44°" were `MainCamera`'s, and showed a different bug. Three causes,
  three fixes, all in `DesignerScene`:
  1. **Channels.** CM2 kept each brain to its own vcams by layer: the cover camera's culling mask
     leaves out layer 11 (`CinemachineMainCamera`: orbit and overhead vcams), `MainCamera`'s leaves
     out layer 12 (`CinemachinePdfBeautyCamera`: the cover vcam). CM3 picks by Output Channel
     instead, and the upgrader put everything on `Default` with both brains' `ChannelMask` at -1, so
     with equal priorities each brain could take the other's vcam (after the photos `MainCamera`
     sat at the cover view with its lens; the cover camera ignored the layout's
     `saveCoverCameraYaw`). Now the cover vcam is on `Channel01`, the cover brain listens to
     `Channel01` only, `MainCamera`'s brain to `Default` only. This also makes
     `DesignerOverheadCamera`'s `FindPotentialTargetBrain` certain to find `MainCamera`. Any new
     vcam/brain pair needs its own channel.
  2. **Hierarchy.** `SavePhotoCoverCameraComposer` (the vcam) was a child of the camera its brain
     drives. A CM3 vcam writes its result to its own transform, and the brain then turns the parent
     again, so the error compounded. It is now a sibling of `SavePhotoCoverCamera` under
     `SavePhotoCoverCameraRotation`, at the camera's local pose, and `KitchenScreenshotManager`
     finds it through the pivot (`m_SaveCoverCamera.transform.parent`).
  3. **Aim.** CM2's Group Composer aimed at the group's centre plus `TargetOffset` (0, 1, 0) and
     only zoomed. CM3's `CinemachineGroupFraming` (lateral adjustment `ChangeRotation`) re-aims at
     the centre of the group's view-space bounds, which undoes the +1 m (about 4–7° steeper), and
     applies that correction around the camera's own up axis, which rolls the camera (0.6–3.5°).
     No Group Framing setting avoids either; a constant `CenterOffset` can cancel the pitch for one
     kitchen size only. `CameraLevelAimBehavior` (see `Docs/systems/cameras-and-photos.md`) runs after it and re-aims
     with world up. It is on the cover vcam and on `OrbitView`.
  Result on all 8 baseline designs: cover camera position within 0.02 m, yaw within 0.5°, FOV
  within 0.2°, roll 0, pitch within 0.6° (27755: 1.5° steeper). Front and top-down photo cameras
  match exactly. Mean pixel difference to the old photos: `normal*`/`plan*` 0.0–3.3/255, cover
  4–18.5 (a few pixels of shift on high-contrast floors; side by side the framing is the same).
- **`ProFitProductModelFbxImporter`:** on a first import, `ProFitEditorOptions` (found with
  `FindAssets`) or its materials may not be loadable yet. Such models are listed in
  `Library/ProFitPendingModelReimports.txt` and reimported on a later editor tick, or on the next
  editor load if batch mode quit first. The `SaveAndReimport` that stores the material remap in the
  model's `.meta` now runs from `delayCall`, because 6.6 rejects reimports during an import.
