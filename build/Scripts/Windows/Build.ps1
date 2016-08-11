[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $configuration = "Debug",
  [string] $deployHive = "Exp",
  [string] $msbuildVersion = "15.0",
  [string] $nugetVersion = "3.5.0-beta2",
  [string] $publishedPackageVersion = "0.1.0-beta",
  [switch] $help,
  [switch] $official,
  [switch] $realSign,
  [switch] $skipBuild,
  [switch] $skipDeploy,
  [switch] $skipPackage,
  [switch] $skipRestore,
  [switch] $skipTest,
  [switch] $skipTest32,
  [switch] $skipTest64,
  [string] $target = "Build",
  [string] $testFilter = "*.UnitTests.dll",
  [string] $xUnitVersion = "2.1.0",
  [string] $csiVersion = "1.3.2"
)

set-strictmode -version 2.0
$ErrorActionPreference = "Stop"

function Create-Directory([string[]] $path) {
  if (!(Test-Path -path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function Download-File([string] $address, [string] $fileName) {
  $webClient = New-Object -typeName "System.Net.WebClient"
  $webClient.DownloadFile($address, $fileName)
}

function Get-ProductVersion([string[]] $path) {
  if (!(Test-Path -path $path)) {
    return ""
  }

  $item = Get-Item -path $path
  return $item.VersionInfo.ProductVersion
}

function Get-RegistryValue([string] $keyName, [string] $valueName) {
  $registryKey = Get-ItemProperty -path $keyName
  return $registryKey.$valueName
}

function Locate-ArtifactsPath {
  $rootPath = Locate-RootPath
  $artifactsPath = Join-Path -path $rootPath -ChildPath "Artifacts\$configuration\"

  Create-Directory -path $artifactsPath
  return Resolve-Path -path $artifactsPath
}

function Locate-BinariesPath {
  $artifactsPath = Locate-ArtifactsPath
  $binariesPath = Join-Path -path $artifactsPath -ChildPath "bin\"

  Create-Directory -path $binariesPath
  return Resolve-Path -path $binariesPath
}

function Locate-BuiltSignToolPath {
  $binariesPath = Locate-BinariesPath
  $signToolPath = Join-Path -path $binariesPath -ChildPath "SignTool\"

  Create-Directory -path $signToolPath
  return Resolve-Path -path $signToolPath
}

function Locate-CsiPath {
  $packagesPath = Locate-PackagesPath
  $csiPath = Join-Path -path $packagesPath -ChildPath "Microsoft.Net.Compilers\$csiVersion\tools\csi.exe"

  if (!(Test-Path -path $csiPath)) {
    throw "The specified CSI version ($csiVersion) could not be located."
  }

  return Resolve-Path -Path $csiPath
}

function Locate-LogPath {
  $artifactsPath = Locate-ArtifactsPath
  $logPath = Join-Path -path $artifactsPath -ChildPath "log\"

  Create-Directory -path $logPath
  return Resolve-Path -path $logPath
}

function Locate-MSBuild {
  $msbuildPath = Locate-MSBuildPath
  $msbuild = Join-Path -path $msbuildPath -childPath "MSBuild.exe"

  if (!(Test-Path -path $msbuild)) {
    throw "The specified MSBuild version ($msbuildVersion) could not be located."
  }

  return Resolve-Path -path $msbuild
}

function Locate-MSBuildLogPath {
  $logPath = Locate-LogPath
  $msbuildLogPath = Join-Path -path $logPath -ChildPath "MSBuild\"

  Create-Directory -path $msbuildLogPath
  return Resolve-Path -path $msbuildLogPath
}

function Locate-MSBuildPath {
  $msbuildVersionPath = Locate-MSBuildVersionPath
  $msbuildPath = Get-RegistryValue -keyName $msbuildVersionPath -valueName "MSBuildToolsPath"
  return Resolve-Path -path $msbuildPath
}

function Locate-MSBuildVersionPath {
  $msbuildVersionPath = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\MSBuild\ToolsVersions\$msbuildVersion"

  if (!(Test-Path -path $msbuildVersionPath)) {
    $msbuildVersionPath = "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$msbuildVersion"

    if (!(Test-Path -path $msbuildVersionPath)) {
      throw "The specified MSBuild version ($msbuildVersion) could not be located."
    }
  }

  return Resolve-Path -path $msbuildVersionPath
}

function Locate-NuGet {
  $rootPath = Locate-RootPath
  $nuget = Join-Path -path $rootPath -childPath "NuGet.exe"

  if (Test-Path -path $nuget) {
    $currentVersion = Get-ProductVersion -path $nuget

    if ($currentVersion.StartsWith($nugetVersion)) {
      return Resolve-Path -path $nuget
    }

    Write-Host -object "The located version of NuGet ($currentVersion) is out of date. The specified version ($nugetVersion) will be downloaded instead."
    Remove-Item -path $nuget | Out-Null
  }

  Download-File -address "https://dist.nuget.org/win-x86-commandline/v$nugetVersion/NuGet.exe" -fileName $nuget

  if (!(Test-Path -path $nuget)) {
    throw "The specified NuGet version ($nugetVersion) could not be downloaded."
  }

  return Resolve-Path -path $nuget
}

function Locate-NuGetConfig {
  $rootPath = Locate-RootPath
  $nugetConfig = Join-Path -path $rootPath -childPath "nuget.config"
  return Resolve-Path -path $nugetConfig
}

function Locate-NuGetOutputPath {
  $signToolDir = Locate-BuiltSignToolPath
  $outDir = Join-Path -path $signToolDir -ChildPath "NuGet\PreRelease"
  Create-Directory -Path $outDir
  return Resolve-Path -path $outDir
}

function Locate-NuGetScriptPath {
  $rootPath = Locate-RootPath
  $scriptPath = Join-Path -path $rootPath -ChildPath "src\NuGet\BuildNuGets.csx"
  return Resolve-Path -Path $scriptPath
}

function Locate-PackagesPath {
  if ($env:NUGET_PACKAGES -eq $null) {
    $env:NUGET_PACKAGES =  Join-Path -path $env:UserProfile -childPath "Packages\"
  }

  $packagesPath = $env:NUGET_PACKAGES

  Create-Directory -path $packagesPath
  return Resolve-Path -path $packagesPath
}

function Locate-RootPath {
  $scriptPath = Locate-ScriptPath
  $rootPath = Join-Path -path $scriptPath -childPath "..\..\..\"
  return Resolve-Path -path $rootPath
}

function Locate-ScriptPath {
  $myInvocation = Get-Variable -name "MyInvocation" -scope "Script"
  $scriptPath = Split-Path -path $myInvocation.Value.MyCommand.Definition -parent
  return Resolve-Path -path $scriptPath
}

function Locate-SettingsFile {
  $rootPath = Locate-RootPath
  $settingsFile = Join-Path -path $rootPath -childPath "build\Targets\VSL.Settings.targets"

  if (!(Test-Path -path $settingsFile)) {
    throw "The settings file could not be located."
  }

  return Resolve-Path -path $settingsFile
}

function Locate-SignTool {
  $binariesPath = Locate-BinariesPath
  $signTool = Join-Path -path $binariesPath -childPath "SignTool\SignTool.exe"

  if (!(Test-Path -path $signTool)) {
    throw "The sign tool could not be located."
  }

  return Resolve-Path -path $signTool
}

function Locate-Solution {
  $rootPath = Locate-RootPath
  $solution = Join-Path -path $rootPath -childPath "RoslynTools.sln"
  return Resolve-Path -path $solution
}

function Locate-Toolset {
  $rootPath = Locate-RootPath
  $toolset = Join-Path -path $rootPath -childPath "build\Toolset\project.json"
  return Resolve-Path -path $toolset
}

function Locate-Toolset-Dev14 {
  $rootPath = Locate-RootPath
  $toolsetDev14 = Join-Path -path $rootPath -childPath "build\Toolset\dev14.project.json"
  return Resolve-Path -path $toolsetDev14
}

function Locate-xUnit-x86 {
  $xUnitPath = Locate-xUnitPath
  $xUnit = Join-Path -path $xUnitPath -childPath "xunit.console.x86.exe"

  if (!(Test-Path -path $xUnit)) {
    throw "The specified xUnit version ($xUnitVersion) could not be located."
  }

  return Resolve-Path -path $xUnit
}

function Locate-xUnit-x64 {
  $xUnitPath = Locate-xUnitPath
  $xUnit = Join-Path -path $xUnitPath -childPath "xunit.console.exe"

  if (!(Test-Path -path $xUnit)) {
    throw "The specified xUnit version ($xUnitVersion) could not be located."
  }

  return Resolve-Path -path $xUnit
}

function Locate-xUnitPath {
  $packagesPath = Locate-PackagesPath
  $xUnitPath = Join-Path -path $packagesPath -childPath "xunit.runner.console\$xUnitVersion\tools\"

  Create-Directory -path $xUnitPath
  return Resolve-Path -path $xUnitPath
}

function Locate-xUnitLogPath {
  $logPath = Locate-LogPath
  $xUnitLogPath = Join-Path -path $logPath -ChildPath "xUnit\"

  Create-Directory -path $xUnitLogPath
  return Resolve-Path -path $xUnitLogPath
}

function Locate-xUnitTestBinaries {
  $binariesPath = Locate-BinariesPath
  $testBinaries = Get-ChildItem -path $binariesPath -filter $testFilter -recurse -force

  $xUnitTestBinaries = @()

  foreach ($xUnitTestBinary in $testBinaries) {
    $xUnitTestBinaries += $xUnitTestBinary.FullName
  }

  return $xUnitTestBinaries
}

function Perform-Build {
  Write-Host -object ""

  if ($skipBuild) {
    Write-Host -object "Skipping build..."
    return
  }

  $msbuild = Locate-MSBuild
  $msbuildLogPath = Locate-MSBuildLogPath
  $solution = Locate-Solution

  $msbuildSummaryLog = Join-Path -path $msbuildLogPath -childPath "RoslynTools.log"
  $msbuildWarningLog = Join-Path -path $msbuildLogPath -childPath "RoslynTools.wrn"
  $msbuildFailureLog = Join-Path -path $msbuildLogPath -childPath "RoslynTools.err"

  $deploy = (-not $skipDeploy)

  Write-Host -object "Starting build..."
  & $msbuild /t:$target /p:Configuration=$configuration /p:DeployExtension=$deploy /p:DeployHive=$deployHive /p:OfficialBuild=$official /m /tv:$msbuildVersion /v:m /flp1:Summary`;Verbosity=diagnostic`;Encoding=UTF-8`;LogFile=$msbuildSummaryLog /flp2:WarningsOnly`;Verbosity=diagnostic`;Encoding=UTF-8`;LogFile=$msbuildWarningLog /flp3:ErrorsOnly`;Verbosity=diagnostic`;Encoding=UTF-8`;LogFile=$msbuildFailureLog /nr:false $solution

  if ($lastExitCode -ne 0) {
    throw "The build failed with an exit code of '$lastExitCode'."
  }

  Write-Host -object "The build completed successfully." -foregroundColor Green
}

function Perform-Package {
  Write-Host -object ""

  if ($skipPackage) {
    Write-Host -object "Skipping NuGet Packaging"
    return
  }

  $csi = Locate-CsiPath
  $nugetScriptPath = Locate-NuGetScriptPath
  $signToolPath = Locate-BuiltSignToolPath
  $nugetOutPath = Locate-NuGetOutputPath

  Write-Host "Starting package..."
  & $csi $nugetScriptPath $signToolPath $publishedPackageVersion $NuGetOutPath

  if ($lastExitCode -ne 0) {
    throw "The package task failed with an exit code of '$lastExitCode'."
  }

  Write-Host -Object "The package task completed successfully." -foregroundColor Green
}

function Perform-RealSign {
  Write-Host -object ""

  if ($skipBuild) {
    Write-Host -object "Skipping real signing..."
    return
  }

  $binariesPath = Locate-BinariesPath
  $msbuild = Locate-MSBuild
  $settingsFile = Locate-SettingsFile
  $signTool = Locate-SignTool

  if ($realSign) {
    Write-Host -object "Starting real signing..."
    & $signTool -binariesPath $binariesPath -msbuildPath $msbuild -settingsFile $settingsFile
  }
  else {
    Write-Host -object "Starting test signing..."
    & $signTool -test -binariesPath $binariesPath -msbuildPath $msbuild -settingsFile $settingsFile
  }

  if ($lastExitCode -ne 0) {
    throw "The real sign task failed with an exit code of '$lastExitCode'."
  }

  Write-Host -object "The real sign task completed successfully." -foregroundColor Green
}

function Perform-Restore {
  Write-Host -object ""

  if ($skipRestore) {
    Write-Host -object "Skipping restore..."
    return
  }

  $nuget = Locate-NuGet
  $nugetConfig = Locate-NuGetConfig
  $packagesPath = Locate-PackagesPath
  $toolset = Locate-Toolset
  $toolsetDev14 = Locate-Toolset-Dev14
  $solution = Locate-Solution

  Write-Host -object "Starting restore..."
  & $nuget restore -packagesDirectory $packagesPath -msbuildVersion $msbuildVersion -verbosity quiet -nonInteractive -configFile $nugetConfig $toolset
  & $nuget restore -packagesDirectory $packagesPath -msbuildVersion $msbuildVersion -verbosity quiet -nonInteractive -configFile $nugetConfig $toolsetDev14
  & $nuget restore -packagesDirectory $packagesPath -msbuildVersion $msbuildVersion -verbosity quiet -nonInteractive -configFile $nugetConfig $solution

  if ($lastExitCode -ne 0) {
    throw "The restore failed with an exit code of '$lastExitCode'."
  }

  Write-Host -object "The restore completed successfully." -foregroundColor Green
}

function Perform-Test-x86 {
  Write-Host -object ""

  if ($skipTest -or $skipTest32) {
    Write-Host -object "Skipping test x86..."
    return
  }

  $xUnit = Locate-xUnit-x86
  $xUnitLogPath = Locate-xUnitLogPath
  $xUnitTestBinaries = @(Locate-xUnitTestBinaries)

  $xUnitResultLog = Join-Path -path $xUnitLogPath -childPath "x86.xml"

  Write-Host -object "Starting test x86..."
  & $xUnit @xUnitTestBinaries -xml $xUnitResultLog

  if ($lastExitCode -ne 0) {
    throw "The test failed with an exit code of '$lastExitCode'."
  }

  Write-Host -object "The test completed successfully." -foregroundColor Green
}

function Perform-Test-x64 {
  Write-Host -object ""

  if ($skipTest -or $skipTest64) {
    Write-Host -object "Skipping test x64..."
    return
  }

  $xUnit = Locate-xUnit-x64
  $xUnitLogPath = Locate-xUnitLogPath
  $xUnitTestBinaries = @(Locate-xUnitTestBinaries)

  $xUnitResultLog = Join-Path -path $xUnitLogPath -childPath "x64.xml"

  Write-Host -object "Starting test x64..."
  & $xUnit @xUnitTestBinaries -xml $xUnitResultLog

  if ($lastExitCode -ne 0) {
    throw "The test failed with an exit code of '$lastExitCode'."
  }

  Write-Host -object "The test completed successfully." -foregroundColor Green
}

function Print-Help {
  if (-not $help) {
    return
  }

  Write-Host -object "Build Script"
  Write-Host -object "    Help                       - [Switch] - Prints this help message."
  Write-Host -object ""
  Write-Host -object "    Configuration              - [String] - Specifies the build configuration. Defaults to 'Debug'."
  Write-Host -object "    DeployHive                 - [String] - Specifies the VSIX deployment hive. Defaults to 'Exp'."
  Write-Host -object "    MSBuildVersion             - [String] - Specifies the MSBuild version. Defaults to '15.0'."
  Write-Host -object "    NuGetVersion               - [String] - Specifies the NuGet version. Defaults to '3.5.0-beta2'."
  Write-Host -object "    publishedPackageVersion    - [String] - Specifies the version of the published NuGet package. Defaults to '0.1.0-beta'."
  Write-Host -object "    Target                     - [String] - Specifies the build target. Defaults to 'Build'."
  Write-Host -object "    TestFilter                 - [String] - Specifies the test filter. Defaults to '*.UnitTests.dll'."
  Write-Host -object "    xUnitVersion               - [String] - Specifies the xUnit version. Defaults to '2.1.0'."
  Write-Host -object "    csiVersion                 - [String] - Specifies the CSI version. Defaults to '1.3.2'."
  Write-Host -object ""
  Write-Host -object "    Official                   - [Switch] - Indicates this is an official build which changes the semantic version."
  Write-Host -object "    RealSign                   - [Switch] - Indicates this build needs real signing performed."
  Write-Host -object "    SkipBuild                  - [Switch] - Indicates the build step should be skipped."
  Write-Host -object "    SkipDeploy                 - [Switch] - Indicates the VSIX deployment step should be skipped."
  Write-Host -object "    SkipPackage                - [Switch] - Indicates the NuGet Package step should be skipped."
  Write-Host -object "    SkipRestore                - [Switch] - Indicates the restore step should be skipped."
  Write-Host -object "    SkipTest                   - [Switch] - Indicates the test step should be skipped."
  Write-Host -object "    SkipTest32                 - [Switch] - Indicates the 32-bit Unit Tests should be skipped."
  Write-Host -object "    SkipTest64                 - [Switch] - Indicates the 64-bit Unit Tests should be skipped."
  Write-Host -object "    Integration                - [Switch] - Indicates the Integration Tests should be run."

  Exit 0
}

try {
  Print-Help
  Perform-Restore
  Perform-Build
  Perform-Package
  # Perform-RealSign
  # Perform-Test-x86
} catch [exception] {
    write-host $_.Exception
    exit -1
}
