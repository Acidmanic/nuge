using System.Collections.Generic;
using System.IO;
using nuge.Compilation.ProjectReferences;

namespace nuge.DotnetProject
{
    public class MergedProject
    {
        public List<FileInfo> SourceFiles { get; private set; } = new List<FileInfo>();
        public List<PackageReference> PackageReferences { get; private set; } = new List<PackageReference>();

        public List<SdkReference> Sdks { get; private set; } = new List<SdkReference>();

        private readonly List<string> _includedProjects = new List<string>();
        private readonly List<string> _includedSources = new List<string>();

        public List<DotnetProjectInfo> InvolvedProjects { get; } = new List<DotnetProjectInfo>();

        public MergedProject()
        {
        }

        public MergedProject(DotnetProjectInfo singleProject)
        {
            Add(singleProject);
        }

        public MergedProject(IEnumerable<DotnetProjectInfo> projects)
        {
            foreach (var dotnetProjectInfo in projects)
            {
                Add(dotnetProjectInfo);
            }
        }

        public void Add(DotnetProjectInfo project)
        {
            var projectKey = project.ProjectFile.FullName.ToLower();

            if (!_includedProjects.Contains(projectKey))
            {
                var sources = project.GetSourceCodes();

                _includedProjects.Add(projectKey);
                
                InvolvedProjects.Add(project);

                AddSources(sources);

                var references = project.GetAllReferences();

                foreach (var reference in references)
                {
                    if (reference is ProjectReference projectReference)
                    {
                        var refProject = new DotnetProjectInfo(projectReference.ReferenceProjectFile);

                        Add(refProject);
                    }
                    else if (reference is PackageReference package)
                    {
                        PackageReferences.Add(package);
                    }
                    else if (reference is SdkReference sdkReference)
                    {
                        Sdks.Add(sdkReference);
                    }
                }
            }
        }

        private void AddSources(List<FileInfo> sources)
        {
            foreach (var source in sources)
            {
                var key = source.FullName;

                if (!_includedSources.Contains(key))
                {
                    _includedSources.Add(key);

                    SourceFiles.Add(source);
                }
            }
        }
    }
}