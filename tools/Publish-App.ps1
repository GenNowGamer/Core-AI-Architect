# Publishes the self-contained WPF app to the single canonical folder:
#   artifacts/CoreAiArchitect/
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'src\CoreAiArchitect.App\CoreAiArchitect.App.csproj'
$outDir = Join-Path $repo 'artifacts\CoreAiArchitect'

# Stop running app instances so DLLs are not locked during compilation/copy
$running = Get-Process -Name 'CoreAiArchitect' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Closing running instance of Core AI Architect..." -ForegroundColor Yellow
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
}

Write-Host "Publishing Core AI Architect -> $outDir"
dotnet publish $project -c Release -p:PublishProfile=Win64-SelfContained
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $outDir 'CoreAiArchitect.exe'
if (!(Test-Path $exe)) {
    throw "Publish succeeded but exe not found at: $exe"
}

Write-Host "OK: $exe"
Write-Host "Run the app from that folder (keep the whole artifacts\CoreAiArchitect directory together)."
