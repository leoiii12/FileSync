using System;
using System.IO;
using FileSync.VirtualFileSystem;
using Microsoft.Extensions.Logging;

namespace FileSync.Comparers
{
    public class ShallowFileComparer : IFileComparer
    {
        private readonly ILogger<ShallowFileComparer> _logger;

        public ShallowFileComparer(ILogger<ShallowFileComparer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool GetIsEqualFile(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath)
        {
            var isEqualFile = true;

            try
            {
                isEqualFile = ShallowFileCompare(srcFileSystem, destFileSystem, srcFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.GetBaseException().ToString());
            }

            return isEqualFile;
        }

        public void EnsureIsEqualFile(IFileSystem srcFileSystem, IFileSystem destFileSystem, string filePath)
        {
            EnsureIsEqualFile(srcFileSystem, destFileSystem, filePath, filePath);
        }

        public void EnsureIsEqualFile(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath)
        {
            if (!GetIsEqualFile(srcFileSystem, destFileSystem, srcFilePath, destFilePath)) throw new Exception($"The dest file \"{destFilePath}\" is different from the src file \"{srcFilePath}\".");
        }

        private static bool ShallowFileCompare(IFileSystem srcFileSystem, IFileSystem destFileSystem, string srcFilePath, string destFilePath)
        {
            var srcFileInfo = srcFileSystem.GetFileInfo(srcFilePath);
            var destFileInfo = destFileSystem.GetFileInfo(destFilePath);

            var srcFileLength = srcFileInfo.Length;

            if (srcFileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                using (var srcFileStream = srcFileSystem.OpenFile(srcFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    srcFileLength = srcFileStream.Length;
                }
            }

            // Different lengths -> not equal
            if (srcFileLength != destFileInfo.Length) return false;

            return true;
        }
    }
}