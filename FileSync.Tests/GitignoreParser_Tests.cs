using FileSync.Filters;
using Xunit;

namespace FileSync.Tests
{
    public class GitignoreParser_Tests
    {
        [Fact]
        public void Convert_Test()
        {
            // RegEx Reference: http://www.javascriptkit.com/javatutors/redev2.shtml

            var gitignoreParser = new GitignoreParser();

            Assert.Null(gitignoreParser.ConvertToRegEx("/"));
            Assert.Null(gitignoreParser.ConvertToRegEx("*"));
            Assert.Null(gitignoreParser.ConvertToRegEx("**"));

            // RULE : A blank line matches no files.
            Assert.Null(gitignoreParser.ConvertToRegEx("     "));

            // RULE : A line starting with # serves as a comment.
            Assert.Null(gitignoreParser.ConvertToRegEx("# User-specific files"));
            Assert.Null(gitignoreParser.ConvertToRegEx(" # User-specific files"));
            Assert.Null(gitignoreParser.ConvertToRegEx(" #      User-specific files "));

            // RULE : "?"
            Assert.Equal(@"(^.|[\S\s]*\/.)$", gitignoreParser.ConvertToRegEx("?"));
            Assert.Equal(@"(^.\.json|[\S\s]*\/.\.json)$", gitignoreParser.ConvertToRegEx("?.json"));

            // RULE : "**"
            // These are not the best representations but do the same jobs
            Assert.Equal(@"(^[\S\s]+\/Properties\/launchSettings\.json|[\S\s]*\/[\S\s]+\/Properties\/launchSettings\.json)$", gitignoreParser.ConvertToRegEx("**/Properties/launchSettings.json"));
            Assert.Equal(@"(^Properties\/[\S\s]+\/launchSettings\.json|[\S\s]*\/Properties\/[\S\s]+\/launchSettings\.json)$", gitignoreParser.ConvertToRegEx("Properties/**/launchSettings.json"));
            Assert.Equal(@"(^Properties\/launchSettings\/[\S\s]+|[\S\s]*\/Properties\/launchSettings\/[\S\s]+)$", gitignoreParser.ConvertToRegEx("Properties/launchSettings/**"));
            Assert.Equal(@"(^Properties\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/launchSettings\/[\S\s]+|[\S\s]*\/Properties\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/launchSettings\/[\S\s]+)$",
                gitignoreParser.ConvertToRegEx("Properties/*/launchSettings/**"));

            // RULE : Special case for "/" line "/"
            Assert.Equal(@"^lib\/[\S\s]*$", gitignoreParser.ConvertToRegEx("/lib/"));

            // RULE : A line starting with "*" serves as directory specifier
            Assert.Equal(@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg$", gitignoreParser.ConvertToRegEx("*/*.jpg"));
            Assert.Equal(@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg$", gitignoreParser.ConvertToRegEx("*/*/*.jpg"));

            // RULE : A line starting with "/" serves as root
            Assert.Equal(@"^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.c$", gitignoreParser.ConvertToRegEx("/*.c"));

            // RULE : A line ending with "/" matches with everything at the beneath
            Assert.Equal(@"(^lib\/[\S\s]*|[\S\s]*\/lib\/[\S\s]*)$", gitignoreParser.ConvertToRegEx("lib/"));
            Assert.Equal(@"(^[Dd]ebug\/[\S\s]*|[\S\s]*\/[Dd]ebug\/[\S\s]*)$", gitignoreParser.ConvertToRegEx("[Dd]ebug/"));
            Assert.Equal(@"(^[Bb]in\/[\S\s]*|[\S\s]*\/[Bb]in\/[\S\s]*)$", gitignoreParser.ConvertToRegEx("[Bb]in/"));
            Assert.Equal(@"(^\.vs\/[\S\s]*|[\S\s]*\/\.vs\/[\S\s]*)$", gitignoreParser.ConvertToRegEx(".vs/"));

            // RULE : A line without starting and ending specifiers
            Assert.Equal(@"(^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg|[\S\s]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.jpg)$", gitignoreParser.ConvertToRegEx("*.jpg"));
            Assert.Equal(gitignoreParser.ConvertToRegEx("*.jpg"), gitignoreParser.ConvertToRegEx(" *.jpg "));
            Assert.Equal(@"(^[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.sln\.docstates|[\S\s]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\.sln\.docstates)$", gitignoreParser.ConvertToRegEx("*.sln.docstates"));
            Assert.Equal(@"(^abc\.jpg|[\S\s]*\/abc\.jpg)$", gitignoreParser.ConvertToRegEx("abc.jpg"));
            Assert.Equal(@"(^\.jpg|[\S\s]*\/\.jpg)$", gitignoreParser.ConvertToRegEx(".jpg"));
            Assert.Equal(@"(^[Bb]uild[Ll]og\.[^\f\n\r\t\v\u00A0\u2028\u2029\/]*|[\S\s]*\/[Bb]uild[Ll]og\.[^\f\n\r\t\v\u00A0\u2028\u2029\/]*)$", gitignoreParser.ConvertToRegEx("[Bb]uild[Ll]og.*"));

            // RULE : Wildcard in path
            Assert.Equal(@"(^[Tt]est[Rr]esult[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[\S\s]*|[\S\s]*\/[Tt]est[Rr]esult[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[\S\s]*)$", gitignoreParser.ConvertToRegEx("[Tt]est[Rr]esult*/"));
            Assert.Equal(@"(^[Tt]est[Rr]esult\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*|[\S\s]*\/[Tt]est[Rr]esult\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*\/[^\f\n\r\t\v\u00A0\u2028\u2029\/]*)$",
                gitignoreParser.ConvertToRegEx("[Tt]est[Rr]esult/*/*"));
        }
    }
}