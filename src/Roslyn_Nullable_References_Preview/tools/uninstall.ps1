Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Exec-CommandCore([string]$command, [string]$commandArgs, [switch]$useConsole = $true) {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $command
    $startInfo.Arguments = $commandArgs

    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = Get-Location

    if (-not $useConsole) {
       $startInfo.RedirectStandardOutput = $true
       $startInfo.CreateNoWindow = $true
    }

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $process.Start() | Out-Null

    $finished = $false
    try {
        if (-not $useConsole) {
            # The OutputDataReceived event doesn't fire as events are sent by the
            # process in powershell.  Possibly due to subtlties of how Powershell
            # manages the thread pool that I'm not aware of.  Using blocking
            # reading here as an alternative which is fine since this blocks
            # on completion already.
            $out = $process.StandardOutput
            while (-not $out.EndOfStream) {
                $line = $out.ReadLine()
                Write-Output $line
            }
        }

        while (-not $process.WaitForExit(100)) {
            # Non-blocking loop done to allow ctr-c interrupts
        }

        $finished = $true
        if ($process.ExitCode -ne 0) {
            throw "Command failed to execute: $command $commandArgs"
        }
    }
    finally {
        # If we didn't finish then an error occured or the user hit ctrl-c.  Either
        # way kill the process
        if (-not $finished) {
            $process.Kill()
        }
    }
}

# Handy function for executing a windows command which needs to go through
# windows command line parsing.
#
# Use this when the command arguments are stored in a variable.  Particularly
# when the variable needs reparsing by the windows command line. Example:
#
#   $args = "/p:ManualBuild=true Test.proj"
#   Exec-Command $msbuild $args
#
function Exec-Command([string]$command, [string]$commandArgs) {
    Exec-CommandCore -command $command -commandArgs $commandargs -useConsole:$false
}

# Functions exactly like Exec-Command but lets the process re-use the current
# console. This means items like colored output will function correctly.
#
# In general this command should be used in place of
#   Exec-Command $msbuild $args | Out-Host
#
function Exec-Console([string]$command, [string]$commandArgs) {
    Exec-CommandCore -command $command -commandArgs $commandargs -useConsole:$true
}


# Get the directory and instance ID of the first Visual Studio version which
# meets our minimal requirements for the Roslyn repo.
function Get-VisualStudioDirAndId() {
    $vswhere = "tools\vswhere\vswhere.exe"
    $output = Exec-Command $vswhere "-prerelease -requires Microsoft.Component.MSBuild -format json" | Out-String
    $j = ConvertFrom-Json $output
    foreach ($obj in $j) {

        # Need to be using at least Visual Studio 15.2 in order to have the appropriate
        # set of SDK fixes. Parsing the installationName is the only place where this is
        # recorded in that form.
        $name = $obj.installationName
        if ($name -match "VisualStudio(Preview)?/([\d.]+)(\+|-).*") {
            $minVersion = New-Object System.Version "15.3.0"
            $version = New-Object System.Version $matches[2]
            if ($version -ge $minVersion) {
                Write-Output $obj.installationPath
                Write-Output $obj.instanceId
                return
            }
        }
        else {
            Write-Host "Unrecognized installationName format $name"
        }
    }

    throw "Could not find a suitable Visual Studio Version"
}

# Deploy our core VSIX libraries to Visual Studio via the Roslyn VSIX tool.  This is an alternative to
# deploying at build time.
function Uninstall-VsixViaTool() {
    $vsixExe = (Resolve-Path "tools\vsixexpinstaller\VsixExpInstaller.exe").Path
    $vsixExe = "`"$vsixExe`""
    $both = Get-VisualStudioDirAndId
    $vsDir = $both[0].Trim("\")
    $vsId = $both[1]
    $hive = ""
    Write-Host "Using VS Instance $vsId at `"$vsDir`""
    $baseArgs = "/rootSuffix:$hive /u /vsInstallDir:`"$vsDir`""
    $all = @(
        "vsix\ExpressionEvaluatorPackage.vsix"
        "vsix\Roslyn.VisualStudio.InteractiveComponents.vsix",
        "vsix\Roslyn.VisualStudio.Setup.Next.vsix",
        "vsix\Roslyn.VisualStudio.Setup.vsix",
        "vsix\Roslyn.Compilers.Extension.vsix")

    Write-Host "Un-Installing all Roslyn VSIXes"
    foreach ($e in $all) {
        $name = $e
        $filePath = "`"$((Resolve-Path $e).Path)`""
        $fullArg = "$baseArgs $filePath"
        Write-Host "`tUn-Installing $name"
        Exec-Console $vsixExe $fullArg
    }
}

Uninstall-VsixViaTool
