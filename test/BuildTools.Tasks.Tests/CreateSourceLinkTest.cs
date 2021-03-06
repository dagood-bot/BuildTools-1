// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.BuildTools;
using Microsoft.DotNet.PlatformAbstractions;
using Xunit;

namespace BuildTools.Tasks.Tests
{
    public class CreateSourceLinkTest
    {
        [Fact]
        public void CreatesFile()
        {
            var destination = "sourcelink.json";

            var repoName = "aspnet/BuildTools";

            var originUrl = $"git@github.com:{repoName}.git";
            var commit = "5153bbdfa98dcc27a61d591ce09a0d632875e66f";
            var sourceLinkRoot = GetSourceLinkRoot();

            var task = new CreateSourceLink
            {
                OriginUrl = originUrl,
                Commit = commit,
                DestinationFile = destination,
                SourceLinkRoot = sourceLinkRoot
            };

            try
            {
                Assert.True(task.Execute(), "The task failed but should have passed.");
                Assert.True(File.Exists(destination), "SourceLink file doesn't exist.");

                var expectedUrl = $"https://raw.githubusercontent.com/{repoName}/{commit}/*";
                var expected = $"{{\"documents\":{{\"{GetExpectedRootDirectory()}*\":\"{expectedUrl}\"}}}}";

                var resultText = File.ReadAllText(destination);

                Assert.Equal(expected, resultText);
            }
            finally
            {
                File.Delete(destination);
            }
        }

        [Theory]
        [InlineData("/_/")]
        [InlineData("C:\\")]
        [InlineData("/home/src/")]
        public void DoesNotChangeBackslashes(string sourceLinkRoot)
        {
            var destination = "sourcelink.json";

            var repoName = "aspnet/BuildTools";

            var originUrl = $"git@github.com:{repoName}.git";
            var commit = "5153bbdfa98dcc27a61d591ce09a0d632875e66f";

            var task = new CreateSourceLink
            {
                OriginUrl = originUrl,
                Commit = commit,
                DestinationFile = destination,
                SourceLinkRoot = sourceLinkRoot
            };

            try
            {
                Assert.True(task.Execute(), "The task failed but should have passed.");
                Assert.True(File.Exists(destination), "SourceLink file doesn't exist.");
                var expectedSourceLinkRoot = sourceLinkRoot.Replace(@"\", @"\\");
                var expectedUrl = $"https://raw.githubusercontent.com/{repoName}/{commit}/*";
                var expected = $"{{\"documents\":{{\"{expectedSourceLinkRoot}*\":\"{expectedUrl}\"}}}}";

                var resultText = File.ReadAllText(destination);

                Assert.Equal(expected, resultText);
            }
            finally
            {
                File.Delete(destination);
            }
        }

        [Fact]
        public void HandlesHttps()
        {
            var destination = "sourcelink.json";

            var repoName = "aspnet/BuildTools";

            var originUrl = $"https://github.com/{repoName}.git";
            var commit = "5153bbdfa98dcc27a61d591ce09a0d632875e66f";
            var rootDir = GetSourceLinkRoot();

            var task = new CreateSourceLink
            {
                OriginUrl = originUrl,
                Commit = commit,
                DestinationFile = destination,
                SourceLinkRoot = rootDir
            };

            try
            {
                Assert.True(task.Execute(), "The task failed but should have passed.");
                Assert.True(File.Exists(destination), "SourceLink file doesn't exist.");

                var expectedUrl = $"https://raw.githubusercontent.com/{repoName}/{commit}/*";
                var expected = $"{{\"documents\":{{\"{GetExpectedRootDirectory()}*\":\"{expectedUrl}\"}}}}";

                Assert.Equal(expected, File.ReadAllText(destination));
            }
            finally
            {
                File.Delete(destination);
            }
        }

        private static string GetExpectedRootDirectory()
        {
            switch (RuntimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    return "C:\\\\";
                case Platform.Linux:
                case Platform.Darwin:
                    return "/home/";
                default:
                    throw new NotImplementedException($"SourceLink tests don't yet support {RuntimeEnvironment.OperatingSystemPlatform}.");
            }
        }

        private static string GetSourceLinkRoot()
        {
            switch (RuntimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    return @"C:\";
                case Platform.Linux:
                case Platform.Darwin:
                    return "/home/";
                default:
                    throw new NotImplementedException($"SourceLink tests don't yet support {RuntimeEnvironment.OperatingSystemPlatform}.");
            }
        }
    }
}
