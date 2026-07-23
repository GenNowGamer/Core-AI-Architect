using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Validation;

/// <summary>
/// Schema validation plus end-to-end readiness checks for handing an SSOT to a coding agent.
/// </summary>
public sealed class SsotReadinessEngine
{
    private readonly ValidationEngine _validationEngine = new();

    public ValidationResult Assess(SsotDocument document, FormSchemaDefinition schema)
    {
        var result = _validationEngine.Validate(document, schema);
        AssessEndToEndReadiness(document, result);
        return result;
    }

    private static void AssessEndToEndReadiness(SsotDocument document, ValidationResult result)
    {
        AssessArchitecture(document, result);
        AssessUiSurfaces(document, result);
        AssessDomain(document, result);
        AssessWorkflows(document, result);
        AssessBuild(document, result);
        AssessTestingConsistency(document, result);
        AssessEnvironmentUiConsistency(document, result);
    }

    private static void AssessArchitecture(SsotDocument document, ValidationResult result)
    {
        if (document.Architecture.Layers.Count == 0 && document.Architecture.Modules.Count == 0)
        {
            result.Issues.Add(Error(
                "architecture",
                "Add at least one Architecture layer or module so the agent has a solution structure to follow."));
        }

        for (var i = 0; i < document.Architecture.Modules.Count; i++)
        {
            var module = document.Architecture.Modules[i];
            if (string.IsNullOrWhiteSpace(module.Name))
            {
                result.Issues.Add(Error(
                    $"architecture.modules[{i}].name",
                    "Each module needs a Name for end-to-end generation."));
            }
        }
    }

    private static void AssessUiSurfaces(SsotDocument document, ValidationResult result)
    {
        var framework = document.Environment.UiFramework ?? string.Empty;
        if (framework.Equals("None", StringComparison.OrdinalIgnoreCase))
            return;

        if (document.Ui.Screens.Count == 0)
        {
            result.Issues.Add(Error(
                "ui.screens",
                "Add at least one screen/surface with controls so the agent knows what UI (or API/CLI surface) to build."));
            return;
        }

        var anyControl = false;
        for (var s = 0; s < document.Ui.Screens.Count; s++)
        {
            var screen = document.Ui.Screens[s];
            if (string.IsNullOrWhiteSpace(screen.Id) || string.IsNullOrWhiteSpace(screen.Title))
            {
                result.Issues.Add(Error(
                    $"ui.screens[{s}]",
                    $"Screen {s + 1} needs both Id and Title."));
            }

            if (screen.Controls.Count == 0)
            {
                result.Issues.Add(Error(
                    $"ui.screens[{s}].controls",
                    $"Screen '{screen.Title}' needs at least one control (button, input, endpoint, command, …)."));
                continue;
            }

            anyControl = true;
            for (var c = 0; c < screen.Controls.Count; c++)
            {
                var control = screen.Controls[c];
                if (string.IsNullOrWhiteSpace(control.Id) || string.IsNullOrWhiteSpace(control.Type))
                {
                    result.Issues.Add(Error(
                        $"ui.screens[{s}].controls[{c}]",
                        $"Control {c + 1} on '{screen.Title}' needs Id and Type."));
                }
            }
        }

        if (!anyControl)
        {
            result.Issues.Add(Error(
                "ui.screens",
                "At least one screen must define controls for an end-to-end UI/API/CLI contract."));
        }
    }

