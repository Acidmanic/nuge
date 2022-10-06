using System.Collections.Generic;
using Acidmanic.Utilities.Results;
using Meadow.Tools.Assistant.Nuget;

namespace nuge.Nuget.PackageSources
{
    public class BatchPackageSource : IPackageSource
    {
        private readonly List<IPackageSource> _items;

        public BatchPackageSource()
        {
            _items = new List<IPackageSource>();
        }

        public BatchPackageSource Add(IPackageSource packageSource)
        {
            _items.Add(packageSource);

            return this;
        }

        public Result<byte[]> ProvidePackage(PackageId packageId)
        {
            foreach (var packageSource in _items)
            {
                var result = packageSource.ProvidePackage(packageId);

                if (result.Success)
                {
                    return result;
                }
            }

            return new Result<byte[]>().FailAndDefaultValue();
        }

        public string GetNuspec(PackageId packageId)
        {
            foreach (var packageSource in _items)
            {
                var result = packageSource.GetNuspec(packageId);

                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}