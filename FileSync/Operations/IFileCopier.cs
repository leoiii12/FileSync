using FileSync.VirtualFileSystem;

namespace FileSync.Operations
{
    public interface IFileCopier
    {
        void Copy(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath);
    }
}