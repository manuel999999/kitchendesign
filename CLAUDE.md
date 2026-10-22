# cloder.md — Space Sheep Unity house rules

Drop this file into a Unity project's root and rename it `CLAUDE.md` (setup moves it to
`.cc/CLAUDE.md` if the project's notes are to stay secret, R-GIT-1). It carries the architecture,
conventions and working rules proven on Slimer (Slimebane), so every new project is built the same
way. The house rules are sections 0–6; the project's own handover grows under section 7, in this
same file.

Notation: `{Root}` is the project's root namespace. `MUST` / `NEVER` are hard rules; everything
else is the default unless the user says otherwise.

**This project overrides some of these rules.** The list is in section 7, under "Project
overrides". Where an override and a rule in sections 0–6 disagree, the override wins.

```
ROOT_NAMESPACE: KitchenDesign
CODE_COMMENTS:  file-only   inline | file-only
CLAUDE_MD_GIT:  tracked     tracked | ignored
SETUP_DONE:     yes
```

---

## 0. Bootstrap — run this before any other work in a project

1. **Ask the setup questions for every value still `<unset>` in the block above**, all in one go
   (one question each), and do nothing else until they are answered. Write each answer into the
   block. Never guess any of them.
   - **Root namespace**: *"What root namespace should this project use?"* (PascalCase, one word,
     e.g. `Slimer`). Not inferred from the folder or product name.
   - **Code comments**: *"Should explanations live as comments in the code, or only in
     CLAUDE.md?"*
     - `inline`: short `//` comments above a method or tricky block saying why (R-CODE-7).
     - `file-only`: no comments in code. Every why goes into the matching section of this file
       instead (see R-CODE-7).
   - **CLAUDE.md in git**: *"Should CLAUDE.md be committed with the project, or kept out of
     git?"*
     - `tracked`: committed like any other file. The default for personal projects.
     - `ignored`: kept private. All of our notes move into `.cc/`, which git is told to ignore
       (R-GIT-1). Choosing this is the user's go-ahead for those file moves and git changes.
2. **Apply the git choice** (R-GIT-1) if it is `ignored`.
3. **Check required packages** (section 1). If one is missing, tell the user only:
   *"There are missing packages in the project."* Do not name the package, do not suggest where to
   get it, and do not scaffold anything until the user says it has been added. Re-check when they do.
4. **Scaffold** what section 2 lists that does not exist yet: folders, the four asmdefs, the empty
   `GameSceneInitializer`, `.claude/prompts/`, `Tools/`. Never overwrite an existing file or asmdef;
   if the project already has a different layout, list the differences and ask before moving
   anything (moves go `.cs` + `.cs.meta` together, see R-CODE-12).
5. **Start the project's handover** under section 7 of this file, from its skeleton.
6. Set `SETUP_DONE: yes` above.

