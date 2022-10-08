using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugZi
{
    public class PackageTree
    {
        private ILogger logger = NullLogger.Instance;
        private CancellationToken cancellationToken = CancellationToken.None;

        private SourceCacheContext cache = new SourceCacheContext();
        private SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");


        public async Task<List<PackageId>> Get(string id, string version)
        {
            var dependencies = new ConcurrentQueue<PackageId>();

            // await Get(packageId, dependencies, new HashSet<string>());

            await GetRecursiveDependenciesCore(id, version, dependencies);

            return dependencies.ToList();           
        }


        public async Task Get(PackageId packageId, List<PackageId> dependencies, HashSet<string> alreadyadded)
        {
            PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

            IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
                packageId.Id,
                includePrerelease: true,
                includeUnlisted: false,
                cache,
                logger,
                cancellationToken);

            var first = packages.First(
                peta => packageId.Version.Satisfies(peta.Identity.Version));

            foreach (var dependencySet in first.DependencySets)
            {
                foreach (var package in dependencySet.Packages)
                {
                    var key = package.ToString();


                    if (alreadyadded.Contains(key))
                    {
                        Console.Write(".");
                    }
                    else
                    {
                        Console.WriteLine("Found dependency: " + key);

                        alreadyadded.Add(key);

                        var pid = new PackageId
                        {
                            Id = package.Id,
                            Version = package.VersionRange
                        };

                        dependencies.Add(pid);

                        await Get(pid, dependencies, alreadyadded);
                    }
                }
            }
        }


        private async Task GetRecursiveDependenciesCore(string id, string version,
            ConcurrentQueue<PackageId> dependencies)
        {
            //var sourceRepository = new SourceRepository(new PackageSource(NugetOrgSource), Repository.Provider.GetCoreV3());
            var dependencyResource = await repository.GetResourceAsync<DependencyInfoResource>(CancellationToken.None);
            var package = await dependencyResource.ResolvePackage(new PackageIdentity(id, new NuGetVersion(version)),
                NuGetFramework.AnyFramework, cache, logger,
                CancellationToken.None);
            if (package == null)
            {
                throw new InvalidOperationException("Could not locate dependency!");
            }

            foreach (var dependency in package.Dependencies)
            {
                Console.WriteLine($"Ass {dependency.ToString()}");
                dependencies.Enqueue(new PackageId
                {
                    Id = dependency.Id,
                    Version = dependency.VersionRange
                });
            }

            await Task.WhenAll(package.Dependencies.Select(d =>
                GetRecursiveDependenciesCore(d.Id, d.VersionRange.MinVersion.ToNormalizedString(), dependencies)));
        }
    }
}