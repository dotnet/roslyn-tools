param (
    [string]$sourceDirectoryName,
    [string]$destinationArchiveFileName
)

Add-Type -AssemblyName 'System.IO.Compression.FileSystem'
[System.IO.Compression.ZipFile]::CreateFromDirectory($sourceDirectoryName, $destinationArchiveFileName)
