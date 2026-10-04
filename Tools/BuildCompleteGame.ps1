#Requires -Version 5.1
<##
Build a portable Windows game with its verified local model and native runtime.
Only this script, PrepareLocalAI.ps1, the manifest/licenses and Unity sources
belong in Git. Downloaded binaries and generated packages remain ignored.
##>
[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$CrtPath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$versionFile = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
$versionMatch = [regex]::Match((Get-Content -LiteralPath $versionFile -Raw), 'm_EditorVersion:\s*(\S+)')
if (!$versionMatch.Success) { throw 'Cannot determine the Unity version from ProjectVersion.txt.' }
$version = $versionMatch.Groups[1].Value
if (!$UnityPath) {
    $candidates = @(
        $env:UNITY_EDITOR_PATH,
        "${env:ProgramFiles}/Unity/Hub/Editor/$version/Editor/Unity.exe",
        "C:/Unity/Hub/Editor/$version/Editor/Unity.exe",
        "D:/Unity/Hub/Editor/$version/Editor/Unity.exe",
        "D:/unity/$version/Editor/Unity.exe"
    )
    $UnityPath = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } | Select-Object -First 1
}
if (!$UnityPath -or !(Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Install Unity $version with Windows Build Support, or pass -UnityPath '.../Editor/Unity.exe'."
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
$installedVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($UnityPath).ProductVersion
if (!$installedVersion.StartsWith($version, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Expected Unity $version but found $installedVersion at $UnityPath. Use the project's matching editor."
}
$support = Join-Path (Split-Path -Parent $UnityPath) 'Data/PlaybackEngines/WindowsStandaloneSupport'
if (!(Test-Path -LiteralPath $support)) { throw 'Install Windows Build Support for this Unity editor through Unity Hub.' }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile')) {
    throw 'This Unity project appears to be open. Close its Unity Editor before running the build script.'
}

Write-Host '[1/4] Prepare verified model and native runtime.'
& (Join-Path $PSScriptRoot 'PrepareLocalAI.ps1') -CrtPath $CrtPath

$runName = 'Sleepet-Windows-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6)
$buildRoot = Join-Path $projectRoot 'Builds'
$output = Join-Path $buildRoot $runName
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $buildRoot,$logDirectory | Out-Null
$log = Join-Path $logDirectory ($runName + '.log')
Write-Host "[2/4] Build with Unity $version. Log: $log"
# Wait for the editor itself, not its long-lived helper-process tree.
$arguments = '-batchmode -nographics -quit -projectPath "' + $projectRoot +
    '" -executeMethod Sleepet.Editor.SleepetWindowsBuild.Build -sleepet-build-output "' + $output +
    '" -logFile "' + $log + '"'
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $log) -or
    !(Select-String -LiteralPath $log -SimpleMatch 'SLEEPET_WINDOWS_BUILD_SUCCESS' -Quiet)) {
    throw "Unity build did not complete successfully (exit $($process.ExitCode)). See $log"
}

Write-Host '[3/4] Validate complete portable game.'
$manifest = Get-Content -LiteralPath (Join-Path $output 'LocalAI/manifest.json') -Raw | ConvertFrom-Json
foreach ($relative in @('Sleepet.exe','UnityPlayer.dll','Sleepet_Data/globalgamemanagers',
    'LocalAI/runtime/llama-server.exe','LocalAI/runtime/msvcp140.dll','LocalAI/runtime/vcruntime140.dll',
    'LocalAI/runtime/vcruntime140_1.dll','LocalAI/licenses/llama.cpp-LICENSE.txt','LocalAI/licenses/Qwen3-LICENSE.txt')) {
    if (!(Test-Path -LiteralPath (Join-Path $output $relative) -PathType Leaf)) { throw "Package is incomplete: $relative" }
}
$model = Join-Path $output ('LocalAI/models/' + $manifest.modelFile)
if (!(Test-Path -LiteralPath $model) -or (Get-FileHash -LiteralPath $model -Algorithm SHA256).Hash -ne $manifest.modelSha256) {
    throw 'Packaged model checksum mismatch.'
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'LocalAI/README.md') -Destination (Join-Path $output 'LOCAL_AI_README.md')

Write-Host '[4/4] Create and verify ZIP (this can take a few minutes).'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = $output + '.zip'
$partial = $zipPath + '.partial'
[IO.Compression.ZipFile]::CreateFromDirectory($output, $partial, [IO.Compression.CompressionLevel]::Fastest, $true)
. (Join-Path $PSScriptRoot 'TestGameArchive.ps1')
Test-GameArchive -Path $partial -RootName $runName -ModelFile $manifest.modelFile -ModelSha256 $manifest.modelSha256
Move-Item -LiteralPath $partial -Destination $zipPath
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($zipPath + '.sha256', $zipHash + '  ' + [IO.Path]::GetFileName($zipPath) + [Environment]::NewLine)
Write-Host "SUCCESS: $zipPath"
Write-Host "Executable: $(Join-Path $output 'Sleepet.exe')"
Write-Host 'End users only extract the entire ZIP and double-click Sleepet.exe.'
