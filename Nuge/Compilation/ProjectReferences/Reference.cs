using System.IO;

namespace Meadow.Tools.Assistant.Utils.ProjectReferences
{
    public abstract class Reference
    {
        public bool IsProjectReference { get; protected set; }
        
        public bool IsSdk { get; protected set; }
        public FileInfo ReferenceProjectFile { get; protected set; }
        
        
        public bool IsPackageReference { get; protected set; }
        
        public string PackageName { get; protected  set; }
        
        public string PackageVersion { get; protected set; }
        
        public FileInfo OwnerProjectFile { get; protected set; }

        public Reference(string projectFile)
        {
            OwnerProjectFile = projectFile==null?null: new FileInfo(projectFile);
        }

        public override string ToString()
        {
            if (IsPackageReference)
            {
                return PackageName + ":" + PackageVersion;
            }

            if (IsProjectReference)
            {
                return ReferenceProjectFile.Name;
            }

            return base.ToString();
        }
    }
}