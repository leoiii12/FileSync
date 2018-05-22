using FileSync.VirtualFileSystem;

namespace FileSync.Operations
{
    public interface IFileMerger
    {
        void Merge(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath);
    }
}