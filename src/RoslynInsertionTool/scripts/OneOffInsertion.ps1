##########################################################################
# Changes to this file will NOT be automatically deployed to the server. #
#                                                                        #
# Changes should be made on both the server and in source control.       #
##########################################################################

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

$ic = "true"
if (($componentName -eq "Live Unit Testing") -or ($componentName -eq "Project System") -or ($componentName -eq "F#")) {
    $ic = "false"
}

$id = "false"
# only rarely will DevDiv packages be inserted, and only for Roslyn

cd E:\prebuilt\roslyn-tools\RIT
Write-Host "Executing: & .\RIT.exe `"/in=$componentName`" `"/bn=$bn`" `"/vsbn=$vsbn`" `"/bq=$bq`" /ic=$ic /id=$id /qv=true /u=vslsnap@microsoft.com `"/ep=$EnlistmentPath`""
                       & .\RIT.exe  "/in=$componentName"   "/bn=$bn"   "/vsbn=$vsbn"   "/bq=$bq"  /ic=$ic /id=$id /qv=true /u=vslsnap@microsoft.com  "/ep=$EnlistmentPath"
pause
