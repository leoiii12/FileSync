using System;
using System.Collections.Generic;
using System.Linq;
using FileSync.Filters;
using FileSync.VirtualFileSystem;
using Microsoft.Extensions.Logging;

namespace FileSync.Comparers
{
    public class DirectoryStructureComparer : IDirectoryStructureComparer
    {
        private readonly IFileFilter _fileFilter;
        private readonly ILogger<DirectoryStructureComparer> _logger;

        private string[] _addingFiles;
        private string[] _files;
        private string[] _removingFiles;

        public DirectoryStructureComparer(ILogger<DirectoryStructureComparer> logger, IFileFilter fileFilter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        }

        public DirectoryStructureComparer Compare(IFileSystem srcFileSystem, IFileSystem destFileSystem)
        {
            if (srcFileSystem == null) throw new ArgumentNullException(nameof(srcFileSystem));
            if (destFileSystem == null) throw new ArgumentNullException(nameof(destFileSystem));

            _logger.LogDebug("Computing the directory structure...");

            var srcFilePaths = srcFileSystem
                .EnumerateFiles()
                .AsParallel()
                .Where(sfp => !_fileFilter.Filterd(sfp))
                .ToHashSet();
            var destFilePaths = destFileSystem
                .EnumerateFiles()
                .AsParallel()
                .Where(sfp => !_fileFilter.Filterd(sfp))
                .ToHashSet();

            _logger.LogDebug("Computed the directory structure...");

            _addingFiles = srcFilePaths.Except(destFilePaths).ToArray();
            _removingFiles = destFilePaths.Except(srcFilePaths).ToArray();
            _files = srcFilePaths.Where(sf => destFilePaths.Contains(sf)).ToArray();

            _logger.LogInformation($"AddingFiles = {_addingFiles.Length}, RemovingFiles = {_removingFiles.Length}, Files = {_files.Length}.");

            return this;
        }

        public ICollection<Pair> ToPairs()
        {
            if (_addingFiles == null || _files == null || _removingFiles == null) throw new Exception($"Please ${nameof(Compare)} first.");

            var tuples = new List<Pair>();
            tuples.AddRange(_addingFiles.Select(af => new Pair(af, string.Empty)));
            tuples.AddRange(_removingFiles.Select(rf => new Pair(string.Empty, rf)));
            tuples.AddRange(_files.Select(f => new Pair(f, f)));

            return tuples.ToArray();
        }
    }
}