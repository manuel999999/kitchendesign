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
Update the notes in the same session as a change, before reporting done. Edit sections in place;
they describe how the project is, not a changelog. **Since 2026-11-13 the notes are split (user's
decision, to keep this file small because it is loaded whole in every session):** this file keeps
what every session needs (house rules, overrides, gotchas, scene map, Initialize chain, events,
tools, handover); each area's notes are in `Docs/systems/<area>.md` and the finished upgrade
history in `Docs/upgrade-to-unity-6.md` (index under "Key systems"). With `CODE_COMMENTS:
file-only`, new and edited code has no comments and its why goes in the area's file (or in the
chain, events or gotchas here). The old code's own comments stay in the code until that code is
rewritten: a comment beside its line is the cheapest place for a why, and moving about 8,700
comment lines into prose would lose detail. Don't record
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
- **Explanations go in the notes (this file and `Docs/systems/`), not in code comments.** In the old project the user asked for no
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
  old project hard-code `AssetsMelcher` paths (21 places). Findings are in
  `Docs/upgrade-to-unity-6.md`. Location: not chosen yet (2026-10-20).
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
- `Docs/systems/screens.md` lists every screen and whether it is uGUI or UI Toolkit, so the remaining work is
  visible.
- Clicks on the 3D kitchen (selecting cabinets, spans and backings) are "gameplay input" for R-UI-3.
  While uGUI and UI Toolkit screens are both in use, the input check has to block on both.

**Where the notes live (section 7)**
- Section 7's "Key systems" is an index here; each area's notes are a file in `Docs/systems/`
  (2026-11-13, user). Conformance item 8 reads "the area's file in `Docs/systems/` and, where
  they changed, the chain, events, gotchas and handover here are updated". `Docs/` is committed
  with the work it describes, like `.claude/prompts/`.

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

## Gotchas
- **Cabinet doors are found by name, not by GUID.** A GUID walk from a cabinet's visual never
  reaches its doors; they come through `CabinetSectionMappings/CabinetSectionDoor_<brand>.asset`
  (see the plan, step 1b correction). Any thinning or content tool has to follow that mapping.
- **The 6.6 editor writes its log to `kitchendesign/Logs/Editor.log`** and leaves a stub at
  `~/Library/Logs/Unity/Editor.log`, which the 2022.3 editor also uses. With both editors open,
  the old editor's log is lost; read the old app's errors through kitwright's console instead.
- **`Logs/Editor.log` grows without limit while the editor stays open** (7.4 GB on 2026-11-13
  after two days of check runs). A `grep` over it takes minutes; search the tail
  (`tail -c 50000000 Logs/Editor.log | /usr/bin/grep …`). It is replaced when the editor
  restarts. Pointed out to the user; not deleted.
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
- When fonts or UI come over from the old project, see `Docs/upgrade-to-unity-6.md`: the 10 TMP font
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
- **The Unity CLI also logs `Failed to handle /api/exec request: Thread was being aborted.`**
  when a CLI call is cut off by the domain reload at the start of play mode (found 2026-11-13,
  about 2 of 5 cold starts). It adds 1 to the console error count (14 instead of 13 on a cold
  start) and is missing from the CLI's own `console` listing; `Logs/Editor.log` has it right
  after "Begin MonoManager ReloadAssembly".
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
- **Write non-ASCII text in C# strings as `\u` escapes** (`"OASIS\u00AE 45\u00B0 ..."`). The old
  `Kitchen.cs` was saved in Windows-1252; old-repo commit `520d740c9d` (2025-11-26) re-saved it as
  UTF-8 and every `²`, `°` and `™` became U+FFFD, which the app then sent to the server for months
  (handover, "Needs attention"). An escape is plain ASCII and survives any re-encoding. Keep source
  files UTF-8 so they survive moves byte for byte. **The Edit tool turns a typed `\u2122` into the
  character itself**; write escapes with a script and check the bytes.
- **Compile player scripts for WebGL, not macOS** (2026-11-12). `ProjectSettings` defines
  `MELCHER_CORE_USE_TEXTCORE` for WebGL, iOS and Android only, and the core package's
  `MelcherUnicodeDictionary.m_Font` changes type with it, so a `StandaloneOSX`
  `PlayerBuildInterface.CompilePlayerScripts` reports "script class layout is incompatible
  between the editor and the player" while the editor's target is WebGL (since 2026-11-12). The
  CLI cuts the call off after 5 s but the compile goes on; wait for `unity status` ready and check
  the output folder and the console.
