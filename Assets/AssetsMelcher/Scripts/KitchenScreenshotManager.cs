namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using KitchenDesign.Geometry;

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
    public class KitchenScreenshotManager : MelcherMonoBehaviour
    {
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

        public static byte[] TakeScreenshotFromCamera(Camera screenshotCamera, Vector2Int screenshotResolution, System.Action beforeRender = null, System.Action afterRender = null)
        {
            if (Application.isEditor && SkipScreenshotInEditor)
            {
                return null;
            }

            // Force the garbage collector to run to free up memory (iOS).
            // Without this, low-memory devices may run out of memory.
            Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

            // Get a temporary render texture for our screenshot (with MSAA
            // and stenciling enabled). MSAA: for soft edges, stenciling: for
            // cut-out support (render feature)
            RenderTextureDescriptor descriptor =
                new RenderTextureDescriptor(
                    screenshotResolution.x,
                    screenshotResolution.y
                );

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // This fixes an issue we were having on WebGL where the render
                // texture had weird garbage and clipping effects.
                // TODO: We should determine which format Unity is selecting and
                //       use that explicitly.
                descriptor.depthBufferBits = 16;
            }
            else
            {
                descriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
            }

            descriptor.msaaSamples = 8;
            descriptor.mipCount = 0;
            descriptor.useMipMap = false;

            // I was using RenderTexture.GetTemporary() and ReleaseTemporary(),
            // but switched it out when I started having an issue on WebGL
            // where the rendered images had garbage pixels and grey clipping
            // effects (only rendering bottom left corner or bottom half of
            // image). -Ryan
            RenderTexture screenshotTargetTexture = new RenderTexture(descriptor);

            using var releaseRenderTexture =
                MelcherDeferred.Defer(
                    toCapture: screenshotTargetTexture,
                    onDispose: static renderTexture =>
                    {
                        if (renderTexture)
                        {
                            DestroyImmediate(renderTexture);
                        }
                    }
                );

            Debug.Assert(screenshotTargetTexture.width == screenshotResolution.x);
            Debug.Assert(screenshotTargetTexture.height == screenshotResolution.y);

            // Render the screenshot camera to a temporary render texture
            RenderTexture targetTextureToRestore = screenshotCamera.targetTexture;

            using var restoreCameraTarget =
                MelcherDeferred.Defer(
                    () => screenshotCamera.targetTexture = targetTextureToRestore
                );

            screenshotCamera.targetTexture = screenshotTargetTexture;

            beforeRender?.Invoke();
            screenshotCamera.Render();
            afterRender?.Invoke();

            //
            // Copy the render texture pixels into a Texture2D so the
            // screenshot's bytes can be encoded as an image and later uploaded
            // to the server.
            //

            // Create readable texture (one that can use EncodeToJPG)
            Texture2D screenshot =
                new Texture2D(
                    screenshotResolution.x,
                    screenshotResolution.y,
                    TextureFormat.ARGB32,
                    mipChain: false
                );

            if (!screenshot)
            {
                throw new System.InvalidOperationException("Failed to create texture for reading screenshot.");
            }

            using var releaseScreenshot =
                MelcherDeferred.Defer(
                    toCapture: screenshot,
                    onDispose: static screenshot =>
                    {
                        if (screenshot)
                        {
                            DestroyImmediate(screenshot);
                        }
                    }
                );

            // Make the screenshot render texture active so it can be read
            RenderTexture renderTextureToRestore = RenderTexture.active;
            RenderTexture.active = screenshotTargetTexture;

            using var restoreRenderTexture =
                MelcherDeferred.Defer(
                    toCapture: renderTextureToRestore,
                    onDispose: static renderTextureToRestore => RenderTexture.active = renderTextureToRestore
                );

            Debug.Assert(screenshotTargetTexture.width == screenshot.width);
            Debug.Assert(screenshotTargetTexture.height == screenshot.height);

            // Copy render texture to a readable/encodable texture
            Rect rect = new Rect(0, 0, screenshotResolution.x, screenshotResolution.y);
            screenshot.ReadPixels(rect, 0, 0);
            screenshot.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            // We were returning a Texture2D, but to reduce memory overhead
            // we will only return the jpeg bytes.
            byte[] screenshotData = screenshot.EncodeToJPG();

            return screenshotData;
        }

        /// <summary>
        ///     Take all screenshots for the design's PDF.
        /// </summary>
        public async UniTask<ScreenshotGallery> TakeScreenshots(Kitchen kitchen)
        {
            using var drawScope = Cuboid.StartDrawScope();

            ScreenshotGallery screenshots = new ScreenshotGallery();

            // Clear the user's selection so the selection highlight doesn't
            // render in the screenshots.
            await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null);

            // Figure out where products are in relation to each other. We'll
            // use this to find pairs of products to draw measurements between
            List<OccupantNearestNeighbours> occupantToNearestNeighbours =
                FindNeighboursOfAllOccupants(kitchen);

            // A list of measurement pairs. Each occupant has 1 closest
            // neighbour for each 6 ordinal directions (CabinetLocalDirection).
            List<OccupantMeasurementPair> uniqueMeasurementPairs =
                FindUniqueOccupantMeasurementPairs(kitchen, occupantToNearestNeighbours);

            List<MeasurementForScreenshot> measuresForScreenshots =
                PopulateMeasuresForScreenshots(kitchen, uniqueMeasurementPairs);

            kitchen.CenterTheGround();

            bool wasGroundVisible = kitchen.ground.activeSelf;

            using var groundVisibleScope =
                MelcherDeferred.Defer(
                    () => kitchen.ground.SetActive(wasGroundVisible)
                );

            screenshots.cover = TakeScreenshotFromCamera(
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
                    TakeScreenshotFromCamera(m_SaveFrontCamera, PdfImageResolution)
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
                    TakeScreenshotFromCamera(
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
            screenshots.topDown = TakeScreenshotFromCamera(m_SaveTopDownCamera, topDownScreenshotResolution);

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

            screenshots.topDownPlan = TakeScreenshotFromCamera(m_SaveTopDownCamera, topDownPlanScreenshotResolution);

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
                    $"Screenshots are skipped in the editor. See {MelcherFormat.ConsoleTypeName<KitchenScreenshotManager>()}.{MelcherFormat.ConsoleFieldName(nameof(KitchenScreenshotManager.SkipScreenshotInEditor))}."
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

        /// <summary>
        ///     For each occupant on each cabinet in the kitchen, find the
        ///     occupant's nearest neighbouring occupants. The nearest neighbour
        ///     is the one that the occupant would bump into if it moved
        ///     orthogonally. I.e., left, right, up, down, forward, or back.
        ///     <br /><br />
        ///
        ///     If a direction doesn't have a neighbour, it will record the
        ///     distance to the edge of the cabinet. This data will later be
        ///     used to calculate the unique pairs of neighbours which will
        ///     further be used to place measurement lines between each unique
        ///     pair.
        /// </summary>
        protected static List<OccupantNearestNeighbours> FindNeighboursOfAllOccupants(Kitchen kitchen)
        {
            List<OccupantNearestNeighbours> occupantToNeighbours = new();

            foreach (ElementIndex<Cabinet> i in kitchen.layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                foreach (Occupant occupant in cabinet.occupants)
                {
                    FindNeighboursOfOccupant(occupant, occupantToNeighbours);
                }
            }

            return occupantToNeighbours;

            /// <summary>
            ///     The logic for finding nearest neighbour. This is
            ///     encapsulated in its own function as it will be repeated
            ///     for products within shelves and therefore it may be called
            ///     recursively.
            /// </summary>
            static void FindNeighboursOfOccupant(Occupant occupant, List<OccupantNearestNeighbours> occupantToNeighbours)
            {
                OccupantNearestNeighbours entry = new ();

                entry.occupant = occupant;

                occupant.TryGetComponent(out CabinetShelf nestedVolume);

                if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Up))
                {
                    // This product is visible from the top. Limit neighbour
                    // search to other occupants that are also visible from the
                    // top.
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Left);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Right);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Front);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Top, CabinetLocalDirection.Back);
                }

                if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Front))
                {
                    // Only search up from occupants that are not in a shelf.
                    // If a product is in a shelf, the product above will be the
                    // shelf.
                    bool allowSearchUp = occupant.occupiedVolume.parentVolume == null;

                    // Only search down from occupants that are not shelves.
                    // This avoid a measurement from the shelf that overlaps its
                    // nested occupants.
                    bool allowSearchDown = !nestedVolume;

                    // This product is visible from the front. Limit neighbour
                    // search to other occupants that are also visible from the
                    // front.
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Left);
                    FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Right);

                    if (allowSearchUp)
                    {
                        FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Up);
                    }

                    if (allowSearchDown)
                    {
                        FindNeighbourOfOccupantInDirection(occupant, entry, ScreenshotAngle.Front, CabinetLocalDirection.Down);
                    }
                }

                occupantToNeighbours.Add(entry);

                if (nestedVolume)
                {
                    // The current occupant is a shelf. To ensure we handle
                    // *all* occupants, we must recurse into any of the shelf's
                    // occupants and find their neighbours.
                    foreach (Occupant nestedOccupant in nestedVolume.occupants)
                    {
                        FindNeighboursOfOccupant(nestedOccupant, occupantToNeighbours);
                    }
                }
            }

            /// <summary>
            ///     Given an occupant, find its nearest neighbour in the
            ///     direction of <paramref name="directionOfNeighbour"/>.
            /// </summary>
            static void FindNeighbourOfOccupantInDirection(
                Occupant                    occupant,
                OccupantNearestNeighbours   entry,
                ScreenshotAngle             screenshotAngle,
                CabinetLocalDirection       directionOfNeighbour)
            {
                OccupantAndDistance nearestNeighbour =
                    entry.GetNearestInDirection(screenshotAngle, directionOfNeighbour);

                // Use a predicate to filter invalid neighbours for the
                // requested screenshot angle (e.g., we don't want the top-down
                // picture to measure between products that aren't visible from
                // that angle)
                OccupiableVolume.FindNearestOccupantPredicate neighbourPredicate = null;
                switch (screenshotAngle)
                {
                    case ScreenshotAngle.Top:
                        neighbourPredicate = IsFlushWithTop;
                        break;

                    case ScreenshotAngle.Front:
                        neighbourPredicate = IsFlushWithFront;
                        break;

                    default:
                        throw new System.NotImplementedException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");
                }

                OccupiableVolume occupiedVolume = occupant.occupiedVolume;

                nearestNeighbour.occupant =
                    occupiedVolume.FindNearestOccupantInDirection(
                        occupant,
                        directionOfNeighbour,
                        out nearestNeighbour.distanceInMeters,
                        neighbourPredicate
                    );

                if (nearestNeighbour.occupant == null && occupant.occupiedVolume is Cabinet occupiedCabinet)
                {
                    // This is a measurement to the edge of a cabinet. We will
                    // ensure the measurement correctly measures to the
                    // available visible area (as opposed to purely the
                    // occupiable cuboid).
                    nearestNeighbour.distanceInMeters =
                        occupiedCabinet.AdjustCabinetDistanceToEdgeOfVisualArea(
                            nearestNeighbour.distanceInMeters,
                            directionOfNeighbour
                        );
                }

                entry.SetNearestInDirection(screenshotAngle, directionOfNeighbour, nearestNeighbour);
            }

            /// <summary>
            ///     A predicate for checking if an occupant is flush with the
            ///     top of the cabinet. Intended to be used to filter which
            ///     products are considered when finding an occupant's nearest
            ///     neighbours for top-down screenshots.
            /// </summary>
            static bool IsFlushWithTop(Occupant otherOccupant)
            {
                return otherOccupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Up);
            }

            /// <summary>
            ///     A predicate for checking if an occupant is flush with the
            ///     front of the cabinet. Intended to be used to filter which
            ///     products are considered when finding an occupant's nearest
            ///     neighbours for front screenshots.
            /// </summary>
            static bool IsFlushWithFront(Occupant otherOccupant)
            {
                return otherOccupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Front);
            }
        }

        /// <summary>
        ///     Take a list of previously found occupant neighbours and
        ///     construct a new list of all the unique pairs of closest
        ///     neighbours. These new pairs can be used to create the measure
        ///     objects between occupants without any duplicate measurements.
        /// </summary>
        /// <param name="occupantToNearestNeighbours"></param>
        /// <returns></returns>
        protected static List<OccupantMeasurementPair> FindUniqueOccupantMeasurementPairs(
            Kitchen kitchen,
            List<OccupantNearestNeighbours> occupantToNearestNeighbours)
        {
            List<OccupantMeasurementPair> measurementPairs = new ();

            foreach (OccupantNearestNeighbours nearestNeighbours  in occupantToNearestNeighbours)
            {
                Occupant occupant = nearestNeighbours.occupant;

                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Left);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Right);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Down);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Front, CabinetLocalDirection.Up);

                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Left);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Right);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Front);
                RegisterOccupantMeasurementPairIfUnique(measurementPairs, occupant, nearestNeighbours, ScreenshotAngle.Top, CabinetLocalDirection.Back);
            }

            return measurementPairs;

            void RegisterOccupantMeasurementPairIfUnique(
                List<OccupantMeasurementPair>   measurementPairs,
                Occupant                        occupant,
                OccupantNearestNeighbours       nearestNeighbours,
                ScreenshotAngle                 screenshotAngle,
                CabinetLocalDirection           direction)
            {
                OccupantAndDistance nearestNeighbourInDirection =
                    nearestNeighbours.GetNearestInDirection(screenshotAngle, direction);

                if (nearestNeighbourInDirection.distanceInMeters <= 0.0f)
                {
                    // Skip. The occupants are flush/touching and don't need a
                    // marking
                    return;
                }

                // This is a kludge to increase the reported cut-out distance
                // of plunge cuts to account for the thickness of the
                // countertop.
                float extraDistanceInMetersForLabelOnly = 0.0f;

                if (nearestNeighbourInDirection.occupant)
                {
                    // Sort the pair so that the we can reliably detect if this
                    // is a unique pair. (I.e., we can rely on the occupant with
                    // the smallest offset in the cabinet's local-space being
                    // the first entry in the pair.)
                    Occupant a = occupant;
                    Occupant b = nearestNeighbourInDirection.occupant;
                    if (Occupant.CompareByCabinetPosition(a, b) > 0)
                    {
                        MelcherCommon.Swap(ref a, ref b);
                        direction = direction.Reverse();
                    }

                    foreach (OccupantMeasurementPair pair in measurementPairs)
                    {
                        if (pair.occupantA == a && pair.occupantB == b && pair.screenshotAngle == screenshotAngle)
                        {
                            // This pair is not unique. Abort.
                            return;
                        }
                    }

                    measurementPairs.Add(
                        new OccupantMeasurementPair(
                            a,
                            b,
                            nearestNeighbourInDirection.distanceInMeters,
                            extraDistanceInMetersForLabelOnly,
                            direction,
                            screenshotAngle
                        )
                    );
                }
                else
                {
                    // Correct for the "downward growing countertop" problem by
                    // smudging the reported distance to the end of the cabinet
                    // by the countertop's thickness.

                    if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Up) &&
                        direction.ToAxis() == MeasurementAxis.Y)
                    {
                        extraDistanceInMetersForLabelOnly = kitchen.countertopThicknessInMeters;
                    }

                    measurementPairs.Add(
                        new OccupantMeasurementPair(
                            occupant,
                            null,
                            nearestNeighbourInDirection.distanceInMeters,
                            extraDistanceInMetersForLabelOnly,
                            direction,
                            screenshotAngle
                        )
                    );
                }
            }
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

            MeasurementUnit units = ProFitGlobals.userProfile.activeUnits;

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
                    SetupOccupantCutOutMeasurements(kitchen, occupant, units);
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

        /// <summary>
        ///     Calculate the line segments used to render the occupant's
        ///     cut-out measurements.
        /// </summary>
        protected static void SetupOccupantCutOutMeasurements(Kitchen kitchen, Occupant occupant, MeasurementUnit units)
        {
            // Fetch the cabinet's cut-out mode in case there are any special
            // rules as they will affect the measurements here.
            CabinetCutOutMode cabinetCutOutMode =
                CabinetCutOutMode.Normal;

            if (occupant.cabinet is Cabinet cabinet &&
                cabinet.cabinetSectionVisualPrefab)
            {
                // The countertop cut-out does not go all the way through the
                // overhang and the cabinet's cut-out does not extend all the
                // way to the countertop.
                cabinetCutOutMode = cabinet.cabinetSectionVisual.info.sectionCutOutMode;
            }

            ProductVisualContent productVisual = occupant.productVisualContentInstance;

            Cuboid cutOutCuboidInVolumeSpace =
                occupant.GetCutOutCuboidInVolumeSpace();

            if (!ReferenceEquals(occupant.occupiedVolume, occupant.cabinet))
            {
                cutOutCuboidInVolumeSpace.position +=
                    occupant.occupiedVolume.occupiableCuboid.position;
            }

            occupant.TryGetComponent(out CabinetShelf productAsShelf);

            bool isProductOnOrAtFront = occupant.productVisual.info.anchor.IsOnOrAtFront();

            float normalizedCuboidZ = isProductOnOrAtFront ? 0.0f : 1.0f;

            // Define offsets used to push the measurement labels away from
            // cabinet so they render above everything else
            const float ProductMeasurementOffset = 10.0f * Units.Inches2Meters;
            Vector3 towardsFront = Vector3.back * ProductMeasurementOffset;
            Vector3 towardsBack = -towardsFront;
            Vector3 towardsCamera = isProductOnOrAtFront ? towardsFront : towardsBack;
            Vector3 upward = Vector3.up * ProductMeasurementOffset;

            // Calculate the points for the x-axis cut-out on the front
            LineSegment widthForFrontOrBack =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 0.0f, normalizedCuboidZ),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 0.0f, normalizedCuboidZ)
                );
            // Shift the line slightly into the cut-out box
            const float XLabelMetersFromBottom = 1 * Units.Inches2Meters;
            widthForFrontOrBack.begin.y += XLabelMetersFromBottom;
            widthForFrontOrBack.end.y += XLabelMetersFromBottom;
            // Shift the label closer to the camera
            widthForFrontOrBack.begin += towardsCamera;
            widthForFrontOrBack.end += towardsCamera;

            // Calculate the points for the x-axis cut-out on the top
            LineSegment widthForTop =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 1.0f, 0.0f),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 1.0f, 0.0f)
                );
            if (productAsShelf)
            {
                // Shift the line slightly out of the cut-out box. This avoids
                // the shelf's width measurement from overlapping any nested
                // occupants in the shelf.
                widthForTop.begin.z -= XLabelMetersFromBottom * 3;
                widthForTop.end.z -= XLabelMetersFromBottom * 3;
            }
            else
            {
                // Shift the line slightly into the cut-out box
                widthForTop.begin.z += XLabelMetersFromBottom;
                widthForTop.end.z += XLabelMetersFromBottom;
            }
            // Shift the label closer to the camera
            widthForTop.begin += upward;
            widthForTop.end += upward;

            // Calculate the points for the y-axis cut-out
            LineSegment height =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 0.0f, normalizedCuboidZ),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 1.0f, normalizedCuboidZ)
                );
            if (cabinetCutOutMode == CabinetCutOutMode.StollFixedSmokerCabinet &&
                !occupant.productVisual.info.anchor.IsAtBottom())
            {
                height.end.y -= Const.Stoll.FixedSmokerCabinetCutOutMetersToKeepBelowCountertop + kitchen.countertopThicknessInMeters;
            }
            // Shift the line slightly into the cut-out box
            const float YLabelMetersFromLeft = 1 * Units.Inches2Meters;
            height.begin.x -= YLabelMetersFromLeft;
            height.end.x -= YLabelMetersFromLeft;
            // Shift the label closer to the camera
            height.begin += towardsCamera;
            height.end += towardsCamera;

            // Calculate the points for the z-axis cut-out
            LineSegment depth =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 1.0f, 0.0f),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 1.0f, 1.0f)
                );
            // Shift the line slightly into the cut-out box
            const float ZLabelMetersFromLeft = 1 * Units.Inches2Meters;
            depth.begin.x += ZLabelMetersFromLeft;
            depth.end.x += ZLabelMetersFromLeft;
            // Shift the label closer to the camera
            depth.begin += upward;
            depth.end += upward;
            // If the product is at the top and directly at the front or back,
            // we must include the countertop overhang in the cut-out size
            if (!occupant.productVisual.info.anchor.IsAtBottom() && cabinetCutOutMode == CabinetCutOutMode.Normal)
            {
                if (isProductOnOrAtFront && occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Front))
                {
                    depth.begin.z -= kitchen.baseCountertopOverhangInMeters.towardMinZ;
                }
                else if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Back))
                {
                    depth.end.z += kitchen.baseCountertopOverhangInMeters.towardMaxZ;
                }
            }

            //Vector3 pointA = product.cabinet.GetTransform().TransformPoint(height.begin);
            //Vector3 pointB = product.cabinet.GetTransform().TransformPoint(height.end);
            //Debug.DrawLine(pointA, pointB, Color.black, 5.0f, false);

            // Calculate the label positions
            Vector3 frontOrBackLabelPositionInProductSpace =
                PointOnCuboid(cutOutCuboidInVolumeSpace, 0.5f, 0.5f, normalizedCuboidZ) + towardsCamera;

            Vector3 topLabelPositionInProductSpace =
                PointOnCuboid(cutOutCuboidInVolumeSpace, 0.5f, 1.0f, 0.5f) + upward;

            productVisual.SetCutOutMeasurementsUnits(units);

            Transform localOccupantOrientationTransform =
                occupant.cabinet.GetLocalOrientationForOccupants();

            Cabinet cab = (Cabinet)occupant.cabinet;
            if (cab.cabinetSectionVisualPrefab)
            {
                localOccupantOrientationTransform =
                    cab.cabinetSectionVisualPrefab.sectionOccupantOrientation;
            }

            productVisual.SetCutOutMeasurementsPositions(
                occupant.productVisual.info,
                occupant.cabinet.GetTransform(),
                localOccupantOrientationTransform,
                PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 0.0f, 0.0f),
                occupant.productInfo.cutOutSizeInMeters,
                widthForFrontOrBack,
                widthForTop,
                height,
                depth,
                frontOrBackLabelPositionInProductSpace,
                topLabelPositionInProductSpace,
                kitchen.countertopThicknessInMeters
            );

            // Show the top cut-out label only when we can be sure it won't
            // overlap with other text. The label has a chance to overlap other
            // text if the product is a shelf and the shelf has occupants.
            bool showTopLabel = !productAsShelf || productAsShelf.occupants.Count <= 0;
            productVisual.SetCutOutInfoTopLabelVisible(showTopLabel);

            // Calculate a point on a cuboid from a normalized
            // coordinate.
            static Vector3 PointOnCuboid(Cuboid cutOutCuboidInVolumeSpace, float x, float y, float z)
            {
                return Cuboid.NormalizedToPoint(cutOutCuboidInVolumeSpace, new Vector3(x, y, z));
            }
        }

        /// <summary>
        ///     Take a list of unique pairs of occupants (that we want to
        ///     place measurements between) and place the actual <see
        ///     cref="Measure"/> prefabs where they need to go. The returned
        ///     list is the pre-placed <see cref="Measure"/>'s with all the
        ///     data needed to filter them by <see cref="ScreenshotAngle"/> and
        ///     <see cref="CabinetSpan"/>.
        /// </summary>
        protected List<MeasurementForScreenshot> PopulateMeasuresForScreenshots(
            Kitchen kitchen,
            IReadOnlyCollection<OccupantMeasurementPair> measurementPairs)
        {
            List<MeasurementForScreenshot> measurementsInScreenshots = new();

            // Reset the pooled measures objects
            for (int i = 0; i < m_NextMeasurePrefabIndex; ++i)
            {
                m_MeasurePrefabPool[i].gameObject.SetActive(false);
            }
            m_NextMeasurePrefabIndex = 0;

            // Define the offsets to shift the cabinet measurement lines to
            // help them render outside of the cabinet.
            const float CabinetMeasurementOffset = 4.0f * Units.Inches2Meters;
            Vector3 leftward    = CabinetMeasurementOffset * Vector3.left;
            Vector3 downward    = CabinetMeasurementOffset * Vector3.down;
            Vector3 towardsBack = CabinetMeasurementOffset * Vector3.forward;
            Vector3 towardsFront = CabinetMeasurementOffset * Vector3.back;
            Vector3 upward      = CabinetMeasurementOffset * Vector3.up;

            // Add measurements for the cabinets
            foreach (CabinetSpan span in kitchen.layout.spans)
            {
                if (span.IsSpace())
                {
                    // No need to measure a cabinet span that is made up of
                    // invisible spacer cabinets.

                    continue;
                }

                // Fetch this span's potential corner wedges for use to resize
                // the front cabinet measurements later.
                span.GetCornerWedges(
                    out Cabinet leftCornerWedge,
                    out Cabinet rightCornerWedge
                );

                CuboidPadding countertopOverhangInMeters =
                    kitchen.baseCountertopOverhangInMeters;

                if (span.backing && !span.backing.hasRiserAndRiserCountertop)
                {
                    // Some backings, such as Low Bar Tops, don't have a riser
                    // or countertop of their own. They modify the size of the
                    // cabinet's countertop directly. This size change is
                    // accounted for here to ensure the measurements properly
                    // line up with the countertop's real size.

                    CuboidPadding backingOverhangDeltaInMeters =
                        span.backing.countertopOverhangInMeters;

                    countertopOverhangInMeters.left         += backingOverhangDeltaInMeters.left;
                    countertopOverhangInMeters.right        += backingOverhangDeltaInMeters.right;
                    countertopOverhangInMeters.top          += backingOverhangDeltaInMeters.top;
                    countertopOverhangInMeters.bottom       += backingOverhangDeltaInMeters.bottom;
                    countertopOverhangInMeters.towardMinZ   += backingOverhangDeltaInMeters.towardMinZ;
                    countertopOverhangInMeters.towardMaxZ   += backingOverhangDeltaInMeters.towardMaxZ;
                }

                Cuboid outerCabinetSpanCuboid =
                    span.GetOutsideCuboidInSpanSpace(includeCountertop: false);

                Transform cabinetSpanTransform =
                    span.orientationAlignedCabinet.GetTransform();

                // Define common points we will be measuring between.
                // (these are in CabinetSpan-space)
                Vector3 outerLeftBottomFront    = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (0.0f, 0.0f, 0.0f));
                Vector3 outerLeftTopFront       = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (0.0f, 1.0f, 0.0f));
                Vector3 outerRightBottomFront   = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (1.0f, 0.0f, 0.0f));
                Vector3 outerLeftTopBack        = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (0.0f, 1.0f, 1.0f));
                Vector3 outerRightTopBack       = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (1.0f, 1.0f, 1.0f));
                Vector3 outerRightTopFront      = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (1.0f, 1.0f, 0.0f));

                if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections &&
                    span.cabinets.Count > 0)
                {
                    // In Sections building method, we need to account for the
                    // depth of the columns that stick out from the adjoining
                    // perpendicular spans. We can determine whether there are
                    // columns to account for by checking if the first and last
                    // cabinets in the span are corners.

                    if (span.cabinets[0].element.cabinetType == CabinetType.Corner)
                    {
                        outerLeftBottomFront.x  -= countertopOverhangInMeters.towardMinZ;
                        outerLeftTopFront.x     -= countertopOverhangInMeters.towardMinZ;
                        outerLeftTopBack.x      -= countertopOverhangInMeters.towardMinZ;
                    }

                    if (span.cabinets[^1].element.cabinetType == CabinetType.Corner)
                    {
                        outerRightBottomFront.x += countertopOverhangInMeters.towardMinZ;
                        outerRightTopBack.x     += countertopOverhangInMeters.towardMinZ;
                        outerRightTopFront.x    += countertopOverhangInMeters.towardMinZ;
                    }
                }

                // --
                // Front screenshot measurements.
                {
                    // Measure the total width of cabinet span.
                    AddFrontMeasure(
                        span,
                        outerLeftBottomFront,
                        outerRightBottomFront,
                        downward * 2.0f,
                        extraDistanceInMetersForLabelOnly: 0.0f,
                        m_CabinetMeasureColorX,
                        MeasureDisplayFlag.All
                    );

                    // Measure the thickness of the countertop.
                    Vector3 countertopLeftTopFront = outerLeftTopFront;
                    countertopLeftTopFront.x -= countertopOverhangInMeters.left;

                    Vector3 countertopLeftBottomFront = countertopLeftTopFront;
                    countertopLeftBottomFront.y -= kitchen.countertopThicknessInMeters;

                    AddFrontMeasure(
                        span,
                        countertopLeftTopFront,
                        countertopLeftBottomFront,
                        leftward,
                        extraDistanceInMetersForLabelOnly: 0.0f,
                        m_CabinetMeasureColorY,
                        MeasureDisplayFlag.All
                    );

                    // Measure the cabinet's height minus the countertop.
                    Vector3 cabinetLeftTopFront = countertopLeftBottomFront;

                    Vector3 cabinetLeftBottomFront = outerLeftBottomFront;
                    cabinetLeftBottomFront.x -= countertopOverhangInMeters.left;

                    // The countertop currently grows "downward" (i.e. its top
                    // face is always at the same height) which is not correct.
                    // In order to display (some) correct vertical measurements,
                    // the thickness of the countertop is artificially re-added
                    float downwardGrowingCountertopFixDistanceInMeters =
                        kitchen.countertopThicknessInMeters;

                    AddFrontMeasure(
                        span,
                        cabinetLeftTopFront,
                        cabinetLeftBottomFront,
                        leftward,
                        extraDistanceInMetersForLabelOnly: downwardGrowingCountertopFixDistanceInMeters, // intentional, non-zero distance. Ideally will be 0 once countertops grow upwards.
                        m_CabinetMeasureColorY,
                        MeasureDisplayFlag.All
                    );

                    // Measure the total cabinet height (including countertop).
                    AddFrontMeasure(
                        span,
                        outerLeftTopFront,
                        outerLeftBottomFront,
                        leftward * 3.0f,
                        extraDistanceInMetersForLabelOnly: downwardGrowingCountertopFixDistanceInMeters, // intentional, non-zero distance. Ideally will be 0 once countertops grow upwards.
                        m_CabinetMeasureColorY,
                        MeasureDisplayFlag.All
                    );
                }

                // --
                // Top screen shots.
                {
                    bool isCabinetWedge = span.IsCornerWedge();

                    Vector3 countertopLeftTopBack = outerLeftTopBack;
                    Vector3 countertopLeftTopFront = outerLeftTopFront;

                    if (!isCabinetWedge)
                    {
                        countertopLeftTopBack.x -= countertopOverhangInMeters.left;
                        countertopLeftTopFront.x -= countertopOverhangInMeters.left;

                        countertopLeftTopBack.z += countertopOverhangInMeters.towardMaxZ;    // overhang back
                    }

                    // All spans (including corner wedges) overhang the front.
                    countertopLeftTopFront.z -= countertopOverhangInMeters.towardMinZ;   // overhang forward

                    Vector3 backPanel = new Vector3(0.0f, 0.0f, kitchen.backPanelDepthInMeters);
                    Vector3 backPanelMeasureOffset = 
                        (CabinetMeasurementOffset * new Vector3(1f, 0f, 0f)) +
                        (Vector3.forward * Units.Inches2Meters);

                    if (kitchen.layout.style == KitchenStyle.Straight ||
                        kitchen.layout.style == KitchenStyle.Galley)
                    {
                        // Cabinet depth.

                        // This measurement is redundant in L and U shape
                        // kitchens and is therefore omitted in those cases.
                        AddTopMeasure(
                            span,
                            countertopLeftTopBack,
                            countertopLeftTopFront,
                            leftward + upward,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            m_CabinetMeasureColorZ,
                            MeasureDisplayFlag.All
                        );

                        if(kitchen.backPanelDepthInMeters > 0.0f)
                        {
                            AddTopMeasure(
                                span,
                                countertopLeftTopBack - backPanelMeasureOffset,
                                countertopLeftTopBack - backPanel - backPanelMeasureOffset,
                                leftward + upward,
                                extraDistanceInMetersForLabelOnly: 0.0f,
                                m_CabinetMeasureColorZ,
                                MeasureDisplayFlag.All
                            );
                        }
                    }

                    // Measure out the countertop's width
                    Vector3 countertopRightTopBack = outerRightTopBack;
                    if (!isCabinetWedge)
                    {
                        countertopRightTopBack.x += countertopOverhangInMeters.right;

                        countertopRightTopBack.z += countertopOverhangInMeters.towardMaxZ; // overhang back
                    }

                    // Measure the total width of the span.
                    Vector3 countertopWidthMeasureStart = countertopLeftTopBack;
                    Vector3 countertopWidthMeasureEnd = countertopRightTopBack;
                    Vector3 countertopWidthLabelOffset = (towardsBack * 2.0f) + upward;

                    if (span.IsCornerWedge())
                    {
                        // Make the measurement relative to the front of the
                        // cabinet for corner wedge spans.
                        countertopWidthMeasureStart = countertopLeftTopFront;
                        countertopWidthMeasureEnd.z = countertopWidthMeasureStart.z;

                        countertopWidthLabelOffset = (towardsFront * 2.0f) + upward;
                    }

                    AddTopMeasure(
                        span,
                        countertopWidthMeasureStart,
                        countertopWidthMeasureEnd,
                        countertopWidthLabelOffset,
                        extraDistanceInMetersForLabelOnly: 0.0f,
                        m_CabinetMeasureColorX,
                        MeasureDisplayFlag.All
                    );

                    // --
                    // Measure the width of the span's backing. This is only
                    // needed if there are backings on the neighbouring spans:
                    // I.e., if there is no backings on neighbouring spans, the
                    // current span and its backing will have the same width.
                    // This also does not apply to corner wedge cabinets.

                    if (!isCabinetWedge)
                    {
                        bool doesLeftSpanHaveBacking =
                            span.index > 0 &&
                            kitchen.layout.spans[span.index - 1].backing;

                        bool doesRightSpanHaveBacking =
                            span.index < kitchen.layout.spans.Count - 1 &&
                            kitchen.layout.spans[span.index + 1].backing;

                        if (span.backing && (doesLeftSpanHaveBacking || doesRightSpanHaveBacking))
                        {
                            // TODO: Calculate the left and right overhangs properly
                            //       with respect to low bar tops. -Ryan

                            // The countertop's cuboid represents the internal part
                            // of the countertop only. This means we must manually
                            // add the overhang.
                            Cuboid backingCountertopCuboidInSpanSpace =
                                span.backing.GetCountertopCuboidInSpanSpace(span.GetOutsideCuboidInSpanSpace());

                            backingCountertopCuboidInSpanSpace.xMin -= kitchen.baseCountertopOverhangInMeters.left;
                            backingCountertopCuboidInSpanSpace.xMax += kitchen.baseCountertopOverhangInMeters.right;
                            backingCountertopCuboidInSpanSpace.zMin -= kitchen.baseCountertopOverhangInMeters.towardMinZ;
                            backingCountertopCuboidInSpanSpace.zMax += kitchen.baseCountertopOverhangInMeters.towardMaxZ;

                            AddTopMeasure(
                                span,
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new (0.0f, 1.0f, 1.0f)),
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new (1.0f, 1.0f, 1.0f)),
                                upward,
                                extraDistanceInMetersForLabelOnly: 0.0f,
                                m_CabinetMeasureColorX,
                                MeasureDisplayFlag.All
                            );
                        }

                        // Measure the depth of the backing.
                        if (span.backing && span.backing.hasRiserAndRiserCountertop)
                        {
                            Cuboid backingCountertopCuboidInSpanSpace =
                                span.backing.GetCountertopCuboidInSpanSpace(span.GetOutsideCuboidInSpanSpace());

                            AddTopMeasure(
                                span,
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new(0.5f, 1.0f, 0.0f)) + kitchen.baseCountertopOverhangInMeters.towardMaxZ * Vector3.back,
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new(0.5f, 1.0f, 1.0f)) + kitchen.baseCountertopOverhangInMeters.towardMinZ * Vector3.forward,
                                upward,
                                extraDistanceInMetersForLabelOnly: 0.0f,
                                m_CabinetMeasureColorZ,
                                MeasureDisplayFlag.All
                            );
                        }
                    }
                }

                int cabinetCountInSpan = span.cabinets.Count;
                for (int i = 0; i < span.cabinets.Count; i++)
                {
                    Cabinet cabinet = span.cabinets[i].element;

                    kitchen.layout.GetCabinetNeighbours(
                        cabinet,
                        out Cabinet leftCabinet,
                        out Cabinet rightCabinet
                    );

                    Cuboid cabinetCuboid = cabinet.resizerCuboid;

                    Vector3 cabinetLeftBottomFront  = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 0.0f, 0.0f));
                    Vector3 cabinetRightBottomFront = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 0.0f, 0.0f));

                    Vector3 cabinetLeftTopBack      = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 1.0f, 1.0f));
                    Vector3 cabinetRightTopBack     = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 1.0f, 1.0f));

                    if (cabinet.cabinetType == CabinetType.Corner &&
                        !cabinet.IsYawSimilar(span.orientationAlignedCabinet))
                    {
                        cabinetLeftBottomFront  = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 0.0f, 1.0f));
                        cabinetRightBottomFront = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 0.0f, 0.0f));

                        cabinetLeftTopBack      = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 1.0f, 1.0f));
                        cabinetRightTopBack     = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 1.0f, 0.0f));
                    }

                    if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections ||
                        kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
                    {
                        // Make the measurements relative to the span's
                        // main transform. This is necessary because
                        // `AddFrontMeasure` and `AddTopMeasure` assume the
                        // measurement positions are relative to the
                        // orientation aligned cabinet. If we don't transform
                        // the points to the span's main cabinet, the
                        // measurements will not properly offset from the
                        // cabinet's columns/sections.

                        cabinetLeftBottomFront =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetLeftBottomFront,
                                span.orientationAlignedCabinet
                            );

                        cabinetLeftTopBack =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetLeftTopBack,
                                span.orientationAlignedCabinet
                            );

                        cabinetRightBottomFront =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetRightBottomFront,
                                span.orientationAlignedCabinet
                            );

                        cabinetRightTopBack =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetRightTopBack,
                                span.orientationAlignedCabinet
                            );
                    }

                    if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections)
                    {
                        // In Stoll, the cabinet dimensions should be measured
                        // from between the mullions.

                        float halfColumnWidthInMeters =
                            Const.CabinetColumnWidthInMeters * 0.5f;

                        if (!leftCabinet || !leftCabinet.Is(CabinetType.HighLeftToLowRight))
                        {
                            cabinetLeftBottomFront.x    += halfColumnWidthInMeters;
                            cabinetLeftTopBack.x        += halfColumnWidthInMeters;
                        }

                        if (!rightCabinet || !rightCabinet.Is(CabinetType.LowLeftToHighRight))
                        {
                            cabinetRightBottomFront.x   -= halfColumnWidthInMeters;
                            cabinetRightTopBack.x       -= halfColumnWidthInMeters;
                        }
                    }

                    if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog &&
                        !cabinet.cabinetType.IsEnd() &&
                        i < cabinetCountInSpan - 1 &&
                        !span.cabinets[i + 1].element.cabinetType.IsEnd())
                    {
                        Vector3 cabinetRightTopFront = cabinetRightTopBack;
                        cabinetRightTopFront.z = cabinetRightBottomFront.z;

                        // Add separating lines between cabinets in Catalog mode.
                        AddFrontMeasure(
                            span,
                            cabinetRightBottomFront,
                            cabinetRightTopFront,
                            Vector3.zero,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            Color.black,
                            MeasureDisplayFlag.Line
                        );

                        // Add separating lines between cabinets in Catalog mode.
                        AddTopMeasure(
                            span,
                            cabinetRightTopBack,
                            cabinetRightTopFront,
                            Vector3.zero,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            Color.black,
                            MeasureDisplayFlag.Line
                        );
                    }

                    if (cabinet.cabinetType == CabinetType.Corner)
                    {
                        // Skip corners because they are not something the user
                        // can resize. If we ever need to draw their
                        // measurements, we need to transform the corner's
                        // coordinates correctly: the leading corner in a
                        // cabinet span uses a different rotation from the rest.
                        continue;
                    }

                    if (cabinet.cabinetType == CabinetType.CornerWedge)
                    {
                        // Skip corner wedges because they are a span made up
                        // of only one cabinet. Therefore, their measurements
                        // are already visible from the earlier span metrics.
                        continue;
                    }


                    if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections)
                    {
                        if (cabinet.cabinetType.IsEnd(includeTransitions: true) ||
                            cabinet.cabinetType == CabinetType.StraightOnLeftOfCorner ||
                            cabinet.cabinetType == CabinetType.StraightOnRightOfCorner)
                        {
                            // In Sections building method, we want to skip
                            // measuring these cabinets. These cabinets normally
                            // don't exist or have no width, but that is not the
                            // case in Sections mode.
                            continue;
                        }
                    }

                    Transform cabinetTransform = cabinet.GetTransform();

                    if (cabinetCuboid.width > 0.0f)
                    {
                        Vector3 topDownMeasureDirection = towardsBack;

                        Vector3 topDownWidthMeasureStart = cabinetLeftTopBack;
                        Vector3 topDownWidthMeasureEnd = cabinetRightTopBack;

                        if (leftCabinet != null && leftCabinet.Is(CabinetType.Corner) && leftCornerWedge != null)
                        {
                            Cabinet wedgedCornerCabinet = span.cabinets[0].element;
                            MelcherLog.Assert(wedgedCornerCabinet.Is(CabinetType.Corner));

                            float wedgeOrthogonalLength =
                                ProFitUtility.CalculateCabinetCornerWedgeOrthogonalLength(leftCornerWedge);

                            cabinetLeftBottomFront.x += wedgeOrthogonalLength;
                            topDownWidthMeasureStart.x += wedgeOrthogonalLength;
                            topDownWidthMeasureStart.z = cabinetLeftBottomFront.z;
                            topDownWidthMeasureEnd.z = topDownWidthMeasureStart.z;

                            topDownMeasureDirection = towardsFront;
                        }

                        if (rightCabinet != null && rightCabinet.Is(CabinetType.Corner) && rightCornerWedge != null)
                        {
                            Cabinet wedgedCornerCabinet = span.cabinets[^1].element;
                            MelcherLog.Assert(wedgedCornerCabinet.Is(CabinetType.Corner));

                            float wedgeOrthogonalLength =
                                ProFitUtility.CalculateCabinetCornerWedgeOrthogonalLength(rightCornerWedge);

                            cabinetRightBottomFront.x -= wedgeOrthogonalLength;
                            topDownWidthMeasureEnd.x -= wedgeOrthogonalLength;
                            topDownWidthMeasureEnd.z = cabinetRightBottomFront.z;
                            topDownWidthMeasureStart.z = topDownWidthMeasureEnd.z;

                            topDownMeasureDirection = towardsFront;
                        }

                        // Measure the cabinet's width viewed from the front.
                        AddFrontMeasure(
                            span,
                            cabinetLeftBottomFront,
                            cabinetRightBottomFront,
                            downward,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            m_CabinetMeasureColorX,
                            MeasureDisplayFlag.All
                        );

                        if (topDownMeasureDirection != towardsFront)
                        {
                            // Measure the cabinet's width viewed from the top.
                            topDownWidthMeasureStart.z += countertopOverhangInMeters.towardMaxZ;
                            topDownWidthMeasureEnd.z += countertopOverhangInMeters.towardMaxZ;
                        }

                        AddTopMeasure(
                            span,
                            topDownWidthMeasureStart,
                            topDownWidthMeasureEnd,
                            topDownMeasureDirection + upward,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            m_CabinetMeasureColorX,
                            MeasureDisplayFlag.All
                        );
                    }
                }

                foreach (OccupantMeasurementPair pair in measurementPairs)
                {
                    AddInterOccupantMeasures(pair, span);
                }

                /// <param name="point1">
                ///     A position that is relative to the span's orientation-
                ///     aligned cabinet.
                /// </param>
                /// <param name="point2">
                ///     A position that is relative to the span's orientation-
                ///     aligned cabinet.
                /// </param>
                void AddFrontMeasure(
                    CabinetSpan span,
                    Vector3     point1,
                    Vector3     point2,
                    Vector3     pointOffset,
                    float       extraDistanceInMetersForLabelOnly,
                    Color       color,
                    MeasureDisplayFlag displayFlags)
                {
                    AddMeasureBetweenCabinetPoints(
                        ScreenshotAngle.Front,
                        span.orientationAlignedCabinet.GetTransform(),
                        point1 + pointOffset,
                        point2 + pointOffset,
                        extraDistanceInMetersForLabelOnly,
                        span,
                        color,
                        displayFlags,
                        measurementsInScreenshots
                    );
                }

                void AddTopMeasure(
                    CabinetSpan span,
                    Vector3     point1,
                    Vector3     point2,
                    Vector3     pointOffset,
                    float       extraDistanceInMetersForLabelOnly,
                    Color       color,
                    MeasureDisplayFlag displayFlags)
                {
                    AddMeasureBetweenCabinetPoints(
                        ScreenshotAngle.Top,
                        span.orientationAlignedCabinet.GetTransform(),
                        point1 + pointOffset,
                        point2 + pointOffset,
                        extraDistanceInMetersForLabelOnly,
                        span,
                        color,
                        displayFlags,
                        measurementsInScreenshots
                    );
                }
            }

            return measurementsInScreenshots;

            /// <summary>
            ///     Given a measurement pair, generate the <see
            ///     cref="MeasurementForScreenshot"/>s for each camera angle the
            ///     measurement should be visible.
            /// </summary>
            void AddInterOccupantMeasures(
                OccupantMeasurementPair measurementPair,
                CabinetSpan             span)
            {
                if (kitchen.AreProductsForcedToCenterOfCabinets((Cabinet)measurementPair.occupantA.cabinet))
                {
                    // Do not show measurements between products when in
                    // Sections building method (aka. for Stoll).
                    return;
                }

                CabinetSpan cabinetSpan =
                    kitchen.layout.FindCabinetSpan((Cabinet)measurementPair.occupantA.cabinet);

                if (cabinetSpan != span)
                {
                    // This measurement is for a different span. It will be
                    // processed later when the other cabinet span is
                    // requested.
                    return;
                }

                Vector3 directionVector = measurementPair.directionAtoB.ToVector3();

                Cabinet cabinet = (Cabinet)measurementPair.occupantA.cabinet;

                Cuboid cabinetCuboid = new Cuboid(cabinet.sizeInMeters);

                // Get the first occupant's cut-out relative to the cabinet it
                // belongs to. The occupant may be nested in a shelf so this
                // transformation is necessary.
                Cuboid occupantCuboidA = measurementPair.occupantA.GetCutOutCuboidInVolumeSpace();
                if (!ReferenceEquals(measurementPair.occupantA.occupiedVolume, measurementPair.occupantA.cabinet))
                {
                    occupantCuboidA.position +=
                        measurementPair.occupantA.occupiedVolume.occupiableCuboid.position;
                }

                OrientedBounds a =
                    new(
                        new Bounds(occupantCuboidA.center, occupantCuboidA.size),
                        measurementPair.occupantA.cabinet.GetTransform().worldToLocalMatrix
                    );
                a.Draw(Color.red, 2.0f);

                MeasurementAxis axis = measurementPair.directionAtoB.ToAxis();

                if (measurementPair.occupantB)
                {
                    // This is a measurement between two occupants

                    Cuboid occupantCuboidB =
                        measurementPair.occupantB.GetCutOutCuboidInVolumeSpace();

                    if (!ReferenceEquals(measurementPair.occupantB.occupiedVolume, measurementPair.occupantB.cabinet))
                    {
                        occupantCuboidB.position +=
                            measurementPair.occupantB.occupiedVolume.occupiableCuboid.position;
                    }

                    OrientedBounds b =
                        new(
                            new Bounds(occupantCuboidB.center, occupantCuboidB.size),
                            measurementPair.occupantB.cabinet.GetTransform().worldToLocalMatrix
                        );
                    b.Draw(Color.green, 2.0f);

                    //Debug.DrawLine(a.min, b.min, Color.magenta, 2.0f);
                    //Debug.DrawLine(Vector3.zero + Vector3.one, a.min, Color.magenta, 2.0f);

                    AddMeasureBetweenOccupants(
                        measurementPair.screenshotAngle,
                        occupantCuboidA,
                        occupantCuboidB,
                        measurementPair.directionAtoB,
                        measurementPair.extraDistanceInMetersForLabelOnly,
                        span
                    );
                }
                else
                {
                    // This is a measurement between one occupant and the
                    // outside of the cabinet

                    Color lineColor = AxisToColor(axis);
                    Vector3 start = default;
                    Vector3 end = default;

                    switch (measurementPair.screenshotAngle)
                    {
                        default:
                            MelcherLog.LogError($"Unhandled {nameof(ScreenshotAngle)}: {measurementPair.screenshotAngle}");
                            return;

                        case ScreenshotAngle.Front:
                            end = GetPointOnCuboidFaceInDirection(occupantCuboidA, measurementPair.directionAtoB, anchorToBackFace: false);
                            start = end + directionVector * measurementPair.distanceInMeters;
                            break;

                        case ScreenshotAngle.Back:
                            end = GetPointOnCuboidFaceInDirection(occupantCuboidA, measurementPair.directionAtoB, anchorToBackFace: true);
                            start = end + directionVector * measurementPair.distanceInMeters;
                            break;

                        case ScreenshotAngle.Top:
                            start = GetPointOnCuboidFaceInDirection(occupantCuboidA, measurementPair.directionAtoB, anchorToBackFace: null);

                            // Align the points to the cabinet's top
                            start.y = cabinetCuboid.yMax;

                            if (axis == MeasurementAxis.X)
                            {
                                // Center the line within the occupant's depth
                                start.z = occupantCuboidA.center.z;
                            }

                            end = start + directionVector * measurementPair.distanceInMeters;

                            if (measurementPair.occupantA.occupiedVolume.parentVolume == null)
                            {
                                // This product directly occupies a cabinet
                                // (i.e., it is not in a shelf) and therefore
                                // we need to account for the overhang of the
                                // countertop.

                                if (axis == MeasurementAxis.X)
                                {
                                    // Adjust the width by the countertop's overhang
                                    kitchen.layout.GetCabinetNeighbours(cabinet, out Cabinet leftCabinet, out Cabinet rightCabinet);
                                    if (measurementPair.directionAtoB == CabinetLocalDirection.Left)
                                    {
                                        if (leftCabinet != null && leftCabinet.cabinetType != CabinetType.EndLeft)
                                        {
                                            // We have a left neighbour. Contract to accommodate
                                            // its overhang towards us
                                            end -= directionVector * kitchen.baseCountertopOverhangInMeters.right;
                                        }
                                        else
                                        {
                                            // No left neighbour. Extend leftwards to include the overhang
                                            end += directionVector * kitchen.baseCountertopOverhangInMeters.left;
                                        }
                                    }
                                    else if (measurementPair.directionAtoB == CabinetLocalDirection.Right)
                                    {
                                        if (rightCabinet != null && rightCabinet.cabinetType != CabinetType.EndRight)
                                        {
                                            // We have a right neighbour. Contract to accommodate
                                            // its overhang towards us
                                            end -= directionVector * kitchen.baseCountertopOverhangInMeters.left;
                                        }
                                        else
                                        {
                                            // No right neighbour. Extend rightwards to include the overhang
                                            end += directionVector * kitchen.baseCountertopOverhangInMeters.right;
                                        }
                                    }
                                }
                                else
                                {
                                    CuboidPadding countertopOverhang =
                                        cabinet.GetCountertopOverhangInMeters();

                                    // Z-axis. Extend the end point to the overhang
                                    if (measurementPair.directionAtoB == CabinetLocalDirection.Front)
                                    {
                                        end += directionVector * countertopOverhang.towardMinZ;
                                    }
                                    else
                                    {
                                        end += directionVector * countertopOverhang.towardMaxZ;
                                    }
                                }
                            }

                            break;
                    }

                    // Add a measurement that is visible from the front
                    AddMeasureBetweenCabinetPoints(
                        measurementPair.screenshotAngle,
                        cabinet.GetTransform(),
                        start,
                        end,
                        measurementPair.extraDistanceInMetersForLabelOnly,
                        span,
                        lineColor,
                        MeasureDisplayFlag.All,
                        measurementsInScreenshots
                    );
                }

                /// <summary>
                ///     Find the points to draw a measure line between for two
                ///     occupant's cut-outs, then generate an <see
                ///     cref="MeasurementForScreenshot"/> with those points and
                ///     add it to the enclosing function's inter-occupant
                ///     measure list.
                /// </summary>
                /// <param name="screenshotAngle">
                ///     The screenshot type where this measure will be shown.
                /// </param>
                /// <param name="mainCutOutCuboid">
                ///     The cut-out cuboid of the first occupant.
                /// </param>
                /// <param name="otherCutOutCuboid">
                ///     The cut-out cuboid of the second occupant.
                /// </param>
                /// <param name="directionToOtherCuboid">
                ///     The direction from <pararef name="mainCutOutCuboid"/> to
                ///     <paramref name="otherCutOutCuboid"/>.
                /// </param>
                /// <param name="cabinet">
                ///     Must not be <see langword="null"/>. It binds the measure
                ///     to a specific cabinet span so it can be selectively
                ///     rendered when needed.
                /// </param>
                void AddMeasureBetweenOccupants(
                    ScreenshotAngle         screenshotAngle,
                    Cuboid                  mainCutOutCuboid,
                    Cuboid                  otherCutOutCuboid,
                    CabinetLocalDirection   directionToOtherCuboid,
                    float                   extraDistanceInMetersForLabelOnly,
                    CabinetSpan             cabinetSpan)
                {
                    // Determine whether the measure's should be placed along
                    // the back of the product (the screenshot is from the back)
                    // or placed along the product's front (the default).
                    bool anchorToBackFace = screenshotAngle == ScreenshotAngle.Back;

                    Vector3 start = GetPointOnCuboidFaceInDirection(mainCutOutCuboid, directionToOtherCuboid, anchorToBackFace);
                    Vector3 end = GetPointOnCuboidFaceInDirection(otherCutOutCuboid, directionToOtherCuboid.Reverse(), anchorToBackFace);

                    switch (axis)
                    {
                        default:
                            throw new System.ArgumentOutOfRangeException($"Unhandled {nameof(MeasurementAxis)}: {axis}");

                        case MeasurementAxis.X:
                            if (screenshotAngle == ScreenshotAngle.Top)
                            {
                                CenterWithinCuboidOverlap(MeasurementAxis.Z);
                                float cabinetHeight = cabinetSpan.orientationAlignedCabinet.sizeInMeters.y;
                                start.y = cabinetHeight;
                                end.y = cabinetHeight;
                            }
                            else
                            {
                                CenterWithinCuboidOverlap(MeasurementAxis.Y);
                            }
                            break;

                        case MeasurementAxis.Y:
                        case MeasurementAxis.Z:
                            CenterWithinCuboidOverlap(MeasurementAxis.X);
                            break;
                    }

                    Color lineColor = AxisToColor(axis);

                    AddMeasureBetweenCabinetPoints(
                        screenshotAngle,
                        cabinet.GetTransform(),
                        start,
                        end,
                        extraDistanceInMetersForLabelOnly,
                        cabinetSpan,
                        lineColor,
                        MeasureDisplayFlag.All,
                        measurementsInScreenshots
                    );

                    void CenterWithinCuboidOverlap(MeasurementAxis axisToAdjust)
                    {
                        int channel = (int)axisToAdjust;

                        float largestMin = Mathf.Max(mainCutOutCuboid.min[channel], otherCutOutCuboid.min[channel]);
                        float smallestMax = Mathf.Min(mainCutOutCuboid.max[channel], otherCutOutCuboid.max[channel]);

                        // Center the measure along the axis
                        float mid = Mathf.Lerp(largestMin, smallestMax, 0.5f);
                        start[channel] = mid;
                        end[channel] = mid;
                    }
                }

                /// <summary>
                ///     Given a cuboid and a cabinet direction, find a point on
                ///     the edge of that cuboid that is in the direction specified.
                /// </summary>
                /// <param name="cuboid"></param>
                /// <param name="direction"></param>
                /// <param name="anchorToBackFace">
                ///     Whether the measure should be placed on the side of the
                ///     product closest to the back.
                ///     This value is ignored if <paramref name="direction"/> is
                ///     along the <see cref="MeasurementAxis.Z"/> axis.
                /// </param>
                /// <returns></returns>
                Vector3 GetPointOnCuboidFaceInDirection(Cuboid cuboid, CabinetLocalDirection direction, bool? anchorToBackFace)
                {
                    Vector3 normalizedPoint = default;

                    float normalizedZ = anchorToBackFace.GetValueOrDefault() ? 1.0f : 0.0f;

                    switch (direction)
                    {
                        default:
                            throw new System.ArgumentOutOfRangeException($"Unhandled {nameof(CabinetLocalDirection)}: {direction}");

                        case CabinetLocalDirection.Left:
                            // Left edge, vertically centered
                            normalizedPoint = new Vector3(0.0f, 0.5f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Right:
                            // Right edge, vertically centered
                            normalizedPoint = new Vector3(1.0f, 0.5f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Up:
                            // Top face, horizontally centered
                            normalizedPoint = new Vector3(0.5f, 1.0f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Down:
                            // Bottom face, horizontally centered
                            normalizedPoint = new Vector3(0.5f, 0.0f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Front:
                            // Front face, horizontally centered, and at top.
                            // This intentionally does not use normalizedZ
                            normalizedPoint = new Vector3(0.5f, 1.0f, 0.0f);
                            break;

                        case CabinetLocalDirection.Back:
                            // Back face, horizontally centered, and at top.
                            // This intentionally does not use normalizedZ
                            normalizedPoint = new Vector3(0.5f, 1.0f, 1.0f);
                            break;
                    }

                    return Cuboid.NormalizedToPoint(cuboid, normalizedPoint);
                }
            }
        }

        /// <summary>
        ///     This is the base function to add any between-occupant
        ///     measurements. It works with points specified in the
        ///     cabinet's local space.
        /// </summary>
        /// <param name="screenshotAngle">
        ///     The screenshot type where this measure will be shown.
        /// </param>
        /// <param name="fromPointInInputSpace">
        ///     The position to measure from in cabinet-space.
        /// </param>
        /// <param name="toPointInInputSpace">
        ///     The position to measure to in cabinet-space.
        /// </param>
        /// <param name="cabinet">
        ///     Must not be <see langword="null"/>. It binds the measure to
        ///     a specific cabinet span so it can be selectively rendered when
        ///     needed.
        /// </param>
        protected void AddMeasureBetweenCabinetPoints(
            ScreenshotAngle screenshotAngle,
            Transform       pointInputSpace,
            Vector3         fromPointInInputSpace,
            Vector3         toPointInInputSpace,
            float           extraDistanceInMetersForLabelOnly,
            CabinetSpan     cabinetSpan,
            Color           lineColor,
            MeasureDisplayFlag    displayFlags,
            List<MeasurementForScreenshot> measurementsForScreenshots)
        {
            if (m_NextMeasurePrefabIndex >= m_MeasurePrefabPool.Count)
            {
                // Ran out of pooled objects
                MelcherLog.LogWarning($"{nameof(KitchenScreenshotManager)} ran out of pooled {nameof(Measure)} for between-product measurements");
                return;
            }

            // Convert the screenshot angle to an offset that adjusts
            // the measure to be closer to the camera for the angle
            Vector3 cabinetLocalDirectionToCamera = default;
            Vector3 cameraWorldUp = Vector3.up;

            switch (screenshotAngle)
            {
                default:
                    throw new System.ArgumentOutOfRangeException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");

                case ScreenshotAngle.Top:
                    cabinetLocalDirectionToCamera = Vector3.up;
                    cameraWorldUp = Vector3.forward;
                    break;

                case ScreenshotAngle.Front:
                    // Intentional Vector3.back for ScreenshotAngle.Front
                    cabinetLocalDirectionToCamera = Vector3.back;
                    break;

                case ScreenshotAngle.Back:
                    // Intentional Vector3.forward for ScreenshotAngle.Back
                    cabinetLocalDirectionToCamera = Vector3.forward;
                    break;
            }

            // How far to shift the measures away from the cabinet (towards
            // the camera). This is meant to prevent the measure's label or
            // line clipping into the cabinet.
            const float MeasureOffsetFromCabinetInMeters = 2.0f * Units.Inches2Meters;

            Vector3 offset = cabinetLocalDirectionToCamera * MeasureOffsetFromCabinetInMeters;
            fromPointInInputSpace += offset;
            toPointInInputSpace += offset;

            Vector3 worldFrom   = pointInputSpace.TransformPoint(fromPointInInputSpace);
            Vector3 worldTo     = pointInputSpace.TransformPoint(toPointInInputSpace);
            Vector3 worldDirectionToCamera = pointInputSpace.TransformDirection(cabinetLocalDirectionToCamera);

            Measure measure = m_MeasurePrefabPool[m_NextMeasurePrefabIndex];
            measure.units = ProFitGlobals.userProfile.activeUnits;
            measure.transform.LookAt(measure.transform.position - worldDirectionToCamera, cameraWorldUp);
            measure.color = lineColor;
            measure.SetDisplayFlags(displayFlags);
            ++m_NextMeasurePrefabIndex;
            measure.SetExtraDistanceForLabelOnly(extraDistanceInMetersForLabelOnly);
            measure.MeasureFromTo(worldFrom, worldTo);

            measurementsForScreenshots.Add(
                new MeasurementForScreenshot()
                {
                    measure = measure,
                    screenshotAngle = screenshotAngle,
                    cabinetSpan = cabinetSpan
                }
            );
        }

        protected Color AxisToColor(MeasurementAxis axis)
        {
            Color color = Color.black;

            switch (axis)
            {
                case MeasurementAxis.X:
                    color = m_CabinetMeasureColorX;
                    break;

                case MeasurementAxis.Y:
                    color = m_CabinetMeasureColorY;
                    break;

                case MeasurementAxis.Z:
                    color = m_CabinetMeasureColorZ;
                    break;
            }

            return color;
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

        protected int m_NextMeasurePrefabIndex;

        // ====================================================================
        // Nested types
        // ====================================================================

        /// <summary>
        /// Contains all of the screenshots captured during <see
        /// cref="TakeScreenshots(Kitchen)"/>. The creation of one of these
        /// objects is the main purpose of this class.
        /// </summary>
        [System.Serializable]    // DEBUG: Only serializable so I can inspect the pictures in the editor. Will remove at some point. -Ryan
        public class ScreenshotGallery
        {
            /// <summary>
            /// The picture to appear at the start of the PDF.
            /// </summary>
            public byte[] cover;

            /// <summary>
            /// A top-down render of the kitchen with all the products' 3D
            /// models.
            /// </summary>
            public byte[] topDown;

            /// <summary>
            /// A top-down render of the kitchen without the products' 3D
            /// models, but containing the measurements of the products and the
            /// distances between their neighbours.
            /// </summary>
            public byte[] topDownPlan;

            /// <summary>
            /// A front render of each cabinet with the product's 3D models
            /// visible.
            /// </summary>
            public List<byte[]> front = new List<byte[]>();

            /// <summary>
            /// A front render of each cabinet with the product's 3D models
            /// hidden but containing the measurements of the products and the
            /// distances between their neighbours.
            /// </summary>
            public List<byte[]> frontPlan = new List<byte[]>();

        }   // class ScreenshotGallery

        /// <summary>
        /// Records the nearest neighbours for one occupant in the 4 orthogonal
        /// directions that are visible in a specific orthogonal camera angle.
        /// I.e., a top-down picture will only consider the nearest neighbours
        /// in the XZ plane and it will ignore any occupant whose cut-out is
        /// not visible from the top-down camera angle.
        /// <see cref="CabinetLocalDirection"/>).
        /// </summary>
        protected class OccupantNearestNeighbours
        {
            /// <summary>
            /// Get the recorded occupant and distance for the given direction
            /// in the top-down camera angle.
            /// </summary>
            public OccupantAndDistance GetNearestInDirection(ScreenshotAngle screenshotAngle, CabinetLocalDirection direction)
            {
                switch (screenshotAngle)
                {
                    default:
                        throw new System.NotImplementedException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");

                    case ScreenshotAngle.Top:
                        Debug.Assert(direction.ToAxis() != MeasurementAxis.Y, "For top screenshots, the direction should be along the XZ plane");
                        return topNearestNeighbours[(int)direction];

                    case ScreenshotAngle.Front:
                        Debug.Assert(direction.ToAxis() != MeasurementAxis.Z, "For front screenshots, be direction should be along the XY plane");
                        return frontNearestNeighbours[(int)direction];
                }
            }

            /// <summary>
            /// Set the recorded occupant and distance for the given direction
            /// in the top-down camera angle.
            /// </summary>
            public void SetNearestInDirection(ScreenshotAngle screenshotAngle, CabinetLocalDirection direction, OccupantAndDistance occupantAndDistance)
            {
                switch (screenshotAngle)
                {
                    default:
                        throw new System.NotImplementedException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");

                    case ScreenshotAngle.Top:
                        Debug.Assert(direction.ToAxis() != MeasurementAxis.Y, "For top screenshots, the direction should be along the XZ plane");
                        topNearestNeighbours[(int)direction] = occupantAndDistance;
                        break;

                    case ScreenshotAngle.Front:
                        Debug.Assert(direction.ToAxis() != MeasurementAxis.Z, "For front screenshots, be direction should be along the XY plane");
                        frontNearestNeighbours[(int)direction] = occupantAndDistance;
                        break;
                }
            }

            /// <summary>
            /// The main occupant who the other <see cref="OccupantAndDistance"/>'s
            /// are relative to.
            /// </summary>
            public Occupant occupant;

            // It's simplest to use oversized arrays here. We only need 4
            // entries but with 6 we can use the enum value to index the array
            public OccupantAndDistance[] frontNearestNeighbours = new OccupantAndDistance[EnumInfo<CabinetLocalDirection>.count];
            public OccupantAndDistance[] topNearestNeighbours = new OccupantAndDistance[EnumInfo<CabinetLocalDirection>.count];

        }   // class OccupantNearestNeighbours

        /// <summary>
        /// Represents the distance from one occupant (not recorded) to a
        /// particular other occupant.
        /// </summary>
        protected struct OccupantAndDistance
        {
            public Occupant occupant;
            public float distanceInMeters;

        }   // struct OccupantAndDistance

        /// <summary>
        /// Represents the distance between one occupant and it's nearest
        /// neighbour in a specific <see cref="CabinetLocalDirection"/>. The
        /// second occupant can be <see langword="null"/> to indicate the
        /// measurement is a between one occupant and the edge of the cabinet.
        /// </summary>
        protected struct OccupantMeasurementPair
        {
            public OccupantMeasurementPair(Occupant a, Occupant b, float distanceInMeters, float extraDistanceInMetersForLabelOnly, CabinetLocalDirection directionAToB, ScreenshotAngle screenshotAngle)
            {
                occupantA = a;
                occupantB = b;
                this.distanceInMeters = distanceInMeters;
                this.extraDistanceInMetersForLabelOnly = extraDistanceInMetersForLabelOnly;
                this.directionAtoB = directionAToB;
                this.screenshotAngle = screenshotAngle;
            }

            public Occupant occupantA;
            public Occupant occupantB;
            public float distanceInMeters;
            public float extraDistanceInMetersForLabelOnly;
            public CabinetLocalDirection directionAtoB;
            public ScreenshotAngle screenshotAngle;
        }

        /// <summary>
        /// Represents a pre-placed <see cref="Measure"/> between two points.
        /// It may be indicating the distance between two occupants or between
        /// one occupant and the edge of the cabinet.
        /// </summary>
        protected struct MeasurementForScreenshot
        {
            /// <summary>
            /// The measure object rendering a line and number between the two
            /// occupants.
            /// </summary>
            public Measure measure;

            /// <summary>
            /// The screenshot angle(s) where this measure will be displayed
            /// (for plan mode)
            /// </summary>
            public ScreenshotAngle screenshotAngle;

            /// <summary>
            /// The cabinet span this measure will render with.
            /// </summary>
            public CabinetSpan cabinetSpan;

        }   // struct InterOccupantMeasure

    }   // class KitchenScreenshotManager

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-02
/// ===========================================================================
