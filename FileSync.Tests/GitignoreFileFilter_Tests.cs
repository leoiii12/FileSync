﻿using System.Collections.Generic;
using FileSync.Filters;
using FileSync.VirtualFileSystem;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FileSync.Tests
{
    internal class AppConfig : IAppConfig
    {
        public IFileSystem Src { get; } = null;
        public IFileSystem Dest { get; } = null;
        public string Log { get; } = "";
        public bool UseDeepFileComparer { get; }
        public bool KeepRemovedFilesInDest { get; }
    }

    public class GitignoreFileFilter_Tests
    {
        private readonly GitignoreFileFilter _gitignoreFileFilter;

        public GitignoreFileFilter_Tests()
        {
            var patterns = new List<string>
            {
                "*.[oa]",
                "*.html",
                "*.min.js",
                "!foo*.html",
                "foo-excl.html",
                "vmlinux*",
                @"\!important!.txt",
                "log/*.log",
                "!/log/foo.log",
                "**/logdir/log",
                "**/foodir/bar",
                "exclude/**",
                "!findthis*",
                "**/hide/**",
                "subdir/subdir2/",
                "/rootsubdir/",
                "dirpattern/",
                "README.md",

                // arch/foo/kernel/.gitignore
                "!/arch/foo/kernel/vmlinux*",

                // git-sample-3/.gitignore
                "/git-sample-3/*",
                "!/git-sample-3/foo",
                "/git-sample-3/foo/*",
                "!/git-sample-3/foo/bar",

                // htmldoc/.gitignore
                "!/htmldoc/*.html"
            };

            _gitignoreFileFilter = new GitignoreFileFilter(new AppConfig(), new GitignoreParser(NullLogger<GitignoreParser>.Instance));
            _gitignoreFileFilter.SetPatterns(patterns);
        }

        public static IEnumerable<object[]> Filtered_TestData()
        {
            // https://github.com/svent/gitignore-test

            return new List<object[]>
            {
                new object[] {"!important!.txt", false},
                new object[] {"Documentation/foo-excl.html", false},
                new object[] {"Documentation/foo.html", true},
                new object[] {"Documentation/gitignore.html", false},
                new object[] {"Documentation/test.a.html", false},
                new object[] {"arch/foo/kernel/vmlinux.lds.S", true},
                new object[] {"arch/foo/vmlinux.lds.S", false},
                new object[] {"bar/testfile", true},
                new object[] {"dirpattern", true},
                new object[] {"exclude/dir1/dir2/dir3/testfile", false},
                new object[] {"file.o", false},
                new object[] {"foodir/bar/testfile", false},
                new object[] {"git-sample-3/foo/bar", true},
                new object[] {"git-sample-3/foo/test", false},
                new object[] {"git-sample-3/test", false},
                new object[] {"htmldoc/docs.html", true},
                new object[] {"htmldoc/jslib.min.js", false},
                new object[] {"lib.a", false},
                new object[] {"log/foo.log", true},
                new object[] {"log/test.log", false},
                new object[] {"rootsubdir/foo", false},
                new object[] {"src/findthis.o", true},
                new object[] {"src/internal.o", false},
                new object[] {"subdir/hide/foo", false},
                new object[] {"subdir/logdir/log/findthis.log", false},
                new object[] {"subdir/logdir/log/foo.log", false},
                new object[] {"subdir/logdir/log/test.log", false},
                new object[] {"subdir/rootsubdir/foo", true},
                new object[] {"subdir/subdir2/bar", false}
            };
        }

        [Theory]
        [MemberData(nameof(Filtered_TestData))]
        public void Filtered_Test(string path, bool expected)
        {
            var isIncluded = !_gitignoreFileFilter.Filterd(path);

            Assert.Equal(expected, isIncluded);
        }
    }
}