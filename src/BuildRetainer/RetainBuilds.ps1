param([string] $clientId,
      [string] $clientSecret)

$Passed = $true
function Run-BuildRetainer([string] $BuildQueueName, [string] $ComponentName) {
    & ".\BuildRetainer.exe" --BuildQueueName=$BuildQueueName --ComponentName=$ComponentName --ClientId=$clientId --ClientSecret=$clientSecret
    if ($LastExitCode -ne 0) {
        $Script:Passed = $false
    }
}

try {
    Run-BuildRetainer -BuildQueueName Roslyn-Signed -ComponentName Microsoft.CodeAnalysis.Compilers
    Run-BuildRetainer -BuildQueueName FSharp-Signed -ComponentName Microsoft.FSharp
    Run-BuildRetainer -BuildQueueName FSharp-Microbuild -ComponentName Microsoft.FSharp
    Run-BuildRetainer -BuildQueueName FSharp-Microbuild-Dev15-RC -ComponentName Microsoft.FSharp
    Run-BuildRetainer -BuildQueueName FSharp-Microbuild-Dev15-RTM -ComponentName Microsoft.FSharp
    Run-BuildRetainer -BuildQueueName TestImpact-Signed -ComponentName Microsoft.CodeAnalysis.LiveUnitTesting
    Run-BuildRetainer -BuildQueueName DotNet-Project-System -ComponentName Microsoft.VisualStudio.ProjectSystem.Managed

    if (-Not $Passed) {
        Write-Host "Build retainer has failed."
        Exit 1
    }
} catch [Exception] {
    Write-Output $_.Exception|Format-List -Force
    Exit -1
}
