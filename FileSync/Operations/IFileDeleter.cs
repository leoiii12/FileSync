using FileSync.VirtualFileSystem;

namespace FileSync.Operations
{
    public interface IFileDeleter
    {
        void Delete(IFileSystem fileSystem, string filePath);
    }
}