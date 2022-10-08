using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugZi
{
    public class ListVersions
    {
        public async Task List()
        {
            var logger =  NullLogger.Instance;
            
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                "Newtonsoft.Json",
                cache,
                logger,
                cancellationToken);

            foreach (NuGetVersion version in versions)
            {
                Console.WriteLine($"Found version {version}");
            }
        }
    }
}