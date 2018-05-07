using System;
using System.IO;

namespace FileSync.FileWatchers
{
    public interface IFileWatcher : IDisposable
    {
        IObservable<FileSystemEventArgs> Changes { get; }

        void Watch(string src);
    }
}