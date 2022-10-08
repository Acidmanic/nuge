using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugZi
{
    public class PackageDependency
    {
        private ILogger logger = NullLogger.Instance;
        private CancellationToken cancellationToken = CancellationToken.None;

        private SourceCacheContext cache = new SourceCacheContext();
        private SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");



        public  Task<List<PackageId>> Get(string id, string version)
        {
            return Get(new PackageId
            {
                Id = id,
                Version = new VersionRange(new NuGetVersion(version))
            });
        }
        
       

        public async Task<List<PackageId>> Get(PackageId packageId)
        {
            var resource = await repository.GetResourceAsync<DependencyInfoResource>();


            var dependencyInfo = await resource.ResolvePackage(
                new PackageIdentity(packageId.Id,packageId.Version.MaxVersion),
                NuGetFramework.AnyFramework, 
                cache,
                logger,
                cancellationToken
            );


            var result = new List<PackageId>();
            
            foreach (var info in dependencyInfo.Dependencies)
            {
                Console.WriteLine(info.ToString());
                
                result.Add(new PackageId
                {
                    Id = info.Id,
                    Version = info.VersionRange
                });
            }

            return result;
        }
    }
}