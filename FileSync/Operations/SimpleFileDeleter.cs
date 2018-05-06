using System;
using System.IO;
using JetBrains.Annotations;
using Serilog;

namespace FileSync.Operations
{
    public class SimpleFileDeleter : IFileDeleter
    {
        private const string OldExtenstion = ".fsrmd"; // File Sync ReMoveD

        private readonly ILogger _logger;

        public SimpleFileDeleter([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Delete(string filePath)
        {
            try
            {
                var oldFilePath = filePath + OldExtenstion;

                File.Move(filePath, oldFilePath);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().ToString());
            }
        }
    }
}