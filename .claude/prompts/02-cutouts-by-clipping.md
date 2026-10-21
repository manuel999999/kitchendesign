# 02 — Cutouts by clipping in the shaders

Agreed with the user on 2026-10-20; not started. Step 5 of `01-copy-and-upgrade.md` and of the old
plan (`/Users/emmanuel/Documents/Unity/Gemini/CLAUDE.md`, "Future plan: Unity 6 rebuild"). The
prototype ends in a go/no-go with the user before the app is wired up.

## Why
- The old cutouts are stencil tricks in `KitchenForwardRenderPass` and `KitchenShadowRenderPass`
  (`KitchenRendererFeature`). They can't run in 6.6: Compatibility Mode is gone and
  `ScriptableRenderPass` only has `RecordRenderGraph`. Their old code is kept under
  `#if !UNITY_6000_0_OR_NEWER` as the reference.
- Custom URP vs custom shader, decided 2026-10-20 (user): **custom shader, stock URP.** A forked URP would
  still need the passes rewritten for Render Graph, would have to be merged again on every Unity
  update, and its main reason (8 stencil bits in the shadow map, the `ShadowUtils.cs` edit) goes
  away when shadows come from the shader's own ShadowCaster pass. The app also ships to WebGL, where
  `clip()` works on WebGL 2 and WebGPU and costs less than ordered stencil drawing.
- No renderer feature is needed for cutouts, so nothing here depends on Render Graph.

## How the old system works (read before building)
- `CabinetCutOut` (`AssetsMelcher/Scripts/CabinetCutOut.cs`, prefab `AssetsMelcher/CutOut.prefab`):
  every cutout is a box with two six-face cubes, `m_StencilRoot` (the windows) and `m_InteriorRoot`
  (the hollow). `SetActiveStencils(front, top, back)` turns on a window face and hides the matching
  interior face. Countertop cutouts hide their bottom interior face so you look down into the
  cabinet cutout below; cabinet shelf cutouts hide theirs to avoid z-fighting the shelf's
  countertop.
- `KitchenForwardRenderPass`, per cabinet, near to far, each with its own rendering-layer bit
  (`m_RenderingLayerIndexStart` = 8, set in `Kitchen.cs:3746` and `ProductVisualContent.cs`):
  stencils → nested stencils → nested interiors → interiors → nested cabinets → cabinets.
  "Nested" is a cutout seen through another cutout, e.g. a burner placed in a cabinet shelf: the
  shelf's countertop (a "nested cabinet") has to be cut by the burner's box.
- The renderers' opaque layer mask currently includes `Cabinet` and `NestedCabinet` as a stopgap
  (see `CLAUDE.md`, "Upgrade fixes in the copy"). Remove that once cabinets are clipped, or
  keep it, whichever the new design needs, and record which.

## Design
1. **One shared include**, e.g. `KitchenCutout.hlsl`: given a world position, return whether it is
   inside any of this object's cutout boxes. Each box is a world-to-box affine matrix
   (`float3x4`, 3 vectors); inside means all of `abs(p) <= 0.5`.
2. **Shader Graph:** a Custom Function node calls it and drives Alpha, with Alpha Clipping on, so
   URP's generated ShadowCaster, DepthOnly and DepthNormals passes clip too (shadows and SSAO
   match without extra work). Check this is true for each pass in 6.6 rather than assuming.
3. **Which shaders:** `AssetsMelcher/Shaders/TriplanarGraph.shadergraph` (656 of the 658 finish
   materials), plus the two others in `AssetsMelcher/Finishes` and whatever the countertops and
   cabinet interiors use. List every shader cabinet and countertop renderers actually use in a
   loaded design before starting; earlier test renders also showed plain URP Lit, which would need
   a Shader Graph or include-based replacement.
4. **Interior:** the existing interior cube, front faces culled, normal opaque material, not clipped
   by its own box. Nested cases: the shelf's interior and countertop are clipped by the burner's
   box. The window faces and `KitchenCutoutStencil` shader are no longer needed.
5. **Independent of how the cabinet mesh is made.** The clip works on any renderer with the
   shader and a slot number, never on sub-meshes, names or the old window faces. Why: the user may
   later generate cabinets from a resizable box instead of authored models (idea raised
   2026-10-20, to be discussed after cutouts), and authored models stay for unusual shapes.
