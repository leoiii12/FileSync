using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Serilog;

namespace FileSync.Filters
{
    public class GitignoreParser
    {
        private const string Asterisk = "<<<ASTERISK>>>";
        private const string AsteriskRegEx = @"[^\f\n\r\t\v\u00A0\u2028\u2029\/]*";

        private readonly ILogger _logger;

        private readonly List<(string, string)> _availableUserEscapedCharacters = new List<(string, string)>
        {
            (@"\ ", "<<<SPACE>>>"),
            (@"\^", "<<<CARET>>>"),
            (@"\.", "<<<DOT>>>"),
            (@"\|", "<<<PIPE>>>"),
            (@"\$", "<<<DOLLAR>>>"),
            (@"\+", "<<<PLUS>>>"),
            (@"\(", "<<<OPENING>>>"),
            (@"\)", "<<<CLOSING>>>"),
            (@"\[", "<<<OPENING_BRACKET>>>"),
            (@"\]", "<<<CLOSING_BRACKET>>>"),
            (@"\!", "<<<EXCLAMATION>>>")
        };

        public GitignoreParser([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyList<GitignorePattern> ParseFile([NotNull] string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path)) return new List<GitignorePattern>();

            _logger.Verbose($"Parsing .fsignore {path}...");

            var lines = File.ReadAllLines(path);

            var patterns = lines
                .Select(ParsePattern)
                .Where(p => p.Expression != null)
                .ToArray();

            _logger.Verbose($"Parsed .fsignore {path}.");

            return patterns;
        }

        public IReadOnlyList<GitignorePattern> ParsePatterns([NotNull] IReadOnlyList<string> lines)
        {
            return lines
                .Select(ParsePattern)
                .Where(p => p.Expression != null)
                .ToArray();
        }

        public GitignorePattern ParsePattern(string line)
        {
            var pattern = new GitignorePattern();

            // RULE : A line starting with "!" serves as a negation.
            if (line.StartsWith("!"))
            {
                line = line.Substring(1);
                pattern.IsInclusive = true;
            }

            var regexString = ConvertToRegexString(line);

            if (regexString == null) return pattern;

            pattern.Expression = new Regex(regexString, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

            return pattern;
        }

        public string ConvertToRegexString(string line)
        {
            line = Santinize(line);

            if (line == @"\/" ||
                line == Asterisk ||
                line == Asterisk + Asterisk) return null;

            #region RULE : A blank line matches no files.

            if (string.IsNullOrWhiteSpace(line)) return null;

            #endregion

            #region RULE : "#line" A line starting with # serves as a comment.

            if (line.StartsWith("#")) return null;

            #endregion

            #region RULE : "?"

            line = line.Replace("?", ".");

            #endregion

            #region RULE : "**"

            if (line.Contains($@"\/{Asterisk}{Asterisk}\/"))
                line = "(?:" + line + "|" + line.Replace($@"\/{Asterisk}{Asterisk}\/", @"\/[\S\s]+\/") + ")";

            line = line.Replace($@"{Asterisk}{Asterisk}\/", @"");
            line = line.Replace($@"\/{Asterisk}{Asterisk}", @"");

            #endregion

            #region RULE : "*/line/*" matches folders and files which directly neigbours with the specified folder

            if (line.StartsWith(Asterisk + @"\/"))
            {
                line = line.Substring((Asterisk + @"\/").Length);
                line = AsteriskRegEx + @"\/" + line;
                line = "^" + line;
            }

            if (line.EndsWith(@"\/" + Asterisk))
            {
                line = line.Substring(0, line.LastIndexOf(@"\/" + Asterisk, StringComparison.Ordinal));
                line = line + @"\/" + AsteriskRegEx;
                line = line + "$";
            }

            #endregion

            #region RULE : "/line" matches only Root file

            if (line.StartsWith(@"\/"))
            {
                line = line.Substring(2);
                line = "^" + line;
            }

            #endregion

            #region RULE : "line/" matches only Directory

            if (line.EndsWith(@"\/"))
            {
                line = line.Substring(0, line.Length - @"\/".Length);
                line = line + @"(?:\/[\S\s]+)+" + "$";
            }

            #endregion

            #region RULE : "line" matches both File or Directory 

            if (!line.StartsWith("^")) line = "^" + @"(?:[\S\s]+\/)*" + line;
            if (!line.EndsWith("$")) line = line + @"(?:\/[\S\s]+)*" + "$";

            #endregion

            #region RULE : "*" in the middle of "line"

            line = line.Replace(Asterisk, AsteriskRegEx);

            #endregion

            line = Desantinize(line);

            return line;
        }

        private string Santinize(string line)
        {
            line = line.Trim();
            line = line.Replace("*", Asterisk);

            // Preserve user-escaped characters
            foreach (var (c1, c2) in _availableUserEscapedCharacters) line = line.Replace(c1, c2);

            // Escape special characters for processing
            line = line.Replace(".", @"\.");
            line = line.Replace("/", @"\/");
            line = line.Replace("$", @"\$");

            return line;
        }

        private string Desantinize(string line)
        {
            line = line.Replace(Asterisk, "*");

            // Put back user-escaped characters
            foreach (var (c1, c2) in _availableUserEscapedCharacters) line = line.Replace(c2, c1);

            return line;
        }
    }

    public class GitignorePattern
    {
        public Regex Expression { get; set; }

        public bool IsInclusive { get; set; }

        public bool Include(string path)
        {
            return IsInclusive && Expression.IsMatch(path);
        }

        public bool Exclude(string path)
        {
            return !IsInclusive && Expression.IsMatch(path);
        }
    }
}