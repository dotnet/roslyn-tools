using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SignTool.UnitTests
{
    public class ProgramTests
    {
        public class ReadConfigFileTests : ProgramTests
        {
            private BatchSignInput Load(string json)
            {
                using (var reader = new StringReader(json))
                using (var writer = new StringWriter())
                {
                    BatchSignInput data;
                    Assert.True(Program.TryReadConfigFile(writer, reader, @"q:\outputPath", out data));
                    Assert.True(string.IsNullOrEmpty(writer.ToString()));
                    return data;
                }
            }

            [Fact]
            public void MissingExcludeSection()
            {
                var json = @"
{
    ""sign"": []
}";
                var data = Load(json);
                Assert.Equal(0, data.BinaryNames.Length);
                Assert.Equal(0, data.ExternalBinaryNames.Length);
            }
        }
    }
}
