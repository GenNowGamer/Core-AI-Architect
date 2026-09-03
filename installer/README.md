# Core AI Architect (Self-contained) - Folder Installer

This folder installer installs the app from the **already-published self-contained payload**, so users do not need to install the .NET runtime separately.

## What to package

1. Publish the app using the existing self-contained profile:
   - `src/CoreAiArchitect.App/Properties/PublishProfiles/Win64-SelfContained.pubxml`
2. Copy the published output into:
   - `installer/payload/win-x64/`
3. Ship the `installer/` folder to users (or zip it).

## Install

Run as Administrator:

```powershell
cd <folder containing installer>
Set-ExecutionPolicy Bypass -Scope Process -Force
.\installer\Install-CoreAiArchitect.ps1
```

It will:
- Copy files to `C:\Program Files\Core AI Architect`
- Create Start Menu and Desktop shortcuts
- Register an uninstall entry in “Apps & Features”

