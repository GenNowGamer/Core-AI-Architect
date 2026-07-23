using CoreAiArchitect.Core.Documents;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Wizard;

/// <summary>
/// Builds a complete, readiness-friendly SSOT from plain wizard answers.
/// </summary>
public sealed class SsotSmartBuilder
{
    private readonly CompatibilityEngine _compatibility = new();

    public SsotDocument Build(WizardAnswers answers, FormSchemaDefinition profile)
    {
        _compatibility.ApplySafeDefaults(answers, profile);
        var compat = _compatibility.Evaluate(answers, profile);
        if (!compat.IsCompatible)
        {
            throw new InvalidOperationException(
                string.Join(" ", compat.Issues.Select(i => i.Message + " " + i.SuggestedFix)));
        }

        var doc = SsotDocumentFactory.CreateNew(profile);
        ApplyIdentity(doc, answers);
        ApplyEnvironment(doc, answers);
        ApplyLookAndFeel(doc, answers, profile);
        ApplyMemory(doc, answers, profile);
        ApplyArchitecture(doc, profile);
        ApplyCapabilities(doc, answers, profile);
        ApplyBuild(doc, profile);
        ApplyTesting(doc, answers);
        ApplyConstraints(doc, profile);
        ApplyDocumentation(doc);

        return SsotDocumentNormalizer.Normalize(doc);
    }

    private static void ApplyIdentity(SsotDocument doc, WizardAnswers answers)
    {
        doc.Project.Id = answers.DerivedProjectId;
        doc.Project.Name = answers.ProjectName.Trim();
        doc.Project.Description = answers.Description.Trim();
        doc.Project.Owner = answers.Owner.Trim();
        doc.Project.Tags = new List<string> { answers.ProfileId.Replace('_', '-') };
    }

    private static void ApplyEnvironment(SsotDocument doc, WizardAnswers answers)
    {
        doc.Environment.TargetOs = answers.TargetOs;
        doc.Environment.Runtime = answers.Runtime;
        doc.Environment.UiFramework = answers.UiFramework;
        doc.Environment.Language = answers.Language;
    }

    private void ApplyLookAndFeel(SsotDocument doc, WizardAnswers answers, FormSchemaDefinition profile)
    {
        var themes = _compatibility.AllowedThemes(profile);
        doc.Ui.Theme = themes.Contains(answers.Theme, StringComparer.OrdinalIgnoreCase)
            ? answers.Theme
            : themes[0];

        var navs = _compatibility.AllowedNavigationPatterns(profile.Id, answers.UiFramework, profile);
        doc.Ui.NavigationPattern = navs.Contains(answers.NavigationPattern, StringComparer.OrdinalIgnoreCase)
            ? answers.NavigationPattern
            : navs[0];
    }

    private static void ApplyMemory(SsotDocument doc, WizardAnswers answers, FormSchemaDefinition profile)
    {
        var allowed = profile.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("data.storage.type", StringComparison.OrdinalIgnoreCase))
            ?.Values
            ?.ToList()
            ?? new List<string> { "None", "LocalJson", "SQLite" };

        string Pick(params string[] preferred) =>
            preferred.FirstOrDefault(p => allowed.Contains(p, StringComparer.OrdinalIgnoreCase))
            ?? allowed[0];

