using JetBrains.Annotations;

namespace FileSync.Operations
{
    public interface IFileDeleter
    {
        void Delete([NotNull] string filePath);
    }
}