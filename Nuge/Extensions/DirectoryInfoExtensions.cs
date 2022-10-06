using System.IO;

namespace nuge.Extensions
{
    public static  class DirectoryInfoExtensions
    {



        public static void CreateByParents(this DirectoryInfo directory)
        {
            if (directory==null || directory.Exists)
            {
                return;
            }
            CreateByParents(directory.Parent);
            
            directory.Create();
        }
    }
    
    
}