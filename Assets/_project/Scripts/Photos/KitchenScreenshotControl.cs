namespace KitchenDesign.Photos
{
    using KitchenDesign.Utility;
    using KitchenDesign.Session;
    using KitchenDesign.Serializables;
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using KitchenDesign.Geometry;
    using KitchenDesign.Cabinets;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Measures;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Products;

    /// <summary>
    ///     This class is responsible for taking screenshots of the kitchen to
    ///     be used in the PDFs (quotes).
    ///     <br /><br />
    ///
    ///     This includes taking pictures that contain measurements. These are
    ///     used as the CAD/plan pictures in the quote. There is some
    ///     complexity involved to discover which measurements are important to
    ///     display: It searches for the nearest occupant in every orthogonal
    ///     direction for each occupant that is added to the cabinet, and it
    ///     does this for every screenshot camera angle used.
    /// </summary>
    public class KitchenScreenshotControl : MelcherMonoBehaviour
    {
        private SessionStatus _session;
        private PlanMeasurePlacement _measurePlacement;

        /// <summary>
        ///     The resolution of screenshots used in the PDF. The aspect ratio
        ///     was chosen (0.34) because it fits well on the PDF.
        /// </summary>
        public static readonly Vector2Int PdfImageResolution = new Vector2Int(1920, 666);

        /// <summary>
        ///     The resolution of plan screenshots used in the PDF. The aspect
        ///     ratio was chosen because it best fills the PDF size paper.
        /// </summary>
        public static readonly Vector2Int PdfPlanImageResolution = new Vector2Int(1920, 1360);

        /// <summary>
        ///     A development flag that can be enabled to skip taking
        ///     screenshots of the kitchen. This is useful when debugging
        ///     issues with the measurement lines and the pause/computation of
        ///     screenshot-ing slows iteration time down unnecessarily.
        /// </summary>
        public const bool SkipScreenshotInEditor = false;


        /// <summary>
        ///     Take all screenshots for the design's PDF.
        /// </summary>
        public async UniTask<ScreenshotGallery> TakeScreenshots(Kitchen kitchen)
        {
            using var drawScope = Cuboid.StartDrawScope();

            ScreenshotGallery screenshots = new ScreenshotGallery();

            // Clear the user's selection so the selection highlight doesn't
            // render in the screenshots.
            await kitchen.TryHighlightObject(null);

            // Figure out where products are in relation to each other. We'll
            // use this to find pairs of products to draw measurements between
            List<OccupantNearestNeighbours> occupantToNearestNeighbours =
                OccupantNeighbourFinder.FindNeighboursOfAllOccupants(kitchen);

            // A list of measurement pairs. Each occupant has 1 closest
            // neighbour for each 6 ordinal directions (CabinetLocalDirection).
            List<OccupantMeasurementPair> uniqueMeasurementPairs =
                OccupantNeighbourFinder.FindUniqueOccupantMeasurementPairs(kitchen, occupantToNearestNeighbours);

            List<MeasurementForScreenshot> measuresForScreenshots =
                this._measurePlacement.PopulateMeasuresForScreenshots(kitchen, uniqueMeasurementPairs);

            kitchen.CenterTheGround();

            bool wasGroundVisible = kitchen.ground.activeSelf;

            using var groundVisibleScope =
                MelcherDeferred.Defer(
                    () => kitchen.ground.SetActive(wasGroundVisible)
                );

            screenshots.cover = CameraCapture.TakeScreenshotFromCamera(
                m_SaveCoverCamera,
                PdfImageResolution,
                beforeRender: () =>
                {
                    // Apply the camera rotation that the layout thinks looks
                    // best for the cover photo
                    m_SaveCoverCamera.transform.parent.localEulerAngles =
                        Vector3.up * kitchen.layout.saveCoverCameraYaw;

                    // The ground must be active to update the camera because
                    // the camera is a child of the ground
                    kitchen.ground.SetActive(true);

                    //using var groundVisibleScope = new ScopeGuard(
                    //    () => kitchen.ground.SetActive(wasGroundVisible)
                    //);

                    // Update the camera's group composer/framing now that the
                    // screenshot's render texture is set. Otherwise, updating
                    // the framing before the render texture is set will cause
                    // framing issues if the current render texture's aspect
                    // ratio doesn't match the screenshot's aspect ratio.
                    var vcam = m_SaveCoverCamera.transform.parent.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
                    if (m_SaveCoverCamera.TryGetComponent<Unity.Cinemachine.CinemachineBrain>(out var brain))
                    {
                        // This vcam/brain update stuff is pretty flakey. It
                        // appears to be necessary but may be improvable
                        vcam.UpdateCameraState(Vector3.up, deltaTime: 2.0f);
                        brain.ManualUpdate();
                        vcam.UpdateCameraState(Vector3.up, deltaTime: 2.0f);
                    }
                }
            );

            // Hide the ground for the remaining photos
            kitchen.ground.SetActive(false);

            Transform saveFrontCameraTransform = m_SaveFrontCamera.transform;
            float longestOutsideEdgeLengthInMeters = 0.0f;
            foreach (CabinetSpan span in kitchen.layout.spans)
            {
                // Calculate the outside edge length of this cabinet. (This is
                // a length that includes its neighbour extensions. E.g., a
                // corner or end piece.) We must remember the longest external
                // cabinet length to properly size the top-down camera
                Cuboid cabinetOutsideCuboidInSpanSpace =
                    span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                longestOutsideEdgeLengthInMeters =
                    Mathf.Max(
                        longestOutsideEdgeLengthInMeters,
                        cabinetOutsideCuboidInSpanSpace.width
                    );

                // Setup the cabinet visibility for a front screenshot. Hide
                // all cabinets that are not in the screenshot.
                foreach (ElementIndex<Cabinet> i in kitchen.layout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    bool showCabinet = span.Contains(cabinet);

                    cabinet.gameObject.SetActive(showCabinet);
                }

                // There should be at least one non-space cabinet in this
                // span for it to have its picture taken.
                bool willScreenshotSpan = !span.IsSpace();

                if (!willScreenshotSpan)
                {
                    // Skip this span because it is comprised of invisible/fake
                    // cabinets used to space out the kitchen in galley mode.
                    continue;
                }

                Cuboid cabinetOutsideCuboidInWorldSpace =
                    cabinetOutsideCuboidInSpanSpace.Transform(
                        span.orientationAlignedCabinet.GetTransform()
                    );

                // Position the camera at the cabinets mid point
                Vector3 cabinetHalfwayPosition_WorldSpace =
                    cabinetOutsideCuboidInWorldSpace.center;

                // Offset the camera relative the main cabinet's forward transform
                const float FrontCameraDistance = -10.0f;
                Vector3 cameraOffsetFromHalfway =
                    span.orientationAlignedCabinet.GetTransform().forward * FrontCameraDistance;

                saveFrontCameraTransform.position = cabinetHalfwayPosition_WorldSpace + cameraOffsetFromHalfway;
                saveFrontCameraTransform.LookAt(cabinetHalfwayPosition_WorldSpace);

                // Shift the camera up. Some products extend higher nothing
                // is ever down the cabinet
                saveFrontCameraTransform.position += Vector3.up * 0.4f;

                // Size the camera so the cabinet fills the texture
                FitFrontCamera(cabinetOutsideCuboidInSpanSpace, PdfImageResolution, tight: false);

                screenshots.front.Add(
                    CameraCapture.TakeScreenshotFromCamera(m_SaveFrontCamera, PdfImageResolution)
                );

                await ArtificialDelay();

                //
                // Capture the front plan screenshot (i.e. measurements)
                //

                // Shift the camera down. We want the cabinet centered for the
                // plan rendering.
                saveFrontCameraTransform.position -= Vector3.up * 0.4f;

                // Shift the camera left to better fit the total cabinet height measurement
                saveFrontCameraTransform.position -= saveFrontCameraTransform.right * 0.1f;

                await SetupMeasurementScreenshot(
                    kitchen,
                    span,
                    measuresForScreenshots,
                    ScreenshotAngle.Front
                );

                FitFrontCamera(
                    cabinetOutsideCuboidInSpanSpace,
                    PdfPlanImageResolution,
                    tight: true
                );

                byte[] screenshotData =
                    CameraCapture.TakeScreenshotFromCamera(
                        m_SaveFrontCamera,
                        PdfPlanImageResolution
                    );

                await ArtificialDelay();

                screenshots.frontPlan.Add(screenshotData);

                await SetupMeasurementScreenshot(
                    kitchen,
                    span,
                    measuresForScreenshots,
                    ScreenshotAngle.None
                );
            }

            // Restore cabinet visibility
            foreach (ElementIndex<Cabinet> i in kitchen.layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                cabinet.gameObject.SetActive(true);
            }

            // Take the top-down photo now that we know the longest cabinet
            // length
            Vector3 topDownLocalEulerAngles = new Vector3(
                90.0f,  // pointing down,
                0.0f,
                kitchen.layout.saveTopDownCameraRoll
            );
            m_SaveTopDownCamera.transform.localEulerAngles = topDownLocalEulerAngles;
            m_SaveTopDownCamera.orthographicSize = (longestOutsideEdgeLengthInMeters * 0.5f) + 0.5f;
            MelcherLog.Log("Longest length is: " + longestOutsideEdgeLengthInMeters);

            Vector3 recenterOffset = Vector3.zero;
            switch (kitchen.layout.style)
            {
                case KitchenStyle.Straight:
                    // Shift the camera closer to the back to better center it
                    recenterOffset = m_SaveTopDownCamera.transform.up * 0.6f;
                    break;

                case KitchenStyle.L:
                case KitchenStyle.U:
                    // The U-style needs the camera taken closer to the back
                    recenterOffset = m_SaveTopDownCamera.transform.up * 0.4f;
                    break;
            }

            m_SaveTopDownCamera.transform.position += recenterOffset;

            Vector2Int topDownScreenshotResolution = PdfImageResolution;
            topDownScreenshotResolution.y = topDownScreenshotResolution.x;
            screenshots.topDown = CameraCapture.TakeScreenshotFromCamera(m_SaveTopDownCamera, topDownScreenshotResolution);

            await ArtificialDelay();

            Vector2Int topDownPlanScreenshotResolution = PdfPlanImageResolution;
            await SetupMeasurementScreenshot(
                kitchen,
                span: null,
                measuresForScreenshots,
                ScreenshotAngle.Top
            );
            FitTopDownCamera(topDownPlanScreenshotResolution);

            TryAddNapoleonTopDownWatermark(kitchen, topDownPlanScreenshotResolution);

            screenshots.topDownPlan = CameraCapture.TakeScreenshotFromCamera(m_SaveTopDownCamera, topDownPlanScreenshotResolution);

            DisableTopDownWatermark();

            await ArtificialDelay();

            await SetupMeasurementScreenshot(
                kitchen,
                span: null,
                measuresForScreenshots,
                ScreenshotAngle.None
            );

            m_SaveTopDownCamera.transform.position -= recenterOffset;

#if UNITY_EDITOR
            // DEBUG: Save the pictures to disk. This allows them to be opened
            // up with dedicated image programs where they can be viewed full
            // screen and zoomed into (better than Unity's preview window).
#pragma warning disable CS0162  // "Unused code detected"
            if (SkipScreenshotInEditor)
            {
                MelcherLog.LogWarning(
                    $"Screenshots are skipped in the editor. See {MelcherFormat.ConsoleTypeName<KitchenScreenshotControl>()}.{MelcherFormat.ConsoleFieldName(nameof(KitchenScreenshotControl.SkipScreenshotInEditor))}."
                );
            }
            else
            {
                void SaveImage(byte[] screenshotData, string fileName)
                {
                    if (screenshotData == null)
                    {
                        return;
                    }

                    string outPath = System.IO.Path.Combine(Application.dataPath, "..", fileName + ".jpg");
                    outPath = System.IO.Path.GetFullPath(outPath);
                    System.IO.File.WriteAllBytes(outPath, screenshotData);
                    MelcherLog.Log("Wrote: " + outPath);
                }

                SaveImage(screenshots.cover, "photo_cover");
                SaveImage(screenshots.topDown, "photo_normalTopDown");
                SaveImage(screenshots.topDownPlan, "photo_planTopDown");
                for (int i = 0; i < screenshots.front.Count; ++i)
                {
                    SaveImage(screenshots.front[i], $"photo_normalFront{i}");
                }
                for (int i = 0; i < screenshots.frontPlan.Count; ++i)
                {
                    SaveImage(screenshots.frontPlan[i], $"photo_planFront{i}");
                }
            }
#pragma warning restore CS0162  // "Unused code detected"
#endif  // if UNITY_EDITOR

            return screenshots;

            /// <summary>
            ///     Used to briefly pause the function to allow the UI spinner
            ///     to visibly spin. Otherwise, the outer function blocks for
            ///     the full duration and prevents anything from animating,
            ///     resulting in poor user feedback.
            /// </summary>
            static UniTask ArtificialDelay()
            {
                if (MelcherUnityUtility.targetPlatform == RuntimePlatform.IPhonePlayer)
                {
                    // We're using random delays here to make the process feel a bit
                    // more organic as I found a constant delay feels a bit too
                    // rhythmic. Feel free to adjust.
                    const int MillisecondsDelayMin = 200;
                    const int MillisecondsDelayMax = 1000;
                    int randomMilliseconds = UnityEngine.Random.Range(MillisecondsDelayMin, MillisecondsDelayMax);
                    return UniTask.Delay(randomMilliseconds);
                }

                // No delay on non-iOS platforms. We've made speed improvements
                // on these platforms such that the added delays should not be
                // necessary.
                return UniTask.CompletedTask;
            }
            void FitFrontCamera(Cuboid cabinetOutsideCuboid, Vector2Int imageResolution, bool tight)
            {
                // Size the orthographic camera so the cabinet fills its width
                float orthographicWidth = cabinetOutsideCuboid.width * 0.5f;
                float imageWidthToHeight = imageResolution.y / (float)imageResolution.x;
                float orthographicHeight = orthographicWidth * imageWidthToHeight + (tight ? 0.2f : 0.4f);

                // Prevent the camera from becoming too small because then the
                // cabinet will extend beyond the top and bottom edges of the image
                const float MinOrthoSize = 0.9f;
                if (orthographicHeight < MinOrthoSize)
                {
                    orthographicHeight = MinOrthoSize;
                }

                m_SaveFrontCamera.orthographicSize = orthographicHeight;
            }

            /// <summary>
            /// Sizes the top-down camera so it fits however much the kitchen
            /// sprawls in the XZ plane.
            ///
            /// It does this by iterating over the cabinets and calculating
            /// their size within the camera's view-space.
            /// </summary>
            void FitTopDownCamera(Vector2Int imageResolution)
            {
                Bounds? kitchenBoundsInWorldSpace = null;

                System.Span<Vector3> boundCorners = stackalloc Vector3[8];

                foreach (CabinetSpan span in kitchen.layout.spans)
                {
                    // To accurately calculate the kitchen's bounds and account
                    // for cabinets at potentially non-orthogonal angles, we
                    // cannot directly merge bounds; those only support axis
                    // aligned bounds. Therefore, we fetch and convert the
                    // vertices of each span's cuboid local-space to world-space
                    // and size the kitchen bounds to contain each of these
                    // points individually instead.

                    Cuboid spanOutsideCuboidInSpanSpace =
                        span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                    MelcherUnityUtility.CalculateAABBCorners(
                        spanOutsideCuboidInSpanSpace.min,
                        spanOutsideCuboidInSpanSpace.max,
                        boundCorners
                    );

                    // Convert local-space corners into world-space.
                    for (int i = 0; i < boundCorners.Length; ++i)
                    {
                        boundCorners[i] = span.orientationAlignedCabinet.GetTransform().TransformPoint(boundCorners[i]);
                    }

                    if (kitchenBoundsInWorldSpace.HasValue)
                    {
                        // Expand the existing bounds to include each of the new
                        // points.

                        Bounds bounds = kitchenBoundsInWorldSpace.Value;
                        foreach (Vector3 corner in boundCorners)
                        {
                            bounds.Encapsulate(corner);
                        }
                        kitchenBoundsInWorldSpace = bounds;
                    }
                    else
                    {
                        // No bounds started yet so make one starting from a
                        // single point.

                        Bounds bounds = new Bounds(boundCorners[0], size: Vector3.zero);
                        foreach (Vector3 corner in boundCorners.Slice(1))
                        {
                            bounds.Encapsulate(corner);
                        }
                        kitchenBoundsInWorldSpace = bounds;
                    }
                }

                Transform topDownCameraTransform =
                    m_SaveTopDownCamera.transform;

                Vector3 sizeInCameraLocalSpace =
                   topDownCameraTransform.InverseTransformDirection(kitchenBoundsInWorldSpace.Value.size);

                // Add some breathing room. This is necessary to ensure the
                // outer cabinet measurements are visible. E.g., U-Shaped
                // kitchens have their labels cut-off, even with a breathing
                // room of 1.0.
                sizeInCameraLocalSpace.x += 1.25f;
                sizeInCameraLocalSpace.y += 1.25f;

                float orthographicHeight;

                // Fit the longest of the two axises
                float screenshotAspectRatio = imageResolution.x / (float)imageResolution.y;
                float kitchenAspectRatio = sizeInCameraLocalSpace.x / sizeInCameraLocalSpace.y;
                if (screenshotAspectRatio < kitchenAspectRatio)
                {
                    // Kitchen is wider than screenshot's width

                    float orthographicWidth = sizeInCameraLocalSpace.x * 0.5f;
                    float imageWidthToHeight = imageResolution.y / (float)imageResolution.x;
                    orthographicHeight = orthographicWidth * imageWidthToHeight;
                }
                else
                {
                    // Kitchen is taller than screenshot's height

                    orthographicHeight = sizeInCameraLocalSpace.y * 0.5f;
                }

                m_SaveTopDownCamera.orthographicSize = orthographicHeight;

                // Center the camera over the kitchen.
                Vector3 position = topDownCameraTransform.position;
                position.x = kitchenBoundsInWorldSpace.Value.center.x;
                position.z = kitchenBoundsInWorldSpace.Value.center.z;
                topDownCameraTransform.position = position;
            }
        }

        /// <summary>
        ///     Positions and activates a corner label watermark on the top-down
        ///     plan screenshot for Napoleon. The watermark displays standard
        ///     cabinet and countertop depth values ("CABINET DEPTH: 24\"" and
        ///     "COUNTERTOP DEPTH: 26\"") as a clarification note anchored to
        ///     the bottom-left corner of the camera's orthographic view. Font
        ///     size is scaled relative to the camera's orthographic size so the
        ///     label remains proportionally readable regardless of kitchen scale.
        ///     Does nothing if <c>m_PlanTopDownCornerLabel</c> is unassigned.
        /// </summary>
        protected void TryAddNapoleonTopDownWatermark(Kitchen kitchen, Vector2Int topDownPlanScreenshotResolution)
        {
            if(kitchen.selectedBrandCatalog == null)
            {
                return;
            }

            // only proceed if we're using napoleon
            if(kitchen.selectedBrandCatalog.cabinetManufacturerId != 95)
            {
                return;
            }

            if (m_PlanTopDownCornerLabel != null)
            {
                float halfHeight = m_SaveTopDownCamera.orthographicSize;
                float halfWidth  = halfHeight * (topDownPlanScreenshotResolution.x / (float)topDownPlanScreenshotResolution.y);
                float cameraSize = m_SaveTopDownCamera.orthographicSize;
                float fontSize = 0.5f;

                Transform camTransform = m_SaveTopDownCamera.transform;
                Vector3 cornerPos = camTransform.position
                    - camTransform.up    * (halfHeight - 0.3f * cameraSize)
                    - camTransform.right * (halfWidth  - 0.08f * cameraSize)
                    + camTransform.forward * (m_SaveTopDownCamera.nearClipPlane + 0.1f);

                m_PlanTopDownCornerLabel.transform.position = cornerPos;
                m_PlanTopDownCornerLabel.transform.LookAt(cornerPos + camTransform.forward, camTransform.up);
                m_PlanTopDownCornerLabel.text = "CABINET DEPTH: 24\"\nCOUNTERTOP DEPTH: 26\"";
                m_PlanTopDownCornerLabel.fontSize = fontSize * cameraSize;
                m_PlanTopDownCornerLabel.gameObject.SetActive(true);
            }
        }

        protected void DisableTopDownWatermark()
        {
            if (m_PlanTopDownCornerLabel != null)
            {
                m_PlanTopDownCornerLabel.gameObject.SetActive(false);
            }
        }


        public void Initialize(SessionStatus session)
        {
            this._session = session;
            this._measurePlacement = new PlanMeasurePlacement(m_MeasurePrefabPool, m_CabinetMeasureColorX, m_CabinetMeasureColorY, m_CabinetMeasureColorZ, session);
        }

        protected void Awake()
        {
            m_DesignSceneSkybox = RenderSettings.skybox;

            // Create the pool of measure objects that will be used to measure
            // the gaps between products
            Transform myTransform = transform;
            for (int i = 0; i < m_MeasurePrefabCount; ++i)
            {
                Measure pooledMeasure =
                    Instantiate(m_MeasurePrefab, parent: myTransform);
                pooledMeasure.minimumInchDivisions = 32;
                pooledMeasure.gameObject.SetActive(false);
                m_MeasurePrefabPool.Add(pooledMeasure);
            }
        }

        /// <summary>
        ///     This function is meant to be called immediately before and
        ///     immediately after a screenshot is taken. It handles the
        ///     visibility of objects for a screenshot from a particular angle.
        ///     If the screenshot angle is none, it reverses any changes that
        ///     were made in order to restore the original visibility.
        /// </summary>
        protected async UniTask SetupMeasurementScreenshot(
            Kitchen         kitchen,
            CabinetSpan     span,
            List<MeasurementForScreenshot> measuresForScreenshots,
            ScreenshotAngle screenshotAngle)
        {
            bool showMeasurements = screenshotAngle != ScreenshotAngle.None;

            bool alwaysShowBacking =
                screenshotAngle == ScreenshotAngle.None ||
                screenshotAngle == ScreenshotAngle.Top;

            bool isNoneAngle =
                screenshotAngle == ScreenshotAngle.None;

            bool isTopAngle =
                screenshotAngle == ScreenshotAngle.Top;

            MeasurementUnit units = this._session.UserProfile.activeUnits;

            foreach (CabinetSpan otherSpan in kitchen.layout.spans)
            {
                HandleBackingVisibility(
                    otherSpan,
                    isVisible: alwaysShowBacking || otherSpan == span
                );

                foreach (ElementIndex<Cabinet> i in otherSpan.cabinets)
                {
                    Cabinet cabinet = i.element;

                    bool isLabelVisible =
                        !isNoneAngle && (isTopAngle || otherSpan == span);

                    cabinet.SetInfoLabelVisibility(isLabelVisible, screenshotAngle);

                    foreach (Occupant occupant in cabinet.occupants)
                    {
                        HandleOccupantVisibility(
                            kitchen,
                            screenshotAngle,
                            occupant,
                            showOccupantMeasurements: isLabelVisible,
                            units
                        );
                    }
                }
            }

            foreach (MeasurementForScreenshot measureForScreenshot in measuresForScreenshots)
            {
                bool isSameAngle = screenshotAngle == measureForScreenshot.screenshotAngle;
                bool isSameCabinetSpan = span == null || measureForScreenshot.cabinetSpan == span;

                measureForScreenshot.measure.gameObject.SetActive(
                    showMeasurements && isSameAngle && isSameCabinetSpan
                );
            }

            Material countertopMaterial = m_MeasurementScreenshotCountertopMaterial;
            Material cabinetMaterial = m_MeasurementScreenshotCabinetMaterial;
            Material cutOutMaterial = m_MeasurementScreenshotCutOutMaterial;

            if (screenshotAngle == ScreenshotAngle.None)
            {
                // Restore user selected finishes
                countertopMaterial = null;
                cabinetMaterial = null;
                cutOutMaterial =  cabinetMaterial;
            }

            if (cabinetMaterial == null)
            {
                cabinetMaterial = await kitchen.GetSelectedOrDefaultFinishMaterial(kitchen.selectedCabinetFinish);
            }

            if (countertopMaterial == null)
            {
                countertopMaterial = await kitchen.GetSelectedOrDefaultFinishMaterial(kitchen.selectedCountertopFinish);
            }

            await kitchen.SetTemporaryFinishMaterials(
                cabinetMaterial,
                countertopMaterial,
                cutOutMaterial
            );

            // --
            // Override the material on the cabinet backings so they don't
            // appear to merge into one shape and colour.
            if (screenshotAngle == ScreenshotAngle.Top)
            {
                foreach (CabinetSpan otherSpan in kitchen.layout.spans)
                {
                    otherSpan.backing?.OnCabinetMaterialsChanged(kitchen,
                        m_MeasurementScreenshotCabinetBackingForTopDownMaterial,
                        m_MeasurementScreenshotCabinetBackingForTopDownMaterial
                    );
                }
            }

            // --
            // Now we have to fix up some artifacts that are caused by our
            // imperfect shelf rendering feature:
            //
            // The first issue is that shelf countertops will render with the
            // same colour as a cut-out if we are rendering a plan screenshot.
            // This makes cut-outs look deeper than they are from front or back
            // screenshots. We fix this by overriding the material that was
            // just assigned in SetTemporaryFinishMaterials with a material
            // that better distinguishes the shelf.
            //
            // The next issue is that the shelf countertop is visible within
            // all of its occupants cut-outs when viewed from the front. We
            // must stretch its cabinet cut-out to mask this issue.

            Material shelfCountertopMaterial =
                (screenshotAngle == ScreenshotAngle.Top) ?
                    m_MeasurementScreenshotCabinetMaterial :
                    m_MeasurementScreenshotCountertopMaterial;

            bool needsShelfStencilFix =
                (screenshotAngle == ScreenshotAngle.Front) || (screenshotAngle == ScreenshotAngle.Back);

            foreach (ElementIndex<Cabinet> i in kitchen.layout.EnumerateCabinets())
            {
                Cabinet otherCabinet = i.element;

                foreach (Occupant occupant in otherCabinet.occupants)
                {
                    if (!occupant.TryGetComponent(out CabinetShelf shelf))
                    {
                        // Skip this occupant because we are only fixing up
                        // shelves and their occupants which this is neither.
                        continue;
                    }

                    if (screenshotAngle.IsPlanAngle())
                    {
                        // Fix the shelf's countertop material so it looks
                        // different from other products.
                        await shelf.OnCabinetMaterialsChanged(cabinetMaterial, shelfCountertopMaterial);
                    }

                    foreach (Occupant occupantInShelf in shelf.occupants)
                    {
                        float interiorHeightInMeters =
                            occupantInShelf.currentCutOutSizeInMeters.y;

                        if (!needsShelfStencilFix)
                        {
                            // Restore the cabinet cut-out's height to it's
                            // original size so things look normal after the
                            // screenshots are complete.
                            interiorHeightInMeters -= kitchen.countertopThicknessInMeters;
                        }

                        TransformHelper cabinetCutOut = occupantInShelf.productVisualContentInstance.cabinetCutOut.transformHelper;
                        Vector3 cutOutSizeInMeters = cabinetCutOut.sizeInMeters;
                        cutOutSizeInMeters.y = interiorHeightInMeters;
                        cabinetCutOut.sizeInMeters = cutOutSizeInMeters;
                    }
                }
            }

            RenderSettings.skybox = showMeasurements ? m_MeasurementScreenshotSkyboxMaterial : m_DesignSceneSkybox;

            static void HandleBackingVisibility(
                CabinetSpan cabinetSpan,
                bool        isVisible)
            {
                cabinetSpan.backing?.gameObject.SetActive(isVisible);
            }

            static void HandleOccupantVisibility(
                Kitchen         kitchen,
                ScreenshotAngle screenshotAngle,
                Occupant        occupant,
                bool            showOccupantMeasurements,
                MeasurementUnit units)
            {
                // Show the product's model if we're not showing
                // measurements. Hide the product model if we are
                occupant.productVisualContentInstance.SetProductVisibility(!showOccupantMeasurements);

                if (showOccupantMeasurements)
                {
                    Vector3 occupantSize = occupant.productInfo.cutOutSizeInMeters;

                    // Check which screenshot angle is being used to
                    // determine which product measurements should be
                    // shown. E.g. top-down screenshots should omit
                    // products that are anchored at the cabinet bottom
                    if (screenshotAngle == ScreenshotAngle.Top)
                    {
                        bool hasWidthAndDepth = occupantSize.x > 0.0f && occupantSize.z > 0.0f;

                        bool isCutOutVisible =
                            occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Up);

                        showOccupantMeasurements = hasWidthAndDepth && isCutOutVisible;
                    }
                    else
                    {
                        bool hasWidthAndDepth = occupantSize.x > 0.0f && occupantSize.y > 0.0f;

                        bool isCutOutVisible =
                            occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Front);

                        showOccupantMeasurements = hasWidthAndDepth && isCutOutVisible;
                    }
                }

                occupant.productVisualContentInstance.SetCutOutMeasurementsVisibility(showOccupantMeasurements, screenshotAngle);

                if (showOccupantMeasurements)
                {
                    PlanMeasurePlacement.SetupOccupantCutOutMeasurements(kitchen, occupant, units);
                }

                if (occupant.TryGetComponent(out CabinetShelf nestedVolume))
                {
                    foreach (Occupant nestedOccupant in nestedVolume.occupants)
                    {
                        HandleOccupantVisibility(
                            kitchen,
                            screenshotAngle,
                            nestedOccupant,
                            showOccupantMeasurements,
                            units
                        );
                    }
                }
            }
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] protected Camera m_SaveCoverCamera = null;
        [SerializeField] protected Camera m_SaveTopDownCamera = null;
        [SerializeField] protected Camera m_SaveFrontCamera = null;

        [Space]
        [SerializeField] protected Material m_MeasurementScreenshotSkyboxMaterial = null;
        [SerializeField] protected Material m_MeasurementScreenshotCountertopMaterial = null;
        [SerializeField] protected Material m_MeasurementScreenshotCabinetMaterial = null;

        [Tooltip("To help cabinet backings stand out in the top-down " +
            "a different material is used.")]
        [SerializeField] protected Material m_MeasurementScreenshotCabinetBackingForTopDownMaterial = null;
        [SerializeField] protected Material m_MeasurementScreenshotCutOutMaterial = null;

        [Space]
        [Tooltip("Number of measure prefabs to spawn that will be used to " +
            "measure the distance between neighbouring occupants.")]
        [SerializeField] protected int m_MeasurePrefabCount = 300;
        [SerializeField] protected Measure m_MeasurePrefab = default;

        [Space]
        [SerializeField] protected Color m_CabinetMeasureColorX = default;
        [SerializeField] protected Color m_CabinetMeasureColorY = default;
        [SerializeField] protected Color m_CabinetMeasureColorZ = default;

        [Space]
        [Tooltip("Text label placed in the top-left corner of the plan top-down screenshot.")]
        [SerializeField] protected TMP_Text m_PlanTopDownCornerLabel = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected Material m_DesignSceneSkybox;

        protected List<Measure> m_MeasurePrefabPool = new List<Measure>();

        // ====================================================================
        // Nested types
        // ====================================================================


    }   // class KitchenScreenshotControl

}   // namespace KitchenDesign.Photos

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-02
/// ===========================================================================
