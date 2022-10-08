using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Meadow.Tools.Assistant.Nuget;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;
using Newtonsoft.Json;
using nuge.Extensions;

namespace nuge.Utils
{
    public class JsonCache
    {

        public ILogger Logger { get; set; } = new LoggerAdapter(s => { });

        public JsonCache(string directory)
        {
            Directory = directory;
            
            if(!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }

        private class CacheIndex
        {
            public Dictionary<string, string> IdByName { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, string> NameById { get; set; }= new Dictionary<string, string>();

            public string Add(string name)
            {
                var id = Guid.NewGuid().ToString();
                
                IdByName.Add(name,id);
                NameById.Add(id,name);

                return id;
            }

            public string GetName(string id)
            {
                return NameById[id];
            }
            
            public string GetId(string name)
            {
                return IdByName[name];
            }

            public bool ContainsName(string name)
            {
                return IdByName.ContainsKey(name);
            }

            public void RemoveById(string rId)
            {
                if (NameById.ContainsKey(rId))
                {
                    var name = NameById[rId];

                    NameById.Remove(rId);

                    IdByName.Remove(name);
                }

                if (IdByName.Values.Contains(rId))
                {
                    var name = IdByName
                        .Where(kv => kv.Value == rId)
                        .Select(kv => kv.Key).First();

                    IdByName.Remove(name);
                }
            }

            public bool ContainsId(string id)
            {
                return NameById.ContainsKey(id);
            }
        }
        
        public string Directory { get; }


        public void Cache<T>(T value,string name)
        {
            var index = new CacheIndex().LoadCached<CacheIndex>(Directory,"index.json");

            if (!index.ContainsName(name))
            {
                var id = index.Add(name);
                
                index.CacheInto(Directory,"index.json");
                
                value.CacheInto(Directory,id);
            }
        }

        public bool Contains(string name)
        {
            var index = new CacheIndex().LoadCached<CacheIndex>(Directory,"index.json");

            return index.ContainsName(name);
        }

        public T Load<T>(string name)
        {
            var index = new CacheIndex().LoadCached<CacheIndex>(Directory,"index.json");

            if (index.ContainsName(name))
            {
                var id = index.IdByName[name];

                var json = File.ReadAllText(Path.Join(Directory, id));

                var value = JsonConvert.DeserializeObject<T>(json);

                return value;

            }

            return default;
        }

        public void ClearJunks()
        {
            // delete 0 files
            var files = new DirectoryInfo(Directory).GetFiles();

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    Logger.LogWarning("Deleted Junk File {File}",file.Name);
                    
                    file.Delete();
                }
            }
            var index = new CacheIndex().LoadCached<CacheIndex>(Directory,"index.json");
            // delete null nuspecs
            foreach (var idByName in index.IdByName)
            {
                var nuspec = new Nuspec().LoadCached<Nuspec>(Directory, idByName.Value);

                if (string.IsNullOrEmpty(nuspec.Id) || string.IsNullOrEmpty(nuspec.Version.ToString()))
                {
                    Logger.LogWarning("DELETED {Id} , {Version} cache because of being coruppted.",
                        nuspec.Id,nuspec.Version);
                    
                    File.Delete(Path.Combine(Directory,idByName.Value));
                }
            }
            var removings = new List<string>();
            // remove entries with no file
            foreach (var id in index.NameById.Keys)
            {
                var path = Path.Combine(Directory, id);

                if (!File.Exists(path))
                {
                    Logger.LogWarning("Removed un present {File} from indexing",index.GetName(id));
                    
                    removings.Add(id);
                }
            }
            removings.ForEach(rId => index.RemoveById(rId));
            
            index.CacheInto(Directory,"index.json");
            // remove files with no  entry
            files = new DirectoryInfo(Directory).GetFiles();

            foreach (var file in files)
            {
                if (!file.Name.Contains("."))
                {
                    if (file.Name.Length == 36)
                    {
                        var id = file.Name;

                        if (!index.ContainsId(id))
                        {
                            Logger.LogWarning("Deleted Junk File {File}",file.Name);
                            
                            file.Delete();
                        }
                    }
                }
            }
        }
    }
}