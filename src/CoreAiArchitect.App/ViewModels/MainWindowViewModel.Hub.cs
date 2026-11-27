using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using CoreAiArchitect.Core.Export;
using CoreAiArchitect.Core.Validation;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.App.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private void RunReadyCheck()
    {
        EnsureDocumentFromIdea();
        CompileDocument();
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        var result = _readiness.Assess(Document!, profile);
        var compat = _compatibility.EvaluateDocument(Document!, profile);
        foreach (var issue in compat.Issues)
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Path = "compatibility",
                Message = issue.Message + (string.IsNullOrWhiteSpace(issue.SuggestedFix) ? string.Empty : " " + issue.SuggestedFix)
            });
        }

        ReadyMessages.Clear();
        foreach (var issue in result.Issues)
            ReadyMessages.Add(FriendlyIssueMapper.ToFriendlyMessage(issue));

        IsReadyToExport = result.IsValid;
        ReadySummary = FriendlyIssueMapper.FormatReport(result);
        AppStory = BlueprintCompiler.BuildAppStory(Document!, Answers);
        RefreshHub(result, compat.Issues);
        RefreshRecommendations();
        RefreshArtifactPreviews();
        StatusText = IsReadyToExport
            ? "Ready — you can save and hand this blueprint to an AI coding agent."
            : "Not ready yet — resolve the items in the Blueprint hub.";
    }

    [RelayCommand]
    private void ApplyRecommendation(BlueprintRecommendation? recommendation)
    {
        if (recommendation is null || !recommendation.CanApply) return;
        EnsureDocumentFromIdea();
        _recommendations.Apply(recommendation, Answers, Document);
        OnPropertyChanged(nameof(SavePastResults));
        CompileDocument();
        RefreshRecommendations();
        RunReadyCheck();
        StatusText = $"Applied recommendation: {recommendation.Title}";
    }

    [RelayCommand]
    private void ExplainRecommendation(BlueprintRecommendation? recommendation)
    {
        if (recommendation is null) return;
        SelectedRecommendation = recommendation;
        _dialogs.ShowMessage(
            $"{recommendation.Title}\n\nWhy?\n{recommendation.Why}\n\nRule: {recommendation.RuleId}\nSuggested: {recommendation.SuggestedValue}",
            "Why this recommendation?");
    }

    [RelayCommand]
    private void JumpToHubSection(BlueprintHubSection? section)
    {
        if (section?.JumpToStep is null) return;
        CurrentStep = section.JumpToStep.Value;
        if (CurrentStep == WizardStep.Screens)
            SyncReviewSelections();
        if (CurrentStep == WizardStep.WhenTheyClick)
            SyncClickFlowsFromDocument();
    }

    [RelayCommand]
    private void CopyKickoffPrompt()
    {
        if (Document is null) return;
        var sb = new StringBuilder();
        var name = string.IsNullOrWhiteSpace(Document.Project.Name) ? "this application" : Document.Project.Name;
        sb.AppendLine($"# AI Coding Agent Kickoff: {name}");
        sb.AppendLine();
        sb.AppendLine("Please build this application following the specification in `project.json` and `AGENTS.md` exactly:");
        sb.AppendLine($"- Target: {Document.Environment.TargetOs} ({Document.Environment.Runtime}) using {Document.Environment.UiFramework} with {Document.Environment.Language}");
        sb.AppendLine($"- Architecture Pattern: {Document.Architecture.Pattern} (Decision mode: {Document.Architecture.PatternDecision.Mode})");
        sb.AppendLine($"- Data Storage: {Document.Data.Storage.Type} (Decision mode: {Document.Data.Storage.Decision.Mode})");
        sb.AppendLine($"- Logging Required: {Document.Constraints.LoggingRequired}, Telemetry Allowed: {Document.Constraints.TelemetryAllowed}");
        if (Document.Constraints.ForbiddenLibraries.Count > 0)
            sb.AppendLine($"- Forbidden Libraries: {string.Join(", ", Document.Constraints.ForbiddenLibraries)}");
        sb.AppendLine();
        sb.AppendLine("Please execute the phased tasks starting with Phase 1 in `tasks/001-foundation.md`. Keep commits small and verified with tests.");

        Clipboard.SetText(sb.ToString());
        StatusText = "Copied AI agent kickoff prompt to clipboard!";
        _dialogs.ShowMessage("Copied AI coding agent kickoff prompt to clipboard!\n\nYou can paste this directly to Claude, Cursor, Copilot, or Gemini CLI along with your exported Agent Package.", "Prompt Copied");
    }

    public void RefreshArtifactPreviews()
    {
        if (Document is null) return;
        BlueprintJsonPreview = new JsonExporter().ToJson(Document);
        AgentsMdPreview = AgentPackageExporter.BuildAgentsMd(Document);
        SpecificationsPreview = AgentPackageExporter.BuildRequirements(Document);
        TasksPreview = string.Join("\n\n---\n\n", AgentPackageExporter.BuildTasks(Document).Select(t => t.Body));
    }

    private void RefreshHub(ValidationResult result, IReadOnlyList<CompatibilityIssue> compatIssues)
    {
        var assessment = BlueprintHubAssessor.Assess(Document, Answers, result, compatIssues);
        HubSections.Clear();
        foreach (var section in assessment.Sections)
            HubSections.Add(section);

        HubProgressCaption = assessment.ProgressCaption;
        TechSummary = assessment.TechSummary;
        HubReadyPercent = assessment.ReadyPercent;
        DecisionsRemaining = assessment.DecisionsRemaining;
        ProgressText = assessment.ProgressCaption;
        ProgressValue = assessment.ReadyPercent / 100.0;
    }

    private void RefreshRecommendations()
    {
        Recommendations.Clear();
        foreach (var item in _recommendations.Evaluate(Document, Answers))
            Recommendations.Add(item);
        SelectedRecommendation = Recommendations.FirstOrDefault();
    }
}
