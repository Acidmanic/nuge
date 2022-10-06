using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;

namespace nuge
{
    class Program
    {
        static void Main(string[] args)
        {

            
            var downloader = new AutoPackageDownloader
            {
                Logger = new ConsoleLogger(),
            };
            
            if (args.Length > 0)
            {
                downloader.SearchDirectory = args[0];
            }

            if (args.Length > 1)
            {
                downloader.DownloadDirectory = args[1];
            }
            
            downloader.Download();
        }
    }
}
