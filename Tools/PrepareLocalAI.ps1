#Requires -Version 5.1
[CmdletBinding()]
param([string]$CrtPath)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localRoot = Join-Path $projectRoot 'LocalAI'
$downloads = Join-Path $localRoot 'downloads'
# Validate build-machine prerequisites before downloading the large model.
if (!$CrtPath) {
    $crt = Get-ChildItem "${env:ProgramFiles}/Microsoft Visual Studio/*/*/VC/Redist/MSVC/*/x64/Microsoft.VC*.CRT" -Directory -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
    if ($crt) { $CrtPath = $crt.FullName }
}
if (!$CrtPath) { throw 'Install Visual Studio C++ build tools, or supply -CrtPath pointing to its x64 Microsoft.VC*.CRT redistributable folder.' }
$CrtPath = (Resolve-Path -LiteralPath $CrtPath).Path
foreach ($name in @('msvcp140.dll','vcruntime140.dll','vcruntime140_1.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $CrtPath $name))) { throw "Missing x64 CRT dependency: $name in $CrtPath" }
}
if (!(Get-Command curl.exe -ErrorAction SilentlyContinue)) { throw 'curl.exe is required (included with current Windows 10/11).' }

New-Item -ItemType Directory -Force -Path $downloads,(Join-Path $localRoot 'models'),(Join-Path $localRoot 'licenses') | Out-Null

function Get-VerifiedFile($url, $destination, $hash) {
    if ((Test-Path -LiteralPath $destination) -and (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $hash) {
        Write-Output "Verified existing $([IO.Path]::GetFileName($destination))"
        return
    }
    $partial = "$destination.partial"
    & curl.exe --fail --location --retry 3 --silent --show-error --output $partial $url
    if ($LASTEXITCODE -ne 0) { throw "Download failed: $url" }
    if ((Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash -ne $hash) { throw "Checksum mismatch: $partial" }
    Move-Item -LiteralPath $partial -Destination $destination -Force
    Write-Output "Downloaded and verified $([IO.Path]::GetFileName($destination))"
}

$archive = Join-Path $downloads 'llama-b11146-bin-win-cpu-x64.zip'
Get-VerifiedFile 'https://github.com/ggml-org/llama.cpp/releases/download/b11146/llama-b11146-bin-win-cpu-x64.zip' $archive '14cf1303ca9ac3abd94816850532f9f9a69ac66fbaca3776fc6f9061c2fac1d1'
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $localRoot 'runtime') -Force
Get-VerifiedFile 'https://huggingface.co/ggml-org/Qwen3-1.7B-GGUF/resolve/main/Qwen3-1.7B-Q4_K_M.gguf?download=true' (Join-Path $localRoot 'models/Qwen3-1.7B-Q4_K_M.gguf') 'd2387ca2dbfee2ffabce7120d3770dadca0b293052bc2f0e138fdc940d9bc7b5'
if (!(Test-Path (Join-Path $localRoot 'licenses/llama.cpp-LICENSE.txt'))) { Invoke-WebRequest -UseBasicParsing 'https://raw.githubusercontent.com/ggml-org/llama.cpp/b11146/LICENSE' -OutFile (Join-Path $localRoot 'licenses/llama.cpp-LICENSE.txt') }
if (!(Test-Path (Join-Path $localRoot 'licenses/Qwen3-LICENSE.txt'))) { Invoke-WebRequest -UseBasicParsing 'https://huggingface.co/Qwen/Qwen3-1.7B-GGUF/resolve/main/LICENSE' -OutFile (Join-Path $localRoot 'licenses/Qwen3-LICENSE.txt') }


Copy-Item (Join-Path $CrtPath '*.dll') (Join-Path $localRoot 'runtime') -Force

$vsRoot = [IO.Path]::GetFullPath((Join-Path $CrtPath '../../../../../..'))
$redistNotice = Get-ChildItem (Join-Path $vsRoot 'Licenses') -Filter Redist.txt -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($redistNotice) { Copy-Item $redistNotice.FullName (Join-Path $localRoot 'licenses/Microsoft-Visual-Studio-Redist.txt') -Force }
Write-Output 'Local AI model and runtime are ready. End users do not need this script.'
