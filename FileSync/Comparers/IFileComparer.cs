using FileSync.VirtualFileSystem;

namespace FileSync.Comparers
{
    public interface IFileComparer
    {
        bool GetIsEqualFile(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath);
        void EnsureIsEqualFile(IFileSystem srcFileSystem, IFileSystem destFileSystem, string filePath);
        void EnsureIsEqualFile(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath);
    }
}