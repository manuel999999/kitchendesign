namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;
    using MEC;

    /// <summary>
    ///     <see cref="PlatformSetupScene"/> handles loading the proper scene
    ///     according to whether the user is white labeled.
    /// </summary>
    public class PlatformSetupScene : MelcherStateMachineState
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public override void OnStateAdded(IMelcherStateMachineState fromState)
        {
            ProFitGlobals.ExecuteWhenReady(OnGlobalsReady);
        }

        private void OnEnable()
        {
            EventBridge.Menu.setSpinnerStatusMessage += OnSetSpinnerStatusMessage;
        }

        private void OnDisable()
        {
            EventBridge.Menu.setSpinnerStatusMessage -= OnSetSpinnerStatusMessage;
        }

        private void OnSetSpinnerStatusMessage(string text)
        {
            m_LoadingUi.SetStatusMessage(text);
        }

        private void OnGlobalsReady()
        {
            Timing.RunCoroutine(Coroutine());

            IEnumerator<float> Coroutine()
            {
                // The will-be User info if the user is white labeled
                User? pendingUser = null;

                // The will-be User Profile info the user is white labeled
                UserProfile? pendingUserProfile = null;

#if UNITY_WEBGL || UNITY_EDITOR

                // The intent of this coroutine is to correctly select the next
                // scene to load. We need to choose the designer scene for
                // white labeled WebGL users.

                // To make development easier, there is an option to simulate
                // white label mode in the editor set in ProFitAppOptions.

                string webDomainOfApp = ProFitUtility.GetWebDomainOfApp();

                MelcherLog.Log(
                    "webDomainOfApp: " + webDomainOfApp,
                    severity: MelcherLogSeverity.Verbose
                );

                string distributorUrlParameter =
                    ProFitUtility.GetDistributorUrlParameter();

                MelcherLog.Log(
                    "distributorUrlParameter: " + distributorUrlParameter,
                    severity: MelcherLogSeverity.Verbose
                );

                if (webDomainOfApp != null)
                {
                    // Check if the distributor is setup for white labeling.

                    MelcherWebCoroutine request =
                        ProFitWeb.CheckWhiteLabelAuthorization(
                            webDomainOfApp,
                            distributorUrlParameter,
                            onSuccess: newUser =>
                            {
                                pendingUser = newUser;

                                MelcherLog.Log("Has authorization", severity: MelcherLogSeverity.Verbose);
                            },
                            onError: (builder, error, jsonValue) =>
                            {
                                // "Unauthorized"
                                MelcherLog.LogError(error.Message);
                            }
                        );

                    yield return Timing.WaitUntilDone(request);
                }

#if UNITY_EDITOR
                if (Application.isEditor && pendingUser == null)
                {
                    // Bypass authorization by simulating offline mode

                    string fieldName =
                        $"{nameof(ProFitGlobals)}.{nameof(ProFitGlobals.appOptions)}";

                    MelcherLog.LogWarning(
                        $"The simulated white label options provided in " +
                        $"{MelcherFormat.ConsoleFieldName(fieldName)} are" +
                        $"out-of-date. White label mode will still be " +
                        $"simulated using an offline user.",
                        context: ProFitGlobals.appOptions
                    );

                    MelcherWebCoroutine request =
                        ProFitWeb.LogIn(
                            signInEmail: "",
                            password: "",
                            isOffline: true,
                            onSuccess: newUser =>
                            {
                                pendingUser = newUser;
                            },
                            onError: (builder, error, jsonValue) =>
                            {
                                MelcherLog.LogError("Failed to log in as offline user.");
                            }
                        );

                    yield return Timing.WaitUntilDone(request);
                }
#endif  // if UNITY_EDITOR


                if (pendingUser.HasValue)
                {
                    EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Requesting User Profile");

                    m_LoadingUi.Show();

                    MelcherWebCoroutine request =
                        ProFitWeb.GetProfile(
                            pendingUser.Value.apiToken,
                            isOffline: pendingUser.Value.userType == UserType.Offline,
                            onSuccess: newUserProfile =>
                            {
                                User updatedUser = pendingUser.Value;
                                updatedUser.userType = newUserProfile.userType;
                                pendingUser = updatedUser;

                                pendingUserProfile = newUserProfile;
                            },
                            onError: (builder, error, jsonValue) =>
                            {
                                MelcherLog.LogError(error.Message);
                            }
                        );

                    string userPref = "User/{pendingUser.Value.id}/isKioskModeEnabled";

                    ProFitGlobals.isKioskModeEnabled = PlayerPrefs.GetInt(userPref) != 0;

                    yield return Timing.WaitUntilDone(request);
                }

#if UNITY_EDITOR
                if (pendingUserProfile.HasValue &&
                    ProFitGlobals.appOptions.simulateWhiteLabelMode &&
                    pendingUserProfile.Value.isWhiteLabeled == false)
                {
                    MelcherLog.LogWarning(
                        $"Forcing white mode on as it is requested in the" +
                        $"app options.",
                        context: ProFitGlobals.appOptions
                    );

                    UserProfile profileCopy = pendingUserProfile.Value;
                    profileCopy.isWhiteLabeled = true;
                    pendingUserProfile = profileCopy;
                }
#endif  // if UNITY_EDITOR

#endif  // if UNITY_WEBGL || UNITY_EDITOR

                // Jump to the appropriate scene depending on whether the user
                // is in white label mode or not

                string sceneNameToLoad = "MenuScene";

                ProFitGlobals.useWhiteLabelMode =
                    pendingUser.HasValue &&
                    pendingUserProfile.HasValue &&
                    pendingUserProfile.Value.isActive &&
                    pendingUserProfile.Value.isWhiteLabeled;

                if (ProFitGlobals.useWhiteLabelMode)
                {
                    ProFitGlobals.user = pendingUser.Value;
                    ProFitGlobals.userProfile = pendingUserProfile.Value;

                    sceneNameToLoad = "DesignerScene";

                    yield return Timing.WaitUntilDone(
                        Timing.RunCoroutine(ProFitUtility.FinishLogInCoroutine(rememberMe: false, fetchClients: false, ErrorHandler))
                    );

                    EventBridge.Menu.setSpinnerStatusMessage?.Invoke("Loading 3D Designer");
                }

                MelcherLog.Log(
                    $"{nameof(PlatformSetupScene)} will load scene {sceneNameToLoad}",
                    severity: MelcherLogSeverity.Verbose
                );

                MelcherSceneRequest sceneRequest =
                    MelcherSceneRequest.FromName(sceneNameToLoad);

                MelcherSceneManager.LoadSceneAsync(sceneRequest, MelcherSceneLoadMode.Single);

                yield break;
            }

            static void ErrorHandler(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonResponse)
            {
                MelcherLog.LogError(error.Message);
            }

        }

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected LoadingUI m_LoadingUi = default;

    }   // class PlatformSetupScene

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-14
/// ===========================================================================
