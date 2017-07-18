# Description

RepoToolset is a set of msbuild props and targets files that provide build features used across repos, such as CI integration, packaging, VSIX and VS setup authoring, testing, and signing via Microbuild.

The goals are 
- to reduce the number of copies of the same or similar props, targets and script files across repos
- enable cross-platform build that relies on a standalone dotnet cli (downloaded during restore) as well as desktop msbuild based build
- no dependency on software installed on the machine when using dotnet cli
- be as close to the latest shipping dotnet SDK as possible, with minimal overrides and tweaks
- be modular and flexible, not all repos need all features; let the repo choose subset of features to import
- unify common operations and structure across repos
- abstract away peculiarities of VSSDK and MicroBuild that are not compatible with dotnet SDK

Repos currently using the toolset:
- http://github.com/dotnet/interactive-window
- http://github.com/dotnet/symreader
- http://github.com/dotnet/symreader-portable
- http://github.com/dotnet/symreader-converter
- http://github.com/dotnet/symstore
- http://github.com/dotnet/metadata-tools
- http://github.com/dotnet/roslyn-analyzers
- http://github.com/dotnet/roslyn-debug

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
      $(MSBuildProjectName)_$(TargetFramework)_$(TestArchitecture).log
    VSSetup
      Insertion
        $(VsixPackageId).json
        $(VsixPackageId).vsmand
        $(VsixContainerName).vsix
        $(VisualStudioInsertionComponent).vsman
         
      $(VsixPackageId).json
      $(VsixContainerName).vsix
    VSSetup.obj
      $(VisualStudioInsertionComponent)
    log
      Build.binlog
    tmp
  toolset
```

Having a common output directory structure makes it possible to unify MicroBuild definitions. 

| directory         | description |
|-------------------|-------------|
| bin               | Build output of each project. |
| obj               | Intermediate directory for each project. |
| packages          | NuGet packages produced by all projects in the repo. |
| VSSetup           | Packages produced by VSIX projects in the repo. These packages are experimental and can be used for dogfooding.
| VSSetup/Insertion | Willow manifests and VSIXes to be inserted into VS.
| VSSetup.obj       | Temp files produced by VSIX build. |
| log               | Build binary log and other logs. |
| tmp               | Temp files generated during build. |
| toolset           | Files generated during toolset restore. |

### Sign Tool configuration
SignToolData.json file is present in the repo and describes how build outputs should be signed.

### A single file listing component versions and used tools
Versions.props file is present in the repo and defines versions of all dependencies used in the repository as well as version of the components produced by the repo build.

```xml
<Project>
  <PropertyGroup>
    <!-- Base three-part version used for all outputs of the repo (assemblies, packages, vsixes) -->
    <VersionBase>1.0.0</VersionBase>
    <!-- Package pre-release suffix not including build number -->
    <PreReleaseVersionLabel>rc2</PreReleaseVersionLabel>
  
    <!-- Opt-in repo features -->
    <UsingToolVSSDK>true</UsingToolVSSDK>
    <UsingToolIbcOptimization>true</UsingToolIbcOptimization>
  
    <!-- RepoToolset package version -->
    <RoslynToolsMicrosoftRepoToolsetVersion>1.0.0-alpha27</RoslynToolsMicrosoftRepoToolsetVersion>
    
    <!-- Tool versions when using dotnet cli build driver -->
    <DotNetCliVersion>1.0.0-rc4-004777</DotNetCliVersion>

    <!-- Tool versions when using desktop msbuild driver -->
    <VSWhereVersion>1.0.47</VSWhereVersion>
    
    <!-- Versions of other dependencies -->
    <NewtonsoftJsonVersion>9.0.1</NewtonsoftJsonVersion>
    <MoqVersion>4.2.1402.2112</MoqVersion>
</PropertyGroup>
</Project>
```

The toolset defines a set of tools (or features) that each repo can opt into or opt out. Since different repos have different needs the set of tools that will be imported from the toolset can be controlled by ```UsignTool{tool-name}``` properties, where *tool-name* is e.g. ```Xliff```, ```SourceLink```, ```XUnit```, ```VSSDK```, ```IbcOptimization```, etc. These properties shall be set in the Versions.props file. 

The toolset also defines default versions for various tools and dependencies, such as MicroBuild, XUnit, VSSDK, etc. These defaukts can be overrridden in the Versions.props file.

See [DefaultVersions](https://github.com/dotnet/roslyn-tools/blob/master/src/RepoToolset/DefaultVersions.props]) for a list of *UsingTool* properties and default versions.

### Root build properties
Directory.Build.props in the repo root that imports Versions.props file and defines variables: 

```xml
<Import Project="build\NuGet.props"/>
<Import Project="build\Versions.props"/>