The user can change any answer later ("put comments in the code from now on", "make CLAUDE.md
private"): update the block, then apply it (R-CODE-7, R-GIT-1).

On every later session: if `SETUP_DONE` is `yes`, skip to the task, but still obey sections 1–6.

---

## 1. Required packages

| Package | How to detect it | Used for |
|---|---|---|
| **Doge** (in-house) | an assembly definition named `Doge` (`Doge.asmdef`, usually `Assets/Doge/`), or a `Packages/manifest.json` / `Library/PackageCache` entry whose asmdef is named `Doge`; the namespaces `Doge.Events` and `Doge.UIToolkit` must resolve | every event and every UI Toolkit control |

- **If Doge is missing: say "There are missing packages in the project." and nothing more about it.**
  Never write a stand-in event system or UI base class to work around its absence.
- Recommended, not required: **DOTween** (in `Plugins/`; its shortcuts live in the separate
  `DOTween.Modules` assembly, which an asmdef must reference explicitly). Unity's Input System
  (polled through `Pointer.current`, no action asset), URP, TextMeshPro.
- Of Doge, only `Doge.Events` and `Doge.UIToolkit` are used. Its `Pooling`, `Probability`,
  `Collections` and `Messaging` are not; don't start using them without asking.

### Doge API in one screen

```csharp
// Events: static holders, one property per event.
public static GenericEvent<long> OnGoldChanged { get; } = new();   // payload
public static ActionEvent OnLoadFinished { get; } = new();         // no payload
Event.AddListener(Handler);  Event.RemoveListener(Handler);  Event.Raise(value);
// Handlers bind as Doge.Events.Types.EventTypes.GenericEvents<T>, NOT Action<T>.
// GenericEvent.Raise drops a null payload silently.

// UI: one subclass per screen concern, all on the GameObject holding the UIDocument.
public abstract class BaseDocumentControl : MonoBehaviour   // [RequireComponent(typeof(UIDocument))]
{
    protected UIDocument sourceDocument;
    public bool IsInitialized { get; protected set; }
    protected abstract void GetDisplays(VisualElement root);  // resolve elements (runs in Awake)
    protected abstract void AddClicks();                      // register callbacks (Awake)
    protected abstract void RemoveClicks();                   // unregister (OnDestroy)
}
// Views: LabelDisplay, ButtonDisplay, ImageDisplay, ElementDisplay, DataElementDisplay.
```

---

## 2. Project layout and assemblies

```
Assets/
├── _project/Scripts/          {Root}            asmdef {Root}           — all gameplay
│   ├── Game/                  systems: Game<Name>Status, GameSceneInitializer, controls' logic
│   ├── Behaviors/             one-concern components on prefabs: <Thing><Concern>Behavior
│   ├── UI/                    BaseDocumentControl subclasses: Game<Name>Control
│   ├── Scriptables/           every ScriptableObject type        ({Root}.Scriptables)
│   ├── Serializables/         every plain [Serializable] class   ({Root}.Serializables)
│   ├── Utility/               static helpers                     ({Root}.Utility)
│   └── <System>/              Audio, Save, Spawn, Loot, …        ({Root}.<System>)
├── _projectShared/            {Root}.Shared     asmdef, references NOTHING
│   ├── Enums/                 EVERY project enum                 ({Root}.Shared.Enums)
│   ├── Interfaces/            seams between systems               ({Root}.Shared.Interfaces)
│   ├── Types/                 small value types (structs)         ({Root}.Shared.Types)
│   ├── Common/                pure static rules/tables            ({Root}.Shared.Common)
│   ├── Serializables/         only types an event payload must carry
│   └── Scriptables/           only SOs an interface here must take
├── _projectEvents/Events/     {Root}.Events     asmdef, refs Doge + {Root}.Shared
│                              Event<Domain>.cs static holders + <Name>Info payloads
├── _projectDebugging/         {Root}.Debugging  asmdef, Editor ONLY, autoReferenced false
│                              its own Scriptables/ Serializables/ Enums/ — never reaches a build
├── _projectAssets/            NO code: Audio, Fonts, Prefabs, Scenes, Scriptables (the .asset
│                              instances), Shaders, Sprites, UI (uxml/uss/PanelSettings)
└── _projectDefaults/          render pipeline and project settings assets
Tools/                         Node/C# tooling OUTSIDE Assets so Unity never imports it; one README
                               per folder; generated output (out/, export/, node_modules/) gitignored
.claude/prompts/NN-name.md     a written plan per larger change, executed afterwards
CLAUDE.md                      this file: house rules + the project's handover (section 7)
```

In secret mode (`CLAUDE_MD_GIT: ignored`) every Markdown file we write lives under `.cc/`
instead (R-GIT-1): `.cc/CLAUDE.md`, `.claude/prompts/`, and the tool READMEs as
`.cc/tools/<ToolFolder>.md` rather than inside `Tools/`. Wherever this file says
`.claude/prompts/`, read `.claude/prompts/` in that mode.

**Assembly graph** (an arrow is a reference; nothing points back up):

```
{Root} ──► {Root}.Events ──► {Root}.Shared
   │            └──────────► Doge
   ├──► {Root}.Shared
   ├──► Doge, DOTween.Modules, third-party gameplay libs
{Root}.Debugging (Editor) ──► {Root}, {Root}.Shared
{Root}.<Area>.Editor (Editor) ──► whatever it inspects
```

- R-ASM-1 **`{Root}.Shared` references nothing.** That is why every enum and every type an event or
  interface needs lives there, and why namespaces track assemblies.
- R-ASM-2 A type moves "down" into Shared only when an event payload or interface needs it — say so
  in the project `CLAUDE.md` when it happens.
- R-ASM-3 Editor-only assemblies: asmdef name `{Root}.<Area>.Editor`, but **namespace
  `{Root}.<Area>Editor`** (or `…Inspectors`): a namespace ending in `.Editor` collides with
  `UnityEditor.Editor`.
- R-ASM-4 Every debug-only type (debug drops, save editors) goes in `_projectDebugging/` so it can
  never ship. Ship-side editor helpers are `#if UNITY_EDITOR` and named `…ForEditor`.
- R-ASM-5 Reference asmdefs by name in new projects; check `.asmdef` files into git with their `.meta`.

---

## 3. Code conventions

- R-CODE-1 **Always brace `if` / `else`**, even one statement.
- R-CODE-2 **Always write `private` explicitly.**
- R-CODE-3 `[SerializeField] private` fields are camelCase and read with `this.` inside methods.
- R-CODE-4 Other private fields are `_underscore`.
- R-CODE-5 Guard-clause early returns over nested conditionals.
- R-CODE-6 Wrap `Debug.Log` / `LogWarning` / `LogError` in `#if UNITY_EDITOR`.
- R-CODE-7 **Comments follow `CODE_COMMENTS`.** In either mode: never comment *what* the code does,
  and never write XML doc comments in project code. `[Tooltip]` text is inspector UI, not a
  comment, and is allowed in both modes.
  - `inline`: comment sparingly, a short `//` above a method or tricky block saying **why**.
  - `file-only`: write **no** comments in code. Every why that would have been a comment goes in
    the matching part of section 7 instead (the system's section, the Initialize-chain list, the
    gotchas), so a later session still finds it. In an existing codebase, leave its comments alone
    unless the user asks for them removed. The mode applies to new and edited code.
- R-CODE-8 **`ScriptableObject` types end in `SO`** (`BossRulesSO`), live in `Scriptables/`, namespace
  `…Scriptables`.
- R-CODE-9 **One type per file, filed by what the type IS, not what it is for.** A `[Serializable]`
  class lives in `Serializables/` even if only one `MonoBehaviour` uses it — never nested in it.
- R-CODE-10 **Every enum lives in `_projectShared/Enums/`**, `{Root}.Shared.Enums`, with **explicit
  values, append only** (enums serialize as ints).
- R-CODE-11 `using System;` makes `Random` ambiguous — write `UnityEngine.Random`.
- R-CODE-12 **Renaming or moving a script moves the `.cs` and its `.cs.meta` together** (`git mv`
  both) and renames file and class in the same pass. Assets bind to the MonoScript GUID in the
  `.meta`, so the namespace is then free to change. `[Serializable]` sub-objects bind by field name
  only and can move freely.
- R-CODE-13 Naming: systems `Game<Name>Status` / `Game<Name>Control`; prefab components
  `<Thing><Concern>Behavior`; events `Event<Domain>.On<Thing><PastTense>`; payloads `<Thing>Info`;
  editor probe hooks `<Verb>ForEditor`.
- R-CODE-14 `GetComponent<T>() ?? AddComponent<T>()` is wrong in Unity (fake null): use `== null`.
- R-CODE-15 Keep a game-wide quantity's type wide enough from day one (currency and XP are `long`).

---

## 4. Architecture rules

### Construction and order
- R-ARC-1 **Systems are constructed, not found.** One `GameSceneInitializer` per scene holds
  `[SerializeField]` references to every system and, in `Start`, calls `Initialize(...)` on each in
  dependency order, passing collaborators as arguments. No `FindObjectOfType`, no singletons, no
  setup in `Awake` that depends on another system.
- R-ARC-2 **Every load-bearing position in that chain has its why written down**: a `//` comment
  above the call in `inline` mode, a line in section 7's Initialize-chain list in `file-only` mode.
  A new system joins the chain; it does not self-start.
- R-ARC-3 **No cycles.** If A and B would reference each other, one of them owns the operation
  (e.g. the wallet owns the purchase and holds the upgrade status, never the reverse).
- R-ARC-4 The initializer sets pool capacities (e.g. `DOTween.SetTweensCapacity`) first, sized by
  measurement, and raises `EventGame.OnGameSceneReady` last for anything outside the scene.

### Events
- R-EVT-1 Cross-system communication goes through static `Event<Domain>` classes in
  `{Root}.Events`, built on Doge `GenericEvent<T>` / `ActionEvent`.
- R-EVT-2 **A status system announces its starting state once from `Initialize()`**, so a listener
  that came up earlier sees it; a restore announces once, never replays history.
- R-EVT-3 Subscribe in `Awake` (or `Initialize`), unsubscribe in `OnDestroy`. Events raised from the
  chain have no replay — late subscribers miss them.
- R-EVT-4 High-frequency payloads are `readonly struct` with only what listeners need; one event
  with an enum payload beats an event per category.
- R-EVT-5 Save and similar side effects are *requests* (`OnSaveRequested`) that the owner coalesces,
  not commands.

### Components and pooling
- R-ARC-5 **One behavior per concern** on a prefab, with serialized references to collaborators
  rather than `GetComponent` at runtime.
- R-ARC-6 Pools wrap `UnityEngine.Pool.ObjectPool<T>` with `collectionCheck` on and an explicit
  `Prewarm`. The pool injects dependencies through **interfaces** in `{Root}.Shared.Interfaces`
  (`GetComponents(List<T>)` when several components implement one).
- R-ARC-7 **Per-instance state resets on spawn, not in `Awake`.** A pooled object carries a life id
  that bumps every spawn; any delayed callback checks it so it can't hit the next life.
- R-ARC-8 **Kill every tween in `OnDisable`.**
- R-ARC-9 **Avoid `Update`.** Prefer tween chains, events and scheduled UI ticks. When many things
  must move every frame, one `Update` on the spawner/manager steps all of them (no per-instance
  `Update`, no per-instance tween for floods).
- R-ARC-10 Interfaces fall back to identity behaviour when a dependency is null, so a bare prefab
  still works in a sandbox scene.

### Data, balance and saves
- R-DATA-1 **Config vs progress.** Tables (levels, prices, rules) are ScriptableObjects that ship
  with the build and are never written into a save; a save holds only what the player owns
  (levels, currency, ids). Derived values (unlocks, damage) are recomputed, never saved.
- R-DATA-2 **Balance numbers are generated, never hand-tuned in the inspector**: a script in
  `Tools/` holds them and simulates against them, and a `run_script` builder writes the assets.
  Art/visual numbers live in assets and can be tuned by hand.
- R-DATA-3 **Ids that reach a save are immutable once shipped.**
- R-DATA-4 One save file, written through one store class that is the only `JsonUtility` caller:
  **write `<name>.tmp`, then `File.Replace`**; never serialize onto the live path.
- R-DATA-5 Flush on `OnApplicationPause(true)` (mobile kills without `OnApplicationQuit`); throttle
  ordinary writes; write immediately for anything the player could undo by killing the app.
- R-DATA-6 **A version bump migrates, never rejects.** One migrator owns version decisions: newer →
  leave the file alone; too old → refuse; in range → migrate. Keep a real captured file per old
  version in `Tools/SaveFixtures/`.
- R-DATA-7 Saved fields are public fields of concrete `[Serializable]` classes, primitives,
  strings, or `List<T>`/arrays of those. No `Dictionary`, properties, interfaces — `JsonUtility`
  drops them silently.
- R-DATA-8 Static stores reset on `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.
- R-DATA-9 Per-device preferences (volume, UI mode) go in `PlayerPrefs` under `{Root}.<Area>.<Key>`,
  not in the save.

### UI (UI Toolkit)
- R-UI-1 Every screen concern is a `BaseDocumentControl` subclass; several may share one `UIDocument`.
- R-UI-2 Animation is USS `transition` plus class toggles, not tweens.
- R-UI-3 **Everything is `picking-mode="Ignore"` except what must take presses**, and those carry a
  `ui-blocking` class the game's input checks on the press, so UI never silently eats a gameplay tap.
- R-UI-4 A closed window is `display: none`, never just `opacity: 0` (it would still block).
- R-UI-5 Per-frame events (currency per coin) only toggle classes/enabled state; full redraws happen
  on rare events. Early-return while a window is closed.
- R-UI-6 Safe area: padding goes on the HUD root element, never on the panel root.
- R-UI-7 Size the HUD by the PanelSettings reference resolution, not by editing USS values one by one.

---

## 5. How work is done

- R-WORK-1 **Do prefab, scene and asset work through the Editor, never by hand-editing `.prefab`,
  `.unity`, `.asset` or `.meta` YAML.** Use the Unity CLI (`unity command <name> --json --no-banner`,
  flag syntax `--file x --entry y`); `run_script` compiles a `.cs` from outside `Assets/` and runs a
  static entry point. Wrap its body in try/catch and return the exception text — it swallows throws.
- R-WORK-2 Larger changes start as `.claude/prompts/NN-name.md` (intent, decisions, how to verify),
  then are executed.
- R-WORK-3 **Verify with numbers, not by eye**: reflect on live state from `eval`, count, time,
  measure. Finish with a clean console and stable pool counts.
- R-WORK-4 Never recompile (or save a `.cs`) while play mode runs: the domain reload wipes
  runtime-assigned private fields and looks like an injection bug.
- R-WORK-5 Debug tools are editor-only (`_projectDebugging/`, `…ForEditor`); prefer them to editing
  the save by hand.
- R-WORK-6 Recommend and act on technical choices; ask only at real forks of design or taste.
- R-WORK-7 Use the Edit tool, not `sed`/`perl`, for any edit touching `||` or `\b` (BSD sed has no
  `\b`; `\|` in perl is alternation).

### Git and CLAUDE.md
- R-GIT-1 **`CLAUDE_MD_GIT: ignored` keeps all of our notes in one folder, `.cc/`, and keeps it
  out of the repository.**
  - Move this file to `.cc/CLAUDE.md` and leave no `CLAUDE.md` at the root. Everything else we
    write in Markdown goes under `.cc/` too: `prompts/`, and tool READMEs as
    `.cc/tools/<ToolFolder>.md`.
  - **Claude Code does not load `.cc/CLAUDE.md` by itself** (it only auto-loads a root
    `CLAUDE.md` or `.claude/CLAUDE.md`). The user points to it at the start of each session.
    When they do, read the whole file before any other work. If a session in a project with a
    `.cc/` folder starts without that, read `.cc/CLAUDE.md` before changing anything.
  - Add one line to **`.git/info/exclude`, never to `.gitignore`**: `/.cc/`. `.gitignore` is
    committed, so its line would show the secret to anyone who can see the repo; the exclude file
    works the same way but is never committed. The leading `/` ties the entry to the project root
    and the trailing `/` takes the whole folder. From the root:
    `printf '/.cc/\n' >> .git/info/exclude`.
  - The exclude file belongs to one copy of the repo. **On a fresh clone or another machine, add
    the line again before the first commit**, or `git add .` will pick the folder up. If
    `git status` ever lists `.cc/`, the exclusion is missing.
  - `.claude/` is not excluded (the user's choice). Claude Code can create it by itself for its
    settings, e.g. saved permissions. **Never stage or commit anything under `.claude/`**: stage
    files by path rather than `git add .`, and if `.claude/` shows up in `git status`, tell the
    user it is there.
  - If any of these files are already tracked, untrack them with `git rm --cached` (the files stay
    on disk) and commit that.
  - **Tell the user that earlier commits still contain them.** Removing them from history is a
    rewrite of the repository, so only offer it, never do it unasked.
  - While `ignored`: never quote, name or link CLAUDE.md, `.cc/` or `.claude/` in commit messages, PR
    descriptions, code, README files or anything else that is committed or published. In
    `file-only` comment mode that leaves code with no comments at all, which is the point.
- R-GIT-2 `tracked`: CLAUDE.md stays at the root, where Claude Code loads it by itself, and is
  committed with `.claude/prompts/` alongside the work they describe.

### Transferable Unity gotchas
- `run_script` ends play mode; use `eval` (no `using`s, extension methods called as statics) for a
  running game.
- In the Editor, `Screen.width/height` from a script/eval body is the focused window's size.
- An unfocused Editor doesn't tick: `set_autotick --enable true`, and `Application.runInBackground =
  true` from `eval` (don't change Player Settings for it).
- Texture sizes must be multiples of 4 or compression silently falls back; default max size 2048.
- `TextMeshPro` needs a `RectTransform`; setting outline in code instantiates a material per object —
  use a shared material asset.
- In Unity 6 URP 2D, `SpriteRenderer.color` reaches custom shaders as `unity_SpriteColor`, not
  vertex colour.
- A new `[SerializeField]`'s initializer reaches an open scene only once; change values through a
  `SerializedObject` and save.
- UI Toolkit `ClickEvent` fires on pointer up; gameplay acting on pointer down must test UI on the press.

---

## 6. Conformance check — before reporting any work as done

Run through this list against everything you added or changed:

1. Every new type is in the folder and namespace section 2 assigns (SO → `Scriptables/` + `SO`
   suffix; serializable → `Serializables/`; enum → `_projectShared/Enums/` with explicit values).
2. No new asmdef reference points "up" the graph; `{Root}.Shared` still references nothing.
3. New systems join `GameSceneInitializer` with their why written down (R-ARC-2); no `Find…`
   lookups for systems.
4. Events go through `Event<Domain>` with Doge types; listeners are removed in `OnDestroy`.
5. No new `Update` unless it steps many things at once; tweens die in `OnDisable`; pooled state
   resets on spawn.
6. No balance number typed into an asset by hand; no saved id renamed; save changes migrate.
7. Braces, explicit `private`, `this.` on serialized fields, editor-wrapped logs. Comments match
   `CODE_COMMENTS`: why-only in `inline`, none at all in `file-only` (the whys are in section 7).
8. Section 7 is updated. If `CLAUDE_MD_GIT` is `ignored`, nothing committed mentions it.

If an existing project breaks one of these, point it out and propose the fix — don't silently
reshape someone's code outside the task.

---

## 7. The project handover

Everything below the house rules is this project's handover between sessions. A future session
starts with nothing else. **Update it in the same session as the change, before reporting done.**
Edit sections in place — it describes how the project *is*, not a changelog. In `file-only`
comment mode it is also where every why lives, so it carries more detail.

Skeleton to append at setup (its headings sit at `##` under this section):

```markdown
# {Root}

## What it is
Engine version, render pipeline, orientation, scenes and build order.

## Keeping this file current — read this first
(what to update, what not to record)

## Gotchas
Traps that cost time in THIS project. Highest-value section.

## Scene and prefab map
Hierarchy with the component on each node.

## The Initialize() chain
The order, and why each load-bearing position is where it is (required in `file-only` mode,
where the code carries no comments).

## Events
Each event, its payload, when it is raised.

## Key systems
One section per system: what it does, its non-obvious decisions and their WHY, how it was verified.

## Tools
Every folder under Tools/ and what it generates.

## Not built yet / handover
What is open, what the user is testing, what to ask first next session.
```

Record: new systems/assets/events, changed seams, non-obvious decisions with their reason, traps
that cost time, finished or abandoned features. Don't record: routine fixes, inspector tuning
values, anything obvious from the code or git history.

---

# Kitchen designer (Unity 6 rebuild)

## What it is
The Unity 6 rebuild of the Gemini kitchen designer (one codebase shipping as three apps: ProFit,
Stoll, Gemini). Unity 6000.6.6f1, a supported release. Started on 2026-10-20 with only `Doge` and
`_projectDefaults` in `Assets/`. Setup answers were given on 2026-10-20 (namespace
`KitchenDesign`, `file-only`, secret mode). On 2026-10-22 the user switched to `tracked`: the
notes moved out of `.cc/` to the usual places (this file at the root, plans in `.claude/prompts/`,
tool READMEs in `Tools/<ToolFolder>/README.md`). Section 2's layout was scaffolded the same day. Later that day the whole old
project was copied in (see "The two tracks", B): `AssetsMelcher`, `AssetsThirdParty`, `Cloder`,
the old scenes (`MenuScene`, `DesignerScene`), the old `ProjectSettings`, and the embedded
`ca.melcher.unity.core` and ALINE packages. The empty `_project…` folders are where code moves as
it is refactored.

Git (set up by the user on 2026-10-20): remote `origin` is
`git@github.com:manuel999999/kitchendesign.git` (SSH; checked 2026-10-22). Commits are authored
as `EmmanuelMGemini`.
Branches `main` (pushed, `dec60d9 initial commit`) and `develop` (local, checked out). The user
makes the commits or asks Claude to; the `Co-Authored-By` trailer is fine here, since this is the user's own repo (2026-10-22; it is dropped only on projects made for others).

`_projectDefaults` still holds content from Unity's project template (`Readme.asset`,
`TutorialInfo/`, `InputSystem_Actions.inputactions`, the sample scene). Pointed out to the user,
not removed.

