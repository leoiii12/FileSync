using JetBrains.Annotations;

namespace FileSync.Comparers
{
    public interface IFileComparer
    {
        bool GetIsEqualFile([NotNull] string srcFilePath, [NotNull] string destFilePath);
        void EnsureIsEqualFile([NotNull] string srcFilePath, [NotNull] string destFilePath);
    }
}