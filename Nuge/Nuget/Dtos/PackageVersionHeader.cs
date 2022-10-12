using Newtonsoft.Json;

namespace nuge.Nuget.Dtos
{
    public class PackageVersionHeader
    {
        [JsonProperty(PropertyName = "@id")] public string DownloadIndex { get; set; }
        
        public string Version { get; set; }
        
    }
}