using System;
using Meadow.Tools.Assistant.Nuget;
using nuge.Nuget;
using nuge.Nuget.Dtos;
using nuge.Utils;

namespace nuge.Extensions
{
    public static class NuspecExtensions
    {
        public static Nuspec LoadXml(this Nuspec nuspec, string xml)
        {
            

            var xHelper = new XmlReadHelper();

            var doc = XmlReadHelper.GetDocument(xml);

            var packageName = xHelper.FindValueFor(doc, "id");
            var packageVersion = xHelper.FindValueFor(doc, "version");


            if (string.IsNullOrEmpty(packageVersion) || packageVersion=="xunit.core")
            {
                Console.WriteLine();
            }
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
    }
}