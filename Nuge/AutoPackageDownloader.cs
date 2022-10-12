using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;
using Newtonsoft.Json;
using nuge.DotnetProject;
using nuge.Extensions;
using nuge.Nuget;
using nuge.Nuget.PackageSources;
using nuge.Utils;

namespace nuge
{
    public class AutoPackageDownloader
    {
        private HashSet<string> DownloadedItems = new HashSet<string>();
        public string DownloadDirectory { get; set; } = "Packages";

        public string SearchDirectory { get; set; } = ".";


        private JsonCache _cache = null;

        public ILogger Logger { get; set; } = new LoggerAdapter(s => { });


        public void ClearJunks()
        {
            var files = new DirectoryInfo(DownloadDirectory).GetFiles();

            Logger.LogInformation("Clearing junk files.");

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    Logger.LogWarning("Deleted Junk File {File}", file.Name);

                    file.Delete();
                }
            }

            GetCache().ClearJunks();

            Logger.LogInformation("DONE Clearing junk files.");
        }


        private JsonCache GetCache()
        {
            if (_cache == null)
            {
                _cache = new JsonCache(Path.Combine(DownloadDirectory, "cache"));
            }

            _cache.Logger = Logger;

            return _cache;
        }


        public void DownloadUntilResolves(bool fromScratch = false, bool saveLower = true)
        {
            
            var remaining = GetDirectoryPackages();
            
            Download(remaining, fromScratch, saveLower);
            
            remaining = RemainingDependencies(saveLower);
            
            while (remaining.Count > 0)
            {
                Download(remaining,false, saveLower);

                remaining = RemainingDependencies(saveLower);
            }
            
            Logger.LogInformation("============================================");
            Logger.LogInformation("             Fucking Resolved");
            Logger.LogInformation("============================================");
        }

        private void Download(List<PackageId> packages, bool fromScratch = false, bool saveLower = true)
        {
            var downloaded = 0;
            var fulfilled = 0;
            var failed = 0;

            Logger.LogInformation("--------------------------------------------");

            Logger.LogInformation("Found {Count} Packages to be downloaded.", packages.Count);


            var ndl = new NuGetDownloader
            {
                Logger = Logger
            }.FuckSsl();

            Logger.LogInformation("--------------------------------------------");

            foreach (var pid in packages)
            {
                if (MustDownload(pid, fromScratch,saveLower))
                {
                    Result<byte[]> package;

                    try
                    {
                        package = ndl.ProvidePackageData(pid, 1200, 10);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error downloading: {Exception}", e);

                        package = new Result<byte[]>().FailAndDefaultValue();
                    }

                    if (package)
                    {
                        var path = Path.Combine(DownloadDirectory, pid.AsFileName(saveLower));

                        JustWrite(path, package.Value);

                        Logger.LogInformation("Downloaded: {Name}: {Version}",
                            pid.Id,
                            pid.Version);

                        downloaded += 1;
                        fulfilled += 1;

                        MarkAsDownloaded(pid,saveLower);

                        Thread.Sleep(100);
                    }
                    else
                    {
                        failed += 1;
                        Logger.LogInformation("Unable to Download {Package} from Nuget", pid.ToString());
                    }
                }
                else
                {
                    fulfilled += 1;
                    Logger.LogInformation("Package {Package}, Is already downloaded.", pid.ToString());
                }
            }

            Logger.LogInformation("--------------------------------------------");
            Logger.LogInformation("Fulfilled {Count} of {Total} packages.", fulfilled, packages.Count);
            Logger.LogInformation("Downloaded {Count} of {Total} packages.", downloaded, packages.Count);
            Logger.LogInformation("Failed {Count} of {Total} packages.", failed, packages.Count);
            Logger.LogInformation("--------------------------------------------");
        }


        public List<PackageId> GetDirectoryPackages()
        {
            Logger.LogInformation("Searching: {Search}", SearchDirectory);
            Logger.LogInformation("Downloading Into: {Downloads}", DownloadDirectory);


            var directory = DownloadDirectory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var projects = DotnetProjectInfo.FindProjects(SearchDirectory);

            var mergedProject = new MergedProject(projects);

            var packages = Distincts(mergedProject);

            return packages;
        }


        public void Download(bool fromScratch = false, bool saveLower = true)
        {
            var packages = GetDirectoryPackages();
            
            Download(packages, fromScratch, saveLower);
        }


        public bool PackageExists(PackageId pid, bool saveLower = true)
        {
            var path = Path.Combine(DownloadDirectory, pid.AsFileName(saveLower));

            return File.Exists(path);
        }


        
        private List<Nuspec> ReadAvailableNuspecs()
        {
            var nuspecs = new List<Nuspec>();

            var packages = new DirectoryInfo(DownloadDirectory).GetFiles();

            foreach (var file in packages)
            {
                if (file.Name.ToLower().EndsWith(".nupkg"))
                {
                    var package = new NugetPackage(file.FullName);

                    nuspecs.Add(package.Nuspec);
                }
            }

            return nuspecs;
        }

        private List<PackageId> RemainingDependencies(bool saveLower)
        {
            var availableNuspecs = ReadAvailableNuspecs();

            var requestedDependencies = new List<PackageId>();

            var alreadyAdded = new HashSet<string>();

            foreach (var nuspec in availableNuspecs)
            {
                foreach (var dependency in nuspec.Dependencies)
                {
                    var key = dependency.ToString();

                    if (!alreadyAdded.Contains(key))
                    {
                        alreadyAdded.Add(key);

                        requestedDependencies.Add(dependency);
                    }
                }
            }

            var unavailableDependencies = new List<PackageId>();

            foreach (var dependency in requestedDependencies)
            {
                if (!PackageExists(dependency, saveLower))
                {
                    Logger.LogInformation("Found unresolved dependency: {Dependency}", dependency);

                    unavailableDependencies.Add(dependency);
                }
            }

            return unavailableDependencies;
        }

        // private List<PackageId> AddDependencies(List<PackageId> packages)
        // {
        //     var allPackages = new List<PackageId>(packages);
        //
        //     var added = new HashSet<string>();
        //
        //     foreach (var package in packages)
        //     {
        //         AddDependenciesRecursively(package, added, allPackages);
        //     }
        //
        //     return allPackages;
        // }

        // private void AddDependenciesRecursively(PackageId packageId, HashSet<string> alreadyAdded,
        //     List<PackageId> result)
        // {
        //     var key = packageId.ToString();
        //
        //
        //     Logger.LogDebug("Adding Dependencies for {Package}. Found total {Count} packages.", key, result.Count);
        //
        //     if (!alreadyAdded.Contains(key))
        //     {
        //         alreadyAdded.Add(key);
        //
        //         result.Add(packageId);
        //
        //         var ndl = new NuGetDownloader
        //         {
        //             Logger = Logger
        //         };
        //
        //
        //         var cache = GetCache();
        //
        //         var name = packageId.AsFileName();
        //
        //         Nuspec nuspec;
        //
        //         if (cache.Contains(name))
        //         {
        //             nuspec = cache.Load<Nuspec>(name);
        //         }
        //         else
        //         {
        //             nuspec = ndl.GetNuspecObject(packageId);
        //
        //             GetCache().Cache(nuspec, name);
        //         }
        //
        //         foreach (var dependency in nuspec.Dependencies)
        //         {
        //             AddDependenciesRecursively(dependency, alreadyAdded, result);
        //         }
        //     }
        // }
        //
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

        private void UpdateIndex()
        {
            DownloadedItems = new HashSet<string>().LoadCached<HashSet<string>>(DownloadDirectory, "downloaded.json");

            if (DownloadedItems == null)
            {
                DownloadedItems = new HashSet<string>();
            }

            var removes = new List<string>();

            foreach (var fileName in DownloadedItems)
            {
                var path = Path.Combine(DownloadDirectory, fileName);

                if (!File.Exists(path))
                {
                    removes.Add(fileName);
                }
            }
            
            removes.ForEach( fileName => DownloadedItems.Remove(fileName));
            
            DownloadedItems.CacheInto(DownloadDirectory, "downloaded.json");
        }

        private bool MustDownload(PackageId package, bool fromScratch,bool savelower)
        {
            if (fromScratch)
            {
                return true;
            }

            // DownloadedItems = new HashSet<string>().LoadCached<HashSet<string>>(DownloadDirectory, "downloaded.json");
            //
            // //exceptionally 
            //
            // return DownloadedItems == null || !DownloadedItems.Contains(package.AsFileName(savelower));

            return !PackageExists(package, savelower);
        }

        private void MarkAsDownloaded(PackageId package,bool savelower)
        {
            DownloadedItems = new HashSet<string>().LoadCached<HashSet<string>>(DownloadDirectory, "downloaded.json");

            if (DownloadedItems == null)
            {
                DownloadedItems = new HashSet<string>();
            }

            DownloadedItems.Add(package.AsFileName(savelower));

            DownloadedItems.CacheInto(DownloadDirectory, "downloaded.json");
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