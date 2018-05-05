using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileSync.Filters
{
    public class GitignoreParser
    {
        private const string OriginalAsterisk = "<<<ASTERISK>>>";
        private const string WildcardRegEx = @"[^\f\n\r\t\v\u00A0\u2028\u2029\/]*";

        private readonly List<(string, string)> _availableUserEscapedCharacters = new List<(string, string)>
        {
            (@"\ ", "<<<SPACE>>>"),
            (@"\#", "<<<SHARP>>>"),
            (@"\!", "<<<EXCLAMATION>>>")
        };

        public IReadOnlyList<GitignorePattern> ParseFile(string path)
        {
            var lines = File.ReadAllLines(path);

            return lines.Select(ParseLine).ToArray();
        }

        public GitignorePattern ParseLine(string line)
        {
            var pattern = new GitignorePattern();

            // RULE : A line starting with # serves as a negations.
            if (line.StartsWith("!"))
                pattern.IsInclusive = false;

            return pattern;
        }

        public string ConvertToRegEx(string line)
        {
            line = Santinize(line);

            if (line == @"\/" ||
                line == OriginalAsterisk ||
                line == OriginalAsterisk + OriginalAsterisk) return null;

            // RULE : A blank line matches no files.
            if (string.IsNullOrWhiteSpace(line)) return null;

            // RULE : A line starting with # serves as a comment.
            if (line.StartsWith("#")) return null;

            // RULE : "?"
            line = line.Replace("?", ".");

            // RULE : "**"
            line = line.Replace($"{OriginalAsterisk}{OriginalAsterisk}", @"[\S\s]+");

            // RULE : Special case for "/" line "/"
            if (line.StartsWith(@"\/") && line.EndsWith(@"\/"))
            {
                line = line.Substring(2);
                line = line.Replace(OriginalAsterisk, WildcardRegEx);
                line = "^" + line + @"[\S\s]*" + "$";
            }
            else
            {
                // RULE : A line starting with "*/" serves as directory specifier
                if (line.StartsWith(OriginalAsterisk + @"\/"))
                {
                    line = line.Substring(OriginalAsterisk.Length);
                    line = $@"^{WildcardRegEx}{line}$";
                }

                // RULE : A line starting with "/" serves as root
                else if (line.StartsWith(@"\/"))
                {
                    line = line.Substring(2);
                    line = line.Replace(OriginalAsterisk, WildcardRegEx);
                    line = "^" + line + "$";
                }

                // RULE : A line ending with "/" matches with everything at the beneath
                else if (line.EndsWith(@"\/"))
                {
                    line = line + @"[\S\s]*";
                    line = $@"(^{line}|[\S\s]*\/{line})$";
                }

                // RULE : A line without starting and ending specifiers
                else
                {
                    line = $@"(^{line}|[\S\s]*\/{line})$";
                }
            }

            // RULE : Wildcard in path
            if (line.Contains(OriginalAsterisk))
            {
                line = line.Replace(OriginalAsterisk, WildcardRegEx);

                if (!line.EndsWith("$")) line = line + "$";
            }

            // RULE : A line is read from the start to the end by default
            if (!line.StartsWith("^") && !line.EndsWith("$")) line = "^" + line + "$";

            line = Desantinize(line);

            return line;
        }

        private string Santinize(string line)
        {
            line = line.Trim();
            line = line.Replace("*", OriginalAsterisk);

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
            line = line.Replace(OriginalAsterisk, "*");

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