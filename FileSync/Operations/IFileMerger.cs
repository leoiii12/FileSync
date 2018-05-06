using JetBrains.Annotations;

namespace FileSync.Operations
{
    public interface IFileMerger
    {
        void Merge([NotNull] string srcFilePath, [NotNull] string destFilePath);
    }
}