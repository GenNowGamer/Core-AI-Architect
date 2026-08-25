using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.Validation;

namespace CoreAiArchitect.Core.Wizard;

public enum BlueprintSectionStatus
{
    Ready,
    Attention,
    Optional
}

/// <summary>One row in the Blueprint hub checklist.</summary>
public sealed class BlueprintHubSection
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public BlueprintSectionStatus Status { get; init; }
    public string StatusGlyph => Status switch
    {
        BlueprintSectionStatus.Ready => "✓",
        BlueprintSectionStatus.Attention => "⚠",
        _ => "○"
    };
    public string Summary { get; init; } = string.Empty;
    public WizardStep? JumpToStep { get; init; }
}

public sealed class BlueprintHubAssessment
{
    public IReadOnlyList<BlueprintHubSection> Sections { get; init; } = Array.Empty<BlueprintHubSection>();
    public int DecisionsRemaining { get; init; }
    public int ReadyPercent { get; init; }
    public string ProgressCaption { get; init; } = string.Empty;
    public string TechSummary { get; init; } = string.Empty;
}

/// <summary>Deterministic completeness for the Blueprint hub (no AI).</summary>
public static class BlueprintHubAssessor
{
    public static BlueprintHubAssessment Assess(
        SsotDocument? document,
        WizardAnswers answers,
        ValidationResult? readiness,
        IReadOnlyList<CompatibilityIssue>? compatibilityIssues = null)
    {
        var sections = new List<BlueprintHubSection>();
        var decisions = 0;

        var ideaOk = !string.IsNullOrWhiteSpace(answers.ProjectName)
                     && !string.IsNullOrWhiteSpace(answers.Description);
        if (!ideaOk) decisions++;
        sections.Add(new BlueprintHubSection
        {
            Id = "idea",
            Title = "Overview",
            Status = ideaOk ? BlueprintSectionStatus.Ready : BlueprintSectionStatus.Attention,
            Summary = ideaOk
                ? $"{answers.ProjectName.Trim()} — {Truncate(answers.Description, 90)}"
                : "Name the app and complete the ad-lib (or product rules).",
            JumpToStep = WizardStep.YourIdea
        });

        sections.Add(new BlueprintHubSection
        {
            Id = "needs",
            Title = "Needs",
            Status = BlueprintSectionStatus.Optional,
            Summary =
                $"Accounts={answers.Accounts}; Devices={answers.MultiDevice}; Notifications={(answers.Notifications ? "yes" : "no")}",
            JumpToStep = WizardStep.WhatYouNeed
        });

        var screenCount = document?.Ui.Screens.Count(s => !IsSystemScreen(s)) ?? 0;
        var controlCount = document?.Ui.Screens.Sum(s => s.Controls.Count) ?? 0;
        var screensOk = screenCount > 0 && controlCount > 0;
        if (!screensOk) decisions++;
        sections.Add(new BlueprintHubSection
        {
            Id = "screens",
            Title = "Screens",
            Status = screensOk ? BlueprintSectionStatus.Ready : BlueprintSectionStatus.Attention,
            Summary = screensOk
                ? $"{screenCount} screen(s), {controlCount} control(s)"
                : "Add at least one screen with something people can use.",
            JumpToStep = WizardStep.Screens
        });

        var buttonCount = document?.Ui.Screens.SelectMany(s => s.Controls)
            .Count(c => IsButtonType(c.Type)) ?? 0;
        var workflowsWithSteps = document?.Workflows.Count(w => w.Steps.Count > 0) ?? 0;
        var clicksOk = buttonCount == 0 || workflowsWithSteps > 0;
        if (buttonCount > 0 && workflowsWithSteps == 0) decisions++;
        sections.Add(new BlueprintHubSection
        {
            Id = "clicks",
            Title = "Clicks",
            Status = clicksOk
                ? (buttonCount == 0 ? BlueprintSectionStatus.Optional : BlueprintSectionStatus.Ready)
                : BlueprintSectionStatus.Attention,
            Summary = buttonCount == 0
                ? "No buttons yet (optional until you add one)."
                : $"{buttonCount} button(s), {workflowsWithSteps} with steps",
            JumpToStep = WizardStep.WhenTheyClick
        });

        var rememberOn = answers.SavePastResults || answers.IncludeSettingsScreen;
        sections.Add(new BlueprintHubSection
        {
            Id = "remember",
            Title = "Remember",
            Status = BlueprintSectionStatus.Optional,
            Summary = rememberOn
                ? $"History={(answers.SavePastResults ? "on" : "off")}, Settings={(answers.IncludeSettingsScreen ? "on" : "off")}"
                : "Off — fine for a simple app.",
            JumpToStep = WizardStep.RememberStuff
        });

        var platformOk = !string.IsNullOrWhiteSpace(answers.TargetOs)
                         && !string.IsNullOrWhiteSpace(answers.UiFramework)
                         && !string.IsNullOrWhiteSpace(answers.Language);
        sections.Add(new BlueprintHubSection
        {
            Id = "platform",
            Title = "Platform",
            Status = platformOk ? BlueprintSectionStatus.Ready : BlueprintSectionStatus.Attention,
            Summary = platformOk
                ? $"{answers.UiFramework} · {answers.Language} · {answers.TargetOs}"
                : "Platform defaults are missing — re-pick an app type.",
            JumpToStep = WizardStep.YourIdea
        });

        if (readiness is not null)
        {
            var readinessErrors = readiness.Errors.ToList();
            decisions += readinessErrors.Count;
            foreach (var issue in readinessErrors.Take(3))
            {
                sections.Add(new BlueprintHubSection
                {
                    Id = $"ready-{sections.Count}",
                    Title = "Readiness",
                    Status = BlueprintSectionStatus.Attention,
                    Summary = FriendlyIssueMapper.ToFriendlyMessage(issue),
                    JumpToStep = GuessStepFromPath(issue.Path)
                });
            }
        }

        if (compatibilityIssues is { Count: > 0 })
        {
            decisions += compatibilityIssues.Count;
            foreach (var issue in compatibilityIssues.Take(2))
            {
                sections.Add(new BlueprintHubSection
                {
                    Id = $"compat-{sections.Count}",
                    Title = "Compatibility",
                    Status = BlueprintSectionStatus.Attention,
                    Summary = issue.Message,
                    JumpToStep = WizardStep.YourIdea
                });
            }
        }

        var checklistReady = sections.Count(s => s.Status == BlueprintSectionStatus.Ready);
        var checklistTotal = Math.Max(1, sections.Count(s => s.Status != BlueprintSectionStatus.Optional));
        var percent = (int)Math.Round(100.0 * checklistReady / checklistTotal);
        if (readiness is { IsValid: true } && decisions == 0)
            percent = 100;

        var tech = string.Join(Environment.NewLine,
            $"{WizardAnswers.ProfileLabel(answers.ProfileId)}",
            $"Stack: {answers.Language} / {answers.Runtime} / {answers.UiFramework}",
            $"UI: {answers.Theme} theme, {answers.NavigationPattern}",
            $"Look: {answers.LookStyle}; mainly {answers.ConnectivityLabel.ToLowerInvariant()}",
            document is null
                ? "Storage: (not compiled yet)"
                : $"Storage: {document.Data.Storage.Type}");

        return new BlueprintHubAssessment
        {
            Sections = sections,
            DecisionsRemaining = decisions,
            ReadyPercent = Math.Clamp(percent, 0, 100),
            ProgressCaption = decisions == 0
                ? $"Project definition: {percent}% — ready for agent export"
                : $"Project definition: {percent}% — {decisions} decision(s) remaining",
            TechSummary = tech
        };
    }

    private static WizardStep? GuessStepFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.StartsWith("ui.", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("ui[", StringComparison.OrdinalIgnoreCase))
            return WizardStep.Screens;
        if (path.StartsWith("workflows", StringComparison.OrdinalIgnoreCase))
            return WizardStep.WhenTheyClick;
        if (path.StartsWith("project", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("environment", StringComparison.OrdinalIgnoreCase))
            return WizardStep.YourIdea;
        if (path.StartsWith("data", StringComparison.OrdinalIgnoreCase))
            return WizardStep.RememberStuff;
        return WizardStep.HandOff;
    }

    private static string Truncate(string value, int max)
    {
        var t = value.Trim().Replace("\r\n", " ").Replace("\n", " ");
        return t.Length <= max ? t : t[..(max - 1)] + "…";
    }

    private static bool IsSystemScreen(ScreenInfo screen) =>
        screen.Id.Equals("history", StringComparison.OrdinalIgnoreCase)
        || screen.Id.Equals("settings", StringComparison.OrdinalIgnoreCase);

    private static bool IsButtonType(string type) =>
        type.Equals("Button", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Command", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Cmdlet", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Endpoint", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Stage", StringComparison.OrdinalIgnoreCase);
}
