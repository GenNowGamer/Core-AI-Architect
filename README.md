# Core AI Architect

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%20Windows%2011%20(x64)-0078D6.svg?logo=windows&logoColor=white)](#build-and-run)
[![Version](https://img.shields.io/badge/Version-1.1.0-green.svg)](https://github.com/GenNowGamer/Core-AI-Architect/releases/latest)
[![Release](https://img.shields.io/badge/Release-v1.1.0-orange.svg)](https://github.com/GenNowGamer/Core-AI-Architect/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/GenNowGamer/Core-AI-Architect/total?color=2ea44f&logo=github&label=Downloads)](https://github.com/GenNowGamer/Core-AI-Architect/releases)
[![Framework](https://img.shields.io/badge/Framework-.NET%208%20WPF%20%7C%20PowerShell%207%2B-512BD4.svg?logo=dotnet&logoColor=white)](#solution-layout)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](#stack)
[![Telemetry](https://img.shields.io/badge/Telemetry-Zero%20%7C%20100%25%20Local-success.svg)](#what-it-does)

Windows 11 desktop tool that authors a canonical JSON **App Blueprint** (SSOT — single source of truth) for AI coding agents.

### 📥 Downloads

| Platform | Format | Description | Direct Download |
| :--- | :--- | :--- | :--- |
| **Windows PC** | Standalone Windows Installer (`.msi`) | Windows 10 & 11 (64-bit) — Full self-contained desktop setup | [**Download for PC (.msi)**](https://github.com/GenNowGamer/Core-AI-Architect/releases/latest/download/CoreAiArchitect-Setup.msi) |
| **Windows Portable** | Self-Contained Executable (`.exe`) | Run without installing — Portable x64 folder | [**View Release Assets**](https://github.com/GenNowGamer/Core-AI-Architect/releases/latest) |

---

## What it does

1. Fill a **guided Ad-Lib** (who it’s for, what it helps them do, main capabilities) plus optional product rules / starter templates (Blank, Master-Detail / Catalog, Dashboard & Analytics, File / Batch Processor, Simple form, List + save history)
2. Answer plain-language **Needs** (accounts, multi-device, notifications)
3. Build **screens** with visual controls: choice lists, places to type, buttons, results lists, and interactive wireframes with live reordering
4. Write plain-English steps for **when someone clicks** each button
5. Optionally turn on History / Settings (**Remember**)
6. Review the **Project Blueprint** hub with deterministic recommendations, in-app artifact inspection tabs (App Story, Blueprint JSON, AGENTS.md, Specifications, Tasks), Undo/Redo support, automated git workspace initialization, and one-click "Copy Kickoff Prompt"
7. Save and export **App Blueprint** JSON, Markdown, or a full **Agent Package** folder with agent context files (`.cursorrules`, `CLAUDE.md`, `.gemini/config.json`)

Technical defaults (theme, navigation, architecture, build targets) are filled automatically from the form profile. Starter templates seed customizable screens.

## Solution layout

| Project | Role |
|---|---|
| `src/CoreAiArchitect.Core` | SSOT model, schema catalog, `BlueprintCompiler`, schema versioning & migrations, `UndoManager`, recommendations, JSON/Markdown/Agent Package exporters |
| `src/CoreAiArchitect.App` | Modern Windows 11 Fluent dark-themed WPF wizard (Ad-Lib → Needs → Screens → Blueprint hub) with live wireframe mockup canvas and inspection tabs |
| `src/CoreAiArchitect.PowerShell` | PowerShell 7+ binary module |
| `src/CoreAiArchitect.Installer` | WiX v5 MSI installer (self-contained, builds via `dotnet build`) |
| `src/CoreAiArchitect.Package` | MSIX packaging (Visual Studio — optional) |
| `tests/CoreAiArchitect.Tests` | Core unit and UI integration tests (50 passing tests) |

## Build and run

```powershell
dotnet build CoreAiArchitect.sln -c Release
dotnet test CoreAiArchitect.sln -c Release
dotnet run --project src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release
```

`bin/` and `obj/` under each project are disposable compile intermediates. Do not treat those copies as the app to keep or share.

### Double-clickable app (self-contained exe)

**Canonical output (only maintained runnable folder):** `artifacts\CoreAiArchitect\`

```powershell
powershell -File tools\Publish-App.ps1
# equivalent:
# dotnet publish src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release -p:PublishProfile=Win64-SelfContained
```

Then open:

- `artifacts\CoreAiArchitect\CoreAiArchitect.exe`

Keep the whole `artifacts\CoreAiArchitect` folder together — the `.exe` needs the files beside it (including `schemas\`). Recreate any Desktop shortcut after publish if the target moved. Profile: `src/CoreAiArchitect.App/Properties/PublishProfiles/Win64-SelfContained.pubxml` (wired via `Directory.Build.props`).

### PowerShell module

```powershell
$moduleDir = "d:\Robert\Desktop\App Project Builds\Core_AI_Architect\src\CoreAiArchitect.PowerShell\bin\Release\net8.0"
Import-Module "$moduleDir\CoreAiArchitect.psd1"
Get-SsotFormProfile
New-SsotDocument -Profile wpf_desktop -OutFile .\my-project.ssot.json
Test-SsotDocument -Path .\my-project.ssot.json -Profile wpf_desktop
Export-SsotMarkdown -Path .\my-project.ssot.json -OutFile .\my-project.md
```

### MSI installer (recommended for distribution)

Build a Windows Installer `.msi` that includes the self-contained .NET 8 runtime, schemas, PowerShell module, Desktop and Start Menu shortcuts, and proper Add/Remove Programs registration. Users double-click the MSI — no other installs needed.

```powershell
# 1. Publish the self-contained app -> artifacts/CoreAiArchitect/
powershell -File tools\Publish-App.ps1

# 2. Build the MSI (~60 MB)
dotnet build src/CoreAiArchitect.Installer/CoreAiArchitect.Installer.wixproj -c Release
```

Output: `src/CoreAiArchitect.Installer/bin/Release/CoreAiArchitect.Installer.msi`

Installs to `C:\Program Files\Coresynth Studios\Core AI Architect\`. Requires the WiX v5 CLI tool (`dotnet tool install --global wix`).

The installer project is in the solution but excluded from the default `dotnet build CoreAiArchitect.sln` so normal development doesn't require WiX tooling.

### MSIX packaging (optional)

1. Install Visual Studio workloads with **MSIX Packaging Tools** ([Microsoft Learn](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)).
2. Open `src/CoreAiArchitect.Package/CoreAiArchitect.Package.wapproj` (or add it to the solution in VS).
3. Set platform to **x64**, set the packaging project as startup, then **Publish → Create App Packages** for sideloading.

The WPF app still builds and runs via `dotnet` or the self-contained folder publish above, without MSIX tooling.

## Form profiles

Located in `schemas/` (wizard surfaces the common ones; others remain available to the module/schemas):

- `wpf_desktop` (Windows desktop app)
- `android_app` (Android app — Jetpack Compose / Kotlin defaults)
- `web_api` (Website / API)
- `custom` (Something else)
- Also: `winpe_tool`, `cli_tool`, `powershell_module`, `deployment_automation`

## Agent contract

Blueprint JSON (`*.ssot.json`) specifies canonical system definitions, including **decision modes** (`agent_discretion`) and **requirements**.

From the app you can also **Export Agent Package** — a folder with `AGENTS.md`, specifications, preconfigured agent rules (`.cursorrules`, `CLAUDE.md`, `.gemini/config.json`), and phased tasks generated from the same SSOT.

## Stack

- .NET 8 / WPF
- CommunityToolkit.Mvvm
- System.Text.Json
- PowerShell Standard Library (binary module)
- WiX Toolset v5 (MSI installer)
- xUnit
