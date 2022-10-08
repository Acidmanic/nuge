using System;
using System.IO;
using System.Net;
using System.Reflection;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;
using nuge.Extensions;
using nuge.Nuget.Dtos;

namespace nuge.Nuget.PackageSources
{
    public class NuGetDownloader : IPackageSource
    {
        private static string NugetIndexCacheDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent?.FullName;
        private static string NugetIndexCacheFile = "NugetIndex.json";
        private static NugetIndex NugetIndex { get; set; } = new NugetIndex();
        private static readonly object NugetIndexLock = new object();
        private static readonly string NugetIndexUrl = "https://api.nuget.org/v3/index.json";


        public string Proxy { get; set; } = null;

        public ILogger Logger { get; set; } = new LoggerAdapter(s => { });


        public NuGetDownloader FuckSsl()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => { return true; };

            Logger.LogWarning("SSL issues are being ignored. ,,|,,");

            return this;
        }

        public bool UpdateIndex(bool force = false)
        {
            lock (NugetIndexLock)
            {
                var index = new NugetIndex().LoadCached<NugetIndex>
                    (NugetIndexCacheDirectory, NugetIndexCacheFile);

                if (index != null && !index.IsEmpty)
                {
                    NugetIndex = index;
                }

                if (force || NugetIndex == null || NugetIndex.IsEmpty)
                {
                    var downloader = MakeDownloader();

                    var indexResult = downloader.DownloadObject<NugetIndex>(NugetIndexUrl, 1200, 10).Result;

                    if (indexResult)
                    {
                        NugetIndex = indexResult.Value;
                        
                        NugetIndex.CacheInto(NugetIndexCacheDirectory, NugetIndexCacheFile);
                    }
                }
            }

            return NugetIndex != null && !NugetIndex.IsEmpty;
        }

        private PatientDownloader MakeDownloader()
        {
            return new PatientDownloader
            {
                Logger = Logger,
                Proxy = Proxy
            };
        }


        public Result<byte[]> ProvidePackage(PackageId packageId)
        {
            return ProvidePackage(packageId, 2000, 1);
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

        public Result<byte[]> ProvidePackage(PackageId packageId, int timeout, int retries)
        {
            if (UpdateIndex())
            {
                var url = new NugetApiLinkProvider().GetPackageDownloadLink(NugetIndex, packageId);

                var downloader = MakeDownloader();

                var downloadResult = downloader.DownloadFile(url, timeout, retries).Result;

                if (downloadResult)
                {
                    return downloadResult.Value;
                }

                Logger.LogError("Unable to Download Package. {Package}", url);

                return new Result<byte[]>().FailAndDefaultValue();
            }


            Logger.LogError("Unable to download nuget index file. ");
            Logger.LogDebug("You can Cheat the nuget index downloading by " +
                            "downloading it manually and put it beside the binary " +
                            "file and name it: {Name}", NugetIndexCacheFile);

            return new Result<byte[]>().FailAndDefaultValue();
        }
    }
}