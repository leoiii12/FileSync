using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace FileSync.Filters
{
    public class GitignoreParser
    {
        private const string Asterisk = "<<<ASTERISK>>>";
        private const string AsteriskRegEx = @"[^\f\n\r\t\v\u00A0\u2028\u2029\/]*?";

        private readonly List<(string, string, string)> _escapedCharacters = new List<(string, string, string)>
        {
            // Regex escaped characters
            (" ", "<<<SPACE>>>", @"\ "),
            ("^", "<<<CARET>>>", @"\^"),
            (".", "<<<DOT>>>", @"\."),
            ("|", "<<<PIPE>>>", @"\|"),
            ("$", "<<<DOLLAR>>>", @"\$"),
            ("+", "<<<PLUS>>>", @"\+"),
            ("(", "<<<OPEN>>>", @"\("),
            (")", "<<<CLOSE>>>", @"\)"),
            ("{", "<<<OPEN_BRACE>>>", @"\{"),
            ("}", "<<<CLOSE_BRACE>>>", @"\}"),

            // .gitignore escpaed characters
            (@"\*", "<<<STAR>>>", @"\*"),
            (@"\[", "<<<OPEN_BRACKET>>>", @"\["),
            (@"\]", "<<<CLOSE_BRACKET>>>", @"\]"),
            (@"\!", "<<<EXCLAMATION>>>", @"\!"),

            //
            (@"\", @"\/", @"\/"),
            (@"/", @"\/", @"\/")
        };

        private readonly ILogger<GitignoreParser> _logger;

        public GitignoreParser(ILogger<GitignoreParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyList<GitignorePattern> ParseFile(FileStream fileStream, string parentRelativePath = "")
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));

            if (fileStream.CanRead == false) return new List<GitignorePattern>();

            _logger.LogDebug($"Parsing .fsignore {fileStream.Name}...");

            string[] lines;
            using (var reader = new StreamReader(fileStream))
            {
                lines = reader.ReadToEnd().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }

            var patterns = lines
                .Select(l => ParseLine(l, parentRelativePath))
                .Where(p => p.Expression != null)
                .ToArray();

            _logger.LogDebug($"Parsed .fsignore {fileStream.Name}.");

            return patterns;
        }

        public IReadOnlyList<GitignorePattern> ParseLines(IReadOnlyList<string> lines, string parentRelativePath = "")
        {
            return lines
                .Select(l => ParseLine(l, parentRelativePath))
                .Where(p => p.Expression != null)
                .ToArray();
        }

        public GitignorePattern ParseLine(string line, string parentRelativePath = "")
        {
            var pattern = new GitignorePattern();

            line = line.Trim();

            #region RULE : A blank line matches no files.

            if (string.IsNullOrWhiteSpace(line))
            {
                pattern.Expression = null;
                return pattern;
            }

            #endregion

            #region RULE : "#line" A line starting with # serves as a comment.

            if (line.StartsWith("#"))
            {
                pattern.Expression = null;
                return pattern;
            }

            #endregion

            #region RULE : A line starting with "!" serves as a negation.

            if (line.StartsWith("!"))
            {
                line = line.Substring(1);
                pattern.IsInclusive = true;
            }

            #endregion

            if (parentRelativePath.Length > 0)
                line = "/" + parentRelativePath + line;

            line = ConvertToRegexString(line);

            pattern.Expression = new Regex(line, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

            return pattern;
        }

        public string ConvertToRegexString(string line)
        {
            line = Santinize(line);

            if (line == @"\/" ||
                line == Asterisk ||
                line == Asterisk + Asterisk) return null;

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

            if (!line.StartsWith("^")) line = "^" + @"(?:[\S\s]+\/)*?" + line;
            if (!line.EndsWith("$")) line = line + @"(?:\/[\S\s]+)*?" + "$";

            #endregion

            #region RULE : "*" in the middle of "line"

            line = line.Replace(Asterisk, AsteriskRegEx);

            #endregion

            line = Desantinize(line);

            return line;
        }

        private string Santinize(string line)
        {
            // Preserve user-escaped characters
            foreach (var (character, tempCharacter, _) in _escapedCharacters) line = line.Replace(character, tempCharacter);

            // Escape characters
            line = line.Replace("*", Asterisk);

            return line;
        }

        private string Desantinize(string line)
        {
            line = line.Replace(Asterisk, "*");

            // Put back escaped characters
            foreach (var (_, tempCharacter, escapedCharacter) in _escapedCharacters) line = line.Replace(tempCharacter, escapedCharacter);

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