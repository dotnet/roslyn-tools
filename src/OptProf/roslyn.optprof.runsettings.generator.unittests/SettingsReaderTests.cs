using System.IO;
using roslyn.optprof.runsettings.generator;
using Xunit;

namespace roslyn.optprof.unittests
{
    public class SettingsReaderTests
    {
        [Theory]
        [InlineData(products_only, products_only_expectedContainerString, products_only_expectedTestCaseFilterString)]
        public void TestProductsOnly(string configFile, string expectedContainerString, string expectedTestCaseFilterString)
        {
            using (var reader = new StreamReader(GenerateStreamFromString(configFile)))
            {
                var (result, actualContainerString, actualTestCaseFilterString) = Program.GetContainerString(reader);
                Assert.True(result);
                Assert.Equal(expectedContainerString, actualContainerString);
                Assert.Equal(expectedTestCaseFilterString, actualTestCaseFilterString);
            }
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

        public const string products_only_expectedContainerString = "<TestContainer FileName=\"DDRIT.RPS.CSharp.dll\" />\r\n<TestContainer FileName=\"VSPE.dll\" />";
        public const string products_only_expectedTestCaseFilterString = "FullyQualifiedName=DDRIT.RPS.CSharp.CSharpTest.EditingAndDesigner|FullyQualifiedName=VSPE.OptProfTests.vs_perf_designtime_ide_searchtest|FullyQualifiedName=VSPE.OptProfTests.vs_perf_designtime_editor_intellisense_globalcompletionlist_cs|FullyQualifiedName=VSPE.OptProfTests.vs_asl_cs_scenario|FullyQualifiedName=VSPE.OptProfTests.vs_ddbvtqa_vbwi|FullyQualifiedName=VSPE.OptProfTests.vs_asl_vb_scenario|FullyQualifiedName=VSPE.OptProfTests.vs_env_solution_createnewproject_vb_winformsapp|FullyQualifiedName=DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging";

        public const string products_only = @"
{
  ""products"": [
    {
      ""name"": ""Roslyn.VisualStudio.Setup.vsix"",
      ""tests"": [
        {
          ""container"": ""DDRIT.RPS.CSharp"",
          ""testCases"": [
            ""DDRIT.RPS.CSharp.CSharpTest.EditingAndDesigner""
          ]
    },
        {
          ""container"": ""VSPE"",
          ""testCases"": [
            ""VSPE.OptProfTests.vs_perf_designtime_ide_searchtest"",
            ""VSPE.OptProfTests.vs_perf_designtime_editor_intellisense_globalcompletionlist_cs"",
            ""VSPE.OptProfTests.vs_asl_cs_scenario"",
            ""VSPE.OptProfTests.vs_ddbvtqa_vbwi"",
            ""VSPE.OptProfTests.vs_asl_vb_scenario"",
            ""VSPE.OptProfTests.vs_env_solution_createnewproject_vb_winformsapp""
          ]
}
      ]
    },
    {
      ""name"": ""ExpressionEvaluatorPackage.vsix"",
      ""tests"": [
        {
          ""container"": ""DDRIT.RPS.CSharp"",
          ""testCases"": [
            ""DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging""
          ]
        }
      ]
    },
    {
      ""name"": ""Microsoft.CodeAnalysis.Compilers.vsix"",
      ""tests"": [
        {
          ""container"": ""DDRIT.RPS.CSharp"",
          ""testCases"": [
            ""DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging""
          ]
        }
      ]
    },
    {
      ""name"": ""Roslyn.VisualStudio.InteractiveComponents.vsix"",
      ""tests"": [
        {
          ""container"": ""DDRIT.RPS.CSharp"",
          ""testCases"": [
            ""DDRIT.RPS.CSharp.CSharpTest.EditingAndDesigner""
          ]
        }
      ]
    }
  ]
}
";
    }
}
