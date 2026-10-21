# Tools/Cutouts

Prototype tooling for cutouts by clipping (`.claude/prompts/02-cutouts-by-clipping.md`). Everything runs
against the 6.6 editor in play mode through the Unity CLI. Snippets are `eval_file` bodies (fully
qualified names, no `using`s, no classes), with `__PLACEHOLDERS__` filled in by `run.sh`.

- `open_design.sh <client id> <quote id>`: if the editor isn't playing, opens `MenuScene`, enters
  play mode and presses the quick sign-in; then loads the design as Edit Design does (reuses
  `Tools/Baseline/unity6/`, and writes its generated `load.cs`, so `git checkout` that file
  afterwards). **Signing in rewrites `Resources/<App>/FallbackJSON/*.json`**: back those up first.
- `run.sh <snippet.cs> [sed expressions]`: runs a snippet after substituting placeholders.
- Since step 2 the app does this itself (`Kitchen.instance.cutoutClipping`, refreshed before every
  render; `LastSlotCount`, `LastBoxCount`, `LastDroppedBoxCount` for checks), and it overwrites
  whatever `apply.cs` or `shadow_test.cs` set on the next frame. For an isolated test, call
  `Kitchen.instance.cutoutClipping.Detach()` first and `Attach(Kitchen.instance)` afterwards.
  `make_cam.cs` (`__SHOW__`) creates `CutoutTestCam` without touching the finish; `highlight.cs`
  applies the skin's primary selection material to the first occupied cabinet.
- `compare_lit.cs` (`__PATHS__`): renders a sphere and cube with each material and with a
  `KitchenLitGraph` twin carrying the same values, and prints the per-channel difference.
- `sweep_lit.cs` (`__PATHS__`, one material per call or it hits the CLI's 5 s limit): like
  `compare_lit.cs`, but also sweeps the twin's `_Smoothness` 0–1 in steps of 0.1 and prints the
  best fit. Needed when moving a Simple Lit (Blinn-Phong) material to `KitchenLitGraph`: the same
  smoothness value doesn't look the same there (see `CLAUDE.md`, Matte Black).
- `survey_shaders.cs`: every shader that can land on a cabinet or countertop (skins' selection
  materials, `Kitchen_Default`, catalog overrides, `Cabinet_`/`Countertop_` finishes, section
  overrides, authored section models).
- `apply.cs`: the prototype of step 2. One slot per cabinet (1, 2, …), set as the renderer user
  value on the cabinet's and countertop's renderers, the cutout interiors and a shelf's extra
  meshes; one box per active `CabinetCutOut` (cabinet and countertop, nested shelf occupants too),
  with open faces = active window faces, plus a countertop box's YMin and a cabinet box's YMax when
  its interior YMax face is hidden. Prints every box. Rerun after anything changes the kitchen;
  the boxes are world space and not tracked. Since the stencil path was removed (2026-10-21) it
  reads the window faces as `stencilRoot.GetChild(face)` (`GetStencilFace` is gone).
- `setup_view.cs` (`__SHOW__` true/false, `__EYE__` local offset from the first occupied
  cabinet's centre): applies `Cabinet_Endura_WhiteOak` / `Countertop_Challenger_SilverVein`
  (both `TriplanarGraph`), shows or hides the products, and places a disabled `CutoutTestCam`.
  The app re-applies its own finish when products are added or photos are taken, and the default
  finish (`Kitchen_Default`) is plain URP Lit, which doesn't clip, so rerun this first.
- `topdown_cam.cs`: moves `CutoutTestCam` above the first occupied cabinet.
- `add_product.cs` (`__SKUS__`, a C# list of quoted SKUs): adds products through
  `EventBridge.Designer.onTryAddProduct`, as the Add Part menu does.
- `shadow_test.cs` (`__SLOT__`): a plate above a floor with one box cutting its middle (slot 30,
  or 0 for no cut), seen from above, to check the shadow pass clips. It replaces the global box
  arrays, so rerun `apply.cs` afterwards.
- `inspect.cs`, `probe.cs` (`__NAME__`, part of an occupant's name): which shaders and slots the
  cabinet renderers have, and which renderers cover an occupant's countertop cutout.
- `restore_visuals.py <visual names…> [--apply]`: moves held-back visuals (`ProductVisual_<name>`
  files) and everything they reference by GUID back from `kitchendesign-held-back/`, like
  `Tools/Baseline/content/restore_doors.py`. Then `add_db_entries.cs` (`__NAMES__`, quoted
  `ProductVisual_…` names) adds their `^` assets to `ProductVisualDatabase` and runs only the
  "product visual prefabs" part of `ProFitAddressableGroupMenuCommand.ProcessAssets` (the menu item
  opens a blocking dialog). If `CollectProductVisualDatabaseAssetGuids` is missing (the team's
  version of the command), the snippet updates the group unfiltered. Its `SaveAssets` once also wrote an unrelated pending change to
  `ProductMaterials.asset` (49 read-only path-keyed entries dropped); that file was restored from
  git. Check `git status` after running it.
- `add_into_shelf.cs` (`__SKU__`): instantiates a product into the first shelf (plunge) in the
  kitchen; use the centred call (see the prompt), the `…Anywhere` one can refuse.
- `aim_at.cs` (`__NAME__`, `__EYE__`, `__FOV__`): aims `CutoutTestCam` at an occupant's cutout.
- `find_designs.cs`: searches the signed-in account's saved designs for grills, burners, shelves.
- `shot.sh <out.png> [camera]` with `shot.cs`: renders a camera to a PNG anywhere on disk. The
  CLI's `capture_game_view` always writes under `Assets/` (even for an absolute path), so it
  isn't used.
