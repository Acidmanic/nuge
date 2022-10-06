using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Meadow.Tools.Assistant.Utils.ProjectReferences;
using nuge.Compilation.ProjectReferences;
using nuge.Utils;

namespace nuge.DotnetProject
{
    public class DotnetProjectInfo
    {
        private readonly DirectoryInfo _directory;
        private readonly FileInfo _projectFile;

        public DotnetProjectInfo(FileInfo projectFile)
        {
            _projectFile = projectFile;

            _directory = projectFile.Directory;
        }

        public FileInfo ProjectFile => _projectFile;

        public string GetRootNamespace()
        {
            if (_projectFile.Exists)
            {
                return GetRootNamespace(_projectFile.FullName);
            }

            return "";
        }



        private PackageReference SelectPackageReference(Dictionary<string, string> dic)
        {
            if (dic.ContainsKey("Include") && dic.ContainsKey("Version"))
            {
                return new PackageReference(_projectFile.FullName,
                    dic["Include"], dic["Version"]);
            }

            return null;
        }

        public List<Reference> GetAllReferences()
        {
            var references = new List<Reference>();

            var projectRoot = GetProjectNode(_projectFile.FullName);

            if (projectRoot != null)
            {
                var packages = new XmlReadHelper()
                    .ExtractMappedNodeData(projectRoot, "PackageReference")
                    .Select(SelectPackageReference).Where(r => r!=null);

                var frameworkReferences = new XmlReadHelper()
                    .ExtractMappedNodeData(projectRoot, "FrameworkReference")
                    .Select(SelectPackageReference).Where(r => r!=null);
                
                var targetsName = new XmlReadHelper().FindValueFor(projectRoot, "TargetFramework");

                var projects = new XmlReadHelper().ExtractMappedNodeData(projectRoot, "ProjectReference")
                    .Select(map => new ProjectReference(_projectFile.FullName, GetReferencedProjectFile(map)));

                references.AddRange(packages);
                references.AddRange(projects);
                references.AddRange(frameworkReferences);

                if (!string.IsNullOrEmpty(targetsName))
                {
                    references.Add(new SdkReference(_projectFile.FullName, targetsName));
                }
            }

            return references;
        }

        private FileInfo GetReferencedProjectFile(Dictionary<string, string> xmlAttValue)
        {
            string relativeReference = xmlAttValue["Include"];

            relativeReference = relativeReference.Replace('\\', Path.DirectorySeparatorChar);
            relativeReference = relativeReference.Replace('/', Path.DirectorySeparatorChar);

            var appended = new FileInfo(
                Path.Join(_directory.FullName, relativeReference)
            );

            return appended;
        }

        private string GetProjectFile(string directory)
        {
            var files = Directory.GetFiles(directory);

            foreach (var file in files)
            {
                if (file.ToLower().EndsWith(".csproj") || file.ToLower().EndsWith(".vbproj"))
                {
                    return file;
                }
            }

            var directories = Directory.EnumerateDirectories(directory);

            foreach (var dir in directories)
            {
                var projFile = GetProjectFile(dir);

                if (projFile != null)
                {
                    return projFile;
                }
            }

            return null;
        }


        private string GetRootNamespace(string projectFile)
        {
            var foundValues = FindNodeValue(projectFile, "RootNamespace");

            if (foundValues.Count == 0)
            {
                var file = new FileInfo(projectFile);

                var name = file.Name;

                if (!string.IsNullOrEmpty(file.Extension))
                {
                    name = name.Substring(0, name.Length - file.Extension.Length);
                }

                return name;
            }

            return foundValues.FirstOrDefault() ?? "";
        }

        private List<string> FindNodeValue(string projectFile, string nodeName)
        {
            var foundValues = new List<string>();

            if (!string.IsNullOrEmpty(projectFile) && File.Exists(projectFile))
            {
                var root = GetProjectNode(projectFile);

                foundValues = new XmlReadHelper().ExtractValues(root, nodeName);
            }

            return foundValues;
        }

        private XmlNode GetProjectNode(string projectFile)
        {
            if (!string.IsNullOrEmpty(projectFile) && File.Exists(projectFile))
            {
                XmlDocument doc = new XmlDocument();

                var content = File.ReadAllText(projectFile);

                doc.LoadXml(content);

                XmlNode root = doc.FirstChild;

                return root;
            }

            return null;
        }

        private class SourceFileListObserver : ListFilesObserver
        {
            private readonly string _badObjDir;
            private readonly string _badBinDir;

            public SourceFileListObserver(DirectoryInfo projectRoot)
            {
                _badObjDir = Path.Combine(projectRoot.FullName, "obj").ToLower();
                _badBinDir = Path.Combine(projectRoot.FullName, "bin").ToLower();
            }

            public override void OnFile(DirectoryInfo location, FileInfo file)
            {
                var caseIgnorePath = location.FullName.ToLower();

                if (caseIgnorePath.StartsWith(_badBinDir) || caseIgnorePath.StartsWith(_badObjDir))
                {
                    return;
                }

                base.OnFile(location, file);
            }
        }

        public List<FileInfo> GetSourceCodes()
        {
            return new FileSystemSearchFiles().Search(_directory, "*.cs", new SourceFileListObserver(_directory));
        }

        private class ProjectResult : ISearchObserver<List<DotnetProjectInfo>>
        {
            public List<DotnetProjectInfo> Result { get; } = new List<DotnetProjectInfo>();

            public void OnDirectory(DirectoryInfo dir)
            {
            }

            public void OnFile(DirectoryInfo location, FileInfo file)
            {
                if (file.Extension == ".csproj")
                {
                    Result.Add(new DotnetProjectInfo(file));
                }
            }
        }

        public static List<DotnetProjectInfo> FindProjects(string directory)
        {
            return new FileSystemSearch<List<DotnetProjectInfo>>()
                .Search(directory, "*.*proj", new ProjectResult());
        }

        public static DotnetProjectInfo FromDirectory(string directory)
        {
            var foundItems = new FileSystemSearch<List<DotnetProjectInfo>>()
                .Scan(directory, "*.csproj", new ProjectResult());

            if (foundItems.Count == 0)
            {
                return null;
            }

            return foundItems.FirstOrDefault();
        }
    }
}