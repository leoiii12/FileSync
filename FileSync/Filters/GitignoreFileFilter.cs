using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSync.Filters
{
    public class GitignoreFileFilter : IFileFilter
    {
        private readonly ConcurrentDictionary<string, bool> _directoryResultDictionary = new ConcurrentDictionary<string, bool>();
        private readonly GitignoreParser _gitignoreParser;

        private IReadOnlyList<GitignorePattern> _gitignorePatterns;

        public GitignoreFileFilter(IAppConfig appConfig, GitignoreParser gitignoreParser)
        {
            _gitignoreParser = gitignoreParser ?? throw new ArgumentNullException(nameof(gitignoreParser));

            var patterns = new List<GitignorePattern>();

            if (appConfig.Src != null)
            {
                var fsignorePaths = appConfig.Src.EnumerateFiles("/", ".fsignore").ToArray();

                foreach (var relativePath in fsignorePaths)
                {
                    var fsignoreParentRelativePath = relativePath.Substring(0, relativePath.LastIndexOf(".fsignore", StringComparison.Ordinal));

                    using (var file = appConfig.Src.OpenFile(relativePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        patterns.AddRange(_gitignoreParser.ParseFile(file, fsignoreParentRelativePath));
                    }
                }
            }

            _gitignorePatterns = patterns;
        }

        public bool Filterd(string path)
        {
            if (_gitignorePatterns == null)
                throw new Exception($"Please initialize {nameof(GitignoreFileFilter)}.");

            if (_gitignorePatterns.Count == 0)
                return false;

            var fileName = Path.GetFileName(path);
            var parentPath = path.Substring(0, path.LastIndexOf(fileName, StringComparison.Ordinal));

            var isRootFile = parentPath.Length == 0;
            if (isRootFile)
                return !IsIncluded(path);

            return !IsIncluded(parentPath) || !IsIncluded(path);
        }

        public void SetPatterns(IReadOnlyList<string> lines)
        {
            _gitignorePatterns = _gitignoreParser.ParseLines(lines);
        }

        private bool IsIncluded(string path)
        {
            var lastIncludedIndex = -1;
            var lastExcludedIndex = -2;

            var isDirectory = path.EndsWith("/");
            if (isDirectory)
            {
                path = path.Substring(0, path.Length - 1);

                var hasResult = _directoryResultDictionary.TryGetValue(path, out var result);

                if (hasResult) return result;
            }

            for (var i = 0; i < _gitignorePatterns.Count; i++)
            {
                var p = _gitignorePatterns[i];

                if (p.Expression.IsMatch(path))
                {
                    if (p.IsInclusive)
                        lastIncludedIndex = i;
                    else
                        lastExcludedIndex = i;
                }
            }

            var isIncluded = lastIncludedIndex > lastExcludedIndex;

            if (isDirectory) _directoryResultDictionary.TryAdd(path, isIncluded);

            return isIncluded;
        }
    }
}