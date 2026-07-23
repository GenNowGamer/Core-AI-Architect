# Core AI Architect

Windows 11 desktop tool that authors a canonical JSON **SSOT** (single source of truth) for AI coding agents.

For current session state, open work, and continuity notes, see [session_handoff.md](session_handoff.md).  
For the human-readable user guide (PDF), see [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf).  
Markdown source for that guide: [docs/USER_GUIDE.md](docs/USER_GUIDE.md).

## What it does

1. Load a **form profile** schema (`schemas/*.json`)
2. Guide you through a plain-language **wizard** (app kind, purpose, platform, features, then review screens/data/flows)
3. On **Screens and buttons**, add/reorder screens and add buttons vs input controls
4. Validate readiness and export **JSON SSOT** and **Markdown** documentation

There are **no sample projects** or demo inventory data. New documents start from profile defaults plus smart feature templates; you refine them in the review steps.

## Solution layout

| Project | Role |
|---|---|
| `src/CoreAiArchitect.Core` | SSOT model, form schemas, validation, exporters |
| `src/CoreAiArchitect.App` | WPF authoring UI |
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

The WPF app still builds and runs via `dotnet` without the packaging tooling.

## Form profiles

Located in `schemas/`:

- `wpf_desktop`
- `winpe_tool`
- `web_api`
- `cli_tool`
- `powershell_module`
- `deployment_automation`
- `custom`

## Agent contract

See [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) for how coding agents must interpret exported SSOT JSON.

## Stack

- .NET 8 / WPF
- CommunityToolkit.Mvvm
- System.Text.Json
- PowerShell Standard Library (binary module)
- xUnit
