---
title: Only the app profile is reachable through a static; editor checks and debug menus get the running systems from the app initializer
author: Emmanuel
date: 2026-11-13
branch: feature/clone
cost: { minutes: 10, tokens: 100910 }
id: l-3811809a
scope: repo
status: active
src: agent
touches:
  - Assets/_project/Scripts/Game/DesignerScreensControl.cs
  - Assets/_project/Scripts/Game/MenuNavigationControl.cs
  - Assets/_project/Scripts/UI/DesignerScreens/BrandCatalogUI.cs
  - Assets/_project/Scripts/UI/DesignerScreens/CabinetOtherSizeControl.cs
  - Assets/_project/Scripts/UI/DesignerScreens/CabinetOtherSizesUI.cs
  - Assets/_project/Scripts/UI/DesignerScreens/DesignerDevUI.cs
  - Assets/_project/Scripts/UI/DesignerScreens/DesignerToolbarContentHighlightedProduct.cs
  - Assets/_project/Scripts/UI/DesignerScreens/DesignerToolbarUI.cs
  - Assets/_project/Scripts/UI/DesignerScreens/ProductSearchUI.cs
  - Assets/_project/Scripts/UI/DesignerScreens/SetVolumeSizeUI.cs
---

## Decided
The old static that forwarded to every system is deleted. One static store remains, for the app profile, its options and its skin: they are one immutable asset read by views in Awake/OnEnable before any initializer, by rows made at runtime, by the model for default materials, and by edit-mode tools. Everything else is handed in through Initialize. Editor-only code that needs the running systems (debug menus, check snippets run through the Unity CLI) finds the app initializer and reads its editor-only systems property.

## Tried and ruled out
Putting the editor-only systems property on each scene initializer (the plan's first idea): it would touch the abstract base and every scene's initializer, while the app initializer is one object that exists in every scene.

## Watch out
The check snippets now depend on that property, so they no longer run on commits older than this change. When comparing save photos with the reference run, use the warm capture (final-tip/unity6-run2): the first capture (final-tip/unity6) has three covers without finish textures (28316, 27763, 27764) and comparing against it looks like a regression in exactly those three.

In the code: Assets/_project/Scripts/Game/AppProfileStore.cs (Profile, Options, Skin; Bind from AppInitializer.Awake); AppInitializer.SystemsForEditor (#if UNITY_EDITOR); Assets/_projectDebugging/Menus/DebugSystems.cs (TryGet); Tools/Baseline/unity6/state.cs, load.tmpl, menu.tmpl, contentprobe.cs, editstep.tmpl, listenerorder.cs; Tools/Baseline/unity6/photodiff.sh final-tip/unity6-run2 <new>.
