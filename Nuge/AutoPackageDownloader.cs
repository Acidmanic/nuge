using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;
using nuge.DotnetProject;
using nuge.Extensions;
using nuge.Nuget.PackageSources;

namespace nuge
{
    public class AutoPackageDownloader
    {

        private HashSet<string> DownloadedItems = new HashSet<string>();
        public string DownloadDirectory { get; set; } = "Packages";

        public string SearchDirectory { get; set; } = ".";


        public ILogger Logger { get; set; } = new LoggerAdapter(s => { });

        public void Download(bool fromScratch = false)
        {

            
            Logger.LogInformation("Searching: {Search}", SearchDirectory);
            Logger.LogInformation("Downloading Into: {Downloads}", DownloadDirectory);

            var downloaded = 0;
            var fulfilled = 0;
            var failed = 0;
            
            var nuget = new Nuget.Nuget(DownloadDirectory, Logger);

            var directory = DownloadDirectory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var projects = DotnetProjectInfo.FindProjects(SearchDirectory);

            var mergedProject = new MergedProject(projects);

            var packages = Distincts(mergedProject);

            Logger.LogInformation("--------------------------------------------");

            Logger.LogInformation("Found {Count} Packages to be downloaded.", packages.Count);

            
            var ndl = new NuGetDownloader
            {
                Logger = Logger
            }.FuckSsl();
            
            Logger.LogInformation("--------------------------------------------");
            
            foreach (var pid in packages)
            {

                if (MustDownload(pid, fromScratch))
                {
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
                            pid.Id,
                            pid.Version);

                        downloaded += 1;
                        fulfilled += 1;
                        
                        MarkAsDownloaded(pid);
                        
                        Thread.Sleep(100);
                    }
                    else
                    {
                        failed += 1;
                        Logger.LogInformation("Unable to Download {Package} from Nuget",pid.ToString());
                    }
                }
                else
                {
                    fulfilled += 1;
                    Logger.LogInformation("Package {Package}, Is already downloaded.",pid.ToString());
                }
            }

            Logger.LogInformation("--------------------------------------------");
            Logger.LogInformation("Fulfilled {Count} of {Total} packages.",fulfilled,packages.Count);
            Logger.LogInformation("Downloaded {Count} of {Total} packages.",downloaded,packages.Count);
            Logger.LogInformation("Failed {Count} of {Total} packages.",failed,packages.Count);
            Logger.LogInformation("--------------------------------------------");
        }

        private List<PackageId> Distincts(MergedProject mergedProject)
        {
            var result = new List<PackageId>();
            var existing = new HashSet<string>();
            
            
            foreach (var reference in mergedProject.PackageReferences)
            {
                var pid = new PackageId
                {
                    Id = reference.PackageName,
                    Version = reference.PackageVersion
                };

                string key = reference.ToString();

                if (!existing.Contains(key))
                {
                    existing.Add(key);
                    
                    result.Add(pid);
                }
            }

            return result;
        }

        private bool MustDownload(PackageId package, bool fromScratch)
        {
            if (fromScratch)
            {
                return true;
            }

            DownloadedItems = new HashSet<string>().LoadCached<HashSet<string>>(DownloadDirectory, "downloaded.json");

            return DownloadedItems == null || !DownloadedItems.Contains(package.AsFileName());
        }

        private void MarkAsDownloaded(PackageId package)
        {
            
            DownloadedItems = new HashSet<string>().LoadCached<HashSet<string>>(DownloadDirectory, "downloaded.json");

            if (DownloadedItems == null)
            {
                DownloadedItems = new HashSet<string>();
            }

            DownloadedItems.Add(package.AsFileName());
            
            DownloadedItems.CacheInto(DownloadDirectory,"downloaded.json");
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