        switch (answers.Memory)
        {
            case MemoryChoice.SimpleFiles:
                doc.Data.Storage.Type = Pick("LocalJson", "SQLite", "None");
                doc.Data.Storage.ConnectionStringMode = doc.Data.Storage.Type.Equals("LocalJson", StringComparison.OrdinalIgnoreCase)
                    ? "None"
                    : "ConfigFile";
                doc.Data.Storage.MigrationsRequired = doc.Data.Storage.Type is "SQLite" or "SQLServer" or "PostgreSQL";
                break;
            case MemoryChoice.Database:
                doc.Data.Storage.Type = Pick("SQLite", "SQLServer", "PostgreSQL", "LocalJson");
                doc.Data.Storage.ConnectionStringMode = doc.Data.Storage.Type.Equals("LocalJson", StringComparison.OrdinalIgnoreCase)
                    ? "None"
                    : "ConfigFile";
                doc.Data.Storage.MigrationsRequired = !doc.Data.Storage.Type.Equals("LocalJson", StringComparison.OrdinalIgnoreCase)
                                                     && !doc.Data.Storage.Type.Equals("None", StringComparison.OrdinalIgnoreCase);
                break;
            default:
                doc.Data.Storage.Type = Pick("None");
                doc.Data.Storage.ConnectionStringMode = "None";
                doc.Data.Storage.MigrationsRequired = false;
                break;
        }
    }

    private static void ApplyArchitecture(SsotDocument doc, FormSchemaDefinition profile)
    {
        doc.Architecture.Pattern = ResolvePattern(profile);
        doc.Architecture.Layers = new List<string> { "UI", "Application", "Domain", "Infrastructure" };
        doc.Architecture.Modules = new List<ModuleInfo>
        {
            new() { Name = "AppShell", Description = "Application entry and navigation shell." },
            new()
            {
                Name = "CoreLogic",
                Description = "Main product behavior.",
                Dependencies = new List<string> { "AppShell" }
            }
        };
    }

    private static string ResolvePattern(FormSchemaDefinition profile)
    {
        var field = profile.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("architecture.pattern", StringComparison.OrdinalIgnoreCase));
        return field?.Values?.FirstOrDefault() ?? "MVVM";
    }

    private static void ApplyCapabilities(SsotDocument doc, WizardAnswers answers, FormSchemaDefinition profile)
    {
        doc.Ui.Screens.Clear();
        doc.Domain.Entities.Clear();
        doc.Workflows.Clear();

        var controlTypes = ResolveControlTypes(profile);
        var buttonType = Pick(controlTypes, "Button", "Command", "Cmdlet", "Endpoint", "Stage", controlTypes.FirstOrDefault() ?? "Button");
        var inputType = Pick(controlTypes, "ComboBox", "TextBox", "Option", "Parameter", "Argument", buttonType);
        var listType = Pick(controlTypes, "ListView", "DataGrid", "PipelineInput", "Task", buttonType);

        var main = NewScreen("main", "Main", "/");
        doc.Ui.Screens.Add(main);

        // Always give the main surface at least one primary action for readiness.
        var hasAny = answers.Capabilities.Count > 0;
        if (!hasAny || answers.Capabilities.Contains(AppCapability.GenerateResults))
        {
            main.Controls.Add(NewControl("generateButton", buttonType, "Generate", "GenerateResults"));
            doc.Domain.Entities.Add(new EntityInfo
            {
                Name = "ResultItem",
                Description = "A generated result the user can view or save.",
                Fields =
                {
                    new EntityFieldInfo { Name = "Id", Type = "Guid", Required = true },
                    new EntityFieldInfo { Name = "DisplayText", Type = "string", Required = true },
                    new EntityFieldInfo { Name = "CreatedAt", Type = "DateTime", Required = true }
                }
            });
            doc.Workflows.Add(NewWorkflow(
                "generateResults",
                "Generate Results",
                "generateButton",
                "Read the user's current choices",
                "Create the result using the app rules",
                "Show the result on screen"));
        }

        if (answers.Capabilities.Contains(AppCapability.PickOptions))
        {
            main.Controls.Insert(0, NewControl("optionsPicker", inputType, "Options", "SelectOption"));
            doc.Domain.Entities.Add(new EntityInfo
            {
                Name = "OptionChoice",
                Description = "A selectable option the user can pick.",
                Fields =
                {
                    new EntityFieldInfo { Name = "Id", Type = "string", Required = true },
                    new EntityFieldInfo { Name = "Label", Type = "string", Required = true },
                    new EntityFieldInfo { Name = "IsSelected", Type = "bool", Required = false }
                }
            });
            doc.Workflows.Add(NewWorkflow(
                "selectOption",
                "Select Option",
                "optionsPicker",
                "Show available options",
                "Remember the user's selection",
                "Update the main screen"));
        }

        if (answers.Capabilities.Contains(AppCapability.SaveHistory)
            || answers.Capabilities.Contains(AppCapability.ReviewPastResults)
            || answers.Memory != MemoryChoice.None)
        {
            var history = NewScreen("history", "History", "/history");
            history.Controls.Add(NewControl("historyList", listType, "Past results", "ViewHistory"));
            history.Controls.Add(NewControl("saveButton", buttonType, "Save", "SaveHistory"));
            doc.Ui.Screens.Add(history);

            doc.Domain.Entities.Add(new EntityInfo
            {
                Name = "HistoryEntry",
                Description = "A saved record from a previous run.",
                Fields =
                {
                    new EntityFieldInfo { Name = "Id", Type = "Guid", Required = true },
                    new EntityFieldInfo { Name = "Summary", Type = "string", Required = true },
                    new EntityFieldInfo { Name = "SavedAt", Type = "DateTime", Required = true }
                }
            });

            doc.Workflows.Add(NewWorkflow(
                "saveHistory",
                "Save History",
                "saveButton",
                "Collect the current result",
                "Store it using the chosen memory setting",
                "Confirm it was saved"));
        }

        if (answers.Capabilities.Contains(AppCapability.SettingsScreen))
        {
            var settings = NewScreen("settings", "Settings", "/settings");
            settings.Controls.Add(NewControl("settingsSaveButton", buttonType, "Save settings", "SaveSettings"));
            doc.Ui.Screens.Add(settings);

            doc.Domain.Entities.Add(new EntityInfo
            {
                Name = "AppSettings",
                Description = "User preferences for the application.",
                Fields =
                {
                    new EntityFieldInfo { Name = "ThemePreference", Type = "string", Required = false },
                    new EntityFieldInfo { Name = "LastUsedOption", Type = "string", Required = false }
                }
            });

            doc.Workflows.Add(NewWorkflow(
                "saveSettings",
                "Save Settings",
                "settingsSaveButton",
                "Read settings from the screen",
                "Validate values",
                "Persist settings"));
        }

        // Ensure every workflow that needs a trigger has one matching a control.
        EnsureMinimumSurface(doc, buttonType);
    }

    private static void EnsureMinimumSurface(SsotDocument doc, string buttonType)
    {
        if (doc.Ui.Screens.Count == 0)
        {
            var main = NewScreen("main", "Main", "/");
            main.Controls.Add(NewControl("primaryButton", buttonType, "Start", "Start"));
            doc.Ui.Screens.Add(main);
        }

        if (doc.Ui.Screens.All(s => s.Controls.Count == 0))
        {
            doc.Ui.Screens[0].Controls.Add(NewControl("primaryButton", buttonType, "Start", "Start"));
        }

        if (doc.Domain.Entities.Count == 0)
        {
            doc.Domain.Entities.Add(new EntityInfo
            {
                Name = "AppItem",
                Description = "Primary information item for this app.",
                Fields =
                {
                    new EntityFieldInfo { Name = "Id", Type = "Guid", Required = true },
                    new EntityFieldInfo { Name = "Name", Type = "string", Required = true }
                }
            });
        }

        if (doc.Workflows.Count == 0)
        {
            var trigger = doc.Ui.Screens.SelectMany(s => s.Controls).Select(c => c.Id).FirstOrDefault() ?? "primaryButton";
            doc.Workflows.Add(NewWorkflow(
                "start",
                "Start",
                trigger,
                "Open the main screen",
                "Perform the primary action",
                "Show the outcome"));
        }

        foreach (var workflow in doc.Workflows.Where(w => string.IsNullOrWhiteSpace(w.Trigger)))
        {
            workflow.Trigger = doc.Ui.Screens.SelectMany(s => s.Controls).Select(c => c.Id).FirstOrDefault() ?? string.Empty;
        }

        foreach (var workflow in doc.Workflows.Where(w => w.Steps.Count == 0))
        {
            workflow.Steps.Add("Perform the main action");
            workflow.Steps.Add("Show the result to the user");
        }
    }

    private static void ApplyBuild(SsotDocument doc, FormSchemaDefinition profile)
    {
        doc.Build.OutputType = profile.Id switch
        {
            "web_api" or "cli_tool" or "winpe_tool" or "deployment_automation" => "Exe",
            "powershell_module" => "Library",
            _ => "WinExe"
        };
        doc.Build.Configuration = new List<string> { "Debug", "Release" };
        doc.Build.Targets = new List<BuildTargetInfo>
        {
            new() { Name = "win-x64", RuntimeIdentifier = "win-x64" }
        };
    }

    private static void ApplyTesting(SsotDocument doc, WizardAnswers answers)
    {
        doc.Testing.UnitTestFramework = answers.Language.Equals("PowerShell", StringComparison.OrdinalIgnoreCase)
            ? "Pester"
            : "xUnit";

        var desktop = answers.UiFramework.Equals("WPF", StringComparison.OrdinalIgnoreCase)
                      || answers.UiFramework.Equals("WinUI", StringComparison.OrdinalIgnoreCase);
        doc.Testing.UiTestFramework = desktop ? "FlaUI" : "None";
        doc.Testing.CoverageTarget = 0.6;
    }

    private static void ApplyConstraints(SsotDocument doc, FormSchemaDefinition profile)
    {
        doc.Constraints.LoggingRequired = true;
        doc.Constraints.TelemetryAllowed = false;
        doc.Constraints.ThirdPartyBootloadersAllowed = profile.Id.Equals("winpe_tool", StringComparison.OrdinalIgnoreCase);
        doc.Constraints.AllowedLibraries = profile.Id switch
        {
            "wpf_desktop" or "custom" => new List<string> { "CommunityToolkit.Mvvm", "Microsoft.Extensions.DependencyInjection" },
            _ => new List<string>()
        };
        doc.Constraints.ForbiddenLibraries = new List<string>();
    }

    private static void ApplyDocumentation(SsotDocument doc)
    {
        doc.Documentation.GenerateReadme = true;
        doc.Documentation.GenerateArchitectureDoc = true;
        doc.Documentation.Sections = new List<string> { "Overview", "Architecture", "Workflows", "Entities" };
    }

    private static List<string> ResolveControlTypes(FormSchemaDefinition profile)
    {
        var nested = profile.Sections
            .SelectMany(s => s.Fields)
            .Where(f => f.Path.Equals("ui.screens", StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => f.ItemSchema ?? new List<FieldDefinition>())
            .Where(f => f.Path.Equals("controls", StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => f.ItemSchema ?? new List<FieldDefinition>())
            .FirstOrDefault(f => f.Path.Equals("type", StringComparison.OrdinalIgnoreCase));

        return nested?.Values?.ToList() ?? new List<string> { "Button", "TextBox", "ComboBox", "ListView" };
    }

    private static string Pick(IReadOnlyList<string> available, params string[] preferred)
    {
        foreach (var p in preferred)
        {
            var match = available.FirstOrDefault(a => a.Equals(p, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }

        return available.FirstOrDefault() ?? preferred[0];
    }

    private static ScreenInfo NewScreen(string id, string title, string route) => new()
    {
        Id = id,
        Title = title,
        Route = route,
        Controls = new List<ControlInfo>()
    };

    private static ControlInfo NewControl(string id, string type, string label, string action) => new()
    {
        Id = id,
        Type = type,
        Label = label,
        Action = action
    };

    private static WorkflowInfo NewWorkflow(string id, string name, string trigger, params string[] steps) => new()
    {
        Id = id,
        Name = name,
        Trigger = trigger,
        Steps = steps.ToList(),
        ErrorHandling = new ErrorHandlingInfo
        {
            Strategy = "ShowMessageBox",
            LogLevel = "Warning"
        }
    };
}
