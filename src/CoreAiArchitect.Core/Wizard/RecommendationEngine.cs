using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Wizard;

public sealed class BlueprintRecommendation
{
    public string RuleId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Why { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string SuggestedValue { get; init; } = string.Empty;
    public string ApplyKind { get; init; } = string.Empty;
    public bool IsApplied { get; set; }
    public bool CanApply { get; init; } = true;
}

/// <summary>Deterministic recommendations with transparent rule IDs (no AI).</summary>
public sealed class RecommendationEngine
{
    public IReadOnlyList<BlueprintRecommendation> Evaluate(SsotDocument? document, WizardAnswers answers)
    {
        var list = new List<BlueprintRecommendation>();

        RecommendStorage(document, answers, list);
        RecommendArchitecture(document, answers, list);
        RecommendTheme(document, answers, list);
        RecommendBackup(document, answers, list);
        RecommendAccountsConflict(answers, list);

        return list;
    }

    public void Apply(BlueprintRecommendation recommendation, WizardAnswers answers, SsotDocument? document)
    {
        switch (recommendation.ApplyKind)
        {
            case "storage-sqlite":
                answers.Memory = MemoryChoice.Database;
                answers.SavePastResults = true;
                answers.StorageDecision = TechnicalDecisionMode.Recommended;
                if (document is not null)
                {
                    document.Data.Storage.Type = "SQLite";
                    document.Data.Storage.ConnectionStringMode = "ConfigFile";
                    document.Data.Storage.MigrationsRequired = true;
                    SetDecision(document.Data.Storage.Decision, "recommended", recommendation);
                }
                break;

            case "storage-localjson":
                answers.Memory = MemoryChoice.SimpleFiles;
                answers.SavePastResults = true;
                answers.StorageDecision = TechnicalDecisionMode.Recommended;
                if (document is not null)
                {
                    document.Data.Storage.Type = "LocalJson";
                    document.Data.Storage.ConnectionStringMode = "None";
                    document.Data.Storage.MigrationsRequired = false;
                    SetDecision(document.Data.Storage.Decision, "recommended", recommendation);
                }
                break;

            case "architecture-mvvm":
                answers.ArchitectureDecision = TechnicalDecisionMode.Recommended;
                if (document is not null)
                {
                    document.Architecture.Pattern = "MVVM";
                    SetDecision(document.Architecture.PatternDecision, "recommended", recommendation);
                }
                break;

            case "theme-fluent":
                answers.Theme = "Fluent";
                answers.LookStyle = "Modern";
                if (document is not null)
                    document.Ui.Theme = "Fluent";
                break;

            case "backup-feature":
                if (document is null) break;
                var main = document.Ui.Screens.FirstOrDefault(s =>
                    !s.Id.Equals("history", StringComparison.OrdinalIgnoreCase)
                    && !s.Id.Equals("settings", StringComparison.OrdinalIgnoreCase));
                if (main is null) break;
                if (main.Controls.Any(c => c.Id.Equals("backupExport", StringComparison.OrdinalIgnoreCase)))
                    break;
                main.Controls.Add(new ControlInfo
                {
                    Id = "backupExport",
                    Type = "Button",
                    Label = "Backup / Export",
                    Action = "ExportBackup"
                });
                if (!document.Workflows.Any(w => w.Trigger.Equals("backupExport", StringComparison.OrdinalIgnoreCase)))
                {
                    document.Workflows.Add(new WorkflowInfo
                    {
                        Id = "backupExport",
                        Name = "Backup / Export",
                        Trigger = "backupExport",
                        Steps =
                        {
                            "Collect user-created data from local storage",
                            "Write a backup/export file the user can save",
                            "Confirm success on screen"
                        },
                        ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
                    });
                }
                break;
        }

        recommendation.IsApplied = true;
    }

    private static void RecommendStorage(SsotDocument? document, WizardAnswers answers, List<BlueprintRecommendation> list)
    {
        var needsPersist = answers.SavePastResults
                           || answers.GetFeatureLines().Any(f =>
                               ContainsAny(f, "save", "history", "inventory", "backup", "store", "remember"))
                           || answers.MultiDevice != MultiDeviceChoice.No;

        if (!needsPersist && answers.Memory == MemoryChoice.None)
            return;

        var current = document?.Data.Storage.Type ?? "None";
        if (answers.Connectivity == ConnectivityChoice.Offline
            && answers.MultiDevice == MultiDeviceChoice.No
            && !string.Equals(current, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new BlueprintRecommendation
            {
                RuleId = "STORAGE-OFFLINE-STRUCTURED-001",
                Title = "Local database recommended",
                Why = "Your app stores information and works offline on a single device. SQLite fits structured local data without requiring a server.",
                Category = "Storage",
                SuggestedValue = "SQLite",
                ApplyKind = "storage-sqlite",
                IsApplied = string.Equals(current, "SQLite", StringComparison.OrdinalIgnoreCase)
            });
        }
        else if (!string.Equals(current, "LocalJson", StringComparison.OrdinalIgnoreCase)
                 && !string.Equals(current, "SQLite", StringComparison.OrdinalIgnoreCase)
                 && answers.Memory != MemoryChoice.None)
        {
            list.Add(new BlueprintRecommendation
            {
                RuleId = "STORAGE-SIMPLE-FILES-002",
                Title = "Simple local files recommended",
                Why = "You asked the app to remember things without multi-device sync. Local JSON files keep the design small and easy to back up.",
                Category = "Storage",
                SuggestedValue = "LocalJson",
                ApplyKind = "storage-localjson",
                IsApplied = string.Equals(current, "LocalJson", StringComparison.OrdinalIgnoreCase)
            });
        }
    }

