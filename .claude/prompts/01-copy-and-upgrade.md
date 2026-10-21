# 01 — Copy the old project into this one, upgrade it, then refactor in place

Agreed with the user on 2026-10-20. Replaces "port area by area into an empty project": the whole
old project is copied here, upgraded to 6000.6.6f1 as its own step, checked against the baseline,
and only then refactored area by area into the house layout.

## Why
- Track A showed the upgrade alone is a short, known list of breaks. Patching those for real is far
  less work and risk than porting ~25,000 lines and their assets selectively into an empty project.
- The user wants a working app in this project first. Every later refactor step is then checked
  against an app that runs.
- Copying everything with its `.meta` files keeps every GUID reference intact.
- "Don't combine the engine upgrade with the refactor" still holds: upgrade, verify, then refactor.

## Source
- `/Users/emmanuel/Documents/Unity/Gemini/Unity`, commit **`7700e16a8c`** (2026-10-20, branch
  `____localXXX/MAIN`), not its working tree. The working tree has the user's uncommitted changes
  (11 modified files, among them `AppProfile.asset` set to ProFit and `ProductVisualDatabase.asset`)
  and 3 untracked `.meta` files; the copy takes those 11 files from `HEAD` (`git show HEAD:<path>`)
  and leaves the untracked ones out. Only if the user says to copy the working tree instead.
- Read-only on the old repo: `git ls-files`, `git show`, file reads. No checkout, stash, worktree or
  anything else that writes to its `.git` or working tree.
- LFS: the old repo keeps 26,787 files in Git LFS (70 patterns in its `.gitattributes`: models,
  images, audio, fonts, dlls, pdfs). Its working tree has them all smudged (0 pointer files), so the
  bytes come from the working tree, never from `git archive`, which would give LFS pointers.

## Step 1 — the copy (Claude)
Before: the 6.6 editor on this project is closed (an open editor imports files while they land).
`git status` here is clean, so everything replaced is recoverable from git.

1. File list: `git ls-files Unity/Assets Unity/ProjectSettings Unity/Packages` at `HEAD`, which
   leaves out every ignored file (`.DS_Store`, `AddressableAssetsData/<Platform>/` build output,
   `InactiveResources/<App>/`).
2. Left out of `Packages/`: the embedded `com.unity.render-pipelines.universal`, `…core` (13.1.8) and
   `com.unity.shadergraph` (14.0.11). 6.6's own 17.6.0 packages resolve instead (pass 2 of track A).
   Kept embedded: `ca.melcher.unity.core` and `com.arongranberg.aline`, which get real patches.
3. Left out of `Assets/`: NatureKast files in `AssetsMelcher/ProductVisuals` and
   `AssetsMelcher/CabinetSectionModels` that nothing reaches. Reachability is a GUID walk from every
   other tracked file (including `ProductVisualDatabase.asset` at `HEAD`, excluding the Addressables
   group files, which list everything). `Assets/KitWright` stays behind too (kitwright is not used
   here, and it didn't compile in 6.6).
   - Done 2026-10-20: 12,220 files left behind, exactly NatureKast's 2,444 layouts (fbx, prefab,
     `ProductVisual` assets, thumbnails). NatureKast's cabinet finishes, door handles and mappings
     are listed in the `HEAD` database and came over (370 files). No kept file, and no group file
     at `HEAD`, references a file left behind.
   - Not left behind, on purpose: 2,775 WeatherStrong files and 130 others that the `HEAD`
     database doesn't reach. WeatherStrong is work in progress in the old project; if the team
     adds it to the database later, `git diff 7700e16a8c..` shows the database change but not its
     unchanged models, so they have to be here already.
4. `Assets/` merges into this project's `Assets/`: `Doge/` and the `_project…` folders stay; no old
   top-level path collides with them (checked 2026-10-20; 133,349 tracked files in the three
   folders at `HEAD`). `ProjectSettings/` is replaced by the old one, so the old
   `Assets/Settings` URP assets and `Assets/UniversalRenderPipelineGlobalSettings.asset` are the
   ones in use, and `_projectDefaults/Settings` becomes unused (removed after the first open).
   The four settings files only 6.6 has (`MultiplayerManager`, `PhysicsCoreProjectSettings2D`,
   `ProjectAuditorSettings`, `Switch2PlayerSettings`) were left in place as 6.6 defaults.
   `Packages/manifest.json` is the old one unchanged plus `com.unity.pipeline` `0.8.0-exp.1`; 6.6
   bumps Addressables, Cinemachine and URP by itself on first open, as in track A.
   `packages-lock.json` is the template's and gets rewritten.
