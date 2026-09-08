# Core AI Architect — User Guide

**Preferred reading format:** [Core_AI_Architect_User_Guide.pdf](Core_AI_Architect_User_Guide.pdf) (print-friendly layout).

This Markdown file is the editable source. Regenerate the PDF with:

```powershell
python tools/GenerateUserGuidePdf.py
```

This guide explains how to use Core AI Architect to define an application so an AI coding agent can build it from your exported **App Blueprint** JSON (internally an SSOT — single source of truth).

For build/run commands, see [README.md](../README.md).  
For agent consumption rules, see [SSOT_CONTRACT.md](SSOT_CONTRACT.md).

---

## 1. What this tool is (and is not)

**Core AI Architect does not write your app’s code.**  
It helps you **describe what people see and do**. It compiles that into a structured **App Blueprint** (`.ssot.json`). You (or an AI agent) then use that file to generate the real solution.

| This tool does | This tool does not |
|---|---|
| Guide you through a plain-language Ad-Lib, Needs, and screen storyboard | Compile or publish the target app |
| Let you design screens with everyday “things” | Require you to manage themes, domain entities, or build RIDs by hand |
| Check readiness, show recommendations (with Why?), and export JSON / Markdown / Agent Package | Invent screens if you left them empty |
| | Replace Visual Studio / your coding agent |

**Important:** Empty lists (screens, entities, workflows, modules, etc.) in the JSON mean “intentionally blank.” A careful agent should **not** invent those for you. Fill Screens and Clicks for anything you want built. That is different from `agent_discretion` on architecture/storage, which means the agent **should** choose intentionally (see [SSOT_CONTRACT.md](SSOT_CONTRACT.md)).

---

## 2. Typical workflow

1. **File → New app blueprint** (or start from the first wizard step).
2. **Ad-Lib** — pick app type, fill in the blanks (name, who it’s for, what it helps them do, main capabilities, online/offline, look). Optionally add **product rules** and a **template**. On a blank start, listed capabilities become starter buttons.
3. **Needs** — accounts, multi-device sync, and notifications (plain language; “I’m not sure” is allowed).
4. **Screens** — add screens; on each screen **Add a thing**: Choice list, Place to type, Button, or Results list. Rename labels to match your app.
5. **When they click** — for each button, write plain-English steps (one per line).
6. **Remember stuff?** — optionally turn on History and/or Settings (off by default for simple apps).
7. **Project Blueprint** — review recommendations (with **Why?**), completeness, and detail level (Simple / Standard / Expert), **Check again**, then **Save** / **Export for AI agent** / **Export Agent Package**.
8. Hand the `.ssot.json` or Agent Package folder (plus [SSOT_CONTRACT.md](SSOT_CONTRACT.md)) to your coding agent.

Theme, navigation, architecture, storage details, and build targets are filled automatically unless you change them in Expert detail or by editing the JSON later. On Ad-Lib, **Look** (Modern / Professional / Simple) maps to the profile’s theme (for example Fluent on Windows, Material3 on Android). The Theme dropdown itself only appears when Blueprint hub **Detail level** is **Expert**. Expert detail also lets you set architecture/storage to **Recommended**, **Let coding agent decide** (`agent_discretion`), or **Choose myself**.

### App types (wizard cards)

| Card | Profile behind it | Best for |
|---|---|---|
| Windows desktop app | `wpf_desktop` | Windows GUI (WPF) |
| Android app | `android_app` | Phone/tablet apps (Jetpack Compose / Kotlin by default) |
| Website / API | `web_api` | ASP.NET APIs |
| Something else | `custom` | Flexible defaults |

Additional profiles (`cli_tool`, `powershell_module`, etc.) remain available via schemas / PowerShell module.

---

## 3. Menu reference

