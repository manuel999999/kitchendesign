---
title: "Close or reload open scenes before switching git commits, or the editor blocks on a dialog"
author: Emmanuel
date: 2026-11-12
cost: { minutes: 9, tokens: 58562 }
id: l-d9ce879d
scope: repo
status: active
src: agent
touches:
  - Assets/AssetsMelcher/Scenes/DesignerScene.unity
---

## Decided
- Before `git switch` / `checkout` to another commit while the Unity editor is open, open an empty scene (or close the scenes the switch will change) first.

## Tried and ruled out
- Switching commits with DesignerScene open: Unity showed its "scene modified externally, reload?" dialog, which blocks the main thread. Every CLI `eval` then answered "Main thread operation timed out after 5000ms" while `unity status` still said ready and the editor sat at ~0% CPU. Only a click on the dialog frees it.

## Watch out
- An idle editor that times out every CLI call almost always means a modal dialog; ask the user to look at the editor instead of waiting.

In the code: `git switch --detach <commit>` with `Assets/AssetsMelcher/Scenes/DesignerScene.unity` open; CLI `unity command eval_file` timeouts.
