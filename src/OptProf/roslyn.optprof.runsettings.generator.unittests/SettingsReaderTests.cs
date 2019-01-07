using System.IO;
using roslyn.optprof.runsettings.generator;
using Xunit;

namespace roslyn.optprof.runsettings.generator.UnitTests
{
    public class SettingsReaderTests
    {
        [Theory]
        [InlineData(products_only, products_only_expectedContainerString, products_only_expectedTestCaseFilterString)]
        [InlineData(assemblies_only, assemblies_only_expectedContainerString, assemblies_only_expectedTestCaseFilterString)]
        [InlineData(products_and_assemblies, products_and_assemblies_expectedContainerString, products_and_assemblies_expectedTestCaseFilterString)]
        public void TestProductsOnly(string configJson, string expectedContainerString, string expectedTestCaseFilterString)
        {
            var (actualContainerString, actualTestCaseFilterString) = Program.GetContainerString(configJson, "config.json");
            Assert.Equal(expectedContainerString, actualContainerString);
            Assert.Equal(expectedTestCaseFilterString, actualTestCaseFilterString);
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

        public const string assemblies_only_expectedContainerString = "<TestContainer FileName=\"DDRIT.RPS.CSharp.dll\" />";
        public const string assemblies_only_expectedTestCaseFilterString = "FullyQualifiedName=DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging";
        public const string assemblies_only = @"
{
  ""assemblies"" : [
    {
      ""assembly"": ""System.Collections.Immutable.dll"",
      ""instrumentationArguments"": [
        {
          ""relativeInstallationFolder"": ""Common7/IDE/PrivateAssemblies"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/15.0/Bin/Roslyn"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/Current/Bin"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/Current/Bin/amd64"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""Common7/IDE/Extensions/TestPlatform"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        }
      ],
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
      ""assembly"": ""System.Reflection.Metadata.dll"",
      ""instrumentationArguments"": [
        {
          ""relativeInstallationFolder"": ""Common7/IDE/PrivateAssemblies"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/15.0/Bin/Roslyn"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""Common7/IDE/Extensions/TestPlatform"",
          ""instrumentationExecutable"": ""Common7/IDE/vsn.exe""
        }
      ],
      ""tests"": [
        {
          ""container"": ""DDRIT.RPS.CSharp"",
          ""testCases"": [
            ""DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging""
          ]
        }
      ]
    }
  ]
}
";

        public const string products_and_assemblies_expectedContainerString = "<TestContainer FileName=\"DDRIT.RPS.CSharp.dll\" />\r\n<TestContainer FileName=\"VSPE.dll\" />";
        public const string products_and_assemblies_expectedTestCaseFilterString = "FullyQualifiedName=DDRIT.RPS.CSharp.CSharpTest.EditingAndDesigner|FullyQualifiedName=VSPE.OptProfTests.vs_perf_designtime_ide_searchtest|FullyQualifiedName=VSPE.OptProfTests.vs_perf_designtime_editor_intellisense_globalcompletionlist_cs|FullyQualifiedName=VSPE.OptProfTests.vs_asl_cs_scenario|FullyQualifiedName=VSPE.OptProfTests.vs_ddbvtqa_vbwi|FullyQualifiedName=VSPE.OptProfTests.vs_asl_vb_scenario|FullyQualifiedName=VSPE.OptProfTests.vs_env_solution_createnewproject_vb_winformsapp|FullyQualifiedName=DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging";
        public const string products_and_assemblies = @"
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
  ],
  ""assemblies"" : [
    {
      ""assembly"": ""System.Collections.Immutable.dll"",
      ""instrumentationArguments"": [
        {
          ""relativeInstallationFolder"": ""Common7/IDE/PrivateAssemblies"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/15.0/Bin/Roslyn"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/Current/Bin"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/Current/Bin/amd64"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""Common7/IDE/Extensions/TestPlatform"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        }
      ],
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
      ""assembly"": ""System.Reflection.Metadata.dll"",
      ""instrumentationArguments"": [
        {
          ""relativeInstallationFolder"": ""Common7/IDE/PrivateAssemblies"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""MSBuild/15.0/Bin/Roslyn"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        },
        {
          ""relativeInstallationFolder"": ""Common7/IDE/Extensions/TestPlatform"",
          ""instrumentationExecutable"" : ""Common7/IDE/vsn.exe""
        }
      ],
      ""tests"": [
        {
          ""container"": ""DDRIT.RPS.CSharp"",
          ""testCases"": [
            ""DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging""
          ]
        }
      ]
    }
  ]
}
";
    }
}
