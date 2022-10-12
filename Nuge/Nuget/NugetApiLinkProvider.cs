using System.Linq;
using Meadow.Tools.Assistant.Nuget;
using nuge.Nuget.Dtos;

namespace nuge.Nuget
{
    public class NugetApiLinkProvider
    {
        /// <summary>
        /// Gets as microsoft calls it "@id"
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetApiBaseUrl(NugetIndex index)
        {
            var packageResourceUrl = index.Resources
                .Where(r => r.Type == "PackageBaseAddress/3.0.0")
                .Select(r => r.Id)
                .FirstOrDefault();

            return packageResourceUrl;
        }


        public string GetSearchApiUrl(NugetIndex index, string query)
        {
            var url = GetSearchApiBaseUrl(index);

            url += "?q=" + query + "&prerelease=false";

            return url;
        }
        
        public string GetSearchApiBaseUrl(NugetIndex index)
        {
            var packageResourceUrl = index.Resources
                .Where(r => r.Type == "SearchQueryService/3.5.0")
                .Select(r => r.Id)
                .FirstOrDefault();

            return packageResourceUrl;
        }

        public string GetNuspecLink(NugetIndex index, PackageId packageId)
        {
            return GetNuspecLink(index, packageId.Id, packageId.Version);
        }

        public string GetNuspecLink(NugetIndex index, string packageName, string packageVersion)
        {
            //     var url = NuspecApiBase + $"{loweredId}/{loweredVersion}/{loweredId}.nuspec";
            var baseUrl = GetApiBaseUrl(index);

            return baseUrl
                   + packageName.ToLowerInvariant() + "/"
                   + packageVersion.ToLowerInvariant() + "/"
                   + packageName.ToLowerInvariant()+ ".nuspec";
        }

        public string GetPackageDownloadLink(NugetIndex index, string packageName, string packageVersion)
        {
            //GET {@id}/{LOWER_ID}/{LOWER_VERSION}/{LOWER_ID}.{LOWER_VERSION}.nupkg

            var versionSegment = "/";
            var versionPrefix = "";

            if (!string.IsNullOrEmpty(packageVersion))
            {
                versionSegment = "/" + packageVersion.ToLowerInvariant() + "/";
                versionPrefix = "." + packageVersion.ToLowerInvariant();
            }

            var baseUrl = GetApiBaseUrl(index);

            return baseUrl + packageName.ToLowerInvariant() + versionSegment
                   + packageName.ToLowerInvariant() + versionPrefix + ".nupkg";
        }

        public string GetPackageDownloadLink(NugetIndex index, PackageId packageId)
        {
            return GetPackageDownloadLink(index, packageId.Id, packageId.Version);
        }
    }
}