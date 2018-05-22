using System;
using FileSync.VirtualFileSystem;

namespace FileSync.Operations
{
    public class SimpleFileMerger : IFileMerger
    {
        private readonly IFileCopier _fileCopier;
        private readonly IFileDeleter _fileDeleter;

        public SimpleFileMerger(IFileCopier fileCopier, IFileDeleter fileDeleter)
        {
            _fileCopier = fileCopier ?? throw new ArgumentNullException(nameof(fileCopier));
            _fileDeleter = fileDeleter ?? throw new ArgumentNullException(nameof(fileDeleter));
        }

        public void Merge(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath)
        {
            _fileDeleter.Delete(destFileSystem, destFilePath);
            _fileCopier.Copy(srcFileSystem, destFileSystem, srcFilePath, destFilePath);
        }
    }
}