5. The old `.gitattributes` comes to this repo's root, so LFS applies before the first commit of
   these files. The user installed LFS here on 2026-10-20 and makes the commits.
6. Verify. Done 2026-10-20: 100,564 files copied in 59 s (3.0 GB). All 100,553 unmodified files
   are identical to their source by checksum (`rsync -c -n`), the 10 taken from `HEAD` hash to
   the `HEAD` blobs, none missing. The old repo's `git status` is the same 14 entries as before,
   and `git status` here does not list `.cc/`.

## Step 1b — thin the products to a few hundred (user, 2026-10-20)
The handover already said to copy only a few products, not the whole catalog; step 1 missed it,
and the full first import was stopped partway (`Logs/upgrade-pass6-import.log`). The copy keeps
a few hundred product and cabinet visuals:
- everything the baseline designs use (their SKUs → `ProductVisual` entries), so step 4 can
  compare them, plus one whole catalog for testing the designer (Challenger, the catalog the old
  notes recommend for testing);
- with everything those visuals reference by GUID (prefabs, models, materials, textures).
Finishes, layouts, backings and everything outside the product folders stay whole. The other
visuals are moved out of the project to
`/Users/emmanuel/Documents/Unity/kitchendesign-held-back/` (same relative paths, nothing deleted),
and the database's entries for them are removed in the editor, not by editing YAML. Because the
subset depends on the baseline designs, step 2 runs before the first full import.

Done 2026-10-20, with one change: the whole Challenger catalog alone is 1,012 cabinet visuals
(216 cabinets × Pan and Shaker door styles plus the bases), too many for "a few hundred", so it
was dropped. Kept instead (rule in `Tools/Baseline/content/thin.py`):
- cabinets: every cabinet visual the 8 usable baseline designs use, with all its door-style
  variants (`<name>@<style>`), plus generic fillers: 75 entries;
- products: the 7 the designs use plus all Twin Eagles and Bull products: 142 entries;
- finishes (`ProductVisual_Cabinet_`, `_Flooring_`, `_Countertop_`): all 631;
- everything those reach by GUID; nothing outside the three thinned folders
  (`ProductVisuals`, `CabinetSectionModels`, `ProductModels`) reached any further visuals.
848 of 8,552 database entries stay. 41,311 assets (82,622 files with `.meta`) were moved to the
holding folder; `Assets` went from 3.0 GB to 1.8 GB. The database still lists the moved entries
until the editor cleanup.

Correction (2026-10-20): that rule missed the **door visuals**. A cabinet's doors are separate
entries reached by name, not GUID: `CabinetSectionMappings/CabinetSectionDoor_<brand>.asset` maps
the cabinet id to a door id (`CabinetSection_Grill_Challenger_X` →
`CabinetSection_Door_Challenger_X`), and the visual is `ProductVisual_<door id>@<style>^`
(`CabinetSectionDoorUtility.FindDoorStyleVisual`). Handles are only added when a door visual is
found, and each handle adds a `HARDWARE` price-sheet entry. Without them the cabinets had no doors
or handles, and design 36449 lost its 5 `HARDWARE` entries. Restored with `restore_doors.py`: 51
door entries (21 of the 32 used cabinets have a mapping; the rest use `<name>@<style>` variants,
already kept), 255 assets moved back, entries re-added in the editor: 899 database entries now.
Anything selected by name later (doors, fillers, door handle styles) has to be followed through
these mappings, not only through GUIDs. Restored or newly added visuals load in play mode only
after `ProFitAddressableGroupMenuCommand.ProcessAssets` (Update addressable groups) has put them
in the groups and the assets are saved (`AssetDatabase.SaveAssets`); until then Addressables
logs `InvalidKeyException: No Location found for Key=…` and the design loads partly.

