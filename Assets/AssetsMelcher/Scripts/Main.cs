namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.Globalization;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    /// <summary>
    /// Pro-Fit app startup. This initializes systems all systems that are
    /// expected by scenes.
    /// </summary>
    internal static class Main
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInit()
        {
            // Ensure floating point numbers are always formatted with a period
            // to separate the decimal part.
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            bool isDebugMode = Application.isEditor || Debug.isDebugBuild;
            bool isVerbose = isDebugMode || MelcherWebGL.GetUrlParameter("verbose") != null;

            if (!isVerbose && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                string[] arguments = System.Environment.GetCommandLineArgs();

                int indexOfVerboseArg =
                    System.Array.FindIndex(
                        arguments,
                        x => "--verbose".Equals(x, System.StringComparison.OrdinalIgnoreCase)
                    );

                isVerbose |= indexOfVerboseArg >= 0;
            }

            MelcherLog.Log(
                $"{nameof(Main)}.{nameof(RuntimeInit)}:isDebugMode={isDebugMode} isVerbose={isVerbose}",
                severity: MelcherLogSeverity.MaxValue
            );

            MelcherLog.minimumSeverityToDisplay = isVerbose ? MelcherLogSeverity.Verbose : MelcherLogSeverity.Warning;
            MelcherWebRequestBuilder.runtimeDebug = isVerbose;
            ProFitGlobals.isVerbose = isVerbose;

            // Ensure that first Quality Level is chosen (i.e. not Render quality)
            if (QualitySettings.GetQualityLevel() != 0)
            {
                QualitySettings.SetQualityLevel(0);
            }
            MelcherLog.LogEditorOnly($"Using quality setting: {QualitySettings.names[0]}");

            MelcherLog.LogEditorOnly("Disabling debug render UI");
            UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;

            if (Application.isPlaying)
            {
                DOTweenInit.Init(isDebugMode);

#if UNITY_EDITOR
                // When in the editor, only initialize the global data for
                // scenes that are actually in the build settings. This allows
                // for faster iteration times in test scenes.
                // TODO: Move ProFitGlobals to a MelcherGlobalMonoBehaviour
                //       component so scenes can request global data or not and
                //       we don't need to use tricks like this. -Ryan
                if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().buildIndex >= 0)
#endif  // if UNITY_EDITOR
                    ProFitGlobals.Init();

                //PerformanceIMGUI.CreatePersistentInstance();

                // Target 60 frames per second
                //QualitySettings.vSyncCount = 1;
                //Application.targetFrameRate = 60;

#if UNITY_EDITOR
                MEC.Timing.RunCoroutine(DebugUpdateLoop());

                IEnumerator<float> DebugUpdateLoop()
                {
                    while (true)
                    {
                        if (Input.GetKey(KeyCode.F1))
                        {
                            ProFitGlobals.KickUser("Kicked by debug hotkey.");
                        }

                        yield return MEC.Timing.WaitForOneFrame;
                    }
                }
#endif // if UNITY_EDITOR
            }
        }

    }   // class Main

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-08
/// ===========================================================================
