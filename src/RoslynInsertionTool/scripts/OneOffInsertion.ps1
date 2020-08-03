param([string] $clientId,
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
      [string] $specificBuild,
      [string] $updateAssemblyVersions,
      [string] $updateCoreXTLibraries,
      [string] $visualStudioBranchName,
      [string] $titlePrefix,
      [string] $writePullRequest,
      [int] $insertionCount,
      [string] $autoComplete,
      [string] $createDraftPR,
      [string] $cherryPick)

. $PSScriptRoot\HelperFunctions.ps1

EnsureRequiredValue -friendlyName "ComponentName" -value $componentName
EnsureRequiredValue -friendlyName "ComponentBranchName" -value $componentBranchName
EnsureRequiredValue -friendlyName "VisualStudioBranchName" -value $visualStudioBranchName

$buildQueueName = GetBuildQueueName -componentName $componentName -buildQueueName $buildQueueName
$dropPathFlag = GetDropPathFlag -componentName $componentName -dropPath $dropPath
$insertCore = GetInsertCore -componentName $componentName -insertCore $insertCore
$insertDevDiv = GetInsertDevDiv -insertDevDiv $insertDevDiv
$toolsetFlag = GetinsertToolsetFlag -componentName $componentName -insertToolset $insertToolset
$queueValidation = GetQueueValidation -visualStudioBranchName $visualStudioBranchName -queueValidation $queueValidation
$specificBuildFlag = GetSpecificBuildFlag -specificBuild $specificBuild
$updateAssemblyVersions = GetUpdateAssemblyVersions -componentName $componentName -visualStudioBranchName $visualStudioBranchName -updateAssemblyVersions $updateAssemblyVersions
$updateCoreXTLibraries = GetUpdateCoreXTLibraries -componentName $componentName -updateCoreXTLibraries $updateCoreXTLibraries
$autoComplete = GetAutoComplete -autoComplete $autoComplete
$createDraftPR = GetCreateDraftPR -createDraftPR $createDraftPR
$cherryPick = GetCherryPick -cherryPick $cherryPick

if ($insertionCount -lt 1) {
    $insertionCount = 1
}

for ($i = 0; $i -lt $insertionCount; $i++) {
    & $PSScriptRoot\RIT.exe  "/in=$componentName" "/bn=$componentBranchName" "/bq=$buildQueueName" "/vsbn=$visualStudioBranchName" "/ic=$insertCore" "/id=$insertDevDiv" "/qv=$queueValidation" "/ua=$updateAssemblyVersions" "/uc=$updateCoreXTLibraries" "/u=vslsnap@microsoft.com" "/ci=$clientId" "/cs=$clientSecret" "/tp=$titlePrefix" "/wpr=$writePullRequest" "/ac=$autoComplete" "/dpr=$createDraftPR" $specificBuildFlag $toolsetFlag $dropPathFlag $cherryPick
}
