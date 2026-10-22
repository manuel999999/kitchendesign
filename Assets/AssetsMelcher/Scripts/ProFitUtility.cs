namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using MEC;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Rendering.Universal;

    using Conditional = System.Diagnostics.ConditionalAttribute;

    using JsonDict = System.Collections.Generic.IDictionary<string, object>;

    public static class ProFitUtility
    {



        /// <summary>
        /// Open the product URL. This only works in the editor and development
        /// builds.
        /// <br /><br />
        ///
        /// Conditional compilation directives are used to ensure the URL is
        /// not leaked.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void OpenProductUrl(int productServerId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            const string Endpoint = "/admin/components/{product_id}/edit";
            Application.OpenURL(
                ProFitWeb.Host + Endpoint.Replace("{product_id}", productServerId.ToString())
            );
#endif  // if UNITY_EDITOR || DEVELOPMENT_BUILD
        }

        /// <summary>
        ///     Open a web browser to the app's subscription landing page as
        ///     defined in the active ProFitAppOptions.
        /// </summary>
        public static void OpenSubscriptionLandingPage()
        {
            string subscriptionUrl =
                ProFitGlobals.userProfile.userType switch
                {
                    UserType.Trial => ProFitGlobals.appOptions.subscriptionUrlForTrialAccount,
                    _ => ProFitGlobals.appOptions.subscriptionUrlForDealerAccount
                };

            Application.OpenURL(subscriptionUrl);
        }

        public static void ShowTermsAndConditions()
        {
            const string Endpoint = "/webgl/privacy-policy.html";
            Application.OpenURL(ProFitWeb.Host + Endpoint);
        }

        public static ProductVisual FindProductVisual(IReadOnlyList<ProductVisual> productVisuals, string visualId)
        {
            if (string.IsNullOrEmpty(visualId))
            {
                return null;
            }

            return MelcherCommon.Find(
                productVisuals,
                currentVisual => currentVisual.info.id.Equals(visualId)
            );
        }

        /// <summary>
        ///     Search all product visuals in <paramref name="productVisuals"/>
        ///     for a <see cref="ProductCategorySpecific.CabinetDoor"/> whose
        ///     name matches the expected format.
        /// </summary>
        /// <remarks>
        ///     This is a brute force approach to find the requested door
        ///     style.
        /// </remarks>
        public static ProductVisual FindDoorStyleVisual(
            IReadOnlyList<ProductVisual>    productVisuals,
            ProductVisual                   cabinetSectionVisual,
            string                          doorStyleName)
        {
            if (productVisuals == null)
            {
                throw new System.ArgumentNullException(nameof(productVisuals));
            }

            if (!cabinetSectionVisual)
            {
                throw new System.ArgumentNullException(nameof(cabinetSectionVisual));
            }

            // This has a '^' suffix.
            string cabinetSectionVisualName = cabinetSectionVisual.name;

            string expectedDoorStyleVisualName =
                cabinetSectionVisualName.Substring(0, length: cabinetSectionVisualName.Length - 1) + "@" + doorStyleName + "^";

            ProductVisual doorStyleVisual = null;

            foreach (ProductVisual visual in productVisuals)
            {
                if (visual.info.runtimePrefabCategory != ProductCategorySpecific.CabinetDoor)
                {
                    continue;
                }

                if (visual.name != expectedDoorStyleVisualName)
                {
                    continue;
                }

                doorStyleVisual = visual;
                break;
            }

            bool foundCabinetDoorVisual = doorStyleVisual;

            if (foundCabinetDoorVisual != cabinetSectionVisual.info.expectingCabinetDoors)
            {
                // Warn that finding a door style was or wasn't expected here.
                // The type of expectation depends on the cabinet section.

                if (cabinetSectionVisual.info.expectingCabinetDoors)
                {
                    MelcherLog.LogError($"Could not find a visual with the name {MelcherFormat.ConsoleString(doorStyleName)}");
                }
                else
                {
                    MelcherLog.LogError($"Found a visual with the name {MelcherFormat.ConsoleString(doorStyleName)} but expected to find none.");
                }
            }

            return doorStyleVisual;
        }


        /// <summary>
        ///     Calculate a plane suitable for raycasting drag events.
        ///     Depending on the camera's angle, the plane is either aligned
        ///     with the front face of the transform (XY-plane) or, if the
        ///     camera is looking down at the transform, the plane is aligned
        ///     with the XZ-plane.
        /// </summary>
        /// <param name="camera"></param>
        public static Plane CalculateCameraLocalDragPlane(OccupiableVolume dragIsRelativeTo, Camera camera)
        {
            Transform resizableCuboidTransform = dragIsRelativeTo.GetTransform();

            // Cabinet's top-left corner (cabinet min + counter height)
            Vector3 worldPlanePosition =
                resizableCuboidTransform.position + Vector3.up * dragIsRelativeTo.occupiableCuboid.size.y;

            // The two possible plane normals
            Vector3 worldPlaneNormalCabinetTop = resizableCuboidTransform.up;
            Vector3 worldPlaneNormalCabinetFront = -resizableCuboidTransform.forward;

            // Calculate the angle between the camera's viewing angle and the
            // normal of the cabinet top
            Transform cameraTransform = camera.transform;
            Vector3 cameraDirection = cameraTransform.forward;
            float lookDotFront = Vector3.Dot(cameraDirection, worldPlaneNormalCabinetFront);
            float angleDegreesFromCabinetFront = Mathf.Acos(-lookDotFront) * Mathf.Rad2Deg;

            // Choose between the top plane or front plane depending on the
            // camera's viewing angle
            const float MaxDegreeAngleForHorizontalPlane = 65.0f;
            bool useTopPlane = angleDegreesFromCabinetFront > MaxDegreeAngleForHorizontalPlane;
            Vector3 cameraLocalPlaneNormal = cameraTransform.InverseTransformDirection(
                useTopPlane ? worldPlaneNormalCabinetTop : worldPlaneNormalCabinetFront
            );
            Vector3 cameraLocalPlanePosition = cameraTransform.InverseTransformPoint(worldPlanePosition);

            return new Plane(cameraLocalPlaneNormal, cameraLocalPlanePosition);
        }

        /// <summary>
        ///     This method is used when dragging products along cabinets.
        ///     It calculates the coordinates of the mouse/pointer relative to
        ///     the camera. It is useful to be relative to the camera because
        ///     it means the camera is free to move between frames and future
        ///     drag coordinates will be relative to the same screen position
        ///     of the cursor preview frame. Therefore, it provides a more
        ///     stable drag experience.
        /// </summary>
        /// <param name="dragIsRelativeTo">
        ///     The volume the product is being dragged along. The product
        ///     does not need to belong to this volume.
        /// </param>
        /// <param name="camera">
        ///     The "event"/main camera. It is the camera the pointer events
        ///     are relative to.
        /// </param>
        /// <param name="eventData">
        ///     A pointer drag event as generated by Unity's default <see
        ///     cref="EventSystem"/>.
        /// </param>
        /// <param name="cameraLocalPosition">
        ///     Output: The position of the pointer along <paramref
        ///     name="dragIsRelativeTo"/>. This is relative to <paramref
        ///     name="camera"/>'s local-space.
        /// </param>
        /// <param name="cameraLocalDragPlane">
        ///     A plane (relative to <paramref name="camera"/>) that represents
        ///     the plane the drag is sliding along.
        /// </param>
        /// <returns>
        ///     Return <see langword="true"/> if the function successfully
        ///     detected a drag plane. Otherwise, return <see
        ///     langword="false"/>.
        /// </returns>
        public static bool CalculateCameraLocalDragPosition(
            OccupiableVolume    dragIsRelativeTo,
            Camera              camera,
            PointerEventData    eventData,
            out Vector3         cameraLocalPosition,
            out Plane           cameraLocalDragPlane)
        {
            cameraLocalPosition = Vector3.zero;
            cameraLocalDragPlane = CalculateCameraLocalDragPlane(dragIsRelativeTo, camera);

            Ray worldRay = camera.ScreenPointToRay(eventData.position);

            Transform cameraTransform = camera.transform;

            Ray cameraLocalRay =
                new Ray(
                    cameraTransform.InverseTransformPoint(worldRay.origin),
                    cameraTransform.InverseTransformDirection(worldRay.direction)
                );

            if (!cameraLocalDragPlane.Raycast(cameraLocalRay, out float cameraLocalDistance))
            {
                MelcherLog.LogEditorOnly("Raycast didn't hit. Was this the backside?");
                return false;
            }

            cameraLocalPosition = cameraLocalRay.GetPoint(cameraLocalDistance);

            // Debug.DrawLine(
            //     cameraTransform.TransformPoint(cameraLocalPosition),
            //     cameraTransform.position,
            //     debugColor.GetValueOrDefault(Color.yellow),
            //     0.2f
            // );

            return true;
        }


        /// <summary>
        /// This function checks if a distance (measured in meters) is close
        /// enough to zero to mean no size was input in the backend.
        /// </summary>
        public static bool IsDistanceAlmostZero(float meters)
        {
            return Mathf.Abs(meters) < (Units.Inches2Meters * 0.0625f); // 1/16 inch
        }

        /// <summary>
        ///     Check if <paramref name="toBeContained"/> fits within <paramref
        ///     name="toDoContaining"/> without any part of the first expanding
        ///     beyond the bounds of the latter.
        /// </summary>
        /// <param name="toBeContained"></param>
        /// <param name="toDoContaining"></param>
        /// <returns></returns>
        public static bool IsCuboidContainedWithin(Cuboid toBeContained, Cuboid toDoContaining)
        {
            return CuboidOverlapCheck(toBeContained, toDoContaining) == OccupantCuboidStatus.Fits;
        }

        public static OccupantCuboidStatus CuboidOverlapCheck(Cuboid toBeContained, Cuboid toDoContaining)
        {
            OccupantCuboidStatus cuboidStatus;

            // A very small distance, used to handle floating point
            // inaccuracies that appear when transforming cuboid between
            // coordinate spaces.
            const float Threshold = 1e-8f;

            toBeContained.xMin += Threshold;
            toBeContained.xMax -= Threshold;
            toBeContained.yMin += Threshold;
            toBeContained.yMax -= Threshold;
            toBeContained.zMin += Threshold;
            toBeContained.zMax -= Threshold;

            if (SetStatusThenReturnResult(OccupantCuboidStatus.TooFarLeft,      toBeContained.xMin < toDoContaining.xMin) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarRight,     toBeContained.xMax > toDoContaining.xMax) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarDown,      toBeContained.yMin < toDoContaining.yMin) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarUp,        toBeContained.yMax > toDoContaining.yMax) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarForward,   toBeContained.zMin < toDoContaining.zMin) ||
                SetStatusThenReturnResult(OccupantCuboidStatus.TooFarBack,      toBeContained.zMax > toDoContaining.zMax))
            {
                MelcherLog.Log("Failing on condition " + cuboidStatus, severity: MelcherLogSeverity.Verbose);
                MelcherLog.Log($"To be contained '{toBeContained}' vs container '{toDoContaining}", severity: MelcherLogSeverity.Verbose);
            }
            else
            {
                cuboidStatus = OccupantCuboidStatus.Fits;
            }

            return cuboidStatus;

            /// <summary>
            ///     Nested function that sets the test-type being performed,
            ///     then returns the result. This is intended to be used in an
            ///     if statement with many or'd together checks. The first check
            ///     to fail will have set the status to the test-type that
            ///     failed which avoids needing separate `if` statements for
            ///     each fail condition.
            /// </summary>
            /// <param name="status">
            ///     The cuboid overlap type being tested. E.g., <see
            ///     cref="OccupantCuboidStatus.TooFarLeft"/>.
            /// </param>
            /// <param name="condition">
            ///     The result of the overlap type being tested. E.g., when
            ///     <paramref name="status"/> is <see
            ///     cref="OccupantCuboidStatus.TooFarLeft"/>, the test result
            ///     is expected to be <see langword="true"/> when the cuboid is
            ///     within range - it is *not* too far left.
            /// </param>
            /// <returns>
            ///     Returns <paramref name="testResult"/>.
            /// </returns>
            bool SetStatusThenReturnResult(OccupantCuboidStatus status, bool testResult)
            {
                cuboidStatus = status;

                return testResult;
            }
        }

        /// <summary>
        /// Return <see langword="true"/> if the product category uses cut-out
        /// sizes and should therefore also report errors when they are missing
        /// during parsing. If a category doesn't use cut-out sizes, no errors
        /// have to be reported if the products' cut-out size is missing.
        /// </summary>
        public static bool DoesCategoryUseSizes(ProductCategory category)
        {
            return !s_CategoriesWithoutSizes.Contains(category);
        }

        public static JsonDict Vector3ToJson(Vector3 vec)
        {
            return new Dictionary<string, object>()
            {
                { "x", vec.x },
                { "y", vec.y },
                { "z", vec.z }
            };
        }

        // Not used anywhere because MelcherJsonObjectParser cannot fetch an
        // object.
        // public static Vector3 Vector3FromJson(JsonDict vecAsJson)
        // {
        //     float x, y, z;
        //     vecAsJson.TryGetJsonVaue("x", out x);
        //     vecAsJson.TryGetJsonVaue("y", out y);
        //     vecAsJson.TryGetJsonVaue("z", out z);
        //     return new Vector3(x, y, z);
        // }

        /// <summary>
        ///     Given a corner wedge cabinet, compute the projected length of
        ///     the corner wedge onto its orthogonal neighbours. In other words,
        ///     uses the length of the corner wedge's hypotenuse to calculate
        ///     the length of its other two sides.
        /// </summary>
        public static float CalculateCabinetCornerWedgeOrthogonalLength(Cabinet cornerWedge)
        {
            return Mathf.Sqrt(cornerWedge.sizeInMeters.x * cornerWedge.sizeInMeters.x / 2.0f);
        }



        // =====================================================================
        // Static fields
        // =====================================================================

        // Unicode quote characters. Used to normalize input of our imperial
        // measurements (string) to meters
        // From here: https://www.fileformat.info/info/unicode/char/search.htm?q=quotation&preview=entity



        /// <summary>
        /// Product categories to ignore cut-out and overlap size validation
        /// </summary>
        private static List<ProductCategory> s_CategoriesWithoutSizes = new List<ProductCategory>()
        {
            ProductCategory.Finish,
            ProductCategory.FinishFloor,
            ProductCategory.FinishCabinet,
            ProductCategory.FinishCountertop,
        };

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================
        #region Editor-only

        public static class Editor
        {
            /// <summary>
            ///     Creates or overwrites a file in "&lt;UnityProjectRoot&gt;/Logs/ProFit/*"
            ///     with the content, <paramref name="content"/>. We use this
            ///     to capture ignorable errors that may otherwise spam the
            ///     console.
            /// </summary>
            /// <returns>
            ///     The full path of the log file.
            /// </returns>
            public static string OverwriteSpecificLogFile(string logFileName, string content)
            {
                string logFolderPath =
                    System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(
                            Application.dataPath, "../Logs/ProFit"
                        )
                    );

                System.IO.Directory.CreateDirectory(logFolderPath);

                string outputPath =
                    System.IO.Path.Combine(logFolderPath, logFileName);

                System.IO.File.WriteAllText(outputPath, content);

                return outputPath;
            }

            /// <summary>
            ///     Get all asset GUIDs for assets in the requested folder that
            ///     match the filter. The filter uses the same rules as
            ///     searching the project window.
            /// </summary>
            /// <returns></returns>
            public static string[] FindAllAssetGuidsInFolder(string assetFilter, string projectRelativePath)
            {
                return
                    UnityEditor.AssetDatabase.FindAssets(
                        filter: assetFilter,
                        searchInFolders: new[] { projectRelativePath }
                    );
            }

            /// <summary>
            ///     Get all asset GUIDs for models in the CabinetSectionModels
            ///     folder.
            /// </summary>
            public static string[] FindAllCabinetSectionModelGuids()
            {
                string[] guids =
                    UnityEditor.AssetDatabase.FindAssets(
                        filter: "t:model",
                        searchInFolders: new[] { "Assets/AssetsMelcher/CabinetSectionModels" }
                    );

                return guids;
            }

        }   // class Editor



        #endregion  // Editor-only
#endif  // if UNITY_EDITOR

    }   // class ProFitUtility

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-31
/// ===========================================================================
