param([string] $enlistmentPath,
      [string] $clientId,
      [string] $clientSecret,
      [string] $requiredValueSentinel,
      [string] $defaultValueSentinel,
      [string] $componentName,
      [string] $buildQueueName,
      [string] $componentBranchName,
      [string] $existingPr,
      [string] $insertCore,
      [string] $insertDevDiv,
      [string] $visualStudioBranchName)

# check for unspecified required values

$requiredValues = @(
    @($componentName, "ComponentName"),
    @($componentBranchName, "ComponentBranchName"),
    @($visualStudioBranchName, "VisualStudioBranchName"),
    @($existingPr, "ExistingPR")
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

& .\RIT.exe  "/in=$componentName" "/bn=$componentBranchName" "/vsbn=$visualStudioBranchName" "/bq=$buildQueueName" "/ic=$insertCore" "/id=$insertDevDiv" /qv=true "/updateexistingpr=$existingPr" /u=vslsnap@microsoft.com "/ep=$enlistmentPath" "/ci=$clientId" "/cs=$clientSecret"
