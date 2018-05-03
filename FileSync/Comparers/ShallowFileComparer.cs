using System;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace FileSync.Comparers
{
    public class ShallowFileComparer : IShallowFileComparer
    {
        private readonly ILogger _logger;

        public ShallowFileComparer(ILogger logger)
        {
            _logger = logger;
        }

        public bool GetIsEqualFile(string srcFilePath, string destFilePath)
        {
            var isEqualFile = true;

            var sw = Stopwatch.StartNew();

            try
            {
                isEqualFile = ShallowFileCompare(srcFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().ToString());
            }

            sw.Stop();

            if (sw.Elapsed.Milliseconds > 500) _logger.Warning($"Compute hash for \"{srcFilePath}\", elapsed = {sw.Elapsed.Milliseconds} ms.");

            return isEqualFile;
        }

        private static bool ShallowFileCompare(string srcFilePath, string destFilePath)
        {
            var srcFileInfo = new FileInfo(srcFilePath);
            var destFileInfo = new FileInfo(destFilePath);

            return srcFileInfo.Length == destFileInfo.Length &&
                   srcFileInfo.LastWriteTimeUtc == destFileInfo.LastWriteTimeUtc &&
                   srcFileInfo.CreationTimeUtc == destFileInfo.CreationTimeUtc;
        }
    }
}