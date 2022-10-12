using System.Net;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;
using Microsoft.Extensions.Logging;
using nuge.Extensions;

namespace nuge.Nuget.PackageSources
{
    public class NuGetDownloader : NugetApiBase, IPackageSource
    {
        public NuGetDownloader FuckSsl()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            Logger.LogWarning("SSL issues are being ignored. ,,|,,");

            return this;
        }


        public Result<byte[]> ProvidePackage(PackageId packageId)
        {
            return ProvidePackageData(packageId, 2000, 1);
        }


        public Nuspec GetNuspecObject(PackageId packageId)
        {
            var xml = GetNuspec(packageId);

            return new Nuspec().LoadXml(xml);
        }

        public string GetNuspec(PackageId packageId)
        {
            if (UpdateIndex())
            {
                var url = new NugetApiLinkProvider().GetNuspecLink(NugetIndex, packageId);

                var downloader = MakeDownloader();

                var downloadResult = downloader.DownloadString(url, 3000, 5).Result;

                if (downloadResult)
                {
                    return downloadResult.Value;
                }

                Logger.LogError("Unable to get Nuspec for {Url}", url);

                return null;
            }

            Logger.LogError("Unable to update index file...");

            return null;
        }
    }
}