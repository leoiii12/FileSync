using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FileSync.Filters;
using Serilog;
using Serilog.Core;
using Xunit;

namespace FileSync.Tests
{
    // RegEx Reference: http://www.javascriptkit.com/javatutors/redev2.shtml
    public class GitignoreParser_Tests
    {
        private readonly Logger _logger;

        public GitignoreParser_Tests()
        {
            _logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }

        public static IEnumerable<object[]> ParseLine_TestData()
        {
            return new List<object[]>
            {
                // RULE : A blank line matches no files.
                new object[] {null, false, "     "},

                // RULE : "#line" A line starting with # serves as a comment.
                new object[] {null, false, "# User-specific files"},
                new object[] {null, false, " # User-specific files"},
                new object[] {null, false, " #      User-specific files "},

                new object[] {@"^(?:[\S\s]+\/)*[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.png(?:\/[\S\s]+)*$", false, "*.png"},
                new object[] {@"^(?:[\S\s]+\/)*[Tt]est[Rr]esult[^\f\n\r\t\v\u00A0\u2028\u2029\/]*(?:\/[\S\s]+)+$", false, "[Tt]est[Rr]esult*/"},
            };
        }

        [Theory]
        [MemberData(nameof(ParseLine_TestData))]
        public void ParseLine_Test(string expectedExpression, bool expectedIsInclusive, string line)
        {
            var gitignoreParser = new GitignoreParser(_logger);

            var pattern = gitignoreParser.ParseLine(line);
            Assert.Equal(expectedExpression, pattern.Expression?.ToString());
            Assert.Equal(expectedIsInclusive, pattern.IsInclusive);
        }

        public static IEnumerable<object[]> ConvertToRegexString_TestData()
        {
            return new List<object[]>
            {
                new object[] {null, "/"},
                new object[] {null, "*"},
                new object[] {null, "**"},

                // Previous error cases
                new object[] {@"^(?:[\S\s]+\/)*hide(?:\/[\S\s]+)*$", "**/hide/**"},
                new object[] {@"^git-sample-3\/foo\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*$", "/git-sample-3/foo/*"},

                // RULE : "?"
                new object[] {@"^(?:[\S\s]+\/)*logs(?:\/[\S\s]+)*$", "logs"},
                new object[] {@"^(?:[\S\s]+\/)*.(?:\/[\S\s]+)*$", "?"},
                new object[] {@"^(?:[\S\s]+\/)*.\.json(?:\/[\S\s]+)*$", "?.json"},

                // RULE : "**"
                new object[] {@"^(?:[\S\s]+\/)*logs\/debug\.log(?:\/[\S\s]+)*$", "**/logs/debug.log"},
                new object[] {@"^(?:[\S\s]+\/)*(?:Properties\/launchSettings\.json|Properties\/[\S\s]+\/launchSettings\.json)(?:\/[\S\s]+)*$", "Properties/**/launchSettings.json"},
                new object[] {@"^(?:[\S\s]+\/)*Properties\/launchSettings(?:\/[\S\s]+)*$", "Properties/launchSettings/**"},
                new object[] {@"^(?:[\S\s]+\/)*Properties\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/launchSettings(?:\/[\S\s]+)*$", "Properties/*/launchSettings/**"},

                // RULE : "*/line/*" matches folders and files which directly neigbours with the specified folder
                new object[] {@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg(?:\/[\S\s]+)*$", "*/*.jpg"},
                new object[] {@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg(?:\/[\S\s]+)*$", "*/*/*.jpg"},
                new object[] {@"^(?:[\S\s]+\/)*[Tt]est[Rr]esult\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*$", "[Tt]est[Rr]esult/*/*"},

                // RULE : "/line" matches only Root file
                new object[] {@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.c(?:\/[\S\s]+)*$", "/*.c"},

                // RULE : "line/" matches only Directory
                new object[] {@"^(?:[\S\s]+\/)*lib(?:\/[\S\s]+)+$", "lib/"},
                new object[] {@"^(?:[\S\s]+\/)*[Dd]ebug(?:\/[\S\s]+)+$", "[Dd]ebug/"},
                new object[] {@"^(?:[\S\s]+\/)*[Bb]in(?:\/[\S\s]+)+$", "[Bb]in/"},
                new object[] {@"^(?:[\S\s]+\/)*\.vs(?:\/[\S\s]+)+$", ".vs/"},

                // RULE : Special case for "/line/"
                new object[] {@"^lib(?:\/[\S\s]+)+$", "/lib/"},

                // RULE : "line" matches both File or Directory 
                new object[] {@"^(?:[\S\s]+\/)*[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg(?:\/[\S\s]+)*$", "*.jpg"},
                new object[] {@"^(?:[\S\s]+\/)*[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.sln\.docstates(?:\/[\S\s]+)*$", "*.sln.docstates"},
                new object[] {@"^(?:[\S\s]+\/)*abc\.jpg(?:\/[\S\s]+)*$", "abc.jpg"},
                new object[] {@"^(?:[\S\s]+\/)*\.jpg(?:\/[\S\s]+)*$", ".jpg"},
                new object[] {@"^(?:[\S\s]+\/)*[Bb]uild[Ll]og\.[^\f\n\r\t\v\u00A0\u2028\u2029\/]*(?:\/[\S\s]+)*$", "[Bb]uild[Ll]og.*"},

                // RULE : "*" in the middle of "line"
                new object[] {@"^(?:[\S\s]+\/)*[Tt]est[Rr]esult[^\f\n\r\t\v\u00A0\u2028\u2029\/]*(?:\/[\S\s]+)+$", "[Tt]est[Rr]esult*/"},
            };
        }

        [Theory]
        [MemberData(nameof(ConvertToRegexString_TestData))]
        public void ConvertToRegexString_Test(string expected, string line)
        {
            var gitignoreParser = new GitignoreParser(_logger);

            Assert.Equal(expected, gitignoreParser.ConvertToRegexString(line));
        }
    }
}