using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using nuge.Utils;

namespace nuge.Compilation.ProjectReferences
{
    public class ReferenceResolver
    {
        public MetadataReference Resolve(PackageReference reference)
        {
            var dllsUnder = new FileSystemSearchFiles().Search(
                reference.OwnerProjectFile.Directory.FullName,
                "*.dll", new ListFilesObserver());

            var failureMessage = "Unable to find a dll.";

            foreach (var dll in dllsUnder)
            {
                var assName = TryLoadAsAssembly(dll);

                if (assName != null && assName.Name == reference.PackageName)
                {
                    var assVersion = assName.Version?.ToString();
                    var refVersion = reference.PackageVersion;


                    // failureMessage = $"Found dll ({assName.Name}) version is not match: {assVersion} vs {refVersion}";
                    //
                    // if (VersionMatch(assName.Version, refVersion))
                    // {
                        try
                        {
                            return MetadataReference.CreateFromFile(dll.FullName);
                        }
                        catch (Exception e)
                        {
                            failureMessage = e.Message;
                        }
                    //}
                }
            }

            Console.WriteLine(
                failureMessage = $"Unable to load reference for {reference.PackageName}: {failureMessage}");
            return null;
        }

        public MetadataReference Resolve(SdkReference reference)
        {
            return null;
        }

        private bool VersionMatch(Version assemblyVersion, string referenceVersionString)
        {
            if (string.IsNullOrEmpty(referenceVersionString))
            {
                return true;
            }

            Version referenceVersion;
            try
            {
                referenceVersion = Version.Parse(referenceVersionString);
                
            }
            catch (Exception e)
            {
                return true;
            }

            var comp = referenceVersion.CompareTo(assemblyVersion);

            return comp > -1;
        }

        private bool VersionMatch(string assemblyVersion, string referenceVersion)
        {
            if (assemblyVersion == null)
            {
                return referenceVersion == null;
            }

            if (referenceVersion == null)
            {
                return false;
            }

            if (assemblyVersion.StartsWith(referenceVersion) || referenceVersion.StartsWith(assemblyVersion))
            {
                return true;
            }

            var comp = String.Compare(assemblyVersion, referenceVersion, StringComparison.Ordinal);

            return comp >= 0;
        }


        private AssemblyName TryLoadAsAssembly(FileInfo dll)
        {
            try
            {
                return AssemblyName.GetAssemblyName(dll.FullName);
            }
            catch (Exception e)
            {
            }

            return null;
        }

        private bool NullabilityEquals<T>(T o1, T o2)
        {
            if (o1 == null && o2 == null)
            {
                return true;
            }

            return !(o1 == null || o2 == null);
        }

        private bool StringEqualsNullChecked(string s1, string s2)
        {
            return NullabilityEquals(s1, s2) && s1 == s2;
        }
    }
}