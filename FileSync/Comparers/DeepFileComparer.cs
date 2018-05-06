using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Serilog;

namespace FileSync.Comparers
{
    public class DeepFileComparer : IFileComparer
    {
        private const int BufferSize = 1024 * 1024 * 10;

        private readonly ILogger _logger;

        public DeepFileComparer([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool GetIsEqualFile(string srcFilePath, string destFilePath)
        {
            if (srcFilePath == null) throw new ArgumentNullException(nameof(srcFilePath));
            if (destFilePath == null) throw new ArgumentNullException(nameof(destFilePath));

            var isEqualFile = true;

            var sw = Stopwatch.StartNew();

            try
            {
                isEqualFile = DeepFileCompare(srcFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().ToString());
            }

            sw.Stop();

            if (sw.Elapsed.Milliseconds > 500) _logger.Warning($"Compute hash for \"{srcFilePath}\", elapsed = {sw.Elapsed.Milliseconds} ms.");

            return isEqualFile;
        }

        private static bool DeepFileCompare(string srcFilePath, string destFilePath)
        {
            bool isEqual;

            using (var srcFileStream = new FileStream(srcFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize))
            using (var destFileStream = new FileStream(destFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize))
            {
                isEqual = srcFileStream.Length == destFileStream.Length && IncrementallyCompare(srcFileStream, destFileStream);
            }

            return isEqual;
        }

        private static bool IncrementallyCompare(FileStream fs1, FileStream fs2)
        {
            var isEqual = true;

            using (var md5 = MD5.Create())
            {
                var srcBuffer = new byte[BufferSize];
                var destBuffer = new byte[BufferSize];

                while (true)
                {
                    if (!isEqual) break;

                    if (!fs1.CanRead && !fs2.CanRead) break;
                    if (fs1.Position > fs1.Length) break;
                    if (fs2.Position > fs2.Length) break;

                    fs1.Read(srcBuffer, 0, BufferSize);
                    fs2.Read(destBuffer, 0, BufferSize);

                    var srcHash = md5.ComputeHash(srcBuffer);
                    var destHash = md5.ComputeHash(destBuffer);

                    fs1.Seek(BufferSize, SeekOrigin.Current);
                    fs2.Seek(BufferSize, SeekOrigin.Current);

                    isEqual = BitConverter.ToString(srcHash) == BitConverter.ToString(destHash);
                }
            }

            return isEqual;
        }
    }
}