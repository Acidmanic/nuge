using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using nuge.Compilation.ProjectReferences;
using nuge.DotnetProject;
using nuge.Extensions;
using nuge.Nuget.PackageSources;
using nuge.Utils;

namespace nuge.Nuget
{
    public class Nuget
    {
        private readonly string _cacheDirectory;

        private readonly BatchPackageSource _packageSource;
        private readonly ILogger _logger;


        public Nuget(string cacheDirectory) : this(cacheDirectory, NullLogger.Instance)
        {
        }

        public Nuget(string cacheDirectory, ILogger logger)
        {
            _cacheDirectory = cacheDirectory;
            _logger = logger;
            _packageSource = new BatchPackageSource().Add(new NuGetDownloader());
        }

        public Nuget AddPackageSource(IPackageSource packageSource)
        {
            this._packageSource.Add(packageSource);

            return this;
        }

        public Nuget AddLocalDirectoryPackageSource(string directory)
        {
            _packageSource.Add(new LocalDirectorySource(directory));

            return this;
        }

        private async Task<Result<byte[]>> DownloadFile(string url)
        {
            using (var client = new HttpClient())
            {
                using (var result = await client.GetAsync(url))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        var bytes = await result.Content.ReadAsByteArrayAsync();

                        return new Result<byte[]>().Succeed(bytes);
                    }
                }
            }