6. **Cost only where needed:** a local keyword so cabinets with no cutouts don't use the clip
   variant (`discard` turns off early-Z on tile-based mobile GPUs).

## Decisions and open points
- **How a renderer finds its boxes: per-draw data, decided 2026-10-20 (user left it to Claude).**
  All boxes go in one global array (`Shader.SetGlobal…`); each renderer has to know which entries
  apply to it without breaking the SRP Batcher (MaterialPropertyBlocks would). Fall back to (b)
  only if per-draw data turns out not to work on WebGL, and ask first.
  - (a) **Renderer Shader User Value** (chosen, refined from "rendering layer bits" while
    building): `MeshRenderer.SetShaderUserValue(uint)` (Unity 6.3+, on `MeshRenderer` and
    `SkinnedMeshRenderer`, not the base `Renderer`) gives each renderer a 32-bit value, read in
    URP shaders as `unity_RendererUserValue` (packed into `unity_RenderingLayer.y`, so per-draw like
    the rendering layer, and also wired for instancing and DOTS). Same idea as rendering layer bits,
    but a purpose-built API that can't clash with light or decal layers. The slot is the low 8 bits.
  - (b) Per-cabinet material instances with a start/count property. Simple, batches fine, but
    every finish material is copied per cabinet (memory, and finish swaps must go through the
    instances).
  - (c) Global array only, no per-renderer data. Simplest, but a box must never reach a
    neighbouring cabinet, which isn't guaranteed.
- **Box cap.** Start with 32 boxes (96 vectors), measured against WebGL 2's fragment uniform limit
  (spec minimum 224 vectors). The largest saved design has 15 cabinets; the old notes mention a
  25-cabinet stencil limit that saved designs can't exercise.
- **Shader Graph vs hand-written HLSL** for any replacement of URP Lit: Shader Graph is the default
  (it survives URP updates); hand-written only if a measured look difference requires it.

## Steps
1. **Prototype** in the copy, one test scene: one cabinet with a grill cutout (front + top) and a
   countertop cutout above it, plus a cabinet shelf with a burner (the nested case). Clip in the
   forward, shadow and depth passes. Compare against the old app's look by eye. Go/no-go with the
   user.

   **Built 2026-10-20. Go from the user the same day ("looks awesome").** The user also agreed to
   bring back the Stoll burner plunge kit from `kitchendesign-held-back/` to test the nested case,
   but asked to wait for their signal before touching the project (they were working in the old
   project). Instead of a hand-built scene: a saved
   design of the signed-in ProFit account (21156 / 72249, an L of plain cabinets) with a Twin Eagles
   36" grill, a 13" double side burner and the Fire Magic double sink added through the Add Part
   event (tools and snippets: `Tools/Cutouts/`, notes `Tools/Cutouts/README.md`).
   - Pieces: `_projectAssets/Shaders/KitchenCutout.hlsl` (`KitchenCutoutAlpha`, slot from
     `unity_RendererUserValue & 0xFF`, 32 slots × 32 boxes as global `float4` arrays, returns
     alpha 1 in the Shader Graph preview); `TriplanarGraph.shadergraph` gained a world Position →
     Custom Function → Alpha chain, Alpha Clip Threshold 0.5 and Alpha Clipping on (added by a script
     editing the graph JSON; the pre-edit file is in git); `KitchenDesign.Cutouts.CutoutClipSet`
     (boxes per slot from a transform + local bounds + open faces, uploads the arrays, sets a
     renderer's slot); `KitchenDesign.Shared.Enums.CutoutFaces`; both renderer assets' opaque mask
     now also include `CutOutInterior` and `NestedCutOutInterior` (34,615 → 51,127).
   - Open faces: a box face is pushed out 1 mm when open and pulled in 1 mm when closed, so a
     cabinet surface lying exactly on a closed face (a top-mounted product flush with the front)
     stays, and one on an open face is surely cut. Open = the old active window faces, plus a
     countertop box's bottom (so the cabinet top under a countertop hole is cut).
   - Verified by A/B renders (boxes applied vs. cleared): the grill and burner are cut at the front
     and top through cabinet and countertop, the sink only at the top with the front rim kept, the
     interiors show through, a top-down view shows all three holes, and the app's own save photos
     (F6 path) come out right. Shadow pass: a plate cut by a box casts a shadow with the matching
     gap. No shader errors in the console.
   - Found: the default finish `Kitchen_Default` (used before a finish is picked, and re-applied by
     the app when products are added or photos are taken) is **plain URP Lit**, so cabinets with it
     don't clip. Step 2 needs a clip-capable shader for it, e.g. a small Lit Shader Graph with the
     same chain. List the other non-triplanar cabinet and countertop materials then too.
   - Nested case, tested later on 2026-10-20 after the user's OK: `ProductVisual__ShelfTop` (ProFit's
     generic "Plunge", SKU `Shelf:Top`) and `ProductVisual_Accessory_Stoll_StainlessSteelBurnerPlungeKit`
     (SKU `SARBKT`) were moved back from `kitchendesign-held-back/` (7 assets, no models;
     `Tools/Cutouts/restore_visuals.py`), added to the database (899 → 901) and to the
     `ProductVisuals` Addressables group (`add_db_entries.cs`). A plunge plus a Twin Eagles side
     burner placed into it (`add_into_shelf.cs`, the shelf's `TryInstantiateOccupantCentered`; the
     `…Anywhere` variant answered `TooFarLeft`) renders right: the plunge is cut from countertop and
     cabinet and open at the front, and the burner's hole goes down through the plunge's floor (its
     nested countertop) into the cabinet, open at the front. All four boxes share the cabinet's slot.
   - Not tested: the baseline photo comparison (the 8 designs belong to
     the Gemini quick sign-in, and the working tree's `AppProfile.asset` is set to another app by
     the user); WebGL and phone cost (modules not installed); the local keyword (every material on
     `TriplanarGraph` now runs the alpha-test variant).
   - The prototype's slot is clamped at 31 cabinets (the old stencil limit was 25); step 2 must
     handle more, or refuse visibly.
