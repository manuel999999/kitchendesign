---
title: Set scene references by reopening and saving the scene in one editor call
author: Emmanuel
date: 2026-11-12
branch: feature/clone
cost: { minutes: 66, tokens: 1362886 }
id: l-4cca9783
scope: repo
status: active
src: agent
touches:
  - CLAUDE.md
  - Assets/AssetsMelcher/Scenes/DesignerScene.unity
  - Assets/AssetsMelcher/Scripts/Scenes/DesignerScreensControl.cs
  - Assets/AssetsMelcher/Scripts/UI/BrandCatalogUI.cs
  - Assets/AssetsMelcher/Scripts/UI/CabinetOtherSizesUI.cs
  - Assets/AssetsMelcher/Scripts/UI/CabinetShapeUI.cs
  - Assets/AssetsMelcher/Scripts/UI/CabinetTickerWidget.cs
  - Assets/AssetsMelcher/Scripts/UI/DesignerAboutProductUI.cs
  - Assets/AssetsMelcher/Scripts/UI/DesignerToolbarUI.cs
  - Assets/AssetsMelcher/Scripts/UI/KitchenTemplateUI.cs
---

## Decided
- When a scene needs a new serialized reference, open the scene, set the field through `SerializedObject` and save, all in one `eval_file` call. The scene then changes only by the new lines.

## Tried and ruled out
- Setting the field on a scene that was already open in the editor, then saving: the layout groups' computed RectTransform values (anchors, sizes) and new TextMeshPro font-feature fields got written too, about 3,000 lines in MenuScene for a one-line change. Reverted and redone as above.

## Watch out
- Opening or playing a scene rewrites the OpenSans-Bold font atlas asset. Restore it from git before staging.

In the code: `EditorSceneManager.OpenScene(path, OpenSceneMode.Single)` + `SerializedObject.ApplyModifiedPropertiesWithoutUndo()` + `EditorSceneManager.SaveScene(scene)` in one snippet; `Assets/AssetsMelcher/Scenes/MenuScene.unity`; `Assets/AssetsMelcher/Fonts/OpenSans/OpenSans-Bold.asset`.
