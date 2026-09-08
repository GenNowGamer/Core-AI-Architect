using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Wizard;

public sealed class CompatibilityIssue
{
    public string Message { get; init; } = string.Empty;
    public string? SuggestedFix { get; init; }
    public IReadOnlyList<string> AllowedAlternatives { get; init; } = Array.Empty<string>();
}

public sealed class CompatibilityResult
{
    public bool IsCompatible => Issues.Count == 0;
    public List<CompatibilityIssue> Issues { get; } = new();
}

/// <summary>
/// Hard-stop compatibility rules between wizard/profile choices.
/// </summary>
public sealed class CompatibilityEngine
{
    public CompatibilityResult Evaluate(WizardAnswers answers, FormSchemaDefinition? profile = null)
    {
        var result = new CompatibilityResult();

        EvaluateLanguageUi(answers, result);
        EvaluateNavigation(answers, profile, result);
        EvaluateTheme(answers, profile, result);
        EvaluateMemory(answers, result);
        EvaluateTestingHints(answers, result);

        return result;
    }

    public CompatibilityResult EvaluateDocument(SsotDocument document)
    {
        var answers = new WizardAnswers
        {
            TargetOs = document.Environment.TargetOs,
            Runtime = document.Environment.Runtime,
            UiFramework = document.Environment.UiFramework,
            Language = document.Environment.Language,
            Theme = document.Ui.Theme,
            NavigationPattern = document.Ui.NavigationPattern,
            Memory = document.Data.Storage.Type switch
            {
                "LocalJson" or "Registry" => MemoryChoice.SimpleFiles,
                "SQLite" or "SQLServer" or "PostgreSQL" => MemoryChoice.Database,
                _ => MemoryChoice.None
            }
        };

        var result = Evaluate(answers);
        EvaluateTriggerBindings(document, result);
        EvaluateDesktopPlaywright(document, result);
        EvaluateAndroidDesktopTests(document, result);
        return result;
    }

    public IReadOnlyList<string> AllowedNavigationPatterns(string profileId, string uiFramework, FormSchemaDefinition? profile)
    {
        var fromSchema = profile?.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("ui.navigationPattern", StringComparison.OrdinalIgnoreCase))
            ?.Values;

        if (fromSchema is { Count: > 0 })
            return fromSchema;

