# Tools/Baseline

Scripts that recorded the old app's baseline (2026-10-20) and compare the Unity 6 copy against
it. Data lives outside both repos in `/Users/emmanuel/Documents/Unity/GeminiBaseline/`:
`designs/<quote id>/` (old app, 2022.3), `unity6/<quote id>/` (this project), `index.json`
(method, per-design notes), `_inventory/` (all 102 saved designs of the Gemini quick sign-in
account), `_guard/` (the old project's uncommitted files before the baseline). The data holds
client names; never copy it into a repo.

## unity6/ — this project, through the Unity CLI (`com.unity.pipeline`)
- `runall.sh`: the whole check. Opens `MenuScene`, enters play mode, sets
  `runInBackground`, presses the quick sign-in, waits for the server data, then runs
  `capture.sh` for the 8 usable designs and finally `../compare.py`. Needs the 6.6 editor open on
  this project (`unity open …`; `unity status` shows it ready). Takes about 10–15 minutes.
- `capture.sh <client id> <quote id>`: opens a design the way Edit Design does (`load.tmpl`),
  waits until `kstatus.cs` is stable three times, takes the save photos (`photos.cs`, as F6),
  measures (`measure.tmpl` → `measured.json` + `resave.json`), and moves `photo_*.jpg` from the
  project root into `GeminiBaseline/unity6/<quote id>/photos/`.
- `menucheck.sh` (2026-10-22): the menu smoke check, read-only (never creates, edits, deletes or
  saves on the server). Signs in with the quick sign-in, then drives the menu through the same
  `EventBridge.Menu` delegates the buttons call (`menu.tmpl`, one step per call): home, client
  list, client 10191's quotes, client info, quote info (opened and closed), user profile, the
  log-out prompt (opened and closed). Each step prints the top of `MenuScene.m_MenuHistory` and
  which screens are visible (`MelcherUI.IsVisible()`; `activeInHierarchy` is true for all of
  them). Then it opens design 27755, goes back with `EventBridge.Designer.loadMenuScene`
  (`tomenu.cs`, no save prompt, nothing saved) and checks the menu again. Prints the editor
  console's error count after sign-in, after the menu and at the end. Reference counts
  (2026-10-22, Gemini quick sign-in): 21 errors after sign-in (server JSON missing
  `manufacturer` / `cabinet_section_type` keys), unchanged through the menu, 1,268 at the end
  (almost all "Could not find product visual" for visuals held back by the product thinning).
  The CLI's `console` command keeps only the last 2,000 entries, so read the errors themselves
  from `Logs/Editor.log`.
- **Run against run** (2026-10-22, for the refactor's "no behaviour change" checks):
  `runall.sh` deletes `GeminiBaseline/unity6/`, so first copy it to a named folder (e.g.
  `unity6-after-A3-2026-10-22`). Then `python3 -I compare_runs.py <ref> <new>` (folder names under
  `GeminiBaseline/`): `resave.json` must be byte-identical, `measured.json` may differ only in
  camera angles within 0.01° (runs vary by about 0.0001°). `photodiff.sh <ref> <new>`: mean
  per-channel difference (/255) of every save photo, one CLI call per design (the 5 s main-thread
  limit). Fills `photodiff.tmpl` into a temp file.
- `coldstart.sh <scene path>` (2026-10-22): plays from that scene, lists every scene it passes
  through, waits for the menu to settle, prints the sign-in state, the visible menu screens and the
  console counts. Read-only. Start from `PlatformSetupScene`: `SplashScene` is switched off in the
  build list (in the old project too), so builds start at PlatformSetup, and Splash played on its
  own asks for a build index that doesn't exist. Reference (B1.1 code, signed out, no saved
  token): `PlatformSetupScene MenuScene`, Welcome screen, 13 errors, 4 warnings.
- `rulescheck.cs` (2026-10-22): for each of the three `AppOptions` assets, the old app checks
  written out by hand against what `AppRulesFactory.Create(userFilter)` returns. Edit mode, no
  play. Must print `mismatches=0`.
- `visualscensus.cs` (2026-10-22, area 3): edit mode. Writes every source visual of the product
  visual database (name, content address, every public value of its info) to
  `GeminiBaseline/visualscensus-latest.txt` and prints its hash. Types are found by name, so it
  works before and after the `…SO` renames. Compare the file with an earlier copy (`cmp`) after
  any change to the visual types; the reference before area 3 is `visualscensus-before-B3.txt`
  (901 entries, hash `D72B3E9CB18746DF`). The output is Unicode-normalised (NFC): two
  content addresses flip between composed and decomposed "è" in memory (CLAUDE.md, Gotchas).
- `runall.sh` and `menucheck.sh` don't swap the app profile. Before running them: hash and back
  up `Resources/AppProfile.asset` and every `Resources/*/FallbackJSON` folder, then put in the
  committed profile (`git show HEAD:Assets/AssetsMelcher/Resources/AppProfile.asset`, Gemini) and
  `AssetDatabase.Refresh()`. Afterwards restore from that backup, not from git, and compare the
  hashes.
- Snippets are `eval_file` bodies: statements and local functions, fully qualified names, no
  `using`s, no classes (the CLI rejects them; `run_script` would end play mode).
- `dbstate.cs` shows whether the visual database's runtime lookup is built; `mkrt.cs` builds it
  by hand (not needed since the `ProductVisualDatabase` fix).

## old2022/ — the old project, through kitwright
- `kw.sh`: MCP over HTTP to kitwright at `127.0.0.1:8765` (only while the 2022.3 editor is open).
  `kw.sh code <file.cs>` runs a snippet with `skip_refresh`; snippets here are full classes with a
  `public static string Run()`. The user's Claude Code permission allows this script by its full
  path; after it moved here from the scratchpad, the rule must name
  `/Users/emmanuel/Documents/Unity/kitchendesign/Tools/Baseline/old2022/kw.sh` (ask the user).
- `capture.sh`, `finish.sh`: the same flow as `unity6/`, writing `GeminiBaseline/designs/<id>/`.
  `dump.cs` wrote `_inventory/all-designs.json` from the signed-in client list.
- Before running against the old project: it must stay unchanged. Snapshot `git status` and the
  dirty files first; the old app rewrites `Resources/<App>/FallbackJSON/*.json` and a font atlas in
  play mode, which are discarded with `git restore` afterwards (the user allows git commands
  except commits). The user's own changes are in the old repo's `stash@{0}`.

## compare.py
`python3 -I compare.py <quote ids…>` compares `designs/` with `unity6/`: measurement differences
grouped (render bounds, cameras, ground, renderer count, other) with a 0.0002 m tolerance, and
the top-level keys of `resave.json` that differ (ignoring `timeStamp`, `fileName`). Lists of
named objects (`cameras`) are matched by name, so lines read `measured.cameras.MainCamera.euler`,
not `cameras[0]`; read which camera a line is about before drawing conclusions. `runall.sh`
deletes `GeminiBaseline/unity6/` first, so copy anything you want to keep from an earlier run.

## content/ — the copy and the product thinning
`plan_copy.py` (file list for the copy from commit `7700e16a8c`), `thin.py` (which visuals stay;
`--apply` moves the rest to `kitchendesign-held-back/`), `restore_doors.py` (brings back the door
visuals reached through `CabinetSectionMappings`). See the plan, step 1b, before rerunning any.
