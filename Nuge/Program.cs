using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;
using nuge.Commands;

namespace nuge
{
    class Program
    {


        private static List<ICommand> _commands = new List<ICommand>
        {
            new Nuge()
        };


        static void Main(string[] args)
        {

            var logger = new ConsoleLogger().EnableAll();
            
            
            if (args.Length == 0)
            {
                Console.WriteLine("What the ghiz?");
                return;
            }
            
            var commandname = args[0].ToLower();

            foreach (var command in _commands)
            {
                if (command.Name.ToLower() == commandname)
                {
                    command.SetLogger(logger);
                    
                    var subArgs = new string[args.Length - 1];
                    
                    Array.Copy(args,1, subArgs,0,args.Length-1);
                    
                    command.Execute(subArgs);

                    return;
                }
            }

            Console.WriteLine("Seriously what the hell??");
            
        }

        
    }
}
