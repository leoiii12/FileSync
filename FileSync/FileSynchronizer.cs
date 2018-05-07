using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FileSync.Comparers;
using FileSync.FileWatchers;
using FileSync.Operations;
using Serilog;

namespace FileSync
{
    public class FileSynchronizer
    {
        private readonly IAppConfig _appConfig;
        private readonly ILogger _logger;
        private readonly IDirectoryStructureComparer _directoryStructureComparer;
        private readonly IFileComparer _fileComparer;
        private readonly IFileCopier _fileCopier;
        private readonly IFileDeleter _fileDeleter;
        private readonly IFileMerger _fileMerger;
        private readonly IFileWatcher _fileWatcher;

        private readonly string _src;
        private readonly string _dest;

        public FileSynchronizer(
            IAppConfig appConfig,
            ILogger logger,
            IDirectoryStructureComparer directoryStructureComparer,
            IFileComparer fileComparer,
            IFileCopier fileCopier,
            IFileDeleter fileDeleter,
            IFileMerger fileMerger,
            IFileWatcher fileWatcher)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _directoryStructureComparer = directoryStructureComparer ?? throw new ArgumentNullException(nameof(directoryStructureComparer));
            _fileComparer = fileComparer ?? throw new ArgumentNullException(nameof(fileComparer));
            _fileCopier = fileCopier ?? throw new ArgumentNullException(nameof(fileCopier));
            _fileDeleter = fileDeleter ?? throw new ArgumentNullException(nameof(fileDeleter));
            _fileMerger = fileMerger ?? throw new ArgumentNullException(nameof(fileMerger));
            _fileWatcher = fileWatcher;

            _src = _appConfig.Src;
            _dest = _appConfig.Dest;
        }

        public void Sync()
        {
            _logger.Information($"Synchronizing from {_src} to {_dest}...");

            EnsureSrcAndDest();

            var pairs = _directoryStructureComparer.Compare(_src, _dest).ToPairs();

            try
            {
                Parallel.ForEach(pairs, new ParallelOptions {MaxDegreeOfParallelism = 4}, SyncOnPair);
            }
            catch (Exception e)
            {
                _logger.Verbose("Synced files = {" + string.Join(", ", pairs.Where(p => p.HasSynced).Select(p => p.SourcePath)) + "}");
                _logger.Verbose("Not synced files = {" + string.Join(", ", pairs.Where(p => !p.HasSynced).Select(p => p.SourcePath)) + "}");

                throw;
            }

            _logger.Information("Synchronized.");
        }

        private void SyncOnPair(Pair pair)
        {
            var srcFileRelativePath = pair.SourcePath;
            var destFileRelativePath = pair.DestinationPath;

            var isEmptySrcFile = string.IsNullOrEmpty(srcFileRelativePath);
            var isEmptyDestFile = string.IsNullOrEmpty(destFileRelativePath);

            EnsureSrcAndDest();

            // Src file does not exist, delete the file in the dest directory
            if (isEmptySrcFile)
            {
                Delete(destFileRelativePath);
            }

            // Dest file does not exist, copy the src file to dest
            else if (isEmptyDestFile)
            {
                Copy(srcFileRelativePath);
            }

            // If both src and dest files exist, compare and sync them
            else
                CompareAndSync(srcFileRelativePath, destFileRelativePath);

            pair.Done();
        }

        public void WatchAndSync()
        {
            _fileWatcher.Watch(_src);
            _fileWatcher.Changes
                .Throttle(TimeSpan.FromSeconds(10))
                .Subscribe(e => { Sync(); });
        }

        private void Delete(string destFileRelativePath)
        {
            if (!_appConfig.KeepRemovedFilesInDest)
            {
                _fileDeleter.Delete(Path.Combine(_dest, destFileRelativePath));
            }
        }

        private void Copy(string srcFileRelativePath)
        {
            var srcFilePath = Path.Combine(_src, srcFileRelativePath);
            var destFilePath = Path.Combine(_dest, srcFileRelativePath);

            _fileCopier.Copy(srcFilePath, destFilePath);
        }

        private void CompareAndSync(string srcFileRelativePath, string destFileRelativePath)
        {
            var srcFilePath = Path.Combine(_src, srcFileRelativePath);
            var destFilePath = Path.Combine(_dest, destFileRelativePath);

            var isDifferentFile = !_fileComparer.GetIsEqualFile(srcFilePath, destFilePath);
            if (isDifferentFile)
            {
                _fileMerger.Merge(srcFilePath, destFilePath);
            }
        }

        private void EnsureSrcAndDest()
        {
            if (!Directory.Exists(_src)) throw new Exception($"Src {_src} not found. Terminated.");
            if (!Directory.Exists(_dest)) throw new Exception($"Dest {_dest} not found. Terminated.");
        }
    }
}