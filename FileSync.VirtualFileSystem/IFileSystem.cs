using System.Collections.Generic;
using System.IO;

namespace FileSync.VirtualFileSystem
{
    public interface IFileSystem
    {
        #region Directory

        /// <summary>
        /// </summary>
        /// <param name="vfsPath"></param>
        void CreateDirectory(string vfsPath);

        /// <summary>
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <returns></returns>
        bool DirectoryExists(string vfsPath);

        #endregion

        #region File

        /// <summary>
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        IEnumerable<string> EnumerateFiles(string vfsPath = "/", string searchPattern = "*.*");

        /// <summary>
        ///     Create a empty file in the current VFS.
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <returns></returns>
        FileStream CreateFile(string vfsPath);

        /// <summary>
        ///     Initialize a new instance of FileStream.
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="share"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        FileStream OpenFile(string vfsPath, FileMode mode, FileAccess access, FileShare share, int bufferSize = 10 * 1024);

        FileInfo GetFileInfo(string vfsPath);

        /// <summary>
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <param name="srcFileStream"></param>
        void WriteFile(string vfsPath, FileStream srcFileStream);

        /// <summary>
        /// </summary>
        /// <param name="vfsPath"></param>
        /// <returns></returns>
        bool FileExists(string vfsPath);

        /// <summary>
        ///     Copy a file in the current VFS.
        /// </summary>
        /// <param name="vfsSrcPath"></param>
        /// <param name="vfsDestPath"></param>
        /// <param name="willOverwrite"></param>
        void CopyFile(string vfsSrcPath, string vfsDestPath, bool willOverwrite = true);

        /// <summary>
        ///     Copy a file from the current VFS to the destFileSystem.
        /// </summary>
        /// <param name="vfsSrcPath"></param>
        /// <param name="destFileSystem">The destination file system.</param>
        /// <param name="vfsDestPath"></param>
        /// <param name="willOverwrite">If willOverwrite is false, throw VirtualFileSystemException when a file exists on destPath.</param>
        void CopyFile(string vfsSrcPath, IFileSystem destFileSystem, string vfsDestPath, bool willOverwrite = false);

        /// <summary>
        ///     Delete a file in the current VFS.
        /// </summary>
        /// <param name="vfsPath"></param>
        void DeleteFile(string vfsPath);

        /// <summary>
        ///     Move a file in the current VFS.
        /// </summary>
        /// <param name="vfsSrcPath"></param>
        /// <param name="vfsDestPath"></param>
        /// <param name="willOverwrite">If willOverwrite is false, throw VirtualFileSystemException when a file exists on destPath.</param>
        void MoveFile(string vfsSrcPath, string vfsDestPath, bool willOverwrite = false);

        #endregion
    }
}