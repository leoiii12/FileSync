using System;
using System.IO;
using JetBrains.Annotations;
using Serilog;

namespace FileSync.Operations
{
    public class SimpleFileDeleter : IFileDeleter
    {
        private const string TempExtenstion = ".fsrmd"; // "FileSync Removed"

        private readonly ILogger _logger;

        public SimpleFileDeleter([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Delete(string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var originalExtension = Path.GetExtension(filePath);
                if (originalExtension == TempExtenstion)
                {
                    File.Delete(filePath);
                }
                else
                {
                    var newExtension = originalExtension + TempExtenstion;
                    var newPath = Path.ChangeExtension(filePath, newExtension);

                    File.Move(filePath, newPath);
                    File.Delete(newPath);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().ToString());
            }
        }
    }
}