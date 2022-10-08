using Microsoft.Extensions.Logging.LightWeight;

namespace nuge.Commands
{
    public class Nuge:CommandBase
    {
        public override void Execute(string[] args)
        {
            var downloader = new AutoPackageDownloader
            {
                Logger = Logger,
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

        public override string Name  => "nuge";
    }
}