- **`kitchendatacensus.cs` includes in-memory instance ids** (2026-11-12): it enters object
  references, so each one's `m_FileID` (the instance id, e.g. `26401`) is in the output and the
  hash changes with every editor session. Compare its output only within one session, or drop the
  `m_FileID`/`m_PathID` lines first; the `guid:localId` line beside them is the stable part.
- **The edit probe's drag positions depend on the live camera** (2026-11-13). One run logged
  its drag steps 30 to 100 pixels away from every earlier run, with `MainCamera` at FOV 14.39
  instead of 21.37, while every snapshot was identical; the next run was back to normal. Cause
  not found (the live orbit FOV is the open item in the handover). Re-run the probe before
  reading a drag-position difference as a regression.
- **Photo comparisons:** Python here has no Pillow. Compare JPGs in an `eval_file` with
  `ImageConversion.LoadImage` and `GetPixels32`.

## Scene and prefab map
Build list (2026-10-22): `BootScene` (0), `SplashScene` (switched off), `PlatformSetupScene` (1),
`MenuScene` (2), `DesignerScene` (3), `IntermediateDesignerToMenuScene` (4).
- `_projectAssets/Scenes/BootScene.unity`: one root, `AppInitializer` (an instance of
  `_projectAssets/Prefabs/AppInitializer.prefab`, `AppInitializer` component, `appProfile` →
  `Resources/AppProfile.asset`). Moves itself to `DontDestroyOnLoad`.
- Every scene has its `<Scene>SceneInitializer` on the first root (B5.3b, `_project/Scripts/Game/`),
  holding a serialized reference to the scene's control and calling its `Initialize`:
  `SplashScene` → `SplashVideoControl` (the splash video, then the next build index),
  `PlatformSetupScene` → `PlatformSetupControl` (white-label check, then the menu or the
  designer), `IntermediateDesignerToMenuScene` → `MemoryFlushControl` (unloads unused assets and
  collects garbage, then loads the menu: low-memory iPhones ran out of memory going straight from
  the designer), `MenuScene` → `MenuNavigationControl` (`navigation`: every menu screen, the
  history, the prompts, screen orientation). The controls are the old scene scripts renamed in
  place (same GUID), on `MelcherMonoBehaviour` for the editor's `[MelcherFindComponent]` fill.
  `DesignerScene` → `DesignerScreensControl` (`screens`: the setup wizard, toolbar, navigation
  and loading screens, the cabinet swap, back to the menu, the product overrides; B5.4a). No scene
  uses the core package's state machine any more. `DesignerScreensControl` does the designer's
  frame work in its own `Update` (the debug keys) and `LateUpdate` (`Kitchen.LateTick`), and
  `KitchenSelectionClickControl` (B5.4b, `_project/Scripts/Game/`, on the same object, handed the
  kitchen and the navigation screen by `DesignerSceneInitializer`) does the 3D selection click in
  its `Update`; both have `[DefaultExecutionOrder(-10)]`, the order the state machine forwarded
  them at, so they still run before ordinary scripts. The click tests every cabinet's oriented
  bounds by hand instead of using colliders, because a collider box wouldn't know about the
  cut-outs (so the user can select through a cabinet; known, not solved). Occupants flagged
  `requireManualSelection` (shelves) are tested the same way, so a shelf's own collider doesn't
  hide the products inside it. A cabinet that can't be selected but has `canOccludeSelectionClick`
  still blocks the cabinets behind it. A procedural Catalog corner selects the blind cabinet next
  to it, which stands for the corner. In Standard a click on any cabinet selects the whole
  kitchen. Only one click is handled at a time (the handler was synchronous once and was never
  tested with two running). The rest of both scenes is the old app's, except
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
   the `ProductVisualContentStatus` (B3.3), collect them with the profile, the rules and the launch
   options in one `AppSystems` (B5.2), and give the profile to `AppProfileStore.Bind` (B8.4)
   before initializing anything, because views read the skin from it in `Awake`. The content status is
   app-wide only because the designer's start-up (`DesignerScene.OnEnable` subscribes to it)
   runs before a scene initializer can hand anything over; it holds nothing outside the designer
   (see step 4). Then `session.Initialize()`, `clientList.Initialize()`,
   `catalogData.Initialize()`: the order the old `ProFitGlobals.Init` reset its fields in (the
   session's reset writes user 0's kiosk preference, as it always did). The catalog starts loading
   the fallback data, then the server's; the screens wait for it through
   `CatalogDataStatus.ExecuteWhenLoaded`.
