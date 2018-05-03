using System.Collections.Generic;

namespace FileSync.Comparers
{
    public interface IDirectoryStructureComparer
    {
        DirectoryStructureComparer Compare(string src, string dest);
        IEnumerable<(string, string)> ToTuples();
    }
}