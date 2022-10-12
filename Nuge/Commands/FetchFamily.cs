using nuge.Nuget;

namespace nuge.Commands
{
    public class FetchFamily:CommandBase
    {
        public override void Execute(string[] args)
        {
            var query = "";

            if (args.Length > 0)
            {
                query = args[0];
            }



            var fam = new NugetFetchFamily
            {
                Logger = Logger
            };
            
            fam.Fetch(query);

        }

        public override string Name => "fetch";
    }
}