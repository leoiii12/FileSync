namespace FileSync.Operations
{
    public interface IFileMerger
    {
        void Merge(string srcFilePath, string destFilePath);
    }
}