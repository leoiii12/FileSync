namespace FileSync.Comparers
{
    public interface IFileComparer
    {
        bool GetIsEqualFile(string srcFilePath, string destFilePath);
    }
}