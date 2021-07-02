param([string] $clientId,
      [string] $clientSecret,
      [string] $requiredValueSentinel,
      [string] $defaultValueSentinel,
      [string] $buildQueueName,
      [string] $componentAzdoUri,
      [string] $componentProjectName,
      [string] $componentBranchName,
      [string] $componentName,
      [string] $componentGitHubRepoName,
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
      [string] $titleSuffix,
      [string] $writePullRequest,
      [int] $insertionCount,
      [string] $autoComplete,
      [string] $createDraftPR,
      [string] $cherryPick,
      [string] $skipCoreXTPackages,
      [string] $reviewerGUID)

. $PSScriptRoot\HelperFunctions.ps1

EnsureRequiredValue -friendlyName "ComponentName" -value $componentName
EnsureRequiredValue -friendlyName "ComponentBranchName" -value $componentBranchName
EnsureRequiredValue -friendlyName "VisualStudioBranchName" -value $visualStudioBranchName

$componentAzdoUri = GetComponentAzdoUri -componentAzdoUri $componentAzdoUri
$componentProjectName = GetComponentProjectName -componentProjectName $componentProjectName
$componentGitHubRepoName = GetComponentGitHubRepoName -componentGitHubRepoName $componentGitHubRepoName
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
$cherryPick = GetCherryPick -cherryPick $cherryPick
$skipCoreXTPackages = GetSkipCoreXTPackages -skipCoreXTPackages $skipCoreXTPackages
$reviewerGUID = GetReviewerGUID -reviewerGUID $reviewerGUID

if ($insertionCount -lt 1) {
    $insertionCount = 1
}

for ($i = 0; $i -lt $insertionCount; $i++) {
    & $PSScriptRoot\RIT.exe  "/in=$componentName" "/bn=$componentBranchName" "/bq=$buildQueueName" "/vsbn=$visualStudioBranchName" "/ic=$insertCore" "/id=$insertDevDiv" "/qv=$queueValidation" "/ua=$updateAssemblyVersions" "/uc=$updateCoreXTLibraries" "/u=vslsnap@microsoft.com" "/ci=$clientId" "/cs=$clientSecret" "/tp=$titlePrefix" "/ts=$titleSuffix" "/wpr=$writePullRequest" "/ac=$autoComplete" "/dpr=$createDraftPR" "/reviewerGUID=$reviewerGUID" $specificBuildFlag $toolsetFlag $dropPathFlag $cherryPick $skipCoreXTPackages $componentAzdoUri $componentProjectName $componentGitHubRepoName $componentUserName
}
