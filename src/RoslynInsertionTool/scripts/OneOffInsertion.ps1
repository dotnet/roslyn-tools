param([string] $enlistmentPath,
      [string] $clientId,
      [string] $clientSecret,
      [string] $requiredValueSentinel,
      [string] $defaultValueSentinel,
      [string] $buildQueueName,
      [string] $componentBranchName,
      [string] $componentName,
      [string] $dropPath,
      [string] $insertCore,
      [string] $insertDevDiv,
      [string] $insertToolset,
      [string] $queueValidation,
      [string] $updateAssemblyVersions,
      [string] $updateCoreXTLibraries,
      [string] $visualStudioBranchName)

# check for unspecified required values

$requiredValues = @(
    @($componentName, "ComponentName"),
    @($componentBranchName, "ComponentBranchName"),
    @($visualStudioBranchName, "VisualStudioBranchName")
)
foreach ($var in $requiredValues) {
    if (($var[0] -eq "") -or ($var[0] -eq $requiredValueSentinel)) {
        Write-Host "Missing required value: $var[1]"
    }
}

# process default values
function IsDefaultValue([string] $value) {
    return ($value -eq "") -or ($value -eq $defaultValueSentinel)
}

# $buildQueueName
if (IsDefaultValue $buildQueueName) {
    switch ($componentName) {
        "F#" { $buildQueueName = "FSharp-Signed"; break }
        "Live Unit Testing" { $buildQueueName = "TestImpact-Signed"; break }
        "Project System" { $buildQueueName = "DotNet-Project-System"; break }
        "Roslyn" { $buildQueueName = "Roslyn-Signed"; break }
        "VS Unit Testing" { $buildQueueName = "VSUnitTesting-Signed"; break }
        default {
            Write-Host "Unable to determine BuildQueueName from ComponentName"
            exit 1
        }
    }
}

# $dropPath
if (IsDefaultValue $dropPath) {
    switch ($componentName) {
        "F#" { $dropPath = "\\cpvsbuild\drops\FSharp"; break }
        "VS Unit Testing" { $dropPath = "server"; break }
        default { $dropPath = ""; break }
    }
}

# $insertCore
if (IsDefaultValue $insertCore) {
    if (($componentName -eq "Live Unit Testing") -or ($componentName -eq "Project System") -or ($componentName -eq "F#")) {
        $insertCore = "false"
    }
    else {
        $insertCore = "true"
    }
}

# $insertDevDiv
if (IsDefaultValue $insertDevDiv) {
    $insertDevDiv = "false"
}

# $insertToolset
if (IsDefaultValue $insertToolset) {
    if (($componentName -eq "Roslyn") -and ($visualStudioBranchName.StartsWith("lab/"))) {
        $insertToolset = "true"
    }
    else {
        $insertToolset = "false"
    }
}

# $queueValidation
if (IsDefaultValue $queueValidation) {
    if ($visualStudioBranchName -eq "lab/ml") {
        $queueValidation = "false"
    }
    else {
        $queueValidation = "true"
    }
}

# $updateAssemblyVersions
if (IsDefaultValue $updateAssemblyVersions) {
    if ($visualStudioBranchName.StartsWith("rel/")) {
        $updateAssemblyVersions = "false"
    }
    else {
        $updateAssemblyVersions = "true"
    }
}

# $updateCoreXTLibraries
if (IsDefaultValue $updateCoreXTLibraries) {
    if ($componentName -eq "Roslyn") {
        $updateCoreXTLibraries = "true"
    }
    else {
        $updateCoreXTLibraries = "false"
    }
}

$dropPathFlag = ""
if ($dropPath) {
    $dropPathFlag = "/dp=$dropPath"
}

$toolsetFlag = ""
if ($insertToolset -eq "true") {
    $toolsetFlag = "/t"
}

& .\RIT.exe  "/in=$componentName" "/bn=$componentBranchName" "/bq=$buildQueueName" "/vsbn=$visualStudioBranchName" "/ic=$insertCore" "/id=$insertDevDiv" "/qv=$queueValidation" "/ua=$updateAssemblyVersions" "/uc=$updateCoreXTLibraries" "/u=vslsnap@microsoft.com" "/ci=$clientId" "/cs=$clientSecret" "/ep=$enlistmentPath" $toolsetFlag $dropPathFlag
