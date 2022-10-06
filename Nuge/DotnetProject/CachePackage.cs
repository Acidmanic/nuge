using System.IO;
using Meadow.Tools.Assistant.Nuget;

namespace nuge.DotnetProject
{
    public class CachePackage
    {
        public string ByNameDirectory { get; set; }

        public string ByVersionDirectory { get; set; }

        public string LibDirectory => Path.Combine(ByVersionDirectory, "lib");

        public static CachePackage FromDirectory(string directory, PackageId packageInfo)
        {
            var name = Path.Join(directory, packageInfo.Id);

            var version = Path.Join(name, packageInfo.Version);

            return new CachePackage
            {
                ByNameDirectory = name,
                ByVersionDirectory = version
            };
        }

        public bool Exists()
        {
            return Directory.Exists(ByNameDirectory) &&
                   Directory.Exists(ByVersionDirectory);
        }
    }
}