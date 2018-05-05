using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSync.Filters
{
    public class SimpleFileFilter : IFileFilter
    {
        private readonly HashSet<string> _ignoringFileNames;

        public SimpleFileFilter(AppConfig appConfig)
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