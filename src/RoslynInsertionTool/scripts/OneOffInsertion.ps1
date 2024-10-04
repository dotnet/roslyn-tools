param([string] $clientId,
      [string] $clientSecret,
      [string] $requiredValueSentinel,
      [string] $defaultValueSentinel,
      [string] $buildQueueName,
      [string] $componentAzdoUri,
      [string] $componentProjectName,
      [string] $componentBranchName,
      [string] $componentName,
      [string] $componentUserName,
      [string] $componentPassword,
      [string] $componentGitHubRepoName,
      [string] $dropPath,
      [string] $insertCore,
      [string] $insertDevDiv,
      [string] $insertToolset,
      [string] $queueValidation,
      [string] $queueSpeedometerValidation,
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
      [string] $reviewerGUID,
      [string] $userName,
      [string] $password,
      [string] $existingPR,
      [switch] $overwritePR,
      [switch] $createPlaceholderPR,
      [switch] $updateXamlRoslynVersion)

. $PSScriptRoot\HelperFunctions.ps1

EnsureRequiredValue -friendlyName "ComponentName" -value $componentName
EnsureRequiredValue -friendlyName "ComponentBranchName" -value $componentBranchName
EnsureRequiredValue -friendlyName "VisualStudioBranchName" -value $visualStudioBranchName

$componentAzdoUri = GetComponentAzdoUri -componentAzdoUri $componentAzdoUri
$componentProjectName = GetComponentProjectName -componentProjectName $componentProjectName
$componentGitHubRepoName = GetComponentGitHubRepoName -componentGitHubRepoName $componentGitHubRepoName
$componentUserName = GetComponentUserName -componentUserName $componentUserName
$componentPassword = GetComponentPassword -componentPassword $componentPassword
$buildQueueName = GetBuildQueueName -componentName $componentName -buildQueueName $buildQueueName
$dropPathFlag = GetDropPathFlag -componentName $componentName -dropPath $dropPath
$insertCore = GetInsertCore -componentName $componentName -insertCore $insertCore
$insertDevDiv = GetInsertDevDiv -insertDevDiv $insertDevDiv
$toolsetFlag = GetinsertToolsetFlag -componentName $componentName -insertToolset $insertToolset
$queueValidation = GetQueueValidation -visualStudioBranchName $visualStudioBranchName -queueValidation $queueValidation
$queueSpeedometerValidation = GetQueueSpeedometerValidation $queueSpeedometerValidation
$specificBuildFlag = GetSpecificBuildFlag -specificBuild $specificBuild
$updateAssemblyVersions = GetUpdateAssemblyVersions -componentName $componentName -visualStudioBranchName $visualStudioBranchName -updateAssemblyVersions $updateAssemblyVersions
$updateCoreXTLibraries = GetUpdateCoreXTLibraries -componentName $componentName -updateCoreXTLibraries $updateCoreXTLibraries
$autoComplete = GetAutoComplete -autoComplete $autoComplete
$createDraftPR = GetCreateDraftPR -createDraftPR $createDraftPR
$cherryPick = GetCherryPick -cherryPick $cherryPick
$skipCoreXTPackages = GetSkipCoreXTPackages -skipCoreXTPackages $skipCoreXTPackages
$reviewerGUID = GetReviewerGUID -reviewerGUID $reviewerGUID
$userName = GetUserName -userName $userName
$password = GetPassword -password $password
$clientId = GetClientId -clientId $clientId
$clientSecret = GetClientSecret -clientSecret $clientSecret
$existingPR = GetExistingPR -existingPR $existingPR

$overwritePRFlag = ""
if ($overwritePR.IsPresent -and $overwritePR)
{
    $overwritePRFlag = "/overwritepr"
}

$placeholderFlag = ""
if ($createPlaceholderPR.IsPresent -and $createPlaceholderPR)
{
    $placeholderFlag = "/createdummypr"
}

$updateXamlRoslynVersionFlag = ""
if ($updatexamlroslynversion.IsPresent -and $updatexamlroslynversion)
{
    $updateXamlRoslynVersionFlag = "/updatexamlroslynversion=true"
}

if ($insertionCount -lt 1) {
    $insertionCount = 1
}

for ($i = 0; $i -lt $insertionCount; $i++) {
    & $PSScriptRoot\RIT.exe  "/in=$componentName" "/bn=$componentBranchName" "/bq=$buildQueueName" "/vsbn=$visualStudioBranchName" "/ic=$insertCore" "/id=$insertDevDiv" "/qv=$queueValidation" "/ua=$updateAssemblyVersions" "/uc=$updateCoreXTLibraries" "/tp=$titlePrefix" "/ts=$titleSuffix" "/wpr=$writePullRequest" "/ac=$autoComplete" "/dpr=$createDraftPR" "/reviewerGUID=$reviewerGUID" $specificBuildFlag $toolsetFlag $dropPathFlag $cherryPick $skipCoreXTPackages $componentAzdoUri $componentProjectName $componentGitHubRepoName $componentUserName $componentPassword $userName $password $clientId $clientSecret $existingPR $overwritePRFlag $placeholderFlag $queueSpeedometerValidation $updateXamlRoslynVersionFlag
}
