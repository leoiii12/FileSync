using System.IO;

namespace FileSync.Comparers
{
    public interface IFileFilter
    {
        bool Filterd(FileInfo fileInfo);
    }
}