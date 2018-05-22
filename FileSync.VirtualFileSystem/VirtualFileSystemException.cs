using System;

namespace FileSync.VirtualFileSystem
{
    public class VirtualFileSystemException : Exception
    {
        public VirtualFileSystemException()
        {
        }

        public VirtualFileSystemException(string message) : base(message)
        {
        }

        public VirtualFileSystemException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}