using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;

namespace nuge.Commands
{
    public abstract class CommandBase:ICommand
    {
        protected ILogger Logger { get; set; } = new LoggerAdapter(s => { });

        protected bool IsPresent(string option, string[] args)
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


        public abstract void Execute(string[] args);
        
        public void SetLogger(ILogger logger)
        {
            Logger = logger;
        }

        public abstract string Name { get; }
    }
}