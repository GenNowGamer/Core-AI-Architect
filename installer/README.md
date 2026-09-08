# Core AI Architect (Self-contained) - Folder Installer

This folder installer installs the app from the **canonical self-contained publish output**, so users do not need to install the .NET runtime separately.

## What to package

1. Publish the app (writes the only maintained runnable folder):
   ```powershell
   powershell -File tools\Publish-App.ps1
   ```
   Output: `artifacts/CoreAiArchitect/`

2. **Day-to-day / MSI:** use that folder directly (no extra copy needed).

3. **Optional** — if you are zipping the `installer/` folder alone for someone else, stage a copy:
   ```powershell
   powershell -File installer\stage_payload.ps1
   ```
   That fills `installer/payload/win-x64/` from `artifacts/CoreAiArchitect/`.

## Install

Run as Administrator:

```powershell
cd <repo root or folder containing installer>
Set-ExecutionPolicy Bypass -Scope Process -Force
.\installer\Install-CoreAiArchitect.ps1
```

It prefers `artifacts/CoreAiArchitect/` when present, otherwise `installer/payload/win-x64/`.

It will:
- Copy files to `C:\Program Files\Core AI Architect`
- Create Start Menu and Desktop shortcuts
- Register an uninstall entry in “Apps & Features”
