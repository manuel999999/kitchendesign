---
title: "When start-up work queued from a screen's Awake moves into its Initialize, initialize that screen first so the queued work keeps its order"
author: Emmanuel
date: 2026-11-13
branch: feature/clone
cost: { minutes: 15, tokens: 196802 }
id: l-257191c7
scope: repo
status: active
src: agent
touches:
  - Assets/_project/Scripts/Game/DesignerScreensControl.cs
  - Assets/_project/Scripts/UI/DesignerScreens/BrandCatalogUI.cs
  - Assets/_project/Scripts/CatalogData/CatalogDataStatus.cs
---

## Decided
Work that designer screens queue to run "when the catalog data is loaded" runs in the order it was queued. Screens used to queue from Awake, which runs before the scene initializer, so their work came before anything queued by the initializer. When such a queue call moves into the screen's Initialize, that screen is initialized first in the designer control, ahead of the toolbar, so the order stays: fill the runtime brand catalogs, dev product list, toolbar set-up, hamburger menu set-up, restart.

## Tried and ruled out
Leaving the queue call in Awake while the callback reads a field set in Initialize: when the catalog is already loaded (every start from the menu) the callback runs at once, inside Awake, before Initialize. The dev screen did exactly this since B5.4d and threw a NullReferenceException on every designer load, disabling itself; no baseline check showed it, only Logs/Editor.log.

## Watch out
The check helper scripts in GeminiBaseline/area5-map resolve their own folder from $0 and then cd into the project, so run them by absolute path; run as ./playercompile.sh they report files=0 and "0 errors" without having compiled anything useful. The player compile continues after the CLI call returns: wait for KitchenDesign.dll to appear.

In the code: Assets/_project/Scripts/Game/DesignerScreensControl.cs (Initialize: m_BrandCatalogUi, then devUi, then m_ToolbarUi); Assets/_project/Scripts/UI/DesignerScreens/BrandCatalogUI.cs (PopulateRuntimeBrandCatalogs); Assets/_project/Scripts/CatalogData/CatalogDataStatus.cs (ExecuteWhenLoaded runs at once when IsLoaded).
