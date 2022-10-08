using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NugZi
{
    public class PackageMetadata
    {
        public async Task Get(string packageId,string version)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

            IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
                packageId,
                includePrerelease: true,
                includeUnlisted: false,
                cache,
                logger,
                cancellationToken);

            // foreach (IPackageSearchMetadata package in packages)
            // {
            //     Console.WriteLine($"Version: {package.Identity.Version}");
            //     Console.WriteLine($"Listed: {package.IsListed}");
            //     Console.WriteLine($"Tags: {package.Tags}");
            //     Console.WriteLine($"Description: {package.Description}");
            // }

            var first = packages.First(
                peta => peta.Identity.Version.OriginalVersion==version);

            foreach (var dependencySet in first.DependencySets)
            {
                foreach (var package in dependencySet.Packages)
                {
                    Console.WriteLine("Found dependency: " + package.ToString());

                }
            }
        }
    }
}