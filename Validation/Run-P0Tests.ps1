param([string]$UnityEditor = 'D:\unity\6000.3.21f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$p0Project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw "Unity Editor not found: $UnityEditor" }
$p0PrepareArgs = '-batchmode -quit -projectPath "' + $p0Project + '" -executeMethod Sleepet.Editor.P0ProjectSetup.Prepare -logFile "' + (Join-Path $PSScriptRoot 'prepare.log') + '"'
$p0Process = Start-Process -FilePath $UnityEditor -ArgumentList $p0PrepareArgs -WindowStyle Hidden -PassThru -Wait
if ($p0Process.ExitCode -ne 0) { throw 'Unity scene preparation failed. See Validation/prepare.log.' }
$p0TestArgs = '-batchmode -projectPath "' + $p0Project + '" -runTests -testPlatform PlayMode -assemblyNames Sleepet.PlayModeTests -testResults "' + (Join-Path $PSScriptRoot 'editable-results.xml') + '" -logFile "' + (Join-Path $PSScriptRoot 'editable-playmode.log') + '"'
$p0Process = Start-Process -FilePath $UnityEditor -ArgumentList $p0TestArgs -WindowStyle Hidden -PassThru -Wait
if ($p0Process.ExitCode -ne 0) { throw 'Unity tests failed. See Validation/editable-results.xml and editable-playmode.log.' }
[xml]$p0Results = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'editable-results.xml')
$p0Results.'test-run' | Select-Object result, total, passed, failed, skipped
