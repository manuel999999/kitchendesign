using System;
using System.Text.RegularExpressions;
using Melcher;
using UnityEngine;

namespace KitchenDesign
{
    public sealed class LaunchOptions
    {
        private readonly AppOptions _appOptions;

        private LaunchOptions(AppOptions appOptions, bool isDebugMode, bool isVerbose)
        {
            this._appOptions = appOptions;
            this.IsDebugMode = isDebugMode;
            this.IsVerbose = isVerbose;
        }

        public bool IsDebugMode { get; }
        public bool IsVerbose { get; }

        public static LaunchOptions FromEnvironment(AppOptions appOptions)
        {
            bool isDebugMode = Application.isEditor || Debug.isDebugBuild;
            bool isVerbose = isDebugMode || MelcherWebGL.GetUrlParameter("verbose") != null;

            if (!isVerbose && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                string[] arguments = Environment.GetCommandLineArgs();
                isVerbose |= Array.FindIndex(arguments, x => "--verbose".Equals(x, StringComparison.OrdinalIgnoreCase)) >= 0;
            }

            return new LaunchOptions(appOptions, isDebugMode, isVerbose);
        }

        public string GetWebDomainOfApp()
        {
#if UNITY_EDITOR
            if (this._appOptions.simulateWhiteLabelMode)
            {
                return this._appOptions.simulatedWhiteLabelDomain;
            }
#endif

            string urlOfApp = MelcherWebGL.GetTopLevelUrl();

            MelcherLog.Log("urlOfApp: " + urlOfApp, severity: MelcherLogSeverity.Verbose);

            string domain = null;

            if (urlOfApp != null)
            {
                domain = Utility.UrlUtility.GetDomain(urlOfApp);
            }

            return domain;
        }

        public string GetDistributorUrlParameter()
        {
#if UNITY_EDITOR
            if (this._appOptions.simulateWhiteLabelMode)
            {
                return this._appOptions.simulatedWhiteLabelDistributorUrlParameter;
            }
#endif

            string distributor = MelcherWebGL.GetUrlParameter("distributor");

            MelcherLog.Log("outer site distributor = " + distributor, severity: MelcherLogSeverity.Verbose);

            string embeddedUrl = Application.absoluteURL;
            if (distributor == null && embeddedUrl != null)
            {
                Match embeddedMatch =
                    Regex.Match(embeddedUrl, "[?&]distributor=(.*?)(?:&|$)");

                if (embeddedMatch.Success && embeddedMatch.Groups.Count >= 2)
                {
                    distributor = embeddedMatch.Groups[1].Value;

                    if (string.IsNullOrEmpty(distributor))
                    {
                        distributor = null;
                    }

                    MelcherLog.Log("embedded distributor = " + distributor, severity: MelcherLogSeverity.Verbose);
                }
            }

            return distributor;
        }
    }
}
