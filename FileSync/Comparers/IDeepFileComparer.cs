namespace FileSync.Comparers
{
    public interface IDeepFileComparer
    {
        bool GetIsEqualFile(string srcFilePath, string destFilePath);
    }
}