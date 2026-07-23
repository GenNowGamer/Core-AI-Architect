# Core AI Architect — User Guide

**Preferred reading format:** [Core_AI_Architect_User_Guide.pdf](Core_AI_Architect_User_Guide.pdf) (print-friendly layout).

This Markdown file is the editable source. Regenerate the PDF with:

```powershell
python tools/GenerateUserGuidePdf.py
```

This guide explains how to use Core AI Architect to define an application so an AI coding agent can build it from your exported SSOT (single source of truth) JSON.

For build/run commands, see [README.md](../README.md).  
For agent consumption rules, see [SSOT_CONTRACT.md](SSOT_CONTRACT.md).

---

## 1. What this tool is (and is not)

**Core AI Architect does not write your app’s code.**  
It helps you **author a structured blueprint** (the SSOT JSON). You (or an AI agent) then use that blueprint to generate the real solution.

| This tool does | This tool does not |
|---|---|
| Collect project choices in guided sections | Compile or publish the target app |
| Validate required fields | Invent screens/entities if you left them empty |
| Export JSON SSOT + Markdown docs | Replace Visual Studio / your coding agent |

**Important:** Empty lists (screens, entities, workflows, modules, etc.) mean “intentionally blank.” A careful agent should **not** invent those for you. Fill the sections that matter for the app you want built.

---

## 2. Typical workflow

1. **File → New** — pick a **form profile** (e.g. WPF Desktop App).
2. Walk the **left section list** (Project → … → Documentation).
3. Use **Previous / Next** or click a section name.
4. **Tools → Validate** when you think you’re done.
5. **File → Save** (keeps your SSOT) and/or **Export JSON** / **Export Markdown**.
6. Hand the `.ssot.json` (plus [SSOT_CONTRACT.md](SSOT_CONTRACT.md)) to your coding agent.

### Form profiles

Profiles prefill environment defaults and tune available enums. They do not create demo content.

| Profile | Best for |
|---|---|
| WPF Desktop App | Windows desktop GUI (WPF) |
| WinPE Tool | WinPE / recovery-style tools |
| Web API | ASP.NET APIs |
| CLI Tool | Console commands |
| PowerShell Module | Cmdlets / modules |
| Deployment Automation | Pipelines / orchestration |
| Custom | Broadest option sets (blank canvas) |

---

## 3. Menu reference

| Menu | Action | Purpose |
|---|---|---|
| File → New | Start a new SSOT with a profile | Creates empty document + profile defaults |
| File → Open | Open an existing `.json` SSOT | Continue editing |
| File → Save / Save As | Persist the SSOT | Your working source of truth file |
| File → Export JSON | Write SSOT JSON | Handoff file for agents |
| File → Export Markdown | Write human-readable docs | Overview from filled sections only |
| Tools → Validate | Check required fields & warnings | Catch missing Project Name, bad triggers, etc. |
| Tools → Copy PowerShell Command | Clipboard helper | For PowerShell 7+ module workflows |
| Help | Schemas path + module hint | Orientation |

---

## 4. Section-by-section reference

Each section below maps to part of the exported SSOT and tells a coding agent **what to build**.

---

### 4.1 Project

**What it contributes:** Identity and intent for the whole app (names, ownership, summary).

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Project Id** | Yes | Machine-friendly key (prefer kebab-case, no spaces), e.g. `core-lottery-generator` | Used for filenames, package identity hints, stable references |
| **Project Name** | Yes | Human-readable title, e.g. `Core Lottery Generator` | Window titles, README headings, branding text |
| **Description** | Yes | Plain-language purpose and scope | Primary narrative the agent uses to understand *what* to build |
| **Owner** | No | Person, team, or company | Metadata in docs (optional) |
| **Tags** | No | Short labels (add one at a time) | Categorization / search metadata in docs |

**Tip:** Put *why* and *scope* in Description. Put *structured rules* (entities, screens, workflows) in later sections—not only in the description paragraph.

---

### 4.2 Environment

