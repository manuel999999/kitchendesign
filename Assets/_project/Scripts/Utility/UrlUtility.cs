using System.Text.RegularExpressions;

namespace KitchenDesign.Utility
{
    public static class UrlUtility
    {
        public static string GetBaseDomain(string url)
        {
            string baseDomain = url ?? "";

            const string SchemeSeparator = "://";
            int schemeIndex = baseDomain.IndexOf(SchemeSeparator);
            if (schemeIndex >= 0)
            {
                baseDomain = baseDomain.Substring(schemeIndex + SchemeSeparator.Length);
            }

            int slashIndex = baseDomain.IndexOf('/');
            if (slashIndex >= 0)
            {
                baseDomain = baseDomain.Substring(0, slashIndex);
            }

            Match ipMatch = Regex.Match(baseDomain, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            if (ipMatch != null && ipMatch.Success)
            {
                return ipMatch.Value;
            }

            int lastDotIndex = baseDomain.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                int subdomainDotIndex = baseDomain.LastIndexOf('.', lastDotIndex - 1);
                if (subdomainDotIndex >= 0)
                {
                    baseDomain = baseDomain.Substring(subdomainDotIndex + 1);
                }
            }

            return baseDomain;
        }

        public static string GetDomain(string url)
        {
            if (!url.StartsWith("https://") && !url.StartsWith("http://"))
            {
                url = "https://" + url;
            }

            string host = "";

            try
            {
                System.Uri uri = new System.Uri(url);
                host = uri.Host;
            }
            catch
            {
            }

            return host;
        }
    }
}
