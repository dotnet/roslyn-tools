# Description

RepoToolset is a set of msbuild props and targets files that provide build features needed across repos, such as CI integration, packaging, testing, and signing.

The goals are 
- to reduce the amount of copies of same or similar props, targets and scripts across repos
- enable cross-platform build that relies on dotnet cli, which is expected to be downloaded during restore, as well as destkop msbuild based build
- no dependency on software installed on the machine when using dotnet cli
- be as close to the latest shipping dotnet SDK as possible, with minimal overrides and tweaks
- be modular and flexible, not all repos need all features; let the repo choose subset of features to import
- unify common operations and structure across repos

The toolset has following requirements on the repo layout.

### Single build output
All build outputs are located under a single directory called ```artifacts```. 
The RepoToolset defines the following output structure:

```
artifacts
  $(Configuration)
    bin
       $(MSBuildProjectName)
    obj
       $(MSBuildProjectName)
    packages
       $(MSBuildProjectName).$(PackageVersion).nupkg
    TestResults
       $(MSBuildProjectName)_$(TargetFramework)_$(TestArchitecture).xml
    VSSetup
       $(VisualStudioSetupComponent)
         *.vsix
         *.json
         *.vsmand
    tmp
  log
```

Having a common output directory structure makes it possible to unify microbuild definitions. 

### Sign Tool configuration
SignToolData.json file is present in the repo and describes how build outputs should be signed.

### A single file listing component versions
Versions.props file is present in the repo and defines versions of all dependencies used in the repository as well as version of the components produced by the repo build.

```xml
<Project>
  <PropertyGroup>
    <!-- Base three-part version used for all outputs of the repo (assemblies, packages, vsixes) -->
    <VersionBase>1.0.0</VersionBase>
    <!-- Package pre-release suffix not including build number -->
    <PreReleaseVersionLabel>rc2</PreReleaseVersionLabel>
  
    <!-- Toolset and dependency package versions -->
    <RoslynToolsMicrosoftRepoToolsetVersion>1.0.0-alpha5</RoslynToolsMicrosoftRepoToolsetVersion>
    <RoslynToolsMicrosoftXUnitLoggerVersion>1.0.0-alpha1</RoslynToolsMicrosoftXUnitLoggerVersion>
    <RoslynToolsMicrosoftSignToolVersion>0.3.1-beta</RoslynToolsMicrosoftSignToolVersion>
    <MicroBuildCoreVersion>0.2.0</MicroBuildCoreVersion>
    <MicroBuildPluginsSwixBuildVersion>1.0.101</MicroBuildPluginsSwixBuildVersion>
    <ToolsetCompilerPackageVersion>2.0.0-rc4</ToolsetCompilerPackageVersion>
    <XUnitVersion>2.2.0-beta4-build3444</XUnitVersion>

    <!-- Tool versions when using dotnet cli build driver -->
    <DotNetCliVersion>1.0.0-rc4-004777</DotNetCliVersion>

    <!-- Tool versions when using desktop msbuild driver -->
    <VSWhereVersion>1.0.47</VSWhereVersion>
    <XUnitRunnerConsoleVersion>2.2.0-beta4-build3444</XUnitRunnerConsoleVersion>
</PropertyGroup>
</Project>
```

### Root build properties
Directory.Build.props in the repo root that imports Versions.props file and defines variables: 

```xml
<!-- NuGet package root -->
<NuGetPackageRoot Condition="'$(NuGetPackageRoot)' == ''">$(NUGET_PACKAGES)</NuGetPackageRoot>
<NuGetPackageRoot Condition="'$(NuGetPackageRoot)' == '' AND '$(OS)' == 'Windows_NT'">$(UserProfile)\.nuget\packages\</NuGetPackageRoot>
<NuGetPackageRoot Condition="'$(NuGetPackageRoot)' == '' AND '$(OS)' != 'Windows_NT'">$([System.Environment]::GetFolderPath(SpecialFolder.Personal))\.nuget\packages\</NuGetPackageRoot>
<NuGetPackageRoot Condition="!HasTrailingSlash('$(NuGetPackageRoot)')">$(NuGetPackageRoot)\</NuGetPackageRoot>

<!-- Root of the repository -->
<RepoRoot>$([System.IO.Path]::GetFullPath('$(MSBuildThisFileDirectory)\'))</RepoRoot>

<!-- Full path to SignToolData.json -->
<SignToolDataPath>$(RepoRoot)build\SignToolData.json</SignToolDataPath>

<!-- Full path to Versions.props -->
<VersionsPropsPath>$(RepoRoot)build\Versions.props</VersionsPropsPath>

<!-- Repository and project URLs (used in nuget packages) -->
<RepositoryUrl>https://github.com/dotnet/symreader-converter</RepositoryUrl>
<PackageProjectUrl>$(RepositoryUrl)</PackageProjectUrl>

<!-- Not required, but useful: allows easy importing of props/targets files from RepoToolset -->
<RepoToolsetDir>$(NuGetPackageRoot)RoslynTools.Microsoft.RepoToolset\$(RoslynToolsMicrosoftRepoToolsetVersion)\tools\</RepoToolsetDir>
```

### Sources
Projects are located under ```src``` directory under root repo, in any subdirectory structure appropriate for the repo.

Projects shall be standard dotnet SDK based projects. No project level customization is required, that is a project created via ```dotnet new``` will work just fine without further modifications.

Test project file names shall end with "UnitTest", e.g. "MyProject.UnitTest.csproj".  

