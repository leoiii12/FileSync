using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FileSync.Comparers;
using FileSync.Filters;
using FileSync.Operations;
using FileSync.VirtualFileSystem;
using Microsoft.Extensions.Logging;

namespace FileSync
{
    public class FileSynchronizer
    {
        private readonly IAppConfig _appConfig;
        private readonly IDirectoryStructureComparer _directoryStructureComparer;
        private readonly IFileComparer _fileComparer;
        private readonly IFileCopier _fileCopier;
        private readonly IFileDeleter _fileDeleter;
        private readonly IFileMerger _fileMerger;
        private readonly IFileFilter _fileFilter;
        private readonly ILogger<FileSynchronizer> _logger;

        private readonly IFileSystem _destFileSystem;
        private readonly IFileSystem _srcFileSystem;

        public FileSynchronizer(
            IAppConfig appConfig,
            ILogger<FileSynchronizer> logger,
            IDirectoryStructureComparer directoryStructureComparer,
            IFileComparer fileComparer,
            IFileCopier fileCopier,
            IFileDeleter fileDeleter,
            IFileMerger fileMerger,
            IFileFilter fileFilter)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _directoryStructureComparer = directoryStructureComparer ?? throw new ArgumentNullException(nameof(directoryStructureComparer));
            _fileComparer = fileComparer ?? throw new ArgumentNullException(nameof(fileComparer));
            _fileCopier = fileCopier ?? throw new ArgumentNullException(nameof(fileCopier));
            _fileDeleter = fileDeleter ?? throw new ArgumentNullException(nameof(fileDeleter));
            _fileMerger = fileMerger ?? throw new ArgumentNullException(nameof(fileMerger));
            _fileFilter = fileFilter;

            _srcFileSystem = _appConfig.Src;
            _destFileSystem = _appConfig.Dest;
        }

        public void Sync()
        {
            _logger.LogInformation($"Synchronizing from {_srcFileSystem} to {_destFileSystem}...");

            var pairs = _directoryStructureComparer.Compare(_srcFileSystem, _destFileSystem).ToPairs();

            try
            {
                Parallel.ForEach(pairs, new ParallelOptions {MaxDegreeOfParallelism = 4}, SyncOnPair);
            }
            catch
            {
                _logger.LogDebug("Synced files = {" + string.Join(", ", pairs.Where(p => p.HasSynced).Select(p => p.SourcePath)) + "}");
                _logger.LogDebug("Not synced files = {" + string.Join(", ", pairs.Where(p => !p.HasSynced).Select(p => p.SourcePath)) + "}");

                throw;
            }

            _logger.LogInformation("Synchronized.");
        }

        private void SyncOnPair(Pair pair)
        {
            var srcFileRelativePath = pair.SourcePath;
            var destFileRelativePath = pair.DestinationPath;

            var isEmptySrcFile = string.IsNullOrEmpty(srcFileRelativePath);
            var isEmptyDestFile = string.IsNullOrEmpty(destFileRelativePath);

            // Src file does not exist, delete the file in the dest directory
            if (isEmptySrcFile)
                Delete(destFileRelativePath);

            // Dest file does not exist, copy the src file to dest
            else if (isEmptyDestFile)
                Copy(srcFileRelativePath);

            // If both src and dest files exist, compare and sync them
            else
                CompareAndSync(srcFileRelativePath, destFileRelativePath);

            pair.Done();
        }

        public void WatchAndSync()
        {
            _srcFileSystem
                .Watch()
                .Where(e => !_fileFilter.Filterd(e.Name))
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e =>
                {
                    _logger.LogTrace("File changes are detected.");

                    Sync();
                });
        }

        private void Delete(string relativeDestFilePath)
        {
            if (!_appConfig.KeepRemovedFilesInDest) _fileDeleter.Delete(_destFileSystem, relativeDestFilePath);
        }

        private void Copy(string relativeSrcFilePath)
        {
            _fileCopier.Copy(_srcFileSystem, _destFileSystem, relativeSrcFilePath, relativeSrcFilePath);
        }

        private void CompareAndSync(string relativeSrcFilePath, string relativeDestFilePath)
        {
            var isDifferentFile = !_fileComparer.GetIsEqualFile(_srcFileSystem, _destFileSystem, relativeSrcFilePath, relativeDestFilePath);

            if (isDifferentFile)
                _fileMerger.Merge(_srcFileSystem, _destFileSystem, relativeSrcFilePath, relativeDestFilePath);
        }
    }
}