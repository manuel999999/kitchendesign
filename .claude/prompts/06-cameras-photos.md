# 06 — Cameras and photos (area 6 of the refactor)

Planned 2026-11-13. Area 6 of `03-refactor.md`; read its recipe, then `04-kitchen-model.md` and
`05-screens.md`. The user left the design to Claude. **No behaviour checks between steps** (user,
2026-11-13): each step compiles (editor and WebGL player scripts) and is committed; every check
runs once after area 7, against `3d25da55`. Anything suspicious goes in `05-screens.md`'s bug list.

## Today (read 2026-11-13)
- `KitchenScreenshotManager` (2,717 lines, `AssetsMelcher/Scripts`): takes the save photos. One
  orchestrating method (`TakeScreenshots`, 440 lines), the measure placement for the plan photos
  (`PopulateMeasuresForScreenshots`, 950 lines, plus `SetupMeasurementScreenshot`,
  `SetupOccupantCutOutMeasurements`, `AddMeasureBetweenCabinetPoints`), two pure static searches
  (`FindNeighboursOfAllOccupants`, `FindUniqueOccupantMeasurementPairs`), a static camera capture
  (`TakeScreenshotFromCamera`, also used by the hamburger menu's Export JPG), the Napoleon
  watermark, and five nested types. Reads `ProFitGlobals.userProfile` twice (the units), compares
  the building method five times, and is found with `FindObjectOfType` by the F6 debug key.
- `ProductVisualThumbnailCreator` (665 lines): editor tool code that renders product thumbnails
  in the screenshot scene; static, called from the product editor tools; reads
  `ProFitGlobals.appProfile` six times.
- Cameras: `DesignerOrbitCamera` (197), `DesignerOverheadCamera` (157), `RotateCamera` (21, spins
  an object; on `Kitchen/Rotate`). `Kitchen` holds the orbit camera, the plan and cover virtual
  cameras and the camera target group, and has the framing code
  (`UpdateCameraAndFramingGroups`, `GetCenterOfTargetGroup`, `RotateCameraToFrontOfCabinet`,
  `CenterTheGround`, `GetCamera`): about 120 lines.
- `ScreenshotAngle` (enum, implicit values) and its extension are in `AssetsMelcher/Scripts`.

## Decisions
1. **Names.** `KitchenScreenshotManager` → `KitchenScreenshotControl`, `DesignerOrbitCamera` →
   `OrbitCameraControl`, `DesignerOverheadCamera` → `OverheadCameraControl`, `RotateCamera` →
   `SpinBehavior` (it turns whatever it is on). Renamed in place (`git mv`, same GUID).
2. **Folders.** `_project/Scripts/Photos/` (`KitchenDesign.Photos`), `_project/Scripts/Cameras/`
   (`KitchenDesign.Cameras`), `SpinBehavior` in `Behaviors/`.
3. **The photo control is split along what is already separate**, not rewritten: the nested types
   and the static helpers leave; the measure placement stays in the control (it uses most of its
   serialized fields, and splitting a 950-line method without a check after it is not worth the
   risk). Listed as open below.
4. **The kitchen's camera code becomes `KitchenCameraControl`**, a part of `Kitchen` like area 4's
   (constructed in `Kitchen.Awake` with the serialized references, which stay on `Kitchen` so the
   scene doesn't change).
5. **The orbit start-yaw race is not fixed here** (no behaviour change; it is in the handover for
   the user to decide after the checks).
6. `ProductVisualThumbnailCreator` keeps reading `ProFitGlobals.appProfile`: it runs in edit mode
   from editor tools, where nothing constructs systems (05, decision 6).

## Steps
- **B6.1 — types and static helpers out of the photo manager.** `ScreenshotAngle` to
  `_projectShared/Enums/` with explicit values equal to today's, its extension to
  `_projectShared/Common/`; `ScreenshotGallery` to `Serializables/`; `OccupantNearestNeighbours`,
  `OccupantAndDistance`, `OccupantMeasurementPair`, `MeasurementForScreenshot` to `Photos/`;
  `OccupantNeighbourFinder` (the two searches) and `CameraCapture` (the capture) as static
  classes in `Photos/`.
- **B6.2 — references and rules.** The control gets the session through `Initialize` from
  `DesignerSceneInitializer`; the F6 key uses a handed reference; the five building-method
  comparisons become rules.
- **B6.3 — `KitchenCameraControl` out of `Kitchen`.**
- **B6.4 — file and rename** (decisions 1 and 2), namespaces following the folders.

## Results
- **B6.1 and B6.4 done together, 2026-11-13** (the moves and renames are one script pass). The
  nested types and the three static methods were cut out by a script (brace matching) and their
  comments dropped; `CameraCapture` reads `KitchenScreenshotControl.SkipScreenshotInEditor`. Seven
  files moved with their `.meta`, four types renamed, `using` lines added where needed,
  `photos.cs` requalified. No text asset names these types. Compiles in the editor and as WebGL
  player scripts.

- **B6.2 done 2026-11-13.** `KitchenScreenshotControl.Initialize(SessionStatus)` (the units), called
  by `DesignerScreensControl`, which holds the control (one reference set through the editor) and
  uses it for the F6 debug key instead of `FindObjectOfType`. The five comparisons are rules:
  `HasCabinetColumns` twice (the span's outside points account for the columns of adjoining
  spans; cabinet widths are measured between the mullions), `CabinetsAreProducts` (measure
  points relative to the span's transform), `UsesBrandCatalog` (the extra front measure between
  neighbouring cabinets) and the new `MeasuresOnlyProductCabinets` (Sections: ends, transitions
  and the straights beside a corner have a width there but aren't measured).
  `buildingrulescheck.cs`: 132 checks.

## Open after this area
- The measure placement (`PopulateMeasuresForScreenshots` and its helpers) is still one block
  inside `KitchenScreenshotControl`.
- The live orbit view's differences from the old app and the start-yaw race (handover).
