using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSync.VirtualFileSystem
{
    public class SimpleFileSystem : IFileSystem, IEquatable<SimpleFileSystem>
    {
        private readonly string _rootPath;

        public SimpleFileSystem(string rootPath)
        {
            if (!Directory.Exists(rootPath))
                throw new VirtualFileSystemException($"The {rootPath} does not exist.");

            _rootPath = rootPath;
        }

        public void CreateDirectory(string vfsPath)
        {
            var actualPath = GetActualPath(vfsPath);

            Directory.CreateDirectory(actualPath);
        }

        public bool DirectoryExists(string vfsPath)
        {
            var actualPath = GetActualPath(vfsPath);

            return Directory.Exists(actualPath);
        }

        public IEnumerable<string> EnumerateFiles(string vfsPath = "/", string searchPattern = "*.*")
        {
            var actualPath = GetActualPath(vfsPath);

            return Directory
                .EnumerateFiles(actualPath, searchPattern, SearchOption.AllDirectories)
                .Select(sfp => Path.GetRelativePath(_rootPath, sfp));
        }

        public FileStream CreateFile(string vfsPath)
        {
            var actualPath = GetActualPath(vfsPath);

            return File.Create(actualPath);
        }

        public FileStream OpenFile(string vfsPath, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            var actualPath = GetActualPath(vfsPath);

            return new FileStream(actualPath, mode, access, share, bufferSize);
        }

        public FileInfo GetFileInfo(string vfsPath)
        {
            var actualPath = GetActualPath(vfsPath);

            return new FileInfo(actualPath);
        }

        public bool FileExists(string vfsPath)
        {
            var actualPath = GetActualPath(vfsPath);

            return File.Exists(actualPath);
        }

        public void CopyFile(string vfsSrcPath, string vfsDestPath, bool willOverwrite)
        {
            EnsureDifferentPaths(vfsSrcPath, vfsDestPath);
            EnsureNotOverwrite(vfsDestPath, willOverwrite);

            var actualSrcPath = GetActualPath(vfsSrcPath);
            var actualDestPath = GetActualPath(vfsDestPath);

            File.Copy(actualSrcPath, actualDestPath);
        }

        public void CopyFile(string vfsSrcPath, IFileSystem destFileSystem, string vfsDestPath, bool willOverwrite = false)
        {
            EnsureNotOverwrite(vfsDestPath, willOverwrite);

            using (var srcFileStream = OpenFile(vfsSrcPath, FileMode.Open, FileAccess.Read, FileShare.Read, 10 * 1024))
            {
                destFileSystem.WriteFile(vfsDestPath, srcFileStream);
            }
        }

        public void DeleteFile(string vfsPath)
        {
            var actualPath = GetActualPath(vfsPath);

            File.Delete(actualPath);
        }

        public void MoveFile(string vfsSrcPath, string vfsDestPath, bool willOverwrite = false)
        {
            EnsureDifferentPaths(vfsSrcPath, vfsDestPath);
            EnsureNotOverwrite(vfsDestPath, willOverwrite);

            var actualSrcPath = GetActualPath(vfsSrcPath);
            var actualDestPath = GetActualPath(vfsDestPath);

            File.Move(actualSrcPath, actualDestPath);
        }

        public void WriteFile(string vfsPath, FileStream srcFileStream)
        {
            if (!srcFileStream.CanRead)
                throw new VirtualFileSystemException($"The {nameof(srcFileStream)} cannot be read.");

            srcFileStream.Seek(0, SeekOrigin.Begin);

            using (var destFileStream = OpenFile(vfsPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 10 * 1024))
            {
                srcFileStream.CopyTo(destFileStream);
            }
        }

        public override string ToString()
        {
            return $"{nameof(SimpleFileSystem)}({_rootPath})";
        }

        private string GetActualPath(string path)
        {
            return path == "/" ? _rootPath : Path.Combine(_rootPath, path);
        }

        private void EnsureNotOverwrite(string vfsDestPath, bool willOverwrite)
        {
            var actualDestPath = GetActualPath(vfsDestPath);

            if (willOverwrite)
            {
                if (File.Exists(actualDestPath))
                    DeleteFile(actualDestPath);
            }
            else
            {
                if (File.Exists(actualDestPath))
                    throw new VirtualFileSystemException($"{nameof(actualDestPath)} is there already.");
            }
        }

        private void EnsureDifferentPaths(string vfsSrcPath, string vfsDestPath)
        {
            var actualSrcPath = GetActualPath(vfsSrcPath);
            var actualDestPath = GetActualPath(vfsDestPath);

            if (actualSrcPath == actualDestPath)
                throw new VirtualFileSystemException($"The source path should not be same as the destination path.");
        }

        public bool Equals(SimpleFileSystem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_rootPath, other._rootPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SimpleFileSystem) obj);
        }

        public override int GetHashCode()
        {
            return _rootPath != null ? _rootPath.GetHashCode() : 0;
        }
    }
}