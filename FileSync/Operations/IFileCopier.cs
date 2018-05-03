namespace FileSync.Operations
{
    public interface IFileCopier
    {
        void Copy(string srcFilePath, string destFilePath);
    }
}