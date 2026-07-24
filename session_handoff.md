# Session handoff — Core AI Architect

**Last updated:** 2026-07-24  
**Workspace:** `d:\Robert\Desktop\App Project Builds\Core_AI_Architect`  
**Product:** Core AI Architect (App Blueprint / SSOT authoring tool for AI coding agents)

Use this file for current-state / continuity context. Operator build/run basics stay in [README.md](README.md). End-user section reference (PDF): [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf) (source: [docs/USER_GUIDE.md](docs/USER_GUIDE.md)). Agent export rules stay in [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md).

---

## What shipped this session (2026-07-24)

Beginner-friendly **App Blueprint** redesign plus earlier bugfixes from the same chat.

### Product redesign (5-step wizard)

| Change | Detail |
|---|---|
| **5 steps** | Your idea → Screens → When they click → Remember stuff? → Hand off to AI |
| **Screen-first** | Users design what people see/do; `BlueprintCompiler` derives entities/workflows |
| **Add a thing** | Plain English: Choice list, Place to type, Button, Results list (no Add Button vs Add Control) |
| **Templates** | Blank, Number/lottery picker, Simple form, List + save history (`BlueprintTemplates`) |
| **Description tips** | Keyword hints from the product description |
| **Remember** | History/Settings only when opted in (no silent feature-checkbox side effects) |
| **Terminology** | User-facing **App Blueprint**; export still `*.ssot.json` / `SsotDocument` shape |

Primary code: `MainWindow.xaml` / `MainWindowViewModel.cs`, `WizardAnswers.cs`, `BlueprintCompiler.cs`, `BlueprintTemplates.cs`, `SsotSmartBuilder.cs` (thin delegate), `FriendlyIssueMapper.cs`, `HelpCatalog.cs`.

### Bugfixes (same session)

| Fix | Detail |
|---|---|
| **Open wipe** | Opening a blueprint no longer clears Theme/Navigation via ComboBox `SelectionChanged` during list refresh |
| **Menu contrast** | File/Help submenu text is dark on light popup (top-level stays white on teal) |
| **Back→Next wipe** | Leaving Features no longer force-rebuilds an existing document (preserved in redesign: document kept once created) |

### Verify

```powershell
dotnet build CoreAiArchitect.sln
dotnet test  CoreAiArchitect.sln
```

Last check this session: build succeeded (0 warnings); **28** tests passed.

---

## Product shape (current)

Plain-language **5-step wizard**. Technical defaults (OS, language, theme, navigation, architecture, build/test) are applied automatically from the form profile.

Older section-editor / UI Layout Builder views remain in the project but are **not** the active MainWindow flow.

---

## Architecture (quick)

```text
schemas/*.json → CompatibilityEngine + BlueprintCompiler → SsotDocument
                              → Validation / Hand-off check → JSON / Markdown export
Core ← App (WPF wizard) + PowerShell module
```

Templates seed screens; compiler derives `domain` / `workflows` from controls and button steps.

---

## How to verify

```powershell
cd "d:\Robert\Desktop\App Project Builds\Core_AI_Architect"
dotnet build CoreAiArchitect.sln -c Release
dotnet test  CoreAiArchitect.sln -c Release
dotnet run --project src/CoreAiArchitect.App/CoreAiArchitect.App.csproj -c Release
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

**Current review (2026-07-24):** Build-ready for the core loop. Main screen has game picker, ticket count, Generate, results list; Generate workflow steps match. Description lists IL games and allows agent to research/cache rules. History/Settings are present (Remember opted in). Domain entities remain generic (`OptionChoice` / `ResultItem`); agent should enrich ticket/game models. Settings Save workflow steps still look copy-pasted from Save History — fix in generated app or re-edit Clicks.

### Litmus checklist (met for core)

1. One main screen: game + count + Generate + results  
2. Generate steps mention game, count, show results  
3. Description aligns with single-screen flow  
4. Export still WPF / .NET 8 / agent-compatible JSON  

---

## Known tooling notes

- Schema JSON is generated via `tools/GenerateSchemas.ps1` (avoids PowerShell single-element array collapse).
- MSIX project is **outside** `CoreAiArchitect.sln` so `dotnet build` stays portable; see `src/CoreAiArchitect.Package/README.md`.
- Git repo is initialized in this workspace.

---

## Suggested next steps

1. Optional: regenerate user-guide PDF from updated [docs/USER_GUIDE.md](docs/USER_GUIDE.md) (`python tools/GenerateUserGuidePdf.py`).
2. Hand `Core-Lottery-Generator.ssot.json` + [SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) to an agent to generate the WPF lottery app.
3. Install PowerShell 7 if module smoke-testing is needed on this machine.
4. Optional: turn off History/Settings in Remember and re-export if a thinner lottery app is preferred.

---

## Doc map

| Doc | Role |
|---|---|
| [README.md](README.md) | Product overview, build/run, profiles, module import |
| [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf) | End-user guide (PDF) — regenerate from USER_GUIDE.md after guide edits |
| [docs/USER_GUIDE.md](docs/USER_GUIDE.md) | Markdown source for the user guide PDF |
| [session_handoff.md](session_handoff.md) | This file — session continuity and open work |
| [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) | Rules for agents consuming exported SSOT / App Blueprint JSON |
| [src/CoreAiArchitect.Package/README.md](src/CoreAiArchitect.Package/README.md) | MSIX / Visual Studio packaging only |
