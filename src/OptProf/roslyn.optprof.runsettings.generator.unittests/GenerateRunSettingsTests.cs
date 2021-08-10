// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
using System;
using System.IO;
using Xunit;

namespace roslyn.optprof.runsettings.generator.UnitTests
{
    public class GenerateRunSettingsTests
    {
        [Fact]
        public void GenerateRunSettings()
        {
            var temp = Path.GetTempPath();
            var dir = Path.Combine(temp, Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);

            var configPath = Path.Combine(dir, "OptProf.json");
            File.WriteAllText(configPath, SettingsReaderTests.products_only);

            var bootstrapperPath = Path.Combine(dir, "BootstrapperInfo.json");
            File.WriteAllText(bootstrapperPath, @"[{""BuildDrop"": ""https://vsdrop.corp.microsoft.com/file/v1/Products/42.42.42.42/42.42.42.42""}]");

            var runSettings = Program.GenerateRunSettings(configPath, vsDropName: "Products/abc", bootstrapperInfoPath: bootstrapperPath);

            Assert.Equal(string.Format(Constants.RunSettingsTemplate,
                @"vstsdrop:ProfilingInputs/abc",
                @"vstsdrop:Tests/42.42.42.42/42.42.42.42",
                @"<TestContainer FileName=""DDRIT.RPS.CSharp.dll"" />
<TestContainer FileName=""VSPE.dll"" />",
                @"FullyQualifiedName=DDRIT.RPS.CSharp.CSharpTest.EditingAndDesigner|FullyQualifiedName=VSPE.OptProfTests.vs_perf_designtime_ide_searchtest|FullyQualifiedName=VSPE.OptProfTests.vs_perf_designtime_editor_intellisense_globalcompletionlist_cs|FullyQualifiedName=VSPE.OptProfTests.vs_asl_cs_scenario|FullyQualifiedName=VSPE.OptProfTests.vs_ddbvtqa_vbwi|FullyQualifiedName=VSPE.OptProfTests.vs_asl_vb_scenario|FullyQualifiedName=VSPE.OptProfTests.vs_env_solution_createnewproject_vb_winformsapp|FullyQualifiedName=DDRIT.RPS.CSharp.CSharpTest.BuildAndDebugging"), runSettings);

            Directory.Delete(dir, recursive: true);
        }
    }
}