## Keeping this file current — read this first
Update this file in the same session as a change, before reporting done. Edit sections in place;
it describes how the project is, not a changelog. With `CODE_COMMENTS: file-only` the code has no
comments, so every why lives here, in the section of the system it belongs to. Don't record
routine fixes, inspector values, or anything the code or git history already shows.

The old project is `/Users/emmanuel/Documents/Unity/Gemini/Unity` (Unity 2022.3.62, URP 14, game
code in `Assets/AssetsMelcher/Scripts`, namespace `Melcher`). Its `CLAUDE.md`
(`/Users/emmanuel/Documents/Unity/Gemini/CLAUDE.md`) is the reference for how the app behaves:
backings, end panels, save photos, Addressables, the product visual database, flooring, pricing.
Its section "Future plan: Unity 6 rebuild" is the agreed plan this project follows. Read it before
porting any area. Never edit the old project from a session here.

The user's own tools in the old project (the `Cloder` folder, the product visual database
window's "Add multiple", "Update addressable groups" limited to database assets) live only on the
user's local branches (`____localXXX/MAIN`, also `____localXXX/WORK-IN-PROGRESS`), never on the
team's branches such as `feature/model-integration` (checked 2026-10-21: none of it there). Keep it
that way: don't copy them into a team branch.

This copy came from `7700e16a8c`, so it has them, and **they stay in this project** (user,
2026-10-21, after weighing a local-only branch setup and deciding it wasn't worth the
complexity).

**graft (persistent memory).** The user's global setup (`~/.claude/rules/graft.md`) gives every
session a memory graph shared across projects; it is used here too. In graft this project is
`github.com/manuel999999/kitchendesign`, not bootstrapped yet (2026-10-22). This file is the
source of truth for the project: its state, decisions and handover live here and are committed.
graft holds what carries beyond one session or project: gotchas, procedures, fixes, each with
`--source file:<path>` when it comes from a file here. When a graft note disagrees with this file
or the code, this file wins, and the graft note is superseded or marked stale in the same session.
## How we work together
These carry over from the sessions on the old project.

