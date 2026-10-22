namespace KitchenDesign
{
    using Melcher;
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

        public static void SetAllRendererMaterials(Renderer renderer, Material material)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            for (int i = 0; i < sharedMaterials.Length; ++i)
            {
                sharedMaterials[i] = material;
            }
            renderer.sharedMaterials = sharedMaterials;
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
            return Mathf.Abs(meters) < (Const.Inches2Meters * 0.0625f); // 1/16 inch
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
        /// Try to parse a length in inches into a length in meters. Tries to
        /// decode an imperial formatted string E.g. <code>2' 3 1/2"</code>
        /// </summary>
        /// <param name="imperialText">The measurement in imperial format</param>
        /// <param name="lengthInMeters"></param>
        /// <param name="name">An optional name to associate with error messages</param>
        /// <param name="errors">An optional list to collect error messages</param>
        /// <returns>
        /// Return true if the measurement could be parsed; otherwise, return
        /// false. This function may return true but still append errors into
        /// the error list.
        /// </returns>
        public static bool TryParseImperialString(
            string          imperialText,
            out float       lengthInMeters,
            string          name = null,
            List<string>    errors = null)
        {
            // Initialize the output to zero
            lengthInMeters = 0.0f;

            // Prep the optional name for better logging
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.Empty;
            }
            else
            {
                name += ": ";
            }

            // Normalize the quote characters. Users may copy and paste the
            // measurements from a webpage that has some special quote characters
            System.Text.StringBuilder buffer = new System.Text.StringBuilder(imperialText);
            foreach (string singleQuote in SingleQuoteCharacters)
            {
                buffer.Replace(singleQuote, "'");
            }
            foreach (string doubleQuote in DoubleQuoteCharacters)
            {
                buffer.Replace(doubleQuote, "\"");
            }
            imperialText = buffer.ToString();

            // Look for groups of numbers. Each group may have one of the
            // following optional suffixes:
            //  ' = feet
            //  " = inches
            //  / = division with next number

            const char Feet = '\'';
            const char Inch = '"';
            const char Divide = '/';

            MatchCollection regexMatches = Regex.Matches(imperialText, @$"(?:(\d+(?:\.\d*)?)\s*[{Inch}{Feet}{Divide}]?)");

            int matchCount = regexMatches.Count;
            float lengthInInches = 0.0f;
            for (int i = 0; i < matchCount; ++i)
            {
                string numberMatchText = regexMatches[i].Groups[1].Value;
                if (!float.TryParse(numberMatchText, out float numberMatchAsFloat))
                {
                    errors?.Add(name + "Failed to parse float from: " + numberMatchText);
                    return false;
                }

                string wholeMatchText = regexMatches[i].Value;
                if (wholeMatchText.EndsWith(Feet))
                {
                    numberMatchAsFloat *= Const.InchesPerFoot;

                }
                else if (i == 0 && wholeMatchText.EndsWith(" ") &&
                    ((i + 1 >= matchCount) || !regexMatches[i + 1].Value.EndsWith(Divide)))
                {
                    // If this is the first number, it doesn't have a suffix and the
                    // next number is not a divisor, then this must be feet
                    numberMatchAsFloat *= Const.InchesPerFoot;
                }
                else if (wholeMatchText.EndsWith(Divide))
                {
                    if (i + 1 >= matchCount)
                    {
                        errors?.Add(name + "Invalid imperial format: Division symbol must have a number after it");
                        break;
                    }

                    // Get the next number and divide by it
                    string nextNumberMatchText = regexMatches[i + 1].Groups[1].Value;
                    if (!float.TryParse(nextNumberMatchText, out float divisor))
                    {
                        errors?.Add(name + "Failed to parse float from: " + nextNumberMatchText);
                        break;
                    }

                    int numeratorAsInt = Mathf.RoundToInt(numberMatchAsFloat);
                    int divisorAsInt = Mathf.RoundToInt(divisor);

                    // Some quick input validation
                    if (divisor == 0.0f)
                    {
                        errors?.Add(name + $"Would-be division by zero: {wholeMatchText}/{regexMatches[i + 1].Value}");
                        return false;
                    }

                    if (!Mathf.Approximately(numberMatchAsFloat - numeratorAsInt, 0.0f))
                    {
                        errors?.Add(name + $"Numerator is a decimal: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    if (!Mathf.Approximately(divisor - divisorAsInt, 0.0f))
                    {
                        errors?.Add(name + $"Denominator is a decimal: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    if (divisorAsInt != 3 && divisorAsInt != 5 && Mathf.NextPowerOfTwo(divisorAsInt) != divisorAsInt)
                    {
                        // Warn if the fraction has a weird denominator. It
                        // should be a power of two but we also allow 3 and 5.
                        errors?.Add(name + $"Non-standard imperial denominator: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    numberMatchAsFloat /= divisor;

                    if (numberMatchAsFloat >= 1.0f)
                    {
                        errors?.Add(name + $"Improper fraction: {wholeMatchText}/{regexMatches[i + 1].Value}");
                    }

                    // Extra increment to skip the denominator group we already
                    // processed
                    ++i;
                }

                lengthInInches += numberMatchAsFloat;
            }

            lengthInMeters = lengthInInches * Const.Inches2Meters;

            return true;
        }

        /// <summary>
        ///     Return a string representation of a size in centimeters. E.g.,
        ///     1m => "100.00cm"
        /// </summary>
        /// <param name="sizeInMeters"></param>
        /// <param name="rightDecimalPlaces">
        ///     The number of trailing decimal places that will be padded with
        ///     zeros. If this is <see langword="null"/> the right end is not
        ///     restricted or padded.
        /// </param>
        /// <param name="leftDecimalPlaces">
        ///     The number of leading decimal places that will be padded with
        ///     spaces. If this is <see langword="null"/>, the leading part
        ///     of the number is not be padded.
        /// </param>
        /// <returns></returns>
        public static string FormatMetersToCentimeters(float sizeInMeters, int? rightDecimalPlaces = 2, int? leftDecimalPlaces = null)
        {
            float sizeInCentimeters = sizeInMeters * Const.Meters2Centimeters;

            string result =
                rightDecimalPlaces.HasValue ?
                    sizeInCentimeters.ToString("N" + rightDecimalPlaces.Value) :
                    sizeInCentimeters.ToString();

            if (leftDecimalPlaces.HasValue)
            {
                int numberOfDigitsBeforeDecimal =
                    result.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                if (numberOfDigitsBeforeDecimal < 0)
                {
                    numberOfDigitsBeforeDecimal = 0;
                }

                int unrestrictedLength =
                    result.Length - numberOfDigitsBeforeDecimal;

                result = result.PadLeft(unrestrictedLength + leftDecimalPlaces.Value);
            }

            return $"{result}cm";
        }

        public static string FormatMetersToImperial(float sizeInMeters, bool showFeet = true, bool showFraction = true, int minimumInchDivisions = ImperialUnit.DefaultMaximumInchDivision)
        {
            // Calculate values to display
            return ImperialUnit.FromTotalMeters(sizeInMeters).ToString(showFeet, showFraction, minimumInchDivisions);
        }

        /// <summary>
        /// Create a string for a measurement that is formatted with monospace
        /// font so the text is less jittery if it is updated rapidly. Uses a
        /// string builder for better performance.
        /// </summary>
        /// <param name="stringBuilder"></param>
        /// <param name="monoSize"></param>
        /// <param name="sizeInMeters"></param>
        /// <param name="showFeet"></param>
        /// <param name="showFraction"></param>
        /// <returns></returns>
        public static string FormatMetersToImperialMonospaced(
            StringBuilder stringBuilder,
            int monoSize,
            float sizeInMeters,
            bool showFeet = true,
            bool showFraction = true,
            int minimumInchDivisions = ImperialUnit.DefaultMaximumInchDivision,
            bool showInchesWhenZero = true)
        {
            stringBuilder.Clear();

            ImperialUnit.FromTotalMeters(sizeInMeters)
                .GetValuesUsingMaximumInchDivisions(minimumInchDivisions, out int feet, out int inches, out int numerator, out int denominator);

            stringBuilder.AppendFormat("<mspace={0}>", monoSize);
            {
                // Feet
                if (feet > 0)
                {
                    stringBuilder.AppendFormat("{0,2}'", feet);
                }
                else
                {
                    stringBuilder.Append(' ', 3);   // Spaces to pad for: 2 number chars + 1 quote (') char
                }

                // Inches
                if (inches > 0 || feet > 0 || showInchesWhenZero)
                {
                    stringBuilder.AppendFormat("{0,2}\"", inches);
                }
                else
                {
                    stringBuilder.Append(' ', 3);
                }
            }
            stringBuilder.Append("</mspace>");

            // Fractional inches
            stringBuilder.AppendFormat("<size=50%><mspace={0}>", monoSize / 2);
            {
                if (numerator > 0 && denominator > 0)
                {
                    stringBuilder.AppendFormat("{0,1}/{1,1}", numerator, denominator);
                }
                else
                {
                    stringBuilder.Append(' ', 3);   // 1 numerator char + 1 slash char + 1 denominator char
                }
            }
            stringBuilder.Append("</mspace></size>");

            return stringBuilder.ToString();
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

        /// <summary>
        /// Given a URL, return the second-level domain and top-level domain
        /// part. E.g. "https://sub.root.com/abc" => "root.com". The URL's
        /// scheme (e.g. http://) is optional and isn't include in the output.
        /// If the input string doesn't have a second-level domain, the
        /// function will behave the same but only return the root-level part.
        /// E.g. "com", "ca", etc.
        /// <br /><br />
        ///
        /// URL parts:
        /// [Scheme]http://     [Domain]third.second.first/     [Path]other/part/
        /// <br /><br />
        ///
        /// E.g. All of the following inputs would return <c>site.com</c>:
        /// <code>
        ///     site.com
        ///     www.site.com/
        ///     https://www.site.com
        ///     https://subdomain.domain.site.com/abcd/1234/file.txt
        /// </code>
        /// </summary>
        /// <param name="url">
        /// URL to format. Does not require a scheme (e.g. "http://").
        /// </param>
        public static string GetBaseDomain(string url)
        {
            string baseDomain = url ?? "";

            // Remove the scheme ("http://", "https://", etc.)
            const string SchemeSeparator = "://";
            int schemeIndex = baseDomain.IndexOf(SchemeSeparator);
            if (schemeIndex >= 0)
            {
                baseDomain = baseDomain.Substring(schemeIndex + SchemeSeparator.Length);
            }

            // Remove anything after and including the first slash
            int slashIndex = baseDomain.IndexOf('/');
            if (slashIndex >= 0)
            {
                baseDomain = baseDomain.Substring(0, slashIndex);
            }

            // Test if this is an IP.
            Match ipMatch = Regex.Match(baseDomain, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            if (ipMatch != null && ipMatch.Success)
            {
                return ipMatch.Value;
            }

            // Remove all tertiary and higher subdomains
            int lastDotIndex = baseDomain.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                // Get second last dot:
                //  lots.of.subdomains.root.com
                //                    ^
                int subdomainDotIndex = baseDomain.LastIndexOf('.', lastDotIndex - 1);
                if (subdomainDotIndex >= 0)
                {
                    baseDomain = baseDomain.Substring(subdomainDotIndex + 1);
                }
            }

            return baseDomain;
        }

        public static string GetDomain(string url)
        {
            if (!url.StartsWith("https://") && !url.StartsWith("http://"))
            {
                url = "https://" + url;
            }

            string host = "";

            try
            {
                System.Uri uri = new System.Uri(url);
                host = uri.Host;
            }
            catch
            {
            }

            return host;
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
        ///     Given a regular prefab, this function returns a duplicate as a
        ///     "runtime prefab". A runtime prefab is intended to be like a
        ///     regular prefab (as would be created in the editor) and live in
        ///     memory only, but it must live in the scene.
        /// </summary>
        /// <typeparam name="PrefabType"></typeparam>
        /// <param name="originalPrefab"></param>
        /// <returns></returns>
        public static PrefabType InstantiatePrefabAsRuntimePrefab<PrefabType>(PrefabType originalPrefab)
            where PrefabType : Component
        {
            PrefabType runtimePrefab = GameObject.Instantiate(originalPrefab);

            // Remove the "(clone)" suffix
            runtimePrefab.gameObject.name = originalPrefab.name;

            // Hide the clone; it is never meant to be seen. It is intended
            // to live in-memory only and this is the closest to that we
            // can get.
            runtimePrefab.gameObject.SetActive(false);

            // It is important that DontDestroyOnLoad get called after
            // the object is disabled. Otherwise, some components in the
            // runtime prefab may not behave correctly. E.g., the editor-only
            // lights remaining active in ProductVisuals. Unity bug?
            GameObject.DontDestroyOnLoad(runtimePrefab.gameObject);

            return runtimePrefab;
        }

        /// <summary>
        ///     Return <see cref="true"/> if <paramref name="gameObject"/> is
        ///     a prefab asset or a Pro-Fit "runtime prefab". A runtime prefab
        ///     is a simulation of a prefab asset at runtime; they are
        ///     simulated by being instantiated into the DontDestroyOnLoadScene,
        ///     allowing them to persist between scene changes, and having their
        ///     game object disabled to remain hidden and non-updating.
        /// </summary>
        public static bool IsPrefab(GameObject gameObject)
        {
            if (!gameObject)
            {
                throw new System.ArgumentNullException(nameof(gameObject));
            }

            bool isPrefabAsset =
                !gameObject.scene.IsValid();

#if UNITY_EDITOR
            // The scene.IsValid() trick doesn't work with prefabs in the prefab
            // stage, so check against it explicitly.
            isPrefabAsset |=
                MelcherEditorUtility.IsPartOfCurrentPrefabStage(gameObject);
#endif  // if UNITY_EDITOR

            // "Runtime prefabs" exist in the DontDestroyOnLoadScene.
            bool isRuntimePrefab =
                gameObject.scene.path == "DontDestroyOnLoad";

            return isPrefabAsset || isRuntimePrefab;
        }

        /// <summary>
        ///     Set the GameObject and all of its grandchildren to a specific
        ///     layer. This was initially created for applying the Cabinet game
        ///     layer to all mesh subparts of a cabinet created within the
        ///     Catalog building method.
        /// </summary>
        public static void SetGameObjectLayerRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;

            foreach (Transform child in transform)
            {
                SetGameObjectLayerRecursively(child, layer);
            }
        }

        public static bool TrySetRenderQuality(UniversalRenderPipelineAsset requestedQuality)
        {
            int qualityCount = QualitySettings.names.Length;

            for (int i = 0; i < qualityCount; ++i)
            {
                UniversalRenderPipelineAsset qualityPipeline =
                    (UniversalRenderPipelineAsset)
                        QualitySettings.GetRenderPipelineAssetAt(i);

                if (qualityPipeline != requestedQuality)
                {
                    continue;
                }

                int currentQuality = QualitySettings.GetQualityLevel();
                if (currentQuality != i)
                {
                    QualitySettings.SetQualityLevel(i);
                }

                return true;
            }

            return false;
        }

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

        /// <summary>
        ///     Force the garbage collector to run to free up memory (iOS).
        ///      Without this, low-memory devices may run out of memory.
        /// </summary>
        public static void GarbageCollectOnLowMemoryDevices()
        {
#if UNITY_IOS
            System.GC.Collect();
#endif  // if UNITY_IOS
        }


#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Melcher Studios/Tests/ProFitUtility.GetBaseDomain")]
        private static void TestUrls()
        {
            MelcherLog.Log(GetBaseDomain(""));
            MelcherLog.Log(GetBaseDomain("root.com"));
            MelcherLog.Log(GetBaseDomain("root.com/"));
            MelcherLog.Log(GetBaseDomain("www.root.com/"));
            MelcherLog.Log(GetBaseDomain("https://www.root.com"));
            MelcherLog.Log(GetBaseDomain("https://subdomain.domain.root.com/abcd"));
            MelcherLog.Log(GetBaseDomain("https://subdomain.domain.root.com/abcd/1234/file.txt"));
            MelcherLog.Log(GetBaseDomain("http://23.111.69.219/admin/"));
            MelcherLog.Log(GetBaseDomain("http://23.111.69.219:8080/admin/"));
            MelcherLog.Log(GetBaseDomain("23.111.69.219:8080/admin/"));
            MelcherLog.Log(GetBaseDomain("23.111.69.219:8080"));
        }

        [UnityEditor.MenuItem("Tools/Melcher Studios/Tests/ProFitUtility.GetDomain")]
        private static void TestGetDomain()
        {
            MelcherLog.Log(GetDomain(""));
            MelcherLog.Log(GetDomain("root.com"));
            MelcherLog.Log(GetDomain("root.com/"));
            MelcherLog.Log(GetDomain("www.root.com/"));
            MelcherLog.Log(GetDomain("https://www.root.com"));
            MelcherLog.Log(GetDomain("https://subdomain.domain.root.com/abcd"));
            MelcherLog.Log(GetDomain("https://subdomain.domain.root.com/abcd/1234/file.txt"));
            MelcherLog.Log(GetDomain("http://23.111.69.219/admin/"));
            MelcherLog.Log(GetDomain("http://23.111.69.219:8080/admin/"));
            MelcherLog.Log(GetDomain("23.111.69.219:8080/admin/"));
            MelcherLog.Log(GetDomain("23.111.69.219:8080"));
        }
#endif  // if UNITY_EDITOR

        // =====================================================================
        // Static fields
        // =====================================================================

        // Unicode quote characters. Used to normalize input of our imperial
        // measurements (string) to meters
        // From here: https://www.fileformat.info/info/unicode/char/search.htm?q=quotation&preview=entity

        /// <summary>
        /// Unicode characters to replace with ASCII double quote " (\u0022)
        /// </summary>
        public static readonly string[] DoubleQuoteCharacters = new[]
        {
            "\u201C",
            "\u201D",
            "\u201F",
            "\u275D",
            "\u275E",
            "\u301D",
            "\u301E",
            "\U0001F676",
            "\U0001F677",
            "\U0001F678"
        };

        /// <summary>
        /// Unicode characters to replace with ASCII single quote ' (\u0027)
        /// </summary>
        public static readonly string[] SingleQuoteCharacters = new[]
        {
            "\u2018",
            "\u2019",
            "\u201B",
            "\u275B",
            "\u275C",
            "\uFF02",
        };

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

        /// <summary>
        /// Retrieve a random name to use when filling out a user-sign up
        /// form. The first time this function is called, it opens and reads a
        /// file for the names (one name per line) and caches them for next
        /// time.
        /// </summary>
        public static string RandomName()
        {
            if (s_RandomNames == null)
            {
                s_RandomNames = GetLinesFromAsset(
                    "Assets/AssetsMelcher/Editor/RandomNames.txt"
                );
            }

            return s_RandomNames[Random.Range(0, s_RandomNames.Count)];
        }

        public static string RandomPlace()
        {
            if (s_RandomPlaces == null)
            {
                s_RandomPlaces = GetLinesFromAsset(
                    "Assets/AssetsMelcher/Editor/RandomPlaces.txt"
                );
            }

            return s_RandomPlaces[Random.Range(0, s_RandomPlaces.Count)];
        }

        private static List<string> GetLinesFromAsset(string assetPath)
        {
            TextAsset namesAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

            string[] names = namesAsset.text.Split(
                new string[] { "\r\n", "\r", "\n" },
                System.StringSplitOptions.None
            );

            return new List<string>(names);
        }

        private static List<string> s_RandomNames;
        private static List<string> s_RandomPlaces;

        #endregion  // Editor-only
#endif  // if UNITY_EDITOR

    }   // class ProFitUtility

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-31
/// ===========================================================================