    private static void RecommendArchitecture(SsotDocument? document, WizardAnswers answers, List<BlueprintRecommendation> list)
    {
        var desktop = answers.UiFramework.Equals("WPF", StringComparison.OrdinalIgnoreCase)
                      || answers.UiFramework.Equals("WinUI", StringComparison.OrdinalIgnoreCase);
        if (!desktop) return;

        var screenCount = document?.Ui.Screens.Count ?? 0;
        var pattern = document?.Architecture.Pattern ?? string.Empty;
        if (screenCount >= 1 && !pattern.Equals("MVVM", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new BlueprintRecommendation
            {
                RuleId = "ARCH-DESKTOP-MVVM-001",
                Title = "MVVM architecture recommended",
                Why = "This is a desktop app with screens and persistent UI state. MVVM separates the interface from application logic and works well with WPF/WinUI.",
                Category = "Architecture",
                SuggestedValue = "MVVM",
                ApplyKind = "architecture-mvvm",
                IsApplied = pattern.Equals("MVVM", StringComparison.OrdinalIgnoreCase)
            });
        }
        else if (pattern.Equals("MVVM", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new BlueprintRecommendation
            {
                RuleId = "ARCH-DESKTOP-MVVM-001",
                Title = "MVVM architecture recommended",
                Why = "This is a desktop app with screens and persistent UI state. MVVM separates the interface from application logic and works well with WPF/WinUI.",
                Category = "Architecture",
                SuggestedValue = "MVVM",
                ApplyKind = "architecture-mvvm",
                IsApplied = true,
                CanApply = false
            });
        }
    }

    private static void RecommendTheme(SsotDocument? document, WizardAnswers answers, List<BlueprintRecommendation> list)
    {
        if (!answers.LookStyle.Equals("Modern", StringComparison.OrdinalIgnoreCase))
            return;

        var theme = document?.Ui.Theme ?? answers.Theme;
        if (theme.Equals("Fluent", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new BlueprintRecommendation
            {
                RuleId = "UI-MODERN-FLUENT-001",
                Title = "Fluent theme recommended",
                Why = "You asked for a modern look. Fluent is the modern Windows desktop theme option in this blueprint profile.",
                Category = "UI",
                SuggestedValue = "Fluent",
                ApplyKind = "theme-fluent",
                IsApplied = true,
                CanApply = false
            });
            return;
        }

        list.Add(new BlueprintRecommendation
        {
            RuleId = "UI-MODERN-FLUENT-001",
            Title = "Fluent theme recommended",
            Why = "You asked for a modern look. Fluent is the modern Windows desktop theme option in this blueprint profile.",
            Category = "UI",
            SuggestedValue = "Fluent",
            ApplyKind = "theme-fluent"
        });
    }

    private static void RecommendBackup(SsotDocument? document, WizardAnswers answers, List<BlueprintRecommendation> list)
    {
        if (document is null) return;
        var hasLocalData = !string.Equals(document.Data.Storage.Type, "None", StringComparison.OrdinalIgnoreCase)
                           || answers.SavePastResults;
        if (!hasLocalData) return;

        var hasBackup = document.Ui.Screens.SelectMany(s => s.Controls)
            .Any(c => ContainsAny(c.Label + " " + c.Id, "backup", "export"));
        if (hasBackup) return;

        list.Add(new BlueprintRecommendation
        {
            RuleId = "DATA-BACKUP-EXPORT-001",
            Title = "Backup / export recommended",
            Why = "Your application stores user-created information locally. Offering backup or export protects users if the device is replaced or files are lost.",
            Category = "Reliability",
            SuggestedValue = "Add Backup / Export button",
            ApplyKind = "backup-feature"
        });
    }

    private static void RecommendAccountsConflict(WizardAnswers answers, List<BlueprintRecommendation> list)
    {
        if (answers.Accounts == AccountsChoice.None && answers.MultiDevice == MultiDeviceChoice.Yes)
        {
            list.Add(new BlueprintRecommendation
            {
                RuleId = "REQ-ACCOUNTS-SYNC-001",
                Title = "Accounts may be needed for multi-device",
                Why = "You want information to follow the user across devices, but also chose no accounts. Syncing private data across devices usually needs some form of user identity.",
                Category = "Requirements",
                SuggestedValue = "Consider separate accounts",
                ApplyKind = string.Empty,
                CanApply = false
            });
        }

        if (answers.Connectivity == ConnectivityChoice.Offline && answers.MultiDevice == MultiDeviceChoice.Yes)
        {
            list.Add(new BlueprintRecommendation
            {
                RuleId = "REQ-OFFLINE-SYNC-002",
                Title = "Offline vs multi-device tension",
                Why = "Fully offline apps cannot sync across devices without some online path. Prefer “sometimes online” if sync matters, or keep data local-only.",
                Category = "Requirements",
                SuggestedValue = "Review connectivity",
                ApplyKind = string.Empty,
                CanApply = false
            });
        }
    }

    private static void SetDecision(DecisionInfo decision, string mode, BlueprintRecommendation recommendation)
    {
        decision.Mode = mode;
        decision.RuleId = recommendation.RuleId;
        decision.Rationale = recommendation.Why;
        decision.Guidance = new List<string>
        {
            "maintainable",
            "testable",
            "appropriate_for_project_scale"
        };
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        var hay = text.ToLowerInvariant();
        return needles.Any(n => hay.Contains(n, StringComparison.Ordinal));
    }
}
