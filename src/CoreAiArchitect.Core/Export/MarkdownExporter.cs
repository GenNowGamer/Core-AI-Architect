using System.Text;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Export;

public sealed class MarkdownExporter
{
    public string ToMarkdown(SsotDocument document)
    {
        var sb = new StringBuilder();
        var sections = document.Documentation.Sections
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (document.Documentation.GenerateReadme || sections.Contains("Overview"))
            WriteOverview(sb, document);

        if (document.Documentation.GenerateArchitectureDoc || sections.Contains("Architecture"))
            WriteArchitecture(sb, document);

        if (sections.Contains("Workflows"))
            WriteWorkflows(sb, document);

        if (sections.Contains("Entities"))
            WriteEntities(sb, document);

        if (sections.Contains("Constraints") || HasConstraintContent(document))
            WriteConstraints(sb, document);

        if (sections.Contains("Environment"))
            WriteEnvironment(sb, document);

        if (sections.Contains("Build") || sections.Contains("Deployment"))
            WriteBuildDeployment(sb, document);

        return sb.ToString().Trim() + Environment.NewLine;
    }

    public void ExportToFile(SsotDocument document, string path)
    {
        var markdown = ToMarkdown(document);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, markdown);
    }

    private static void WriteOverview(StringBuilder sb, SsotDocument document)
    {
        var title = string.IsNullOrWhiteSpace(document.Project.Name) ? "Project" : document.Project.Name;
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(document.Project.Description))
        {
            sb.AppendLine(document.Project.Description);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(document.Project.Id) ||
            !string.IsNullOrWhiteSpace(document.Project.Owner) ||
            document.Project.Tags.Count > 0)
        {
            sb.AppendLine("## Overview");
            if (!string.IsNullOrWhiteSpace(document.Project.Id))
                sb.AppendLine($"- **Id:** {document.Project.Id}");
            if (!string.IsNullOrWhiteSpace(document.Project.Owner))
                sb.AppendLine($"- **Owner:** {document.Project.Owner}");
            if (document.Project.Tags.Count > 0)
                sb.AppendLine($"- **Tags:** {string.Join(", ", document.Project.Tags)}");
            sb.AppendLine();
        }

        WriteEnvironment(sb, document);
    }

    private static void WriteEnvironment(StringBuilder sb, SsotDocument document)
    {
        var env = document.Environment;
        if (string.IsNullOrWhiteSpace(env.TargetOs) &&
            string.IsNullOrWhiteSpace(env.Runtime) &&
            string.IsNullOrWhiteSpace(env.UiFramework) &&
            string.IsNullOrWhiteSpace(env.Language))
            return;

        sb.AppendLine("## Environment");
        if (!string.IsNullOrWhiteSpace(env.TargetOs)) sb.AppendLine($"- **OS:** {env.TargetOs}");
        if (!string.IsNullOrWhiteSpace(env.Runtime)) sb.AppendLine($"- **Runtime:** {env.Runtime}");
        if (!string.IsNullOrWhiteSpace(env.UiFramework)) sb.AppendLine($"- **UI Framework:** {env.UiFramework}");
        if (!string.IsNullOrWhiteSpace(env.Language)) sb.AppendLine($"- **Language:** {env.Language}");
        sb.AppendLine();
    }

    private static void WriteArchitecture(StringBuilder sb, SsotDocument document)
    {
        var arch = document.Architecture;
        if (string.IsNullOrWhiteSpace(arch.Pattern) && arch.Layers.Count == 0 && arch.Modules.Count == 0)
            return;

        sb.AppendLine("## Architecture");
        if (!string.IsNullOrWhiteSpace(arch.Pattern))
            sb.AppendLine($"- **Pattern:** {arch.Pattern}");
        if (arch.Layers.Count > 0)
            sb.AppendLine($"- **Layers:** {string.Join(", ", arch.Layers)}");
        sb.AppendLine();

        if (arch.Modules.Count == 0) return;

        sb.AppendLine("### Modules");
        foreach (var module in arch.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Name) && string.IsNullOrWhiteSpace(module.Description))
                continue;
            sb.AppendLine($"- **{module.Name}** — {module.Description}");
            if (module.Dependencies.Count > 0)
                sb.AppendLine($"  - Dependencies: {string.Join(", ", module.Dependencies)}");
        }

        sb.AppendLine();
    }

    private static void WriteWorkflows(StringBuilder sb, SsotDocument document)
    {
        if (document.Workflows.Count == 0) return;

        sb.AppendLine("## Workflows");
        foreach (var workflow in document.Workflows)
        {
            if (string.IsNullOrWhiteSpace(workflow.Name) && string.IsNullOrWhiteSpace(workflow.Id))
                continue;
            sb.AppendLine($"### {workflow.Name}");
            if (!string.IsNullOrWhiteSpace(workflow.Id))
                sb.AppendLine($"- **Id:** {workflow.Id}");
            if (!string.IsNullOrWhiteSpace(workflow.Trigger))
                sb.AppendLine($"- **Trigger:** {workflow.Trigger}");
            if (workflow.Steps.Count > 0)
            {
                sb.AppendLine("- **Steps:**");
                foreach (var step in workflow.Steps)
                    sb.AppendLine($"  1. {step}");
            }

            if (workflow.ErrorHandling is not null &&
                !string.IsNullOrWhiteSpace(workflow.ErrorHandling.Strategy))
                sb.AppendLine($"- **Error handling:** {workflow.ErrorHandling.Strategy} ({workflow.ErrorHandling.LogLevel})");
            sb.AppendLine();
        }
    }

    private static void WriteEntities(StringBuilder sb, SsotDocument document)
    {
        if (document.Domain.Entities.Count == 0) return;

        sb.AppendLine("## Entities");
        foreach (var entity in document.Domain.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Name)) continue;
            sb.AppendLine($"### {entity.Name}");
            if (!string.IsNullOrWhiteSpace(entity.Description))
                sb.AppendLine(entity.Description);
            if (entity.Fields.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("| Field | Type | Required |");
                sb.AppendLine("| --- | --- | --- |");
                foreach (var field in entity.Fields)
                    sb.AppendLine($"| {field.Name} | {field.Type} | {field.Required} |");
            }

            sb.AppendLine();
        }
    }

    private static bool HasConstraintContent(SsotDocument document) =>
        document.Constraints.AllowedLibraries.Count > 0
        || document.Constraints.ForbiddenLibraries.Count > 0
        || document.Constraints.LoggingRequired
        || !document.Constraints.TelemetryAllowed;

    private static void WriteConstraints(StringBuilder sb, SsotDocument document)
    {
        var c = document.Constraints;
        sb.AppendLine("## Constraints");
        sb.AppendLine($"- **Logging required:** {c.LoggingRequired}");
        sb.AppendLine($"- **Telemetry allowed:** {c.TelemetryAllowed}");
        sb.AppendLine($"- **Third-party bootloaders allowed:** {c.ThirdPartyBootloadersAllowed}");
        if (c.AllowedLibraries.Count > 0)
            sb.AppendLine($"- **Allowed libraries:** {string.Join(", ", c.AllowedLibraries)}");
        if (c.ForbiddenLibraries.Count > 0)
            sb.AppendLine($"- **Forbidden libraries:** {string.Join(", ", c.ForbiddenLibraries)}");
        sb.AppendLine();
    }

    private static void WriteBuildDeployment(StringBuilder sb, SsotDocument document)
    {
        sb.AppendLine("## Build and Deployment");
        sb.AppendLine($"- **Output type:** {document.Build.OutputType}");
        if (document.Build.Configuration.Count > 0)
            sb.AppendLine($"- **Configurations:** {string.Join(", ", document.Build.Configuration)}");
        foreach (var target in document.Build.Targets)
            sb.AppendLine($"- **Target:** {target.Name} ({target.RuntimeIdentifier})");
        sb.AppendLine($"- **Packaging:** {document.Deployment.Packaging}");
        sb.AppendLine($"- **Auto update:** {document.Deployment.AutoUpdate}");
        sb.AppendLine();
    }
}
