using JetBrains.Annotations;

namespace FileSync.Operations
{
    public interface IFileCopier
    {
        void Copy([NotNull] string srcFilePath, [NotNull] string destFilePath);
    }
}