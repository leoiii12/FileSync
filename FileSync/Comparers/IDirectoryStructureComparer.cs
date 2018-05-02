using System.Collections.Generic;

namespace FileSync.Comparers
{
    public interface IDirectoryStructureComparer
    {
        DirectoryStructureComparer Compare();
        IEnumerable<(string, string)> ToTuples();
    }
}