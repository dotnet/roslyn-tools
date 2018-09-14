. .\RitMutexHelper.ps1
ExitOnNoEnlistment -pause $true
# at this point `$EnlistmentPath` contains the enlistment path to use

Write-Host "PERFORM MANUAL INSERTION"
Write-Host ""
Write-Host "Supported Component Names:"
Write-Host "    Roslyn"
Write-Host "    Live Unit Testing"
Write-Host "    VS Unit Testing"
Write-Host "    Project System"
Write-Host "    F#"
$componentName = Read-Host -Prompt "Component name to insert"

Write-Host ""
$vsbn = Read-Host -Prompt "Visual Sudio branch name (insertion destination)"

cd E:\prebuilt\roslyn-tools\RIT
Write-Host "Executing: & .\RIT.exe `"/in=$componentName`" `"/vsbn=$vsbn`" /createdummypr /u=vslsnap@microsoft.com /mr=vslsnap@microsoft.com `"/ep=$EnlistmentPath`""
                       & .\RIT.exe  "/in=$componentName"   "/vsbn=$vsbn"  /createdummypr /u=vslsnap@microsoft.com /mr=vslsnap@microsoft.com  "/ep=$EnlistmentPath"
pause
