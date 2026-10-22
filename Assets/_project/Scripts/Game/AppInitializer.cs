using System.Globalization;
using KitchenDesign.AppRules;
using KitchenDesign.CatalogData;
using KitchenDesign.Events;
using KitchenDesign.Server;
using KitchenDesign.Session;
using KitchenDesign.Shared.Interfaces;
using Melcher;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KitchenDesign
{
    public sealed class AppInitializer : MonoBehaviour
    {
        public const int BootSceneBuildIndex = 0;

        private const int FirstAppSceneBuildIndex = 1;

        [SerializeField] private AppProfile appProfile;

        private bool _loadsFirstAppScene;
        private IAppRules _appRules;

        private void Awake()
        {
            this._loadsFirstAppScene = this.gameObject.scene.buildIndex == BootSceneBuildIndex;
            DontDestroyOnLoad(this.gameObject);

            LaunchOptions launchOptions = LaunchOptions.FromEnvironment(this.appProfile.appOptions);

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            MelcherLog.Log(
                $"{nameof(AppInitializer)}:isDebugMode={launchOptions.IsDebugMode} isVerbose={launchOptions.IsVerbose}",
                severity: MelcherLogSeverity.MaxValue
            );

            MelcherLog.minimumSeverityToDisplay = launchOptions.IsVerbose ? MelcherLogSeverity.Verbose : MelcherLogSeverity.Warning;
            MelcherWebRequestBuilder.runtimeDebug = launchOptions.IsVerbose;

            if (QualitySettings.GetQualityLevel() != 0)
            {
                QualitySettings.SetQualityLevel(0);
            }

            UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;

            DOTweenInit.Init(launchOptions.IsDebugMode);

            this._appRules = AppRulesFactory.Create(this.appProfile.appOptions.userFilter);

            ServerClient server = new ServerClient(this.appProfile, this._appRules);
            CatalogDataStatus catalogData = new CatalogDataStatus(this.appProfile, this._appRules, server);
            ClientListStatus clientList = new ClientListStatus();
            SessionStatus session = new SessionStatus(this.appProfile, this._appRules, server, clientList, catalogData);

            ProFitGlobals.Bind(this.appProfile, this._appRules, launchOptions, server, catalogData, clientList, session);

            session.Initialize();
            clientList.Initialize();
            catalogData.Initialize();

            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }

        private void Start()
        {
            if (!this._loadsFirstAppScene)
            {
                return;
            }

            MelcherSceneManager.LoadSceneAsync(MelcherSceneRequest.FromBuildIndex(FirstAppSceneBuildIndex), MelcherSceneLoadMode.Single);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!root.TryGetComponent(out SceneInitializer sceneInitializer))
                {
                    continue;
                }

                sceneInitializer.Initialize(this.appProfile, this._appRules);
                EventApp.OnSceneReady.Raise();
                return;
            }
        }
    }
}
