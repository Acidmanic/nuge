using Newtonsoft.Json;

namespace nuge.Nuget.Dtos
{
    public class PackageVersionDownload
    {
        [JsonProperty(PropertyName = "@id")] public string Id { get; set; }
        
        [JsonProperty(PropertyName = "@type")] public string[] Type { get; set; }
        
        public string CatalogEntry { get; set; }
        
        public bool Listed { get; set; }
     
        public string PackageContent { get; set; }
        
        public string Published { get; set; }
        
        public string Registration { get; set; }
        
        
    }
}
