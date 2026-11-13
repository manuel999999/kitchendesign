# Cutouts

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Cutouts (wired into the app 2026-10-20, baseline-checked 2026-10-21, old stencil path
  removed 2026-10-21; WebGL and phone not measured):** cutouts are
  `clip()` against world-to-box matrices in the cabinet and countertop shaders (the shadow pass
  clips too), with the interior drawn as the existing inside-out box. Stock URP, no fork. Plan,
  results and open items: `.claude/prompts/02-cutouts-by-clipping.md` (steps 1 and 2).
  - `_projectAssets/Shaders/KitchenCutout.hlsl`: the Custom Function `KitchenCutoutAlpha`. Called
    by `TriplanarGraph` (finishes), `KitchenLitGraph` (`Kitchen_Default`, NatureKast marble) and
    `PosterizeGraph` (selection highlights). A renderer with user value 0 (every renderer by
    default) never clips. **Any new material that can land on a cabinet or countertop needs one
    of these shaders**, and a graph that allows material overrides (like `PosterizeGraph`) also
    needs `_AlphaClip` on in each material. URP Lit and Simple Lit don't clip.
  - `KitchenDesign.KitchenModel.KitchenCutoutClipping` (`_project/Scripts/KitchenModel/`, owned by `Kitchen`): rebuilds
    slots and boxes before every render (`RenderPipelineManager.beginContextRendering`). Why:
    every change path (load, drag, resize, inset, shelf, highlight, the photos' manual renders)
    is covered without hooking each one; 0.03 ms per refresh. `CabinetCutOut.TryGetClipBox`
    gives each cutout's box and open faces.
  - `KitchenDesign.Cutouts.CutoutClipSet` (`_project/Scripts/Cutouts/`): a plain class the
    kitchen will own (shader globals are global, but the set isn't static state). `AddBox` takes a
    transform, local bounds and `CutoutFaces` (open faces); `Apply` uploads
    `_KitchenCutoutSlotRanges` / `_KitchenCutoutBoxRows`; `SetRendererSlot` sets a
    `MeshRenderer`'s shader user value. Open faces are pushed out 1 mm and closed faces pulled in
    1 mm. Why: cabinet surfaces often lie exactly on a box face; a closed one (top-mounted product
    flush with the front) must stay and an open one must be cut.
  - Each renderer finds its boxes through a cabinet slot in its **Renderer Shader User Value**
    (`MeshRenderer.SetShaderUserValue`, read as `unity_RendererUserValue`). Why: per-draw data keeps
    the SRP Batcher, needs no copy of each finish material per cabinet, can't reach a neighbouring
    cabinet's renderers, and doesn't touch rendering layers.
  - **A very slight change in look is acceptable to make cutouts work** (user, 2026-10-21).
    So a material on a cabinet or countertop may be moved to a clip-capable shader even if it
    renders slightly differently; measure the difference and record it, but it isn't a blocker.
  - **Every cabinet material is clip-capable since 2026-10-21.** The last three
    (`Cabinet_Stoll_Standard_White`, `Product_Metal_Napoleon_MatteBlack` (both Simple Lit),
    `Product_Visual_Napoleon_PowderedMetal` (URP Lit + normal map)) were moved to
    `KitchenLitGraph` **in place**, for cabinets and products alike. Why not cabinet-only
    copies: Powdered Metal is only ever on Napoleon cabinet-section models despite its name; Matte
    Black is the Napoleon catalog's `m_DefaultCabinetFinishOverride` (every Napoleon cabinet body
    and riser, "the carbon coloured finish") plus ~29 Napoleon cabinet-section models, and also 9
    held-back product models (Fire Magic access doors, ProFit alcove cabinets, XO oven, one
    Napoleon grill). A copy would mean re-pointing every cabinet model's `.fbx.meta` remap, and
    any model the team adds later would point at the original again. Products keep user value 0,
    so they never clip; their cost is the alpha-test variant until the local keyword exists.
    Each was rebuilt as a fresh material and `CopySerialized` onto the asset (GUID kept, old
    shader's properties dropped, queue 2450, no keywords, MotionVectors off, mask slot empty).
    **Matte Black's smoothness went 0.978 → 0:** Simple Lit's 0.978 is a tight Blinn-Phong dot
    with no reflection probe; the same value in a PBR graph is a near-mirror (sphere/cube test:
    mean 7.64/255, max 155). Smoothness 0 was the best fit (mean 2.13, max 57, the missing dot)
    and is what "matte" means. The others kept their values: Powdered Metal 0.01/255 (max 6),
    Stoll White 0.69 (max 10). Baseline after the switch: `resave.json` identical in all 8,
    every save photo within 0.09/255 of the run before (32569, the Napoleon design, draws Matte
    Black on 13 renderer slots and Powdered Metal on 5). A test box through a 32569 cabinet cut
    both cleanly. Stoll White wasn't seen in a design (no Stoll design in the Gemini account).
    The old project still has these on Simple Lit / URP Lit: if these `.mat` files ever come
    over again from `git diff 7700e16a8c..`, the switch is undone.
  - **Napoleon designs can't get cutouts from the app today:** `BrandCatalog_Napoleon` has
    `m_HasGrills: 0` and `m_HasBurners: 0` (Add Part refuses a grill), and its grills are
    cabinet-section models. The switch matters for a catalog change or a later product.
  - **The window cube (`CabinetCutOut.m_StencilRoot`) is data, not geometry that draws.** Its
    MeshFilters' bounds are the clip box and its active faces (`SetActiveStencils`) are the open
    faces, so it stays in `CutOut.prefab`; its MeshRenderers were removed there (Default layer).
    Why kept rather than replaced by a stored box and flags: `TryGetClipBox` and the screenshot
    shelf fix (which stretches the cabinet cutout for front/back photos, and so the clip box) work
    on it unchanged. The interiors keep `CutOutInterior` / `NestedCutOutInterior`
    (`CabinetCutOut.SetInteriorLayer`), both in the opaque mask.
  - Removed with the stencil path (2026-10-21): `KitchenRendererFeature` (off both renderer
    assets), `KitchenForwardRenderPass`, `KitchenShadowRenderPass`, `Scopic/KitchenCutoutStencil`
    + `Kitchen_CutOutStencil.mat`, `Scopic/KitchenShadowCaster` + `Kitchen_ShadowCaster.mat` (only
    the feature used them), `Kitchen`'s near-to-far sort and rendering-mask helpers,
    `ProductVisualContent.SetCutOutRenderingMask`, `CabinetCutOut.m_DrawOrder`, the stencil-layer
    swap in the screenshot shelf fix, `DesignerScene.m_RendererData`. `DesignerScene.unity` keeps
    the orphan `m_RendererData` value until the scene is next saved, and Weaver's generated
    `Procedural Assets/Layers.cs` still lists the two layer constants until it regenerates; nothing
    uses either.
  - Still to measure: the uniform budget on WebGL 2 and the cost of `discard` on tile-based
    mobile GPUs.
