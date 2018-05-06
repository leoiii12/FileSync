using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace FileSync.Filters
{
    public class GitignoreFileFilter : IFileFilter
    {
        private readonly IReadOnlyList<GitignorePattern> _gitignorePatterns;

        public GitignoreFileFilter([NotNull] IAppConfig appConfig, [NotNull] GitignoreParser gitignoreParser)
        {
            if (appConfig == null) throw new ArgumentNullException(nameof(appConfig));
            if (gitignoreParser == null) throw new ArgumentNullException(nameof(gitignoreParser));
            
            _gitignorePatterns = gitignoreParser.ParseFile(Path.Combine(appConfig.Src, ".gitignore"));
        }

        public GitignoreFileFilter([NotNull] IReadOnlyList<string> gitignorePatterns, [NotNull] GitignoreParser gitignoreParser)
        {
            if (gitignorePatterns == null) throw new ArgumentNullException(nameof(gitignorePatterns));
            if (gitignoreParser == null) throw new ArgumentNullException(nameof(gitignoreParser));
            Console.WriteLine(nameof(GitignoreFileFilter));

            _gitignorePatterns = gitignoreParser.ParsePatterns(gitignorePatterns);
        }

        public bool Filterd(string path)
        {
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

            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

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

            return lastIncludedIndex > lastExcludedIndex;
        }
    }
}