            return new Result<byte[]>().FailAndDefaultValue();
        }

        private Nuspec DownloadNuspec(PackageId packageId)
        {
            var downloadResult = _packageSource.GetNuspec(packageId);

            if (!string.IsNullOrEmpty(downloadResult))
            {
                var nuspec = new Nuspec().LoadXml(downloadResult);

                return nuspec;
            }

            return null;
        }

        private Nuspec Cache(byte[] data)
        {
            var package = new NugetPackage(data);

            var packageInfo = package.Nuspec;

            var cachePackage = CachePackage.FromDirectory(_cacheDirectory, packageInfo);


            if (!cachePackage.Exists())
            {
                package.ExtractInto(cachePackage);

                WriteNugetPackage(cachePackage, data, packageInfo);
            }

            return packageInfo;
        }

        private void WriteNugetPackage(CachePackage cachePackage, byte[] data, PackageId packageInfo)
        {
            string fileName = packageInfo.Id + packageInfo.Version + ".nupkg";

            fileName = Path.Join(cachePackage.ByVersionDirectory, fileName);

            var packageFile = new FileInfo(fileName);

            if (packageFile.Exists)
            {
                packageFile.Delete();
            }

            File.WriteAllBytes(packageFile.FullName, data);
        }


        private bool PackageExists(PackageReference packageInfo)
        {
            var packageDir = Path.Join(_cacheDirectory, packageInfo.PackageName);

            if (Directory.Exists(packageDir))
            {
                var versionDir = Path.Join(packageDir, packageInfo.PackageVersion);

                if (Directory.Exists(versionDir))
                {
                    return true;
                }
            }

            return false;
        }


        private Nuspec GetNuspecAnyway(Dictionary<string, Nuspec> cached, PackageId packageId)
        {
            var hash = packageId.ToString().ToLower();

            if (cached.ContainsKey(hash))
            {
                return cached[hash];
            }

            var nuspec = GetNuspecAnyway(packageId);

            if (nuspec != null)
            {
                cached.Add(hash, nuspec);
            }

            return nuspec;
        }

        private Nuspec GetNuspecAnyway(PackageId packageId)
        {
            var existence = CheckPackageExistence(packageId.Id, packageId.Version);

            if (existence.Success)
            {
                return existence.Value;
            }

            return DownloadNuspec(packageId);
        }


        private List<Nuspec> BuildDependencyTree(PackageId packageId)
        {
            var result = new List<Nuspec>();

            var cache = new Dictionary<string, Nuspec>();

            cache = cache.LoadCached<Dictionary<string, Nuspec>>(_cacheDirectory, "nuspec.cache.json");

            BuildDependencyTree(packageId, cache, new List<string>(), result);

            cache.CacheInto(_cacheDirectory, "nuspec.cache.json");

            return result;
        }


        /// <summary>
        /// Searches recursively for package dependencies and list them into result.
        /// </summary>
        /// <param name="packageId">package to be searched for</param>
        /// <param name="cached">any cached nuspec can help speeding up the process of reading nuspecs</param>
        /// <param name="hashes">keeps track of already investigated packages to avoid loops in circular dependencies. </param>
        /// <param name="result">the list of overall found dependencies</param>
        private void BuildDependencyTree(PackageId packageId, Dictionary<string, Nuspec> cached, List<string> hashes,
            List<Nuspec> result)
        {
            var hash = packageId.ToString().ToLower();

            if (hashes.Contains(hash))
            {
                return;
            }
            // the package is not investigated yet

            var nuspec = GetNuspecAnyway(cached, packageId);

            if (nuspec == null)
            {
                _logger.LogError($"There was a problem getting information for package: {packageId}");

                return;
            }
            // package is valid and yet not investigated

            _logger.LogError($". Received nuspec metadata for {nuspec}.");

            hashes.Add(hash);

            result.Add(nuspec);

            nuspec.Dependencies.ForEach(package => BuildDependencyTree(package, cached, hashes, result));
        }


        private Nuspec ProvidePackageWithDependencies(PackageId packageId)
        {
            _logger.LogInformation($"Building Dependency Tree for {packageId}");

            var dependencies = BuildDependencyTree(packageId);

            _logger.LogInformation($"Restoring {dependencies.Count} Dependencies for {packageId}.");

            foreach (var dependency in dependencies)
            {
                Result<Nuspec> existence = CheckPackageExistence(packageId.Id, packageId.Version);

                if (existence.Success)
                {
                    _logger.LogInformation($"[OK] Package {dependency} already cached.");
                }
                else
                {
                    _logger.LogInformation($"[DL] Installing {dependency} into {_cacheDirectory}...");

                    var downloadResult = _packageSource.ProvidePackage(dependency);

                    if (downloadResult.Success)
                    {
                        Cache(downloadResult.Value);
                    }
                    else
                    {
                        _logger.LogError($"[ER] Unable to download package {dependency}");
                    }
                }
            }

            return new Nuspec
            {
                Id = packageId.Id,
                Version = packageId.Version,
                Dependencies = new List<PackageId>(dependencies)
            };
        }

        private List<FileInfo> ProvidePackage(string packageName, string packageVersion)
        {
            var assemblies = new List<FileInfo>();

            var package = new PackageId(packageName, packageVersion);

            var fullPackage = ProvidePackageWithDependencies(package);

            LoadAssemblies(fullPackage, assemblies);

            fullPackage.Dependencies.ForEach(p => LoadAssemblies(p, assemblies));

            return assemblies;
        }

        private bool Containes(List<FileInfo> files, FileInfo file)
        {
            var pathToFind = file.FullName;


            foreach (var fileInfo in files)
            {
                if (fileInfo.FullName == pathToFind)
                {
                    return true;
                }
            }

            return false;
        }

        private void LoadAssemblies(PackageId package, List<FileInfo> runtimes)
        {
            var cachePackage = CachePackage.FromDirectory(_cacheDirectory, package);

            if (cachePackage.Exists())
            {
                var dlls = new FileSystemSearch<List<FileInfo>>()
                    .Search(cachePackage.LibDirectory, "*.dll", new ListFilesObserver());

                foreach (var dll in dlls)
                {
                    try
                    {
                        var assemblyName = AssemblyName.GetAssemblyName(dll.FullName);

                        //var metadata = MetadataReference.CreateFromFile(dll.FullName);

                        if (!Containes(runtimes, dll))
                        {
                            runtimes.Add(dll);
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        private Result<Nuspec> CheckPackageExistence(string packageName, string packageVersion)
        {
            var packageNameDirectory = Path.Join(_cacheDirectory, packageName);

            if (Directory.Exists(packageNameDirectory))
            {
                var versionDirectory = Path.Join(packageNameDirectory, packageVersion);

                var version = packageVersion;

                if (!string.IsNullOrEmpty(packageVersion) || !Directory.Exists(versionDirectory))
                {
                    version = GetLatestVersion(packageNameDirectory);

                    versionDirectory = Path.Join(packageNameDirectory, version);
                }

                if (version != null)
                {
                    var nuspecFile = Path.Combine(versionDirectory, packageName + ".nuspec");

                    if (File.Exists(nuspecFile))
                    {
                        var nuspecContent = File.ReadAllText(nuspecFile);

                        Nuspec package = new Nuspec().LoadXml(nuspecContent);

                        return new Result<Nuspec>().Succeed(package);
                    }
                }
            }

            return new Result<Nuspec>().FailAndDefaultValue();
        }


        private string GetLatestVersion(string packageNameDirectory)
        {
            var directory = new DirectoryInfo(packageNameDirectory);

            var versionDirectories = directory.EnumerateDirectories().ToList();

            var latestVersion = versionDirectories[0].Name;

            for (int i = 1; i < versionDirectories.Count; i++)
            {
                var version = versionDirectories[i].Name;

                if (CompareVersionStrings(latestVersion, version) < 0)
                {
                    latestVersion = version;
                }
            }

            return latestVersion;
        }


        public List<FileInfo> GetCompilingReferences(string packageName, string packageVersion)
        {
            return ProvidePackage(packageName, packageVersion);
        }


        private int CompareVersionStrings(string first, string second)
        {
            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(second))
            {
                return 0;
            }

            if (string.IsNullOrEmpty(first))
            {
                return -1;
            }

            if (string.IsNullOrEmpty(second))
            {
                return 1;
            }

            var firstParsed = System.Version.TryParse(first, out var firstVersion);
            var secondsParsed = System.Version.TryParse(second, out var secondsVersion);

            if (!firstParsed && !secondsParsed)
            {
                return 0;
            }

            if (!firstParsed)
            {
                return -1;
            }

            if (!secondsParsed)
            {
                return 1;
            }

            return firstVersion.CompareTo(secondsVersion);
        }
    }
}