        return uiFramework.ToLowerInvariant() switch
        {
            "wpf" => new[] { "Window+Dialogs", "NavigationFrame", "Tabbed" },
            "winui" => new[] { "NavigationFrame", "Tabbed" },
            "jetpack compose" => new[] { "BottomNavigation", "NavigationCompose", "SingleActivity", "Tabbed" },
            "android views" => new[] { "BottomNavigation", "SingleActivity", "Tabbed" },
            ".net maui" => new[] { "BottomNavigation", "Tabbed", "SingleActivity" },
            "asp.net" => new[] { "HttpRoutes" },
            "console" => new[] { "CommandLine" },
            "powershell" => new[] { "CmdletPipeline" },
            "none" => new[] { "Custom" },
            _ => profileId switch
            {
                "android_app" => new[] { "BottomNavigation", "NavigationCompose", "SingleActivity", "Tabbed" },
                "winpe_tool" => new[] { "Wizard", "CommandLine", "Menu" },
                "deployment_automation" => new[] { "Pipeline" },
                _ => new[] { "Window+Dialogs", "Custom" }
            }
        };
    }

    public IReadOnlyList<string> AllowedThemes(FormSchemaDefinition? profile)
    {
        var fromSchema = profile?.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("ui.theme", StringComparison.OrdinalIgnoreCase))
            ?.Values;
        return fromSchema is { Count: > 0 } ? fromSchema : new[] { "Default", "Fluent", "Custom" };
    }

    public void ApplySafeDefaults(WizardAnswers answers, FormSchemaDefinition profile)
    {
        answers.ProfileId = profile.Id;
        answers.TargetOs = profile.EnvironmentDefaults.TargetOs;
        answers.Runtime = profile.EnvironmentDefaults.Runtime;
        answers.UiFramework = profile.EnvironmentDefaults.UiFramework;
        answers.Language = profile.EnvironmentDefaults.Language;

        var themes = AllowedThemes(profile);
        if (!themes.Contains(answers.Theme, StringComparer.OrdinalIgnoreCase))
            answers.Theme = themes[0];

        var navs = AllowedNavigationPatterns(profile.Id, answers.UiFramework, profile);
        if (!navs.Contains(answers.NavigationPattern, StringComparer.OrdinalIgnoreCase))
            answers.NavigationPattern = navs[0];
    }

    private static void EvaluateLanguageUi(WizardAnswers answers, CompatibilityResult result)
    {
        if (answers.Language.Equals("PowerShell", StringComparison.OrdinalIgnoreCase)
            && (answers.UiFramework.Equals("WPF", StringComparison.OrdinalIgnoreCase)
                || answers.UiFramework.Equals("WinUI", StringComparison.OrdinalIgnoreCase)
                || answers.UiFramework.Equals("ASP.NET", StringComparison.OrdinalIgnoreCase)
                || answers.UiFramework.Equals("Jetpack Compose", StringComparison.OrdinalIgnoreCase)
                || answers.UiFramework.Equals("Android Views", StringComparison.OrdinalIgnoreCase)))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "PowerShell language does not mix with a desktop, web, or Android UI framework.",
                SuggestedFix = "Pick the PowerShell module app kind, or switch language to C# / Kotlin.",
                AllowedAlternatives = new[] { "Change language to C#", "Change language to Kotlin", "Choose PowerShell module app kind" }
            });
        }

        if ((answers.Language.Equals("Kotlin", StringComparison.OrdinalIgnoreCase)
             || answers.Language.Equals("Java", StringComparison.OrdinalIgnoreCase))
            && (answers.UiFramework.Equals("WPF", StringComparison.OrdinalIgnoreCase)
                || answers.UiFramework.Equals("WinUI", StringComparison.OrdinalIgnoreCase)
                || answers.UiFramework.Equals("ASP.NET", StringComparison.OrdinalIgnoreCase)))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "Kotlin/Java language does not mix with Windows desktop or ASP.NET UI frameworks.",
                SuggestedFix = "Choose the Android app kind, or switch language to C#.",
                AllowedAlternatives = new[] { "Choose Android app kind", "Change language to C#" }
            });
        }

        if (answers.ProfileId.Equals("android_app", StringComparison.OrdinalIgnoreCase)
            && !IsAndroidUi(answers.UiFramework))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "An Android app should use an Android UI framework.",
                SuggestedFix = "Use Jetpack Compose, Android Views, or .NET MAUI.",
                AllowedAlternatives = new[] { "Jetpack Compose", "Android Views", ".NET MAUI" }
            });
        }

        if (answers.ProfileId.Equals("powershell_module", StringComparison.OrdinalIgnoreCase)
            && !answers.UiFramework.Equals("PowerShell", StringComparison.OrdinalIgnoreCase)
            && !answers.UiFramework.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "A PowerShell module should use the PowerShell UI surface.",
                SuggestedFix = "Use UI framework PowerShell (or None).",
                AllowedAlternatives = new[] { "PowerShell", "None" }
            });
        }
    }

    private static bool IsAndroidUi(string uiFramework) =>
        uiFramework.Equals("Jetpack Compose", StringComparison.OrdinalIgnoreCase)
        || uiFramework.Equals("Android Views", StringComparison.OrdinalIgnoreCase)
        || uiFramework.Equals(".NET MAUI", StringComparison.OrdinalIgnoreCase);

    private void EvaluateNavigation(WizardAnswers answers, FormSchemaDefinition? profile, CompatibilityResult result)
    {
        var allowed = AllowedNavigationPatterns(answers.ProfileId, answers.UiFramework, profile);
        if (!allowed.Contains(answers.NavigationPattern, StringComparer.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = $"“{answers.NavigationPattern}” does not work with {answers.UiFramework}.",
                SuggestedFix = $"Choose one of: {string.Join(", ", allowed)}.",
                AllowedAlternatives = allowed.ToList()
            });
        }

        if (answers.UiFramework.Equals("WinUI", StringComparison.OrdinalIgnoreCase)
            && answers.NavigationPattern.Equals("Window+Dialogs", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "WinUI apps should not use Window+Dialogs navigation.",
                SuggestedFix = "Use NavigationFrame or Tabbed instead.",
                AllowedAlternatives = new[] { "NavigationFrame", "Tabbed" }
            });
        }

        if (answers.UiFramework.Equals("ASP.NET", StringComparison.OrdinalIgnoreCase)
            && !answers.NavigationPattern.Equals("HttpRoutes", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "Website APIs should use HttpRoutes for how users reach features.",
                SuggestedFix = "Set navigation to HttpRoutes.",
                AllowedAlternatives = new[] { "HttpRoutes" }
            });
        }
    }

    private void EvaluateTheme(WizardAnswers answers, FormSchemaDefinition? profile, CompatibilityResult result)
    {
        var allowed = AllowedThemes(profile);
        if (!allowed.Contains(answers.Theme, StringComparer.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = $"Theme “{answers.Theme}” is not available for this app kind.",
                SuggestedFix = $"Choose one of: {string.Join(", ", allowed)}.",
                AllowedAlternatives = allowed.ToList()
            });
        }
    }

    private static void EvaluateMemory(WizardAnswers answers, CompatibilityResult result)
    {
        if (answers.Memory == MemoryChoice.Database
            && answers.ProfileId.Equals("powershell_module", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "PowerShell modules usually should not require a full database.",
                SuggestedFix = "Pick “Simple files” or “Nothing to remember”.",
                AllowedAlternatives = new[] { "None", "SimpleFiles" }
            });
        }
    }

    private static void EvaluateTestingHints(WizardAnswers answers, CompatibilityResult result)
    {
        // Documented for EvaluateDocument; wizard forces safe test defaults in smart builder.
        _ = answers;
        _ = result;
    }

    private static void EvaluateTriggerBindings(SsotDocument document, CompatibilityResult result)
    {
        var controlIds = document.Ui.Screens
            .SelectMany(s => s.Controls)
            .Select(c => c.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (controlIds.Count == 0) return;

        foreach (var workflow in document.Workflows)
        {
            if (string.IsNullOrWhiteSpace(workflow.Trigger)) continue;
            var trigger = workflow.Trigger;
            if (trigger.StartsWith("UI:", StringComparison.OrdinalIgnoreCase))
                trigger = trigger[3..];

            if (!controlIds.Contains(trigger))
            {
                result.Issues.Add(new CompatibilityIssue
                {
                    Message = $"“{workflow.Trigger}” is not one of your screen buttons/controls, so workflow “{workflow.Name}” cannot start correctly.",
                    SuggestedFix = "Pick a trigger from the list of buttons you created.",
                    AllowedAlternatives = controlIds.OrderBy(x => x).ToList()
                });
            }
        }
    }

    private static void EvaluateDesktopPlaywright(SsotDocument document, CompatibilityResult result)
    {
        var ui = document.Environment.UiFramework;
        var isDesktop = ui.Equals("WPF", StringComparison.OrdinalIgnoreCase)
                        || ui.Equals("WinUI", StringComparison.OrdinalIgnoreCase);
        if (isDesktop && document.Testing.UiTestFramework.Equals("Playwright", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "Playwright is for websites, not desktop window apps.",
                SuggestedFix = "Use FlaUI, WinAppDriver, or None.",
                AllowedAlternatives = new[] { "None", "FlaUI", "WinAppDriver" }
            });
        }
    }

    private static void EvaluateAndroidDesktopTests(SsotDocument document, CompatibilityResult result)
    {
        var ui = document.Environment.UiFramework;
        var isAndroid = ui.Equals("Jetpack Compose", StringComparison.OrdinalIgnoreCase)
                        || ui.Equals("Android Views", StringComparison.OrdinalIgnoreCase)
                        || ui.Equals(".NET MAUI", StringComparison.OrdinalIgnoreCase);
        if (!isAndroid) return;

        var framework = document.Testing.UiTestFramework;
        if (framework.Equals("FlaUI", StringComparison.OrdinalIgnoreCase)
            || framework.Equals("WinAppDriver", StringComparison.OrdinalIgnoreCase)
            || framework.Equals("Playwright", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Message = "Windows or web UI test frameworks do not apply to Android apps.",
                SuggestedFix = "Use Espresso, ComposeUiTest, or None.",
                AllowedAlternatives = new[] { "None", "Espresso", "ComposeUiTest" }
            });
        }
    }
}
