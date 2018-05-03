namespace FileSync.Operations
{
    public class SimpleFileMerger : IFileMerger
    {
        private readonly IFileCopier _fileCopier;
        private readonly IFileDeleter _fileDeleter;

        public SimpleFileMerger(IFileCopier fileCopier, IFileDeleter fileDeleter)
        {
            _fileCopier = fileCopier;
            _fileDeleter = fileDeleter;
        }

        public void Merge(string srcFilePath, string destFilePath)
        {
            _fileDeleter.Delete(destFilePath);
            _fileCopier.Copy(srcFilePath, destFilePath);
        }
    }
}