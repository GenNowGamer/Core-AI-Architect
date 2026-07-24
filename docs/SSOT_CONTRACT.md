# SSOT Contract for AI Coding Agents

**Input:** a single JSON document with `"ssotVersion": "1.0.0"` matching the Core AI Architect export shape.

In the Core AI Architect UI this file is called an **App Blueprint**; the on-disk format and property names below are unchanged (`*.ssot.json`).

## Required adherence

Generate a complete solution that strictly follows:

| Section | Meaning |
|---|---|
| `environment` | Target OS, runtime, UI framework, language |
| `architecture` | Pattern, layers, modules and dependencies |
| `ui` | Theme, navigation, screens, controls, actions |
| `domain` | Entities and fields |
| `workflows` | Triggers, ordered steps, error handling |
| `constraints` | Allowed/forbidden libraries, logging, telemetry, bootloaders |
| `data` | Storage type and connection strategy |
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
