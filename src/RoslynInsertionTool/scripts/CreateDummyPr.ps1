param([string] $enlistmentPath,
      [string] $componentName,
      [string] $visualStudioBranchName)

& .\RIT.exe  "/in=$componentName" "/vsbn=$visualStudioBranchName" /createdummypr /u=vslsnap@microsoft.com "/ep=$enlistmentPath"
