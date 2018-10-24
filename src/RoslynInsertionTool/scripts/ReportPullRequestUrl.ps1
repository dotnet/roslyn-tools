$sentinelFileName = "pullRequestId.txt"
$sentinelFilePath = Join-Path (Get-Location) $sentinelFileName

if (Test-Path $sentinelFilePath) {
    $pullRequestId = [System.IO.File]::ReadAllText($sentinelFilePath).Trim()
    Write-Host "Pull request URL: https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/$pullRequestId"
}
else {
    Write-Host "No ``$sentinelFilePath`` file found."
}
