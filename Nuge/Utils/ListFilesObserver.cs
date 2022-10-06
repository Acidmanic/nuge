using System.Collections.Generic;
using System.IO;

namespace nuge.Utils
{
    public class ListFilesObserver : ISearchObserver<List<FileInfo>>
    {
        public List<FileInfo> Result { get; }

        public ListFilesObserver()
        {
            Result = new List<FileInfo>();
        }

        public void OnDirectory(DirectoryInfo dir)
        {
        }

        public virtual void OnFile(DirectoryInfo location, FileInfo file)
        {
            Result.Add(file);
        }
    }
}