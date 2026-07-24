using CoreAiArchitect.Core.Documents;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Wizard;

/// <summary>
/// Compiles a screen-first storyboard into a complete agent-ready SSOT document.
/// Screens and button steps are the source of truth; entities/workflows are derived.
/// </summary>
public sealed class BlueprintCompiler
{
    private readonly CompatibilityEngine _compatibility = new();

    /// <summary>Create a new document from answers, optionally seeding a blank main screen.</summary>
    public SsotDocument BuildNew(WizardAnswers answers, FormSchemaDefinition profile, bool seedMainScreen = true)
    {
        _compatibility.ApplySafeDefaults(answers, profile);
        var doc = SsotDocumentFactory.CreateNew(profile);
        ApplyShell(doc, answers, profile);

        if (seedMainScreen)
        {
            doc.Ui.Screens.Add(NewScreen("main", "Main", "/"));
        }

        ApplyRememberScreens(doc, answers, profile);
        DeriveEntitiesAndWorkflows(doc, profile);
        return SsotDocumentNormalizer.Normalize(doc);
    }

    /// <summary>Recompile metadata, remember screens, entities, and workflows while keeping user screens/steps.</summary>
    public SsotDocument Compile(SsotDocument document, WizardAnswers answers, FormSchemaDefinition profile)
    {
        _compatibility.ApplySafeDefaults(answers, profile);
        ApplyShell(document, answers, profile);
        ApplyRememberScreens(document, answers, profile);
        DeriveEntitiesAndWorkflows(document, profile);
        return SsotDocumentNormalizer.Normalize(document);
    }

    public static ControlInfo CreateThing(BlueprintThingKind kind, IReadOnlyList<string> controlTypes, int index)
    {
        var (type, label, action, idBase) = kind switch
        {
            BlueprintThingKind.ChoiceList => (
                Pick(controlTypes, "ComboBox", "Option", "Parameter", "TextBox"),
                "Choice list",
                "SelectOption",
                "choiceList"),
            BlueprintThingKind.PlaceToType => (
                Pick(controlTypes, "TextBox", "Parameter", "Argument", "ComboBox"),
                "Place to type",
                "SetValue",
                "textBox"),
            BlueprintThingKind.Button => (
                Pick(controlTypes, "Button", "Command", "Cmdlet", "Endpoint", "Stage"),
                "Button",
                "DoAction",
                "button"),
            BlueprintThingKind.ResultsList => (
                Pick(controlTypes, "ListView", "DataGrid", "PipelineInput", "Task"),
                "Results",
                "ViewResults",
                "resultsList"),
            _ => ("Button", "Button", "DoAction", "button")
        };

        return new ControlInfo
        {
            Id = $"{idBase}{index}",
            Type = type,
            Label = $"{label} {index}",
            Action = action
        };
    }

    public static string ScreenCaption(ScreenInfo? screen)
    {
        if (screen is null || screen.Controls.Count == 0)
            return "This screen is empty. Add things people will see and use.";

        var parts = new List<string>();
        foreach (var c in screen.Controls)
        {
            var label = string.IsNullOrWhiteSpace(c.Label) ? c.Id : c.Label;
            if (IsButton(c))
                parts.Add($"press “{label}”");
            else if (IsChoice(c))
                parts.Add($"pick from “{label}”");
            else if (IsList(c))
                parts.Add($"see results in “{label}”");
            else
                parts.Add($"type into “{label}”");
        }

        if (parts.Count == 1)
            return $"People {parts[0]}.";
        if (parts.Count == 2)
            return $"People {parts[0]}, then {parts[1]}.";
        return $"People {string.Join(", ", parts.Take(parts.Count - 1))}, then {parts[^1]}.";
    }

