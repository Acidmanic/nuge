using System;
using System.IO;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;

namespace nuge.Nuget.PackageSources
{
    public class LocalDirectorySource : IPackageSource
    {
        private readonly string _localDirectory;

        public LocalDirectorySource(string localDirectory)
        {
            _localDirectory = localDirectory;
        }


        public Result<byte[]> ProvidePackage(PackageId packageId)
        {
            var packageFiles = new DirectoryInfo(_localDirectory).EnumerateFiles(packageId.Id + "*.nupkg");

            int removingIntro = packageId.Id.Length + 1;
            int removingOutro = ".nupkg".Length;

            System.Version latest = new System.Version(0, 0, 0);
            FileInfo latestFile = null;

            foreach (var packageFile in packageFiles)
            {
                var fileVersion = packageFile.Name.Substring(removingIntro,
                    packageFile.Name.Length - removingIntro - removingOutro);

                if (fileVersion == packageId.Version)
                {
                    return new Result<byte[]>().Succeed(File.ReadAllBytes(packageFile.FullName));
                }

                System.Version version;

                if (System.Version.TryParse(fileVersion, out version))
                {
                    if (version.CompareTo(latest) > 0)
                    {
                        latest = version;

                        latestFile = packageFile;
                    }
                }
            }

            if (latestFile != null)
            {
                return new Result<byte[]>().Succeed(File.ReadAllBytes(latestFile.FullName));
            }

            return new Result<byte[]>().FailAndDefaultValue();
        }

        public string GetNuspec(PackageId packageId)
        {
            var readFile = ProvidePackage(packageId);

            if (readFile.Success)
            {
                var package = new NugetPackage(readFile.Value);

                return package.NuspecXml;
            }

            return null;
        }
    }
}