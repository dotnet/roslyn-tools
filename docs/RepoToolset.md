# Description

RepoToolset is a set of msbuild props and targets files that provide build features used across repos, such as CI integration, packaging, VSIX and VS setup authoring, testing, and signing via Microbuild.

The goals are 
- to reduce the number of copies of the same or similar props, targets and script files across repos
- enable cross-platform build that relies on a standalone dotnet cli (downloaded during restore) as well as desktop msbuild based build
- no dependency on software installed on the machine when using _dotnet cli_
- be as close to the latest shipping dotnet SDK as possible, with minimal overrides and tweaks
- be modular and flexible, not all repos need all features; let the repo choose subset of features to import
- unify common operations and structure across repos
- unify VSTS build definitions used to produce official builds

The toolset has four kinds of features and helpers:
- Common conventions applicable to all repos using the toolset.
- Infrastructure required for Jenkins, MicroBuild, orchestrated build and build from source.
- Workarounds for bugs in shipping tools (dotnet SDK, VS SDK, msbuild, VS, NuGet client, etc.).
  Will be removed once the bugs are fixed in the product and the toolset moves to the new version of the tool.
- Abstraction of peculiarities of VSSDK and VS insertion process that are not compatible with dotnet SDK.

Repos currently using the toolset:
- http://github.com/dotnet/project-system
- http://github.com/dotnet/interactive-window
- http://github.com/dotnet/symreader
- http://github.com/dotnet/symreader-portable
- http://github.com/dotnet/symreader-converter
- http://github.com/dotnet/symstore
- http://github.com/dotnet/metadata-tools
- http://github.com/dotnet/roslyn-analyzers
- http://github.com/dotnet/roslyn-debug (private)
- http://github.com/dotnet/roslyn-sdk (private)
- http://github.com/dotnet/dotnet-cli-archiver
- [http://github.com/dotnet/sdk](http://github.com/dotnet/sdk/tree/dev/release/2.0)

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
      $(MSBuildProjectName)_$(TargetFramework)_$(TestArchitecture).(xml|html|log|error.log)
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
  
  <PropertyGroup>
    <!-- Feeds to use to restore dependent packages from. -->  
    <RestoreSources>
      $(RestoreSources);
      https://dotnet.myget.org/F/msbuild/api/v3/index.json;
      https://dotnet.myget.org/F/nuget-build/api/v3/index.json;
      https://dotnet.myget.org/F/roslyn-analyzers/api/v3/index.json;
    </RestoreSources>
  </PropertyGroup>
</Project>
```

The toolset defines a set of tools (or features) that each repo can opt into or opt out. Since different repos have different needs the set of tools that will be imported from the toolset can be controlled by ```UsingTool{tool-name}``` properties, where *tool-name* is e.g. ```Xliff```, ```SourceLink```, ```XUnit```, ```VSSDK```, ```IbcOptimization```, etc. These properties shall be set in the Versions.props file. 

The toolset also defines default versions for various tools and dependencies, such as MicroBuild, XUnit, VSSDK, etc. These defaults can be overridden in the Versions.props file.

See [DefaultVersions](https://github.com/dotnet/roslyn-tools/blob/master/src/RepoToolset/DefaultVersions.props]) for a list of *UsingTool* properties and default versions.

### Root build properties
Directory.Build.props in the repo root that imports Versions.props file and defines variables: 

```xml
<Import Project="build\NuGet.props"/>
<Import Project="build\Versions.props"/>

<PropertyGroup>
  <!-- Root of the repository -->
  <RepoRoot>$(MSBuildThisFileDirectory)</RepoRoot>

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

#### Conventions used by the toolset

- Unit test project file names shall end with ```.UnitTests``` or ```.Tests```, e.g. ```MyProject.UnitTests.csproj``` or ```MyProject.Tests.csproj```. 
- Integration test project file names shall end with ```.IntegrationTests```, e.g. ```MyProject.IntegrationTests.vbproj```.
- If ```source.extension.vsixmanifest``` is present next to the project file the project is by default considered to be a VSIX producing project.

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

Example of PowerShell script invoking the build:

```PowerShell
#
# Installs the RepoToolset
#
function InstallToolset {
  if (!(Test-Path $ToolsetBuildProj)) {
    & $DotNetExe msbuild Toolset.proj /t:restore /m /nologo /clp:None /warnaserror /v:quiet /p:NuGetPackageRoot=$NuGetPackageRoot /p:BaseIntermediateOutputPath=$ToolsetDir /p:ExcludeRestorePackageImports=true
  }
}

#
# Invokes the build driver.
#
function Build { 
  $ToolsetBuildProj = Join-Path $NuGetPackageRoot "RoslynTools.Microsoft.RepoToolset\$ToolsetVersion\tools\Build.proj"
 
  & $DotNetExe msbuild $ToolsetBuildProj /m /nologo /clp:Summary /warnaserror /v:$verbosity /p:Configuration=$configuration /p:SolutionPath=$solution /p:Restore=$restore /p:Build=$build /p:Rebuild=$rebuild /p:Test=$test /p:Sign=$sign /p:Pack=$pack /p:CIBuild=$ci /p:NuGetPackageRoot=$NuGetPackageRoot $properties
}
```

Example of common ```Toolset.proj```:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net462</TargetFramework>
    <RestoreSources>https://dotnet.myget.org/F/roslyn-tools/api/v3/index.json</RestoreSources>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RoslynTools.Microsoft.RepoToolset" Version="$(RoslynToolsMicrosoftRepoToolsetVersion)" />
  </ItemGroup>
</Project>
```

### Building VSIX packages

Building Visual Studio components is an opt-in feature of the RepoToolset. Property ```UsingToolVSSDK``` needs to be set to ```true``` in the ```Versions.props``` file (or in root Directory.Build.props).

Set ```VSSDKTargetPlatformRegRootSuffix``` property to specify the root suffix of the VS hive to deploy to.

If ```source.extension.vsixmanifest``` is present next to a project file the project is by default considered to be a VSIX producing project. 
A package reference to ```Microsoft.VSSDK.BuildTools``` is automatically added to such project. 
A project that needs ```Microsoft.VSSDK.BuildTools``` for generating pkgdef file needs to include the PackageReference explicitly.

RepoToolset include build target for generating VS Template VSIXes. Adding ```VSTemplate``` items to project will trigger the target.

```source.extension.vsixmanifest``` shall sepcify ```Experimental="true"``` attribute in ```Installation``` section. The experimental flag will be stripped from VSIXes inserted into Visual Studio.

VSIX packages are built to ```VSSetup``` directory.

### Visual Studio Insertion components

To include the output VSIX of a project in Visual Studio Insertion, set the ```VisualStudioInsertionComponent``` property.
Multiple VSIXes can specify the same component name, in which case their manifests will be merged into a single insertion unit.

The Visual Studio insertion manifests and VSIXes are generated during Pack task into ```VSSetup\Insertion``` directory, where they are picked by by MicroBuild VSTS publishing task during official builds.

RepoToolset also enables building VS Setup Components from .swr files (as opposed to components comprised of one or more VSIXes).

Use ```SwrProperty``` and ```SwrFile``` items to define a property that will be substituted in .swr files for given value and the set of .swr files, respectively.

For example,

```xml
<Project Sdk="Microsoft.NET.Sdk">
   <PropertyGroup>
    <TargetFramework>net46</TargetFramework>
    <VisualStudioInsertionComponent>Microsoft.VisualStudio.ProjectSystem.Managed</VisualStudioInsertionComponent>
  </PropertyGroup>
  <ItemGroup>
    <SwrProperty Include="Version=$(VsixVersion)" />
    <SwrProperty Include="VisualStudioXamlRulesDir=$(VisualStudioXamlRulesDir)" />
  </ItemGroup>
  <ItemGroup>
    <SwrFile Include="*.swr" />
  </ItemGroup>
</Project>
```

Where .swr file is:

```
use vs

package name=Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles
        version=$(Version)

vs.localizedResources
  vs.localizedResource language=en-us
                       title="Microsoft VisualStudio Managed Project System Common Files"
                       description="Microsoft VisualStudio ProjectSystem for C#/VB/F#(Managed) Projects"

folder "InstallDir:MSBuild\Microsoft\VisualStudio\Managed"
  file source="$(VisualStudioXamlRulesDir)Microsoft.CSharp.DesignTime.targets"
  file source="$(VisualStudioXamlRulesDir)Microsoft.VisualBasic.DesignTime.targets"
  file source="$(VisualStudioXamlRulesDir)Microsoft.FSharp.DesignTime.targets"
  file source="$(VisualStudioXamlRulesDir)Microsoft.Managed.DesignTime.targets"
```

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
