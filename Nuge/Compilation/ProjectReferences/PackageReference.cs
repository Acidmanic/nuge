using Meadow.Tools.Assistant.Utils.ProjectReferences;

namespace nuge.Compilation.ProjectReferences
{
    public class PackageReference:Reference
    {
        public PackageReference(string projectFile, string nugetName, string nugetVersion):base(projectFile)
        {
            PackageName = nugetName;

            PackageVersion = nugetVersion;

            IsPackageReference = true;
            
        }
        
    }
}