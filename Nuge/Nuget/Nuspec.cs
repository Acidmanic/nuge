using System.Collections.Generic;

namespace Meadow.Tools.Assistant.Nuget
{
    public class Nuspec:PackageId
    {
        
        public List<PackageId> Dependencies { get; set; }
    }
}