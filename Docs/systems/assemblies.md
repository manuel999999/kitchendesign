# Assemblies

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Assemblies (2026-10-20):** `KitchenDesign` (`_project/Scripts`, refs Events, Shared, Doge),
  `KitchenDesign.Events` (`_projectEvents/Events`, refs Doge, Shared), `KitchenDesign.Shared`
  (`_projectShared`, refs nothing), `KitchenDesign.Debugging` (`_projectDebugging`, Editor only,
  autoReferenced off, refs KitchenDesign, Shared). All reference by name. `KitchenDesign` also
  references `Unity.Cinemachine` (2026-10-20, for `CameraLevelAimBehavior`).
  **Since phase A of the refactor (2026-10-22) all the old code is in our assemblies and namespace.**
  Since area 7 (2026-11-13) all runtime code is under `_project/Scripts`; `.asmref` files still
  put `Procedural Assets` (Weaver's `Layers.cs`) and the user's `Cloder` into `KitchenDesign`,
  and the old editor tools (`_project/Editor/Tools/` since B8.6, namespace
  `KitchenDesign.ToolsEditor`, and `Cloder/Editor` through an `.asmref`) are
  `KitchenDesign.Tools.Editor` (Editor only). Namespaces follow the folders under
  `KitchenDesign`; every old file has `using Melcher;` for the core package, which
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
