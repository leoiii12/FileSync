using JetBrains.Annotations;

namespace FileSync
{
    public interface IAppConfig
    {
        [NotNull] string Src { get; }
        [NotNull] string Dest { get; }
        [NotNull] string Log { get; }
        bool UseDeepFileComparer { get; }
        bool KeepRemovedFilesInDest { get; }
    }
}