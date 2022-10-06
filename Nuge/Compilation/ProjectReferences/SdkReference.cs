using System.Collections.Generic;
using Meadow.Tools.Assistant.Utils.ProjectReferences;

namespace nuge.Compilation.ProjectReferences
{
    public class SdkReference : Reference
    {
        private static readonly Dictionary<string, string> AssemblyNameBySkd = new Dictionary<string, string>
        {
            {"netstandard2.0", "netstandard"},
            {"netstandard2.1", "netstandard"},
            {"netcoreapp3.1", "netcoreapp"},
        };

        private static readonly Dictionary<string, string> AssemblyVersionBySkd = new Dictionary<string, string>
        {
            {"netstandard2.0", "2.1.0.0"},
            {"netstandard2.1", "2.1.0.0"},
            {"netcoreapp3.1", "3.1.0.0"},
        };

        public string TargetFramework { get; protected set; }

        public SdkReference(string projectFile, string targetFramework) : base(projectFile)
        {
            TargetFramework = targetFramework;
            IsSdk = true;
        }

        public override string ToString()
        {
            return TargetFramework;
        }
    }
}