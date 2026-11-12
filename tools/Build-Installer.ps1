<#
.SYNOPSIS
  Publishes the self-contained app and compiles the standalone Windows Installer (.msi).
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot 'artifacts'

Write-Host "1/2. Publishing self-contained application..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Publish-App.ps1')

Write-Host "`n2/2. Compiling Windows Installer (.msi)..." -ForegroundColor Cyan
$wixProj = Join-Path $repoRoot 'src\CoreAiArchitect.Installer\CoreAiArchitect.Installer.wixproj'
dotnet build $wixProj -c Release

$sourceMsi = Join-Path $repoRoot 'src\CoreAiArchitect.Installer\bin\Release\CoreAiArchitect.Installer.msi'
$destMsi = Join-Path $artifactsDir 'CoreAiArchitect-Setup.msi'

if (Test-Path $sourceMsi) {
    Copy-Item -Path $sourceMsi -Destination $destMsi -Force
    Write-Host "`nInstaller ready at: $destMsi" -ForegroundColor Green
    Write-Host "Double-click this file to install Core AI Architect to your PC." -ForegroundColor Green
} else {
    throw "MSI build completed but output file not found at $sourceMsi"
}