2. **Wire it into the app:** `CabinetCutOut` publishes its box (world-to-box matrix) instead of
   stencil faces; `Kitchen` fills the global array and the per-renderer lookup when a design
   loads, a product moves, an inset changes, or a cutout resizes
   (`IOccupantCutOutSizeChangedHandler`). No per-frame sorting.

   **Done 2026-10-20.**
   - `CabinetCutOut.TryGetClipBox` (local bounds of the window cube, measured once and cached,
     plus the open faces); `Melcher.KitchenCutoutClipping` (`AssetsMelcher/Scripts/`, next to
     `Kitchen` until that area is refactored) rebuilds slots and boxes on every
     `RenderPipelineManager.beginContextRendering`, so dragging, resizing, insets and the save
     photos' manual `Camera.Render` calls are always current without tracking every change path;
     `Kitchen` attaches it in `OnEnable`, detaches in `OnDisable`, exposes it as `cutoutClipping`.
     Only cabinets with an active cutout get a slot (up to 31; more are drawn uncut with one
     editor warning, as is a 33rd box). Measured on the test kitchen (1 slot, 8 boxes): 0.031 ms
     and about 840 bytes of garbage per refresh (old-code enumerators), roughly what the old pass
     did every frame. Worth trimming for WebGL later.
   - Shaders: `_projectAssets/Shaders/KitchenLitGraph.shadergraph` (Unity's "Lit Basic" template,
     HDRP target removed, `_Metallic` added as mask R × `_Metallic`, the cutout chain) replaces URP
     Lit on `Kitchen_Default` and `Cabinet_NatureKast_Marble` (A/B render: mean 0.21 and 0.28/255;
     NatureKast's URP "receive shadows off" isn't carried over). `PosterizeGraph` (all six
     selection-highlight materials) got the cutout chain; it allows material overrides, so its
     materials needed `_AlphaClip` 1, `_ALPHATEST_ON` and queue 2450 as well. Without that, a
     selected cabinet lost its holes.
   - The last three, `Product_Visual_Napoleon_PowderedMetal` (URP Lit with a normal map),
     `Cabinet_Stoll_Standard_White` and `Product_Metal_Napoleon_MatteBlack` (Simple Lit), were
     **switched in place on 2026-10-21** (user: "do what you must"), for cabinets and products
     alike. Matte Black's smoothness went 0.978 → 0 (`sweep_lit.cs`). Measured against the
     originals: Powdered Metal 0.01/255, Stoll White 0.69, Matte Black 2.13 (max 57). The first
     measurement (2.62 / 0.87 / 3.04) had used the same smoothness and the older twin. Baseline:
     `resave.json` identical, photos within 0.09/255 of the run before (kept in
     `GeminiBaseline/unity6-after-stencil-removal-2026-10-21/`). A test box through a Napoleon
     cabinet in 32569 cut both materials cleanly. Why in place, and the details: `CLAUDE.md`,
     "Key systems: Cutouts".
   - The first baseline run after the switch (2026-10-20) showed black cabinet frames on the
     Napoleon designs: the switch had put the template's `default_masks` texture on
     `Kitchen_Default`'s mask map. Fixed by clearing the graph's mask default and the materials'
     mask slot; the app's cover photo then matched URP Lit by eye (see `CLAUDE.md`, Gotchas).
   - Verified in play mode without the prototype driver: grill, sink, plunge with a nested
     burner, default finish, selection highlight, save photos; console clean.
