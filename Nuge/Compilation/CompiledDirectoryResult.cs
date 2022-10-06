using System.Collections.Generic;
using System.IO;
using System.Reflection;
using nuge.DotnetProject;

namespace nuge.Compilation
{
    public class CompiledDirectoryResult
    {
        
        
        public List<DotnetProjectInfo> ProjectsOnPath { get; set; }
        
        public List<DotnetProjectInfo> AllIncludedProjects { get; set; }
        
        public List<FileInfo> AllCSharpFiles { get; set; }
        
        public Assembly Assembly { get; set; }
        
    }
}