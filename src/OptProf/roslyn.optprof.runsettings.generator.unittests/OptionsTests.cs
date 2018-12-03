using roslyn.optprof.runsettings.generator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace roslyn.optprof.unittests
{
    public class ArgumentsTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData(null, "")]
        public async Task NullArguments(string configFile, string outputFolder)
        {
            var result = await Program.ExecuteAsync(
                configFile: configFile,
                outputFolder: outputFolder,
                teamProject: null,
                repoName: null,
                sourceBranchName: null,
                buildId: null,
                insertTargetBranch: null,
                testsUrl: null,
                buildNumber: null,
                console: null);
            Assert.True(result != 0);
        }

        public Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private class TestFileWriter : IFileWriter
        {
            private Func<string, string, int> _writeOutFile;

            public TestFileWriter(Func<string, string, int> writeOutFile)
            {
                _writeOutFile = writeOutFile;
            }

            public int WriteOutFile(string outputFolder, string runSettings) => _writeOutFile(outputFolder, runSettings);
        }
    }
}
