using System;
using System.IO;
using System.Security.Cryptography;
using FileSync.VirtualFileSystem;
using Microsoft.Extensions.Logging;
using Serilog.Parsing;

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
            if (!fileSystem.FileExists(filePath)) return;

            try
            {
                if (Path.GetExtension(filePath) == TempExtenstion)
                {
                    fileSystem.DeleteFile(filePath);
                }
                else
                {
                    var tempFilePath = filePath + TempExtenstion;

                    fileSystem.MoveFile(filePath, tempFilePath, true);
                    _logger.LogTrace($"Moved file {filePath} to temp place {tempFilePath}.");

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