<PropertyGroup>
  <!-- Root of the repository -->
  <RepoRoot>$([System.IO.Path]::GetFullPath('$(MSBuildThisFileDirectory)\'))</RepoRoot>

  <!-- Full path to SignToolData.json -->
  <SignToolDataPath>$(RepoRoot)build\SignToolData.json</SignToolDataPath>

  <!-- Full path to Versions.props -->
  <VersionsPropsPath>$(RepoRoot)build\Versions.props</VersionsPropsPath>

  <!-- Not required, but useful: allows easy importing of props/targets files from RepoToolset -->
  <RepoToolsetDir>$(NuGetPackageRoot)RoslynTools.Microsoft.RepoToolset\$(RoslynToolsMicrosoftRepoToolsetVersion)\tools\</RepoToolsetDir>

  <!-- Repository and project URLs (used in nuget packages) -->
  <RepositoryUrl>https://github.com/dotnet/symreader-converter</RepositoryUrl>
  <PackageProjectUrl>$(RepositoryUrl)</PackageProjectUrl>
  
  <!-- Public keys used by InternalsVisibleTo project items -->
  <MoqPublicKey>00240000048000009400...</MoqPublicKey> 
</PropertyGroup>
```

### Source Projects
Projects are located under ```src``` directory under root repo, in any subdirectory structure appropriate for the repo. 

Projects shall be standard dotnet SDK based projects. No project level customization is required, that is a project created via ```dotnet new``` will work just fine without further modifications.

#### Conventions

- Unit test project file names shall end with ".UnitTest", e.g. "MyProject.UnitTest.csproj".  
- Integration test project file names shall end with ".IntegrationTest", e.g. "MyProject.IntegrationTest.vbproj".

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

### Other Projects

It might be useful to create other top-level directories containing projects for projects that are not standard C#/VB/F# projects. For example, projects that aggregate outputs of multiple projects into a single NuGet package or Willow component. These projects should also be included in the main solution so that the build driver includes them in build process, but their ```Directory.Build.*``` settings are gonna be different from source projects. Hence the separate root directory.

### Default build scripts

The RepoToolset provides a build driver ```$(RepoToolsetDir)Build.proj```. 

It is recommended to add the following ```build.proj``` to the repo that invokes the driver. This example assumes ```build.proj``` located in the repo root along with ```MyMainSolution.sln``` that contains all projects of the repo.

```xml
<Project DefaultTargets="Build" TreatAsLocalProperty="SolutionPath">
  <!--
    Optional parameters:
      SolutionPath     Path to the solution to build
      Configuration    Build configuration: "Debug", "Release", etc.
      CIBuild          "true" if building on CI server
      Restore          "true" to restore toolset and solution
      Build            "true" to build solution
      Rebuild          "true" to rebuild solution
      Deploy           "true" to deploy assets (e.g. VSIXes) built in this repo
      DeployDeps       "true" to deploy assets (e.g. VSIXes) this repo depeends on.
      Test             "true" to run tests
      IntegrationTest  "true" to run integration tests
      Sign             "true" to sign built binaries
      Pack             "true" to build NuGet packages
      Properties        List of properties to pass to each build phase ("Name=Value;Name=Value;...")
  -->
  <PropertyGroup>
    <SolutionPath Condition="'$(SolutionPath)' == ''">$(MSBuildThisFileDirectory)..\MyMainSolution.sln</SolutionPath>
  </PropertyGroup>

  <!-- Import the repo root props -->
  <Import Project="Directory.build.props"/>
  
  
  <Target Name="Build">
    <!-- Restore RepoToolset package and potential non-nuget dependencies (such as VSIX components) --> 
    <MSBuild Projects="Toolset.proj"
             Targets="Restore"
             Properties="BaseIntermediateOutputPath=$(MSBuildThisFileDirectory)..\artifacts\toolset\;ExcludeRestorePackageImports=true;DeployDeps=$(DeployDeps)" 
             Condition="'$(Restore)' == 'true'"/>

    <!-- Invoke the RepoToolset build driver -->
        <MSBuild Projects="$(RepoToolsetDir)Build.proj"
                 Properties="SolutionPath=$(SolutionPath);Configuration=$(Configuration);CIBuild=$(CIBuild);Restore=$(Restore);Build=$(Build);Rebuild=$(Rebuild);Deploy=$(Deploy);Test=$(Test);IntegrationTest=$(IntegrationTest);Sign=$(Sign);Pack=$(Pack);Properties=$(Properties)" />
  </Target>
</Project>
```

Example of default ```Toolset.proj```:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net462</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RoslynTools.Microsoft.RepoToolset" Version="$(RoslynToolsMicrosoftRepoToolsetVersion)" />
  </ItemGroup>
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