- **Agree the approach before editing.** While the user is still weighing options ("or…", "would
  it be possible to…"), reply with analysis and one recommendation, not a survey of options.
  Read-only investigation is fine. Edit once they say to go ahead. Trivial edits they ask for
  directly don't need a separate go-ahead.
- **Explanations go in this file, not in code comments.** In the old project the user asked for no
  comments on our changes. Propose `file-only` for `CODE_COMMENTS` when section 0 asks, but let
  the user answer.
- **Stage by default, commit only when asked.** Stage only the files from the change at hand. The
  user commits, or asks Claude to. The `Co-Authored-By: Claude` trailer is fine in this repo, the user's own (user,
  2026-10-22); no need to ask about it any more.
- **Measure, don't guess.** Sizes, memory and timings come from the editor or a build, not from
  estimates.
- **Plain, short replies.** Say what was found, what is recommended, and what is waiting on the
  user. Flag uncertainty (for example about Unity 6.6 features newer than the model's knowledge)
  instead of stating it as fact.
- Dates are written out (2026-10-20), and decisions are recorded here with their reason.

## The two tracks
- **A. Upgrade test (not this folder).** A throwaway copy of the old project's `Assets`,
  `Packages` and `ProjectSettings` (no `Library`), opened in 6.6 unchanged, to see what the engine
  upgrade alone breaks: whether Compatibility Mode still runs the old render passes
  (`KitchenForwardRenderPass`, `KitchenShadowRenderPass`, the Kawase UI blur), which packages and
  plugins fail to compile (`ca.melcher.unity.core`, Addressables, Cinemachine), how many shaders go
  pink, and how long the import takes. Its folder names stay as they are, because 15 C# files in the
  old project hard-code `AssetsMelcher` paths (21 places). Findings go in "Upgrade test findings"
  below. Location: not chosen yet (2026-10-20).
- **B. This project.** Changed on 2026-10-20 (user): instead of porting into an empty project,
  the whole old project is copied here from commit `7700e16a8c`, upgraded to 6.6 as its own step,
  checked against the baseline, and then refactored area by area into this file's layout (section
  2). Why: track A showed the upgrade alone is a short, known list of breaks, the user wants a
  working app first, and a whole copy keeps every GUID reference. The code starts as
  `AssetsMelcher` / `Assembly-CSharp` and moves to the house layout as each area is refactored;
  the house rules apply to refactored code, not to untouched old code. Unreleased and unused
  content (NatureKast) stays behind. The steps, exclusions and checks are in
  `.claude/prompts/01-copy-and-upgrade.md`.

## Project overrides
The house rules were written for a game. This app is a designer with two scenes, server data and
three app builds, so the rules below replace or adjust the numbered rules they name. Decided on
2026-10-20. Rules not named here apply as written.

**Working with the user**
- **R-WORK-6 is replaced:** agree the approach before editing (see "How we work together").
  Recommend one option. Don't act on design-level choices until the user says to go ahead.
- **R-WORK-1 and testing:** the editor is driven with the Unity CLI (`unity command …`, `eval`,
  `run_script`) through the `com.unity.pipeline` package. kitwright, which the old project used,
  is not used here. The package (`0.8.0-exp.1`) is already in `Packages/manifest.json`
  (checked 2026-10-20).

**Git (R-GIT-2)**
- `tracked` since 2026-10-22 (user): R-GIT-1 no longer applies. CLAUDE.md, `.claude/prompts/` and
  the tool READMEs are committed with the work they describe. Nothing else under `.claude/` is
  staged (settings, worktrees and other Claude Code state); `.claude/settings.local.json` is
  already ignored by the user's global gitignore.

**Construction and naming (R-ARC-1, R-ARC-4, R-CODE-13)**
- The app has a menu scene and a designer scene, and some systems must live across both: the
  server client, the signed-in user, the app profile, the app rules and the product and cabinet
  data. One `AppInitializer` in a boot scene constructs these once and keeps them alive across
  scene loads. Each scene has its own `<Scene>SceneInitializer` (for example
  `DesignerSceneInitializer`), which receives them from the `AppInitializer` and builds that
  scene's systems in order, as R-ARC-1 describes.
- Still no singletons and no `Find…`. The old `Kitchen.instance`, `ProFitGlobals` and other statics
  become references passed through `Initialize(...)` when their area is ported. R-DATA-8 static
  stores are allowed only for data that really is app-wide, and each one is listed in "Key
  systems".
- Names drop the `Game` prefix: systems are `<Name>Status` / `<Name>Control`, and the ready event is
  `EventApp.OnSceneReady`. Kitchen terms (`Kitchen`, `CabinetSpan`, `CabinetBacking`, `Cabinet`)
  stay as they are in the old project, so the old CLAUDE.md still reads correctly against the new
  code.

**The three apps**
- Every difference between ProFit, Stoll and Gemini goes behind one app-rules layer: an interface
  in `{Root}.Shared.Interfaces` with one implementation per app, chosen once by the
  `AppInitializer`. Code asks the rules ("riser thickness for a high bar top"). It never checks
  which app it is. This replaces `ProFitGlobals.isAppOptionStoll` and the other flags. Every rule
  the old project hard-codes per app (the Stoll high bar top sizes, the Stoll price sheet) moves
  into this layer.

**Data and saves (R-DATA-1 to R-DATA-7)**
- **R-DATA-1:** config is the server's product, cabinet and manufacturer data, plus the catalogs and
  layouts that ship with the build. The player's "progress" is a saved design, and it is stored on
  the server, not in a local file.
- **R-DATA-2 does not apply.** There are no balance numbers. Sizes and prices come from server
  product data, and the app-specific exceptions live in the app-rules layer.
- **R-DATA-3 is strict here:** designs saved by the old app must still load. The ids a saved design
  records (product SKUs, the `brand_catalog` catalog name, layout and span data) and the server's
  JSON format do not change. A format change is a migration (R-DATA-6), checked against designs
  saved by the old app.
- **R-DATA-4, R-DATA-5 and R-DATA-7 apply only to local files** (the offline fallback data, cached
  downloads). The design format is the server's, not `JsonUtility`'s, so R-DATA-7's type limits
  apply only where `JsonUtility` is the serializer.

**UI**
- **uGUI first, UI Toolkit later.** The old uGUI screens are ported as uGUI, so the app works
  before its UI is rewritten. They move to UI Toolkit one screen at a time afterwards, when the user
  picks the screen. Decided on 2026-10-20.
- R-UI-1 to R-UI-7 and `Doge.UIToolkit` apply to screens already moved to UI Toolkit. A ported
  uGUI screen keeps its uGUI structure, but still follows the rest of the house rules: it is
  constructed by its scene initializer and talks to other systems through events, not
  singletons. Screens built from scratch are UI Toolkit from the start.
- "Key systems" lists every screen and whether it is uGUI or UI Toolkit, so the remaining work is
  visible.
- Clicks on the 3D kitchen (selecting cabinets, spans and backings) are "gameplay input" for R-UI-3.
  While uGUI and UI Toolkit screens are both in use, the input check has to block on both.

**Conformance check (section 6):** item 3 reads "`AppInitializer` or a `<Scene>SceneInitializer`".
Item 6 reads "no saved-design id or format changed without a migration; no app check outside the
app-rules layer".