> Due to a [bug](https://github.com/Microsoft/msbuild/issues/1721) in msbuild targets each project file that targets multiple frameworks is currently required to import ```src\Directory.Build.targets``` file manually at its end:
> ```<Import Project="..\Directory.Build.targets" Condition="'$(TargetFramework)' == ''"/>```

Source directory ```src``` shall contain ```Directory.Build.props``` and ```Directory.Build.targets``` files like so:

#### Directory.Build.props

```xml
<Project>
  <!-- Import the repo root props -->
  <Import Project="..\Directory.Build.props"/>

  <!-- Import common project settings provided by RepoToolset -->
  <Import Project="$(RepoToolsetDir)Settings.props" />

  <!-- Any property customization common to all projects in the repo --> 
</Project>
```

#### Directory.Build.targets

```xml
<Project>
  <!-- Import common project targets provided by RepoToolset -->
  <Import Project="$(RepoToolsetDir)Imports.targets" />

  <!-- Any target customization common to all projects in the repo --> 
</Project>
```

### Default build scripts

The RepoToolset provides a build driver ```$(RepoToolsetDir)Build.proj```. 

It is recommended to add the following ```build.proj``` to the repo that invokes the driver. This example assumes ```build.proj``` located in the repo root along with ```MyMainSolution.sln``` that contains all projects of the repo.

```xml
<Project DefaultTargets="Build" TreatAsLocalProperty="SolutionPath">
  <!--
    Optional parameters:
      SolutionPath     Path to the solution to build
      Configuration    Build configuration: "Debug", "Release", etc.
      CIBuild        "true" if building on CI server
      Restore          "true" to restore toolset and solution
      Build            "true" to build solution
      Test             "true" to run tests
      Sign             "true" to sign built binaries
      Pack             "true" to build nuget and Visual Studio Setup packages
  -->
  <PropertyGroup>
    <SolutionPath Condition="'$(SolutionPath)' == ''">$(MSBuildThisFileDirectory)MyMainSolution.sln</SolutionPath>
  </PropertyGroup>

  <!-- Import the repo root props -->
  <Import Project="Directory.build.props"/>
  
  <Target Name="Build">
    <!-- Restore toolset packages, including RepoToolset --> 
    <MSBuild Projects="Toolset.proj" Targets="Restore" Condition="'$(Restore)' == 'true'"/>

    <!-- Invoke the RepoToolset build driver -->
    <MSBuild Projects="$(RepoToolsetDir)Build.proj" 
             Properties="SolutionPath=$(SolutionPath);Configuration=$(Configuration);CIBuild=$(CIBuild);Restore=$(Restore);Build=$(Build);Test=$(Test);Sign=$(Sign);Pack=$(Pack)" />
  </Target>
</Project>
```

Example of ```Toolset.proj``` that lists all toolset-level packages required by the repo:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net462</TargetFramework>
    <!-- Output dir for restore artifacts -->
    <BaseIntermediateOutputPath>$(MSBuildThisProjectDirectory)..\..\artifacts\Toolset</BaseIntermediateOutputPath>
  </PropertyGroup>
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
  <ItemGroup>
    <PackageReference Include="RoslynTools.Microsoft.RepoToolset" Version="$(RoslynToolsMicrosoftRepoToolsetVersion)" />
    <PackageReference Include="RoslynTools.Microsoft.XUnitLogger" Version="$(RoslynToolsMicrosoftXUnitLoggerVersion)" />
    <PackageReference Include="RoslynTools.Microsoft.SignTool" Version="$(RoslynToolsMicrosoftSignToolVersion)" />
    <PackageReference Include="MicroBuild.Core" Version="$(MicroBuildCoreVersion)" />
    <PackageReference Include="MicroBuild.Core.Sentinel" Version="1.0.0" />
    <PackageReference Include="MicroBuild.Plugins.SwixBuild" Version="$(MicroBuildPluginsSwixBuildVersion)" />
    <PackageReference Include="Microsoft.Net.Compilers" Version="$(ToolsetCompilerPackageVersion)" />
  </ItemGroup>
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
</Project>
```

### Building VSIX packages and Visual Studio Setup components

Set ```VisualStudioDeploymentRootSuffix``` property to specify the root suffix of the VS hive to deploy to.

Import .props and .targets files to each project building a VSIX:

```xml
  <Import Project="$(RepoToolsetDir)VisualStudio.props"/>
```

```xml
  <Import Project="$(RepoToolsetDir)VisualStudio.targets"/>
```

To include the VSIX in Visual Studio setup component that is inserted into Visual Studio by MicroBuild, set the following properties:

```xml
    <VsixPackageId>{Package ID as specified in .vsixmanifest file}</VsixPackageId>
    <VisualStudioSetupComponent>{VS setup component name to include the VSIX in}</VisualStudioSetupComponent>
```

The Visual Studio setup package will be built by Pack task.

### Main build script (OS Specific)

Used to acquire and restore dotnet cli (if not restored yet) and kick off build locally as well as from CI.

#### build.ps1 
Example of dotnet cli driven build:

https://github.com/dotnet/symreader-converter/blob/master/build/build.ps1.

Example of desktop msbuild driven build:
https://github.com/dotnet/interactive-window/blob/master/build/build.ps1.

#### CIBuild.cmd

```
@echo off
powershell -ExecutionPolicy ByPass .\build\Build.ps1 -restore -build -test -sign -pack -ci %*
exit /b %ErrorLevel%
```

#### MicroBuild

Use the following build command in MicroBuild definition:

```
CIBuild.cmd -configuration $(BuildConfiguration)
```

RepoToolset expects MicroBuild to set the following environment variables:

- BUILD_BUILDNUMBER=yyyymmdd.nn
- SignType="real"
