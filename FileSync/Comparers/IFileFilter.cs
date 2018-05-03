namespace FileSync.Comparers
{
    public interface IFileFilter
    {
        bool Filterd(string path);
    }
}