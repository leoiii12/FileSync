using System;
using System.IO;
using JetBrains.Annotations;
using Serilog;

namespace FileSync.Comparers
{
    public class ShallowFileComparer : IFileComparer
    {
        private readonly ILogger _logger;

        public ShallowFileComparer([NotNull] ILogger logger)
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
                _logger.Error(e.GetBaseException().ToString());
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

            var isEqualFile = srcFileInfo.Length == destFileInfo.Length &&
                              Math.Abs((srcFileInfo.LastWriteTimeUtc - destFileInfo.LastWriteTimeUtc).TotalMilliseconds) < 2000;

            return isEqualFile;
        }
    }
}