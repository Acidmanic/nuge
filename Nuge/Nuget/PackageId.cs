namespace nuge.Nuget
{
    public class PackageId
    {
        public string Id { get; set; }
        
        public Version Version { get; set; }

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
            return Id?.Trim() + ":" + Version?.ToString()?.Trim();
        }

        public string AsFileName(bool lower = true)
        {
            if (lower)
            {
                return Id?.ToLower().Trim() + "." + Version?.ToString()?.Trim() +".nupkg";    
            }
            else
            {
                return Id?.Trim() +"." + Version?.ToString()?.Trim() +".nupkg";
            }
            
        }
    }
}