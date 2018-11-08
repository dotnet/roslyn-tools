param([string] $enlistmentPath,
      [string] $clientId,
      [string] $clientSecret,
      [string] $componentName,
      [string] $visualStudioBranchName,
      [string] $titlePrefix,
      [string] $writePullRequest)

. .\HelperFunctions.ps1

EnsureRequiredValue -friendlyName "ComponentName" -value $componentName
EnsureRequiredValue -friendlyName "VisualStudioBranchName" -value $visualStudioBranchName

& .\RIT.exe "/in=$componentName" "/vsbn=$visualStudioBranchName" /createdummypr "/u=vslsnap@microsoft.com" "/ep=$enlistmentPath" "/ci=$clientId" "/cs=$clientSecret" "/tp=$titlePrefix" "/wpr=$writePullRequest"