3. `Start`, boot scene only: load build index 1 (`PlatformSetupScene`). Not in `Awake`, see
   Gotchas (`MelcherSceneManager` and a load from the first scene's `Awake`).
4. On every scene load: the root `SceneInitializer` of that scene gets `Initialize(AppSystems)`,
   then `EventApp.OnSceneReady` is raised. Why one object rather than one argument per system: a
   scene initializer hands each screen only what it uses, and adding an app system then changes
   one constructor instead of every initializer's signature. `DesignerSceneInitializer`
   purges the content status in its `OnDestroy`, when the designer scene goes: the moment the old
   loader component released its Addressables handles. This runs after the old scene script's
   `OnStateAdded` (the core package's state machine is subscribed first). Since B5.3b the
   menu-side scenes have no state: their start-up is the control's `Initialize`, called from the
   scene initializer, so it now runs after the load request's `onLoaded` callbacks (only the kick
   prompt uses one, on the intermediate scene, with nothing listening either way). The designer
   followed in B5.4a: `DesignerSceneInitializer` calls `DesignerScreensControl.Initialize`.

5. `DesignerSceneInitializer.Initialize` (B5.5c, B8.1): first `Kitchen.Initialize(systems)`,
   which builds the kitchen's parts (widgets, camera, materials, drag, save writer and reader),
   hides the ground and makes the two shelf resize widgets: what `Kitchen.Awake` did until
   2026-11-13. It comes first because everything after it may call into the parts. Between the
   scene's `Awake`/`OnEnable` pass and this call the kitchen has its selections and events but no
   parts, so nothing may call into them from `Awake`/`OnEnable`; `Kitchen.OnEnable` hooks the
   shelf widgets only once they exist. Then `MeasurementOverlayManager.Initialize(session)`:
   its `Awake` runs its restart handler before this call, so the handler sets the measures' units
   only when it has the session, and `Initialize` sets them (reading the session there
   unguarded threw in `Awake` and disabled the component, found by the listener-order check). Then every scene-load listener's
   `ListenTo(kitchen.events)`, in the fixed order given under "Events"; then
   `DesignerScreensControl.Initialize`, which hands the screens their kitchen, systems and
   prompts and then starts the designer (`restart`, at once when the catalog data is already
   loaded); then the selection click. Inside it the brand catalog screen is initialized first,
   then the dev screen, then the toolbar and the rest (B8.3): each of these queues work on
   `CatalogDataStatus.ExecuteWhenLoaded`, and the brand catalog screen's (fill every runtime
   brand catalog with its cabinet sections and door visuals) has to run before the toolbar's
   set-up and before `restart`, as it did when it was queued from the screen's `Awake`. The listeners come first because that `restart` must reach
   all of them.
## Events
- `EventApp.OnSceneReady` (`ActionEvent`, `_projectEvents/Events/EventApp.cs`): after a scene's
  `SceneInitializer` was initialized. No listeners yet.
