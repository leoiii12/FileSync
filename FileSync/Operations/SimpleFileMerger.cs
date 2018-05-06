using System;
using JetBrains.Annotations;

namespace FileSync.Operations
{
    public class SimpleFileMerger : IFileMerger
    {
        private readonly IFileCopier _fileCopier;
        private readonly IFileDeleter _fileDeleter;

        public SimpleFileMerger([NotNull] IFileCopier fileCopier, [NotNull] IFileDeleter fileDeleter)
        {
            _fileCopier = fileCopier ?? throw new ArgumentNullException(nameof(fileCopier));
            _fileDeleter = fileDeleter ?? throw new ArgumentNullException(nameof(fileDeleter));
        }

        public void Merge(string srcFilePath, string destFilePath)
        {
            _fileDeleter.Delete(destFilePath);
            _fileCopier.Copy(srcFilePath, destFilePath);
        }
    }
}