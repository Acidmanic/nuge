using System;
using System.IO;
using Newtonsoft.Json;

namespace nuge.Extensions
{
    public static class ObjectExtensions
    {
        public static void CacheInto(this object value, string directory, string name)
        {
            var filePath = Path.Join(directory, name);

            var json = JsonConvert.SerializeObject(value);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.WriteAllText(filePath, json);
        }

        public static T LoadCached<T>(this object value, string directory, string name)
        {
            var filePath = Path.Join(directory, name);

            if (File.Exists(filePath))
            {

                try
                {
                    var json = File.ReadAllText(filePath);

                    var readValue = JsonConvert.DeserializeObject<T>(json);

                    return readValue;
                }
                catch (Exception e)
                {
                }
            }
            return (T) value;
        }
    }
}