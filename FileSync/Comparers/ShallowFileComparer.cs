using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FileSync.Comparers
{
    public class ShallowFileComparer : IFileComparer
    {
        private readonly ILogger<ShallowFileComparer> _logger;

        public ShallowFileComparer([NotNull] ILogger<ShallowFileComparer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool GetIsEqualFile(string srcFilePath, string destFilePath)
        {
            var isEqualFile = true;

            try
            {
                isEqualFile = ShallowFileCompare(srcFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.GetBaseException().ToString());
            }

            return isEqualFile;
        }

        public void EnsureIsEqualFile(string srcFilePath, string destFilePath)
        {
            if (!GetIsEqualFile(srcFilePath, destFilePath)) throw new Exception($"The dest file \"{destFilePath}\" is different from the src file \"{srcFilePath}\".");
        }

        private static bool ShallowFileCompare(string srcFilePath, string destFilePath)
        {
            var srcFileInfo = new FileInfo(srcFilePath);
            var destFileInfo = new FileInfo(destFilePath);

            bool isEqualFile;

            if (srcFileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                long length;
                using (var fileStream = File.Open(srcFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    length = fileStream.Length;
                }

                isEqualFile = length == destFileInfo.Length;
            }
            else
            {
                isEqualFile = srcFileInfo.Length == destFileInfo.Length &&
                              Math.Abs((srcFileInfo.LastWriteTimeUtc - destFileInfo.LastWriteTimeUtc).TotalMilliseconds) < 2000;
            }

            return isEqualFile;
        }
    }
}