    private static void AssessDomain(SsotDocument document, ValidationResult result)
    {
        if (document.Domain.Entities.Count == 0)
        {
            result.Issues.Add(Error(
                "domain.entities",
                "Add at least one domain entity with fields so the agent has a data model (not only prose in Description)."));
            return;
        }

        for (var i = 0; i < document.Domain.Entities.Count; i++)
        {
            var entity = document.Domain.Entities[i];
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                result.Issues.Add(Error(
                    $"domain.entities[{i}].name",
                    "Each entity needs a Name."));
            }

            if (entity.Fields.Count == 0)
            {
                result.Issues.Add(Error(
                    $"domain.entities[{i}].fields",
                    $"Entity '{entity.Name}' needs at least one field."));
                continue;
            }

            for (var f = 0; f < entity.Fields.Count; f++)
            {
                var field = entity.Fields[f];
                if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Type))
                {
                    result.Issues.Add(Error(
                        $"domain.entities[{i}].fields[{f}]",
                        $"Field {f + 1} on '{entity.Name}' needs Name and Type."));
                }
            }
        }
    }

    private static void AssessWorkflows(SsotDocument document, ValidationResult result)
    {
        if (document.Workflows.Count == 0)
        {
            result.Issues.Add(Error(
                "workflows",
                "Add at least one workflow with ordered steps so the agent knows the main user/system flows."));
            return;
        }

        var controlIds = document.Ui.Screens
            .SelectMany(s => s.Controls)
            .Select(c => c.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var expectsUiTriggers = RequiresInteractiveSurfaces(document.Environment.UiFramework)
                                && controlIds.Count > 0;

        for (var i = 0; i < document.Workflows.Count; i++)
        {
            var workflow = document.Workflows[i];
            if (string.IsNullOrWhiteSpace(workflow.Id) || string.IsNullOrWhiteSpace(workflow.Name))
            {
                result.Issues.Add(Error(
                    $"workflows[{i}]",
                    $"Workflow {i + 1} needs both Id and Name."));
            }

            if (workflow.Steps.Count == 0)
            {
                result.Issues.Add(Error(
                    $"workflows[{i}].steps",
                    $"Workflow '{workflow.Name}' needs at least one step."));
            }

            if (expectsUiTriggers && string.IsNullOrWhiteSpace(workflow.Trigger))
            {
                result.Issues.Add(Error(
                    $"workflows[{i}].trigger",
                    $"Workflow '{workflow.Name}' needs a Trigger matching a UI control Id for end-to-end wiring."));
            }
        }
    }

    private static void AssessBuild(SsotDocument document, ValidationResult result)
    {
        if (document.Build.Targets.Count == 0)
        {
            result.Issues.Add(Error(
                "build.targets",
                "Add at least one build target (e.g. Name=win-x64, Runtime Identifier=win-x64) so the agent knows what to publish."));
            return;
        }

        for (var i = 0; i < document.Build.Targets.Count; i++)
        {
            var target = document.Build.Targets[i];
            if (string.IsNullOrWhiteSpace(target.Name) || string.IsNullOrWhiteSpace(target.RuntimeIdentifier))
            {
                result.Issues.Add(Error(
                    $"build.targets[{i}]",
                    $"Build target {i + 1} needs Name and Runtime Identifier."));
            }
        }
    }

    private static void AssessTestingConsistency(SsotDocument document, ValidationResult result)
    {
        var uiFramework = document.Environment.UiFramework ?? string.Empty;
        var uiTest = document.Testing.UiTestFramework ?? string.Empty;
        var isDesktop = uiFramework.Equals("WPF", StringComparison.OrdinalIgnoreCase)
                        || uiFramework.Equals("WinUI", StringComparison.OrdinalIgnoreCase);

        if (isDesktop && uiTest.Equals("Playwright", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(Warning(
                "testing.uiTestFramework",
                "Playwright is web-oriented; for WPF/WinUI prefer FlaUI, WinAppDriver, or None for a coherent agent handoff."));
        }
    }

    private static void AssessEnvironmentUiConsistency(SsotDocument document, ValidationResult result)
    {
        var uiFramework = document.Environment.UiFramework ?? string.Empty;
        var nav = document.Ui.NavigationPattern ?? string.Empty;

        if (uiFramework.Equals("WinUI", StringComparison.OrdinalIgnoreCase)
            && nav.Equals("Window+Dialogs", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(Warning(
                "ui.navigationPattern",
                "WinUI projects usually use NavigationFrame or Tabbed rather than Window+Dialogs."));
        }

        if (uiFramework.Equals("ASP.NET", StringComparison.OrdinalIgnoreCase)
            && !(nav.Equals("HttpRoutes", StringComparison.OrdinalIgnoreCase)))
        {
            result.Issues.Add(Warning(
                "ui.navigationPattern",
                "ASP.NET projects usually use HttpRoutes as the navigation pattern."));
        }
    }

    private static bool RequiresInteractiveSurfaces(string? uiFramework) =>
        !string.IsNullOrWhiteSpace(uiFramework)
        && !uiFramework.Equals("None", StringComparison.OrdinalIgnoreCase);

    private static ValidationIssue Error(string path, string message) => new()
    {
        Severity = ValidationSeverity.Error,
        Path = path,
        Message = message
    };

    private static ValidationIssue Warning(string path, string message) => new()
    {
        Severity = ValidationSeverity.Warning,
        Path = path,
        Message = message
    };
}
