# Optional: copy the canonical publish folder into installer/payload for shipping
# a standalone installer zip. Day-to-day running should use artifacts/CoreAiArchitect.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = Split-Path -Parent $PSScriptRoot
$src = Join-Path $repo 'artifacts\CoreAiArchitect'
$dest = Join-Path $repo 'installer\payload\win-x64'

if (!(Test-Path (Join-Path $src 'CoreAiArchitect.exe'))) {
    throw @"
Canonical publish output not found: $src

Publish first:
  powershell -File tools\Publish-App.ps1
  # or: dotnet publish src\CoreAiArchitect.App\CoreAiArchitect.App.csproj -c Release -p:PublishProfile=Win64-SelfContained
"@
}

if (Test-Path $dest) {
    Remove-Item -Path $dest -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item -Path (Join-Path $src '*') -Destination $dest -Recurse -Force

$exe = Join-Path $dest 'CoreAiArchitect.exe'
if (!(Test-Path $exe)) {
    throw "Payload staging failed; CoreAiArchitect.exe not found at: $exe"
}

Write-Output "Staged installer payload from canonical publish:"
Write-Output "  $src"
Write-Output "  -> $dest"
