# SSOT Contract for AI Coding Agents

**Input:** a single JSON document with `"ssotVersion": "1.0.0"` matching the Core AI Architect export shape.

In the Core AI Architect UI this file is called an **App Blueprint**; the on-disk format and property names below are unchanged (`*.ssot.json`).

## Required adherence

Generate a complete solution that strictly follows:

| Section | Meaning |
|---|---|
| `environment` | Target OS, runtime, UI framework, language |
| `architecture` | Pattern, layers, modules and dependencies (see decision modes) |
| `ui` | Theme, navigation, screens, controls, actions |
| `domain` | Entities and fields |
| `workflows` | Triggers, ordered steps, error handling |
| `constraints` | Allowed/forbidden libraries, logging, telemetry, bootloaders |
| `data` | Storage type and connection strategy (see decision modes) |
| `requirements` | Accounts, multi-device, notifications (from gap questions) |
| `build` | Output type, configurations, runtime targets |
| `deployment` | Packaging and auto-update |
| `testing` | Unit/UI test frameworks and coverage target |
| `documentation` | Which docs to produce |

## Non-negotiables

1. Do **not** use any library listed in `constraints.forbiddenLibraries`.
2. Prefer libraries listed in `constraints.allowedLibraries` when relevant.
3. Implement logging if `constraints.loggingRequired` is `true`.
4. Respect `constraints.telemetryAllowed` — no analytics/telemetry when `false`.
5. Respect `constraints.thirdPartyBootloadersAllowed`.

## Expected outputs

- Solution and project structure matching `architecture` / `build`
- Core code for UI (or API/CLI surfaces), domain, and infrastructure
- Configuration files as implied by `data.storage`
- Documentation sections requested by `documentation` (README / architecture / workflows / entities as applicable)

## Empty collections

Empty arrays (`screens`, `entities`, `workflows`, `modules`, etc.) mean the author intentionally left them blank. Do not invent demo inventory or placeholder business domains unless the user asks.

## Decision modes (`agent_discretion`)

Technical choices may include a nested `decision` / `patternDecision` object:

| `mode` | Meaning |
|---|---|
| `specified` | Use the provided value exactly. |
| `recommended` | Core AI Architect chose this value (see `ruleId` / `rationale`). Keep it unless the user overrides. |
| `agent_discretion` | The author **intentionally** left the choice to you. Pick a solid default that satisfies `guidance` and the rest of the blueprint. Do not treat this as an accidental blank. |

Applies today to:

- `architecture.patternDecision`
- `data.storage.decision`

## Requirements (gap questions)

`requirements` captures human-language needs that are not the UI storyboard:

| Field | Values |
|---|---|
| `accounts` | `none` \| `separate` \| `unsure` |
| `multiDevice` | `no` \| `yes` \| `later` |
| `notifications` | boolean + optional `notificationNotes` |

## Android profile (`android_app`)

When `environment.targetOs` is `Android` (or the blueprint was authored with the Android app card):

| Field | Typical values |
|---|---|
| `environment.runtime` | `Android Runtime` (native), or `.NET 8` / `.NET 9` for MAUI |
| `environment.uiFramework` | `Jetpack Compose` (default), `Android Views`, `.NET MAUI` |
| `environment.language` | `Kotlin` (default), `Java`, `C#` |
| `ui.theme` | `Material3` / `Material` / `Custom` |
| `ui.navigationPattern` | `BottomNavigation`, `NavigationCompose`, `SingleActivity`, `Tabbed` |
| `build.outputType` | `Aab` / `Apk` / `Library` |
| `deployment.packaging` | `AAB` / `APK` / `PlayStore` / `None` |
| `testing` | Prefer `JUnit` + `ComposeUiTest` or `Espresso` |

Map beginner wizard controls to Compose/Android widgets (`TextField`, `DropdownMenu`, `LazyColumn`, `Button`, etc.) as listed in `ui.screens`. Respect `requirements.notifications` for notification permission / channels.

## Generic domain scaffolds (Core AI Architect wizard)

When `domain.entities` use generic names such as `OptionChoice`, `UserInput`, `ResultItem`, `HistoryEntry`, or `AppSettings`, and entity descriptions contain **Scaffold only**:

1. Treat **`ui.screens`** as the binding UI contract (controls, labels, actions).
2. Treat **`project.description`** and **`workflows[].steps`** as the product-rules source (game lists, number rules, validation, caching, ticket semantics).
3. **Invent a richer domain model** in code (e.g. `LotteryGame`, `Ticket`, ball sets) that fulfills those rules. Do not stop at the scaffold types if the description implies a fuller model.
4. Keep scaffold entities only as a minimal fallback for display/list bindings when no richer mapping is needed.

Authors are instructed to put rich rules in description + click steps on purpose; they are not expected to hand-edit `domain.entities` in the beginner wizard.

## Agent Package folder (optional export)

When the author exports an **Agent Package**, treat the folder as the working set:

| Path | Role |
|---|---|
| `project.json` / `*.ssot.json` | Canonical App Blueprint (same shape as this contract) |
| `AGENTS.md` | How to interpret decision modes, scaffolds, and non-negotiables |
| `specifications/` | Human-readable requirements, architecture, UI, data, testing |
| `tasks/` | Suggested build phases (execute in order unless the user reprioritizes) |
| `assets/` | Optional icons / reference images |

Do not invent a second source of truth — the JSON remains authoritative; Markdown is derived.
