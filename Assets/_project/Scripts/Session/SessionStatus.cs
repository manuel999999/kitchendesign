using KitchenDesign.Server;
using System;
using System.Collections.Generic;
using KitchenDesign.CatalogData;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;
using MEC;
using Melcher;
using UnityEngine;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;

namespace KitchenDesign.Session
{
    public sealed class SessionStatus
    {
        public const string RememberMeTokenKey = "Token";

        private readonly AppProfile _appProfile;
        private readonly ClientListStatus _clientList;
        private readonly CatalogDataStatus _catalogData;
        private readonly ServerClient _server;
        private readonly IAppRules _appRules;

        private User _user;
        private bool _isKioskModeEnabled;
        private CoroutineHandle _autoKickUserCoroutine;

        public SessionStatus(AppProfile appProfile, IAppRules appRules, ServerClient server, ClientListStatus clientList, CatalogDataStatus catalogData)
        {
            this._appRules = appRules;
            this._server = server;
            this._appProfile = appProfile;
            this._clientList = clientList;
            this._catalogData = catalogData;
        }

        public LogInState LogInState { get; set; }

        public bool UseWhiteLabelMode { get; set; }

        public UserProfile UserProfile { get; set; }

        public bool IsKioskModeEnabled
        {
            get => this._isKioskModeEnabled;
            set
            {
                this._isKioskModeEnabled = value;

                PlayerPrefs.SetInt(GetKioskModePlayerPrefKey(this._user), value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public User User
        {
            get => this._user;
            set
            {
                if (value.userType != this._user.userType)
                {
                    MelcherLog.LogEditorOnly("Changing user type to " + value.userType);
                }

                this._user = value;
                this._isKioskModeEnabled = PlayerPrefs.GetInt(GetKioskModePlayerPrefKey(value)) != 0;
            }
        }

        public void Initialize()
        {
            this.LogInState = LogInState.NotStarted;
            this.IsKioskModeEnabled = false;
            this.UseWhiteLabelMode = false;
            this.User = default;
            this.UserProfile = default;
        }

        public bool ShouldUseWhiteLabelMode()
        {
#if UNITY_EDITOR
            if (this._appProfile.appOptions.simulateWhiteLabelMode)
            {
                return true;
            }
#endif

            return this.UseWhiteLabelMode;
        }

        public string GetWhiteLabelAuthorizedDomain()
        {
#if UNITY_EDITOR
            if (this._appProfile.appOptions.simulateWhiteLabelMode)
            {
                return this._appProfile.appOptions.simulatedWhiteLabelDistributorUrlParameter;
            }
#endif

            return this.UserProfile.authorizedDomain;
        }

        public void ForgetRememberMeData()
        {
            MelcherLog.LogEditorOnly("Removing session token from PlayerPrefs");
            PlayerPrefs.DeleteKey(RememberMeTokenKey);
            PlayerPrefs.Save();
        }

        public void LogOut()
        {
            this.User = default;
            this.UserProfile = default;
            this._clientList.UserClients.Clear();
            this._catalogData.SourceProducts.Clear();
            this._catalogData.BaseProductIdToProductVariantIds.Clear();

            if (this.LogInState == LogInState.Finished)
            {
                this.LogInState = LogInState.NotStarted;
            }

            this.ForgetRememberMeData();
            this.StopAutoKickCoroutine();

            EventBridge.Menu.hideTrialCountdownUi?.Invoke();
            EventBridge.Designer.hideTrialCountdownUi?.Invoke();
        }

        public void KickUser(string kickReason)
        {
            this.LogOut();

            MelcherSceneRequest menuSceneRequest =
                MelcherSceneRequest.FromName("MenuScene");

            if (!MelcherSceneManager.TryGetScene(menuSceneRequest, out var _))
            {
                MelcherLog.Log("Menu isn't loaded");

                EventBridge.Designer.loadMenuScene.Invoke(
                    onLoaded: (_, _) =>
                    {
                        ShowKickPrompt();
                    }
                );
            }
            else
            {
                EventBridge.Menu.showWelcomeUi.Invoke(MelcherUITransitionStyle.Regress);

                ShowKickPrompt();
            }

            void ShowKickPrompt()
            {
                EventBridge.Menu.showUserKickedPrompt?.Invoke(kickReason);
            }
        }

        public IEnumerator<float> FinishLogInCoroutine(bool rememberMe, bool fetchClients, MelcherWebRequestBuilder.OnRequestErrorJson errorHandler)
        {
            this.LogInState = LogInState.Running;

            bool requestSucceeded = false;

            if (fetchClients)
            {
                MelcherWebCoroutine getClientListRequest =
                    this._server.Clients.GetClientList(
                        this.User,
                        onSuccess: clientList =>
                        {
                            this._clientList.UserClients = clientList;
                            requestSucceeded = true;
                        },
                        onError: errorHandler
                    );

                yield return Timing.WaitUntilDone(getClientListRequest);

                if (!requestSucceeded)
                {
                    goto Error;
                }
            }

            EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
            EventBridge.Menu.showBackgroundSpinner?.Invoke(true);
            EventBridge.Menu.showHomeUi?.Invoke();

            if (this.UserProfile.IsTrialActive())
            {
                EventBridge.Menu.showTrialCountdownUi?.Invoke();
            }

            if (rememberMe)
            {
                MelcherLog.LogEditorOnly("Saving session token to PlayerPrefs");
                PlayerPrefs.SetString(RememberMeTokenKey, this.User.apiToken);
                PlayerPrefs.Save();
            }
            else
            {
                this.ForgetRememberMeData();
            }

            this.StartAutoKickCoroutine();

            requestSucceeded = false;

            MelcherWebCoroutine getProductListRequest =
                this._server.Catalog.GetProductList(
                    this.User,
                    this._catalogData.Manufacturers,
                    () => this._catalogData.ProductGroups,
                    onSuccess: productCatalog =>
                    {
                        this._catalogData.SourceProducts = productCatalog;
                        this._catalogData.ClearRuntimeProducts();
                        requestSucceeded = true;
                    },
                    onError: errorHandler
                );

            yield return Timing.WaitUntilDone(getProductListRequest);

            if (!requestSucceeded || this.User.id == 0)
            {
                goto Error;
            }

            CabinetDataAvailability cabinetDataAvailability = this._appProfile.appOptions.cabinetDataAvailability;

            if (cabinetDataAvailability >= CabinetDataAvailability.Server)
            {
                requestSucceeded = false;

                MelcherWebCoroutine getCabinetSectionCategoriesRequest =
                    this._server.Catalog.GetCabinetSectionCategories(
                        onSuccess: cabinetSectionCategories =>
                        {
                            this._catalogData.CabinetSectionCategories = cabinetSectionCategories;
                            requestSucceeded = true;
                        },
                        onError: errorHandler
                    );

                yield return Timing.WaitUntilDone(getCabinetSectionCategoriesRequest);

                if (!requestSucceeded || this.User.id == 0)
                {
                    goto Error;
                }

                requestSucceeded = false;

                MelcherWebCoroutine getBrandCatalogsRequest =
                    this._server.Catalog.GetBrandCatalogs(
                        onSuccess: brandCatalog =>
                        {
                            this._catalogData.ServerBrandCatalogs = brandCatalog;
                            requestSucceeded = true;
                        },
                        onError: errorHandler
                    );

                yield return Timing.WaitUntilDone(getBrandCatalogsRequest);

                if (!requestSucceeded || this.User.id == 0)
                {
                    goto Error;
                }

                this._catalogData.PopulateRuntimeBrandCatalogs();

                requestSucceeded = false;

                MelcherWebCoroutine getCabinetListRequest =
                    this._server.Catalog.GetCabinetList(
                        this.User,
                        this._catalogData.Manufacturers,
                        this._catalogData.CabinetSectionCategories,
                        () => this._catalogData.ProductGroups,
                        () => this._catalogData.SourceProducts,
                        onSuccess: cabinetList =>
                        {
                            if (this._catalogData.SourceProducts != null)
                            {
                                this._catalogData.SourceProducts.AddRange(cabinetList);
                            }
                            else
                            {
                                this._catalogData.SourceProducts = cabinetList;
                            }

                            requestSucceeded = true;
                        },
                        onError: errorHandler
                    );

                yield return Timing.WaitUntilDone(getCabinetListRequest);

                if (!requestSucceeded || this.User.id == 0)
                {
                    goto Error;
                }
            }
            else if (cabinetDataAvailability >= CabinetDataAvailability.ClientOnly)
            {
                if (this._catalogData.LoadFallbackWebResponse(FallbackJsonFiles.Cabinets) is JsonDict response)
                {
                    Exception error =
                        CatalogJson.TryParseCabinetList(
                            new MelcherJsonObjectParser(response),
                            this._catalogData.Manufacturers,
                            this._catalogData.CabinetSectionCategories,
                            this._catalogData.ProductGroups,
                            this._catalogData.SourceProducts,
                            this._appRules,
                            out List<ProductInfo> cabinetList
                        );

                    if (error != null)
                    {
                        MelcherLog.LogError("Error parsing fallback product catalog: " + error.Message);
                    }

                    if (cabinetList != null)
                    {
                        this._catalogData.SourceProducts.AddRange(cabinetList);
                    }
                }
            }

            this._catalogData.LoadAndApplyProductOverrides();

            this._catalogData.BuildProductVariantLists();

            EventBridge.Menu.showBackgroundSpinner?.Invoke(false);

            this.LogInState = LogInState.Finished;

            yield break;

        Error:
            bool didUserLogOut = this.UserProfile.userId == 0;

            EventBridge.Menu.showFullScreenSpinner?.Invoke(false);
            EventBridge.Menu.showBackgroundSpinner?.Invoke(false);

            this.LogOut();

            EventBridge.Menu.showWelcomeUi.Invoke(MelcherUITransitionStyle.Regress);
            EventBridge.Menu.pushLogInUi.Invoke();

            if (!didUserLogOut)
            {
                errorHandler?.Invoke(null, new Exception("Error retrieving product or cabinet data."), default);
            }

            this.LogInState = LogInState.NotStarted;
        }

        private void StartAutoKickCoroutine()
        {
            Debug.Assert(!this._autoKickUserCoroutine.IsValid);

            this._autoKickUserCoroutine = Timing.RunCoroutine(this.KickUserWhenPaymentExpires());
        }

        private void StopAutoKickCoroutine()
        {
            Timing.KillCoroutines(this._autoKickUserCoroutine);
            this._autoKickUserCoroutine = default;
        }

        private IEnumerator<float> KickUserWhenPaymentExpires()
        {
            while (this.User.id != 0)
            {
                bool wasTrialActive = this.UserProfile.IsTrialActive();
                yield return Timing.WaitForSeconds(20.0f);

                yield return Timing.WaitUntilDone(
                    this._server.Accounts.GetProfile(
                        this.User.apiToken,
                        this.User.userType == UserType.Offline,
                        onSuccess: newUserProfile =>
                        {
                            this.UserProfile = newUserProfile;
                        },
                        onError: null
                    )
                );

                if (this.UserProfile.IsSubscriptionActive())
                {
                    EventBridge.Menu.hideTrialCountdownUi?.Invoke();

                    continue;
                }

                if (!this.UserProfile.IsTrialActive())
                {
                    this.KickUser(
                        kickReason: wasTrialActive ? "Trial expired." : "Subscription expired."
                    );
                    yield break;
                }
            }
        }

        private static string GetKioskModePlayerPrefKey(User user)
        {
            return $"User.{user.id}.isKioskModeEnabled";
        }
    }
}