## Step 2 — the baseline (old project, 2022.3)
As proposed on 2026-10-20 (see `CLAUDE.md`, "Not built yet / handover"): `server.json`,
`resave.json`, `measured.json` and the photos per saved design, outside both repos. Still waiting
on the user's answers (which designs, which app, sign-in, server copies, location). It doesn't
touch the copy and can run before or after step 1, but must be finished before step 4. Its
`index.json` records that it was taken from the old working tree and lists the uncommitted changes
that were in it.

## Step 3 — the upgrade, done properly (no throwaway patches)
From track A's list, as real fixes:
- `GetInstanceID()` → `GetEntityId()`; `Scene.handle` → `SceneHandle` with
  `GetRawData()`/`FromRawData(ulong)`; `TreeView` → `TreeView<int>`;
  `hierarchyWindowItemOnGUI` → `hierarchyWindowItemByEntityIdOnGUI`; SerializeInterfaces'
  `InstanceIDToObject`/`HierarchyProperty`; ALINE's old `Configure`/`Execute` removed.
- Cinemachine 3 in the recorded order: `using Cinemachine;` → `using Unity.Cinemachine;` only, run
  the upgrader, then move field types and calls to the CM3 API.
- TMP: retarget the 10 font assets' `m_Script` (or regenerate them), keep
  `m_defaultFontAssetPath: Fonts/` in `TMP Settings`.
- `ProFitBuildProcessor`'s Android icon code behind `#if UNITY_ANDROID` (or the module installed).
- `ProFitProductModelFbxImporter`: null guard for `ProFitEditorOptions.Get()` on a first import,
  and no `AssetDatabase.ImportAsset` inside the postprocessor.
- DOTween `.meta` importer versions (reimport or update DOTween).
- `KitchenForwardRenderPass`, `KitchenShadowRenderPass` and `KawaseBlur` can't run in 6.6. Until
  step 5 the `KitchenRendererFeature` is off: no cutouts, no cutout shadows, no UI blur. Listed in
  the handover as known missing, not a regression to chase.
- Measure the first full import time.

## Step 4 — check against the baseline
Load the same saved designs in the upgraded copy (same app, same account, no saving) and compare:
`resave.json` and `measured.json` must match exactly (sizes to 0.1 mm, SKUs, price inputs); photos
side by side, with framing differences expected from Cinemachine 3 and the missing cutouts.

Result (2026-10-20, editor play mode, signed in as the same dealer, 8 usable designs; outputs in
`GeminiBaseline/unity6/<quote id>/`): **every span, cabinet, product and backing matches the old
app to 0.1 mm (size, position, rotation, SKU, visual), renderer counts match, and `resave.json`
(the save JSON with all price inputs) is identical in all 8.** Remaining differences, all
presentation:
- the cover photo camera: yaw about 85–90° instead of about 44°, pitch about 30° instead of 12°,
  so the cover photo looks along the kitchen and crops it (Cinemachine 3 rotation composer /
  group framing need retuning);
- lighting: the scene renders noticeably darker (floor dark grey instead of light blue-grey);
  cause not investigated yet (URP 17 lighting, ambient, post-processing or the skybox);
- no cutouts (step 5).

Follow-up (2026-10-20): both presentation differences are fixed; see `CLAUDE.md`, "Upgrade
fixes in the copy", ambient light and save-photo cover camera. The "cover camera" numbers above
were `MainCamera`'s, which had taken over the cover vcam (Cinemachine 3 channels). Re-run of all 8:
`resave.json` identical, the cover camera within about 0.5° of the old one (27755: 1.5° pitch),
other photos 0–3.3/255 mean pixel difference. Still open: the live orbit view's FOV and start
angle.

## Step 5 — cutouts by clipping
Moved up from the old plan's step 5, because the app has no cutouts until it's done. Design as in
"Key systems: Cutouts (plan, not built)". Prototype first; measure on WebGL and a phone. The
plan is `.claude/prompts/02-cutouts-by-clipping.md`.

## Step 6 — refactor area by area
Into the house layout (section 2), with `git mv` of `.cs` + `.cs.meta` together: server and offline
data, layouts and spans, cabinets, products, UI, photos. App rules behind one layer. The house rules
apply to code as it is refactored, not to untouched old code. Then the old plan's steps 6–8.

## Bringing over later work from the old project
The copy's source commit is `7700e16a8c`. Content the team adds to the old project afterwards shows
up in `git diff 7700e16a8c..<later>` in the old repo, and is brought over from that diff.
