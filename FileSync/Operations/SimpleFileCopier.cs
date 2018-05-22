using System;
using System.IO;
using FileSync.Comparers;
using FileSync.VirtualFileSystem;
using Microsoft.Extensions.Logging;

namespace FileSync.Operations
{
    public class SimpleFileCopier : IFileCopier
    {
        /// <summary>
        ///     "FileSync Temp"
        /// </summary>
        private const string TempExtenstion = ".fstmp";

        private readonly IFileComparer _fileComparer;
        private readonly ILogger<SimpleFileCopier> _logger;

        public SimpleFileCopier(ILogger<SimpleFileCopier> logger, IFileComparer fileComparer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileComparer = fileComparer ?? throw new ArgumentNullException(nameof(fileComparer));
        }

        public void Copy(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath)
        {
            if (srcFileSystem == null) throw new ArgumentNullException(nameof(srcFileSystem));
            if (destFileSystem == null) throw new ArgumentNullException(nameof(destFileSystem));
            if (srcFilePath == null) throw new ArgumentNullException(nameof(srcFilePath));
            if (destFilePath == null) throw new ArgumentNullException(nameof(destFilePath));

            if (srcFilePath.EndsWith(TempExtenstion)) return;

            try
            {
                var tempFilePath = srcFilePath + TempExtenstion;

                destFileSystem.CreateDirectory(Path.GetDirectoryName(srcFilePath));

                srcFileSystem.CopyFile(srcFilePath, destFileSystem, tempFilePath, true);
                _logger.LogTrace($"Copied file from source {srcFilePath} to temp {tempFilePath}.");

                destFileSystem.MoveFile(tempFilePath, destFilePath, true);
                _logger.LogTrace($"Moved file from source {srcFilePath} to dest {destFilePath} successfully.");

                _fileComparer.EnsureIsEqualFile(srcFileSystem, destFileSystem, srcFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.GetBaseException().ToString());
            }
        }
    }
}