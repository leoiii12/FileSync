using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using FileSync.Filters;
using Serilog;

namespace FileSync.FileWatchers
{
    public class FileWatcher : IFileWatcher
    {
        private readonly IFileFilter _fileFilter;
        private readonly ILogger _logger;

        private FileSystemWatcher _srcWatcher;

        public FileWatcher(ILogger logger, IFileFilter fileFilter)
        {
            _logger = logger;
            _fileFilter = fileFilter;
        }

        public IObservable<FileSystemEventArgs> Changes { get; private set; }

        public void Watch(string src)
        {
            _srcWatcher = new FileSystemWatcher
            {
                Path = src,
                Filter = "*.*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            bool FileFilterPredicate(EventPattern<FileSystemEventArgs> e) => !_fileFilter.Filterd(e.EventArgs.Name);

            FileSystemEventArgs Selector(EventPattern<FileSystemEventArgs> pattern)
            {
                var e = pattern.EventArgs;

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
                Observable.FromEventPattern<FileSystemEventArgs>(_srcWatcher, "Changed").Where(FileFilterPredicate).Select(Selector),
                Observable.FromEventPattern<FileSystemEventArgs>(_srcWatcher, "Created").Where(FileFilterPredicate).Select(Selector),
                Observable.FromEventPattern<FileSystemEventArgs>(_srcWatcher, "Deleted").Where(FileFilterPredicate).Select(Selector),
                Observable.FromEventPattern<FileSystemEventArgs>(_srcWatcher, "Renamed").Where(FileFilterPredicate).Select(Selector)
            };

            Changes = observables.Merge();
        }

        public void Dispose()
        {
            _srcWatcher?.Dispose();
        }
    }
}