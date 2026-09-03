# Session handoff — Core AI Architect

**Last updated:** 2026-09-03  
**Workspace:** `d:\Robert\Desktop\App Project Builds\Core_AI_Architect`  
**Product:** Core AI Architect (App Blueprint / SSOT authoring tool for AI coding agents)

Use this file for current-state / continuity context. Operator build/run basics stay in [README.md](README.md). End-user section reference (PDF): [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf) (source: [docs/USER_GUIDE.md](docs/USER_GUIDE.md)). Agent export rules stay in [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md).

---

## What shipped this session (2026-09-03)

App icon integration and MSI installer for end-user distribution.

| Change | Detail |
|---|---|
| **App icon** | `Assets/Core-AI-Architect.ico` wired as `ApplicationIcon` in `CoreAiArchitect.App.csproj` (WPF desktop/taskbar icon). MSIX tile logos in `.wapproj` now link to `Assets/Core-AI-Architect-icon-256.png` and `Assets/Core-AI-Architect-icon1024.1024.png`. |
| **MSI installer** | New WiX v5 project `src/CoreAiArchitect.Installer/` — `dotnet build` produces a ~60 MB self-contained `.msi`. Installs to `C:\Program Files\Coresynth Studios\Core AI Architect\`, creates Desktop + Start Menu shortcuts, registers in Apps & Features. Supports upgrade/uninstall. |
| **Solution** | `CoreAiArchitect.Installer.wixproj` added to `CoreAiArchitect.sln` (excluded from default build so `dotnet build` of the solution still works without WiX). |
| **PowerShell installer scripts** | `installer/Install-CoreAiArchitect.ps1` / `Uninstall-CoreAiArchitect.ps1` / `stage_payload.ps1` — alternative script-based installer (copies files, creates shortcuts, registers ARP). |
| **.gitignore** | Added `installer/payload/` (large staged binaries). |

Primary new/updated files: `Assets/*`, `CoreAiArchitect.App.csproj`, `CoreAiArchitect.Package.wapproj`, `CoreAiArchitect.sln`, `src/CoreAiArchitect.Installer/*`, `installer/*`, `.gitignore`.

### Build the MSI

```powershell
dotnet publish src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release -p:PublishProfile=Win64-SelfContained
dotnet build src/CoreAiArchitect.Installer/CoreAiArchitect.Installer.wixproj -c Release
# Output: src/CoreAiArchitect.Installer/bin/Release/CoreAiArchitect.Installer.msi
```

Last check this session: MSI built successfully, user-tested and confirmed working.

---

## What shipped previously (2026-08-25)

Product evolution: **Ad-Lib → Needs → storyboard → Blueprint hub**, plus deterministic recommendations, `agent_discretion`, and **Agent Package** export. Same `SsotDocument` / compiler core — no full rewrite.

| Change | Detail |
|---|---|
| **Guided Ad-Lib** | Fill-in-the-blank intake (audience, helps-them, capabilities, connectivity, look) + optional product rules; composes `project.description`. Blank template seeds capability lines as buttons (`SeedAdLibFeatures`). |
| **Needs step** | Accounts / multi-device / notifications → `requirements` + description summary; notifications can add a Main-screen UI hint. |
| **Blueprint hub** | Completeness checklist (`BlueprintHubAssessor`), jump-to-section, Simple \| Standard \| Expert detail, tech summary / expert overrides. |
| **Recommendations** | `RecommendationEngine` with rule IDs + Why? + Use (storage, MVVM, Fluent, backup/export, requirement conflicts). |
| **Decision modes** | `architecture.patternDecision` and `data.storage.decision`: `specified` \| `recommended` \| `agent_discretion` (Expert UI). |
| **Agent Package** | Folder export: `project.json`, `.ssot.json`, `AGENTS.md`, `specifications/`, `tasks/`, `assets/` (`AgentPackageExporter`). |
| **Docs / help** | [USER_GUIDE.md](docs/USER_GUIDE.md), [SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md), [README.md](README.md), in-app `HelpCatalog` updated for the new flow. |

Primary new/updated code: `WizardAnswers.cs`, `BlueprintCompiler.cs`, `BlueprintHubAssessor.cs`, `RecommendationEngine.cs`, `AgentPackageExporter.cs`, `SsotDocument` (+ normalizer), `MainWindow.xaml` / `MainWindowViewModel.cs`, `DialogService` (folder pick), `SsotDocumentService`.

### Verify

```powershell
dotnet build CoreAiArchitect.sln -c Release
dotnet test  CoreAiArchitect.sln -c Release
```

Last check this session: build succeeded (0 warnings); **35** tests passed.

---

## What shipped previously (2026-08-14)

Self-contained **Windows x64 exe** so Core AI Architect can be opened without `dotnet run`.

| Change | Detail |
|---|---|
| **Publish profile** | `src/CoreAiArchitect.App/Properties/PublishProfiles/Win64-SelfContained.pubxml` — Release, `win-x64`, self-contained, ReadyToRun |
| **Output** | `src/CoreAiArchitect.App/bin/publish/win-x64/CoreAiArchitect.exe` (keep the whole folder; includes `schemas\`) |
| **Desktop shortcut** | `%USERPROFILE%\Desktop\Core AI Architect.lnk` → that exe, working directory = publish folder |

```powershell
dotnet publish src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release -p:PublishProfile=Win64-SelfContained
```

---

## What shipped previously (2026-07-24)

Beginner-friendly **App Blueprint** redesign (screen-first storyboard) plus earlier bugfixes.

| Change | Detail |
|---|---|
| **Storyboard steps** | Screens → When they click → Remember (still core of the experience-first contract) |
| **Add a thing** | Choice list, Place to type, Button, Results list |
| **Templates** | Blank, Number/lottery picker, Simple form, List + save history |
| **Terminology** | User-facing **App Blueprint**; export still `*.ssot.json` |

Bugfixes from that session: Open wipe, menu contrast, Back→Next document preserve.

---

## Product shape (current)

**Wizard flow:** Ad-Lib → Needs → Screens → Clicks → Remember → Project Blueprint hub.

- Novices stay in plain language; Expert detail exposes stack + decision modes.
- Technical defaults (OS, language, theme, navigation, architecture, build/test) still come from the form profile + rules.
- Older section-editor / UI Layout Builder views remain in the project but are **not** the active MainWindow flow.

---

## Architecture (quick)

```text
schemas/*.json
    → CompatibilityEngine + RecommendationEngine + BlueprintCompiler
    → SsotDocument (incl. requirements + decision objects)
    → SsotReadinessEngine / BlueprintHubAssessor
    → JSON | Markdown | Agent Package folder
Core ← App (WPF wizard) + PowerShell module
```

Templates / Ad-Lib seed screens; compiler derives `domain` / `workflows` from controls and button steps. Domain scaffolds stay generic on purpose (`DomainScaffoldPolicy`).

---

## How to verify

```powershell
cd "d:\Robert\Desktop\App Project Builds\Core_AI_Architect"
dotnet build CoreAiArchitect.sln -c Release
dotnet test  CoreAiArchitect.sln -c Release
dotnet run --project src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release
# Or open the published exe / Desktop shortcut (see README)
```

PowerShell module (requires **PowerShell 7+** / `pwsh`):

```powershell
Import-Module ".\src\CoreAiArchitect.PowerShell\bin\Release\net8.0\CoreAiArchitect.psd1"
Get-SsotFormProfile
```

This machine may only have Windows PowerShell 5.1 — module import will fail there by design (`PowerShellVersion = 7.0`).

---

## User trial — lottery App Blueprint

File at repo root (user-authored, **not** a product sample):

- `Core-Lottery-Generator.ssot.json`

**Review (2026-07-24; still valid for core loop):** Build-ready for the core loop. Main screen has game picker, ticket count, Generate, results list; Generate workflow steps match. Description lists IL games and allows agent to research/cache rules. History/Settings are present (Remember opted in). Domain entities remain generic (`OptionChoice` / `ResultItem`); agent should enrich ticket/game models. Settings Save workflow steps still look copy-pasted from Save History — fix in generated app or re-edit Clicks.

**Note:** Re-open/re-export after this session if you want `requirements` / decision-mode fields populated by the new wizard (older JSON still loads; missing sections normalize to defaults).

### Litmus checklist (met for core)

1. One main screen: game + count + Generate + results  
2. Generate steps mention game, count, show results  
3. Description aligns with single-screen flow  
4. Export still WPF / .NET 8 / agent-compatible JSON  

---

## Known tooling notes

- Schema JSON is generated via `tools/GenerateSchemas.ps1` (avoids PowerShell single-element array collapse).
- **MSI installer** (`src/CoreAiArchitect.Installer/`) is the recommended distribution format. Requires WiX v5 CLI: `dotnet tool install --global wix`. The `.wixproj` is in the solution but excluded from default build.
- MSIX project (`src/CoreAiArchitect.Package/`) is **outside** the default solution build; see `src/CoreAiArchitect.Package/README.md`.
- `installer/` folder contains PowerShell-based install/uninstall scripts as an alternative to the MSI. `installer/payload/` is gitignored (large binaries — regenerate via `stage_payload.ps1`).
- App icon assets live in `Assets/` — `.ico` for the WPF exe, PNGs for MSIX tiles.
- Everyday launch path is the self-contained folder publish (`Win64-SelfContained`), the MSI install, or `dotnet run`. Do not copy `CoreAiArchitect.exe` out of its folder by itself.
- Git repo is initialized in this workspace.
- User-guide **PDF** may be missing or stale relative to [docs/USER_GUIDE.md](docs/USER_GUIDE.md) — regenerate when needed.

---

## Suggested next steps

1. Regenerate user-guide PDF from [docs/USER_GUIDE.md](docs/USER_GUIDE.md) (`python tools/GenerateUserGuidePdf.py`).
2. Code-sign the MSI with an Authenticode certificate for trusted distribution.
3. Optionally re-author lottery blueprint through Ad-Lib → Needs → hub, then export Agent Package for a coding agent.
4. Install PowerShell 7 if module smoke-testing is needed on this machine.

---

## Doc map

| Doc | Role |
|---|---|
| [README.md](README.md) | Product overview, build/run, self-contained exe publish, profiles, module import |
| [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf) | End-user guide (PDF) — regenerate from USER_GUIDE.md after guide edits |
| [docs/USER_GUIDE.md](docs/USER_GUIDE.md) | Markdown source for the user guide PDF |
| [session_handoff.md](session_handoff.md) | This file — session continuity and open work |
| [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) | Rules for AI agents consuming exported SSOT / App Blueprint JSON / Agent Package |
| [installer/README.md](installer/README.md) | PowerShell-based installer scripts (alternative to MSI) |
| [src/CoreAiArchitect.Package/README.md](src/CoreAiArchitect.Package/README.md) | MSIX / Visual Studio packaging only |
