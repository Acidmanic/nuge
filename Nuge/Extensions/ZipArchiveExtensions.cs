using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace nuge.Extensions
{
    public static class ZipArchiveExtensions
    {
        public static List<ZipArchiveEntry> FindEntries(this ZipArchive archive, string regex)
        {
            var result = new List<ZipArchiveEntry>();

            var entries = archive.Entries;

            foreach (var entry in entries)
            {
                if (Regex.IsMatch(entry.FullName, regex))
                {
                    result.Add(entry);
                }
            }

            return result;
        }


        public static string AsText(this ZipArchiveEntry entry, System.Text.Encoding encoding)
        {
            var data = entry.AsBytes();

            return encoding.GetString(data);
        }

        public static byte[] AsBytes(this ZipArchiveEntry entry)
        {
            byte[] data;
            
            using (var stream = entry.Open())
            {
                var buffer = new MemoryStream();
                
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                
                stream.CopyTo(buffer);

                buffer.Seek(0, SeekOrigin.Begin);

                data = buffer.ToArray();
            }

            return data;
        }

        public static void ExtractToDirectory(this ZipArchiveEntry entry,string directory)
        {
            var entryFile =   new FileInfo(Path.Join(directory, entry.FullName));
            
            entryFile.Directory.CreateByParents();
            
            entry.ExtractToFile(entryFile.FullName);
        }
    }
}