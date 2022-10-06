using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Meadow.Tools.Assistant.Nuget;
using nuge.DotnetProject;
using nuge.Extensions;
using nuge.Utils;

namespace nuge.Nuget
{
    public class NugetPackage
    {
        private readonly ZipArchive _packageContent;

        public NugetPackage(ZipArchive packageContent)
        {
            _packageContent = packageContent;
        }

        public NugetPackage(byte[] contentData)
        {
            var stream = new MemoryStream(contentData);

            stream.Seek(0, SeekOrigin.Begin);

            _packageContent = new ZipArchive(stream);
        }

        public NugetPackage(string nugetFile) : this(File.ReadAllBytes(nugetFile))
        {
        }


        public Nuspec Nuspec => ReadNuspecFromZipArchive(_packageContent);

        public string NuspecXml => ReadNuspecXml(_packageContent);


        private ZipArchiveEntry GetNuspecEntry(ZipArchive zip)
        {
            return zip.FindEntries(".*\\.nuspec").FirstOrDefault();
        }

        private string ReadNuspecXml(ZipArchive package)
        {
            var nuspecEntry = GetNuspecEntry(package);

            var xmlString = nuspecEntry?.AsText(Encoding.Default);

            return xmlString;
        }


        private Nuspec ReadNuspecFromZipArchive(ZipArchive package)
        {
            var nuspecString = ReadNuspecXml(package);

            if (!string.IsNullOrEmpty(nuspecString))
            {
                var nuspec = GetNuspecFromXml(nuspecString);

                return nuspec;
            }

            return null;
        }

        private static string ClearEncodingHeader(string xmlContent)
        {
            var st = xmlContent.IndexOf('<');

            return xmlContent.Substring(st, xmlContent.Length - st);
        }

        public static Nuspec GetNuspecFromXml(string xmlString)
        {
            xmlString = ClearEncodingHeader(xmlString);

            var xHelper = new XmlReadHelper();

            var doc = XmlReadHelper.GetDocument(xmlString);

            var packageName = xHelper.FindValueFor(doc, "id");
            var packageVersion = xHelper.FindValueFor(doc, "version");

            return new Nuspec
            {
                Id = packageName,
                Version = packageVersion,
                Dependencies = xHelper.ExtractData(doc, "dependency", n => new PackageId
                {
                    Id = n.Attributes.GetNamedItem("id").InnerText,
                    Version = n.Attributes.GetNamedItem("version").InnerText
                })
            };
        }


        public void ExtractInto(CachePackage cachePackage)
        {
            Delete(cachePackage);

            CreateStructure(cachePackage);

            var nuspecEntry = GetNuspecEntry(_packageContent);

            nuspecEntry.ExtractToDirectory(cachePackage.ByVersionDirectory);

            List<ZipArchiveEntry> libEntries = _packageContent.FindEntries("lib/.*");

            libEntries.ForEach(e => e.ExtractToDirectory(cachePackage.ByVersionDirectory));
        }


        private void CreateStructure(CachePackage cachePackage)
        {
            new DirectoryInfo(cachePackage.ByNameDirectory).Create();
            new DirectoryInfo(cachePackage.ByVersionDirectory).Create();
        }

        private void Delete(CachePackage cachePackage)
        {
            var directory = new DirectoryInfo(cachePackage.ByNameDirectory);

            if (directory.Exists)
            {
                directory.Delete(true);
            }
        }
    }
}