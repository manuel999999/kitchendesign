using System;
using System.Globalization;
using KitchenDesign.Events;
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

        private void Awake()
        {
            this._loadsFirstAppScene = this.gameObject.scene.buildIndex == BootSceneBuildIndex;
            DontDestroyOnLoad(this.gameObject);

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            bool isDebugMode = Application.isEditor || Debug.isDebugBuild;
            bool isVerbose = isDebugMode || MelcherWebGL.GetUrlParameter("verbose") != null;

            if (!isVerbose && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                string[] arguments = Environment.GetCommandLineArgs();
                isVerbose |= Array.FindIndex(arguments, x => "--verbose".Equals(x, StringComparison.OrdinalIgnoreCase)) >= 0;
            }

            MelcherLog.Log(
                $"{nameof(AppInitializer)}:isDebugMode={isDebugMode} isVerbose={isVerbose}",
                severity: MelcherLogSeverity.MaxValue
            );

            MelcherLog.minimumSeverityToDisplay = isVerbose ? MelcherLogSeverity.Verbose : MelcherLogSeverity.Warning;
            MelcherWebRequestBuilder.runtimeDebug = isVerbose;
            ProFitGlobals.isVerbose = isVerbose;

            if (QualitySettings.GetQualityLevel() != 0)
            {
                QualitySettings.SetQualityLevel(0);
            }

            UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;

            DOTweenInit.Init(isDebugMode);

            SceneManager.sceneLoaded += this.OnSceneLoaded;

            ProFitGlobals.Init();
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

                sceneInitializer.Initialize(this.appProfile, ProFitGlobals.appRules);
                EventApp.OnSceneReady.Raise();
                return;
            }
        }
    }
}
