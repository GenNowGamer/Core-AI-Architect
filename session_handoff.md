# Session handoff — Core AI Architect

**Last updated:** 2026-07-23  
**Workspace:** `d:\Robert\Desktop\App Project Builds\Core_AI_Architect`  
**Product:** Core AI Architect (SSOT authoring tool for AI coding agents)

Use this file for current-state / continuity context. Operator build/run basics stay in [README.md](README.md). End-user section reference (PDF): [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf) (source: [docs/USER_GUIDE.md](docs/USER_GUIDE.md)). Agent export rules stay in [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md).

---

## What shipped this session (2026-07-23)

Guided-wizard **Screens and buttons** step (Step 7) editing and layout fixes.

| Change | Detail |
|---|---|
| **Add Screen** | Creates a new screen on the live SSOT `ui.screens` list (mirrored in `ReviewScreens`) |
| **Add Button** / **Add Control** | Adds a Button-like control vs an input control (ComboBox/TextBox/… per profile enums) on the selected screen |
| **Remove** | Remove selected screen or selected control |
| **Move Up / Move Down** | Reorders screens in both the list UI and `Document.Ui.Screens` |
| **Control type** | ComboBox of profile-allowed types; list shows `Label (Type)` |
| **Layout** | Two-row screen toolbar (no clipped Up/Down); label + type side-by-side; lists/fields grid so control editors are not clipped |

Primary code: `MainWindow.xaml` (ReviewScreens), `MainWindowViewModel.cs` (`AddScreen`, `AddButton`, `AddControl`, `MoveScreenUp`/`Down`, `ReviewScreens`/`ReviewControls` observable collections).

**Verify:** `dotnet build CoreAiArchitect.sln -c Release` (succeeded this session). Wizard-related tests: `dotnet test … --filter "FullyQualifiedName~WizardBuilderTests"` (7 passed).

---

## Product shape (current)

The app shell is a **plain-language guided wizard** (app kind → name → platform → look → memory → features → review screens/data/flows → ready). Smart templates (`SsotSmartBuilder`) pre-populate screens/entities/workflows from feature checkboxes; the review steps let you refine them.

Older section-editor / UI Layout Builder views remain in the project but are **not** the active MainWindow flow.

---

## Architecture (quick)

```text
schemas/*.json → CompatibilityEngine + SsotSmartBuilder → SsotDocument
                              → Validation / Ready check → JSON / Markdown export
Core ← App (WPF wizard) + PowerShell module
```

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

## User trial — lottery SSOT (open)

File at repo root (user-authored, **not** a product sample):

- `Core-Lottery-Generator.ssot.json`

**Earlier review:** rich lottery intent lived mainly in **Description**; structured `ui` / `domain` / `workflows` were generic or incomplete. Empty collections still mean “do not invent” per [SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md).

**This session:** Users can now **add/reorder screens** and **add buttons vs controls** (and set control type) on wizard Step 7 — so lottery structure can be authored in-app without the old advanced UI Layout Builder path.

### Gaps still typically needing attention

| Gap | Where to fill |
|---|---|
| Screen order / titles / ComboBox vs Button | Wizard → **Screens and buttons** |
| Domain / game rules as structure | Wizard → **Information it stores** (and Description for overview) |
| Workflows / triggers = control ids | Wizard → **What happens when someone clicks** |
| Layers / modules / build RID / test framework | Still limited in friendly wizard — may need export + manual SSOT edit or future steps |

---

## Known tooling notes

- Schema JSON is generated via `tools/GenerateSchemas.ps1` (avoids PowerShell single-element array collapse).
- MSIX project is **outside** `CoreAiArchitect.sln` so `dotnet build` stays portable; see `src/CoreAiArchitect.Package/README.md`.
- No git repository initialized in this workspace as of this handoff.

---

## Suggested next steps

1. User opens lottery SSOT (or rebuilds via wizard features) and uses **Screens and buttons** to match intended screens/controls; wire flows on the workflows step.
2. Optional: bring USER_GUIDE “typical workflow” fully in line with the 10-step wizard (partially updated for Screens this session).
3. Optional: init git when ready to version the product.
4. Install PowerShell 7 if module smoke-testing is needed on this machine.
5. When SSOT is complete, hand `*.ssot.json` + [SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) to an agent to generate the app.

---

## Doc map

| Doc | Role |
|---|---|
| [README.md](README.md) | Product overview, build/run, profiles, module import |
| [docs/Core_AI_Architect_User_Guide.pdf](docs/Core_AI_Architect_User_Guide.pdf) | End-user guide (PDF) — regenerate from USER_GUIDE.md after guide edits |
| [docs/USER_GUIDE.md](docs/USER_GUIDE.md) | Markdown source for the user guide PDF |
| [session_handoff.md](session_handoff.md) | This file — session continuity and open work |
| [docs/SSOT_CONTRACT.md](docs/SSOT_CONTRACT.md) | Rules for agents consuming exported SSOT |
| [src/CoreAiArchitect.Package/README.md](src/CoreAiArchitect.Package/README.md) | MSIX / Visual Studio packaging only |
