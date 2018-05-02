using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace FileSync.Comparers
{
    public class DirectoryStructureComparer : IDirectoryStructureComparer
    {
        private readonly AppConfig _appConfig;
        private readonly ILogger _logger;

        private string[] _addingFiles;
        private string[] _removingFiles;
        private string[] _files;

        public DirectoryStructureComparer(AppConfig appConfig, ILogger logger)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DirectoryStructureComparer Compare()
        {
            var ignoringFiles = _appConfig.IgnoringFiles;
            var src = _appConfig.Src;
            var dest = _appConfig.Dest;

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
                .Where(sfp => ignoringFiles.All(inf => !sfp.EndsWith(inf)))
                .Select(sfp => sfp.Replace(src, string.Empty))
                .ToHashSet();
            var destFiles = destFilePaths
                .AsParallel()
                .Where(sfp => ignoringFiles.All(inf => !sfp.EndsWith(inf)))
                .Select(sfp => sfp.Replace(dest, string.Empty))
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
            if (_addingFiles == null || _files == null || _removingFiles == null)
            {
                Compare();
            }

            var tuples = new List<(string, string)>();
            tuples.AddRange(_addingFiles.Select(af => (af, string.Empty)));
            tuples.AddRange(_removingFiles.Select(rf => (string.Empty, rf)));
            tuples.AddRange(_files.Select(f => (f, f)));

            return tuples;
        }
    }
}