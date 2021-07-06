param([string] $clientId,
      [string] $clientSecret,
      [string] $componentName,
      [string] $visualStudioBranchName,
      [string] $titlePrefix,
      [string] $writePullRequest,
      [string] $reviewerGUID)

. $PSScriptRoot\HelperFunctions.ps1

EnsureRequiredValue -friendlyName "ComponentName" -value $componentName
EnsureRequiredValue -friendlyName "VisualStudioBranchName" -value $visualStudioBranchName

$reviewerGUID = GetReviewerGUID -reviewerGUID $reviewerGUID

& $PSScriptRoot\RIT.exe "/in=$componentName" "/vsbn=$visualStudioBranchName" /createdummypr "/u=vslsnap@microsoft.com" "/ci=$clientId" "/cs=$clientSecret" "/tp=$titlePrefix" "/wpr=$writePullRequest" "/reviewerGUID=$reviewerGUID"
