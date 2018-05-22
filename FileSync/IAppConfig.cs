using FileSync.VirtualFileSystem;

namespace FileSync
{
    public interface IAppConfig
    {
        IFileSystem Src { get; }
        IFileSystem Dest { get; }
        string Log { get; }
        bool UseDeepFileComparer { get; }
        bool KeepRemovedFilesInDest { get; }
    }
}