param(
  [string]$PayloadDir = '',
  [string]$InstallDir = (Join-Path $env:ProgramFiles 'Core AI Architect')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-PayloadDir([string]$requested) {
  if (-not [string]::IsNullOrWhiteSpace($requested)) {
    return $requested
  }

  $repoRoot = Split-Path -Parent $PSScriptRoot
  $canonical = Join-Path $repoRoot 'artifacts\CoreAiArchitect'
  $staged = Join-Path $PSScriptRoot 'payload\win-x64'

  if (Test-Path (Join-Path $canonical 'CoreAiArchitect.exe')) {
    return $canonical
  }
  if (Test-Path (Join-Path $staged 'CoreAiArchitect.exe')) {
    return $staged
  }

  throw @"
No app payload found. Publish first, then re-run this installer:

  powershell -File tools\Publish-App.ps1

Expected: $canonical
Optional staged copy for a standalone installer zip: $staged
"@
}

$PayloadDir = Resolve-PayloadDir $PayloadDir

function Test-IsAdministrator {
  $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
  $principal = New-Object Security.Principal.WindowsPrincipal($identity)
  return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Restart-ElevatedIfNeeded {
  if (Test-IsAdministrator) { return }

  Write-Output 'Administrator privileges are required. Relaunching elevated...'
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = 'powershell.exe'
  $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -PayloadDir `"$PayloadDir`" -InstallDir `"$InstallDir`""
  $psi.Verb = 'runas'
  [System.Diagnostics.Process]::Start($psi) | Out-Null
  exit 0
}

Restart-ElevatedIfNeeded

if (!(Test-Path $PayloadDir)) {
  throw "Payload directory not found: $PayloadDir"
}

$exePath = Join-Path $PayloadDir 'CoreAiArchitect.exe'
if (!(Test-Path $exePath)) {
  throw "Expected installer payload to contain CoreAiArchitect.exe at: $exePath"
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

Write-Output "Copying files to: $InstallDir"
# Copy everything from the published folder (includes schemas and PowerShell module files).
Copy-Item -Path (Join-Path $PayloadDir '*') -Destination $InstallDir -Recurse -Force

# Copy uninstall script into the install folder so ARP uninstall can always find it.
$installedInstallerDir = Join-Path $InstallDir 'installer'
New-Item -ItemType Directory -Force -Path $installedInstallerDir | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'Uninstall-CoreAiArchitect.ps1') -Destination (Join-Path $installedInstallerDir 'Uninstall-CoreAiArchitect.ps1') -Force

# Create shortcuts for the current user.
$shell = New-Object -ComObject WScript.Shell

$shortcutDir = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Core AI Architect'
New-Item -ItemType Directory -Force -Path $shortcutDir | Out-Null

$startMenuShortcut = Join-Path $shortcutDir 'Core AI Architect.lnk'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Core AI Architect.lnk'

function Create-Shortcut([string]$shortcutPath, [string]$targetPath) {
  $shortcut = $shell.CreateShortcut($shortcutPath)
  $shortcut.TargetPath = $targetPath
  $shortcut.WorkingDirectory = Split-Path $targetPath
  $shortcut.IconLocation = $targetPath
  $shortcut.Save()
}

Create-Shortcut -shortcutPath $startMenuShortcut -targetPath (Join-Path $InstallDir 'CoreAiArchitect.exe')
Create-Shortcut -shortcutPath $desktopShortcut -targetPath (Join-Path $InstallDir 'CoreAiArchitect.exe')

# Register uninstall info in ARP (Programs & Features).
$uninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\CoreAiArchitect'
New-Item -Path $uninstallKey -Force | Out-Null

$uninstallScriptInInstallDir = Join-Path $installedInstallerDir 'Uninstall-CoreAiArchitect.ps1'
$uninstallCmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallScriptInInstallDir`""

New-ItemProperty -Path $uninstallKey -Name 'DisplayName' -Value 'Core AI Architect' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'DisplayVersion' -Value '1.0.0.0' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'Publisher' -Value 'Coresynth Studios' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'InstallLocation' -Value $InstallDir -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'UninstallString' -Value $uninstallCmd -PropertyType String -Force | Out-Null

Write-Output 'Install complete.'

