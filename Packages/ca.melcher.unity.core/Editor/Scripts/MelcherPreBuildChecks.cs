namespace Melcher
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.IMGUI.Controls;

    using JsonObject = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// </summary>
    public class MelcherPreBuildIssuesWindow : EditorWindow
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public static WindowResult PromptForFixes(List<MelcherPreBuildCheckErrorInfo> errors)
        {
            s_WindowResult = WindowResult.Cancel;

            MelcherPreBuildIssuesWindow window = ScriptableObject.CreateInstance<MelcherPreBuildIssuesWindow>();
            window.titleContent = new GUIContent("Melcher Pre-Build Issues");
            window.m_Errors = errors;
            window.ShowModalUtility();

            return s_WindowResult;
        }

        private void OnEnable()
        {
            void ClearOffset(RectOffset toClear)
            {
                toClear.left = 0;
                toClear.right = 0;
                toClear.top = 0;
                toClear.bottom = 0;
            }

            if (s_EvenEntryStyle == null)
            {
                s_EvenEntryStyle = new GUIStyle(TreeView<int>.DefaultStyles.backgroundEven);
                ClearOffset(s_EvenEntryStyle.padding);
            }

            if (s_OddEntryStyle == null)
            {
                s_OddEntryStyle = new GUIStyle(TreeView<int>.DefaultStyles.backgroundOdd);
                ClearOffset(s_OddEntryStyle.padding);
            }

            if (s_SelectedEntryStyle == null)
            {
                s_SelectedEntryStyle = new GUIStyle(EditorStyles.linkLabel);
                s_SelectedEntryStyle.fontStyle = FontStyle.Bold;
                ClearOffset(s_SelectedEntryStyle.padding);
            }

            if (s_UnselectedEntryStyle == null)
            {
                s_UnselectedEntryStyle = new GUIStyle(EditorStyles.label);
                ClearOffset(s_UnselectedEntryStyle.padding);
            }

            if (s_ErrorIcon == null)
            {
                s_ErrorIcon = EditorGUIUtility.FindTexture("console.erroricon.sml");
            }

            if (s_WarningIcon == null)
            {
                s_WarningIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");
            }

            if (m_Errors == null)
            {
                m_Errors = new List<MelcherPreBuildCheckErrorInfo>();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Pre-Build Issues:", EditorStyles.boldLabel);

            using (var errorList = new GUILayout.ScrollViewScope(m_ErrorListPosition))
            {
                m_ErrorListPosition = errorList.scrollPosition;

                using (var vertical = new GUILayout.VerticalScope())
                {
                    int errorCount = m_Errors != null ? m_Errors.Count : 0;
                    for (int i = 0; i < errorCount; ++i)
                    {
                        bool isEven = i % 2 == 0;
                        GUILayout.BeginHorizontal(isEven ? s_EvenEntryStyle : s_OddEntryStyle);
                        {
                            bool isSelected = i == m_SelectedErrorIndex;
                            var icon = m_Errors[i].isFatal ? s_ErrorIcon : s_WarningIcon;
                            GUIContent cont = new GUIContent(m_Errors[i].error, icon);
                            if (GUILayout.Button(cont, isSelected ? s_SelectedEntryStyle : s_UnselectedEntryStyle))
                            {
                                m_ErrorDescriptionPosition = Vector2.zero;
                                if (m_SelectedErrorIndex == i)
                                {
                                    const double DoubleClickThresholdSeconds = 0.3;
                                    if ((EditorApplication.timeSinceStartup - m_LastErrorClickTime) < DoubleClickThresholdSeconds)
                                    {
                                        if (m_Errors[i].fix != null)
                                        {
                                            m_Errors[i].fix();
                                            EditorApplication.delayCall += () => m_Errors.RemoveAt(i);
                                            EditorApplication.delayCall += Repaint;
                                        }
                                    }
                                }
                                else
                                {
                                    m_SelectedErrorIndex = i;
                                    EditorApplication.delayCall += Repaint;
                                }

                                m_LastErrorClickTime = EditorApplication.timeSinceStartup;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }

            using (var errorDescription = new GUILayout.ScrollViewScope(m_ErrorDescriptionPosition, GUILayout.MinHeight(40.0f)))
            {
                m_ErrorDescriptionPosition = errorDescription.scrollPosition;

                MelcherPreBuildCheckErrorInfo error;
                error.optionsLocation = null;
                if (m_SelectedErrorIndex >= 0 && m_SelectedErrorIndex < m_Errors.Count)
                {
                    error = m_Errors[m_SelectedErrorIndex];
                }

                bool wasEnabled = GUI.enabled;
                GUI.enabled = false;
                GUILayout.TextArea(error.optionsLocation, GUILayout.ExpandHeight(true));
                GUI.enabled = wasEnabled;
            }

            using (var horizontal = new GUILayout.HorizontalScope())
            {
                GUI.enabled = m_SelectedErrorIndex >= 0 &&
                    m_SelectedErrorIndex < m_Errors.Count &&
                    m_Errors[m_SelectedErrorIndex].fix != null;

                if (GUILayout.Button("Fix"))
                {
                    m_Errors[m_SelectedErrorIndex].fix();
                }

                GUI.enabled = true;

                GUILayout.Space(32.0f);

                if (GUILayout.Button("Ignore All"))
                {
                    s_WindowResult = WindowResult.Continue;
                    Close();
                }

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
            }
        }

        // ====================================================================
        // Static variables
        // ====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_EvenEntryStyle;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_OddEntryStyle;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_SelectedEntryStyle;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_UnselectedEntryStyle;


        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static Texture2D s_WarningIcon;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static Texture2D s_ErrorIcon;


        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static WindowResult s_WindowResult;

        // ====================================================================
        // Serialized members
        // ====================================================================

        [SerializeField] private Vector2 m_ErrorListPosition;
        [SerializeField] private Vector2 m_ErrorDescriptionPosition;

        [SerializeField] private List<MelcherPreBuildCheckErrorInfo> m_Errors;

        [SerializeField] private int m_SelectedErrorIndex;

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        private double m_LastErrorClickTime;

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum WindowResult
        {
            Cancel,
            Continue,
        }

    }   // class MelcherPreBuildIssuesWindow

    /// <summary>
    /// Run checks on the project before it is built to prevent common
    /// configuration issues.
    /// </summary>
    /// <remarks>
    /// Build interruption based on Vandarthul's suggestion here:
    /// https://forum.unity.com/threads/ipreprocessbuild-editor-script-how-to-stop-a-build-that-fails-validation.483578/#post-4421074
    /// </remarks>
    [InitializeOnLoad]
    public static class MelcherPreBuildChecks
    {
        static MelcherPreBuildChecks()
        {
            // Fixed?
            // // On my linux machine, ShowModalUtility doesn't block. This breaks
            // // our build pre checker. TODO: Manually implement blocking using
            // // editor coroutines?
            // if (Application.platform == RuntimePlatform.LinuxEditor)
            // {
            //     return;
            // }

            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuild);
        }

        public static bool IsReleaseBuild(BuildPlayerOptions buildOptions)
        {
            return (buildOptions.options & BuildOptions.Development) == 0;
        }

        public static void OnBuildFromExternalScript(BuildPlayerOptions buildOptions)
        {
            OnBuild(buildOptions);
        }

        private static void OnBuild(BuildPlayerOptions buildOptions)
        {
            bool doBuild = true;

            List<MelcherPreBuildCheckErrorInfo> errors = new List<MelcherPreBuildCheckErrorInfo>();

            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            switch (buildTargetGroup)
            {
                case BuildTargetGroup.Android:
                    CheckAndroidBuild(buildOptions, errors);
                    break;
            }

            foreach (var error in errors)
            {
                if (error.isFatal)
                {
                    MelcherLog.LogError("Build error: " + error.error + "\n\n" + error.optionsLocation);
                    doBuild = false;
                }
                else
                {
                    MelcherLog.LogWarning("Build warning: " + error.error + "\n\n" + error.optionsLocation);
                }
            }

            // If there are pre-build errors, allow them to be fixed or ignored
            if (!doBuild)
            {
                doBuild = MelcherPreBuildIssuesWindow.PromptForFixes(errors) == MelcherPreBuildIssuesWindow.WindowResult.Continue;
            }

            // After pre-build errors are handled, handle version changing
            if (doBuild && MelcherEasyVersioning.ShowPromptBeforeBuild())
            {
                doBuild = MelcherEasyVersioning.PromptForVersionChange();
            }

            if (doBuild)
            {
                MelcherBuildNameOptions.RenameOutput(ref buildOptions);

                MelcherLog.Log(System.DateTime.Now.ToString() + " :: Started build for platform " + buildTargetGroup + "\n  Output path: " + buildOptions.locationPathName);
                BuildPipeline.BuildPlayer(buildOptions);
            }
            else
            {
                throw new BuildPlayerWindow.BuildMethodException();
            }
        }

        public static void CheckAndroidBuild(BuildPlayerOptions buildOptions, List<MelcherPreBuildCheckErrorInfo> errors)
        {
            bool isReleaseBuild = IsReleaseBuild(buildOptions);

            bool isXrProject = false;

            // Check if the Oculus XR package is installed in the project and,
            // if it is, assume the app is intended for the Quest.
            //
            // It would be nice if we could check for the package via the
            // editor PackageManager API, but there doesn't seem to be a way to
            // determine if the package is installed.
            //
            // We parse the manifest file ourself. This is a simple and
            // reliable solution.

            string packageManifestPath =
                MelcherTransformFilePath.From(
                    path: "Packages/manifest.json",
                    relativeTo: MelcherPredefinedFilePath.project
                ).ToAbsolute();

            if (!File.Exists(packageManifestPath))
            {
                MelcherLog.LogError($"Could not find package manifest. Assuming the app is *not* built for Quest. Manifest path: " +  packageManifestPath);
            }
            else
            {
                var cancellationTokenSource =
                    new System.Threading.CancellationTokenSource();

                var readManifestTask =
                    File.ReadAllTextAsync(
                        packageManifestPath,
                        cancellationTokenSource.Token
                    );

                // Allow the process of reading the package manifest to be
                // cancelled, just because you never know.

                double readManifestStartTime = EditorApplication.timeSinceStartup;

                using var readManifestProgresHider =
                    MelcherDeferred.Defer(
                        static () => EditorUtility.ClearProgressBar()
                    );

                while (!readManifestTask.IsCompleted)
                {
                    // Simulate the progress bar because we can't know how long
                    // it will take.

                    // The number of times to simulate the progress bar filling
                    // per second.
                    const float ProgressBarFillFrequency = 2.0f;

                    float simulatedProgress =
                        Mathf.Repeat(
                            (float)(EditorApplication.timeSinceStartup - readManifestStartTime) * ProgressBarFillFrequency,
                            length: 1.0f
                        );

                    bool isReadManifestCancelled =
                        EditorUtility.DisplayCancelableProgressBar(
                            title: "Checking XR project",
                            info: "Reading: " + packageManifestPath,
                            progress: simulatedProgress
                        );

                    if (isReadManifestCancelled)
                    {
                        cancellationTokenSource.Cancel();
                    }
                }

                if (readManifestTask.IsCompletedSuccessfully)
                {
                    JsonObject deserializedJson =
                        (JsonObject)MiniJSON.Json.Deserialize(readManifestTask.Result);

                    MelcherJsonObjectParser jsonParser =
                        new MelcherJsonObjectParser(deserializedJson);

                    jsonParser
                        .GetAndPushObject("dependencies")
                            .GetValue("com.unity.xr.management", out string xrManagerVersion, defaultValue: null);

                    if (!jsonParser.hasErrors)
                    {
                        isXrProject = !string.IsNullOrEmpty(xrManagerVersion);
                    }
                }
            }

            // This marks the end of reading the manifest file.

            // Ensure a keystore was specified
            if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            {
                string description =
                    "A Keystore is required to sign your project when uploading to the PlayStore.\n" +
                    "\n" +
                    "See: Player Settings > Publishing Settings > Project Keystore > Custom Keystore";
                errors.Add(new MelcherPreBuildCheckErrorInfo(
                    "Android Keystore is not set",
                    description,
                    fix: null)
                );
            }
            else
            {
                // If a keystore is provided, the user must provide the
                // keystore password and keyalias details.

                if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass))
                {
                    string description =
                        "The Keystore password must be set.\n" +
                        "\n" +
                        "See: Player Settings > Publishing Settings > Project Keystore > Password";

                    errors.Add(
                        new MelcherPreBuildCheckErrorInfo(
                            "Android Keystore password is not set",
                            description,
                            fix: null
                        )
                    );
                }

                if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName) ||
                    string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                {
                    string description =
                        "The Keystore alias name and password must be set.\n" +
                        "\n" +
                        "See: Player Settings > Publishing Settings > Project Key";

                    errors.Add(
                        new MelcherPreBuildCheckErrorInfo(
                            "Android Keystore alias name or password is not set",
                            description,
                            fix: null
                        )
                    );
                }
            }

            // Ensure IL2CPP is enabled
            NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            ScriptingImplementation scriptingBackend = PlayerSettings.GetScriptingBackend(buildTarget);

            if (scriptingBackend != ScriptingImplementation.IL2CPP)
            {
                errors.Add(
                    new MelcherPreBuildCheckErrorInfo(
                        "Scripting backend is not set to IL2CPP",
                        "Player Settings > Other Settings > Configuration > Scripting Backend",
                        fix: () => PlayerSettings.SetScriptingBackend(buildTarget, ScriptingImplementation.IL2CPP),
                        isFatal: isReleaseBuild
                    )
                );
            }

            // Ensure correct target architectures are selected
            AndroidArchitecture selectedArchitecture = PlayerSettings.Android.targetArchitectures;
            AndroidArchitecture expectedArchitecture = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            if (isXrProject)
            {
                // Don't need ARMv7 on Quest.
                expectedArchitecture &= ~AndroidArchitecture.ARMv7;
            }

            if (selectedArchitecture != expectedArchitecture)
            {
                string[] expectedNames =
                    MelcherEnumInfo<AndroidArchitecture>.GetNamesFromMask(expectedArchitecture);

                errors.Add(
                    new MelcherPreBuildCheckErrorInfo(
                        "Target Architectures should exactly match: " + string.Join(", ", expectedNames),
                        "Player Settings > Other Settings > Configuration > Target Architectures",
                        fix: () => PlayerSettings.Android.targetArchitectures = expectedArchitecture,
                        isFatal: isReleaseBuild
                    )
                );
            }

            // Warn when this is a release build that isn't marked as an
            // App Bundle. This is only necessary when uploading builds to
            // Google Play so it is ignored during Quest builds.
            bool isAppBundle = EditorUserBuildSettings.buildAppBundle;

            if (!isAppBundle && !isXrProject && isReleaseBuild)
            {
                errors.Add(
                    new MelcherPreBuildCheckErrorInfo(
                        $"If you're trying to upload a build to Google Play, please enable the Build App Bundle in the build settings. Ignore this if you're just deploying to a device, making an APK for side-loading, or building for Oculus.",
                        "Build Settings > Build App Bundle (Google Play)",
                        fix: () => EditorUserBuildSettings.buildAppBundle = true,
                        // This is never fatal because it's a common part of testing an app
                        isFatal: false
                    )
                );
            }

            // Ensure the target SDK matches the platforms minimum allowed
            // version.
            const int MinimumGooglePlaySdkVersion = 33;
            const int MinimumOculusSdkVersion = 32;

            int minimumSdkVersion =
                isXrProject ?
                    MinimumOculusSdkVersion :
                    MinimumGooglePlaySdkVersion;

            if ((int)PlayerSettings.Android.targetSdkVersion < minimumSdkVersion)
            {
                errors.Add(
                    new MelcherPreBuildCheckErrorInfo(
                        $"The Android target API level must be {minimumSdkVersion} or higher",
                        "Player Settings > Other Settings > Identification > Target API Level",
                        fix: () => PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)MinimumGooglePlaySdkVersion,
                        // This is only fatal if we're building an app bundle
                        isFatal: isReleaseBuild && (isAppBundle || isXrProject)
                    )
                );
            }
        }
    }   // class MelcherPreBuildChecks

    public struct MelcherPreBuildCheckErrorInfo
    {
        public MelcherPreBuildCheckErrorInfo(string error, string optionsLocation, System.Action fix, bool isFatal = true)
        {
            this.error = error;
            this.optionsLocation = optionsLocation;
            this.fix = fix;
            this.isFatal = isFatal;
        }

        /// <summary>
        /// The error message being reported to the user. This is typically
        /// says what needs to be fixed and why.
        /// </summary>
        public string error;

        /// <summary>
        /// A quasi-path to the setting if the user wants to fix the problem
        /// manually.
        /// <br /><br />
        ///
        /// E.g., Player Settings > Other Settings > Configuration > Target Architectures
        /// </summary>
        public string optionsLocation;

        /// <summary>
        /// If not null, this is a lambda that will fix the reported error
        /// automatically for the user.
        /// </summary>
        public System.Action fix;

        /// <summary>
        /// Whether this error should block a build from proceeding. For
        /// development builds, this is almost always false.
        /// </summary>
        public bool isFatal;

    }   // struct MelcherPreBuildCheckErrorInfo

}   // namespace Melcher

