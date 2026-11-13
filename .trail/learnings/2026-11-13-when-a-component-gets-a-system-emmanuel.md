---
title: "When a component gets a system handed in, check what its own Awake calls, and trust the listener-order check to catch it"
author: Emmanuel
date: 2026-11-13
branch: feature/clone
cost: { minutes: 23, tokens: 295998 }
id: l-df310348
scope: repo
status: active
src: agent
touches:
  - Assets/_project/Scripts/Measures/MeasurementOverlayManager.cs
  - CLAUDE.md
  - Assets/_project/Scripts/Cabinets/Cabinet.cs
  - Assets/_project/Scripts/Cabinets/CabinetLayout.cs
  - Assets/_project/Scripts/Cabinets/CabinetSectionDoorHandle.cs
  - Assets/_project/Scripts/Cabinets/CabinetSpan.cs
  - Assets/_project/Scripts/Game/DesignerSceneInitializer.cs
  - Assets/_project/Scripts/KitchenModel/IKitchen.cs
  - Assets/_project/Scripts/KitchenModel/Kitchen.cs
  - Assets/_project/Scripts/KitchenModel/KitchenWidgetsControl.cs
---

## Decided
When a read of a static system is replaced by a field set in Initialize, first check whether the component's own Awake or OnEnable reaches that read (directly or through a handler it calls). If it does, guard the read and apply the value again from Initialize.

## Tried and ruled out
Relying on the save photos, resave.json and the edit probe to show a broken component: all three passed while the measurement overlay had lost every event subscription.

## Watch out
An exception in Awake disables the component, so its OnEnable never subscribes; nothing visible fails in the baseline designs. Only the listener-order dump (and NullReferenceException in Logs/Editor.log) showed it. Every designer screen is active when the scene loads, so its Awake runs before the scene initializer.

In the code: Assets/_project/Scripts/Measures/MeasurementOverlayManager.cs (Awake calls OnKitchenRestart; ApplyUserUnits is guarded and also called from Initialize). Check: GeminiBaseline/area5-map/listeners.sh <tag>, then cmp listenerorder-designer-tip.txt against the new file.