3. **Remove the old path:** `KitchenRendererFeature`, the two passes, the stencil layers and
   `KitchenCutoutStencil`. The opaque mask keeps `Cabinet`, `NestedCabinet` and the two interior
   layers (no pass draws them separately any more). Keep the old code until step 4 passes.

   **Done 2026-10-21.** What went and what stayed is listed in `CLAUDE.md` ("Key systems:
   Cutouts"); in short, the feature, both passes, both stencil-path shaders and their materials,
   the two window layer names and the code that only served them. The window cube stays as the
   clip box's data, without MeshRenderers. Checked with `runall.sh` (Gemini profile swapped in,
   then the user's `AppProfile.asset` and every `FallbackJSON` folder restored from a hashed
   backup; the sign-in had rewritten `Gemini/FallbackJSON/products.json`): `resave.json` identical
   in all 8 designs; every save photo 0.00/255 from the run before the removal (kept in
   `GeminiBaseline/unity6-after-cutouts-2026-10-21/`); `renderer_count` lower by exactly the
   active window faces (27763 and 27764 −4, 27740 −6); the only other change is 32569's live
   orbit starting at 45° instead of 0° (the open orbit issue, photos unaffected). No console
   errors during the run. `MenuScene` had unsaved changes in the editor, so the run used a copy of
   `runall.sh` without its `open_scene` line.
4. **Check against the baseline (done 2026-10-21 in the editor; WebGL and phone not yet):**
   with the committed (Gemini) `AppProfile.asset` swapped in for the run and the user's version
   and all fallback files restored afterwards. All 8 designs: `resave.json` identical, the only
   measurement differences are cameras (the open orbit-view issue). Photos against the old app,
   before → after cutouts: the grill designs' top-down photos 1.80 → 0.11/255; covers within
   0.1/255 of the pre-cutout run (the products sit in their holes like in the old app); every
   other photo within 0.1/255 (render-order noise from the alpha-test queue). The previous
   Unity 6 run is kept in `GeminiBaseline/unity6-before-cutouts-2026-10-20/`. Two runs before
   that showed black cabinet frames on the Napoleon design: the first right after the
   `default_masks` slip, the second right after the shader reimport (a first-render artefact;
   the third run and every manual photo were grey). `runall.sh` now waits for
   `login=Finished clients>0`; it used to accept `NotStarted` and once skipped 27755.

   Original step text: **Check against the baseline:** `Tools/Baseline/unity6/runall.sh` on the 8 designs.
   `resave.json` and measurements must stay identical; the save photos should now match the old
   ones where they differed only by missing cutouts. Then a WebGL build and a phone: frame time and
   any uniform or compile errors (install the 6000.6.6f1 WebGL module first; ask the user).
5. Record the result and decisions in `CLAUDE.md` ("Key systems: Cutouts", "Open questions":
   the custom-URP question is answered except for the `UberPost` alpha edit, which needs its own
   check; URP may have an "Alpha Processing" setting for it).

## Not part of this
- The Kawase UI blur (needs its own Render Graph feature or a replacement).
- The `UberPost` transparent-render-texture alpha edit.
