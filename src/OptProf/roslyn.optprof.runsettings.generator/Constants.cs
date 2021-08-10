// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.
namespace roslyn.optprof.runsettings.generator
{
    internal static class Constants
    {
        public const string RunSettingsTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <RunConfiguration>
    <ResultsDirectory>C:\Test\Results</ResultsDirectory>
    <TargetPlatform>X86</TargetPlatform>
    <MaxCpuCount>1</MaxCpuCount>
    <BatchSize>10</BatchSize>
    <TestSessionTimeout>21600000</TestSessionTimeout>
    <DesignMode>False</DesignMode>
    <InIsolation>False</InIsolation>
    <CollectSourceInformation>False</CollectSourceInformation>
    <DisableAppDomain>False</DisableAppDomain>
    <DisableParallelization>False</DisableParallelization>
    <TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion>
    <ExecutionThreadApartmentState>STA</ExecutionThreadApartmentState>
    <TestAdaptersPaths>%SystemDrive%\Test</TestAdaptersPaths>
    <TreatTestAdapterErrorsAsWarnings>False</TreatTestAdapterErrorsAsWarnings>
  </RunConfiguration>
  <SessionConfiguration>
    <TestStores>
      <TestStore Uri=""{0}"" />
      <TestStore Uri=""{1}"" />
    </TestStores>
    <TestContainers>
      {2}
    </TestContainers>
    <TestCaseFilter>{3}</TestCaseFilter>
  </SessionConfiguration>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector uri=""datacollector://microsoft/DevDiv/TestExtensions/ProcDumpCollector/v1"" friendlyName=""ProcDump Collector"" enabled=""True"">
        <Configuration>
          <RootDumpDirectory>C:\Test\Dumps</RootDumpDirectory>
          <Deployment PackageName = ""Microsoft.DevDiv.TestExtensions.ProcDumpCollector"" />
        </Configuration>
      </DataCollector>
      <DataCollector uri=""datacollector://microsoft/DevDiv/TestExtensions/LingeringProcessCollector/v1"" friendlyName=""Lingering Process Collector"" enabled=""True"">
        <Configuration>
          <KillLingeringProcesses>true</KillLingeringProcesses>
          <ShutdownCommands>
            <ShutdownCommand Process=""VBCSCompiler"" Command=""%ProcessPath%"" Arguments=""-shutdown"" Timeout=""60000"" />
          </ShutdownCommands>
          <LoggingBehavior>Warning</LoggingBehavior>
          <WhiteList>
            <ProcessName>conhost</ProcessName>
            <ProcessName>MSBuild</ProcessName>
            <ProcessName>MSpdbsrv</ProcessName>
            <ProcessName>node</ProcessName>
            <ProcessName>PerfWatson2</ProcessName>
            <ProcessName>ServiceHub.DataWarehouseHost</ProcessName>
            <ProcessName>ServiceHub.Host.CLR.x86</ProcessName>
            <ProcessName>ServiceHub.Host.Node.x86</ProcessName>
            <ProcessName>ServiceHub.IdentityHost</ProcessName>
            <ProcessName>ServiceHub.RoslynCodeAnalysisService32</ProcessName>
            <ProcessName>ServiceHub.SettingsHost</ProcessName>
            <ProcessName>ServiceHub.VSDetouredHost</ProcessName>
            <ProcessName>VBCSCompiler</ProcessName>
            <ProcessName>VCTip</ProcessName>
            <ProcessName>VSTestVideoRecorder</ProcessName>
            <ProcessName>wermgr</ProcessName>
          </WhiteList>
          <Deployment PackageName = ""Microsoft.DevDiv.TestExtensions.LingeringProcessCollector"" />
        </Configuration>
      </DataCollector>
      <DataCollector uri=""datacollector://microsoft/DevDiv/TestExtensions/OptimizationDataCollector/v2"" friendlyName=""Optimization Data Collector"" enabled=""True"">
        <Configuration>
          <ProfilesDirectory>%SystemDrive%\Profiles</ProfilesDirectory>
          <Deployment PackageName = ""Microsoft.DevDiv.TestExtensions.OptimizationDataCollector"" />
        </Configuration>
      </DataCollector>
      <DataCollector uri=""datacollector://microsoft/DevDiv/VideoRecorder/2.0"" friendlyName=""Screen and Voice Recorder"" enabled=""True"">
        <Configuration>
          <Deployment PackageName = ""Microsoft.DevDiv.Validation.MediaRecorder"" />
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  <LoggerRunSettings>
    <Loggers />
  </LoggerRunSettings>
  <TestRunParameters />
</RunSettings> ";
    }
}
