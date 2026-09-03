$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = 'D:\Robert\Desktop\App Project Builds\Core_AI_Architect'
$src = Join-Path $repo 'src\CoreAiArchitect.App\bin\publish\win-x64'
$dest = Join-Path $repo 'installer\payload\win-x64'

if (!(Test-Path $src)) {
  throw "Source payload not found: $src"
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

Write-Output "Staged payload to: $dest"

