namespace Melcher
{
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public static class MelcherUnityUtility
    {
        // ====================================================================
        // UnityEngine.Object
        // ====================================================================

        /// <summary>
        ///     This is a compatibility layer function. It calls the version of
        ///     <c>FindObjects*</c> that is most appropriate for the running
        ///     Unity version.
        /// </summary>
        /// <param name="sortMode">
        ///     The type of sort order to use on the returned array. This
        ///     parameter is ignored in versions before Unity 6.
        /// </param>
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode)
            where T : Object
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindObjectsByType<T>((UnityEngine.FindObjectsSortMode)sortMode);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }

        public enum FindObjectsSortMode
        {
            None,
            InstanceID
        }

        // =====================================================================
        // Transform
        // =====================================================================

        public static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;

            foreach (Transform child in target)
            {
                SetLayerRecursively(child, layer);
                child.gameObject.layer = layer;
            }
        }

        public static void MakeChildWithIdentityPose(Transform parent, Transform child)
        {
            child.SetParent(parent, worldPositionStays: false);
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }

        public static string GetHierarchyPath(Transform t, bool includeSceneName = false)
        {
            string path = $"/{t.name}";

            while (t.parent)
            {
                t = t.parent;
                path = $"/{t.name}{path}";
            }

            if (includeSceneName)
            {
                path = $"{t.gameObject.scene.path}:{path}";
            }

            return path;
        }

        // =====================================================================
        // Component
        // =====================================================================

        /// <inheritdoc cref="IsComponentEnabled(Component, out bool)"/>
        public static bool IsComponentEnabled(Component component)
        {
            return IsComponentEnabled(component, out bool ignored);
        }

        /// <summary>
        ///     Check if <paramref name="component"/> is enabled.
        /// </summary>
        /// <param name="component">
        ///     The component that will be checked to see if it is enabled.
        ///     <br /><br />
        ///
        ///     <see langword="null"/> is allowed and will always return <see
        ///     langword="false"/>.
        /// </param>
        /// <param name="componentHasEnabledProperty">
        ///     An output argument that is set to <see langword="true"/> if
        ///     <paramref name="component"/> supports an `enabled` property;
        ///     otherwise, it is set to <see langword="false"/>.
        ///     <br /><br />
        ///
        ///     This is always <see langword="false"/> if <paramref
        ///     name="component"/> is <see langword="null"/>.
        /// </param>
        /// <returns>
        ///     Return <see langword="true"/> if the component is enabled.
        ///     <br /><br />
        ///
        ///     <see langword="null"/> components always return <see
        ///     langword="false"/>.
        ///     <br /><br />
        ///
        ///     Many components do not support an "enabled" property and these
        ///     will always return <see langword="true"/>. To know if <paramref
        ///     name="component"/> supports an enabled property, check the
        ///     value of <paramref name="componentHasEnabledProperty"/>.
        /// </returns>
        public static bool IsComponentEnabled(Component component, out bool componentHasEnabledProperty)
        {
            // It is simpler to assume all non-null components are enabled by
            // default (to handle components that don't have such a property),
            // and then check for and handle the exception cases after.

            bool isEnabled = component != null;

            componentHasEnabledProperty = true;

            switch (component)
            {
                case Behaviour behaviour:
                    // Behaviours (including MonoBehaviours) have an enabled property
                    isEnabled = behaviour.enabled;
                    break;

#if ENABLE_CLOTH
                case Cloth cloth:
                    isEnabled = cloth.enabled;
                    break;
#endif  // if ENABLE_CLOTH

#if ENABLE_PHYSICS
                case Collider collider:
                    isEnabled = collider.enabled;
                    break;
#endif  // if ENABLE_PHYSICS

                case LODGroup lodGroup:
                    isEnabled = lodGroup.enabled;
                    break;

                case Renderer renderer:
                    isEnabled = renderer.enabled;
                    break;

                default:
                    componentHasEnabledProperty = false;
                    break;
            }

            return isEnabled;
        }


        /// <inheritdoc cref="SetComponentEnabled(Component, bool, out bool)"/>
        public static void SetComponentEnabled(Component component, bool enabled)
        {
            SetComponentEnabled(component, enabled, out bool ignored);
        }

        /// <summary>
        ///     Set the `enabled` property of <paramref name="component"/>.
        ///     <br /><br />
        ///
        ///     Not all components support an `enabled` property. If it is not
        ///     supported, the function does nothing.
        /// </summary>
        /// <param name="component">
        ///     The component whose `enabled` property will be set.
        ///     <br /><br />
        ///
        ///     <see langword="null"/> is allowed and will be silently ignored.
        /// </param>
        /// <param name="componentHasEnabledProperty">
        ///     An output argument that is set to <see langword="true"/> if
        ///     <paramref name="component"/> supports an `enabled` property;
        ///     otherwise, it is set to <see langword="false"/>.
        ///     <br /><br />
        ///
        ///     This is always <see langword="false"/> if <paramref
        ///     name="component"/> is <see langword="null"/>.
        /// </param>
        public static void SetComponentEnabled(Component component, bool enabled, out bool componentHasEnabledProperty)
        {
            componentHasEnabledProperty = true;

            switch (component)
            {
                case Behaviour behaviour:
                    // Behaviours (including MonoBehaviours) have an enabled property
                    behaviour.enabled = enabled;
                    break;

#if ENABLE_CLOTH
                case Cloth cloth:
                    cloth.enabled = enabled;
                    break;
#endif  // if ENABLE_CLOTH

#if ENABLE_PHYSICS
                case Collider collider:
                    collider.enabled = enabled;
                    break;
#endif  // if ENABLE_PHYSICS

                case LODGroup lodGroup:
                    lodGroup.enabled = enabled;
                    break;

                case Renderer renderer:
                    renderer.enabled = enabled;
                    break;

                default:
                    componentHasEnabledProperty = false;
                    break;
            }
        }


        /// <summary>
        ///     Return <see langword="true"/> if the object is part of any
        ///     prefab.  At runtime, no scene objects are considered prefabs
        ///     and therefore always returns <see langword="false"/>.
        ///     <br /><br />
        ///
        ///     This method was added to allow code that executes during
        ///     play-mode and edit-mode to only modify an object while it is
        ///     not a prefab (to avoid dirtying an asset and causing version
        ///     control conflicts).
        /// </summary>
        /// <param name="toCheck">
        ///     The object to inspect as being part of a prefab.
        /// </param>
        public static bool IsPartOfAnyPrefab(Object toCheck)
        {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.IsPartOfAnyPrefab(toCheck);
#else   // if UNITY_EDITOR
            // Never a prefab at runtime.
            return false;
#endif  // if UNITY_EDITOR else

        }

        // ====================================================================
        // EventSystem
        // ====================================================================

        /// <summary>
        /// Return true if any pointer is over a game object. The check is
        /// performed using Unity's UGUI method <see
        /// cref="EventSystem.IsPointerOverGameObject(int)"/> and also
        /// considers any touch inputs.
        /// </summary>
        /// <param name="onlyCheckTouchPhase">
        /// Optional filter for touch inputs. If this is not null, only touches
        /// whose phase matches this value will be considered.
        /// </param>
        public static bool IsAnyPointerOverGameObject(TouchPhase? onlyCheckTouchPhase = TouchPhase.Began)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem)
            {
                // Handle the mouse pointer
                if (eventSystem.IsPointerOverGameObject())
                {
                    return true;
                }

                // Handle touch inputs
                int touchCount = Input.touchCount;
                for (int i = 0; i < touchCount; ++i)
                {
                    Touch touch = Input.GetTouch(i);

                    if (onlyCheckTouchPhase.HasValue && touch.phase != onlyCheckTouchPhase.Value)
                    {
                        continue;
                    }

                    if (eventSystem.IsPointerOverGameObject(touch.fingerId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // =====================================================================
        // Application
        // =====================================================================

        /// <summary>
        /// Return <see langword="true"/> if the app is playing or about to
        /// begin. Otherwise, return <see langword="false"/>.
        /// <br /><br />
        ///
        /// This encapsulates checking <see
        /// cref="UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode"/>
        /// when running in the editor. This is necessary to know if the app
        /// is truly running during play-mode because <see
        /// cref="Application.isPlaying"/> is set too late (E.g., after
        /// ScriptableObject's OnEnable).
        /// </summary>
        public static bool IsApplicationPlaying()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
            return Application.isPlaying;
#endif  // if UNITY_EDITOR else
        }

        // =====================================================================
        // Platform
        // =====================================================================

        public static bool isPlatformCaseSensitive =>
            Application.platform != RuntimePlatform.WindowsEditor &&
            Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.XboxOne &&
            Application.platform != RuntimePlatform.GameCoreXboxOne &&
            Application.platform != RuntimePlatform.GameCoreXboxSeries;


        /// <summary>
        /// Return the platform that the build is targeting.
        /// </summary>
        public static RuntimePlatform targetPlatform
        {
            get
            {
#if UNITY_STANDALONE_WIN
                return RuntimePlatform.WindowsPlayer;
#elif UNITY_STANDALONE_OSX
                return RuntimePlatform.OSXPlayer;
#elif UNITY_STANDALONE_LINUX
                return RuntimePlatform.LinuxPlayer;
#elif UNITY_ANDROID
                return RuntimePlatform.Android;
#elif UNITY_IOS
                return RuntimePlatform.IPhonePlayer;
#elif UNITY_WEBGL
                return RuntimePlatform.WebGLPlayer;
#else
                #error Unhandled platform. Please add support for it
#endif
            }
        }

        /// <summary>
        /// Replicates <see cref="Application.isMobilePlatform"/> but uses
        /// the target platform.
        /// </summary>
        public static bool isTargetMobilePlatform
        {
            get
            {
                RuntimePlatform platform = targetPlatform;
                return platform == RuntimePlatform.Android ||
                    platform == RuntimePlatform.IPhonePlayer ||
                    platform == RuntimePlatform.WSAPlayerARM ||
                    platform == RuntimePlatform.WSAPlayerX64 ||
                    platform == RuntimePlatform.WSAPlayerX86;
            }
        }

        public static bool isTargetStandalonePlatform
        {
            get
            {
#if UNITY_STANDALONE
                return true;
#else   // if UNITY_STANDALONE
                return false;
#endif  // if UNITY_STANDALONE else
            }
        }

        // ====================================================================
        // Version
        // ====================================================================

        public static int VersionCode(string versionString)
        {
            if (!System.Version.TryParse(versionString, out System.Version version))
            {
                version = new System.Version();
            }
            return (version.Major * 1000000) + (version.Minor * 1000) + (version.Build * 1);
        }

        public static int AppVersionCode()
        {
            return VersionCode(Application.version);
        }

        // =====================================================================
        // Serialization
        // =====================================================================

        /// <summary>
        ///     Attempt to build a string containing the type and name of the
        ///     <see cref="UnityEngine.Object"/> that is actively being
        ///     (de)serialized.
        ///     <br /><br />
        ///
        ///     This function will only succeed if it is called during
        ///     serialization (e.g., from within <see
        ///     cref="ISerializationCallbackReceiver.OnBeforeSerialize"/>).
        /// </summary>
        /// <returns>
        ///     If serialization is occurring, return a string in the format:
        ///     "Called from MonoBehaviour '{TypeName}' on game object
        ///     '{GameObjectName}'". Otherwise, return <see langword="null"/>.
        /// </returns>
        public static string ExtractSerializationContext()
        {
            string serializationContext = null;

            try
            {
                // If this executes during an ISerializationCallbackReceiver
                // callback, GameObject.Find will throw a UnityException, which
                // in this case, is desired. The exception is caught and its
                // message is parsed to obtain the context of the
                // (de)serializing object. This context is otherwise
                // inaccessible to us.

                GameObject.Find(null);
            }
            catch (UnityException e)
            {
                string exceptionMessage = e.Message;

                // Extract the serialization context from the exception
                // message.
                Match errorContextMatch =
                    Regex.Match(exceptionMessage, @"Called from [^\r\n]+");

                if (errorContextMatch.Success)
                {
                    serializationContext = errorContextMatch.Value;
                }
            }

            return serializationContext;
        }

        // ====================================================================
        // Math
        // ====================================================================

        /// <summary>
        ///     Calculate the 8 corners of a cuboid from its min and maximum
        ///     extents. The bounds are assumed to be axis-aligned, therefore
        ///     callers should pass local-space coordinates if the bounds are
        ///     rotated and transform the results into the rotated space
        ///     afterwards.
        /// </summary>
        /// <param name="min">
        ///     The minimum extent.
        /// </param>
        /// <param name="max">
        ///     The maximum extent.
        /// </param>
        /// <param name="output">
        ///     The output vertices. This is a <see cref="System.Span{T}>"/> so
        ///     callers can use a <see langword="stackalloc"/>'d array.
        ///     <br /><br />
        ///
        ///     The output vertices follow the order of minimum extent first,
        ///     followed by counter-clockwise rotation through the bottom most
        ///     vertices, followed by the vertex above the minimum extent, and
        ///     counter-clockwise rotation through the remaining top vertices.
        ///     <br /><br />
        ///
        ///     This means that <c>output[i + 4]</c> can be used with the index
        ///     on a vertex in the bottom ring to retrieve the vertex in the top
        ///     ring that is directly above it.
        /// </param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void CalculateAABBCorners(Vector3 min, Vector3 max, System.Span<Vector3> output)
        {
            if (output.Length < 8)
            {
                throw new System.ArgumentException("Expected `output` to be at least 8 elements.", nameof(output));
            }

            // Calculate the corner vertices between min and max.

            output[0] = min;
            output[1] = new (max.x, min.y, min.z);
            output[2] = new (max.x, min.y, max.z);
            output[3] = new (min.x, min.y, max.z);

            output[4] = new (min.x, max.y, min.z);
            output[5] = new (max.x, max.y, min.z);
            output[6] = max;
            output[7] = new (min.x, max.y, max.z);
        }
    }   // class MelcherUnityUtility

}   // namespace Melcher