| Menu | Action | Purpose |
|---|---|---|
| File → New app blueprint | Start fresh | Clears the wizard; begins at Ad-Lib |
| File → Open | Open an existing App Blueprint | Continue editing (usually lands on Screens) |
| File → Save / Save As | Persist the blueprint | Working `.ssot.json` file |
| File → Export for AI agent | Write blueprint JSON | Handoff file for agents |
| File → Export Agent Package | Write a folder for coding agents | `project.json`, `AGENTS.md`, specs, tasks |
| File → Export Markdown | Write human-readable docs | Overview from filled sections |
| Help → How this works | Short orientation | Ad-Lib → Blueprint hub overview |

---

## 4. What ends up in the exported JSON

The sections below describe the **exported App Blueprint** shape. In the wizard you mostly edit **Ad-Lib**, **Needs**, **Screens**, **Clicks**, and **Remember**; the compiler fills domain entities, workflows, architecture, build, and related fields for you. The **Project Blueprint** hub shows completeness, recommendations, and (at Standard/Expert detail) the technology summary and decision-mode overrides.

---

### 4.1 Project

**What it contributes:** Identity and intent for the whole app (names, ownership, summary).

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Project Id** | Yes | Machine-friendly key (prefer kebab-case, no spaces), e.g. `core-lottery-generator` | Used for filenames, package identity hints, stable references |
| **Project Name** | Yes | Human-readable title, e.g. `Core Lottery Generator` | Window titles, README headings, branding text |
| **Description** | Yes | Plain-language purpose and scope | Primary narrative the agent uses to understand *what* to build |
| **Owner** | No | Person, team, or company | Metadata in docs (optional) |
| **Tags** | No | Short labels | Categorization / search metadata in docs |

**Tip:** Put *why* and *scope* in Description. Put *what people see* on Screens and *what happens on click* in Clicks—not only in the description paragraph.

---

### 4.2 Environment

