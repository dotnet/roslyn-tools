using System.IO;
using roslyn.optprof.runsettings.generator;
using Xunit;

namespace roslyn.optprof.unittests
{
    public class GetTestsUrl
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
