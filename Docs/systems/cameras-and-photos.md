# Cameras and photos

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Cameras and photos** (area 6 of the refactor, from 2026-11-13; plan:
  `.claude/prompts/06-cameras-photos.md`).
  - `KitchenScreenshotControl` (`_project/Scripts/Photos/`, `KitchenDesign.Photos`; was
    `KitchenScreenshotManager`): takes the save photos (`TakeScreenshots(kitchen)` returns a
    `ScreenshotGallery`: the cover, the top-down photo and its plan, and a front photo and plan per
    span) and places the measures for the plan photos. The PDF photo size is 1920×666 (an aspect
    that fits the PDF page) and the plan size 1920×1360 (fills the page).
    `SkipScreenshotInEditor` is a development switch that skips the renders to debug the measure
    lines faster. Out of it since B6.1: `ScreenshotGallery` (`Serializables/`; serializable only
    so the pictures can be inspected in the editor), `CameraCapture.TakeScreenshotFromCamera`
    (renders a camera to JPG bytes; the hamburger menu's Export JPG uses it too),
    `OccupantNeighbourFinder` (for every occupant, its nearest neighbour in each direction visible
    from the top and from the front, then the unique pairs to measure between; a pair's second
    occupant is null when the measure runs to the cabinet's edge) with its types
    `OccupantNearestNeighbours` (arrays indexed by `CabinetLocalDirection`, oversized so the enum
    value is the index), `OccupantAndDistance`, `OccupantMeasurementPair`, and
    `MeasurementForScreenshot` (a placed measure, the angle it shows in, and its span).
    It gets the session (for the user's units) through `Initialize` from
    `DesignerScreensControl`, which holds it and uses it for the F6 debug key (B6.2), and asks
    the building-method rules; `MeasuresOnlyProductCabinets` (Sections) was added for it: ends,
    transitions and the straights beside a corner have a width in Sections but get no measure.
  - `PlanMeasurePlacement` (B8.5, 2026-11-13, `Photos/`, a plain class): places the measures
    for the plan photos. `PopulateMeasuresForScreenshots` resets the pooled measures and places
    one set per span (cabinet and countertop sizes, pushed 4" outside the cabinet so the lines
    don't draw over it) and one per occupant pair, each tagged with the angle and span it shows
    in; the static `SetupOccupantCutOutMeasurements` lays out an occupant's own cut-out lines.
    `KitchenScreenshotControl` keeps the cameras, the renders, the visibility and material
    swaps around each photo, and the Napoleon watermark. It still instantiates the pool of
    measures in `Awake` (300, from its serialized prefab and count) and builds the placement in
    `Initialize` with that pool, its three serialized axis colours and the session (the user's
    units). The colours are read once there, so changing them in the inspector during play no
    longer affects the next photos.
  - `ScreenshotAngle` (`_projectShared/Enums/`, values as before: None, Cover, Top, Front, Back)
    and `IsPlanAngle` (`_projectShared/Common/`: top, front or back).
  - `ProductVisualThumbnailCreator` (`Photos/`): static editor-tool code that renders product
    thumbnails in the screenshot scene. It reads `AppProfileStore.Profile`: it runs in
    edit mode, where nothing constructs systems.
  - Cameras (`_project/Scripts/Cameras/`, `KitchenDesign.Cameras`): `OrbitCameraControl` (was
    `DesignerOrbitCamera`), `OverheadCameraControl` (was `DesignerOverheadCamera`).
    `SpinBehavior` (`Behaviors/`, was `RotateCamera`) turns the object it is on.
  - `KitchenCameraControl` (B6.3, `_project/Scripts/KitchenModel/`): the kitchen's camera and
    framing code, a part of `Kitchen` like area 4's (constructed in `Kitchen.Awake` with the orbit
    camera, the cover virtual camera, the two target groups and the ground, whose serialized
    fields stay on `Kitchen`; `Kitchen`'s old methods forward to it). `FrameLayout` points the
    orbit and cover cameras at the empty target group while the kitchen target group is rebuilt
    (a target group can't be changed while cameras look at it), then fills it with the layout's
    corner and end cabinets (weight 1, radius 2) and points the cameras back.
    `RotateToFrontOfCabinet` turns the orbit to face a cabinet the camera is behind when an
    occupant is added or dropped there (a client request), in steps of the orbit's rotation
    interval. `MoveOrbitRootToCenter` runs every frame from `Kitchen.LateTick`.
    `CenterTheGround` moves the ground under the kitchen's centre; the ground is hidden while a
    new kitchen is being sized, so it never has to slide or be walked off, and is centred just
    before it is shown. `GetCamera` is still `Camera.main`.

- **`CameraLevelAimBehavior`** (`_project/Scripts/Behaviors/`, 2026-10-20): a `CinemachineExtension`
  that, at the Finalize stage, takes the framed direction, moves the aim point by
  `worldAimOffset` (world space, at the Look At target's distance) and re-aims with the reference
  up, replacing the orientation correction. Why: it reproduces CM2's Group Composer aim (centre +
  offset, no roll) on top of CM3's Group Framing; see `Docs/upgrade-to-unity-6.md`, save-photo cover
  camera. Its offset is set to the vcam's `RotationComposer.TargetOffset`. Verified against the
  8 baseline designs. The first type in the house layout that old code depends on, through the
  scene only.
