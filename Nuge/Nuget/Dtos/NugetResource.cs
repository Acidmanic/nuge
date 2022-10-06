using Newtonsoft.Json;

namespace nuge.Nuget.Dtos
{
    public class NugetResource
    {
        [JsonProperty(PropertyName = "@id")] public string Id { get; set; }
        [JsonProperty(PropertyName = "@type")] public string Type { get; set; }

        public string Comment { get; set; }
    }
}