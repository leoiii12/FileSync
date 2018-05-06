using System;
using System.IO;
using JetBrains.Annotations;
using Serilog;

namespace FileSync.Operations
{
    public class SimpleFileCopier : IFileCopier
    {
        private const string TempExtenstion = ".fstmp"; // File Sync TeMP

        private readonly ILogger _logger;

        public SimpleFileCopier([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Copy(string srcFilePath, string destFilePath)
        {
            if (srcFilePath == null) throw new ArgumentNullException(nameof(srcFilePath));
            if (destFilePath == null) throw new ArgumentNullException(nameof(destFilePath));

            if (srcFilePath.EndsWith(TempExtenstion)) return;

            try
            {
                var directoryName = Path.GetDirectoryName(destFilePath);
                EnsureDirectory(directoryName);

                var tempFilePath = destFilePath + TempExtenstion;

                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);

                File.Copy(srcFilePath, tempFilePath);
                File.Move(tempFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().ToString());
            }
        }

        private void EnsureDirectory(string directoryName)
        {
            if (Directory.Exists(directoryName)) return;

            Directory.CreateDirectory(directoryName);
            _logger.Verbose($"The directory name \"{directoryName}\" does not exist. Created it.");
        }
    }
}