## Open questions
- Own modified URP: **no, decided 2026-10-20 (user).** Stock URP, cutouts clipped in the shaders
  (`.claude/prompts/02-cutouts-by-clipping.md`). Still open: the old `UberPost` edit that kept alpha
  through post processing for transparent render textures needs its own 6.6 answer (check
  whether URP's own alpha processing setting covers it).
- When products are imported (into a test copy or this project), copy only a few, not the whole
  catalog (user, 2026-10-20).
- Where layout recipes live (Unity assets or server data). Open in the old plan. Content the team
  adds to the old project after the copy is brought over from `git diff 7700e16a8c..` in the old
  repo (the copy's source commit).
- Generated cabinets (user's idea, 2026-10-20, parked until cutouts are done): build the carcass,
  recess and toe kick from a resizable box instead of authored models, keeping authored models for
  unusual shapes. Claude's first view: generate that shape as geometry in code and keep clip
  cutouts for product holes. Not agreed; discuss after step 5.
- The folder layout for content (cabinet models, product visuals, finishes) inside
  `_projectAssets/`. Not agreed yet.

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
     kitchen size only. `CameraLevelAimBehavior` (see "Key systems") runs after it and re-aims
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

## Gotchas
- **Cabinet doors are found by name, not by GUID.** A GUID walk from a cabinet's visual never
  reaches its doors; they come through `CabinetSectionMappings/CabinetSectionDoor_<brand>.asset`
  (see the plan, step 1b correction). Any thinning or content tool has to follow that mapping.
- **The 6.6 editor writes its log to `kitchendesign/Logs/Editor.log`** and leaves a stub at
  `~/Library/Logs/Unity/Editor.log`, which the 2022.3 editor also uses. With both editors open,
  the old editor's log is lost; read the old app's errors through kitwright's console instead.
- **The Unity CLI's `eval`/`eval_file` take statements and local functions, no `using`s and no
  classes;** `run_script` takes a class but ends play mode. Measuring in play mode therefore uses
  fully qualified `eval_file` snippets.
- **Signing in to the old app from a background editor fails.** Entering play mode resets
  `Application.runInBackground`, the unfocused editor throttles, the large `components/all`
  download never completes, and the app logs "Error retrieving product or cabinet data" and signs
  out. Set `Application.runInBackground = true` after entering play mode, then sign in (about 40 s).
- **kitwright `execute_code` refreshes the AssetDatabase first** and times out (30 s) while the
  editor is importing; pass `skip_refresh: true` for snippets that don't need new files.
- The user's Claude Code permission for the kitwright helper names its old scratchpad path. The
  helper now lives at `Tools/Baseline/old2022/kw.sh`; before using it, ask the user to allow
  `Bash(/Users/emmanuel/Documents/Unity/kitchendesign/Tools/Baseline/old2022/kw.sh:*)`.
- **`-accept-apiupdate` rewrites source files** (it fully qualified the Cinemachine names in four
  files). Check `[API Updater] Updated Files:` in the log after any batch run, and diff before
  editing those files by hand.
- An asmdef with no scripts logs "will not be compiled, because it has no scripts associated with
  it". `KitchenDesign.Shared`, `.Events` and `.Debugging` show it until their first type lands.
  Harmless.
- Unity 6000.6.6f1 is installed without the WebGL, iOS and Android modules (checked 2026-10-20 with
  `unity editors list`). Install them before any build or WebGL measurement in 6.6.
- When fonts or UI come over from the old project, see "Upgrade test findings": the 10 TMP font
  assets need their script retargeted or regenerating, and `TMP Settings` must keep
  `m_defaultFontAssetPath: Fonts/` or every icon breaks. **Importing TMP Essentials (the prompt
  TMP shows, or Window > TextMeshPro) resets it too**; check the path after any such import. When cameras come over, run the
  Cinemachine upgrader before changing any field types.
- After a fix to an asset that other assets reference (fonts, settings), the editor can keep the
  broken objects in memory. Unload unused assets and reopen the scene before judging the fix.
- **A scene loaded at runtime has no skybox ambient in 6.6** unless it has baked lighting data
  or something calls `DynamicGI.UpdateEnvironment()`. In edit mode the same scene looks right
  (bake-on-load fills the probe), so check `RenderSettings.ambientProbe` in play mode, after the
  scene was loaded the way the app loads it. Any new scene with a skybox needs the same care.
- **`CinemachineVirtualCameraBase.UpdateCameraState` runs at most once per frame** (it goes
  through `CameraUpdateManager`). Several calls in one `eval` are one update; to test settings in
  a loop, call `InternalUpdateCameraState` and read `State.RawOrientation *
  State.OrientationCorrection`. In `eval`, CM extension methods (`GetCorrectedOrientation`)
  don't resolve; write them out.
- **Signing in rewrites `Resources/<App>/FallbackJSON/*.json` in this project too** (the app
  saves the server's data as its offline fallback). Back them up before any play-mode sign-in. On
  2026-10-20 a cutout-prototype sign-in (ProFit account) rewrote `ProFit/FallbackJSON`
  `manufacturers.json` and `products.json`, which already had uncommitted changes, without a
  backup; `productGroups.json` came out identical to git. The state after that sign-in is in the
  session scratchpad only. A second sign-in later that day (nested test) was backed up first and
  `products.json` was restored afterwards; the server's ProFit products had changed in between
  (American Made Grills entries), so a sign-in brings in whatever the server has now.
- **The CLI's `capture_game_view` writes under `Assets/`** whatever `save_path` says (an absolute
  path becomes `Assets/<path>`), which imports the PNG. Use `Tools/Cutouts/shot.sh`, which renders a
  camera to a file anywhere.
- **The working tree's `AppProfile.asset` decides which account the quick sign-in uses.** The
  baseline designs belong to the Gemini quick sign-in; with another app selected, `load.tmpl`
  answers "not found".
- **Switching a material to a Shader Graph assigns the graph's default textures** to texture
  slots the material leaves empty. Unity's "Lit Basic" template has `default_masks` as the mask
  map default; on `Kitchen_Default` that turned the Napoleon cabinet frames black in the save
  photos (2026-10-20), while a fresh test material looked right. `KitchenLitGraph` no longer has a
  mask default. After any shader switch, check the material's texture slots and compare a real
  save photo, not only a test object.
- **Swapping a material's shader at runtime and back is not a fair A/B**: leftover keywords from
  the other shader change the result (a countertop turned green). Compare fresh materials, or
  reset `shaderKeywords` and `renderQueue` after each swap.
- **The app re-applies the kitchen's finish** when products are added and when photos are taken,
  undoing a finish set from a snippet.
- **Saving `CutOut.prefab` or `ProductVisualTemplate.prefab` reimports all ~900 product
  visuals.** A CLI `eval` that saves either one answers "Main thread operation timed out after
  5000ms" while the work goes on; wait for `unity status` to say `ready` and check the result
  with a second snippet. During that reimport the door visuals' `OnValidate`
  (`ValidateCabinetDoorValues`) logs a few hundred "Failed to load product visual" errors for
  cabinet visuals that exist; they load fine afterwards. Transient, not a broken reference.
- **The Unity CLI has no `refresh` command** (`unity command refresh` answers "Command Not
  Found", and `import_asset` failed on `AppProfile.asset`). After restoring files from a backup
  or swapping `AppProfile.asset`, run `UnityEditor.AssetDatabase.Refresh()` in an `eval_file`.
  Entering play mode refreshes too, which is why the baseline runs still used the swapped profile.
- **The CLI's main-thread limit is 5 s per snippet.** About 60 test renders, or decoding every
  baseline photo in one call, times out; split by material or by design.
- **Opening the project in batch mode rewrote `AddressableAssetsData/AssetGroups/ProductMaterials.asset`**
  (2026-10-22): Addressables dropped 49 read-only entries (`Kitchen_Default.mat` and product
  display materials) on load. Not from any code change; restored from git. After any batch run,
  check `git status` for Addressables group changes before staging. It happened again on 2026-10-22 when
  the user reopened the editor (the same 49 entries); restored from git.
- **The shell's `grep` is ugrep**, and its `--include` returned nothing on asset searches without
  an error. Use `find … | xargs /usr/bin/grep` for searches over scenes, prefabs and assets.
- **DOTween manages its own asmdef.** `DOTween.Modules.asmdef` exists only while
  `DOTweenSettings.createASMDEF` is 1; with 0, DOTween's asset postprocessor deletes the file on the
  next editor start (`ASMDEFManager.ApplyASMDEFSettings`). Change it through DOTween (Utility Panel,
  or the setting plus `ApplyASMDEFSettings`), never by writing the asmdef by hand.
- **Renaming a type by script also renames it inside string literals and property names.** In
  B2.3 a plain word-boundary rename would have turned the path `Resources/AppProfile.asset` into
  `AppProfileSO.asset` and `public AppProfile AppProfile { get; }` into `AppProfileSO AppProfileSO`.
  Rename outside string literals only (the code inside `{…}` of interpolated strings excepted), and
  compile to catch renamed members.
- **Type names stored as text break on a namespace or assembly change.** Scenes, prefabs and
  `.asset` files bind scripts by GUID, but UXML (`<uie:EnumField type="Ns.Type, Assembly">`),
  `[SerializeReference]` data, UnityEvent argument types and `MelcherSignal` argument types store
  `Namespace.Type, Assembly`. When code moves namespace or assembly in phase B, search every text
  asset type (`find … | xargs /usr/bin/grep`), not only scenes, prefabs and assets.
- **A freshly opened editor can photograph finishes without their textures.** After batch
  compiles and a new editor start (2026-10-22), three designs' cover photos in the first
  `runall.sh` drew cabinets with a white noisy texture and black countertops (2.4–19.5/255), while
  every other photo matched; on the warm editor they matched (≤0.14). Re-capture before treating a
  cover-photo difference as a regression. The CLI's `console` keeps only the last 2,000 entries;
  read errors from `Logs/Editor.log`.
- **Don't start a scene load from the first scene's `Awake`.** That scene doesn't count as loaded
  yet, so `MelcherSceneManager` doesn't register it for the unload and logs "appears to have been
  unloaded from outside of MelcherSceneManager". `AppInitializer` loads from `Start`.
- **`EditorSceneManager.playModeStartScene` makes 6.6's Hierarchy window log 4
  `Assertion failed on expression: 'manager != NULL'`** on every play when the start scene isn't
  the open scene. Hence the `StartAppForEditor` prefab hook instead.
- **`SplashScene` is switched off in the build list** (the old project's too): the app starts at
  `PlatformSetupScene`. Played on its own in the editor, Splash's `buildIndex + 1` asks for a scene
  that doesn't exist and it stays on the splash. Test start-up from `PlatformSetupScene`
  (`Tools/Baseline/unity6/coldstart.sh`).
- **The Unity CLI logs its own timeouts as console errors**: "Failed to handle /api/exec request:
  Main thread operation timed out after 5000ms" when an `eval` lands on a heavy frame (sign-in
  parsing). It adds 1 to the check scripts' error counts now and then; `menucheck.sh` lists the
  errors after sign-in so it can be told apart.
- **Two Techobloc floor visuals have an "è" in their path, stored two ways.** The visual assets
  hold the content address composed (U+00E8, as in the file name); the Addressables entry holds
  it decomposed (U+0300, as macOS gives paths). `AssetAddress.OnBeforeSerialize` replaces the
  address with the entry's whenever they differ and logs "Updating stale asset address" (2
  warnings), once per load of those assets from disk: at editor start and after anything that
  reloads them (a script rename of their class, for example). The asset files never change, so
  it repeats. Harmless, but it adds 2 warnings to a run, and any comparison of addresses must
  normalise them first.
- **Photo comparisons:** Python here has no Pillow. Compare JPGs in an `eval_file` with
  `ImageConversion.LoadImage` and `GetPixels32`.

## Scene and prefab map
Build list (2026-10-22): `BootScene` (0), `SplashScene` (switched off), `PlatformSetupScene` (1),
`MenuScene` (2), `DesignerScene` (3), `IntermediateDesignerToMenuScene` (4).
- `_projectAssets/Scenes/BootScene.unity`: one root, `AppInitializer` (an instance of
  `_projectAssets/Prefabs/AppInitializer.prefab`, `AppInitializer` component, `appProfile` →
  `Resources/AppProfile.asset`). Moves itself to `DontDestroyOnLoad`.
- `MenuScene`: root `MenuSceneInitializer` (first sibling). `DesignerScene`: root
  `DesignerSceneInitializer` (first sibling). The rest of both scenes is the old app's, except
  that `DesignerScene`'s root `Product Visual Content Loader` object is gone (B3.3; its job is
  `ProductVisualContentStatus`).
- In the editor, playing any other build-list scene gets an `AppInitializer` from the prefab at
  `BeforeSceneLoad` (`StartAppForEditor`), and that scene plays as itself.

## The Initialize() chain
`AppInitializer` (`_project/Scripts/Game/`), B1.2 of the refactor (2026-10-22):
1. `Awake`: culture en-US, log level and `isVerbose` (editor, debug build, `?verbose`,
   `--verbose`), quality level 0, render debug UI off, `DOTweenInit.Init` (capacity). First,
   because everything after it logs, formats numbers or tweens.
2. `Awake` (B1.3): `LaunchOptions` first (the log level and DOTween's debug mode depend on it),
   then the `IAppRules` for the profile's `userFilter` (product parsing reads the padding rule),
   then construct the `ServerClient`, `CatalogDataStatus`, `ClientListStatus`, `SessionStatus` and
   the `ProductVisualContentStatus` (B3.3) and hand all of them to `ProFitGlobals.Bind` before
   initializing anything, because old code still reads the forwards. The content status is
   app-wide only because the designer's start-up (`DesignerScene.OnEnable` subscribes to it)
   runs before a scene initializer can hand anything over; it holds nothing outside the designer
   (see step 4). Then `session.Initialize()`, `clientList.Initialize()`,
   `catalogData.Initialize()`: the order the old `ProFitGlobals.Init` reset its fields in (the
   session's reset writes user 0's kiosk preference, as it always did). The catalog starts loading
   the fallback data, then the server's; the screens wait for it through
   `ProFitGlobals.ExecuteWhenReady`.
3. `Start`, boot scene only: load build index 1 (`PlatformSetupScene`). Not in `Awake`, see
   Gotchas (`MelcherSceneManager` and a load from the first scene's `Awake`).
4. On every scene load: the root `SceneInitializer` of that scene gets `Initialize(appProfile,
   appRules, productContent)`, then `EventApp.OnSceneReady` is raised. `DesignerSceneInitializer`
   purges the content status in its `OnDestroy`, when the designer scene goes: the moment the old
   loader component released its Addressables handles. This runs after the old scene script's
   `OnStateAdded` (the core package's state machine is subscribed first), so the old scene
   scripts can't use what the initializer receives yet; they keep using `ProFitGlobals`.

## Events
- `EventApp.OnSceneReady` (`ActionEvent`, `_projectEvents/Events/EventApp.cs`): after a scene's
  `SceneInitializer` was initialized. No listeners yet.
- Everything else still goes through the old `EventBridge`.

## Key systems
- **Assemblies (2026-10-20):** `KitchenDesign` (`_project/Scripts`, refs Events, Shared, Doge),
  `KitchenDesign.Events` (`_projectEvents/Events`, refs Doge, Shared), `KitchenDesign.Shared`
  (`_projectShared`, refs nothing), `KitchenDesign.Debugging` (`_projectDebugging`, Editor only,
  autoReferenced off, refs KitchenDesign, Shared). All reference by name. `KitchenDesign` also
  references `Unity.Cinemachine` (2026-10-20, for `CameraLevelAimBehavior`).
  **Since phase A of the refactor (2026-10-22) all the old code is in our assemblies and namespace**,
  files not moved yet: `.asmref` files put `AssetsMelcher/Scripts`, `/Miscellaneous`,
  `/Plugins/WebGL`, `Procedural Assets` and `Cloder` into `KitchenDesign`; the old editor tools
  (`AssetsMelcher/Editor`, and `Cloder/Editor` through an `.asmref`) are
  `KitchenDesign.Tools.Editor` (Editor only). The namespace is `KitchenDesign` throughout (sub
  namespaces come with phase B); every old file has `using Melcher;` for the core package, which
  keeps `namespace Melcher` until it is replaced by our own library. `KitchenDesign` references the
  core package, MEC, MiniJSON, UniTask (+Addressables), `DOTween.Modules`, `RecyclingListView`,
  TMP, uGUI, Addressables (+ its editor assembly, used under `#if UNITY_EDITOR`), URP and
  `NativeGallery.Runtime`; the list was trimmed to what the compiled assembly uses. `Assembly-CSharp`
  holds only third-party code. Details and checks: `.claude/prompts/03-refactor.md`, phase A.
  `KitchenDesign.Products.Editor` (B3.4, `_project/Editor/Products/`, Editor only, autoReferenced
  off, namespace `KitchenDesign.ProductsEditor`): the product visual tools; refs KitchenDesign,
  Shared, the core package and its editor assembly, MEC, MiniJSON, Addressables (+ editor),
  ResourceManager. `KitchenDesign.Tools.Editor` references it (the finish material generator uses
  `FilteredObjectSelector`, the FBX importer `ProductEditorOptionsSO`); nothing points back.
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
    `ProductVisualContentStatus` (plain class, constructed by `AppInitializer`; old code reaches
    it as `ProFitGlobals.productContent`) loads a visual's content prefab through Addressables
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
    `ProFitEditorOptions`; the asset `AssetsMelcher/Editor/ProFitEditorOptions.asset` is found by
    type). Comments in these files stay: they were moved, not refactored. **The FBX import
    postprocessor (`ProFitProductModelFbxImporter`) stays in `KitchenDesign.Tools.Editor` on
    purpose:** Unity counts the model postprocessors (type and version) among each model's import
    dependencies, so moving or renaming it is expected to reimport every model and run its material
    remap again (not measured; avoided). Move it only together with a planned model reimport.
  - Checked with `Tools/Baseline/unity6/visualscensus.cs` (edit mode): every source visual's
    content address and every public value of its info, written to
    `GeminiBaseline/visualscensus-latest.txt` with a hash; it finds the types by name, so it
    works across the renames. Reference before area 3: 901 entries, hash `D72B3E9CB18746DF`
    (output in Unicode NFC, see Gotchas).
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
- **Server client** (B2.2 of the refactor, 2026-10-22), `_project/Scripts/Server/`
  (`KitchenDesign.Server`); the old static `ProFitWeb` is gone. `ServerClient` (constructed by
  `AppInitializer` before the catalog data and the session, which get it in their constructors;
  old code reaches it as `ProFitGlobals.server`) owns:
  - `ServerConnection`: the server address from the profile, `UseHttps` (starts true;
    `ProbeCoroutine` flips it once if the first request to the host fails, then tries again),
    `IsTestServer`, the domains the WebGL build may be embedded in, and the editor-only
    `SaveFallbackJsonResource`: in the editor every successful catalog request (manufacturers,
    categories, groups, products, brand catalogs, cabinets, tags, compatibilities) rewrites
    `Resources/<App>/FallbackJSON`, which is how the offline data stays current (and why a sign-in
    changes those files; see Gotchas).
  - One request class per server resource: `AccountRequests`, `ClientRequests`,
    `QuoteRequests`, `CatalogRequests`, `AnalyticsRequests`. An offline user (`UserType.Offline`)
    gets simulated sign-in and profile responses and reads products and cabinets from the fallback
    JSON; analytics are skipped for it.
  - Static, pure parsers next to them: `AccountJson`, `ClientJson`, `QuoteJson`, `CatalogJson`
    (the JSON key strings in them are the frozen format, R-DATA-3), `ServerJson`
    (`ValidateJsonResponse`: a JSON `error`, an `errors` map (its keys are shown only in debug
    builds), parser errors or a status other than 200 fail the request), `CatalogLookups`
    (lookups in a given list), `FallbackJsonFiles` (the fallback file names).
  - The product and cabinet parsers take the product groups, the products already loaded and the
    `IAppRules` as arguments. `CatalogRequests.GetProductList`/`GetCabinetList` take them as
    `Func<…>` providers, read when the response is parsed: that is when the old code read them,
    and the server refresh can replace the lists in between. Why the cabinet parser needs the loaded
    products: Stoll's cabinets come from the fallback JSON without server ids, and each gets an id
    below the smallest one in use. Manufacturers without an `order` (a server bug for newly added
    ones) sort as -1.
- **Server data types** (B2.1 of the refactor, 2026-10-22): `_project/Scripts/Server/Data/`
  (`KitchenDesign.Server`): `User`, `UserProfile`, `UserClient`, `UserClientQuote`,
  `ManufacturerInfo` (+ `ManufacturerDoorStyleInfo`), `ProductGroupInfo`, `ProductTagInfo`,
  `CabinetCompatibilityInfo`, `BrandCatalogInfo`, `CabinetSectionCategoryInfo`; `ResponseCode`
  and `MeasurementUnitExtensions` in `Server/`. Enums `ServerType`, `UserType`, `UserFilter`,
  `Currency`, `MeasurementUnit` in `_projectShared/Enums/`, values as before (the first three are
  serialized as ints in `AppProfile`/`AppOptions`). None of these is serialized by name: the
  parsers read the server's JSON by key, so **the JSON key strings in the parsers are the frozen
  format (R-DATA-3), not the C# names**. `User.userType` is set by the app, not sent by the
  server. `UserProfile`: `dealerSelectedManufacturerIds` is the JSON `manufacturers`,
  `adminSelectedManufacturerIds` is `whitelisted_manufacturers`; `whiteLabelReturnUrl` is where a
  white label user goes after saving a design.
- **Generic helpers** (B1.4 of the refactor, 2026-10-22). `KitchenDesign.Shared.Common.Units`
  (`_projectShared/Common/`): inch/metre/centimetre/foot conversions, the old `Const.Inches2Meters`
  and friends. `_project/Scripts/Utility/` (`KitchenDesign.Utility`, called as
  `Utility.<Class>.<Method>` from old code):
  - `MeasurementFormat`: metres to centimetres or feet/inches text, and parsing typed imperial
    sizes. Why the quote tables: phones and pasted text produce typographic quotes (‘ ’ “ ”), which
    are normalised to ' and " before parsing.
  - `UrlUtility`: `GetDomain` (host of a URL, a scheme added if missing) and `GetBaseDomain`
    (the last two labels, or the IP). Self-tests: `Tools/Kitchen Design/Tests/…`
    (`_projectDebugging/Menus/UrlDebugMenu.cs`).
  - `RuntimePrefabs`: a "runtime prefab" is an inactive copy in `DontDestroyOnLoad` that stands
    in for a prefab asset at runtime; `IsPrefab` treats those and real prefab assets (including the
    prefab stage) alike. `DontDestroyOnLoad` must come after `SetActive(false)`, or some components
    of the copy stay active (the product visuals' editor-only lights did).
  - `RendererUtility`, `RenderQuality` (switch to the quality level that uses a given URP asset),
    `MemoryUtility` (a GC on iOS only, where low-memory devices otherwise run out).
  - `RandomNamesForEditor` (editor only): random names and places to fill the sign-up and client
    forms while testing.
  `KitchenDesign.Tools.Editor` now references `KitchenDesign.Shared` (for `Units`).
  What stays in `Const` and `ProFitUtility` until its area: the plan, B1.4.
- **App-wide systems** (B1.3 of the refactor, 2026-10-22). Plain classes, constructed and owned
  by `AppInitializer`; collaborators come in through the constructor.
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
  - **`ProFitGlobals` is now only forwards** to these systems, the profile and the rules, plus
    the system instances themselves (`catalogData`, `clientList`, `session`, `launchOptions`). It is
    the one app-wide static store of this area (reset at `SubsystemRegistration`). New code doesn't
    use it; each area replaces its own uses with references passed in, and the class goes when no
    one uses it. With no `AppInitializer` running (edit mode, test scenes), the forwards create
    default instances, and `appProfile` loads `Resources/AppProfile` once.
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
  `QuickLogInWidget`) use it as the app's identity on the server, not as a rule. Old code reaches
  the rules through `ProFitGlobals.appRules` (handed over by `AppInitializer`; in edit mode
  created from the profile on first use). Why rules return `null` for "leave the value alone" (the high bar top
  sizes, fridge padding, bottom inset): only Stoll overrides them, and the caller keeps its own
  value otherwise. Each app has exactly one building method today (ProFit Standard, Stoll
  Sections, Gemini Catalog), but app and building method stay separate axes: what the old code
  tested by building method (the Sections bowing limit, the 0.5" overhang) stays a building-method
  rule, and the per-brand checks (`Const.Gemini.ManufactersIds`) belong on `BrandCatalog` (area 4).
  Why two manufacturer rules (`IncludesStollManufacturer`, `IsManufacturerIdAvailable`): the
  whitelist screen hides only Stoll, the toolbar also hides 137. Checked with
  `Tools/Baseline/unity6/rulescheck.cs` (all three apps) and the Gemini baseline.
- **`CameraLevelAimBehavior`** (`_project/Scripts/Behaviors/`, 2026-10-20): a `CinemachineExtension`
  that, at the Finalize stage, takes the framed direction, moves the aim point by
  `worldAimOffset` (world space, at the Look At target's distance) and re-aims with the reference
  up, replacing the orientation correction. Why: it reproduces CM2's Group Composer aim (centre +
  offset, no roll) on top of CM3's Group Framing; see "Upgrade fixes in the copy", save-photo cover
  camera. Its offset is set to the vcam's `RotationComposer.TargetOffset`. Verified against the
  8 baseline designs. The first type in the house layout that old code depends on, through the
  scene only.
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
  - `KitchenDesign.KitchenCutoutClipping` (`AssetsMelcher/Scripts/`, owned by `Kitchen`): rebuilds
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
- Screens: none yet.

## Tools
- `Tools/Baseline/`: records the old app's baseline and compares this project against it
  (`unity6/runall.sh` is the whole check), and `unity6/menucheck.sh` walks the menu screens and
  the designer round trip read-only. Notes in `Tools/Baseline/README.md`.
- `Tools/Cutouts/`: the cutout prototype's driver and test snippets (sign in, load a design, add
  products, apply boxes, render a camera to a PNG). Notes in `Tools/Cutouts/README.md`.

## Not built yet / handover
- Track A: done 2026-10-20 (summary at the end of "Upgrade test findings"). The copy at
  `/Users/emmanuel/Documents/Unity/GeminiUpgradeTest/` is throwaway and can be deleted; its
  captures are in `Captures/` and `Assets/Captures/`. A real kitchen in the designer was not
  reached because offline guest (ProFit) has no data for the kept brands; signing in from
  `MenuScene` would need a fuller copy.
- **The copy is done (2026-10-20)**, from commit `7700e16a8c`, step 1 of
  `.claude/prompts/01-copy-and-upgrade.md` (counts and checks there). Committed as `131b0020`
  ("Initial project clone"). The old repo's `.gitattributes` came over (26,787 files there are in Git LFS) and the
  user installed LFS here. Step 3's compile fixes are done (see "Upgrade fixes in the copy").
  **Next:** the user asked (2026-10-20) for only a few hundred products in the copy, so the full
  import was stopped. The baseline (step 2) is done. Step 1b is done: 899 of 8,552 database
  entries stay (the baseline designs' cabinets with their door styles and mapped doors, their
  products plus all Twin Eagles and Bull, all finishes); the rest is in
  `/Users/emmanuel/Documents/Unity/kitchendesign-held-back/`, nothing deleted (details in the
  plan). The whole Challenger catalog was not kept: 1,012 visuals on its own.
- **Step 3 (upgrade) and step 4 (check) done 2026-10-20.** First import 145 s (thinned copy).
  The app runs in the 6.6 editor, signs in and loads saved designs; all 8 usable baseline designs
  match the old app in every measurement and in the save JSON (plan, step 4 result).
- **Darker render and save-photo cameras: done 2026-10-20** (details in "Upgrade fixes in the
  copy": ambient light, save-photo cover camera). The runtime ambient probe was black;
  Cinemachine 3 had mixed up the two brains' vcams, and Group Framing tilted and rolled the
  aim. After the fixes `runall.sh` shows `resave.json` identical in all 8 designs and the only
  measurement differences in cameras; every save photo is close to the old one (cover framing
  within about 0.5°, others 0–3.3/255 mean pixel difference, the rest is the missing cutouts).
  Changed: `DesignerScene.cs` (`DynamicGI.UpdateEnvironment()`), `KitchenScreenshotManager.cs`
  (vcam lookup), `DesignerScene.unity` (channels, cover vcam moved, `CameraLevelAimBehavior` on two
  vcams), new `CameraLevelAimBehavior`, `KitchenDesign.asmdef` (+`Unity.Cinemachine`),
  `Tools/Baseline/compare.py` (cameras matched by name).
- **Open, found 2026-10-20, live orbit view (not the photos), present before these fixes:**
  `MainCamera`'s FOV is 10–20% narrower than the old app's (e.g. 27755: 19.8 vs 23.1; check
  `DesignerOrbitCamera`'s framing-size tween and `OrbitView`'s Group Framing damping 1 against
  CM2's frame damping), and in 3 of 8 designs the orbit starts at yaw 0° where the old app had 45°
  or the reverse (32569, 44392, 36449). These looked like the previous design's orbit angle
  carried over, but 28316 doesn't fit that, so it's unconfirmed. Ask the user whether to chase
  these next or go to cutouts. Clue (2026-10-22): after reopening the editor, the first
  design loaded (27755) started its orbit at yaw 89° instead of 45°, and the next run on the warm
  editor was back to 45°, so the start angle depends on something left from earlier (or not yet
  initialized) rather than on the design.
- **Cutouts by clipping: prototype built 2026-10-20** (results in `.claude/prompts/02-cutouts-by-clipping.md`,
  step 1). The user gave the go on the look (2026-10-20). The nested case (a burner in a plunge)
  was tested the same day and works; the two plunge visuals are back in the copy (901 database
  entries). Steps 2 and 4 done (2026-10-21): wired into the app, all 8 baseline designs save
  identically, photos match. Step 3 done 2026-10-21 (old stencil path removed; baseline
  unchanged, see the plan). The three non-clipping materials were switched on 2026-10-21 (see
  "Key systems: Cutouts"; committed in `ba666e23`). Next, when the user asks: WebGL
  and phone, then the UI blur, the orbit camera, the `UberPost` alpha. WebGL and phone measurements are
  parked: the user will add the 6000.6.6f1 build modules later (2026-10-21); ask before starting.
  (The 2022.3.62f2 editor has WebGL, iOS and Android if the old app's numbers are ever wanted.) Was: step 2 (wire `CutoutClipSet` into `Kitchen`/`CabinetCutOut`, a
  clip-capable shader for `Kitchen_Default`, the local keyword). The editor was left in play mode
  on design 72249 with the boxes applied (`CutoutTestCam` shows the cutouts).
- Later: removing the unused `_projectDefaults/Settings` URP
  assets, checking the ambient fix in a player build. The user makes the commits (LFS is set up).
- **The baseline (old plan step 2), proposed 2026-10-20, waiting on the user's answers.** Per
  saved design, kept outside both repos (design names and JSON contain client names; proposed
  `/Users/emmanuel/Documents/Unity/GeminiBaseline/`): `server.json` (the design as stored, from
  the client list), `resave.json` (what `Kitchen.SaveToJson()` builds after loading it, never
  sent), `measured.json` (span, cabinet, product and backing sizes and world positions, ground and
  photo cameras; metres to 0.1 mm plus inches), `photos/` rendered now, and optionally the
  server's photos and `pdf` price sheet. The capture script goes in `Tools/Baseline/`, its notes
  in `Tools/Baseline/README.md`. Open questions to the user: which designs (or let Claude propose a
  covering set from the client list), which app (the old working tree has `AppProfile` set to
  ProFit as an uncommitted change), which sign-in, whether to download the server's photos and
  PDF, and the location. Never create clients or save designs on the server.
- **Prices aren't totalled by the app.** `SaveToJson()` sends the price inputs (`cabinet_list`,
  `manual_price_sheet_entries` with their prices, `price_changes`, `countertop_area`) and the
  server builds the price sheet PDF (`pdf` on each saved design). The rebuild's check is that
  these inputs match exactly.
- **The baseline run (2026-10-20).** The user stashed all their uncommitted changes in the old
  repo (`stash@{0}`, "stash XXX"), so it ran at commit `7700e16a8c` with the Gemini profile and
  the full database (8,553 entries). Their earlier state is also backed up in
  `/Users/emmanuel/Documents/Unity/GeminiBaseline/_guard/`. Signed in with the Gemini quick
  sign-in (a dealer account with 81 clients and 102 saved designs, all Catalog building method;
  the largest has 15 cabinets, so the 25-cabinet stencil question can't be tested from saved
  designs). Ten designs, chosen to cover layouts, catalogs, backings, end panels, flooring and
  products: 27755, 32569, 43352, 28316, 27763, 27764, 27740, 44392, 36449, 44917. Output per design
  in `GeminiBaseline/designs/<quote id>/`, summary and per-design notes in
  `GeminiBaseline/index.json`. **Done 2026-10-20: 8 usable designs.** 43352 (Galley, Berlin
  Gardens, the only one with end panels) fails to load in the old app at this commit (1 of 3 spans;
  "No replacement cabinet … index 5"), so end panels have no usable baseline. 44917 can't load at
  all (no `WeatherStrongSelect` catalog at this commit). On Challenger the low bar top has no riser
  or renderers; it is the deeper overhang of the span's cabinet countertops. Afterwards the old app
  had rewritten `Fonts/OpenSans/OpenSans-Bold.asset` and five `Resources/Gemini/FallbackJSON` files;
  the user discards those with `git restore Unity/` and keeps their own changes in the stash for
  now. Each design is opened exactly as Edit Design does
  (`activeClientId`/`activeQuoteId`, then load `DesignerScene`), photos are taken as F6 does
  (`TakeScreenshots`, which re-centres the ground first), then measured. Scripts:
  `Tools/Baseline/` (notes in `Tools/Baseline/README.md`).
- **The server's copy of a design has no price inputs.** In the client list, `cabinet_list`,
  `manual_price_sheet_entries`, `price_changes` and `countertop_area` are absent, and older
  designs' `islandDimensions` are in the 2024 per-cabinet format. So `resave.json` (what today's
  app would send) is the price and measurement baseline; `server.json` is only the input.
- **Driving the old 2022.3 editor from here:** kitwright is only in the old project's
  `Unity/.mcp.json` (`http://127.0.0.1:8765/`, listening only while that editor is open), so it is
  reached over HTTP from Bash rather than added to the old project. Entering play mode there
  rewrites `Resources/<App>/FallbackJSON/*.json`, which already carry the user's uncommitted
  edits, so files are hashed and backed up before and restored from that backup, never from git.
- Done 2026-10-20: the template content in `_projectDefaults` was removed (sample scene, removed
  from the build list; `InputSystem_Actions`, cleared as project-wide actions; Readme,
  TutorialInfo, `SampleSceneProfile`). `Settings/` (URP assets) stays. The track A copy was
  deleted. A Unity `.gitignore` was added.
- **Refactor plan agreed 2026-10-22:** `.claude/prompts/03-refactor.md` (step 6 of the copy
  plan, with the `Melcher` → `KitchenDesign` namespace change; the core package keeps `Melcher`
  until we replace it with our own library). Phase A moves the old code into our assemblies and
  namespace unchanged, in one session; phase B refactors area by area. First, all code missing
  from the old repo comes over (audit in the plan): forced product occupancy is in; the user's rear wall (`77f53840ee`) comes over later, once the user has finished it
  in the old project (user, 2026-10-22; the commit there is incomplete, no
  `ProductCategory.CabinetRearWall`).
- **Phase A of the refactor done and committed 2026-10-22** (four commits). A1 (third-party asmdefs), A2
  (`Melcher` → `KitchenDesign`, 239 files), A3 (old code in `KitchenDesign` /
  `KitchenDesign.Tools.Editor`). Each checked: compile (and a macOS player-script compile), baseline
  identical to the pre-rename run, menu check, editor tool windows. **Next: phase B, area 1 (app foundation).**
  Its section ("Area 1 — App foundation" in `.claude/prompts/03-refactor.md`) was agreed on
  2026-10-22; the user left names and refactoring details to Claude. `ProFitGlobals` keeps
  forwards until its last user is gone. Steps B1.1–B1.4 are each checked on their own and
  committed. **Area 1 is done and committed (B1.1–B1.4, 2026-10-22; results in the plan).**
  **Area 2 is done and committed (B2.1–B2.3, 2026-10-22).** **Area 3 is done and committed
  (B3.1–B3.4, 2026-10-22/23; results in the plan, "Area 3").** Next: area 4, the kitchen model,
  which the plan says gets its own plan (`.claude/prompts/04-…`) first: split `Kitchen` into the
  systems it is, replace `Kitchen.instance`, then spans and layouts, cabinets, occupancy and
  products; it also takes over `ProductVisualContent`, `CabinetType`, the door-style lookup and
  the brand rules (`Const.Gemini`, the padding numbers in `RuntimeProductOverrides`). The user
  left the design and the details to Claude and wants each step checked and committed on its own.
- The build modules (WebGL/iOS/Android for 6000.6.6f1) wait until the port runs properly; the user
  wants a working copy of the app in this project first (2026-10-20).
- **Disk space decides the module install (2026-10-21).** 55 GB free of 460 GB (88% used), and a
  release of the old project uses up space quickly, so the user will install the **WebGL module
  only, later, themselves**. One phone platform comes after, not all three at once. When they're
  back: ask whether WebGL is installed (`unity editors list`), then measure the cutouts on WebGL
  (uniform budget, compile errors, frame time). Sizes measured then (nothing deleted): old
  project `Gemini/` 43 GB, editors 2022.3.62f2 20 GB / **6000.3.9f1 20 GB (used by neither
  project; the biggest saving if the user doesn't need it)** / 6000.6.6f1 9.9 GB, this project
  13 GB, `~/Library/Unity/cache` 1.9 GB, `Builds/` 1.3 GB, `kitchendesign-held-back/` 1.2 GB.
  Never delete any of these unasked.
- Git is set up (see "What it is"). The notes are tracked (see "Project overrides"): of
  `.claude/`, only `prompts/` is ever staged.
