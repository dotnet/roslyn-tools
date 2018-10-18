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
      [string] $specificBuild,
      [string] $updateAssemblyVersions,
      [string] $updateCoreXTLibraries,
      [string] $visualStudioBranchName)

. .\HelperFunctions.ps1

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
$updateAssemblyVersions = GetUpdateAssemblyVersions -visualStudioBranchName $visualStudioBranchName -updateAssemblyVersions $updateAssemblyVersions
$updateCoreXTLibraries = GetUpdateCoreXTLibraries -componentName $componentName -updateCoreXTLibraries $updateCoreXTLibraries

& .\RIT.exe  "/in=$componentName" "/bn=$componentBranchName" "/bq=$buildQueueName" "/vsbn=$visualStudioBranchName" "/ic=$insertCore" "/id=$insertDevDiv" "/qv=$queueValidation" "/ua=$updateAssemblyVersions" "/uc=$updateCoreXTLibraries" "/u=vslsnap@microsoft.com" "/ci=$clientId" "/cs=$clientSecret" "/ep=$enlistmentPath" $specificBuildFlag $toolsetFlag $dropPathFlag