**What it contributes:** Platform stack the generated solution must use.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Target OS** | Yes | Where the app runs (e.g. Windows 11) | OS assumptions, APIs, packaging |
| **Runtime** | Yes | Execution stack (e.g. .NET 8) | SDK / target framework |
| **UI Framework** | Yes | Presentation tech (WPF, WinUI, Console, ASP.NET, …) | Project type and UI libraries |
| **Language** | Yes | Primary language (C#, PowerShell, F#) | Language of generated code |

**Tip:** Keep UI Framework consistent with later **UI → Navigation Pattern** (e.g. WPF + `Window+Dialogs`).

---

### 4.3 Architecture

**What it contributes:** How code is organized (pattern, layers, feature modules).

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Pattern** | Yes | Overall design style (MVVM, MVC, Clean Architecture, …) | Folder layout, ViewModels vs controllers, dependency direction |
| **Layers** | No | Named layers as a string list, e.g. `UI`, `Domain`, `Infrastructure` | Instructs the agent to keep those layers separate |
| **Modules** (Add Item) | No | Feature areas of the product | Splits the solution into logical modules |

**Each module item:**

| Field | Meaning | Effect |
|---|---|---|
| **Name** | Short module name (e.g. `Drawing`) | Module / folder / project naming |
| **Description** | What that area does | Scope for that module’s code |
| **Dependencies** | Other module names it needs | Build order and references between modules |

**Tip:** Modules are *feature areas*, not NuGet package names. Leave empty only if the app is tiny and you accept a single undifferentiated codebase.

---

### 4.4 UI / Screens and buttons

**What it contributes:** Windows/screens, controls, navigation, and look—so the agent knows what to put on screen.

In the **guided wizard**, this is the **Screens and buttons** step. Theme and navigation pattern are chosen earlier (Look and movement). Screens and controls are reviewed and edited here.

| Action | What it does |
|---|---|
| **Add Screen** | Adds a new screen to the blueprint |
| **Remove** (screens) | Deletes the selected screen |
| **Move Up / Move Down** | Reorders screens (order is saved in the SSOT) |
| **Add Button** | Adds a clickable action control (usually type `Button`) |
| **Add Control** | Adds an input/picker control (e.g. `ComboBox`, `TextBox`) |
| **Remove** (controls) | Deletes the selected control on the current screen |

The controls list shows **Label (Type)** so you can tell buttons from combo boxes. You can edit the selected control’s **label**, **type** (from profile-allowed values), and **id** (ids are what workflows use as triggers).

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Theme** | Yes | Visual style (Default, Fluent, Custom, …) | Styling approach |
| **Navigation Pattern** | Yes | How users move between surfaces | Shell: dialogs, tabs, frame, routes, etc. |
| **Screens** | Strongly recommended | Each window/page/surface | Creates those views |
| **Controls** (per screen) | Strongly recommended | Buttons, lists, inputs, etc. | Concrete UI elements and actions |

**Each screen:**

| Field | Meaning | Effect |
|---|---|---|
| **Id** | Stable screen key (e.g. `main`) | Code identifiers, navigation targets |
| **Title** | User-visible title | Window/page header |
| **Route** | Path or route hint (e.g. `/`) | Navigation routing when applicable |

**Each control:**

| Field | Meaning | Effect |
|---|---|---|
| **Id** | Stable control key (e.g. `generateButton`) | Bindings; workflow **Trigger** should match this |
| **Type** | Control kind (Button, TextBox, ComboBox, …) | Which UI widget to create |
| **Label** | Visible text | Button/caption text |
| **Action** | Named behavior (e.g. `GenerateNumbers`) | Maps click/command to a workflow or command |

**Tip:** If Screens is empty, the agent has no UI contract—only your Description prose. Use **Add Button** for actions and **Add Control** for lists/text boxes (e.g. a lottery game picker).

---

### 4.5 Domain

**What it contributes:** Data model—the “things” the app knows about.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Entities** (Add Item) | Strongly recommended | Business objects (e.g. `LotteryGame`, `DrawResult`) | Classes / records / tables |

**Each entity:**

| Field | Meaning | Effect |
|---|---|---|
| **Name** | Type name | Class / table name |
| **Description** | What it represents | Modeling guidance |
| **Fields** | Properties of the entity | Properties with types and required flags |

**Each entity field:**

| Field | Meaning | Effect |
|---|---|---|
| **Name** | Property name | Code property name |
| **Type** | `string`, `int`, `bool`, `decimal`, `DateTime`, `Guid` | Property / column type |
| **Required** | Must have a value | Validation / non-nullability |

**Tip:** Encode product rules here when possible (e.g. game name, ball count, min/max). Don’t rely only on the Project Description.

---

### 4.6 Workflows (Workflow Builder)

**What it contributes:** User/system processes—what happens when someone clicks Generate, Save, etc.

Uses a dedicated **Workflow Builder**.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Workflows** (Add) | Strongly recommended | Named processes | Commands / use-case methods |

**Each workflow:**

| Field | Meaning | Effect |
|---|---|---|
| **Id** | Stable key (e.g. `generateNumbers`) | Code identifier |
| **Name** | Display name | Docs and UI labels |
| **Trigger** | What starts it (prefer a **control Id** from UI) | Wires UI action → workflow |
| **Steps** | Ordered plain-language steps | Implementation checklist for the agent |
| **Error Strategy** | How failures are handled | MessageBox vs throw vs retry, etc. |
| **Log Level** | Severity for logging on errors | Logging behavior |

**Tip:** Fill UI controls first, then set Trigger to those control Ids so validation can warn on mismatches.

---

### 4.7 Constraints

**What it contributes:** Hard rules the agent must obey (libraries, logging, telemetry).

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Allowed Libraries** | No | Preferred packages (e.g. `CommunityToolkit.Mvvm`) | Agent should prefer these when needed |
| **Forbidden Libraries** | No | Packages that must not be used | Agent must not add them |
| **Logging Required** | Yes | Whether logging must exist | Adds logging if true |
| **Telemetry Allowed** | Yes | Analytics/telemetry permission | No telemetry if false |
| **Third-Party Bootloaders Allowed** | Yes | Policy flag for bootloader-style third parties | Agent must respect this policy |

---

### 4.8 Data

**What it contributes:** How (and if) the app persists data.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Storage Type** | Yes | `None`, `LocalJson`, `SQLite`, `SQLServer`, … | Persistence technology |
| **Connection String Mode** | Yes | Where connection/path config lives | Config file vs env vs secrets vs none |
| **Migrations Required** | Yes | Whether DB schema migrations are needed | Migration projects/tooling if true |

**Tips:**
- Local file history/settings → often **LocalJson** + connection mode **None**.
- Real database → SQLite/SQLServer + ConfigFile or UserSecrets as appropriate.

---

### 4.9 Build

**What it contributes:** How the solution is compiled and for which platforms.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Output Type** | Yes | `WinExe` (GUI) vs `Exe` (console/host) | Project output type |
| **Configurations** | Yes | Usually `Debug` and `Release` | Build configurations |
| **Targets** (Add Item) | Recommended | Named publish targets | RIDs / publish profiles |

**Each target:**

| Field | Meaning | Effect |
|---|---|---|
| **Name** | Label (e.g. `Desktop`) | Human name for the target |
| **Runtime Identifier** | e.g. `win-x64` | Publish RID |

---

### 4.10 Deployment

**What it contributes:** How users receive the finished app.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Packaging** | Yes | `None`, `MSIX`, `Zip`, … | Packaging approach |
| **Auto Update** | Yes | Whether auto-update is in scope | Update mechanism (or none) |

---

### 4.11 Testing

**What it contributes:** Expected automated test approach for the *generated* app.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Unit Test Framework** | Yes | xUnit, NUnit, MSTest, Pester, None | Test project type |
| **UI Test Framework** | Yes | None, FlaUI, WinAppDriver, Playwright | UI automation stack |
| **Coverage Target** | Yes | Number from `0` to `1` (e.g. `0.6` ≈ 60%) | Coverage goal for agents/CI |

**Tip:** For desktop WPF/WinUI, prefer **FlaUI** or **None**—not Playwright (web-oriented)—unless you intentionally have a web UI.

---

### 4.12 Documentation

**What it contributes:** Which docs the agent (or Markdown export) should produce.

| Option | Required? | What it means | Effect |
|---|---|---|---|
| **Generate README** | Yes | Want a README | Agent/export includes overview-style README content |
| **Generate Architecture Doc** | Yes | Want architecture documentation | Architecture write-up when data exists |
| **Sections** | No | Named doc sections (Overview, Architecture, Workflows, Entities, …) | Which topics appear when you have filled data |

Empty product sections are **omitted** from Markdown export (no filler lorem ipsum).

---

## 5. Recommended fill order (for a complete handoff)

1. **Project** — Id, Name, Description  
2. **Environment** — OS, Runtime, UI Framework, Language  
3. **Architecture** — Pattern; add Layers/Modules if useful  
4. **UI** — Theme, navigation, screens, controls  
5. **Domain** — Entities and fields (rules as data)  
6. **Workflows** — Processes tied to control Ids  
7. **Constraints** — Libraries, logging, telemetry  
8. **Data** — Storage choices  
9. **Build** — Output type + at least one Target (`win-x64`)  
10. **Deployment** — Packaging  
11. **Testing** — Frameworks + coverage  
12. **Documentation** — Flags/sections  
13. **Validate → Save → Export JSON**

---

## 6. Completeness checklist

Before handing SSOT to an agent, ask:

- [ ] Project Id / Name / Description filled  
- [ ] Environment matches the kind of app you want  
- [ ] At least one **screen** with meaningful **controls** (for GUI apps)  
- [ ] Core **entities** defined (not only described in prose)  
- [ ] Key **workflows** with steps and triggers  
- [ ] Data/storage choice matches whether you need history/settings  
- [ ] Build **target** RID set (e.g. `win-x64`)  
- [ ] UI test framework makes sense for desktop vs web  
- [ ] **Validate** reports no unexpected errors  

---

## 7. After export — using the SSOT

1. Keep your saved `.ssot.json` as the contract.  
2. Point the coding agent at that file and [SSOT_CONTRACT.md](SSOT_CONTRACT.md).  
3. Expect the agent to follow environment, UI, domain, workflows, and constraints strictly.  
4. If something was left empty, either fill it in Core AI Architect and re-export, or explicitly tell the agent it may propose those parts.

---

## 8. Related docs

| Doc | Role |
|---|---|
| [README.md](../README.md) | Install, build, run, PowerShell module |
| [SSOT_CONTRACT.md](SSOT_CONTRACT.md) | Rules for AI agents consuming the JSON |
| [session_handoff.md](../session_handoff.md) | Developer session continuity (not end-user) |
