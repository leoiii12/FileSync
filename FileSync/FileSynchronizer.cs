using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FileSync.Comparers;
using Serilog;

namespace FileSync
{
    public class FileSynchronizer
    {
        private readonly AppConfig _appConfig;
        private readonly IDeepFileComparer _deepFileComparer;
        private readonly string _dest;
        private readonly IDirectoryStructureComparer _directoryStructureComparer;
        private readonly IFileFilter _fileFilter;
        private readonly ILogger _logger;
        private readonly string _src;

        public FileSynchronizer(
            AppConfig appConfig,
            ILogger logger,
            IFileFilter fileFilter,
            IDirectoryStructureComparer directoryStructureComparer,
            IDeepFileComparer deepFileComparer)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
            _directoryStructureComparer = directoryStructureComparer ?? throw new ArgumentNullException(nameof(directoryStructureComparer));
            _deepFileComparer = deepFileComparer ?? throw new ArgumentNullException(nameof(deepFileComparer));

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

                if (!_appConfig.ShouldKeepRemovedFilesInDest) return;

                DeleteFile(Path.Combine(_dest, destFile));

                _logger.Verbose($"Removed file \"{destFile}\" in \"{_dest}\"");
            }
            else if (isEmptyDestFile)
            {
                // 2. Dest file does not exits

                var srcFilePath = Path.Combine(_src, srcFile);
                var destFilePath = Path.Combine(_dest, srcFile);

                CopyFile(srcFilePath, destFilePath);

                _logger.Verbose($"Copied file \"{srcFile}\" from \"{_src}\" to \"{_dest}\".");
            }
            else
            {
                // 3. Both src and dest files exist => Compare

                var srcFilePath = Path.Combine(_src, srcFile);
                var destFilePath = Path.Combine(_dest, destFile);
                var isEqualFile = _deepFileComparer.GetIsEqualFile(srcFilePath, destFilePath);

                if (isEqualFile) return;

                MergeFile(srcFilePath, destFilePath);

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

            var changed = Observable
                .FromEventPattern<FileSystemEventArgs>(srcWatcher, "Changed")
                .Select(pattern =>
                {
                    var e = pattern.EventArgs;

                    _logger.Verbose($"Source {e.FullPath} has been modified.");

                    return e;
                });
            var created = Observable
                .FromEventPattern<FileSystemEventArgs>(srcWatcher, "Created")
                .Select(pattern =>
                {
                    var e = pattern.EventArgs;

                    _logger.Verbose($"Source {e.FullPath} has been created.");

                    return e;
                });
            var deleted = Observable
                .FromEventPattern<FileSystemEventArgs>(srcWatcher, "Deleted")
                .Select(pattern =>
                {
                    var e = pattern.EventArgs;

                    _logger.Verbose($"Source {e.FullPath} has been deleted.");

                    return e;
                });
            var renamed = Observable
                .FromEventPattern<FileSystemEventArgs>(srcWatcher, "Renamed")
                .Select(pattern =>
                {
                    var e = (RenamedEventArgs) pattern.EventArgs;

                    _logger.Verbose($"Source {e.OldFullPath} has been renamed to {e.FullPath}.");

                    return e;
                });

            var observables = new List<IObservable<FileSystemEventArgs>>
            {
                changed,
                created,
                deleted,
                renamed
            };

            observables.Merge().Throttle(TimeSpan.FromSeconds(1)).Subscribe(e => { Sync(); });
        }

        private void CopyFile(string srcFilePath, string destFilePath)
        {
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

        private void DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().ToString());
            }
        }

        private void MergeFile(string srcFilePath, string destFilePath)
        {
            DeleteFile(destFilePath);
            CopyFile(srcFilePath, destFilePath);
        }

        private void EnsureDirectory(string directoryName)
        {
            if (Directory.Exists(directoryName)) return;

            Directory.CreateDirectory(directoryName);
            _logger.Verbose($"The directory name \"{directoryName}\" does not exist. Created it.");
        }
    }
}