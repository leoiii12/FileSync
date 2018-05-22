using System;
using System.IO;
using FileSync.VirtualFileSystem;
using Microsoft.Extensions.Logging;

namespace FileSync.Operations
{
    public class SimpleFileDeleter : IFileDeleter
    {
        /// <summary>
        ///     "FileSync Removed"
        /// </summary>
        private const string TempExtenstion = ".fsrmd";

        private readonly ILogger<SimpleFileDeleter> _logger;

        public SimpleFileDeleter(ILogger<SimpleFileDeleter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Delete(IFileSystem fileSystem, string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var originalExtension = Path.GetExtension(filePath);
                if (originalExtension == TempExtenstion)
                {
                    fileSystem.DeleteFile(filePath);
                }
                else
                {
                    var newExtension = originalExtension + TempExtenstion;
                    var newPath = Path.ChangeExtension(filePath, newExtension);

                    fileSystem.MoveFile(filePath, newPath, true);
                    _logger.LogTrace($"Moved file {filePath} to temp place {newPath}.");

                    fileSystem.DeleteFile(filePath);
                    _logger.LogTrace($"Deleted file {filePath} successfully.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.GetBaseException().ToString());
            }
        }
    }
}