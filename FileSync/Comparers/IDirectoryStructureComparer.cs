using System.Collections.Generic;
using JetBrains.Annotations;

namespace FileSync.Comparers
{
    public interface IDirectoryStructureComparer
    {
        DirectoryStructureComparer Compare([NotNull] string src, [NotNull] string dest);
        ICollection<Pair> ToPairs();
    }
}