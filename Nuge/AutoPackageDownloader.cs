using System;
using System.IO;
using System.Threading;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.LightWeight;
using nuge.DotnetProject;
using nuge.Nuget.PackageSources;

namespace nuge
{
    public class AutoPackageDownloader
    {
        public string DownloadDirectory { get; set; } = "Packages";

        public string SearchDirectory { get; set; } = ".";


        public ILogger Logger { get; set; } = new LoggerAdapter(s => { });

        public void Download()
        {
            Logger.LogInformation("Searching: {Search}", SearchDirectory);
            Logger.LogInformation("Downloading Into: {Downloads}", DownloadDirectory);

            var downloaded = 0;
            
            var nuget = new Nuget.Nuget(DownloadDirectory, Logger);

            var directory = DownloadDirectory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var projects = DotnetProjectInfo.FindProjects(SearchDirectory);

            var mergedProject = new MergedProject(projects);

            var packages = mergedProject.PackageReferences;

            Logger.LogInformation("--------------------------------------------");

            var ndl = new NuGetDownloader
            {
                Logger = Logger
            }.FuckSsl();


            foreach (var packageReference in packages)
            {
                var pid = new PackageId
                {
                    Id = packageReference.PackageName,
                    Version = packageReference.PackageVersion
                };

                Result<byte[]> package;

                try
                {
                    package = ndl.ProvidePackage(pid,1200,10);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error downloading: {Exception}", e);

                    package = new Result<byte[]>().FailAndDefaultValue();
                }

                if (package)
                {
                    var path = Path.Combine(directory, pid.AsFileName());

                    JustWrite(path, package.Value);

                    Logger.LogInformation("Downloaded: {Name}: {Version}",
                        packageReference.PackageName,
                        packageReference.PackageVersion);

                    downloaded += 1;

                    Thread.Sleep(100);
                }
                else
                {
                    Logger.LogInformation("Unable to Download from Nuget");
                }
            }

            Logger.LogInformation("--------------------------------------------");
            Logger.LogInformation("Downloaded {Count} of {Total} packages.",downloaded,packages.Count);
            Logger.LogInformation("--------------------------------------------");
        }

        private void JustWrite(string path, byte[] data)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllBytes(path, data);
        }
    }
}