using System.IO;
using Meadow.Tools.Assistant.Utils;

namespace nuge.Utils
{
    public partial class FileSystemSearch<TOutput>
    {
        public TOutput Search(string location, string searchPattern, ISearchObserver<TOutput> observer)
        {
            return Search(new DirectoryInfo(location), searchPattern, observer);
        }
        
        public TOutput Search(DirectoryInfo location, string searchPattern, ISearchObserver<TOutput> observer)
        {
            Scan(location, searchPattern, observer);

            var directories = location.EnumerateDirectories();

            foreach (var directory in directories)
            {
                Search(directory, searchPattern, observer);
            }

            return observer.Result;
        }

        public TOutput Scan(DirectoryInfo location,string searchPattern, ISearchObserver<TOutput> observer)
        {
            observer.OnDirectory(location);

            var files = location.EnumerateFiles(searchPattern);

            foreach (var file in files)
            {
                observer.OnFile(location,file);
            }

            return observer.Result;
        }

        public TOutput Scan(string location, string searchPattern, ISearchObserver<TOutput> observer)
        {
            return Scan(new DirectoryInfo(location), searchPattern, observer);
        }
    }
}
