namespace Meadow.Tools.Assistant.Nuget
{
    public class PackageId
    {
        public string Id { get; set; }
        
        public string Version { get; set; }

        public PackageId()
        {
            
        }

        public PackageId(string id,string version)
        {
            Id = id;
            Version = version;
        }

        public override string ToString()
        {
            return Id?.Trim() + ":" + Version?.Trim();
        }

        public string AsFileName()
        {
            return Id?.ToLower().Trim() + Version?.Trim() +".nupkg";
        }
    }
}