param([string] $clientId,
      [string] $clientSecret,
      [string] $requiredValueSentinel,
      [string] $defaultValueSentinel,
      [string] $buildQueueName,
      [string] $componentAzdoUri,
      [string] $componentProjectName,
      [string] $componentBranchName,
      [string] $componentName,
      [string] $dropPath,
      [string] $existingPr,
      [string] $insertCore,
      [string] $insertDevDiv,
      [string] $insertToolset,
      [string] $queueValidation,
      [string] $specificBuild,
      [string] $updateAssemblyVersions,
      [string] $updateCoreXTLibraries,
      [string] $visualStudioBranchName,
      [string] $writePullRequest,
      [switch] $overwritePR,
      [string] $autoComplete,
      [string] $createDraftPR)

. $PSScriptRoot\HelperFunctions.ps1

EnsureRequiredValue -friendlyName "ComponentName" -value $componentName
EnsureRequiredValue -friendlyName "ComponentBranchName" -value $componentBranchName
EnsureRequiredValue -friendlyName "VisualStudioBranchName" -value $visualStudioBranchName
EnsureRequiredValue -friendlyName "ExistingPR" -value $existingPr

$componentAzdoUri = GetComponentAzdoUri -componentAzdoUri $componentAzdoUri
$componentProjectName = GetComponentProjectName -componentProjectName $componentProjectName
$componentUserName = GetComponentUserName -componentAzdoUri $componentAzdoUri
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

if($overwritePR)
{
  $overwritePrflag = "/overwritepr"
}

& $PSScriptRoot\RIT.exe "/in=$componentName" "/bn=$componentBranchName" "/bq=$buildQueueName" "/vsbn=$visualStudioBranchName" "/ic=$insertCore" "/id=$insertDevDiv" "/qv=$queueValidation" "/updateexistingpr=$existingPr" $overwritePrflag "/ua=$updateAssemblyVersions" "/uc=$updateCoreXTLibraries" "/u=vslsnap@microsoft.com" "/ci=$clientId" "/cs=$clientSecret" "/wpr=$writePullRequest" "/ac=$autoComplete" "/dpr=$createDraftPR" $specificBuildFlag $toolsetFlag $dropPathFlag $componentAzdoUri $componentProjectName $componentUserName
