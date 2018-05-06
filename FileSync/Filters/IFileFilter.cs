using JetBrains.Annotations;

namespace FileSync.Filters
{
    public interface IFileFilter
    {
        bool Filterd([NotNull] string path);
    }
}