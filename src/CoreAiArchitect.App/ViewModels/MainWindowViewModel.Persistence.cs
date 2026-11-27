using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using CoreAiArchitect.Core.Export;
using CoreAiArchitect.Core.Json;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.App.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private void NewDocument()
    {
        if (!ConfirmDiscard()) return;
        _suppressKindChange = true;
        Answers.ProjectName = string.Empty;
        Answers.Owner = string.Empty;
        Answers.ClearAdLib();
        Answers.SavePastResults = false;
        Answers.IncludeSettingsScreen = false;
        Answers.Memory = MemoryChoice.None;
        SelectedAppKind = AppKinds[0];
        ApplyProfileDefaults(SelectedAppKind.ProfileId);
        SelectedTemplate = Templates[0];
        Document = null;
        _documentInitialized = false;
        _documents.NewDocument(_schemaCatalog.RequireById(SelectedAppKind.ProfileId));
        CurrentStep = WizardStep.YourIdea;
        IsDirty = false;
        _suppressKindChange = false;
        DescriptionTips.Clear();
        HubSections.Clear();
        Recommendations.Clear();
        SyncConnectivityLabel();
        SyncDetailLevelLabel();
        SyncAccountsLabel();
        SyncMultiDeviceLabel();
        SyncDecisionModeLabels();
        NotifyProfileLabelsChanged();
        ShowNotificationNotes = false;
        StatusText = "Started a new app blueprint — fill in the ad-lib.";
        UpdateTitle();
        RefreshDescriptionTips();
        OnPropertyChanged(nameof(SavePastResults));
        OnPropertyChanged(nameof(IncludeSettingsScreen));
        OnPropertyChanged(nameof(Notifications));
        OnPropertyChanged(nameof(ShowStandardDetails));
        OnPropertyChanged(nameof(ShowExpertDetails));
    }

    [RelayCommand]
    private void OpenDocument()
    {
        if (!ConfirmDiscard()) return;
        var path = _dialogs.OpenFile("App Blueprint JSON|*.json;*.ssot.json|All files|*.*", "Open App Blueprint");
        if (path is null) return;
        OpenBlueprintFromPath(path);
    }

    [RelayCommand]
    private void OpenRecentFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!System.IO.File.Exists(path))
        {
            _dialogs.ShowMessage($"File not found:\n{path}", "Recent Blueprint", isError: true);
            _settings.RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            RecentFiles.Remove(path);
            _settingsService.Save(_settings);
            return;
        }
        if (!ConfirmDiscard()) return;
        OpenBlueprintFromPath(path);
    }

    public void RecordRecentFile(string path)
    {
        _settings.AddRecentFile(path);
        RecentFiles.Clear();
        foreach (var item in _settings.RecentFiles)
            RecentFiles.Add(item);
        _settingsService.Save(_settings);
    }

    public Core.Models.UserSettings GetUserSettings() => _settings;

    public void SaveUserSettings() => _settingsService.Save(_settings);

    protected void OpenBlueprintFromPath(string path)
    {
        var doc = _documents.Open(path);
        HydrateFromDocument(doc);
        Document = doc;
        _documentInitialized = true;
        CurrentStep = WizardStep.Screens;
        SyncReviewSelections();
        NotifyProfileLabelsChanged();
        IsDirty = false;
        RecordRecentFile(path);
        StatusText = $"Opened {path}. Review your screens, then continue.";
        UpdateTitle();
    }

    [RelayCommand]
    private void SaveDocument()
    {
        EnsureDocumentFromIdea();
        CompileDocument();
        var path = _documents.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            SaveDocumentAs();
            return;
        }

        _documents.Save(path);
        IsDirty = false;
        RecordRecentFile(path);
        StatusText = $"Saved {path}";
        UpdateTitle();
    }

    [RelayCommand]
    private void SaveDocumentAs()
    {
        EnsureDocumentFromIdea();
        CompileDocument();
        var suggested = $"{Answers.DerivedProjectId}.ssot.json";
        var path = _dialogs.SaveFile("App Blueprint JSON|*.ssot.json;*.json", "Save App Blueprint", suggested);
        if (path is null) return;
        _documents.Save(path);
        IsDirty = false;
        RecordRecentFile(path);
        StatusText = $"Saved {path}";
        UpdateTitle();
    }

    [RelayCommand]
    private void ExportJson()
    {
        EnsureDocumentFromIdea();
        CompileDocument();
        var path = _dialogs.SaveFile("App Blueprint JSON|*.ssot.json;*.json", "Export for AI agent", $"{Answers.DerivedProjectId}.ssot.json");
        if (path is null) return;
        _documents.ExportJson(path);
        StatusText = $"Exported blueprint to {path}";
    }

    [RelayCommand]
    private void ExportMarkdown()
    {
        EnsureDocumentFromIdea();
        CompileDocument();
        var path = _dialogs.SaveFile("Markdown|*.md", "Export Markdown", $"{Answers.DerivedProjectId}.md");
        if (path is null) return;
        _documents.ExportMarkdown(path);
        StatusText = $"Exported Markdown to {path}";
    }

    [RelayCommand]
    private void ExportAgentPackage()
    {
        EnsureDocumentFromIdea();
        CompileDocument();
        var folder = _dialogs.PickFolder("Choose folder for Agent Package");
        if (folder is null) return;
        var target = System.IO.Path.Combine(folder, Answers.DerivedProjectId + "-Agent-Package");
        var written = _documents.ExportAgentPackage(target);
        var msg = $"Exported agent package ({written.Count} files) to {target}";

        if (InitializeGitOnExport)
        {
            if (AgentPackageExporter.InitializeGitRepository(target, Document!, out var gitMsg))
                msg += $"\n\nGit: {gitMsg}";
            else
                msg += $"\n\nGit (note): {gitMsg}";
        }

        StatusText = $"Exported agent package to {target}";
        _dialogs.ShowMessage($"{msg}\n\nIncludes project.json, AGENTS.md, .cursor/rules/project.mdc, .github/copilot-instructions.md, CLAUDE.md, specifications/, and tasks/.");
    }

    [RelayCommand]
    private void Undo()
    {
        var state = _undoManager.Undo(CurrentStep, Answers, Document);
        if (state is null) return;
        RestoreFromUndoState(state);
        StatusText = $"Undid: {state.Description}";
    }

    [RelayCommand]
    private void Redo()
    {
        var state = _undoManager.Redo(CurrentStep, Answers, Document);
        if (state is null) return;
        RestoreFromUndoState(state);
        StatusText = $"Redid: {state.Description}";
    }

    public void RecordUndo(string description)
    {
        _undoManager.RecordSnapshot(description, CurrentStep, Answers, Document);
        UpdateUndoState();
    }

    private void UpdateUndoState()
    {
        CanUndo = _undoManager.CanUndo;
        CanRedo = _undoManager.CanRedo;
    }

    private void RestoreFromUndoState(UndoState state)
    {
        var options = SsotJsonOptions.Create();
        if (!string.IsNullOrWhiteSpace(state.AnswersJson))
        {
            var restored = JsonSerializer.Deserialize<WizardAnswers>(state.AnswersJson, options);
            if (restored != null)
            {
                Answers.ProjectName = restored.ProjectName;
                Answers.Description = restored.Description;
                Answers.Audience = restored.Audience;
                Answers.HelpsThem = restored.HelpsThem;
                Answers.FeatureListText = restored.FeatureListText;
                Answers.ProductRules = restored.ProductRules;
                Answers.LookStyle = restored.LookStyle;
                Answers.Connectivity = restored.Connectivity;
                Answers.Accounts = restored.Accounts;
                Answers.MultiDevice = restored.MultiDevice;
                Answers.Notifications = restored.Notifications;
                Answers.NotificationNotes = restored.NotificationNotes;
                Answers.SavePastResults = restored.SavePastResults;
                Answers.IncludeSettingsScreen = restored.IncludeSettingsScreen;
                Answers.Theme = restored.Theme;
                Answers.NavigationPattern = restored.NavigationPattern;
                Answers.ProfileId = restored.ProfileId;
            }
        }

        if (!string.IsNullOrWhiteSpace(state.DocumentJson))
        {
            Document = JsonSerializer.Deserialize<SsotDocument>(state.DocumentJson, options);
            if (Document != null)
            {
                var profile = _schemaCatalog.RequireById(Answers.ProfileId);
                _documents.ReplaceDocument(Document, profile, _documents.FilePath);
            }
        }

        CurrentStep = state.Step;
        SyncReviewSelections();
        SyncClickFlowsFromDocument();
        RefreshWireframe();
        UpdateUndoState();
        MarkDirty();
    }

    private void HydrateFromDocument(SsotDocument doc)
    {
        _suppressKindChange = true;
        var match = AppKinds.FirstOrDefault(k =>
            string.Equals(k.ProfileId, doc.ProfileId, StringComparison.OrdinalIgnoreCase))
            ?? AppKinds.FirstOrDefault(k =>
            string.Equals(_schemaCatalog.RequireById(k.ProfileId).EnvironmentDefaults.UiFramework,
                doc.Environment.UiFramework, StringComparison.OrdinalIgnoreCase));
        SelectedAppKind = match ?? AppKinds[0];

        var profile = _schemaCatalog.RequireById(SelectedAppKind.ProfileId);
        Answers.ProfileId = profile.Id;
        Answers.ProjectName = doc.Project.Name;
        Answers.HydrateAdLibFromDescription(doc.Project.Description);
        Answers.Owner = doc.Project.Owner;
        Answers.TargetOs = doc.Environment.TargetOs;
        Answers.Runtime = doc.Environment.Runtime;
        Answers.UiFramework = doc.Environment.UiFramework;
        Answers.Language = doc.Environment.Language;
        Answers.Theme = doc.Ui.Theme;
        Answers.NavigationPattern = doc.Ui.NavigationPattern;

        Answers.Accounts = doc.Requirements.Accounts switch
        {
            "separate" => AccountsChoice.Separate,
            "unsure" => AccountsChoice.Unsure,
            _ => AccountsChoice.None
        };
        Answers.MultiDevice = doc.Requirements.MultiDevice switch
        {
            "yes" => MultiDeviceChoice.Yes,
            "later" => MultiDeviceChoice.Later,
            _ => MultiDeviceChoice.No
        };
        Answers.Notifications = doc.Requirements.Notifications;
        Answers.NotificationNotes = doc.Requirements.NotificationNotes ?? string.Empty;
        Answers.ArchitectureDecision = doc.Architecture.PatternDecision.Mode switch
        {
            "agent_discretion" => TechnicalDecisionMode.AgentDiscretion,
            "specified" => TechnicalDecisionMode.ChooseMyself,
            _ => TechnicalDecisionMode.Recommended
        };
        Answers.StorageDecision = doc.Data.Storage.Decision.Mode switch
        {
            "agent_discretion" => TechnicalDecisionMode.AgentDiscretion,
            "specified" => TechnicalDecisionMode.ChooseMyself,
            _ => TechnicalDecisionMode.Recommended
        };

        Answers.SavePastResults = doc.Ui.Screens.Any(s => s.Id.Equals("history", StringComparison.OrdinalIgnoreCase))
                                  || !string.Equals(doc.Data.Storage.Type, "None", StringComparison.OrdinalIgnoreCase);
        Answers.IncludeSettingsScreen = doc.Ui.Screens.Any(s => s.Id.Equals("settings", StringComparison.OrdinalIgnoreCase));
        Answers.Memory = Answers.SavePastResults ? MemoryChoice.SimpleFiles : MemoryChoice.None;

        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == "blank") ?? Templates[0];
        RefreshOptionLists(profile);
        SyncConnectivityLabel();
        SyncDetailLevelLabel();
        SyncAccountsLabel();
        SyncMultiDeviceLabel();
        SyncDecisionModeLabels();
        ShowNotificationNotes = Answers.Notifications;
        _documents.SetProfile(profile);
        _suppressKindChange = false;
        OnPropertyChanged(nameof(SavePastResults));
        OnPropertyChanged(nameof(IncludeSettingsScreen));
        OnPropertyChanged(nameof(Notifications));
        OnPropertyChanged(nameof(ShowStandardDetails));
        OnPropertyChanged(nameof(ShowExpertDetails));
        RefreshDescriptionTips();
    }

    private bool ConfirmDiscard()
    {
        if (!IsDirty) return true;
        return _dialogs.Confirm("Discard unsaved changes?");
    }
}
