using System.Collections.Generic;
using Newtonsoft.Json;

namespace nuge.Nuget.Dtos
{
    public class PackageInfo
    {
        [JsonProperty(PropertyName = "@id")] public string AtId { get; set; }
        
        [JsonProperty(PropertyName = "@type")] public string Type { get; set; }
        
        public string Registration { get; set; }
        
        public string Id  { get; set; }
        
        public string Version { get; set; }
        
        public string Summary { get; set; }
        
        public string Title { get; set; }
        
        public string IconUrl { get; set; }
        
        public string ProjectUrl { get; set; }
        
        public string LicenseUrl { get; set; }
        
        public List<PackageVersionHeader> Versions { get; set; }


    }
}