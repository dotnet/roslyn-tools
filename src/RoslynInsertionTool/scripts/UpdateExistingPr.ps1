param([string] $enlistmentPath,
      [string] $componentName,
      [string] $buildQueueName,
      [string] $componentBranchName,
      [string] $visualStudioBranchName,
      [string] $existingPr,
      [bool] $insertComponent,
      [bool] $insertDevDiv)

if ($insertComponent) {
    $icFlag = "true"
} else {
    $icFlag = "false"
}

if ($insertDevDiv) {
    $idFlag = "true"
} else {
    $idFlag = "false"
}

& .\RIT.exe  "/in=$componentName" "/bn=$componentBranchName" "/vsbn=$visualStudioBranchName" "/bq=$buildQueueName" /ic=$icFlag /id=$idFlag /qv=true /updateexistingpr=$existingPr /u=vslsnap@microsoft.com "/ep=$enlistmentPath"
