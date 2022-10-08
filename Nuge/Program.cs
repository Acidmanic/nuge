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
                Logger = new ConsoleLogger().EnableAll(),
            };
            
            
            
            if (args.Length > 0)
            {
                downloader.SearchDirectory = args[0];
            }

            if (args.Length > 1)
            {
                downloader.DownloadDirectory = args[1];
            }

            bool saveLower = !IsPresent("--no-lower",args);
            
            bool fromScratch = IsPresent("--scratch",args);;

            downloader.DownloadUntilResolves(fromScratch, saveLower);
        }

        private static bool IsPresent(string option, string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.ToLower() == option)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
