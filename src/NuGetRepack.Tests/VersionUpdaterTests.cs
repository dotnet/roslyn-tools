// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Tools.NuGet.Repack;
using Xunit;

namespace NuGetRepack.Tests
{
    public class VersionUpdaterTests
    {
        private static void AssertPackagesEqual(byte[] expected, byte[] actual)
        {
            // Compare parts of the packages.
            // The zip archive contains file time stamps hence comparing raw bits directly is impractical.

            (string name, byte[] blob)[] GetPackageParts(byte[] packageBytes)
            {
                using (var package = new ZipArchive(new MemoryStream(packageBytes), ZipArchiveMode.Read))
                {
                    return package.Entries.Select(e =>
                    {
                        using (var s = e.Open())
                        {
                            var m = new MemoryStream();
                            s.CopyTo(m);
                            return (e.FullName, m.ToArray());
                        }
                    }).ToArray();
                }
            }

            var expectedParts = GetPackageParts(expected);
            var actualParts = GetPackageParts(actual);

            Assert.Equal(expectedParts.Length, actualParts.Length);
            for (int i = 0; i < expectedParts.Length; i++)
            {
                Assert.Equal(expectedParts[i].name, actualParts[i].name);
                AssertEx.Equal(expectedParts[i].blob, actualParts[i].blob);

                // all parts of test packages are XML documents, test that they can be loaded:
                XDocument.Load(new MemoryStream(actualParts[i].blob));
            }
        }

        [Fact]
        public void TestPackages()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);

            string a_daily, b_daily, c_daily, d_daily;
            File.WriteAllBytes(a_daily = Path.Combine(dir, TestResources.DailyBuildPackages.NameA), TestResources.DailyBuildPackages.A);
            File.WriteAllBytes(b_daily = Path.Combine(dir, TestResources.DailyBuildPackages.NameB), TestResources.DailyBuildPackages.B);
            File.WriteAllBytes(c_daily = Path.Combine(dir, TestResources.DailyBuildPackages.NameC), TestResources.DailyBuildPackages.C);
            File.WriteAllBytes(d_daily = Path.Combine(dir, TestResources.DailyBuildPackages.NameD), TestResources.DailyBuildPackages.D);

            var a_pre = Path.Combine(dir, TestResources.PreReleasePackages.NameA);
            var b_pre = Path.Combine(dir, TestResources.PreReleasePackages.NameB);
            var c_pre = Path.Combine(dir, TestResources.PreReleasePackages.NameC);
            var d_pre = Path.Combine(dir, TestResources.PreReleasePackages.NameD);

            var a_rel = Path.Combine(dir, TestResources.ReleasePackages.NameA);
            var b_rel = Path.Combine(dir, TestResources.ReleasePackages.NameB);
            var c_rel = Path.Combine(dir, TestResources.ReleasePackages.NameC);
            var d_rel = Path.Combine(dir, TestResources.ReleasePackages.NameD);

            VersionUpdater.Run(new[] { a_daily, b_daily, c_daily, d_daily }, dir, release: true);
            VersionUpdater.Run(new[] { a_daily, b_daily, c_daily, d_daily }, dir, release: false);

            AssertPackagesEqual(TestResources.ReleasePackages.A, File.ReadAllBytes(a_rel));
            AssertPackagesEqual(TestResources.ReleasePackages.B, File.ReadAllBytes(b_rel));
            AssertPackagesEqual(TestResources.ReleasePackages.C, File.ReadAllBytes(c_rel));
            AssertPackagesEqual(TestResources.ReleasePackages.D, File.ReadAllBytes(d_rel));

            AssertPackagesEqual(TestResources.PreReleasePackages.A, File.ReadAllBytes(a_pre));
            AssertPackagesEqual(TestResources.PreReleasePackages.B, File.ReadAllBytes(b_pre));
            AssertPackagesEqual(TestResources.PreReleasePackages.C, File.ReadAllBytes(c_pre));
            AssertPackagesEqual(TestResources.PreReleasePackages.D, File.ReadAllBytes(d_pre));

            Directory.Delete(dir, recursive: true);
        }

        [Fact]
        public void TestValidation()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);

            string a_daily, b_daily, c_daily;
            File.WriteAllBytes(a_daily = Path.Combine(dir, TestResources.DailyBuildPackages.NameA), TestResources.DailyBuildPackages.A);
            File.WriteAllBytes(b_daily = Path.Combine(dir, TestResources.DailyBuildPackages.NameB), TestResources.DailyBuildPackages.B);
            File.WriteAllBytes(c_daily = Path.Combine(dir, TestResources.DailyBuildPackages.NameC), TestResources.DailyBuildPackages.C);

            var e1 = Assert.Throws<InvalidOperationException>(() => VersionUpdater.Run(new[] { c_daily }, outDirectoryOpt: null, release: true));
            AssertEx.AreEqual("Package 'C' depends on a pre-release package 'B, [1.0.0-beta-12345-01]'", e1.Message);

            var e2 = Assert.Throws<AggregateException>(() => VersionUpdater.Run(new[] { a_daily }, outDirectoryOpt: null, release: true));
            AssertEx.Equal(new[]
            {
                "System.InvalidOperationException: Package 'A' depends on a pre-release package 'B, 1.0.0-beta-12345-01'",
                "System.InvalidOperationException: Package 'A' depends on a pre-release package 'C, (, 1.0.0-beta-12345-01]'",
                "System.InvalidOperationException: Package 'A' depends on a pre-release package 'C, 1.0.0-beta-12345-01'"
            }, e2.InnerExceptions.Select(i => i.ToString()));

            var e3 = Assert.Throws<AggregateException>(() => VersionUpdater.Run(new[] { a_daily, b_daily }, outDirectoryOpt: null, release: true));
            AssertEx.Equal(new[]
            {
                "System.InvalidOperationException: Package 'A' depends on a pre-release package 'C, (, 1.0.0-beta-12345-01]'",
                "System.InvalidOperationException: Package 'A' depends on a pre-release package 'C, 1.0.0-beta-12345-01'"
            }, e3.InnerExceptions.Select(i => i.ToString()));

            var e4 = Assert.Throws<AggregateException>(() => VersionUpdater.Run(new[] { a_daily, c_daily }, outDirectoryOpt: null, release: true));
            AssertEx.Equal(new[]
            {
                "System.InvalidOperationException: Package 'A' depends on a pre-release package 'B, 1.0.0-beta-12345-01'",
                "System.InvalidOperationException: Package 'C' depends on a pre-release package 'B, [1.0.0-beta-12345-01]'"
            }, e4.InnerExceptions.Select(i => i.ToString()));

            Directory.Delete(dir, recursive: true);
        }
    }
}
