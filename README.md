# Core AI Architect

Windows 11 desktop tool that authors a canonical JSON **App Blueprint** (SSOT — single source of truth) for AI coding agents.

For current session state, open work, and continuity notes, see [session_handoff.md](session_handoff.md).  
For the human-readable user guide (PDF), see [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf).  
Markdown source for that guide: [docs/USER_GUIDE.md](docs/USER_GUIDE.md).

## What it does

1. Pick an app type and describe what the app should do (optional starter **template**)
2. Build **screens** with everyday things: choice list, place to type, button, results list
3. Write plain-English steps for **when someone clicks** each button
4. Optionally turn on History / Settings (**Remember**)
5. **Hand off** — readiness check, then save/export **App Blueprint** JSON (and optional Markdown)

Technical defaults (theme, navigation, architecture, build targets) are filled automatically from the form profile. There are **no sample projects** in the product itself; templates only seed screens you can edit.

## Solution layout

| Project | Role |
|---|---|
| `src/CoreAiArchitect.Core` | SSOT model, form schemas, `BlueprintCompiler`, validation, exporters |
| `src/CoreAiArchitect.App` | WPF guided wizard |
| `src/CoreAiArchitect.PowerShell` | PowerShell 7+ binary module |
| `src/CoreAiArchitect.Package` | MSIX packaging (Visual Studio) |
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

### MSIX packaging

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

See [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) for how coding agents must interpret exported blueprint JSON (`*.ssot.json`).

## Stack

- .NET 8 / WPF
- CommunityToolkit.Mvvm
- System.Text.Json
- PowerShell Standard Library (binary module)
- xUnit
