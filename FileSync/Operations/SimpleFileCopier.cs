﻿using System;
using System.IO;
using Serilog;

namespace FileSync.Operations
{
    public class SimpleFileCopier : IFileCopier
    {
        private const string TempExtenstion = ".fstmp"; // File Sync TeMP

        private readonly ILogger _logger;

        public SimpleFileCopier(ILogger logger)
        {
            _logger = logger;
        }

        public void Copy(string srcFilePath, string destFilePath)
        {
            if (srcFilePath == null) throw new ArgumentNullException(nameof(srcFilePath));
            if (destFilePath == null) throw new ArgumentNullException(nameof(destFilePath));

            try
            {
                var directoryName = Path.GetDirectoryName(destFilePath);
                EnsureDirectory(directoryName);

                var tempFilePath = destFilePath + TempExtenstion;

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