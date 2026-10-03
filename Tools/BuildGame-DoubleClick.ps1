#Requires -Version 5.1
[CmdletBinding()]
param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
try {
    $projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $builder = Join-Path $PSScriptRoot 'BuildCompleteGame.ps1'
    foreach ($file in @($builder, (Join-Path $PSScriptRoot 'PrepareLocalAI.ps1'))) {
        if (!(Test-Path -LiteralPath $file)) { throw "Required project script is missing: $file" }
        $tokens = $null; $parseErrors = $null
        [System.Management.Automation.Language.Parser]::ParseFile($file, [ref]$tokens, [ref]$parseErrors) | Out-Null
        if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
    }
    if ($CheckOnly) { Write-Host 'Launcher and build scripts: PASS'; exit 0 }
    Write-Host 'SLEEPET - ONE-CLICK WINDOWS BUILD'
    Write-Host 'Downloading dependencies, building the game and creating the complete ZIP.'
    Write-Host 'Keep this window open until SUCCESS appears. No commands need to be entered.'
    Write-Host ''
    & $builder
    $outputDirectory = Join-Path $projectRoot 'Builds'
    Write-Host ''
    Write-Host "Finished. Your complete game ZIP is in: $outputDirectory" -ForegroundColor Green
    # Open the user's output directory after a successful build only.
    Invoke-Item -LiteralPath $outputDirectory
    exit 0
}
catch {
    Write-Host ''
    Write-Host 'BUILD FAILED' -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host 'Check the message above and the project Logs folder. Existing packages are unchanged.'
    exit 1
}
