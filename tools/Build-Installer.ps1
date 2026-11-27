<#
.SYNOPSIS
  Publishes the self-contained app and compiles the standalone Windows Installer (.msi).
  Optional -Bump increments the patch version in Directory.Build.props and README.md.
  Optional -Install launches CoreAiArchitect-Setup.msi after building.
#>
[CmdletBinding()]
param(
  [switch]$Bump,
  [switch]$Install
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot 'artifacts'

if ($Bump) {
    $propsPath = Join-Path $repoRoot 'Directory.Build.props'
    $props = Get-Content -Raw -Path $propsPath
    if ($props -match '<Version>([^<]+)</Version>') {
        $currentVersion = $Matches[1].Trim()
        $parts = $currentVersion.Split('.')
        while ($parts.Length -lt 3) { $parts += '0' }
        $patch = 0
        [void][int]::TryParse($parts[2], [ref]$patch)
        $nextVersion = "$($parts[0]).$($parts[1]).$($patch + 1)"

        $props = $props -replace '(<Version>)[^<]+(</Version>)', "`${1}$nextVersion`$2"
        $props = $props -replace '(<FileVersion>)[^<]+(</FileVersion>)', "`${1}$nextVersion.0`$2"
        $props = $props -replace '(<AssemblyVersion>)[^<]+(</AssemblyVersion>)', "`${1}$nextVersion.0`$2"
        Set-Content -Path $propsPath -Value $props -NoNewline

        $readmePath = Join-Path $repoRoot 'README.md'
        if (Test-Path $readmePath) {
            $readme = Get-Content -Raw -Path $readmePath
            $readme = $readme.Replace("Version-$currentVersion", "Version-$nextVersion")
            $readme = $readme.Replace("Release-v$currentVersion", "Release-v$nextVersion")
            Set-Content -Path $readmePath -Value $readme -NoNewline
        }

        Write-Host "Bumped version: $currentVersion -> $nextVersion" -ForegroundColor Yellow
    }
}

Write-Host "1/2. Publishing self-contained application..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Publish-App.ps1')

Write-Host "`n2/2. Compiling Windows Installer (.msi)..." -ForegroundColor Cyan
$wixProj = Join-Path $repoRoot 'src\CoreAiArchitect.Installer\CoreAiArchitect.Installer.wixproj'
dotnet build $wixProj -c Release
if ($LASTEXITCODE -ne 0) {
    throw "MSI build failed with exit code $LASTEXITCODE"
}

$sourceMsi = Join-Path $repoRoot 'src\CoreAiArchitect.Installer\bin\Release\CoreAiArchitect.Installer.msi'
$destMsi = Join-Path $artifactsDir 'CoreAiArchitect-Setup.msi'

if (Test-Path $sourceMsi) {
    Copy-Item -Path $sourceMsi -Destination $destMsi -Force
    Write-Host "`nInstaller ready at: $destMsi" -ForegroundColor Green
    Write-Host "Double-click this file to install Core AI Architect to your PC." -ForegroundColor Green
    if ($Install) {
        Write-Host "`nLaunching installer..." -ForegroundColor Cyan
        Start-Process -FilePath $destMsi
    }
} else {
    throw "MSI build completed but output file not found at $sourceMsi"
}