    public static string BuildAppStory(SsotDocument doc, WizardAnswers answers)
    {
        var name = string.IsNullOrWhiteSpace(answers.ProjectName) ? "This app" : answers.ProjectName.Trim();
        var lines = new List<string>
        {
            $"{name} is a {WizardAnswers.ProfileLabel(answers.ProfileId).ToLowerInvariant()}."
        };

        if (!string.IsNullOrWhiteSpace(answers.Description))
            lines.Add(answers.Description.Trim());

        foreach (var screen in doc.Ui.Screens)
        {
            lines.Add($"On “{screen.Title}”: {ScreenCaption(screen)}");
        }

        foreach (var workflow in doc.Workflows)
        {
            var button = doc.Ui.Screens.SelectMany(s => s.Controls)
                .FirstOrDefault(c => c.Id.Equals(workflow.Trigger, StringComparison.OrdinalIgnoreCase));
            var buttonLabel = button?.Label ?? workflow.Trigger;
            var steps = workflow.Steps.Count == 0
                ? "do the main work"
                : string.Join(" → ", workflow.Steps);
            lines.Add($"When someone uses “{buttonLabel}”: {steps}.");
        }

        if (answers.SavePastResults)
            lines.Add("The app can save past results for later.");
        if (answers.IncludeSettingsScreen)
            lines.Add("The app includes a settings screen.");

        return string.Join(Environment.NewLine + Environment.NewLine, lines);
    }

    private void ApplyShell(SsotDocument doc, WizardAnswers answers, FormSchemaDefinition profile)
    {
        doc.Project.Id = answers.DerivedProjectId;
        doc.Project.Name = answers.ProjectName.Trim();
        doc.Project.Description = answers.Description.Trim();
        doc.Project.Owner = answers.Owner.Trim();
        doc.Project.Tags = new List<string> { answers.ProfileId.Replace('_', '-') };

        doc.Environment.TargetOs = answers.TargetOs;
        doc.Environment.Runtime = answers.Runtime;
        doc.Environment.UiFramework = answers.UiFramework;
        doc.Environment.Language = answers.Language;

        var themes = _compatibility.AllowedThemes(profile);
        doc.Ui.Theme = themes.Contains(answers.Theme, StringComparer.OrdinalIgnoreCase)
            ? answers.Theme
            : themes[0];

        var navs = _compatibility.AllowedNavigationPatterns(profile.Id, answers.UiFramework, profile);
        doc.Ui.NavigationPattern = navs.Contains(answers.NavigationPattern, StringComparer.OrdinalIgnoreCase)
            ? answers.NavigationPattern
            : navs[0];

        ApplyMemory(doc, answers, profile);
        ApplyArchitecture(doc, profile);
        ApplyBuild(doc, profile);
        ApplyTesting(doc, answers);
        ApplyConstraints(doc, profile);
        ApplyDocumentation(doc);
    }

    private static void ApplyMemory(SsotDocument doc, WizardAnswers answers, FormSchemaDefinition profile)
    {
        if (answers.SavePastResults && answers.Memory == MemoryChoice.None)
            answers.Memory = MemoryChoice.SimpleFiles;

        var allowed = profile.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("data.storage.type", StringComparison.OrdinalIgnoreCase))
            ?.Values
            ?.ToList()
            ?? new List<string> { "None", "LocalJson", "SQLite" };

        string PickStorage(params string[] preferred) =>
            preferred.FirstOrDefault(p => allowed.Contains(p, StringComparer.OrdinalIgnoreCase))
            ?? allowed[0];

