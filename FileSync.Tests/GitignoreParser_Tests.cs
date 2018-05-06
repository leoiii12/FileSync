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

        [Fact]
        public void Convert_Test_1()
        {
            var gitignoreParser = new GitignoreParser(_logger);

            // Assert.Equal(@"^(?:[\S\s]+\/)*hide(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("**/hide/**"));
            Assert.Equal(@"^git-sample-3\/foo\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*$", gitignoreParser.ConvertToRegexString("/git-sample-3/foo/*"));

            Assert.Null(gitignoreParser.ConvertToRegexString("/"));
            Assert.Null(gitignoreParser.ConvertToRegexString("*"));
            Assert.Null(gitignoreParser.ConvertToRegexString("**"));

            // RULE : A blank line matches no files.
            Assert.Null(gitignoreParser.ConvertToRegexString("     "));

            // RULE : "#line" A line starting with # serves as a comment.
            Assert.Null(gitignoreParser.ConvertToRegexString("# User-specific files"));
            Assert.Null(gitignoreParser.ConvertToRegexString(" # User-specific files"));
            Assert.Null(gitignoreParser.ConvertToRegexString(" #      User-specific files "));
        }

        [Fact]
        public void Convert_Test_2()
        {
            var gitignoreParser = new GitignoreParser(_logger);

            // RULE : "?"
            Assert.Equal(@"^(?:[\S\s]+\/)*logs(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("logs"));
            Assert.Equal(@"^(?:[\S\s]+\/)*.(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("?"));
            Assert.Equal(@"^(?:[\S\s]+\/)*.\.json(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("?.json"));

            // RULE : "**"
            Assert.Equal(@"^(?:[\S\s]+\/)*logs\/debug\.log(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("**/logs/debug.log"));
            Assert.Equal(@"^(?:[\S\s]+\/)*(?:Properties\/launchSettings\.json|Properties\/[\S\s]+\/launchSettings\.json)(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("Properties/**/launchSettings.json"));
            Assert.Equal(@"^(?:[\S\s]+\/)*Properties\/launchSettings(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("Properties/launchSettings/**"));
            Assert.Equal(@"^(?:[\S\s]+\/)*Properties\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/launchSettings(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("Properties/*/launchSettings/**"));
        }

        [Fact]
        public void Convert_Test_3()
        {
            var gitignoreParser = new GitignoreParser(_logger);

            // RULE : "*/line/*" matches folders and files which directly neigbours with the specified folder
            Assert.Equal(@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("*/*.jpg"));
            Assert.Equal(@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("*/*/*.jpg"));
            Assert.Equal(@"^(?:[\S\s]+\/)*[Tt]est[Rr]esult\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*$", gitignoreParser.ConvertToRegexString("[Tt]est[Rr]esult/*/*"));

            // RULE : "/line" matches only Root file
            Assert.Equal(@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.c(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("/*.c"));

            // RULE : "line/" matches only Directory
            Assert.Equal(@"^(?:[\S\s]+\/)*lib(?:\/[\S\s]+)+$", gitignoreParser.ConvertToRegexString("lib/"));
            Assert.Equal(@"^(?:[\S\s]+\/)*[Dd]ebug(?:\/[\S\s]+)+$", gitignoreParser.ConvertToRegexString("[Dd]ebug/"));
            Assert.Equal(@"^(?:[\S\s]+\/)*[Bb]in(?:\/[\S\s]+)+$", gitignoreParser.ConvertToRegexString("[Bb]in/"));
            Assert.Equal(@"^(?:[\S\s]+\/)*\.vs(?:\/[\S\s]+)+$", gitignoreParser.ConvertToRegexString(".vs/"));

            // RULE : Special case for "/line/"
            Assert.Equal(@"^lib(?:\/[\S\s]+)+$", gitignoreParser.ConvertToRegexString("/lib/"));
        }

        [Fact]
        public void Convert_Test_4()
        {
            var gitignoreParser = new GitignoreParser(_logger);

            // RULE : "line" matches both File or Directory 
            Assert.Equal(@"^(?:[\S\s]+\/)*[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("*.jpg"));
            Assert.Equal(gitignoreParser.ConvertToRegexString("*.jpg"), gitignoreParser.ConvertToRegexString(" *.jpg "));
            Assert.Equal(@"^(?:[\S\s]+\/)*[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.sln\.docstates(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("*.sln.docstates"));
            Assert.Equal(@"^(?:[\S\s]+\/)*abc\.jpg(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("abc.jpg"));
            Assert.Equal(@"^(?:[\S\s]+\/)*\.jpg(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString(".jpg"));
            Assert.Equal(@"^(?:[\S\s]+\/)*[Bb]uild[Ll]og\.[^\f\n\r\t\v\u00A0\u2028\u2029\/]*(?:\/[\S\s]+)*$", gitignoreParser.ConvertToRegexString("[Bb]uild[Ll]og.*"));
        }

        [Fact]
        public void Convert_Test_5()
        {
            var gitignoreParser = new GitignoreParser(_logger);

            // RULE : "*" in the middle of "line"
            Assert.Equal(@"^(?:[\S\s]+\/)*[Tt]est[Rr]esult[^\f\n\r\t\v\u00A0\u2028\u2029\/]*(?:\/[\S\s]+)+$", gitignoreParser.ConvertToRegexString("[Tt]est[Rr]esult*/"));
        }

        [Fact]
        public void ParseLine_Test()
        {
            var gitignoreParser = new GitignoreParser(_logger);

            GitignorePattern pattern;

            pattern = gitignoreParser.ParsePattern("*.png");
            Assert.False(pattern.IsInclusive);
            Assert.True(pattern.Expression.IsMatch("filesync.png"));
            Assert.True(pattern.Expression.IsMatch("1/filesync.png"));
            Assert.True(pattern.Expression.IsMatch("2/1/filesync.png"));

            pattern = gitignoreParser.ParsePattern("[Tt]est[Rr]esult*/");
            Assert.False(pattern.IsInclusive);
            Assert.True(pattern.Expression.IsMatch("Testresult/filesync.cs"));
            Assert.True(pattern.Expression.IsMatch("TestresultFILESYNC/filesync.cs"));
        }
    }
}