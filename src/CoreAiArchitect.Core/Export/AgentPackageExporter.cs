using System.Text;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Export;

/// <summary>
/// Writes a folder an AI coding agent can consume: SSOT JSON, AGENTS.md, specs, and phased tasks.
/// Deterministic — no AI API required.
/// </summary>
public sealed class AgentPackageExporter
{
    private readonly JsonExporter _json = new();
    private readonly MarkdownExporter _markdown = new();

    public IReadOnlyList<string> Export(SsotDocument document, string directory)
    {
        Directory.CreateDirectory(directory);
        var written = new List<string>();

        var projectJson = Path.Combine(directory, "project.json");
        _json.ExportToFile(document, projectJson);
        written.Add(projectJson);

        var ssotName = string.IsNullOrWhiteSpace(document.Project.Id)
            ? "app-blueprint.ssot.json"
            : document.Project.Id + ".ssot.json";
        var ssotPath = Path.Combine(directory, ssotName);
        _json.ExportToFile(document, ssotPath);
        written.Add(ssotPath);

        var agents = Path.Combine(directory, "AGENTS.md");
        File.WriteAllText(agents, BuildAgentsMd(document));
        written.Add(agents);

        var cursorRules = Path.Combine(directory, ".cursorrules");
        File.WriteAllText(cursorRules, BuildCursorRules(document));
        written.Add(cursorRules);

        var claudeMd = Path.Combine(directory, "CLAUDE.md");
        File.WriteAllText(claudeMd, BuildClaudeMd(document));
        written.Add(claudeMd);

        var geminiDir = Path.Combine(directory, ".gemini");
        Directory.CreateDirectory(geminiDir);
        var geminiCfg = Path.Combine(geminiDir, "config.json");
        File.WriteAllText(geminiCfg, BuildGeminiConfig(document));
        written.Add(geminiCfg);

        var gitIgnorePath = Path.Combine(directory, ".gitignore");
        if (!File.Exists(gitIgnorePath))
        {
            File.WriteAllText(gitIgnorePath, BuildGitIgnore(document));
            written.Add(gitIgnorePath);
        }

        var specsDir = Path.Combine(directory, "specifications");
        Directory.CreateDirectory(specsDir);
        written.Add(WriteSpec(specsDir, "requirements.md", BuildRequirements(document)));
        written.Add(WriteSpec(specsDir, "architecture.md", BuildArchitecture(document)));
        written.Add(WriteSpec(specsDir, "ui.md", BuildUi(document)));
        written.Add(WriteSpec(specsDir, "database.md", BuildData(document)));
        written.Add(WriteSpec(specsDir, "testing.md", BuildTesting(document)));

        var overview = Path.Combine(specsDir, "overview.md");
        File.WriteAllText(overview, _markdown.ToMarkdown(document));
        written.Add(overview);

        var tasksDir = Path.Combine(directory, "tasks");
        Directory.CreateDirectory(tasksDir);
        var tasks = BuildTasks(document);
        for (var i = 0; i < tasks.Count; i++)
        {
            var name = $"{i + 1:000}-{tasks[i].Slug}.md";
            var path = Path.Combine(tasksDir, name);
            File.WriteAllText(path, tasks[i].Body);
            written.Add(path);
        }

        var assets = Path.Combine(directory, "assets");
        Directory.CreateDirectory(assets);
        var readmeAssets = Path.Combine(assets, "README.md");
        File.WriteAllText(readmeAssets, "# Assets\n\nPlace icons, logos, and reference screenshots here for the coding agent.\n");
        written.Add(readmeAssets);

        return written;
    }

