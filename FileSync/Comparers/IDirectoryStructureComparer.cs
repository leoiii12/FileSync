using System.Collections.Generic;
using FileSync.VirtualFileSystem;

namespace FileSync.Comparers
{
    public interface IDirectoryStructureComparer
    {
        DirectoryStructureComparer Compare(IFileSystem srcFileSystem, IFileSystem destFileSystem);
        ICollection<Pair> ToPairs();
    }
}