        switch (answers.Memory)
        {
            case MemoryChoice.SimpleFiles:
                doc.Data.Storage.Type = PickStorage("LocalJson", "SQLite", "None");
                doc.Data.Storage.ConnectionStringMode = "None";
                doc.Data.Storage.MigrationsRequired = false;
                break;
            case MemoryChoice.Database:
                doc.Data.Storage.Type = PickStorage("SQLite", "SQLServer", "PostgreSQL", "LocalJson");
                doc.Data.Storage.ConnectionStringMode = "ConfigFile";
                doc.Data.Storage.MigrationsRequired = true;
                break;
            default:
                doc.Data.Storage.Type = PickStorage("None");
                doc.Data.Storage.ConnectionStringMode = "None";
                doc.Data.Storage.MigrationsRequired = false;
                break;
        }
    }

    private void ApplyRememberScreens(SsotDocument doc, WizardAnswers answers, FormSchemaDefinition profile)
    {
        var types = ResolveControlTypes(profile);
        var buttonType = Pick(types, "Button", "Command", "Cmdlet", "Endpoint", "Stage");
        var listType = Pick(types, "ListView", "DataGrid", "PipelineInput", "Task");

        if (answers.SavePastResults)
        {
            var history = doc.Ui.Screens.FirstOrDefault(s => s.Id.Equals("history", StringComparison.OrdinalIgnoreCase));
            if (history is null)
            {
                history = NewScreen("history", "History", "/history");
                history.Controls.Add(new ControlInfo
                {
                    Id = "historyList",
                    Type = listType,
                    Label = "Past results",
                    Action = "ViewHistory"
                });
                history.Controls.Add(new ControlInfo
                {
                    Id = "saveButton",
                    Type = buttonType,
                    Label = "Save",
                    Action = "SaveHistory"
                });
                doc.Ui.Screens.Add(history);
            }
        }
        else
        {
            doc.Ui.Screens.RemoveAll(s => s.Id.Equals("history", StringComparison.OrdinalIgnoreCase));
        }

        if (answers.IncludeSettingsScreen)
        {
            var settings = doc.Ui.Screens.FirstOrDefault(s => s.Id.Equals("settings", StringComparison.OrdinalIgnoreCase));
            if (settings is null)
            {
                settings = NewScreen("settings", "Settings", "/settings");
                settings.Controls.Add(new ControlInfo
                {
                    Id = "settingsSaveButton",
                    Type = buttonType,
                    Label = "Save settings",
                    Action = "SaveSettings"
                });
                doc.Ui.Screens.Add(settings);
            }
        }
        else
        {
            doc.Ui.Screens.RemoveAll(s => s.Id.Equals("settings", StringComparison.OrdinalIgnoreCase));
        }

        if (doc.Ui.Screens.Count == 0)
            doc.Ui.Screens.Add(NewScreen("main", "Main", "/"));
    }

    private void DeriveEntitiesAndWorkflows(SsotDocument doc, FormSchemaDefinition profile)
    {
        var existingWorkflowSteps = doc.Workflows
            .Where(w => !string.IsNullOrWhiteSpace(w.Trigger))
            .ToDictionary(w => w.Trigger, w => w.Steps.ToList(), StringComparer.OrdinalIgnoreCase);

        doc.Domain.Entities.Clear();
        doc.Workflows.Clear();

        var allControls = doc.Ui.Screens.SelectMany(s => s.Controls).ToList();
        var hasChoice = allControls.Any(IsChoice);
        var hasResults = allControls.Any(IsList);
        var hasText = allControls.Any(c => !IsButton(c) && !IsChoice(c) && !IsList(c));
        var buttons = allControls.Where(IsButton).ToList();

        if (hasChoice)
        {
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
        }

        if (hasText)
        {
            doc.Domain.Entities.Add(new EntityInfo
            {
                Name = "UserInput",
                Description = "Values the user types on screen (counts, names, notes).",
                Fields =
                {
                    new EntityFieldInfo { Name = "FieldName", Type = "string", Required = true },
                    new EntityFieldInfo { Name = "Value", Type = "string", Required = true }
                }
            });
        }

        if (hasResults || buttons.Count > 0)
        {
            doc.Domain.Entities.Add(new EntityInfo
            {
                Name = "ResultItem",
                Description = "A generated or listed result shown to the user.",
                Fields =
                {
                    new EntityFieldInfo { Name = "Id", Type = "Guid", Required = true },
                    new EntityFieldInfo { Name = "DisplayText", Type = "string", Required = true },
                    new EntityFieldInfo { Name = "CreatedAt", Type = "DateTime", Required = true }
                }
            });
        }

        if (doc.Ui.Screens.Any(s => s.Id.Equals("history", StringComparison.OrdinalIgnoreCase)))
        {
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
        }

        if (doc.Ui.Screens.Any(s => s.Id.Equals("settings", StringComparison.OrdinalIgnoreCase)))
        {
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

        foreach (var button in buttons)
        {
            var steps = existingWorkflowSteps.TryGetValue(button.Id, out var saved) && saved.Count > 0
                ? saved
                : DefaultStepsForButton(button, allControls);

            doc.Workflows.Add(new WorkflowInfo
            {
                Id = slugify(button.Id),
                Name = string.IsNullOrWhiteSpace(button.Label) ? button.Id : button.Label,
                Trigger = button.Id,
                Steps = steps,
                ErrorHandling = new ErrorHandlingInfo
                {
                    Strategy = "ShowMessageBox",
                    LogLevel = "Warning"
                }
            });
        }

        if (doc.Workflows.Count == 0 && allControls.Count > 0)
        {
            var trigger = allControls[0].Id;
            doc.Workflows.Add(new WorkflowInfo
            {
                Id = "start",
                Name = "Start",
                Trigger = trigger,
                Steps = { "Open the main screen", "Perform the primary action", "Show the outcome" },
                ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
            });
        }

        if (doc.Ui.Screens.All(s => s.Controls.Count == 0))
        {
            var types = ResolveControlTypes(profile);
            var buttonType = Pick(types, "Button", "Command", "Cmdlet", "Endpoint");
            doc.Ui.Screens[0].Controls.Add(new ControlInfo
            {
                Id = "primaryButton",
                Type = buttonType,
                Label = "Start",
                Action = "Start"
            });
            if (doc.Workflows.Count == 0)
            {
                doc.Workflows.Add(new WorkflowInfo
                {
                    Id = "start",
                    Name = "Start",
                    Trigger = "primaryButton",
                    Steps = { "Open the main screen", "Perform the primary action", "Show the outcome" },
                    ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
                });
            }
        }
    }

    private static List<string> DefaultStepsForButton(ControlInfo button, IReadOnlyList<ControlInfo> all)
    {
        var label = (button.Label ?? string.Empty).ToLowerInvariant();
        var hasChoice = all.Any(IsChoice);
        var hasText = all.Any(c => !IsButton(c) && !IsChoice(c) && !IsList(c));
        var hasResults = all.Any(IsList);

        if (label.Contains("generate") || label.Contains("create") || button.Action.Contains("Generate", StringComparison.OrdinalIgnoreCase))
        {
            var steps = new List<string>();
            if (hasChoice) steps.Add("Read the selected option");
            if (hasText) steps.Add("Read what the user typed (such as a count)");
            steps.Add("Create results using the app rules");
            steps.Add(hasResults ? "Show the results in the results list" : "Show the results on screen");
            return steps;
        }

        if (label.Contains("save", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                "Collect the current result",
                "Store it using the chosen memory setting",
                "Confirm it was saved"
            };
        }

        return new List<string>
        {
            "Read the user's current choices",
            "Do the action for “" + (string.IsNullOrWhiteSpace(button.Label) ? button.Id : button.Label) + "”",
            "Show the outcome on screen"
        };
    }

    private static void ApplyArchitecture(SsotDocument doc, FormSchemaDefinition profile)
    {
        doc.Architecture.Pattern = profile.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("architecture.pattern", StringComparison.OrdinalIgnoreCase))
            ?.Values?.FirstOrDefault() ?? "MVVM";
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

    public static List<string> ResolveControlTypes(FormSchemaDefinition profile)
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

    private static bool IsButton(ControlInfo c) =>
        c.Type.Equals("Button", StringComparison.OrdinalIgnoreCase)
        || c.Type.Equals("Command", StringComparison.OrdinalIgnoreCase)
        || c.Type.Equals("Cmdlet", StringComparison.OrdinalIgnoreCase)
        || c.Type.Equals("Endpoint", StringComparison.OrdinalIgnoreCase)
        || c.Type.Equals("Stage", StringComparison.OrdinalIgnoreCase);

    private static bool IsChoice(ControlInfo c) =>
        c.Type.Equals("ComboBox", StringComparison.OrdinalIgnoreCase)
        || c.Type.Equals("Option", StringComparison.OrdinalIgnoreCase);

    private static bool IsList(ControlInfo c) =>
        c.Type.Equals("ListView", StringComparison.OrdinalIgnoreCase)
        || c.Type.Equals("DataGrid", StringComparison.OrdinalIgnoreCase)
        || c.Type.Equals("PipelineInput", StringComparison.OrdinalIgnoreCase);

    private static ScreenInfo NewScreen(string id, string title, string route) => new()
    {
        Id = id,
        Title = title,
        Route = route,
        Controls = new List<ControlInfo>()
    };

    private static string slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(slug.Trim('-')) ? "action" : slug.Trim('-');
    }
}
