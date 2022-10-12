using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using nuge.Nuget.Dtos;

namespace nuge.Nuget
{
    public class NugetFetchFamily:NugetApiBase
    {


        public string DownloadDirectory { get; set; }


        private void CheckDirectory()
        {
            if (string.IsNullOrEmpty(DownloadDirectory))
            {
                DownloadDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location)
                    .DirectoryName;

                DownloadDirectory = Path.Combine(DownloadDirectory, "FetchDownload");

                if (!Directory.Exists(DownloadDirectory))
                {
                    Directory.CreateDirectory(DownloadDirectory);
                }
                
            }
        }

        public void Fetch(string query)
        {
            UpdateIndex();
            
            CheckDirectory();
            
            var url = new NugetApiLinkProvider().GetSearchApiUrl(NugetIndex,query);
            
            var downloader = new PatientDownloader
            {
                Logger = Logger
            };

            var result =  downloader.DownloadObject<SearchResult>(url,5000,15).Result;

            if (result)
            {
                Logger.LogInformation("Got Search Result for given query.");
                
                foreach (var packageInfo in result.Value.Data)
                {
                    foreach (var version in packageInfo.Versions)
                    {
                        
                       

                        var fileName = new PackageId
                            { Id = packageInfo.Id, Version = version.Version }.AsFileName();

                        var path = Path.Combine(DownloadDirectory, fileName);
                        
                        if (File.Exists(path))
                        {
                            Logger.LogInformation("File {File} Already exists.",fileName);
                        }
                        else
                        {
                            Download(version.DownloadIndex, path);
                        }
                    }
                }
            }
            else
            {
                Logger.LogError("Unable to query for given package");
            }
        }

        private void Download(string downloadIndexUrl, string path)
        {
            var downloader = new PatientDownloader
            {
                Logger = Logger
            };
            
            var index = downloader.
                DownloadObject<PackageVersionDownload>
                    (downloadIndexUrl,5000,15).Result;
            
            if (index)
            {
                var link = index.Value.PackageContent;
                
                var downloadResult = downloader.DownloadFile(link, 2000, 15).Result;

                if (downloadResult)
                {
                    JustWrite(path,downloadResult.Value);
                                
                    Logger.LogInformation("Package Downloaded {Url}",link);
                }
                else
                {
                    Logger.LogError("Failed Downloading {Url}",link);
                }
            }
            else
            {
                Logger.LogError("Error downloading. {Exception}",index.Exception);
            }
        }
    }
}