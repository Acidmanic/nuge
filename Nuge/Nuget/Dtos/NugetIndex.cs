using System;
using System.Collections.Generic;

namespace nuge.Nuget.Dtos
{
    public class NugetIndex
    {
        public string Version { get; set; }

        public List<NugetResource> Resources { get; set; } = new List<NugetResource>();


        public bool IsEmpty => string.IsNullOrEmpty(Version) || Resources == null || Resources.Count == 0;

    }
}