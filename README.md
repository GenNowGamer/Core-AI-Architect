# Core AI Architect

Windows 11 desktop tool that authors a canonical JSON **App Blueprint** (SSOT — single source of truth) for AI coding agents.

For current session state, open work, and continuity notes, see [session_handoff.md](session_handoff.md).  
For the human-readable user guide (PDF), see [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf).  
Markdown source for that guide: [docs/USER_GUIDE.md](docs/USER_GUIDE.md).

## What it does

1. Fill a **guided Ad-Lib** (who it’s for, what it helps them do, main capabilities) plus optional product rules / template
2. Answer plain-language **Needs** (accounts, multi-device, notifications)
3. Build **screens** with everyday things: choice list, place to type, button, results list
4. Write plain-English steps for **when someone clicks** each button
5. Optionally turn on History / Settings (**Remember**)
6. Review the **Project Blueprint** hub (recommendations with Why?, Simple/Standard/Expert detail), then save/export **App Blueprint** JSON, Markdown, or a full **Agent Package** folder

Technical defaults (theme, navigation, architecture, build targets) are filled automatically from the form profile. There are **no sample projects** in the product itself; templates only seed screens you can edit.

## Solution layout

| Project | Role |
|---|---|
| `src/CoreAiArchitect.Core` | SSOT model, form schemas, `BlueprintCompiler`, recommendations, validation, JSON/Markdown/Agent Package exporters |
| `src/CoreAiArchitect.App` | WPF guided wizard (Ad-Lib → Needs → Screens → Blueprint hub) |
| `src/CoreAiArchitect.PowerShell` | PowerShell 7+ binary module |
| `src/CoreAiArchitect.Installer` | WiX v5 MSI installer (self-contained, builds via `dotnet build`) |
| `src/CoreAiArchitect.Package` | MSIX packaging (Visual Studio — optional) |
| `tests/CoreAiArchitect.Tests` | Core unit tests |

## Build and run

```powershell
cd "d:\Robert\Desktop\App Project Builds\Core_AI_Architect"
dotnet build CoreAiArchitect.sln -c Release
dotnet test CoreAiArchitect.sln -c Release
dotnet run --project src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release
```

### Double-clickable app (self-contained exe)

Publish a Windows x64 build that includes the .NET 8 desktop runtime (no separate runtime install, no `dotnet run`):

```powershell
dotnet publish src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release -p:PublishProfile=Win64-SelfContained
```

Then open:

- Desktop shortcut **Core AI Architect** (`%USERPROFILE%\Desktop\Core AI Architect.lnk`), or
- `src\CoreAiArchitect.App\bin\publish\win-x64\CoreAiArchitect.exe`

Keep the whole `win-x64` folder together — the `.exe` needs the files beside it (including `schemas\`). Recreate the Desktop shortcut after publish if you moved the folder. Profile: `src/CoreAiArchitect.App/Properties/PublishProfiles/Win64-SelfContained.pubxml`.

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
# 1. Publish the self-contained app
dotnet publish src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release -p:PublishProfile=Win64-SelfContained

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
- `web_api` (Website / API)
- `custom` (Something else)
- Also: `winpe_tool`, `cli_tool`, `powershell_module`, `deployment_automation`

## Agent contract

See [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) for how coding agents must interpret exported blueprint JSON (`*.ssot.json`), including **decision modes** (`agent_discretion`) and **requirements**.

From the app you can also **Export Agent Package** — a folder with `AGENTS.md`, specifications, and phased tasks generated from the same SSOT.

## Stack

- .NET 8 / WPF
- CommunityToolkit.Mvvm
- System.Text.Json
- PowerShell Standard Library (binary module)
- WiX Toolset v5 (MSI installer)
- xUnit
