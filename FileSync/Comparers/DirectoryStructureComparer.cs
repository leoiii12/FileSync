using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace FileSync.Comparers
{
    public class DirectoryStructureComparer : IDirectoryStructureComparer
    {
        private readonly ILogger _logger;
        private readonly IFileFilter _fileFilter;

        private string[] _addingFiles;
        private string[] _files;
        private string[] _removingFiles;

        public DirectoryStructureComparer(ILogger logger, IFileFilter fileFilter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileFilter = fileFilter;
        }

        public DirectoryStructureComparer Compare(string src, string dest)
        {
            _logger.Verbose("Computing the directory structure...");

            if (!Directory.Exists(src))
            {
                Directory.CreateDirectory(src);
                _logger.Verbose($"src \"{src}\" does not exist. Created.");
            }

            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
                _logger.Verbose($"dest \"{dest}\" does not exist. Created.");
            }

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