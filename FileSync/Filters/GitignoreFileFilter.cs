using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace FileSync.Filters
{
    public class GitignoreFileFilter : IFileFilter
    {
        private readonly GitignoreParser _gitignoreParser;
        private readonly ConcurrentDictionary<string, bool> _directoryResultDictionary = new ConcurrentDictionary<string, bool>();

        [CanBeNull] private IReadOnlyList<GitignorePattern> _gitignorePatterns;

        public GitignoreFileFilter([NotNull] IAppConfig appConfig, [NotNull] GitignoreParser gitignoreParser)
        {
            _gitignoreParser = gitignoreParser ?? throw new ArgumentNullException(nameof(gitignoreParser));
            _gitignorePatterns = _gitignoreParser.ParseFile(Path.Combine(appConfig.Src, ".fsignore"));
        }

        public void SetPatterns(IReadOnlyList<string> patterns)
        {
            _gitignorePatterns = _gitignoreParser.ParsePatterns(patterns);
        }

        public bool Filterd(string path)
        {
            if (_gitignorePatterns == null)
                throw new Exception($"Please initialize {nameof(GitignoreFileFilter)}.");

            var fileName = Path.GetFileName(path);
            var parentPath = path.Substring(0, path.LastIndexOf(fileName, StringComparison.Ordinal));

            var isRootFile = parentPath.Length == 0;
            if (isRootFile)
                return !IsIncluded(path);

            return !IsIncluded(parentPath) || !IsIncluded(path);
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

            if (isDirectory)
            {
                _directoryResultDictionary.TryAdd(path, isIncluded);
            }

            return isIncluded;
        }
    }
}