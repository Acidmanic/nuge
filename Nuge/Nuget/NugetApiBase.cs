using System.IO;
using System.Reflection;
using Acidmanic.Utilities.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;
using nuge.Extensions;
using nuge.Nuget.Dtos;

namespace nuge.Nuget
{
    public class NugetApiBase
    {
        private static string NugetIndexCacheDirectory =
            new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent?.FullName;

        private static string NugetIndexCacheFile = "NugetIndex.json";
        protected static NugetIndex NugetIndex { get; set; } = new NugetIndex();
        private static readonly object NugetIndexLock = new object();
        private static readonly string NugetIndexUrl = "https://api.nuget.org/v3/index.json";


        public string Proxy { get; set; } = null;

        public ILogger Logger { get; set; } = new LoggerAdapter(s => { });


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


        protected PatientDownloader MakeDownloader()
        {
            return new PatientDownloader
            {
                Logger = Logger,
                Proxy = Proxy
            };
        }

        public Result<byte[]> ProvidePackageData(PackageId packageId, int timeout, int retries)
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

        protected void JustWrite(string path, byte[] data)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllBytes(path, data);
        }
    }
}