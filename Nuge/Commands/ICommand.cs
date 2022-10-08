using Microsoft.Extensions.Logging;

namespace nuge.Commands
{
    public interface ICommand
    {
        void Execute(string[] args);

        void SetLogger(ILogger logger);
        
        string Name { get; }
    }
}