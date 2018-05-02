using System;
using System.IO;
using System.Threading.Tasks;
using FileSync.Comparers;
using Serilog;

namespace FileSync
{
    public class FileSynchronizer
    {
        private readonly AppConfig _appConfig;
        private readonly ILogger _logger;
        private readonly IDirectoryStructureComparer _directoryStructureComparer;
        private readonly IFileComparer _fileComparer;
        private readonly string _src;
        private readonly string _dest;

        public FileSynchronizer(AppConfig appConfig, ILogger logger, IDirectoryStructureComparer directoryStructureComparer, IFileComparer fileComparer)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _directoryStructureComparer = directoryStructureComparer ?? throw new ArgumentNullException(nameof(directoryStructureComparer));
            _fileComparer = fileComparer ?? throw new ArgumentNullException(nameof(fileComparer));
            _src = appConfig.Src;
            _dest = appConfig.Dest;
        }

        public void Sync()
        {
            _logger.Information("Synchronizing...");

            var tuples = _directoryStructureComparer.Compare().ToTuples();

            Parallel.ForEach(tuples, tuple => { SyncOnTuple(tuple); });

            _logger.Information("Synchronized.");
        }

        private void SyncOnTuple((string, string) tuple)
        {
            var (srcFile, destFile) = tuple;

            var isEmptySrcFile = string.IsNullOrEmpty(srcFile);
            var isEmptyDestFile = string.IsNullOrEmpty(destFile);

            if (isEmptySrcFile && isEmptyDestFile) return;

            if (isEmptySrcFile)
            {
                // src file no longer exists
                if (!_appConfig.ShouldRemoveFileInDest) return;

                DeleteFile(_dest, destFile);

                _logger.Verbose($"Removed file \"{destFile}\" in \"{_dest}\"");
            }
            else if (isEmptyDestFile)
            {
                // Dest file does not exits
                CopyFile(_src, srcFile, _dest);

                _logger.Verbose($"Copied file \"{srcFile}\" from \"{_src}\" to \"{_dest}\".");
            }
            else
            {
                // Both src and dest files exist => Compare
                var isEqualFile = _fileComparer.GetIsEqualFile(_src, srcFile, _dest, destFile);

                if (isEqualFile) return;

                DeleteFile(_dest, destFile);
                CopyFile(_src, srcFile, _dest);

                _logger.Verbose($"Detected file \"{destFile}\" changes. Recopied from \"{_src}\" to \"{_dest}\".");
            }
        }

        public void WatchAndSync()
        {
            var srcWatcher = new FileSystemWatcher();
            srcWatcher.Path = _src;
            srcWatcher.Filter = "*.*";
            srcWatcher.NotifyFilter = NotifyFilters.LastWrite;
            srcWatcher.Changed += OnChanged;
            srcWatcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            _logger.Information($"Detected file \"{e.FullPath}\" changes.");

            Sync();
        }

        private void CopyFile(string src, string srcFile, string dest)
        {
            var srcFilePath = src + srcFile;
            var destFilePath = dest + srcFile;

            try
            {
                var directoryName = Path.GetDirectoryName(destFilePath);
                EnsureDirectory(directoryName);

                File.Copy(srcFilePath, destFilePath);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().ToString());
            }
        }

        private void DeleteFile(string dest, string destFile)
        {
            var destFilePath = dest + destFile;

            try
            {
                File.Delete(destFilePath);
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