namespace FileSync.Comparers
{
    public interface IShallowFileComparer
    {
        bool GetIsEqualFile(string srcFilePath, string destFilePath);
    }
}