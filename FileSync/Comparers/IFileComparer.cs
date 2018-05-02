namespace FileSync.Comparers
{
    public interface IFileComparer
    {
        bool GetIsEqualFile(string src, string srcFile, string dest, string destFile);
    }
}