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

        private static bool ShallowFileCompare(string srcFilePath, string destFilePath)
        {
            var srcFileInfo = new FileInfo(srcFilePath);
            var destFileInfo = new FileInfo(destFilePath);

            // TODO: Check timestamp
            return srcFileInfo.Length == destFileInfo.Length;
        }
    }
}