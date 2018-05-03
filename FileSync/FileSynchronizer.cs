using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FileSync.Comparers;
using FileSync.Operations;
using Serilog;

namespace FileSync
{
    public class FileSynchronizer
    {
        private readonly AppConfig _appConfig;
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
            AppConfig appConfig,
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
            _logger.Information("Synchronizing...");

            var tuples = _directoryStructureComparer.Compare(_src, _dest).ToTuples();

            Parallel.ForEach(tuples, new ParallelOptions {MaxDegreeOfParallelism = 4}, tuple => { SyncOnTuple(tuple); });

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
                // 1. src file no longer exists

                if (_appConfig.KeepRemovedFilesInDest) return;

                _fileDeleter.Delete(Path.Combine(_dest, destFile));

                _logger.Verbose($"Removed file \"{destFile}\" in \"{_dest}\"");
            }
            else if (isEmptyDestFile)
            {
                // 2. Dest file does not exits

                var srcFilePath = Path.Combine(_src, srcFile);
                var destFilePath = Path.Combine(_dest, srcFile);

                _fileCopier.Copy(srcFilePath, destFilePath);

                _logger.Verbose($"Copied file \"{srcFile}\" from \"{_src}\" to \"{_dest}\".");
            }
            else
            {
                // 3. Both src and dest files exist => Compare

                var srcFilePath = Path.Combine(_src, srcFile);
                var destFilePath = Path.Combine(_dest, destFile);
                var isEqualFile = _fileComparer.GetIsEqualFile(srcFilePath, destFilePath);

                if (isEqualFile) return;

                _fileMerger.Merge(srcFilePath, destFilePath);

                _logger.Verbose($"Merged file \"{srcFile}\" from \"{_src}\" to \"{_dest}\".");
            }
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

            observables
                .Merge()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e =>
                {
                    if (!_fileFilter.Filterd(e.Name)) Sync();
                });
        }
    }
}