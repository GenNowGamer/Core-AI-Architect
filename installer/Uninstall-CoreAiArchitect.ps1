param(
  [string]$InstallDir = (Join-Path $env:ProgramFiles 'Core AI Architect')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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
  $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -InstallDir `"$InstallDir`""
  $psi.Verb = 'runas'
  [System.Diagnostics.Process]::Start($psi) | Out-Null
  exit 0
}

Restart-ElevatedIfNeeded

$shell = New-Object -ComObject WScript.Shell

$shortcutDir = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Core AI Architect'
$startMenuShortcut = Join-Path $shortcutDir 'Core AI Architect.lnk'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Core AI Architect.lnk'

function Try-RemoveFile([string]$path) {
  if (Test-Path $path) {
    Remove-Item -Path $path -Force
  }
}

Try-RemoveFile -path $startMenuShortcut
Try-RemoveFile -path $desktopShortcut

if (Test-Path $InstallDir) {
  Write-Output "Removing: $InstallDir"
  Remove-Item -Path $InstallDir -Recurse -Force
}

$uninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\CoreAiArchitect'
if (Test-Path $uninstallKey) {
  Remove-Item -Path $uninstallKey -Recurse -Force
}

Write-Output 'Uninstall complete.'

