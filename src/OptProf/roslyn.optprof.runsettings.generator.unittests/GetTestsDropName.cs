// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using System.IO;
using Xunit;

namespace roslyn.optprof.runsettings.generator.UnitTests
{
    public class GetTestsDropName
    {
        [Theory]
        [InlineData(@"[{""BuildDrop"": ""https://vsdrop.corp.microsoft.com/file/v1/Products/42.42.42.42/42.42.42.42""}]", "Tests/42.42.42.42/42.42.42.42")]
        public static void TestsCorrectJsonFiles(string jsonString, string expectedUrl)
        {
            Assert.Equal(expectedUrl, Program.GetTestsDropName(jsonString));
        }

        [Theory]
        [InlineData("")]
        [InlineData(@"[{""BuildDrop"": ""https://vsdrop.corp.microsoft.com/file/v1/Tests/42.42.42.42/42.42.42.42""}]")]
        [InlineData(@"Products/42.42.42.42/42.42.42.42")]
        public static void TestsIncorrectJsonFiles(string jsonString)
        {
            Assert.Throws<InvalidDataException>(() => Program.GetTestsDropName(jsonString));
        }
    }
}
