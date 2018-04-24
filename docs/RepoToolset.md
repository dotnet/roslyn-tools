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

<a name="RepoList"></a>Repos currently using the toolset:
- https://github.com/dotnet/project-system
- https://github.com/dotnet/project-system-tools
- https://github.com/dotnet/interactive-window
- https://github.com/dotnet/symreader
- https://github.com/dotnet/symreader-portable
- https://github.com/dotnet/symreader-converter
- https://github.com/dotnet/symstore
- https://github.com/dotnet/metadata-tools
- https://github.com/dotnet/roslyn-analyzers
- https://github.com/dotnet/roslyn-debug (private)
- https://github.com/dotnet/roslyn-sdk (private)
- https://github.com/dotnet/roslyn-tools
- https://github.com/dotnet/dotnet-cli-archiver
- https://github.com/dotnet/clicommandlineparser
- https://github.com/dotnet/cli-migrate
- https://github.com/dotnet/sdk
- https://github.com/dotnet/xliff-tasks
- https://github.com/Microsoft/msbuild

The toolset has following requirements on the repo layout.

### Single build output
All build outputs are located under a single directory called ```artifacts```. 
The RepoToolset defines the following output structure:

```
artifacts
  $(Configuration)
    bin
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
  obj
    $(MSBuildProjectName)
      $(Configuration)
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

### SDK configuration (global.json, nuget.config)

`/global.json` file is present and specifies the version of the donet and `RoslynTools.RepoToolset` SDKs.

For example,

```json
{
  "sdk": {
    "version": "2.1.100-preview-007366"
  },
  "msbuild-sdks": {
    "RoslynTools.RepoToolset": "1.0.0-beta2-62615-01"
  }
}
```

`/nuget.config` file is present and specifies the MyGet feed to retrieve `RoslynTools.RepoToolset` SDK from like so:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <!-- Only specify feed for RepoToolset SDK (see https://github.com/Microsoft/msbuild/issues/2982) -->
  <packageSources>
    <add key="roslyn-tools" value="https://dotnet.myget.org/F/roslyn-tools/api/v3/index.json" />
  </packageSources>
</configuration>
```

> An improvement in SKD resolver is proposed to be able to specify the feed in `global.json` file to avoid the need for extra configuration in `nuget.config`. See https://github.com/Microsoft/msbuild/issues/2982.

### Sign Tool configuration
`/build/SignToolData.json` file is present in the repo and describes how build outputs should be signed.

### A single file listing component versions and used tools
`/build/Versions.props` file is present in the repo and defines versions of all dependencies used in the repository, the NuGet feeds they should be restored from and the version of the components produced by the repo build.

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
        
    <!-- Opt-out repo features -->
    <UsingToolXliff>false</UsingToolXliff>
  
    <!-- Versions of other dependencies -->   
    <MyPackageVersion>1.2.3-beta</MyPackageVersion>
  </PropertyGroup>
  
  <PropertyGroup>
    <!-- Feeds to use to restore dependent packages from. -->  
    <RestoreSources>
      $(RestoreSources);
      https://dotnet.myget.org/F/myfeed/api/v3/index.json
    </RestoreSources>
  </PropertyGroup>
</Project>
```

The toolset defines a set of tools (or features) that each repo can opt into or opt out. Since different repos have different needs the set of tools that will be imported from the toolset can be controlled by `UsingTool{tool-name}` properties, where *tool-name* is e.g. `Xliff`, `SourceLink`, `XUnit`, `VSSDK`, `IbcOptimization`, etc. These properties shall be set in the Versions.props file. 

The toolset also defines default versions for various tools and dependencies, such as MicroBuild, XUnit, VSSDK, etc. These defaults can be overridden in the Versions.props file.

See [DefaultVersions](https://github.com/dotnet/roslyn-tools/blob/master/src/RepoToolset/DefaultVersions.props]) for a list of *UsingTool* properties and default versions.

### Root build properties (optional)
`Directory.Build.props` in the repo root may specify the `RepositoryUrl` and public keys for `InternalsVisibleTo` project items.

```xml
<PropertyGroup>
  <!-- Repository and project URLs (used in nuget packages) -->
  <RepositoryUrl>https://github.com/dotnet/symreader-converter</RepositoryUrl>
  <PackageProjectUrl>$(RepositoryUrl)</PackageProjectUrl>
  
  <!-- Public keys used by InternalsVisibleTo project items -->
  <MoqPublicKey>00240000048000009400...</MoqPublicKey> 