**What it contributes:** Platform stack the generated solution must use (usually auto-filled from app type).

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Target OS** | Yes | Where the app runs (e.g. Windows 11, Android) | OS assumptions, APIs, packaging |
| **Runtime** | Yes | Execution stack (e.g. .NET 8, Android Runtime) | SDK / target framework |
| **UI Framework** | Yes | Presentation tech (WPF, Jetpack Compose, Console, ASP.NET, …) | Project type and UI libraries |
| **Language** | Yes | Primary language (C#, Kotlin, PowerShell, …) | Language of generated code |

---

### 4.3 Architecture

**What it contributes:** How code is organized (pattern, layers, feature modules). Usually auto-filled.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Pattern** | Yes* | Overall design style (MVVM, MVC, Clean Architecture, …) | Folder layout, ViewModels vs controllers, dependency direction |
| **Pattern decision** | No | `specified` / `recommended` / `agent_discretion` | How strongly the agent must keep that pattern (Expert detail) |
| **Layers** | No | Named layers as a string list, e.g. `UI`, `Domain`, `Infrastructure` | Instructs the agent to keep those layers separate |
| **Modules** (Add Item) | No | Feature areas of the product | Splits the solution into logical modules |

\*If pattern decision is `agent_discretion`, the agent chooses a pattern that fits `guidance` and the rest of the blueprint.

**Each module item:**

| Field | Meaning | Effect |
|---|---|---|
| **Name** | Short module name (e.g. `Drawing`) | Module / folder / project naming |
| **Description** | What that area does | Scope for that module’s code |
| **Dependencies** | Other module names it needs | Build order and references between modules |

---

### 4.4 UI / Screens

**What it contributes:** Windows/screens, controls, navigation, and look—so the agent knows what to put on screen.

In the wizard this is the **Screens** step. Theme and navigation are chosen automatically for your app type.

| Action | What it does |
|---|---|
| **Add Screen** | Adds a new screen to the blueprint |
| **Remove** (screens) | Deletes the selected screen (History/Settings are controlled on Remember) |
| **Move Up / Move Down** | Reorders screens |
| **Choice list** | People pick one option (e.g. lottery game) |
| **Place to type** | Number or text (e.g. ticket count) |
| **Button** | Starts work (e.g. Generate) |
| **Results list** | Shows generated or saved items |
| **Remove** (things) | Deletes the selected control on the current screen |

Rename the **Name people will see** so labels match your product. Internal ids are advanced; workflows bind to them automatically.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Theme** | Yes | Visual style (Fluent / Material3 / Default / Custom, by profile) | Styling approach — usually from Ad-Lib **Look**; override at Expert detail |
| **Navigation Pattern** | Yes | How users move between surfaces | Shell pattern (auto) |
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

**Tip:** If Screens is empty, the agent has no UI contract—only your Description prose. Use a template (e.g. lottery picker) or add things manually.

---

### 4.5 Domain

**What it contributes:** Data model—the “things” the app knows about. Usually **derived** from your screens (choice list → options, results → result items, etc.). Enrich in JSON later if you need lottery-specific fields.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Entities** | Strongly recommended | Business objects | Classes / records / tables |

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

**Tip:** Encode product rules in Description and Clicks when possible. Exported domain entities are often **scaffolds** — coding agents should invent richer types from that prose (see [SSOT_CONTRACT.md](SSOT_CONTRACT.md)).

---

### 4.6 Workflows (When they click)

**What it contributes:** What happens when someone uses a button.

In the wizard this is **When they click** — pick a button by label and edit steps (one per line). Triggers are bound to control ids automatically.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Workflows** | Strongly recommended | Named processes | Commands / use-case methods |

**Each workflow:**

| Field | Meaning | Effect |
|---|---|---|
| **Id** | Stable key | Code identifier |
| **Name** | Display name | Docs and UI labels |
| **Trigger** | Starting control **Id** | Wires UI action → workflow |
| **Steps** | Ordered plain-language steps | Implementation checklist for the agent |
| **Error Strategy** | How failures are handled | MessageBox vs throw vs retry, etc. |
| **Log Level** | Severity for logging on errors | Logging behavior |

**Tip:** Fill Screens (with buttons) first, then edit Clicks steps.

---

### 4.7 Constraints

**What it contributes:** Hard rules the agent must obey (libraries, logging, telemetry). Usually auto-filled.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Allowed Libraries** | No | Preferred packages (e.g. `CommunityToolkit.Mvvm`) | Agent should prefer these when needed |
| **Forbidden Libraries** | No | Packages that must not be used | Agent must not add them |
| **Logging Required** | Yes | Whether logging must exist | Adds logging if true |
| **Telemetry Allowed** | Yes | Analytics/telemetry permission | No telemetry if false |
| **Third-Party Bootloaders Allowed** | Yes | Policy flag for bootloader-style third parties | Agent must respect this policy |

---

### 4.8 Requirements (Needs step)

**What it contributes:** Human-language product needs from the **Needs** wizard step (not the screen storyboard).

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Accounts** | Yes | `none` / `separate` / `unsure` | Whether identity/login is in scope |
| **Multi-device** | Yes | `no` / `yes` / `later` | Sync / cloud expectations |
| **Notifications** | Yes | On/off + optional notes | Reminder/notification features; may add a UI contract hint |

Also summarized into `project.description` so agents reading the narrative see the same intent.

---

### 4.9 Data

**What it contributes:** How (and if) the app persists data. Driven largely by **Remember** (Save past results → LocalJson) and recommendations.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Storage Type** | Yes* | `None`, `LocalJson`, `SQLite`, `SQLServer`, … | Persistence technology |
| **Storage decision** | No | `specified` / `recommended` / `agent_discretion` | How strongly the agent must keep that storage choice (Expert detail) |
| **Connection String Mode** | Yes | Where connection/path config lives | Config file vs env vs secrets vs none |
| **Migrations Required** | Yes | Whether DB schema migrations are needed | Migration projects/tooling if true |

\*If storage decision is `agent_discretion`, the agent chooses persistence that fits `guidance` and connectivity/accounts needs.

**Tips:**
- Local file history/settings → often **LocalJson** + connection mode **None**.
- Leave Remember off for a simple generate-only app.
- Blueprint recommendations may suggest SQLite or Backup/Export when local data is present.

---

### 4.10 Build

**What it contributes:** How the solution is compiled and for which platforms. Usually auto-filled (`WinExe`, `win-x64`).

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

### 4.11 Deployment

**What it contributes:** How users receive the finished app.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Packaging** | Yes | `None`, `MSIX`, `Zip`, … | Packaging approach |
| **Auto Update** | Yes | Whether auto-update is in scope | Update mechanism (or none) |

---

### 4.12 Testing

**What it contributes:** Expected automated test approach for the *generated* app.

| Option | Required? | What it means | Effect on the built app |
|---|---|---|---|
| **Unit Test Framework** | Yes | xUnit, NUnit, MSTest, Pester, None | Test project type |
| **UI Test Framework** | Yes | None, FlaUI, WinAppDriver, Playwright | UI automation stack |
| **Coverage Target** | Yes | Number from `0` to `1` (e.g. `0.6` ≈ 60%) | Coverage goal for agents/CI |

**Tip:** For desktop WPF/WinUI, prefer **FlaUI** or **None**—not Playwright (web-oriented)—unless you intentionally have a web UI.

---

### 4.13 Documentation

**What it contributes:** Which docs the agent (or Markdown export) should produce.

| Option | Required? | What it means | Effect |
|---|---|---|---|
| **Generate README** | Yes | Want a README | Agent/export includes overview-style README content |
| **Generate Architecture Doc** | Yes | Want architecture documentation | Architecture write-up when data exists |
| **Sections** | No | Named doc sections (Overview, Architecture, Workflows, Entities, …) | Which topics appear when you have filled data |

Empty product sections are **omitted** from Markdown export (no filler lorem ipsum).

---

## 5. Recommended fill order (wizard)

1. **Ad-Lib** — type, fill-in-the-blank story, product rules; pick a template if it fits
2. **Needs** — accounts, multi-device, notifications
3. **Screens** — at least one screen with meaningful things
4. **When they click** — steps for each button
5. **Remember** — only if you need History/Settings
6. **Project Blueprint** — review recommendations + hub → Check again → Save / Export JSON / Export Agent Package

Most environment, architecture, domain, constraints, build, testing, and documentation fields are filled automatically. Use Detail level **Expert** on the Blueprint hub to override theme, navigation, UI framework, language, and to set architecture/storage to Recommended, Let coding agent decide, or Choose myself. Until Expert is selected, theme comes from Ad-Lib **Look** plus the app-type profile (you will not see a Theme control on Simple/Standard).

---

## 6. Completeness checklist

Before handing the App Blueprint to an agent, ask:

- [ ] App name and Ad-Lib / description filled  
- [ ] Needs answered (accounts, multi-device, notifications)  
- [ ] At least one **screen** with meaningful **things** (for GUI apps)  
- [ ] Each important **button** has click steps  
- [ ] Remember choices match whether you want History/Settings  
- [ ] Blueprint hub **Check again** reports ready (or you accept the listed gaps)  
- [ ] Reviewed recommendations (Use or dismiss intentionally)  
- [ ] Build **target** present in JSON (usually auto `win-x64`)  

---

## 7. After export — using the App Blueprint

1. Keep your saved `.ssot.json` (or Agent Package folder) as the contract.  
2. Point the coding agent at that file/folder and [SSOT_CONTRACT.md](SSOT_CONTRACT.md). For Agent Package, start with `AGENTS.md` then `project.json`.  
3. Expect the agent to follow environment, UI, domain, workflows, requirements, and constraints strictly — and to honor decision modes (`recommended` vs `agent_discretion`).  
4. If something was left empty, either fill it in Core AI Architect and re-export, or explicitly tell the agent it may propose those parts.

---

## 8. Related docs

| Doc | Role |
|---|---|
| [README.md](../README.md) | Install, build, run, PowerShell module |
| [SSOT_CONTRACT.md](SSOT_CONTRACT.md) | Rules for AI agents consuming the JSON |
| [session_handoff.md](../session_handoff.md) | Developer session continuity (not end-user) |
