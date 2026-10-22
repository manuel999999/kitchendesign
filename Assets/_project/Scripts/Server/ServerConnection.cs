using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Utility;
using MEC;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Server
{
    public sealed class ServerConnection
    {
        public ServerConnection(AppProfile appProfile)
        {
            this.AppProfile = appProfile;

            this.WhitelistedEmbedDomains =
                new List<string>()
                {
                    UrlUtility.GetBaseDomain(appProfile.appOptions.GetServerDomain(ServerType.Production)),
                    UrlUtility.GetBaseDomain(appProfile.appOptions.GetServerDomain(ServerType.Test)),
                    "profitoutdoorliving.com",
                    "3doutdoorkitchendesign.com",
                    "stollkitchendesign.com",
                    "geminikitchendesigner.com",
                }.AsReadOnly();
        }

        public AppProfile AppProfile { get; }

        public string Domain => this.AppProfile.serverDomain;

        public bool UseHttps { get; set; } = true;

        public string Host => (this.UseHttps ? "https://" : "http://") + this.Domain;

        public bool IsTestServer => this.AppProfile.targetServerType == ServerType.Test;

        public ReadOnlyCollection<string> WhitelistedEmbedDomains { get; }

        public bool IsHostReachable { get; private set; }

        public IEnumerator<float> ProbeCoroutine()
        {
            this.IsHostReachable = false;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                MelcherLog.LogWarning($"The internet is not reachable.");
                yield break;
            }

            var firstRequest =
                MelcherWebRequestBuilder.Get(this.Host)
                    .OnCompleted(
                        onCompleted: (_, error) =>
                        {
                            this.IsHostReachable = error == null;
                        }
                    )
                    .Send();

            yield return Timing.WaitUntilDone(firstRequest);

            if (!this.IsHostReachable)
            {
                this.UseHttps = !this.UseHttps;
                MelcherLog.LogWarning($"Changed {nameof(this.UseHttps)} to " + this.UseHttps);

                var secondRequest =
                    MelcherWebRequestBuilder.Get(this.Host)
                        .OnCompleted(
                            onCompleted: (_, error) =>
                            {
                                this.IsHostReachable = error == null;
                            }
                        )
                        .Send();

                yield return Timing.WaitUntilDone(secondRequest);
            }

            if (!this.IsHostReachable)
            {
                MelcherLog.LogError("The server is not reachable: " + this.Domain);
            }
        }

#if UNITY_EDITOR
        public void SaveFallbackJsonResource(string fileNameNoExt, string contents)
        {
            string resourcePath = this.AppProfile.GetFallbackJsonPath(fileNameNoExt);

            string assetPath =
                Path.Combine(
                    "Assets/AssetsMelcher/Resources/",
                    resourcePath + ".json"
                );

            UnityEditor.AssetDatabase.DeleteAsset(assetPath);

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

            string projectRoot =
                Path.GetFullPath(
                    Path.Combine(Application.dataPath, "..")
                );
            string fullAssetPath = Path.Combine(projectRoot, assetPath);
            File.WriteAllText(fullAssetPath, contents);

            UnityEditor.AssetDatabase.ImportAsset(assetPath);
            UnityEditor.AssetDatabase.SaveAssets();

            MelcherLog.Log($"Wrote fallback JSON '{fileNameNoExt}': {fullAssetPath}");
        }
#endif
    }
}
