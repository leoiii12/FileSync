namespace FileSync
{
    public interface IAppConfig
    {
        string Src { get; }
        string Dest { get; }
        string Log { get; }
        bool UseDeepFileComparer { get; }
        bool KeepRemovedFilesInDest { get; }
    }
}