    private static string WriteSpec(string dir, string fileName, string body)
    {
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, body.Trim() + Environment.NewLine);
        return path;
    }

    public static string BuildAgentsMd(SsotDocument document)
    {
        var name = string.IsNullOrWhiteSpace(document.Project.Name) ? "this application" : document.Project.Name;
        var sb = new StringBuilder();
        sb.AppendLine($"# Agent instructions — {name}");
        sb.AppendLine();
        sb.AppendLine("You are building an application from a Core AI Architect **App Blueprint**.");
        sb.AppendLine();
        sb.AppendLine("## Source of truth");
        sb.AppendLine();
        sb.AppendLine("1. Read `project.json` (same content as the `.ssot.json` sibling).");
        sb.AppendLine("2. Follow `specifications/` for human-readable detail.");
        sb.AppendLine("3. Execute `tasks/` in order unless the user reprioritizes.");
        sb.AppendLine("4. Obey the SSOT contract below for decision modes and empty collections.");
        sb.AppendLine();
        sb.AppendLine("## Decision modes");
        sb.AppendLine();
        sb.AppendLine("- `specified` — use the given value exactly.");
        sb.AppendLine("- `recommended` — Core AI Architect chose this; keep it unless the user overrides.");
        sb.AppendLine("- `agent_discretion` — the omission is intentional. Choose a solid default that matches `guidance` and the rest of the blueprint. Do not ask the user unless blocked.");
        sb.AppendLine();
        sb.AppendLine("## Non-negotiables");
        sb.AppendLine();
        sb.AppendLine("- Respect `constraints.forbiddenLibraries` / `allowedLibraries`.");
        sb.AppendLine("- Implement logging when `constraints.loggingRequired` is true.");
        sb.AppendLine("- No telemetry when `constraints.telemetryAllowed` is false.");
        sb.AppendLine("- Empty arrays mean intentionally blank — do not invent demo domains.");
        sb.AppendLine("- When domain entities say **Scaffold only**, invent a richer model from `project.description` and workflow steps.");
        sb.AppendLine();
        sb.AppendLine("## Product intent");
        sb.AppendLine();
        sb.AppendLine(document.Project.Description.Trim());
        sb.AppendLine();
        return sb.ToString();
    }

    public static string BuildRequirements(SsotDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Requirements");
        sb.AppendLine();
        sb.AppendLine(document.Project.Description);
        sb.AppendLine();
        sb.AppendLine("## Accounts / devices / notifications");
        sb.AppendLine();
        sb.AppendLine($"- Accounts: `{document.Requirements.Accounts}`");
        sb.AppendLine($"- Multi-device: `{document.Requirements.MultiDevice}`");
        sb.AppendLine($"- Notifications: {(document.Requirements.Notifications ? "yes" : "no")}");
        if (!string.IsNullOrWhiteSpace(document.Requirements.NotificationNotes))
            sb.AppendLine($"- Notification notes: {document.Requirements.NotificationNotes}");
        foreach (var note in document.Requirements.Notes)
            sb.AppendLine($"- {note}");
        sb.AppendLine();
        sb.AppendLine("## Screens (UI contract)");
        sb.AppendLine();
        foreach (var screen in document.Ui.Screens)
        {
            var screenType = string.IsNullOrWhiteSpace(screen.ScreenType) ? "Window" : screen.ScreenType;
            sb.AppendLine($"### {screen.Title} (`{screen.Id}`, Type: {screenType})");
            foreach (var control in screen.Controls)
                sb.AppendLine($"- {control.Label} ({control.Type}) → {control.Action}");
            sb.AppendLine();
        }

        sb.AppendLine("## Workflows");
        sb.AppendLine();
        foreach (var workflow in document.Workflows)
        {
            sb.AppendLine($"### {workflow.Name}");
            sb.AppendLine($"Trigger: `{workflow.Trigger}`");
            foreach (var step in workflow.Steps)
                sb.AppendLine($"1. {step}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildArchitecture(SsotDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Architecture");
        sb.AppendLine();
        sb.AppendLine($"Pattern: **{document.Architecture.Pattern}**");
        AppendDecision(sb, document.Architecture.PatternDecision);
        sb.AppendLine();
        sb.AppendLine("## Environment");
        sb.AppendLine($"- OS: {document.Environment.TargetOs}");
        sb.AppendLine($"- Runtime: {document.Environment.Runtime}");
        sb.AppendLine($"- UI: {document.Environment.UiFramework}");
        sb.AppendLine($"- Language: {document.Environment.Language}");
        sb.AppendLine();
        sb.AppendLine("## Layers");
        foreach (var layer in document.Architecture.Layers)
            sb.AppendLine($"- {layer}");
        sb.AppendLine();
        sb.AppendLine("## Modules");
        foreach (var module in document.Architecture.Modules)
            sb.AppendLine($"- **{module.Name}** — {module.Description}");
        return sb.ToString();
    }

    private static string BuildUi(SsotDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# UI specification");
        sb.AppendLine();
        sb.AppendLine($"- Theme: {document.Ui.Theme}");
        sb.AppendLine($"- Navigation: {document.Ui.NavigationPattern}");
        sb.AppendLine();
        foreach (var screen in document.Ui.Screens)
        {
            var screenType = string.IsNullOrWhiteSpace(screen.ScreenType) ? "Window" : screen.ScreenType;
            sb.AppendLine($"## {screen.Title} (Type: {screenType})");
            sb.AppendLine($"Route: `{screen.Route}`");
            foreach (var control in screen.Controls)
                sb.AppendLine($"- `{control.Id}` {control.Type}: {control.Label} (action {control.Action})");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildData(SsotDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Data / persistence");
        sb.AppendLine();
        sb.AppendLine($"- Storage type: **{document.Data.Storage.Type}**");
        sb.AppendLine($"- Connection string mode: {document.Data.Storage.ConnectionStringMode}");
        sb.AppendLine($"- Migrations required: {document.Data.Storage.MigrationsRequired}");
        AppendDecision(sb, document.Data.Storage.Decision);
        sb.AppendLine();
        sb.AppendLine("## Entities (may be scaffolds)");
        foreach (var entity in document.Domain.Entities)
        {
            sb.AppendLine($"### {entity.Name}");
            sb.AppendLine(entity.Description);
            foreach (var field in entity.Fields)
                sb.AppendLine($"- {field.Name}: {field.Type}{(field.Required ? " (required)" : string.Empty)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildTesting(SsotDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Testing");
        sb.AppendLine();
        sb.AppendLine($"- Unit: {document.Testing.UnitTestFramework}");
        sb.AppendLine($"- UI: {document.Testing.UiTestFramework}");
        sb.AppendLine($"- Coverage target: {document.Testing.CoverageTarget:P0}");
        sb.AppendLine();
        sb.AppendLine("Cover core workflows listed in `requirements.md`. Prefer tests that lock product rules from the description and click steps.");
        return sb.ToString();
    }

    private static void AppendDecision(StringBuilder sb, DecisionInfo decision)
    {
        if (string.IsNullOrWhiteSpace(decision.Mode) || decision.Mode.Equals("specified", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(decision.Rationale))
                sb.AppendLine($"- Rationale: {decision.Rationale}");
            return;
        }

        sb.AppendLine($"- Decision mode: `{decision.Mode}`");
        if (!string.IsNullOrWhiteSpace(decision.RuleId))
            sb.AppendLine($"- Rule: `{decision.RuleId}`");
        if (!string.IsNullOrWhiteSpace(decision.Rationale))
            sb.AppendLine($"- Why: {decision.Rationale}");
        if (decision.Guidance.Count > 0)
            sb.AppendLine($"- Guidance: {string.Join(", ", decision.Guidance)}");
    }

    public static List<(string Slug, string Body)> BuildTasks(SsotDocument document)
    {
        var name = string.IsNullOrWhiteSpace(document.Project.Name) ? "the app" : document.Project.Name;
        var tasks = new List<(string, string)>
        {
            ("foundation", TaskBody("Phase 1 — Foundation",
                $"Create the {name} solution structure, dependencies, architecture ({document.Architecture.Pattern}), and environment ({document.Environment.UiFramework} / {document.Environment.Language}).")),
            ("data-layer", TaskBody("Phase 2 — Data layer",
                $"Implement persistence using `{document.Data.Storage.Type}` (decision mode: {document.Data.Storage.Decision.Mode}). Model entities from screens + description; enrich scaffolds.")),
            ("navigation-shell", TaskBody("Phase 3 — Navigation and shell",
                $"Build navigation (`{document.Ui.NavigationPattern}`), theme (`{document.Ui.Theme}`), and the screen shell.")),
            ("features", TaskBody("Phase 4 — Features",
                "Implement each screen control and its workflow steps. Product rules live in project.description and workflow steps.")),
            ("reliability", TaskBody("Phase 5 — Reliability",
                "Add validation, error handling, logging (per constraints), and backup/export if present in the UI contract.")),
            ("testing-release", TaskBody("Phase 6 — Testing and release",
                $"Add {document.Testing.UnitTestFramework} tests (UI: {document.Testing.UiTestFramework}), then packaging `{document.Deployment.Packaging}`."))
        };
        return tasks;
    }

    private static string TaskBody(string title, string summary) =>
        $"# {title}\n\n{summary}\n\nUse `project.json` and `specifications/` as the contract. Prefer small, reviewable commits.\n";

    private static string BuildCursorRules(SsotDocument document)
    {
        var name = string.IsNullOrWhiteSpace(document.Project.Name) ? "Application" : document.Project.Name;
        var sb = new StringBuilder();
        sb.AppendLine($"# Cursor Rules — {name}");
        sb.AppendLine();
        sb.AppendLine("- You are an expert AI software engineer implementing an application specified by an App Blueprint (`project.json`).");
        sb.AppendLine($"- Target OS & Runtime: {document.Environment.TargetOs} / {document.Environment.Runtime}");
        sb.AppendLine($"- UI Framework & Language: {document.Environment.UiFramework} ({document.Environment.Language})");
        sb.AppendLine($"- Architecture Pattern: {document.Architecture.Pattern} (Decision mode: {document.Architecture.PatternDecision.Mode})");
        sb.AppendLine($"- Data Storage: {document.Data.Storage.Type} (Decision mode: {document.Data.Storage.Decision.Mode})");
        sb.AppendLine();
        sb.AppendLine("## Non-Negotiable Constraints");
        sb.AppendLine($"- Logging required: {document.Constraints.LoggingRequired}");
        sb.AppendLine($"- Telemetry allowed: {document.Constraints.TelemetryAllowed}");
        if (document.Constraints.ForbiddenLibraries.Count > 0)
            sb.AppendLine($"- Forbidden libraries: {string.Join(", ", document.Constraints.ForbiddenLibraries)}");
        if (document.Constraints.AllowedLibraries.Count > 0)
            sb.AppendLine($"- Allowed libraries: {string.Join(", ", document.Constraints.AllowedLibraries)}");
        sb.AppendLine();
        sb.AppendLine("## Implementation Flow");
        sb.AppendLine("1. Review `project.json` and `specifications/` before writing code.");
        sb.AppendLine("2. Complete phases sequentially as outlined in `tasks/`.");
        sb.AppendLine("3. If domain entities say Scaffold only, invent a richer domain model fulfilling `project.description` and workflow steps.");
        return sb.ToString();
    }

    private static string BuildClaudeMd(SsotDocument document)
    {
        var name = string.IsNullOrWhiteSpace(document.Project.Name) ? "Application" : document.Project.Name;
        var sb = new StringBuilder();
        sb.AppendLine($"# Claude Code Instructions — {name}");
        sb.AppendLine();
        sb.AppendLine("## Project Architecture");
        sb.AppendLine($"- Stack: {document.Environment.Language} / {document.Environment.UiFramework} on {document.Environment.TargetOs} ({document.Environment.Runtime})");
        sb.AppendLine($"- Architecture: {document.Architecture.Pattern}");
        sb.AppendLine($"- Storage: {document.Data.Storage.Type}");
        sb.AppendLine($"- Testing: {document.Testing.UnitTestFramework} (UI: {document.Testing.UiTestFramework})");
        sb.AppendLine();
        sb.AppendLine("## Development Guidelines");
        sb.AppendLine("- Consult `project.json` as the Single Source of Truth.");
        sb.AppendLine("- Execute the phased tasks in `tasks/` sequentially.");
        sb.AppendLine("- Keep code modular, cleanly separated by layers, and covered by unit tests.");
        sb.AppendLine("- Respect decision modes: adhere to `specified` / `recommended`; use best judgment for `agent_discretion`.");
        return sb.ToString();
    }

    private static string BuildGeminiConfig(SsotDocument document)
    {
        var name = string.IsNullOrWhiteSpace(document.Project.Name) ? "Application" : document.Project.Name;
        return $$"""
        {
          "projectName": "{{name}}",
          "blueprintVersion": "{{document.SsotVersion}}",
          "profileId": "{{document.ProfileId}}",
          "architecture": "{{document.Architecture.Pattern}}",
          "specificationEntry": "specifications/overview.md",
          "tasksDirectory": "tasks/"
        }
        """;
    }

    private static string BuildGitIgnore(SsotDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Core AI Architect Workspace .gitignore");
        sb.AppendLine(".vs/");
        sb.AppendLine(".idea/");
        sb.AppendLine("*.user");
        sb.AppendLine("*.suo");
        sb.AppendLine();

        if (document.Environment.TargetOs.Equals("Android", StringComparison.OrdinalIgnoreCase)
            || document.ProfileId.Equals("android_app", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("# Android / Gradle");
            sb.AppendLine(".gradle/");
            sb.AppendLine("build/");
            sb.AppendLine("app/build/");
            sb.AppendLine("*.apk");
            sb.AppendLine("*.aab");
            sb.AppendLine("local.properties");
        }
        else
        {
            sb.AppendLine("# .NET / Visual Studio");
            sb.AppendLine("[Bb]in/");
            sb.AppendLine("[Oo]bj/");
            sb.AppendLine("artifacts/");
            sb.AppendLine("TestResults/");
        }

        return sb.ToString();
    }

    public static bool InitializeGitRepository(string directory, SsotDocument document, out string output)
    {
        try
        {
            var gitDir = Path.Combine(directory, ".git");
            if (Directory.Exists(gitDir))
            {
                output = "Git repository already initialized.";
                return true;
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "init",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p1 = System.Diagnostics.Process.Start(startInfo);
            p1?.WaitForExit(5000);

            startInfo.Arguments = "add .";
            using var p2 = System.Diagnostics.Process.Start(startInfo);
            p2?.WaitForExit(5000);

            var commitMsg = $"Initial App Blueprint and Agent Package for {document.Project.Name}".Trim();
            startInfo.Arguments = $"commit -m \"{commitMsg}\"";
            using var p3 = System.Diagnostics.Process.Start(startInfo);
            p3?.WaitForExit(5000);

            output = "Git repository initialized and initial commit created.";
            return true;
        }
        catch (Exception ex)
        {
            output = $"Git initialization skipped or failed: {ex.Message}";
            return false;
        }
    }
}
