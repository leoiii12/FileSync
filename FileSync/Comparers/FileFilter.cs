using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSync.Comparers
{
    public class FileFilter : IFileFilter
    {
        private readonly HashSet<string> _ignoringFileNames;

        public FileFilter(AppConfig appConfig)
        {
            _ignoringFileNames = appConfig.IgnoringFileNames.ToHashSet();
        }

        public bool Filterd(string path)
        {
            var fileName = Path.GetFileName(path);

            return _ignoringFileNames.Contains(fileName);
        }
    }
}