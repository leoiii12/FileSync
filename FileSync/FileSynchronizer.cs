using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FileSync.Comparers;
using FileSync.Filters;
using FileSync.Operations;
using Serilog;

namespace FileSync
{
    public class FileSynchronizer
    {
        private readonly IAppConfig _appConfig;
        private readonly ILogger _logger;
        private readonly IFileFilter _fileFilter;
        private readonly IDirectoryStructureComparer _directoryStructureComparer;
        private readonly IFileComparer _fileComparer;
        private readonly IFileCopier _fileCopier;
        private readonly IFileDeleter _fileDeleter;
        private readonly IFileMerger _fileMerger;

        private readonly string _src;
        private readonly string _dest;

        public FileSynchronizer(
            IAppConfig appConfig,
            ILogger logger,
            IFileFilter fileFilter,
            IDirectoryStructureComparer directoryStructureComparer,
            IFileComparer fileComparer,
            IFileCopier fileCopier,
            IFileDeleter fileDeleter,
            IFileMerger fileMerger)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
            _directoryStructureComparer = directoryStructureComparer ?? throw new ArgumentNullException(nameof(directoryStructureComparer));
            _fileComparer = fileComparer ?? throw new ArgumentNullException(nameof(fileComparer));
            _fileCopier = fileCopier ?? throw new ArgumentNullException(nameof(fileCopier));
            _fileDeleter = fileDeleter ?? throw new ArgumentNullException(nameof(fileDeleter));
            _fileMerger = fileMerger ?? throw new ArgumentNullException(nameof(fileMerger));

            _src = _appConfig.Src;
            _dest = _appConfig.Dest;
        }

        public void Sync()
        {
            if (!Directory.Exists(_src)) throw new Exception($"Src {_src} not found. Terminated.");
            if (!Directory.Exists(_dest)) throw new Exception($"Dest {_dest} not found. Terminated.");

            _logger.Information($"Synchronizing from {_src} to {_dest}...");

            var pairs = _directoryStructureComparer.Compare(_src, _dest).ToPairs();

            try
            {
                Parallel.ForEach(pairs, new ParallelOptions {MaxDegreeOfParallelism = 4}, SyncOnTuple);
            }
            catch (AggregateException e)
            {
                throw new Exception(e.InnerExceptions.GroupBy(ie => ie.Message).Select(g => g.Key).First());
            }

            var notYetSyncedFiles = pairs.Where(p => !p.HasSynced).ToArray();

            _logger.Information(pairs.All(p => p.HasSynced) ? "Synchronized." : $"Synchronized with errors, number of remaining files = {notYetSyncedFiles.Length}.");
            _logger.Verbose("Not synced: " + string.Join(',', notYetSyncedFiles.Select(p => p.SourcePath)));
        }

        private void SyncOnTuple(Pair pair)
        {
            var srcPath = pair.SourcePath;
            var destPath = pair.DestinationPath;

            var isEmptySrcFile = string.IsNullOrEmpty(srcPath);
            var isEmptyDestFile = string.IsNullOrEmpty(destPath);

            if (isEmptySrcFile && isEmptyDestFile) return;

            if (!Directory.Exists(_src)) throw new Exception($"Src {_src} not found. Terminated.");
            if (!Directory.Exists(_dest)) throw new Exception($"Dest {_dest} not found. Terminated.");

            // Src file does not exist, delete in the dest directory
            if (isEmptySrcFile)
            {
                if (!_appConfig.KeepRemovedFilesInDest)
                {
                    _fileDeleter.Delete(Path.Combine(_dest, destPath));

                    _logger.Verbose($"Removed file \"{destPath}\" in \"{_dest}\"");
                }
            }
            
            // Dest file does not exit, copy src to dest
            else if (isEmptyDestFile)
            {
                var srcFilePath = Path.Combine(_src, srcPath);
                var destFilePath = Path.Combine(_dest, srcPath);

                _fileCopier.Copy(srcFilePath, destFilePath);

                _logger.Verbose($"Copied file \"{srcPath}\" from \"{_src}\" to \"{_dest}\".");
            }
            
            // If both src and dest files exist, compare and sync
            else
            {
                var srcFilePath = Path.Combine(_src, srcPath);
                var destFilePath = Path.Combine(_dest, destPath);
                
                var isDifferentFile = !_fileComparer.GetIsEqualFile(srcFilePath, destFilePath);
                if (isDifferentFile)
                {
                    _fileMerger.Merge(srcFilePath, destFilePath);

                    _logger.Verbose($"Merged file \"{srcPath}\" from \"{_src}\" to \"{_dest}\".");
                }
            }

            pair.Done();
        }

        public void WatchAndSync()
        {
            var srcWatcher = new FileSystemWatcher
            {
                Path = _src,
                Filter = "*.*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            FileSystemEventArgs Selector(EventPattern<FileSystemEventArgs> pattern)
            {
                var e = pattern.EventArgs;

                if (_fileFilter.Filterd(e.Name)) return e;

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.All:
                        break;
                    case WatcherChangeTypes.Changed:
                        _logger.Verbose($"Source \"{e.FullPath}\" has been changed.");
                        break;
                    case WatcherChangeTypes.Created:
                        _logger.Verbose($"Source \"{e.FullPath}\" has been created.");

                        break;
                    case WatcherChangeTypes.Deleted:
                        _logger.Verbose($"Source \"{e.FullPath}\" has been deleted.");
                        break;
                    case WatcherChangeTypes.Renamed:
                        var renamedEventArgs = (RenamedEventArgs) e;
                        _logger.Verbose($"Source \"{renamedEventArgs.OldFullPath}\" has been renamed to \"{e.FullPath}\".");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return e;
            }

            var observables = new List<IObservable<FileSystemEventArgs>>
            {
                Observable.FromEventPattern<FileSystemEventArgs>(srcWatcher, "Changed").Select(Selector),
                Observable.FromEventPattern<FileSystemEventArgs>(srcWatcher, "Created").Select(Selector),
                Observable.FromEventPattern<FileSystemEventArgs>(srcWatcher, "Deleted").Select(Selector),
                Observable.FromEventPattern<FileSystemEventArgs>(srcWatcher, "Renamed").Select(Selector)
            };

            // Sync when no changes for 10 seconds
            observables
                .Merge()
                .Throttle(TimeSpan.FromSeconds(10))
                .Subscribe(e =>
                {
                    if (!_fileFilter.Filterd(e.Name)) Sync();
                });
        }
    }
}