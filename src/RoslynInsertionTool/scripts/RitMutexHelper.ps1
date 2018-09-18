##########################################################################
# Changes to this file will NOT be automatically deployed to the server. #
#                                                                        #
# Changes should be made on both the server and in source control.       #
##########################################################################

$BaseMutexName = "Global\RoslynInsertionTool"
$BaseEnlistmentPath = "E:\VS2"

# eventually we may want to add multiple enlistments and cycle through to find the first available; e.g., E:\VS1, E:\VS2, etc.
$mtx = New-Object System.Threading.Mutex($false, $BaseMutexName)
If ($mtx.WaitOne(100)) {
    $EnlistmentPath = $BaseEnlistmentPath
} Else {
    $EnlistmentPath = ""
}

function ExitOnNoEnlistment([string] $message = "", [int] $exitCode = 0, [bool] $pause = $false) {
    If ($EnlistmentPath -eq "") {
        Write-Host "Insertion mutex is locked, please try again later."
        If ($message -ne "") {
            Write-Host $message
        }
        If ($pause) {
            pause
        }
        exit $exitCode
    }   
}
