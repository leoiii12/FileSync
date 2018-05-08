using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileSync.Filters;
using JetBrains.Annotations;
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

        public DirectoryStructureComparer([NotNull] ILogger<DirectoryStructureComparer> logger, [NotNull] IFileFilter fileFilter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        }

        public DirectoryStructureComparer Compare(string src, string dest)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (dest == null) throw new ArgumentNullException(nameof(dest));

            _logger.LogDebug("Computing the directory structure...");

            var srcFilePaths = Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories);
            var destFilePaths = Directory.EnumerateFiles(dest, "*.*", SearchOption.AllDirectories);

            var srcFiles = srcFilePaths
                .AsParallel()
                .Select(sfp => Path.GetRelativePath(src, sfp))
                .Where(sfp => !_fileFilter.Filterd(sfp))
                .ToHashSet();
            var destFiles = destFilePaths
                .AsParallel()
                .Select(sfp => Path.GetRelativePath(dest, sfp))
                .Where(sfp => !_fileFilter.Filterd(sfp))
                .ToHashSet();

            _logger.LogDebug("Computed the directory structure...");

            _addingFiles = srcFiles.Except(destFiles).ToArray();
            _removingFiles = destFiles.Except(srcFiles).ToArray();
            _files = srcFiles.Where(sf => destFiles.Contains(sf)).ToArray();

            _logger.LogInformation($"AddingFiles = {_addingFiles.Length}, RemovingFiles = {_removingFiles.Length}, Files = {_files.Length}");

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