# Shared by the builder and archive recovery/verification. Compatible with PS 5.1 and 7.
function Test-GameArchive {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$RootName,
        [Parameter(Mandatory=$true)][string]$ModelFile,
        [Parameter(Mandatory=$true)][string]$ModelSha256
    )
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        # .NET Framework and modern .NET expose ZIP separators differently.
        $modelName = $RootName + '/LocalAI/models/' + $ModelFile
        $executableName = $RootName + '/Sleepet.exe'
        $modelEntry = $null
        $executableEntry = $null
        foreach ($entry in $archive.Entries) {
            $normalized = $entry.FullName.Replace('\', '/')
            if ($normalized -ceq $modelName) { $modelEntry = $entry }
            if ($normalized -ceq $executableName) { $executableEntry = $entry }
        }
        if ($null -eq $modelEntry -or $null -eq $executableEntry) {
            throw 'ZIP is missing its model or executable.'
        }
        if ($executableEntry.Length -eq 0) { throw 'ZIP contains an empty executable.' }
        $stream = $modelEntry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $actual = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '').ToLowerInvariant() }
        finally { $stream.Dispose(); $sha.Dispose() }
        if ($actual -ne $ModelSha256) { throw 'Model checksum inside ZIP does not match.' }
    } finally { $archive.Dispose() }
}
