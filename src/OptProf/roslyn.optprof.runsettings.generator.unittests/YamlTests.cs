using roslyn.optprof.runsettings.generator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace roslyn.optprof.unittests
{
    public class YamlTests
    {
        [Fact]
        public void BranchUnderJobs()
        {
            var content = @"
jobs:
- job:
  pool:
    name: VSEng-MicroBuildVS2017
    demands: 
    - msbuild
    - visualstudio
    - DotNetFramework

  timeoutInMinutes: 360

  variables:
    BuildPlatform: 'Any CPU'
    InsertTargetBranchFullName: 'lab/d16.0stg'
    InsertTargetBranchShortName: 'd16.0stg'
";

            var yaml = new YamlStream();
            yaml.Load(new StringReader(content));
            var result = Program.GetTargetBranch(yaml);
            Assert.Equal("lab/d16.0stg", result);
        }

        [Fact]
        public void BranchInRoot()
        {
            var content = @"
resources:
- repo: self
  clean: true
queue:
  name: VSEng-MicroBuildVS2017
  timeoutInMinutes: 360
  demands: 
  - msbuild
  - visualstudio
  - DotNetFramework

variables:
  BuildPlatform: 'Any CPU'
  InsertTargetBranchFullName: 'lab/d16.0stg'
  InsertTargetBranchShortName: 'd16.0stg'
";

            var yaml = new YamlStream();
            yaml.Load(new StringReader(content));
            var result = Program.GetTargetBranch(yaml);
            Assert.Equal("lab/d16.0stg", result);
        }
    }
}
