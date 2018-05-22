namespace FileSync.Filters
{
    public interface IFileFilter
    {
        bool Filterd(string path);
    }
}