using System.Collections.Generic;
using nuge.Nuget;

namespace Meadow.Tools.Assistant.Nuget
{
    public class Nuspec:PackageId
    {
        
        public List<PackageId> Dependencies { get; set; }
    }
}