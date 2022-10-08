using NuGet.Versioning;

namespace NugZi
{
    public class PackageId
    {
        public string Id { get; set; }
        
        public VersionRange Version { get; set; }
    }
}