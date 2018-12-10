using System.IO;
using roslyn.optprof.runsettings.generator;
using Xunit;

namespace roslyn.optprof.unittests
{
    public class GetTestsUrl
    {
        [Fact]
        public static void TestsCorrectJsonFiles()
        {
            var jsonString = @"[{""BuildDrop"": ""https://vsdrop.corp.microsoft.com/file/v1/Products/42.42.42.42/42.42.42.42""}]";
            using (var reader = new StreamReader(GenerateStreamFromString(jsonString)))
            {
                var (result, testsUrl) = Program.GetTestsUrl(reader);
                Assert.True(result);
                Assert.Equal("vstsdrop:Tests/42.42.42.42/42.42.42.42", testsUrl);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(@"[{""BuildDrop"": ""https://vsdrop.corp.microsoft.com/file/v1/Tests/42.42.42.42/42.42.42.42""}]")]
        [InlineData(@"vstsdrop:Products/42.42.42.42/42.42.42.42")]
        public static void TestsInCorrectJsonFiles(string jsonString)
        {
            using (var reader = new StreamReader(GenerateStreamFromString(jsonString)))
            {
                var (result, testsUrl) = Program.GetTestsUrl(reader);
                Assert.False(result);
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
