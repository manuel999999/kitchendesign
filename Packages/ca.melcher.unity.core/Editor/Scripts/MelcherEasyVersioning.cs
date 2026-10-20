namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// An editor window for easily managing the project's version and bundle
    /// identifier uniformly across all platforms.
    /// </summary>
    public class MelcherEasyVersioning : EditorWindow
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public static bool PromptForVersionChange()
        {
            s_WasVersionSelected = false;
            DisplayModalBox();
            return s_WasVersionSelected;
        }

        [MenuItem(MelcherEditorConst.Menu.ToolsProjectSettingsPrefix + "Easy versioning... %&v")]
        public static void DisplayModalBox()
        {
            MelcherEasyVersioning window = ScriptableObject.CreateInstance<MelcherEasyVersioning>();
            window.titleContent = new GUIContent("Melcher Easy Versioning");
            window.OnEnable();
            window.ShowModalUtility();
        }

        public static bool ShowPromptBeforeBuild()
        {
            return EditorPrefs.GetBool(EditorPref.ShowPromptBeforeBuild, defaultValue: true);
        }

        private void OnEnable()
        {
            if (!System.Version.TryParse(MelcherVersion.version, out System.Version version))
            {
                version = new System.Version();
            }

            if (s_UseVersionFacadeToggleContent == null)
            {
                s_UseVersionFacadeToggleContent = new GUIContent(
                    text: "Use version facade (to support APK downgrading)",
                    tooltip: "When enabled, the requested version number is " +
                        "applied to MelcherVersion's facadeVersion instead of " +
                        "Application.version. This means that " +
                        "Application.version always stays the same which " +
                        "allows Android builds to be downgraded to older app " +
                        "versions."
                );
            }

            if (s_ShowPromptBeforeBuildToggleContent == null)
            {
                s_ShowPromptBeforeBuildToggleContent =
                    new GUIContent(
                        text: "Show this prompt before each build",
                        tooltip: "When enabled, this version dialog will " +
                            "be displayed before every build to provide " +
                            "an opportunity to bump the version number.\n" +
                            "\n" +
                            "This value is saved as an EditorPref and " +
                            "therefore applies to all projects on this " +
                            "machine."
                    );
            }

            m_CurrentVersion = new Version(version);
            m_CustomVersion = m_CurrentVersion;

            MelcherEditorUtility.SetOverrideDefaultApplicationIdentifier(true);

            // Get the custom package name from Android. Not all platforms have
            // an identifier and this works around the issue
            m_CustomPackageName =
                MelcherEditorUtility.GetApplicationIdentifier(
                    UnityEditor.Build.NamedBuildTarget.Android
                );
        }

        private void OnGUI()
        {
            const float SpaceBetweenSections = 16.0f;

            {
                using var rootScrollView =
                    new EditorGUILayout.ScrollViewScope(
                        m_RootScrollPosition,
                        alwaysShowHorizontal: false,
                        alwaysShowVertical: false,
                        horizontalScrollbar: GUIStyle.none,
                        verticalScrollbar: GUI.skin.verticalScrollbar,
                        background: GUI.skin.scrollView,
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true)
                    );
                m_RootScrollPosition = rootScrollView.scrollPosition;

                if (s_VersionButtonStyle == null)
                {
                    s_VersionButtonStyle = new GUIStyle(GUI.skin.button);
                    s_VersionButtonStyle.richText = true;
                    s_VersionButtonStyle.alignment = TextAnchor.MiddleCenter;
                }

                if (EditorApplication.timeSinceStartup > m_PackageNameMessageClearTime)
                {
                    m_PackageNameMessage = null;
                }

                GUILayout.Label("Melcher - Easy Versioning", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Choose one of the options below to easily update the project's version.", EditorStyles.wordWrappedLabel);

                GUILayout.Space(SpaceBetweenSections);

                GUILayout.Label("Keep version", EditorStyles.boldLabel);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("Continue without changing version");
                    if (GUILayout.Button($"Keep version {m_CurrentVersion}"))
                    {
                        s_WasVersionSelected = true;
                        Close();
                    }
                }

                GUILayout.Space(SpaceBetweenSections);

                GUILayout.Label("Increment Version", EditorStyles.boldLabel);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        using (new GUILayout.VerticalScope())
                        {
                            var nextMajorVersion = new Version(
                                m_CurrentVersion.major + 1,
                                0,
                                0
                            );
                            GUILayout.Label("Next Major");
                            VersionButton(nextMajorVersion, VersionEmphasis.Major);
                        }
                        using (new GUILayout.VerticalScope())
                        {
                            var nextMinorVersion = new Version(
                                m_CurrentVersion.major,
                                m_CurrentVersion.minor + 1,
                                0
                            );
                            GUILayout.Label("Next Minor");
                            VersionButton(nextMinorVersion, VersionEmphasis.Minor);
                        }
                        using (new GUILayout.VerticalScope())
                        {
                            var nextPatchVersion = new Version(
                                m_CurrentVersion.major,
                                m_CurrentVersion.minor,
                                m_CurrentVersion.patch + 1
                            );
                            GUILayout.Label("Next Patch");
                            VersionButton(nextPatchVersion, VersionEmphasis.Patch);
                        }
                    }
                }

                GUILayout.Space(SpaceBetweenSections);

                GUILayout.Label("Custom Version", EditorStyles.boldLabel);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Label("Major");
                            m_CustomVersion.major = EditorGUILayout.IntField(m_CustomVersion.major);
                        }

                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Label("Minor");
                            m_CustomVersion.minor = EditorGUILayout.IntField(m_CustomVersion.minor);
                        }

                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Label("Patch");
                            m_CustomVersion.patch = EditorGUILayout.IntField(m_CustomVersion.patch);
                        }
                    }

                    VersionButton(m_CustomVersion, VersionEmphasis.All);
                }

                GUILayout.Space(SpaceBetweenSections);

                GUILayout.Label("Package Name", EditorStyles.boldLabel);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("Change the package name across all build targets");

                    GUIStyle textFieldStyle = GUI.skin.textField;
                    GUIStyle helpBoxStyle = EditorStyles.helpBox;

                    float packageNameFieldPadding =
                        helpBoxStyle.padding.left + helpBoxStyle.padding.right +
                        helpBoxStyle.margin.left + helpBoxStyle.margin.right +
                        textFieldStyle.margin.left + textFieldStyle.margin.right;

                    m_CustomPackageName =
                        GUILayout.TextField(
                            m_CustomPackageName,
                            GUILayout.Width(Screen.width - packageNameFieldPadding)
                        );

                    if (GUILayout.Button("Set Identifier"))
                    {
                        // Apply our custom package name to all platforms
                        MelcherEditorUtility.SetAllApplicationIdentifiers(m_CustomPackageName);

                        m_PackageNameMessage = $"Changed identifier to: {m_CustomPackageName}";
                        m_PackageNameMessageClearTime = EditorApplication.timeSinceStartup + 3.0f;
                    }

                    GUILayout.Label(m_PackageNameMessage);
                }

                GUILayout.Space(SpaceBetweenSections);

                GUILayout.Label("Options", EditorStyles.boldLabel);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    // Checkbox for whether the Easy Versioning dialog should
                    // appear every time before the project is built.
                    using (var showPromptBeforeBuildChanged = new EditorGUI.ChangeCheckScope())
                    {
                        bool showPromptBeforeBuild = ShowPromptBeforeBuild();

                        showPromptBeforeBuild =
                            GUILayout.Toggle(
                                showPromptBeforeBuild,
                                s_ShowPromptBeforeBuildToggleContent
                            );

                        if (showPromptBeforeBuildChanged.changed)
                        {
                            EditorPrefs.SetBool(
                                EditorPref.ShowPromptBeforeBuild,
                                showPromptBeforeBuild
                            );
                        }
                    }

                    MelcherVersion.useFacade = GUILayout.Toggle(MelcherVersion.useFacade, s_UseVersionFacadeToggleContent);

                    GUILayout.Space(4.0f);

                    MelcherBuildNameOptions.DrawEmbeddedGUI();
                }

                GUILayout.Space(64.0f);
            }

            //GUILayout.Space(SpaceBetweenSections);

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }

        /// <summary>
        /// Apply an app version for all platforms.
        /// </summary>
        private void VersionButton(Version version, VersionEmphasis emphasis)
        {
            string emphasizedVersionString = version.ToStringWithEmphasis(emphasis);

            if (GUILayout.Button(emphasizedVersionString, s_VersionButtonStyle, System.Array.Empty<GUILayoutOption>()))
            {
                // Apply new version
                MelcherVersion.version = version.ToString();

                if (!MelcherVersion.useFacade)
                {
                    // Allow room for 3 digits in each category
                    int versionCode = MelcherUnityUtility.VersionCode(MelcherVersion.version);
                    PlayerSettings.Android.bundleVersionCode = versionCode;
                    PlayerSettings.iOS.buildNumber = version.patch.ToString();
                }

                MelcherLog.Log($"Updated version number from {m_CurrentVersion} to {version}");
                s_WasVersionSelected = true;

                Close();
            }
        }

        // ====================================================================
        // Static variables
        // ====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static bool s_WasVersionSelected;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_VersionButtonStyle;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_UseVersionFacadeToggleContent;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_ShowPromptBeforeBuildToggleContent;

        // ====================================================================
        // Member variables
        // ====================================================================

        private Version m_CurrentVersion;
        private Version m_CustomVersion;
        private string m_CustomPackageName;
        private string m_PackageNameMessage;
        private double m_PackageNameMessageClearTime;
        private Vector2 m_RootScrollPosition;
        private bool m_AutoNameBuild;

        // ====================================================================
        // Nested types
        // ====================================================================

        public struct Version
        {
            public int major;
            public int minor;
            public int patch;

            public Version(int major, int minor, int patch)
            {
                this.major = major;
                this.minor = minor;
                this.patch = patch;
            }

            public Version(System.Version version)
                : this(version.Major, version.Minor, version.Build)
            {
            }

            public override string ToString()
            {
                return $"{major}.{minor}.{patch}";
            }

            public string ToStringWithEmphasis(VersionEmphasis emphasis)
            {
                string Emphasize(int toEmphasize)
                {
                    // Brighter shade of green if using pro skin (dark theme)
                    string color = EditorGUIUtility.isProSkin ? "4AFE36" : "16D101";
                    //return $"<b><size=15><color=#{color}>{toEmphasize}</color></size></b>";
                    return $"<b><size=14>{toEmphasize}</size></b>";
                }

                switch (emphasis)
                {
                    default:
                        return ToString();

                    case VersionEmphasis.Major:
                        return $"{Emphasize(major)}.{minor}.{patch}";

                    case VersionEmphasis.Minor:
                        return $"{major}.{Emphasize(minor)}.{patch}";

                    case VersionEmphasis.Patch:
                        return $"{major}.{minor}.{Emphasize(patch)}";

                    case VersionEmphasis.All:
                        return $"{Emphasize(major)}.{Emphasize(minor)}.{Emphasize(patch)}";
                }
            }
        }   // struct Version

        public enum VersionEmphasis
        {
            None,
            Major,
            Minor,
            Patch,
            All
        }   // enum VersionEmphasis

        public static class EditorPref
        {
            [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
            public static readonly string ShowPromptBeforeBuild = typeof(MelcherEasyVersioning).FullName + ".ShowPromptBeforeBuild";

        }   // static class EditorPref

    }   // class MelcherEasyVersioning

}   // namespace Melcher
