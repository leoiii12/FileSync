using System;
using System.IO;
using FileSync.Comparers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FileSync.Operations
{
    public class SimpleFileCopier : IFileCopier
    {
        private const string TempExtenstion = ".fstmp"; // "FileSync Temp"
        private readonly IFileComparer _fileComparer;

        private readonly ILogger<SimpleFileCopier> _logger;

        public SimpleFileCopier([NotNull] ILogger<SimpleFileCopier> logger, [NotNull] IFileComparer fileComparer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileComparer = fileComparer ?? throw new ArgumentNullException(nameof(fileComparer));
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

                _fileComparer.EnsureIsEqualFile(srcFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.GetBaseException().ToString());
            }
        }

        private void EnsureDirectory(string directoryName)
        {
            if (Directory.Exists(directoryName)) return;

            Directory.CreateDirectory(directoryName);
            _logger.LogDebug($"The directory name \"{directoryName}\" does not exist. Created it.");
        }
    }
}