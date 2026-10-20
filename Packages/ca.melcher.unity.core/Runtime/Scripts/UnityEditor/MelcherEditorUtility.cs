#if UNITY_EDITOR
namespace Melcher
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.PackageManager;

#if UNITY_2021_1_OR_NEWER
    using UnityEditor.SceneManagement;
#else
    using UnityEditor.Experimental.SceneManagement;
#endif

    using PackageInfo = UnityEditor.PackageManager.PackageInfo;


    /// <summary>
    /// Collection of useful methods that are limited to the Unity editor.
    /// </summary>
    public static class MelcherEditorUtility
    {
        /// <summary>
        /// Call the object's OnValidate method (using reflection).
        /// </summary>
        /// <returns>
        /// Return true if the object had a OnValidate method; otherwise,
        /// return false.
        /// </returns>
        public static bool Validate(UnityEngine.Object toValidate)
        {
            if (toValidate == null)
            {
                return false;
            }

            var validateType = toValidate.GetType();
            var validateMethod = validateType.GetMethod(
                name: "OnValidate",
                bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Array.Empty<Type>(),
                modifiers: null
            );
            validateMethod?.Invoke(toValidate, Array.Empty<object>());
            return validateMethod != null;
        }

        /// <summary>
        ///     Ping an object in the project window.
        /// </summary>
        public static void PingObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                throw new System.ArgumentNullException(nameof(obj));
            }

            try
            {
                EditorGUIUtility.PingObject(obj);
            }
            catch (NullReferenceException e)
            {
                MelcherLog.LogWarning(
                    "The following exception may be a bug in Unity caused by having more than 1 project view locked. Try unlocking all but one project view and try again.",
                    context: obj
                );
                Debug.LogException(e);
            }
        }

        public static void OpenPropertyEditor(UnityEngine.Object unityObject)
        {
            Assembly unityEditorAssembly = typeof(Editor).Assembly;
            Type propertyEditorType = unityEditorAssembly.GetType("UnityEditor.PropertyEditor");

            // Find the following function:
            // internal static PropertyEditor OpenPropertyEditor(Object obj, bool showWindow = true)
            MethodInfo openPropertyEditorMethod = propertyEditorType.GetMethod(
                name: "OpenPropertyEditor",
                bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new Type[] {
                    typeof(UnityEngine.Object), // obj
                    typeof(bool)                // showWindow
                },
                modifiers: null
            );

            bool showWindow = true;
            openPropertyEditorMethod?.Invoke(obj: null, new object[] { unityObject, showWindow });
        }

        public static long GetLocalIdentifierInFile(UnityEngine.Object obj)
        {
            var serializedObject = new SerializedObject(obj);

            var inspectorModeProp = serializedObject.GetType().GetProperty(
                name: "inspectorMode",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            inspectorModeProp.SetValue(serializedObject, InspectorMode.Debug, null);

            SerializedProperty localIdProp =
                serializedObject.FindProperty(
                    "m_LocalIdentfierInFile"    // Intentional misspelling
                );

            return localIdProp.longValue;
        }

        public static void SetScriptExecutionOrder(int executionOrderOffset, [CallerFilePath] string absoluteScriptPath = null)
        {
            MelcherEditorCoroutine.StartEditorCoroutine(Routine(absoluteScriptPath));

            IEnumerator Routine(string absoluteScriptPath)
            {
                string scriptName =
                    Path.GetFileNameWithoutExtension(absoluteScriptPath);

                // Check for dots in the filename. We assume this means the file
                // represents a partial class, and stripping the dot and
                // everything after it should give us the true MonoBehaviour
                // name.

                int dotIndex = scriptName.IndexOf('.');
                if (dotIndex >= 0)
                {
                    scriptName = scriptName.Substring(0, dotIndex);

                    absoluteScriptPath =
                        Path.Combine(
                            Path.GetDirectoryName(absoluteScriptPath),
                            scriptName + Path.GetExtension(absoluteScriptPath)
                        );
                }

                if (!absoluteScriptPath.StartsWith(MelcherPredefinedFilePath.project))
                {
                    // The script is outside of the project root. We need to
                    // convert the absolute path into one inside the project's
                    // Packages folder

                    // Find all packages
                    var listOperation =
                        Client.List(
                            offlineMode: true,
                            includeIndirectDependencies: true
                        );

                    while (!listOperation.IsCompleted)
                    {
                        yield return null;
                    }

                    // Abort if packages could not be retrieved
                    if (listOperation.Status != UnityEditor.PackageManager.StatusCode.Success)
                    {
                        MelcherLog.LogError($"Could not list packages.");
                        yield break;
                    }

                    // Check all installed packages to determine if any matches
                    // the script's path
                    foreach (var packageInfo in listOperation.Result)
                    {
                        if (absoluteScriptPath.StartsWith(packageInfo.resolvedPath))
                        {
                            // The script's absolute path will be pointing into
                            // a folder that is outside the project folder. To
                            // access its MonoImporter, we need a path that
                            // points into the project's Packages folder

                            // Get the package-relative path to the script.
                            // E.g., "Runtime/Scripts/MyScript.cs"
                            string packageRelativeScriptPath =
                                absoluteScriptPath.Substring(packageInfo.resolvedPath.Length + 1);

                            // Get the project-relative path to the package.
                            // E.g., "Packages/ca.melcher.unity.core"
                            string projectRelativePackagePath = packageInfo.assetPath;

                            // Remake the absolute path, but pointing into the
                            // project's Packages folder for the script
                            absoluteScriptPath =
                                Path.Combine(
                                    MelcherPredefinedFilePath.project,
                                    projectRelativePackagePath,
                                    packageRelativeScriptPath
                                );

                            break;
                        }
                    }
                }

                string projectRelativeScriptPath =
                    MelcherTransformFilePath.From(absoluteScriptPath)
                        .To(MelcherPredefinedFilePath.project);

                PackageInfo packageInfoForScript =
                    PackageInfo.FindForAssetPath(projectRelativeScriptPath);

                if (packageInfoForScript != null &&
                    packageInfoForScript.source != PackageSource.Local &&
                    packageInfoForScript.source != PackageSource.Embedded)
                {
                    // The asset belongs to a ready-only package
                    yield break;
                }

                // PackageInfo.FindForAssetPath doesn't work with Library files
                // (tested with Unity 2020.3.40f1). AssetImporter.GetAtPath also
                // doesn't work with Library files. We break out of the function
                // if Library files are detected. This avoids logging a message
                // about missing files to the console. Library files are
                // read-only.

                if (projectRelativeScriptPath.StartsWith("Library/") ||
                    projectRelativeScriptPath.StartsWith("Library\\"))
                {
                    yield break;
                }

                // Get the importer object of the script from its dataRelativePath
                MonoImporter monoImporter =
                    (MonoImporter)MonoImporter.GetAtPath(projectRelativeScriptPath);

                if (monoImporter == null)
                {
                    MelcherLog.LogError(
                        $"Can't set execution order of {MelcherFormat.ConsoleTypeName(scriptName)}. MonoScript not found: {projectRelativeScriptPath}"
                    );
                    yield break;
                }

                // Change the script's execution order
                MonoScript monoScript = monoImporter.GetScript();

                if (MonoImporter.GetExecutionOrder(monoScript) != executionOrderOffset)
                {
                    MonoImporter.SetExecutionOrder(monoScript, executionOrderOffset);

                    if (MonoImporter.GetExecutionOrder(monoScript) == executionOrderOffset)
                    {
                        MelcherLog.Log(
                            $"Changed execution order of {MelcherFormat.ConsoleTypeName(scriptName)} to {MelcherFormat.ConsoleNumericValue(executionOrderOffset)}"
                        );
                    }
                    else
                    {
                        MelcherLog.LogError(
                            $"Could not set execution order of {MelcherFormat.ConsoleTypeName(scriptName)} to {MelcherFormat.ConsoleNumericValue(executionOrderOffset)}. Are the files read-only?"
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve the real backing object of a serialized property. I.e.,
        /// a managed object, an element in an array, etc.
        /// </summary>
        /// <param name="fieldInfoFromProperty">PropertyDrawer's fieldInfo</param>
        /// <param name="property">PropertyDrawer's OnGUI SerializedProperty</param>
        /// <param name="arrayIndex">Output array index. If the property is an
        ///                          array, this will be 0 or greater.
        /// </param>
        /// <typeparam name="ClassType">The type of the underlying object.</typeparam>
        /// <returns></returns>
        /// <remarks>
        /// Implementation based on:
        /// http://sketchyventures.com/2015/08/07/unity-tip-getting-the-actual-object-from-a-custom-property-drawer/
        /// https://archive.ph/dU48X
        /// </remarks>
        public static ClassType GetActualObjectForSerializedProperty<ClassType>(
            FieldInfo           fieldInfoFromProperty,
            SerializedProperty  property,
            out int             arrayIndex)
            where ClassType : class
        {
            // Get the field's value (could be an array/list)
            ClassType targetObject = (ClassType)fieldInfoFromProperty.GetValue(
                property.serializedObject.targetObject
            );

            arrayIndex = -1;

            // Translate from an array/list to the list element. Use the
            // property's dataRelativePath to lookup the element index
            if (targetObject is System.Collections.IList targetList)
            {
                Match arraySuffix = Regex.Match(property.propertyPath, @"\[(\d+)\]$");
                if (arraySuffix != null && int.TryParse(arraySuffix.Groups[1].Value, out arrayIndex))
                {
                    targetObject = (ClassType)targetList[arrayIndex];
                }
                else
                {
                    targetObject = null;
                }
            }

            return targetObject;
        }

        /// <inheritdoc cref="AssertClassStaticFieldsAreResetForPlayModeOptimization(System.Type, int, string, string)"/>
        public static void AssertClassStaticFieldsAreResetForDomainNonReload<CheckStaticFieldsOfType>(
                                int     staticFieldResetCount,
                                string  tag                 = null,
            [CallerFilePath]    string  callerFilePath      = null,
            [CallerMemberName]  string  callerMemberName    = null)
        {
            AssertClassStaticFieldsAreResetForDomainNonReload(
                checkStaticFieldsOfType:    typeof(CheckStaticFieldsOfType),
                staticFieldResetCount:      staticFieldResetCount,
                tag:                        tag,
                callerFilePath:             callerFilePath,
                callerMemberName:           callerMemberName
            );
        }

        /// <summary>
        ///     For play-mode optimization to work correctly, static fields must
        ///     be manually reset before play-mode begins. Unity recommends that
        ///     the user do this resetting during the <see
        ///     cref="RuntimeInitializeLoadType.SubsystemRegistration"/> phase
        ///     of initialization.
        ///     <br /><br />
        ///
        ///     This function is intended to help guarantee that *all* static
        ///     fields are reset. It takes a parameter that is the number of
        ///     static fields the user has reset and compares that to the number
        ///     of static fields detected in the class through reflection. If
        ///     they differ, an assert is printed to call attention to the
        ///     possible oversight.
        ///     <br /><br />
        ///
        ///     This function only compares the number of fields reset to the
        ///     number of static fields directly declared by the class. I.e.,
        ///     it ignores static fields declared by the type's base or child
        ///     classes.
        /// </summary>
        public static void AssertClassStaticFieldsAreResetForDomainNonReload(
                                System.Type checkStaticFieldsOfType,
                                int         staticFieldResetCount,
                                string      tag                 = null,
            [CallerFilePath]    string      callerFilePath      = null,
            [CallerMemberName]  string      callerMemberName    = null)
        {
            const BindingFlags LocalStaticFields =
                BindingFlags.DeclaredOnly | // only things declared directly by the type
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            int expectedStaticFieldResetCount = 0;

            s_StaticFieldsToReset.Clear();

            foreach (FieldInfo field in checkStaticFieldsOfType.GetFields(LocalStaticFields))
            {
                if (field.IsLiteral)
                {
                    // Constants do not need to be reset so ignore them.
                    continue;
                }

                if (checkStaticFieldsOfType.GetEvent(field.Name, LocalStaticFields) != null)
                {
                    // This is a backing field for an event. It is ignored
                    // as a field that needs to be reset because the backing
                    // field isn't exposed (plus, our attribute only works on
                    // fields and not events).

                    // TODO: Should we allow [MelcherResetStaticFieldInfo] to
                    //       work on events?

                    continue;
                }

                // Check for a StaticFieldInfo attribute. If it exists, it may
                // instruct for this field to be ignored.

                MelcherStaticFieldEditorTreatment.BaseStaticFieldAttribute staticFieldInfo =
                    field.GetCustomAttribute<MelcherStaticFieldEditorTreatment.BaseStaticFieldAttribute>();

                if (staticFieldInfo != null)
                {
                    // Skip counting this field because it is ignored
                    // or handled automatically.
                    continue;
                }

                ++expectedStaticFieldResetCount;

                s_StaticFieldsToReset.Add(field);
            }

            MelcherLog.Assert(
                staticFieldResetCount == expectedStaticFieldResetCount,
                $"Remember to reset all of {MelcherFormat.ConsoleTypeName(checkStaticFieldsOfType, true)}'s static " +
                $"fields in {MelcherFormat.ConsoleMethodName(callerMemberName)}. " +
                $"Reset {MelcherFormat.ConsoleNumericValue(staticFieldResetCount)} of " +
                $"{MelcherFormat.ConsoleNumericValue(expectedStaticFieldResetCount)}.\n\n" +
                $"Fields expected to be reset:\n    " +
                string.Join("\n    ", s_StaticFieldsToReset.Select(x => $"{x.Name} ({MelcherFormat.ConsoleTypeName(x.FieldType)})")) + "\n" +
                $"" // Blank line to separate the field list from the callstack
            );
        }

        [MelcherStaticFieldEditorTreatment.ClearCollection]
        private static List<FieldInfo> s_StaticFieldsToReset = new List<FieldInfo>();

        // =====================================================================
        // Application identifier
        // =====================================================================

        /// <summary>
        /// Get the active application identifier.
        /// </summary>
        public static string GetApplicationIdentifier(NamedBuildTarget? buildTarget = null)
        {
            return
                PlayerSettings.GetApplicationIdentifier(
                    buildTarget.GetValueOrDefault(
                        NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                    )
                );
        }

        public static void SetAllApplicationIdentifiers(string identifier)
        {
            // Validate the identifier because Unity throws non-obvious error
            // messages during a build when the identifier is invalid.

            const string ValidIdentifierPattern =
                @"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+[0-9a-z_]$";

            if (!Regex.IsMatch(identifier, ValidIdentifierPattern, RegexOptions.IgnoreCase))
            {
                throw
                    new ArgumentException(
                        $"Not a valid identifier: {identifier}",
                        nameof(identifier)
                    );
            }

            // Apply our custom package name to all platforms
            ReadOnlyCollection<BuildTargetGroup> allBuildTargets =
                MelcherEnumInfo<BuildTargetGroup>.values;

            foreach (BuildTargetGroup buildTarget in allBuildTargets)
            {
                // Skip unknown target
                if (buildTarget == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                try
                {
                    PlayerSettings.SetApplicationIdentifier(
                        NamedBuildTarget.FromBuildTargetGroup(buildTarget),
                        identifier
                    );
                }
                catch (ArgumentException)
                {
                    // Some platforms do not have identifiers.
                }
            }
        }

        /// <summary>
        /// Set whether to use the user-provided application identifier.
        /// </summary>
        /// <param name="useOverride">
        /// Pass <see langword="true"/> to use the user-provided app identifier;
        /// otherwise, pass <see langword="false"/> to use Unity's auto-created
        /// identifier.
        /// </param>
        /// <returns>
        /// Return <see langword="true"/> if the function succeeded; otherwise,
        /// return <see langword="false"/>.
        /// </returns>
        public static bool SetOverrideDefaultApplicationIdentifier(bool useOverride)
        {
            return FindOverrideDefaultApplicationIdentifier(
                (property) => property.boolValue = useOverride
            );
        }

        /// <summary>
        /// Get whether the user-provided application identifier is in-use.
        /// </summary>
        /// <returns>
        /// Return <see langword="true"/> if the user-provided app identifier is
        /// in-use. Return <see langword="false"/> if the auto-generated app
        /// identifier is in-use. Return <see langword="null"/> if it could not
        /// be determined.
        /// </returns>
        public static bool? GetOverrideDefaultApplicationIdentifier()
        {
            bool? useOverride = null;

            FindOverrideDefaultApplicationIdentifier(
                (property) => useOverride = property.boolValue
            );

            return useOverride;
        }

        /// <summary>
        /// Unity added a system to automatically name app's identifier but
        /// failed to expose a programmatic way to disable it. This function
        /// is a workaround for that oversight.
        /// </summary>
        private static bool FindOverrideDefaultApplicationIdentifier(System.Action<SerializedProperty> onFound)
        {
            PlayerSettings[] playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            foreach (PlayerSettings settings in playerSettings)
            {
                using SerializedObject serializedSettings = new SerializedObject(settings);

                SerializedProperty overrideDefaultApplicationIdentifier =
                    serializedSettings.FindProperty("overrideDefaultApplicationIdentifier");

                if (overrideDefaultApplicationIdentifier == null)
                {
                    MelcherLog.LogWarning("No override property for " + settings, settings);
                    continue;
                }

                onFound?.Invoke(overrideDefaultApplicationIdentifier);

                serializedSettings.ApplyModifiedProperties();
                serializedSettings.Update();

                return true;
            }

            return false;
        }

        // =====================================================================
        // Global Event Handler
        // =====================================================================

        /// <summary>
        ///     Access to <c>UnityEditor.EditorApplication.globalEventHandler</c>.
        ///     This can be used to implement global hotkeys in the editor.
        /// </summary>
        /// <remarks>
        ///     This is implemented using reflection and is based on this comment:
        ///     https://forum.unity.com/threads/4-6-editorapplication-modifierkeyschanged-how-to-find-out-which-key-was-pressed.357367/#post-2705846
        /// </remarks>
        public static event EditorApplication.CallbackFunction globalEventHandler
        {
            add => UseEditorApplicationGlobalEventHandler(eventHandler => eventHandler += value);
            remove => UseEditorApplicationGlobalEventHandler(eventHandler => eventHandler -= value);
        }

        private static void UseEditorApplicationGlobalEventHandler(Func<EditorApplication.CallbackFunction, EditorApplication.CallbackFunction> eventHandlerUser)
        {
            if (s_EditorApplication_GlobalEventHandler == null)
            {
                // Find and cache the non-public global event handler field
                s_EditorApplication_GlobalEventHandler =
                    typeof(EditorApplication).GetField(
                        "globalEventHandler",
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
            }

            // Fetch the value
            EditorApplication.CallbackFunction globalEventHandler =
                (EditorApplication.CallbackFunction)s_EditorApplication_GlobalEventHandler.GetValue(null);

            // Let the caller use it
            globalEventHandler = eventHandlerUser.Invoke(globalEventHandler);

            // Reassign the value
            s_EditorApplication_GlobalEventHandler.SetValue(null, globalEventHandler);
        }

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static FieldInfo s_EditorApplication_GlobalEventHandler;

        // ====================================================================
        // Editor Application Focus event
        // ====================================================================

        /// <summary>
        ///     Access to <c>UnityEditor.EditorApplication.focusChanged</c>.
        ///     This can be used to listen for the editor gaining and losing
        ///     focus.
        /// </summary>
        /// <remarks>
        ///     This is implemented using reflection and is based on this comment:
        ///     http://answers.unity.com/answers/1886639/view.html
        /// </remarks>
        public static event Action<bool> onUnityEditorFocusChanged
        {
            add => GetEditorApplicationFocusChangedEvent(focusChanged => focusChanged += value);
            remove => GetEditorApplicationFocusChangedEvent(focusChanged => focusChanged -= value);
        }

        private static void GetEditorApplicationFocusChangedEvent(Func<Action<bool>, Action<bool>> focusChangedUser)
        {
            if (s_EditorApplication_FocusChanged == null)
            {
                s_EditorApplication_FocusChanged =
                    typeof(EditorApplication).GetField(
                        "focusChanged",
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
            }

            // Fetch the value
            Action<bool> focusChanged =
                (Action<bool>)s_EditorApplication_FocusChanged.GetValue(null);

            // Let the caller use it
            focusChanged = focusChangedUser.Invoke(focusChanged);

            // Reassign it
            s_EditorApplication_FocusChanged.SetValue(null, focusChanged);

        }

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static FieldInfo s_EditorApplication_FocusChanged;

        // ====================================================================
        // Prefabs
        // ====================================================================

        /// <summary>
        ///     Check if a GameObject is part of the prefab editor. This is
        ///     useful to accurately determine whether a game object belongs to
        ///     a prefab asset. Unity's built-in <see
        ///     cref="PrefabUtility.IsPartOfPrefabAsset(UnityEngine.Object)"/>
        ///     unexpectedly returns <see langword="false"/> when a prefab
        ///     asset's game object is part of the prefab stage. This method
        ///     returns the expected <see langword="true"/> for game objects in
        ///     the prefab stage.
        /// </summary>
        public static bool IsPartOfCurrentPrefabStage(GameObject gameObject)
        {
            PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            return currentPrefabStage != null && currentPrefabStage == PrefabStageUtility.GetPrefabStage(gameObject);
        }

        // ====================================================================
        // Uncategorized
        // ====================================================================

        /// <summary>
        ///     Provides a way to call <c>typeof()</c> in a static context.
        ///     This is limited to the editor because it relies on analyzing
        ///     the stack trace with does not work in release builds.
        /// </summary>
        // Based on the implementation by `T-moty` here:
        // https://stackoverflow.com/a/18851863
        public static Type GetTypeFromStackTrace()
        {
            System.Diagnostics.StackTrace stackTrace = new ();

            if (stackTrace.FrameCount < 2)
            {
                return null;
            }

            MethodInfo previousMethod =
                stackTrace.GetFrame(1).GetMethod() as MethodInfo;

            return previousMethod.DeclaringType;
        }

    }   // class MelcherEditorUtility

}   // namespace Melcher
#endif

