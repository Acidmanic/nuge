using System.IO;
using System.Linq;
using System.Text;

namespace nuge.Commands
{
    
    public class Publish:CommandBase
    {
        public override void Execute(string[] args)
        {
            var downloadDirectory = ".";
            
            var src = args[0];

            var secrete = args[1];
            
            if (args.Length > 2)
            {
                downloadDirectory = args[2];
            }

            var stringBuilder = new StringBuilder();

            var files = new DirectoryInfo(downloadDirectory)
                .GetFiles().Where(f => f.Name.ToLower().EndsWith(".nupkg"))
                .ToList();

            var total = files.Count;

            int pushed = 0;
            
            foreach (var file in files)
            {
                pushed += 1;
                
                var command = $"dotnet nuget push --skip-duplicate --source {src} --api-key {secrete} {file.FullName} &&" +
                              $" echo pushed {file.Name} {pushed}/{total}";

                stringBuilder.AppendLine(command);
            }

            var script = stringBuilder.ToString();

            var path = Path.Combine(downloadDirectory, "publish.sh");
            
            JustWrite(path,script);
            
        }
        
        private void JustWrite(string path, string data)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllText(path, data);
        }

        public override string Name => "pub";
    }
}