- `EventSession` (B5.2, `_projectEvents/Events/`, payloads next to it): what the app systems ask
  of whichever scene is loaded. Why events: the systems outlive every scene and can't hold its
  screens. Raised by `SessionStatus`, `CatalogRequests`, `ClientRequests` and `PlatformSetupScene`;
  listened to in `OnEnable`/`OnDisable` by `MenuScene`, `DesignerScene` and `PlatformSetupScene`
  (until B5.3/B5.4 move them into the scene controls' `Initialize`/`OnDestroy`).
  - `OnSpinnerMessageRequested` (`SpinnerMessageInfo`): the loading spinner's status text, from
    every catalog and client request and the platform setup. The menu and the platform setup
    show it; the designer doesn't listen (it never got these).
  - `OnFullScreenSpinnerRequested`, `OnBackgroundSpinnerRequested` (`SpinnerInfo`): sign-in.
  - `OnHomeRequested`, `OnTrialCountdownRequested`: after sign-in (the countdown only on a trial).
  - `OnWelcomeRequested` (always the `Regress` transition, so no payload; that keeps the core
    package's `MelcherUITransitionStyle` out of the payloads) and `OnLogInRequested`: a failed
    sign-in, a kick while the menu is open.
  - `OnLoggedOut`: end of `SessionStatus.LogOut`; the menu and the designer hide their trial
    countdown. `OnSubscriptionActivated`: the auto-kick check found a paid subscription; only the
    menu hides its countdown, as before (the designer's stays until the scene changes). Named for
    the cause, not "hide countdown", so the two keep their different listeners.
  - `OnUserKickedPromptRequested` (`UserKickedInfo`): the kick prompt, in the menu.
    `OnMenuSceneRequested` (`MenuSceneRequestInfo`, an optional `System.Action` run once the menu
    scene has loaded): a kick while the menu isn't loaded; the designer goes back to the menu and
    the callback asks for the kick prompt there.
  Screens raise the same events where they ask the same thing (B5.3a): the full-screen spinner
  (every server request a menu screen makes; hiding it clears the status text), the background
  spinner (the sign-in downloads go on while the user moves around the menu), the status
  message, welcome, log in, home.
- `EventMenu` (B5.3a, `_projectEvents/Events/`): what the menu's screens ask of the menu's
  navigation (`MenuScene`, which listens in `OnEnable`/`OnDisable`): sign up (`SignUpInfo`),
  sign in with given credentials (`CredentialsInfo`: sign-up, pay-now and the editor's quick
  sign-in jump to the log-in screen and sign in), delete account, reset password
  (`ResetPasswordInfo`), pay now (`CredentialsInfo`), unauthorized domain (WebGL players only,
  `UnauthorizedDomainInfo`), create client, client list, client info and delete client
  (`ClientInfo`), client quote list (the client goes through `ClientListStatus`), its refresh,
  quote info and delete quote (`ClientQuoteInfo`), user profile, manufacturer whitelist, log-out
  prompt, back, screen orientation (mobile only), quit prompt (standalone only). One listener
  each. `MenuScene`'s own start-up calls its handlers directly instead of raising.
- `EventDesigner` (B5.5a, `_projectEvents/Events/`): what the kitchen model and the designer's
  screens ask of the designer's screens. Why events: the model must not hold screens, and screens
  asking the control that holds them would be a cycle. `DesignerScreensControl` listens (in
  `OnEnable`/`OnDisable`, like its `EventSession` listeners) to the setup wizard requests
  (`OnWhiteLabelClientInfoRequested`, `OnBuildingMethodsRequested`, `OnBrandCatalogsRequested`,
  `OnCabinetShapesRequested`, `OnKitchenTemplatesRequested`, `OnCabinetSizesRequested` with a
  `CabinetSizesMode`) and the loading screen (`OnLoadingScreenRequested` with a
  `DesignerLoadingMode`, `OnLoadingImageRefreshRequested` once a loaded design's brand catalog is
  known, `OnLoadingMessageRequested` with `LoadingMessageInfo`); `ToastUI` to `OnToastRequested`
  (`ToastInfo`: text and hold time, 1 s by default; an empty text with 0 s clears a toast),
  `DesignerNotificationUI` to `OnNotificationRequested` (`NotificationInfo`: title, message and
  an optional `System.Action` run on dismiss), `ProductSearchUI` to `OnProductSearchRequested`.
  Going back to the menu is `EventSession.OnMenuSceneRequested` (it existed for the kick), raised
  by the wizard's back buttons and the quit prompt. The prompts that answer through a callback
  are handed over instead: the hamburger menu gets the confirm and save prompts (through the
  navigation screen), the toolbar the confirm and about-product prompts (it passes the latter to
  its highlighted-product content). The save prompt's callback is a
  `Func<string, string, UniTask>` (save name, notes).
- `EventBridge.Menu` is gone (B5.3c1). The prompts that answer through a callback are handed to
  the screens that open them: `MenuNavigationControl` holds the sort-mode, kiosk PIN, edit-profile
  and multiple-choice prompts (serialized) and, first thing in its `Initialize`, gives the sort
  prompt to the client and quote lists (their `ListController`), the edit prompt to the client
  profile, and the edit, multiple-choice and kiosk prompts to the user profile; it uses the kiosk
  prompt itself to guard the client list and the profile. The manufacturer whitelist gets a
  callback that marks it as having pending changes (it already held the edit prompt). Why not
  events: the answers come back through callbacks and the payloads carry TMP and model types.
- **Commands to the kitchen are method calls** (B5.5b): the setup wizard's choices
  (`SetBuildingMethod`, `SetBrandCatalog`, `SetCabinetLayout`, `SetKitchenTemplate`,
  `SetCountertopType`, `SetBaseCabinetDepth`, `SetCountertopThickness`, `SetCountertopOverhang`,
  `CreateKitchen`), the toolbar's and the highlighted-product content's edits (`TryAddProduct`,
  `OnToolbarAddSectionClicked`, `TryCloneSection`, `TryDestroyOccupant`,
  `TryDestroyCabinetBacking`, `TryDestroySection`, `TryRepositionProduct`, `TryReorderCabinet`,
  `CenterOccupantInVolume`, `TryToggleToeKicks`, `SelectFinishProduct`, `GetCabinetLayout`) and
  `TryHighlightObject`, on `Kitchen`, which every caller already holds or was handed. Model
  objects call through `IKitchen`: an `Occupant` (given its kitchen by
  `Kitchen.InstantiateOccupant`) for its pointer-down highlight and its drag begin / drag / drag
  end, a `CabinetBacking` (given it by its span) for its highlight, and an `OccupiableVolume` for
  "occupant instantiated" and "occupant removed", through `FindOwningKitchen` (up the parent
  volumes to the cabinet); a volume with no kitchen above it tells no one.
- **`KitchenEvents`** (B5.5c, `_project/Scripts/KitchenModel/`, `kitchen.events`, also on
  `IKitchen`): the kitchen's notifications, an object the `Kitchen` owns. `EventBridge` is gone.
  Why not Doge: several are awaited (`SequentiallyAwaitedDelegate`: each listener finishes before
  the next starts, because loading content is asynchronous and WebGL can't block on it) and they
  carry model types, which `KitchenDesign.Events` can't see. Members keep their old names:
  - `restart`: clear the kitchen and go back to the setup wizard (raised by the designer's
    start-up and the hamburger menu's restart).
  - `onBuildingMethodChanged` (raised even when the method is the same as before),
    `onBrandCatalogChanged` (awaited), `onKitchenReady` (awaited; a kitchen finished being created
    or loaded, the last step whatever the building method), `onKitchenLayoutChanged` (layout
    prefab and instance, after the layout was created or removed; the orbit camera takes its start
    angle from it).
  - `onHighlightChanged` (awaited; `null` when nothing is highlighted).
  - `onCabinetAddedByUser`, `onCabinetResized` (also while a resize widget is dragged),
    `onCabinetReordered` (Sections: shift left / right), `onPreCabinetDestroyedByUser` (just before
    the cabinet goes), `onCabinetDestroyedByUser` (after; carries the span index),
    `onCabinetSwapStart` / `onCabinetSwapCanceled` (Catalog's swap-cabinet button).
  - `onOccupantAddedToCabinet` (the kitchen resizes the cabinet and its neighbours),
    `onOccupantResized` (a shelf), `onOccupantDragSuccess`, `onOccupantNudged` (the last three
    refresh the measurement lines).
  - `onDoorHandleStyleNameUpdate` (the toolbar picked a handle style; every door handle switches
    its model), `onSetVolumeWidthRequested` (the size button widget was clicked; the set-width
    prompt opens).
  **Who listens, and the order.** A listener gets the events through `ListenTo(KitchenEvents)` and
  adds its handlers in `AddKitchenListeners` (removed in `OnDisable`, added again in `OnEnable`
  once it has the events, so disabling a listener behaves as it did). Handlers of one event run
  in subscription order, and some depend on it (see the orbit start-yaw note in the handover), so
  `DesignerSceneInitializer` calls `ListenTo` in one fixed order that reproduces the order every
  event had on 2026-11-12 (`GeminiBaseline/listenerorder-designer-before-area5.txt`): navigation
  screen, toolbar, cabinet sizes screen, measurement overlay, **kitchen**, orbit camera,
  `DesignerScreensControl`, set-width prompt. The kitchen listens to its own events at its place
  in that list (`restart`, cabinet added / resized / reordered / destroyed, occupant added), not
  first. Listeners made later subscribe when they get their kitchen, after all of these, as
  before: a `CabinetLayout` in `SetKitchen`, a `CabinetTickerWidget` in `Initialize`, a
  `CabinetSectionDoorHandle` when `CabinetLayout` creates it (`InstantiateHandle` takes the
  events; the thumbnail creator's editor variant has none).

## Key systems
Each area's notes are in its own file, so a session reads only the areas it works on.
**Before changing an area, read its file. After changing it, update that file in the same
session.** With `CODE_COMMENTS: file-only`, the why of new and edited code goes there. Comments
already in the old code stay where they are until that code is rewritten (user, 2026-11-13).

- `Docs/systems/assemblies.md`: The assemblies, what each references, and where the code lives.
- `Docs/systems/kitchen-model.md`: `Kitchen` and its parts, cabinets, occupancy, save and load, price inputs, building-method and brand rules.
- `Docs/systems/cameras-and-photos.md`: Save photos, plan measures, the orbit and overhead cameras, the level-aim extension.
- `Docs/systems/products-and-visuals.md`: Product data, the visual database, content loading, Addressables, the product editor tools.
- `Docs/systems/server.md`: The server client, its request classes and parsers, and the server data types.
- `Docs/systems/app-systems.md`: `AppInitializer`, the scene initializers, the app-wide systems, the profile store, the app rules, the app and data assets.
- `Docs/systems/utility-and-editor-tools.md`: Generic helpers, what area 7 moved (`Utility/`, `Behaviors/`, the split of `Const`), `_project/Editor/Tools/`.
- `Docs/systems/cutouts.md`: Cutouts by clipping in the shaders: the clip set, the shaders, the materials.
- `Docs/systems/screens.md`: The menu and designer screens (all uGUI today), how they get their systems, where their files are.
- `Docs/upgrade-to-unity-6.md`: finished history. What the upgrade from 2022.3 broke and how
  each break was fixed (TMP fonts, Cinemachine 3, ambient light, the save-photo cover camera,
  instance ids, scene handles).

## Tools
- `Tools/Baseline/`: records the old app's baseline and compares this project against it
  (`unity6/runall.sh` is the whole check), `unity6/menucheck.sh` walks the menu screens and
  the designer round trip read-only, and `unity6/editprobe.sh` (area 4) makes a fixed series of
  edits (add, drag, resize, delete…) on a saved Catalog design and on a new Standard kitchen and
  snapshots the kitchen after each. Notes in `Tools/Baseline/README.md`.
- `Tools/Cutouts/`: the cutout prototype's driver and test snippets (sign in, load a design, add
  products, apply boxes, render a camera to a PNG). Notes in `Tools/Cutouts/README.md`.

## Not built yet / handover
- **Where we are (2026-11-12, the user's limit ran out mid-area).** New machine: Unity
  6000.6.8f1 with the Web, iOS and Android modules installed; the editor's target is WebGL.
  `GeminiBaseline/` (the old app's baseline and every earlier run) and `kitchendesign-held-back/`
  aren't on this disk: ask the user to copy them from the old machine. Done today, each step
  compiled and committed, behaviour checks deferred until the whole refactor is done (user,
  2026-11-12: "we'll do the checking later after we complete everything"): the team ports
  (`3d25da55`), B5.1, B5.2, B5.3 (a, b, c1, c2), B5.4a, B5.4c, B5.4b and B5.4d (2026-11-13; d is the designer screens' `ProFitGlobals` reads), B5.5
  (a, b, c; `EventBridge` is gone), B5.6, B5.7: **area 5 is done.** Area 6 (cameras and photos, `06-cameras-photos.md`, B6.1–B6.4) and
  area 7 (leftovers, `07-leftovers.md`, B7.1–B7.2) are done too. **The deferred checks ran on 2026-11-13**, the tip
  (`3a8fa33b`) against `3d25da55`, both on the same day; outputs in `GeminiBaseline/final-tip/` and
  `final-base/`, the runner is `GeminiBaseline/area5-map/fullrun.sh`. Results: `resave.json`
  identical in all 8 designs; measurements identical apart from the live `OrbitView` pitch
  (0.01–0.04° in three designs, the known orbit noise); the edit probe identical; the menu check
  the same at every step (21 / 21 errors); the fallback JSON a sign-in writes byte-identical;
  every kitchen event has the same subscribers in the same order as before area 5; the rule
  checks 60 / 132 / 169 with no mismatch. The cold start found and fixed one regression
  (`PayNowUI`, see `Docs/systems/screens.md`). The three cover photos that differed on the tip's first run (28316, 27763,
  27764: cabinets with a white noisy texture) were the "freshly opened editor" gotcha: a second
  capture on the warm editor (`final-tip/unity6-run2`) matches `3d25da55` in every photo (at
  most 0.08/255) and in every `resave.json` (`photodiff.sh final-base/unity6 final-tip/unity6-run2`,
  run again on 2026-11-13: same). The 14th cold-start error is the Unity CLI's own (Gotchas), not
  the app's.
  After a check run, `Tools/Baseline/unity6/load.cs` and `measure.cs` are regenerated (restore
  them) and the Gemini fallback JSON must be put back from `GeminiBaseline/_backup-2026-11-12`.
  `screenprobe.sh` (B5.0) was never written.
  **The work after the refactor has its plan: `.claude/prompts/08-globals-and-cleanup.md`**
  (2026-11-13; order agreed with the user): (a) empty `ProFitGlobals`, (b) split the measure
  placement out of `KitchenScreenshotControl`, (c) `AssetsMelcher/Editor` to `_project/Editor/`
  with a planned model reimport (ask the user before starting it), (d) house conventions inside
  the files that were only moved. Done and committed on 2026-11-13: **B8.1** (the kitchen is
  built by `Kitchen.Initialize`, called from `DesignerSceneInitializer`; full check identical to
  `final-tip`) and **B8.2** (model objects ask their kitchen; full check the same after one fix,
  see the plan's results), **B8.3** (no designer screen reads a
  system from the static; see `Docs/systems/screens.md`) and **B8.4** (`AppProfileStore` replaces
  `ProFitGlobals`, which is deleted; see `Docs/systems/app-systems.md`): **part (a) of the plan is done.**
  **B8.5** is done too (`PlanMeasurePlacement`, see `Docs/systems/cameras-and-photos.md`).
  **B8.6** is done (`_project/Editor/Tools/`, see `Docs/systems/utility-and-editor-tools.md`; the user said to go ahead).
  **B8.7** (2026-11-13), the mechanical part: braces and explicit access modifiers (a C#
  formatter run; the old code already followed both, 9 files changed), six runtime
  `Debug.Log`/`LogError` calls wrapped in `#if UNITY_EDITOR` (`Debug.LogException` is left
  alone: it reports real errors in builds), and 220 non-serialized `m_`/`s_` fields in 77
  files renamed to `_camelCase`. **Still as the old code had it, on purpose:** every serialized
  field keeps its `m_` name (assets bind it; about 1,000 fields), 66 non-serialized fields
  whose `m_` name also occurs in another file (mostly the same private name in several
  classes; each needs a look), 19 whose name is a key in some asset or a string literal, and
  the comments (about 8,700 comment lines in the moved files; R-CODE-7 leaves an existing
  codebase's comments alone unless the user asks, and where their whys should live is the open
  question below). How the renames were made safe, for the next batch: a field is renamed only
  when its declaration has no `Serialize…` attribute and isn't public, its name is not a key in
  any scene, prefab or asset, it occurs in no other file (code, `Tools/`, the check scripts)
  and in no string literal, and the new name is free in the file; afterwards an `eval` over the
  assemblies confirmed no `_`-named field is serialized apart from two that already were
  (`PCMBandaidFix`).
  Noticed in B8.5, not changed:
  `KitchenScreenshotControl.TryAddNapoleonTopDownWatermark` compares the catalog's
  `cabinetManufacturerId` with a literal 95 (Napoleon) instead of asking `BrandRules`; it
  belongs there as a rule.
  The check snippets under `Tools/Baseline/unity6` and `Tools/Cutouts` read the systems through
  `AppInitializer.SystemsForEditor`, so they no longer run on commits before B8.4. The check runner is `GeminiBaseline/area5-map/fullrun.sh <tag>` (reference `final-tip`; for photos
  `final-tip/unity6-run2`, because `final-tip/unity6` has the three untextured covers);
  call `playercompile.sh` there by its absolute path (it now waits for `KitchenDesign.dll`). The notes were split into
  `Docs/systems/` on 2026-11-13 and the old comments stay in the code (see "Keeping this file
  current"): **plan 08 is finished.** Open after it: the 85 fields the rename guards skipped,
  the Napoleon watermark's literal id, and the items below.
- Track A: done 2026-10-20 (summary in `Docs/upgrade-to-unity-6.md`). The copy at
  `/Users/emmanuel/Documents/Unity/GeminiUpgradeTest/` is throwaway and can be deleted; its
  captures are in `Captures/` and `Assets/Captures/`. A real kitchen in the designer was not
  reached because offline guest (ProFit) has no data for the kept brands; signing in from
  `MenuScene` would need a fuller copy.
- **The copy is done (2026-10-20)**, from commit `7700e16a8c`, step 1 of
  `.claude/prompts/01-copy-and-upgrade.md` (counts and checks there). Committed as `131b0020`
  ("Initial project clone"). The old repo's `.gitattributes` came over (26,787 files there are in Git LFS) and the
  user installed LFS here. Step 3's compile fixes are done (see `Docs/upgrade-to-unity-6.md`).
  **Next:** the user asked (2026-10-20) for only a few hundred products in the copy, so the full
  import was stopped. The baseline (step 2) is done. Step 1b is done: 899 of 8,552 database
  entries stay (the baseline designs' cabinets with their door styles and mapped doors, their
  products plus all Twin Eagles and Bull, all finishes); the rest is in
  `/Users/emmanuel/Documents/Unity/kitchendesign-held-back/`, nothing deleted (details in the
  plan). The whole Challenger catalog was not kept: 1,012 visuals on its own.
- **Step 3 (upgrade) and step 4 (check) done 2026-10-20.** First import 145 s (thinned copy).
  The app runs in the 6.6 editor, signs in and loads saved designs; all 8 usable baseline designs
  match the old app in every measurement and in the save JSON (plan, step 4 result).
- **Darker render and save-photo cameras: done 2026-10-20** (details in
  `Docs/upgrade-to-unity-6.md`: ambient light, save-photo cover camera). The runtime ambient probe was black;
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
  **Found 2026-10-23 (logged during B4.2):** `Kitchen.Restart` is subscribed to
  `EventBridge.Designer.restart` before `DesignerOrbitCamera.ResetView`. When a design's load
  reaches its layout without waiting a frame (44392 does), `onKitchenLayoutChanged` sets the
  layout's start yaw first and `ResetView` then puts back the scene's 45°, so 44392 (Straight,
  start yaw 0°) always starts at 45°. 36449's layout arrives two frames later and wins. Separately,
  the live `MainCamera` measured after the save photos sometimes reads yaw 0° for 36449 (3 of 6
  runs on 2026-10-23), with the save photos unchanged; and in one run its product sat 3" left of
  the stored position (the Toppin heated-product padding missing at load; the padding is applied on
  `onBrandCatalogChanged`, which `Restart` raises only after the load). Both intermittent, both
  in code area 4 moves (B4.5); the camera itself is area 6.
- **Cutouts by clipping: prototype built 2026-10-20** (results in `.claude/prompts/02-cutouts-by-clipping.md`,
  step 1). The user gave the go on the look (2026-10-20). The nested case (a burner in a plunge)
  was tested the same day and works; the two plunge visuals are back in the copy (901 database
  entries). Steps 2 and 4 done (2026-10-21): wired into the app, all 8 baseline designs save
  identically, photos match. Step 3 done 2026-10-21 (old stencil path removed; baseline
  unchanged, see the plan). The three non-clipping materials were switched on 2026-10-21 (see
  `Docs/systems/cutouts.md`; committed in `ba666e23`). Next, when the user asks: WebGL
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
- **Older copies of a design on the server have no price inputs.** In the client list, designs
  saved in October 2024 (27740, 27755, 27763, 27764) have no `cabinet_list`,
  `manual_price_sheet_entries`, `price_changes` or `countertop_area`, and their `islandDimensions`
  are in the 2024 per-cabinet format; designs saved from 2025 on have them (70 of the 102 in the
  Gemini account, checked 2026-10-23). So `resave.json` (what today's app would send) is the price
  and measurement baseline; `server.json` is only the input.
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
  (B3.1–B3.4, 2026-10-22/23; results in the plan, "Area 3").** **Area 4, the kitchen model, has its
  own plan: `.claude/prompts/04-kitchen-model.md`** (written 2026-10-23; the user left the design
  and the details to Claude and wants each step checked and committed on its own). The old repo
  has nothing to bring over first except the rear wall, which still waits for the user (checked
  2026-10-23, details in the plan). **Area 4 is done and committed (B4.0–B4.10, 2026-10-23;
  results in the plan).** `Kitchen.cs` went from 7,565 to about 3,040 lines: save, load, price
  inputs, selections, selection and finishes, widgets and dragging are their own classes; the
  building-method and brand checks are named rules; the model's files are in `_project/Scripts/`.
  Every step matched the run before on the baseline, the menu check and the new edit probe.
  Open from area 4, for the user: the orbit start-yaw race and the intermittent 36449 load (orbit
  view note above); the rear wall waits for the user and now has to be ported by hand (map in the
  plan). **Area 5, the screens, has its own plan: `.claude/prompts/05-screens.md`** (written
  2026-10-23; the user left the design to Claude, each step checked and committed on its own).
  It takes the scene start-up into the initializers, `EventBridge`, `Kitchen.instance` and the
  screens' building-method checks.
- **Needs attention: the U+FFFD strings (fixed here 2026-10-23, the user's decision).** Before
  old-repo commit `520d740c9d` (2025-11-26) `Kitchen.cs` was Windows-1252 and held `ft²`, `m²` and
  `OASIS™ 45°`; that commit re-saved it as UTF-8 and they became U+FFFD (Gotchas). The server's
  stored designs show it: `countertop_area` reads `ft²` up to September 2025, `ft�` from February
  to May 2026, and `sq. ft.` from August 2026, after the team's fix `875b80d9f2` (2026-06-11),
  which covered the countertop area only. This project sends `sq. ft.` / `sq. m.` in
  `cabinetFinishArea` (ProFit only, the Standard method; the team's wording) and
  `OASIS® 45° Transition Piece` on Napoleon corners (Gemini). Nothing in the app reads either
  string back. **The old app was fixed on 2026-11-05** (`dbd1a82fdb` by the user, `b2db7bd680` by
  the team, on `feature/model-integration`): `sq. ft.` / `sq. m.` for both areas and the
  transition piece as `OASIS® 45°` (the registered sign ®). Ported here on 2026-11-12,
  replacing the `™` this project had sent since 2026-10-23 (the server got `™` before November
  2025, e.g. designs 31373 and 31642), so both apps send the same text. Still open:
  1. Designs saved since November 2025 keep their broken text until they are saved again.
  2. How the server's PDF prints `®` and `°` was not checked. A Napoleon design's PDF would show
     it (a read-only download; ask first).
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
