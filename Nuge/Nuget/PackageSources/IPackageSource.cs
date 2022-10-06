using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;

namespace nuge.Nuget.PackageSources
{
    public interface IPackageSource
    {

        Result<byte[]> ProvidePackage(PackageId packageId);

        string GetNuspec(PackageId packageId);
    }
}