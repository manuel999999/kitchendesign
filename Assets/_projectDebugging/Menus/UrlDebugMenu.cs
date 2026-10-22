using KitchenDesign.Utility;
using UnityEditor;
using UnityEngine;

namespace KitchenDesign.Debugging
{
    public static class UrlDebugMenu
    {
        private static readonly string[] SampleUrls =
        {
            "",
            "root.com",
            "root.com/",
            "www.root.com/",
            "https://www.root.com",
            "https://subdomain.domain.root.com/abcd",
            "https://subdomain.domain.root.com/abcd/1234/file.txt",
            "http://23.111.69.219/admin/",
            "http://23.111.69.219:8080/admin/",
            "23.111.69.219:8080/admin/",
            "23.111.69.219:8080"
        };

        [MenuItem("Tools/Kitchen Design/Tests/UrlUtility.GetBaseDomain")]
        private static void TestGetBaseDomain()
        {
            foreach (string url in SampleUrls)
            {
                Debug.Log(UrlUtility.GetBaseDomain(url));
            }
        }

        [MenuItem("Tools/Kitchen Design/Tests/UrlUtility.GetDomain")]
        private static void TestGetDomain()
        {
            foreach (string url in SampleUrls)
            {
                Debug.Log(UrlUtility.GetDomain(url));
            }
        }
    }
}