</PropertyGroup>
```

### Source Projects
Projects are located under `src` directory under root repo, in any subdirectory structure appropriate for the repo. 

Projects shall use `RoslynTools.RepoToolset` SDK like so:

```xml
<Project Sdk="RoslynTools.RepoToolset">
    ...
</Project>
```

#### Project name conventions

- Unit test project file names shall end with `.UnitTests` or `.Tests`, e.g. `MyProject.UnitTests.csproj` or `MyProject.Tests.csproj`. 
- Integration test project file names shall end with `.IntegrationTests`, e.g. `MyProject.IntegrationTests.vbproj`.
- Performance test project file names shall end with `.PerformanceTests`, e.g. `MyProject.PerformaceTests.csproj`.
- If `source.extension.vsixmanifest` is present next to the project file the project is by default considered to be a VSIX producing project.

### Other Projects

It might be useful to create other top-level directories containing projects that are not standard C#/VB/F# projects. For example, projects that aggregate outputs of multiple projects into a single NuGet package or Willow component. These projects should also be included in the main solution so that the build driver includes them in build process, but their `Directory.Build.*` may be different from source projects. Hence the different root directory.

### Building VSIX packages (optional)

Building Visual Studio components is an opt-in feature of the RepoToolset. Property `UsingToolVSSDK` needs to be set to `true` in the `Versions.props` file.

Set `VSSDKTargetPlatformRegRootSuffix` property to specify the root suffix of the VS hive to deploy to.

If `source.extension.vsixmanifest` is present next to a project file the project is by default considered to be a VSIX producing project. 
A package reference to `Microsoft.VSSDK.BuildTools` is automatically added to such project. 
A project that needs `Microsoft.VSSDK.BuildTools` for generating pkgdef file needs to include the PackageReference explicitly.

RepoToolset include build target for generating VS Template VSIXes. Adding `VSTemplate` items to project will trigger the target.

`source.extension.vsixmanifest` shall sepcify `Experimental="true"` attribute in `Installation` section. The experimental flag will be stripped from VSIXes inserted into Visual Studio.

VSIX packages are built to `VSSetup` directory.

### Visual Studio Insertion components (optional)

To include the output VSIX of a project in Visual Studio Insertion, set the `VisualStudioInsertionComponent` property.
Multiple VSIXes can specify the same component name, in which case their manifests will be merged into a single insertion unit.

The Visual Studio insertion manifests and VSIXes are generated during Pack task into `VSSetup\Insertion` directory, where they are picked by by MicroBuild VSTS publishing task during official builds.

RepoToolset also enables building VS Setup Components from .swr files (as opposed to components comprised of one or more VSIXes).

Use `SwrProperty` and `SwrFile` items to define a property that will be substituted in .swr files for given value and the set of .swr files, respectively.

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
### Build script (OS Specific)

The RepoToolset provides a build driver `Build.proj`. 

The driver can be built from dotnet CLI or Desktop Framework msbuild. 

Example of dotnet cli driven build:
https://github.com/dotnet/symreader-converter/blob/master/build/build.ps1.

Example of desktop msbuild driven build:
https://github.com/dotnet/interactive-window/blob/master/build/build.ps1.

#### CIBuild.cmd

It is recommended to include `/build/CIBuild.cmd` and `/build/CIBuild.sh` in the repository and execute these scripts from Jenkins and MicroBuild to trigger CI build. The purpose of these scripts is to allow running CI build locally with the same parameters as on the CI server.

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

- `BUILD_BUILDNUMBER=yyyymmdd.nn`
- `SignType="real"`
