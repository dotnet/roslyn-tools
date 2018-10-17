param([string] $enlistmentPath,
      [string] $clientId,
      [string] $clientSecret,
      [string] $componentName,
      [string] $visualStudioBranchName)

& .\RIT.exe  "/in=$componentName" "/vsbn=$visualStudioBranchName" /createdummypr /u=vslsnap@microsoft.com "/ep=$enlistmentPath" "/ci=$clientId" "/cs=$clientSecret"
