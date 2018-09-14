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
$bq = Read-Host -Prompt "Build queue name"
$bn = Read-Host -Prompt "Component branch name"

Write-Host ""
$vsbn = Read-Host -Prompt "Visual Sudio branch name (insertion destination)"
$upr = Read-Host -Prompt "Existing PR number"

$ic = "true"
switch ($componentName) {
    (($_ -eq "Live Unit Testing") -or ($_ -eq "Project System") -or ($_ -eq "F#")) {
        $ic = "false"
    }
}

$id = "false"
# only rarely will DevDiv packages be inserted, and only for Roslyn

cd E:\prebuilt\roslyn-tools\RIT
Write-Host "Executing: & .\RIT.exe `"/in=$componentName`" `"/bn=$bn`" `"/vsbn=$vsbn`" `"/bq=$bq`" /ic=$ic /id=$id /qv=true /updateexistingpr=$upr /u=vslsnap@microsoft.com /mr=vslsnap@microsoft.com `"/ep=$EnlistmentPath`""
                       & .\RIT.exe  "/in=$componentName"   "/bn=$bn"   "/vsbn=$vsbn"   "/bq=$bq"  /ic=$ic /id=$id /qv=true /updateexistingpr=$upr /u=vslsnap@microsoft.com /mr=vslsnap@microsoft.com  "/ep=$EnlistmentPath"
pause
