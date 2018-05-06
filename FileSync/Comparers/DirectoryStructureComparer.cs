using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileSync.Filters;
using JetBrains.Annotations;
using Serilog;

namespace FileSync.Comparers
{
    public class DirectoryStructureComparer : IDirectoryStructureComparer
    {
        private readonly IFileFilter _fileFilter;
        private readonly ILogger _logger;

        private string[] _addingFiles;
        private string[] _files;
        private string[] _removingFiles;

        public DirectoryStructureComparer([NotNull] ILogger logger, [NotNull] IFileFilter fileFilter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        }

        public DirectoryStructureComparer Compare(string src, string dest)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (dest == null) throw new ArgumentNullException(nameof(dest));

            _logger.Verbose("Computing the directory structure...");

            var srcFilePaths = Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories);
            var destFilePaths = Directory.EnumerateFiles(dest, "*.*", SearchOption.AllDirectories);

            var srcFiles = srcFilePaths
                .AsParallel()
                .Where(sfp => !_fileFilter.Filterd(sfp))
                .Select(sfp => Path.GetRelativePath(src, sfp))
                .ToHashSet();
            var destFiles = destFilePaths
                .AsParallel()
                .Where(sfp => !_fileFilter.Filterd(sfp))
                .Select(sfp => Path.GetRelativePath(dest, sfp))
                .ToHashSet();

            _logger.Verbose("Computed the directory structure...");

            _addingFiles = srcFiles.Except(destFiles).ToArray();
            _removingFiles = destFiles.Except(srcFiles).ToArray();
            _files = srcFiles.Where(sf => destFiles.Contains(sf)).ToArray();

            _logger.Information($"AddingFiles = {_addingFiles.Length}, RemovingFiles = {_removingFiles.Length}, Files = {_files.Length}");

            return this;
        }

        public IEnumerable<(string, string)> ToTuples()
        {
            if (_addingFiles == null || _files == null || _removingFiles == null) throw new Exception($"Please ${nameof(Compare)} first.");

            var tuples = new List<(string, string)>();
            tuples.AddRange(_addingFiles.Select(af => (af, string.Empty)));
            tuples.AddRange(_removingFiles.Select(rf => (string.Empty, rf)));
            tuples.AddRange(_files.Select(f => (f, f)));

            return tuples;
        }
    }
}