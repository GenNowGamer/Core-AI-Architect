using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreAiArchitect.App.Services;
using CoreAiArchitect.Core.Export;
using CoreAiArchitect.Core.Json;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.Validation;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.App.ViewModels;

public partial class AppKindOption : ObservableObject
{
    public AppKindOption(string profileId, string title, string description)
    {
        ProfileId = profileId;
        Title = title;
        Description = description;
    }

    public string ProfileId { get; }
    public string Title { get; }
    public string Description { get; }
    [ObservableProperty] private bool _isSelected;
}

public partial class BlueprintTemplateOption : ObservableObject
{
    public BlueprintTemplateOption(string id, string title, string description)
    {
        Id = id;
        Title = title;
        Description = description;
    }

    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    [ObservableProperty] private bool _isSelected;
}

public partial class ClickFlowOption : ObservableObject
{
    public ClickFlowOption(ControlInfo button, WorkflowInfo workflow)
    {
        Button = button;
        Workflow = workflow;
        StepsText = string.Join(Environment.NewLine, workflow.Steps);
    }

    public ControlInfo Button { get; }
    public WorkflowInfo Workflow { get; }
    public string ButtonLabel => string.IsNullOrWhiteSpace(Button.Label) ? Button.Id : Button.Label;

    [ObservableProperty] private string _stepsText = string.Empty;

    partial void OnStepsTextChanged(string value)
    {
        Workflow.Steps = value
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }
}

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ISchemaCatalogService _schemaCatalog;
    private readonly IDialogService _dialogs;
    private readonly ISsotDocumentService _documents;
    private readonly CompatibilityEngine _compatibility = new();
    private readonly BlueprintCompiler _compiler = new();
    private readonly SsotReadinessEngine _readiness = new();
    private readonly RecommendationEngine _recommendations = new();
    private readonly UndoManager _undoManager = new();
    private bool _suppressKindChange;
    private bool _documentInitialized;

    public MainWindowViewModel(
        ISchemaCatalogService schemaCatalog,
        IDialogService dialogs,
        ISsotDocumentService documents)
    {
        _schemaCatalog = schemaCatalog;
        _dialogs = dialogs;
        _documents = documents;
        _schemaCatalog.EnsureLoaded();

        AppKinds = new ObservableCollection<AppKindOption>
        {
            new("wpf_desktop", "Windows desktop app", "A normal Windows program with windows and buttons."),
            new("android_app", "Android app", "A phone or tablet app for Android devices."),
            new("web_api", "Website / API", "A backend that websites or other apps call."),
            new("custom", "Something else", "We’ll use flexible defaults you can refine later.")
        };

        Templates = new ObservableCollection<BlueprintTemplateOption>(
            BlueprintTemplates.All.Select(t => new BlueprintTemplateOption(t.Id, t.Title, t.Description)));

        ConnectivityOptions = new ObservableCollection<string>
        {
            "Offline",
            "Sometimes online",
            "Online (needs the internet)"
        };

        LookStyleOptions = new ObservableCollection<string> { "Modern", "Professional", "Simple" };
        DetailLevelOptions = new ObservableCollection<string> { "Simple", "Standard", "Expert" };
        AccountsOptions = new ObservableCollection<string>
        {
            "No — anyone using the device can use the app",
            "Yes — users need separate accounts",
            "I'm not sure"
        };
        MultiDeviceOptions = new ObservableCollection<string>
        {
            "No — stays on this device",
            "Yes — follow the user across devices",
            "Maybe later"
        };
        DecisionModeOptions = new ObservableCollection<string>
        {
            "Recommended",
            "Let coding agent decide",
            "Choose myself"
        };

        Answers = new WizardAnswers();
        SelectedAppKind = AppKinds[0];
        AppKinds[0].IsSelected = true;
        SelectedTemplate = Templates[0];
        Templates[0].IsSelected = true;
        ApplyProfileDefaults(SelectedAppKind.ProfileId);
        CurrentStep = WizardStep.YourIdea;
        UpdateStepChrome();
        RefreshDescriptionTips();
        SyncDetailLevelLabel();
        SyncConnectivityLabel();
        SyncAccountsLabel();
        SyncMultiDeviceLabel();
        SyncDecisionModeLabels();
    }

    public ObservableCollection<AppKindOption> AppKinds { get; }
    public ObservableCollection<BlueprintTemplateOption> Templates { get; }
    public ObservableCollection<ScreenInfo> ReviewScreens { get; } = new();
    public ObservableCollection<ControlInfo> ReviewControls { get; } = new();
    public ObservableCollection<ClickFlowOption> ClickFlows { get; } = new();
    public ObservableCollection<string> ReadyMessages { get; } = new();
    public ObservableCollection<string> DescriptionTips { get; } = new();
    public ObservableCollection<BlueprintHubSection> HubSections { get; } = new();
    public ObservableCollection<BlueprintRecommendation> Recommendations { get; } = new();
    public ObservableCollection<string> ConnectivityOptions { get; }
    public ObservableCollection<string> LookStyleOptions { get; }
    public ObservableCollection<string> DetailLevelOptions { get; }
    public ObservableCollection<string> AccountsOptions { get; }
    public ObservableCollection<string> MultiDeviceOptions { get; }
    public ObservableCollection<string> DecisionModeOptions { get; }
    public ObservableCollection<string> ThemeOptions { get; } = new();
    public ObservableCollection<string> NavigationOptions { get; } = new();
    public ObservableCollection<string> ScreenTypeOptions { get; } = new()
    {
        "Window",
        "Dialog",
        "Modal",
        "Tab",
        "BottomSheet",
        "Drawer",
        "Page"
    };

    public WizardAnswers Answers { get; }

    public string DescriptionGuidance => DomainScaffoldPolicy.UserGuidanceDescription;
    public string ClicksGuidance => DomainScaffoldPolicy.UserGuidanceClicks;
    public string HandOffGuidance => DomainScaffoldPolicy.UserGuidanceHandOff;
    public string DomainGuidanceShort => DomainScaffoldPolicy.UserGuidanceShort;
    public string AccountsHelp =>
        "You probably don't need accounts if one person/device uses the app and nothing private syncs elsewhere. " +
        "You may need accounts if users need their information on multiple devices or online.";

    public IReadOnlyList<(WizardStep Step, string Title)> StepList { get; } = new (WizardStep, string)[]
    {
        (WizardStep.YourIdea, "Ad-Lib"),
        (WizardStep.WhatYouNeed, "Needs"),
        (WizardStep.Screens, "Screens"),
        (WizardStep.WhenTheyClick, "Clicks"),
        (WizardStep.RememberStuff, "Remember"),
        (WizardStep.HandOff, "Blueprint")
    };

    [ObservableProperty] private WizardStep _currentStep;
    [ObservableProperty] private AppKindOption? _selectedAppKind;
    [ObservableProperty] private BlueprintTemplateOption? _selectedTemplate;
    [ObservableProperty] private string _stepTitle = string.Empty;
    [ObservableProperty] private string _stepSubtitle = string.Empty;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusText = "Start with the ad-lib — fill in the blanks about your app.";
    [ObservableProperty] private string _windowTitle = "Core AI Architect";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoNext = true;
    [ObservableProperty] private bool _isReadyStep;
    [ObservableProperty] private bool _isReadyToExport;
    [ObservableProperty] private string _readySummary = string.Empty;
    [ObservableProperty] private string _appStory = string.Empty;
    [ObservableProperty] private string _screenCaption = string.Empty;
    [ObservableProperty] private SsotDocument? _document;
    [ObservableProperty] private ScreenInfo? _selectedScreen;
    [ObservableProperty] private ControlInfo? _selectedControl;
    [ObservableProperty] private ClickFlowOption? _selectedClickFlow;
    [ObservableProperty] private string _hubProgressCaption = string.Empty;
    [ObservableProperty] private string _techSummary = string.Empty;
    [ObservableProperty] private int _hubReadyPercent;
    [ObservableProperty] private int _decisionsRemaining;
    [ObservableProperty] private string _selectedConnectivityLabel = "Offline";
    [ObservableProperty] private string _selectedDetailLevelLabel = "Simple";
    [ObservableProperty] private string _selectedAccountsLabel = "No — anyone using the device can use the app";
    [ObservableProperty] private string _selectedMultiDeviceLabel = "No — stays on this device";
    [ObservableProperty] private string _selectedArchitectureDecisionLabel = "Recommended";
    [ObservableProperty] private string _selectedStorageDecisionLabel = "Recommended";
    [ObservableProperty] private BlueprintHubSection? _selectedHubSection;
    [ObservableProperty] private BlueprintRecommendation? _selectedRecommendation;
    [ObservableProperty] private string _recommendationWhyText = string.Empty;
    [ObservableProperty] private bool _showNotificationNotes;
    [ObservableProperty] private bool _canUndo;
    [ObservableProperty] private bool _canRedo;
    [ObservableProperty] private bool _isDarkMode = true;
    [ObservableProperty] private bool _initializeGitOnExport = true;
    [ObservableProperty] private int _selectedArtifactTabIndex;
    [ObservableProperty] private string _blueprintJsonPreview = string.Empty;
    [ObservableProperty] private string _agentsMdPreview = string.Empty;
    [ObservableProperty] private string _specificationsPreview = string.Empty;
    [ObservableProperty] private string _tasksPreview = string.Empty;

    public bool SavePastResults
    {
        get => Answers.SavePastResults;
        set
        {
            if (Answers.SavePastResults == value) return;
            Answers.SavePastResults = value;
            OnPropertyChanged();
            ApplyRememberToDocument();
            MarkDirty();
        }
    }

    public bool IncludeSettingsScreen
    {
        get => Answers.IncludeSettingsScreen;
        set
        {
            if (Answers.IncludeSettingsScreen == value) return;
            Answers.IncludeSettingsScreen = value;
            OnPropertyChanged();
            ApplyRememberToDocument();
            MarkDirty();
        }
    }

    public DetailLevel SelectedDetailLevel
    {
        get => Answers.DetailLevel;
        set
        {
            if (Answers.DetailLevel == value) return;
            Answers.DetailLevel = value;
            SyncDetailLevelLabel();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowStandardDetails));
            OnPropertyChanged(nameof(ShowExpertDetails));
        }
    }

    public bool ShowStandardDetails => Answers.DetailLevel >= DetailLevel.Standard;
    public bool ShowExpertDetails => Answers.DetailLevel >= DetailLevel.Expert;

    public bool Notifications
    {
        get => Answers.Notifications;
        set
        {
            if (Answers.Notifications == value) return;
            Answers.Notifications = value;
            ShowNotificationNotes = value;
            OnPropertyChanged();
            MarkDirty();
        }
    }

    partial void OnSelectedConnectivityLabelChanged(string value)
    {
        Answers.Connectivity = value switch
        {
            "Online (needs the internet)" => ConnectivityChoice.Online,
            "Sometimes online" => ConnectivityChoice.SometimesOnline,
            _ => ConnectivityChoice.Offline
        };
        MarkDirty();
    }

    partial void OnSelectedAccountsLabelChanged(string value)
    {
        Answers.Accounts = value.StartsWith("Yes", StringComparison.OrdinalIgnoreCase)
            ? AccountsChoice.Separate
            : value.StartsWith("I'm not sure", StringComparison.OrdinalIgnoreCase)
                ? AccountsChoice.Unsure
                : AccountsChoice.None;
        MarkDirty();
    }

    partial void OnSelectedMultiDeviceLabelChanged(string value)
    {
        Answers.MultiDevice = value.StartsWith("Yes", StringComparison.OrdinalIgnoreCase)
            ? MultiDeviceChoice.Yes
            : value.StartsWith("Maybe", StringComparison.OrdinalIgnoreCase)
                ? MultiDeviceChoice.Later
                : MultiDeviceChoice.No;
        MarkDirty();
    }

    partial void OnSelectedArchitectureDecisionLabelChanged(string value)
    {
        Answers.ArchitectureDecision = ParseDecisionMode(value);
        MarkDirty();
    }

    partial void OnSelectedStorageDecisionLabelChanged(string value)
    {
        Answers.StorageDecision = ParseDecisionMode(value);
        MarkDirty();
    }

    partial void OnSelectedRecommendationChanged(BlueprintRecommendation? value)
    {
        RecommendationWhyText = value is null
            ? string.Empty
            : $"{value.Title}\n\nWhy?\n{value.Why}\n\nRule: {value.RuleId}";
    }

    partial void OnSelectedDetailLevelLabelChanged(string value)
    {
        if (Enum.TryParse<DetailLevel>(value, ignoreCase: true, out var level))
            SelectedDetailLevel = level;
    }

    partial void OnSelectedAppKindChanged(AppKindOption? value)
    {
        foreach (var kind in AppKinds)
            kind.IsSelected = kind == value;

        if (value is null || _suppressKindChange) return;
        ApplyProfileDefaults(value.ProfileId);
        MarkDirty();
    }

    partial void OnSelectedTemplateChanged(BlueprintTemplateOption? value)
    {
        foreach (var t in Templates)
            t.IsSelected = t == value;
    }

    partial void OnCurrentStepChanged(WizardStep value) => UpdateStepChrome();

    partial void OnSelectedScreenChanged(ScreenInfo? value)
    {
        ReloadReviewControls();
        SelectedControl = ReviewControls.FirstOrDefault();
        ScreenCaption = BlueprintCompiler.ScreenCaption(value);
    }

    partial void OnSelectedClickFlowChanged(ClickFlowOption? value)
    {
        // no-op; binding edits StepsText
    }

    [RelayCommand]
    private void Next()
    {
        if (!ValidateCurrentStep()) return;

        if (CurrentStep == WizardStep.YourIdea)
            EnsureDocumentFromIdea();

        if (CurrentStep == WizardStep.WhenTheyClick)
            SyncClickFlowStepsToDocument();

        if (CurrentStep == WizardStep.RememberStuff)
            ApplyRememberToDocument();

        if (CurrentStep < WizardStep.HandOff)
        {
            CurrentStep++;
            if (CurrentStep == WizardStep.Screens)
                SyncReviewSelections();
            if (CurrentStep == WizardStep.WhenTheyClick)
                SyncClickFlowsFromDocument();
            if (CurrentStep == WizardStep.HandOff)
                RunReadyCheck();
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > WizardStep.YourIdea)
            CurrentStep--;
        if (CurrentStep == WizardStep.Screens)
            SyncReviewSelections();
        if (CurrentStep == WizardStep.WhenTheyClick)
            SyncClickFlowsFromDocument();
    }

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

        var doc = _documents.Open(path);
        HydrateFromDocument(doc);
        Document = doc;
        _documentInitialized = true;
        CurrentStep = WizardStep.Screens;
        SyncReviewSelections();
        IsDirty = false;
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
        _dialogs.ShowMessage($"{msg}\n\nIncludes project.json, AGENTS.md, .cursorrules, CLAUDE.md, specifications/, and tasks/.");
    }

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
    private void ShowHelp()
    {
        _dialogs.ShowMessage(
            "Core AI Architect helps you describe an app in plain language.\n\n" +
            "1. Ad-Lib — fill in the blanks (who it’s for, what it helps them do, main capabilities).\n" +
            "2. Needs — accounts, multi-device, notifications (no jargon).\n" +
            "3. Screens — build what people see with everyday things.\n" +
            "4. Clicks — write what happens when someone uses a button.\n" +
            "5. Remember — optional History / Settings.\n" +
            "6. Blueprint hub — recommendations with Why?, detail level, then export JSON or an Agent Package.\n\n" +
            DomainScaffoldPolicy.UserGuidanceShort);
    }

    [RelayCommand]
    private void MarkReviewEdited() => MarkDirty();

    [RelayCommand]
    private void RefreshTips()
    {
        ComposeAdLibIntoDescription();
        RefreshDescriptionTips();
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

    [RelayCommand]
    private void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        ApplyThemeMode(IsDarkMode);
        StatusText = IsDarkMode ? "Switched to Dark theme." : "Switched to Light theme.";
    }

    public static void ApplyThemeMode(bool darkMode)
    {
        var app = Application.Current;
        if (app is null) return;

        if (darkMode)
        {
            app.Resources["SandBrush"] = new SolidColorBrush(Color.FromRgb(0x18, 0x1A, 0x1E)); // Main background
            app.Resources["SandDeepBrush"] = new SolidColorBrush(Color.FromRgb(0x22, 0x26, 0x2E)); // Chrome/status/borders
            app.Resources["CardBrush"] = new SolidColorBrush(Color.FromRgb(0x23, 0x27, 0x30)); // Cards
            app.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x35, 0x3D, 0x4B));
            app.Resources["InnerCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x20, 0x27));
            app.Resources["InnerCardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x32, 0x3A, 0x47));
            app.Resources["InkBrush"] = new SolidColorBrush(Color.FromRgb(0xEE, 0xF1, 0xF5)); // Primary text
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xA6, 0xB0, 0xC0)); // Subtitle / muted
            app.Resources["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x12, 0x14, 0x19));
            app.Resources["InputBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x3E, 0x47, 0x56));
            app.Resources["HeaderGradient"] = new LinearGradientBrush(
                Color.FromRgb(0x1A, 0x1E, 0x24),
                Color.FromRgb(0x12, 0x15, 0x1A),
                new Point(0, 0),
                new Point(1, 1));
            app.Resources["HeaderMenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1E, 0x24));
            app.Resources["HeaderMenuTextBrush"] = new SolidColorBrush(Color.FromRgb(0xEE, 0xF1, 0xF5));
            app.Resources["OuterBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x35, 0x3D, 0x4B));
            app.Resources["TealBrush"] = new SolidColorBrush(Color.FromRgb(0x4E, 0xB8, 0xB5));
            app.Resources["TealSoftBrush"] = new SolidColorBrush(Color.FromRgb(0x3D, 0x94, 0x92));
            app.Resources["CoralBrush"] = new SolidColorBrush(Color.FromRgb(0xFA, 0x7E, 0x6B));
        }
        else
        {
            app.Resources["SandBrush"] = new SolidColorBrush(Color.FromRgb(0xF4, 0xEF, 0xE6));
            app.Resources["SandDeepBrush"] = new SolidColorBrush(Color.FromRgb(0xE8, 0xDF, 0xD0));
            app.Resources["CardBrush"] = new SolidColorBrush(Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF));
            app.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE8, 0xDF, 0xD0));
            app.Resources["InnerCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFC, 0xF8));
            app.Resources["InnerCardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xD8, 0xC8));
            app.Resources["InkBrush"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            app.Resources["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            app.Resources["InputBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xC9, 0xBF, 0xAE));
            var headerGrad = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            headerGrad.GradientStops.Add(new GradientStop(Color.FromRgb(0x0F, 0x3D, 0x3E), 0));
            headerGrad.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x55, 0x56), 0.55));
            headerGrad.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0x5D, 0x4C), 1));
            app.Resources["HeaderGradient"] = headerGrad;
            app.Resources["HeaderMenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x3E));
            app.Resources["HeaderMenuTextBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            app.Resources["OuterBorderBrush"] = Brushes.Transparent;
            app.Resources["TealBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x3E));
            app.Resources["TealSoftBrush"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x55, 0x56));
            app.Resources["CoralBrush"] = new SolidColorBrush(Color.FromRgb(0xE8, 0x5D, 0x4C));
        }
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
        UpdateUndoState();
        MarkDirty();
    }

    public void RefreshArtifactPreviews()
    {
        if (Document is null) return;
        BlueprintJsonPreview = new JsonExporter().ToJson(Document);
        AgentsMdPreview = AgentPackageExporter.BuildAgentsMd(Document);
        SpecificationsPreview = AgentPackageExporter.BuildRequirements(Document);
        TasksPreview = string.Join("\n\n---\n\n", AgentPackageExporter.BuildTasks(Document).Select(t => t.Body));
    }

    [RelayCommand]
    private void AddScreen()
    {
        EnsureDocumentFromIdea();
        if (Document is null) return;

        RecordUndo("Add Screen");
        var n = Document.Ui.Screens.Count(s => !IsSystemScreen(s)) + 1;
        var screen = new ScreenInfo
        {
            Id = $"screen{n}",
            Title = $"Screen {n}",
            Route = n == 1 ? "/" : $"/screen{n}",
            ScreenType = Answers.ProfileId.Equals("android_app", StringComparison.OrdinalIgnoreCase) ? "Screen" : "Dialog"
        };
        Document.Ui.Screens.Add(screen);
        ReviewScreens.Add(screen);
        SelectedScreen = screen;
        MarkDirty();
        StatusText = $"Added screen “{screen.Title}”.";
    }

    [RelayCommand]
    private void RemoveScreen()
    {
        if (Document is null || SelectedScreen is null) return;
        if (IsSystemScreen(SelectedScreen))
        {
            _dialogs.ShowMessage("History and Settings are controlled on the Remember step. Turn them off there instead.");
            return;
        }

        if (Document.Ui.Screens.Count(s => !IsSystemScreen(s)) <= 1)
        {
            _dialogs.ShowMessage("Keep at least one main screen.");
            return;
        }

        RecordUndo($"Remove Screen '{SelectedScreen.Title}'");
        Document.Ui.Screens.Remove(SelectedScreen);
        ReviewScreens.Remove(SelectedScreen);
        SelectedScreen = ReviewScreens.FirstOrDefault();
        MarkDirty();
    }

    [RelayCommand]
    private void MoveScreenUp() => MoveSelectedScreen(-1);

    [RelayCommand]
    private void MoveScreenDown() => MoveSelectedScreen(1);

    [RelayCommand]
    private void MoveControlUp() => MoveSelectedControl(-1);

    [RelayCommand]
    private void MoveControlDown() => MoveSelectedControl(1);

    private void MoveSelectedControl(int delta)
    {
        if (SelectedScreen is null || SelectedControl is null) return;
        var index = SelectedScreen.Controls.IndexOf(SelectedControl);
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= SelectedScreen.Controls.Count) return;

        RecordUndo("Reorder Control");
        var control = SelectedControl;
        SelectedScreen.Controls.RemoveAt(index);
        SelectedScreen.Controls.Insert(newIndex, control);

        var reviewIndex = ReviewControls.IndexOf(control);
        if (reviewIndex >= 0)
        {
            var reviewNew = Math.Clamp(reviewIndex + delta, 0, ReviewControls.Count - 1);
            ReviewControls.Move(reviewIndex, reviewNew);
        }

        SelectedControl = control;
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
        MarkDirty();
    }

    [RelayCommand]
    private void AddChoiceList() => AddThing(BlueprintThingKind.ChoiceList);

    [RelayCommand]
    private void AddPlaceToType() => AddThing(BlueprintThingKind.PlaceToType);

    [RelayCommand]
    private void AddButtonThing() => AddThing(BlueprintThingKind.Button);

    [RelayCommand]
    private void AddResultsList() => AddThing(BlueprintThingKind.ResultsList);

    [RelayCommand]
    private void RemoveThing()
    {
        if (SelectedScreen is null || SelectedControl is null) return;
        RecordUndo($"Remove Thing '{SelectedControl.Label}'");
        SelectedScreen.Controls.Remove(SelectedControl);
        ReviewControls.Remove(SelectedControl);
        SelectedControl = ReviewControls.FirstOrDefault();
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
        MarkDirty();
    }

    private void AddThing(BlueprintThingKind kind)
    {
        EnsureDocumentFromIdea();
        if (Document is null || SelectedScreen is null)
        {
            _dialogs.ShowMessage("Select or add a screen first, then add things to it.");
            return;
        }

        RecordUndo($"Add {WizardAnswers.ThingLabel(kind)}");
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        var types = BlueprintCompiler.ResolveControlTypes(profile);
        var control = BlueprintCompiler.CreateThing(kind, types, SelectedScreen.Controls.Count + 1);
        SelectedScreen.Controls.Add(control);
        ReviewControls.Add(control);
        SelectedControl = control;
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
        MarkDirty();
        StatusText = $"Added {WizardAnswers.ThingLabel(kind).ToLowerInvariant()} “{control.Label}”.";
    }

    private void MoveSelectedScreen(int delta)
    {
        if (Document is null || SelectedScreen is null) return;
        var index = Document.Ui.Screens.IndexOf(SelectedScreen);
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= Document.Ui.Screens.Count) return;

        RecordUndo("Reorder Screen");
        var screen = SelectedScreen;
        Document.Ui.Screens.RemoveAt(index);
        Document.Ui.Screens.Insert(newIndex, screen);
        var reviewIndex = ReviewScreens.IndexOf(screen);
        if (reviewIndex >= 0)
        {
            var reviewNew = Math.Clamp(reviewIndex + delta, 0, ReviewScreens.Count - 1);
            ReviewScreens.Move(reviewIndex, reviewNew);
        }

        SelectedScreen = screen;
        MarkDirty();
    }

    private void EnsureDocumentFromIdea()
    {
        ComposeAdLibIntoDescription();

        if (_documentInitialized && Document is not null) return;

        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        Answers.ApplyLookStyleToTheme(_compatibility.AllowedThemes(profile));
        var doc = _compiler.BuildNew(Answers, profile, seedMainScreen: false);
        var templateId = SelectedTemplate?.Id ?? "blank";
        BlueprintTemplates.Apply(templateId, doc, profile);
        if (templateId == "blank")
        {
            var seeded = BlueprintCompiler.SeedAdLibFeatures(doc, Answers, profile);
            if (seeded > 0)
                StatusText = $"Created a blueprint and added {seeded} button(s) from your ad-lib capabilities.";
        }

        Document = _compiler.Compile(doc, Answers, profile);
        _documents.ReplaceDocument(Document, profile, _documents.FilePath);
        _documentInitialized = true;
        MarkDirty();
        if (templateId != "blank")
            StatusText = $"Started from the “{SelectedTemplate?.Title}” template.";
        else if (Answers.GetFeatureLines().Count == 0)
            StatusText = "Created a blank blueprint. Add screens and things next.";
    }

    private void CompileDocument()
    {
        if (Document is null) return;
        ComposeAdLibIntoDescription();
        CommitAnswersToDocument();
        SyncClickFlowStepsToDocument();
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        Document = _compiler.Compile(Document, Answers, profile);
        _documents.ReplaceDocument(Document, profile, _documents.FilePath);
        SyncReviewSelections();
        SyncClickFlowsFromDocument();
    }

    private void ComposeAdLibIntoDescription()
    {
        Answers.ComposeDescription();
        RefreshDescriptionTips();
    }

    private void ApplyRememberToDocument()
    {
        if (Document is null || !_documentInitialized) return;
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        Document = _compiler.Compile(Document, Answers, profile);
        _documents.ReplaceDocument(Document, profile, _documents.FilePath);
        SyncReviewSelections();
    }

    private void ApplyProfileDefaults(string profileId)
    {
        var profile = _schemaCatalog.RequireById(profileId);
        Answers.ProfileId = profileId;
        _compatibility.ApplySafeDefaults(Answers, profile);
        Answers.ApplyLookStyleToTheme(_compatibility.AllowedThemes(profile));
        RefreshOptionLists(profile);
        if (_documents.Document is null || _documents.Profile?.Id != profileId)
            _documents.NewDocument(profile);
    }

    private void RefreshOptionLists(FormSchemaDefinition profile)
    {
        ThemeOptions.Clear();
        foreach (var theme in _compatibility.AllowedThemes(profile))
            ThemeOptions.Add(theme);

        NavigationOptions.Clear();
        foreach (var nav in _compatibility.AllowedNavigationPatterns(profile.Id, Answers.UiFramework, profile))
            NavigationOptions.Add(nav);
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

    private void SyncConnectivityLabel()
    {
        SelectedConnectivityLabel = Answers.Connectivity switch
        {
            ConnectivityChoice.Online => "Online (needs the internet)",
            ConnectivityChoice.SometimesOnline => "Sometimes online",
            _ => "Offline"
        };
    }

    private void SyncAccountsLabel()
    {
        SelectedAccountsLabel = Answers.Accounts switch
        {
            AccountsChoice.Separate => "Yes — users need separate accounts",
            AccountsChoice.Unsure => "I'm not sure",
            _ => "No — anyone using the device can use the app"
        };
    }

    private void SyncMultiDeviceLabel()
    {
        SelectedMultiDeviceLabel = Answers.MultiDevice switch
        {
            MultiDeviceChoice.Yes => "Yes — follow the user across devices",
            MultiDeviceChoice.Later => "Maybe later",
            _ => "No — stays on this device"
        };
    }

    private void SyncDecisionModeLabels()
    {
        SelectedArchitectureDecisionLabel = FormatDecisionMode(Answers.ArchitectureDecision);
        SelectedStorageDecisionLabel = FormatDecisionMode(Answers.StorageDecision);
    }

    private static string FormatDecisionMode(TechnicalDecisionMode mode) => mode switch
    {
        TechnicalDecisionMode.AgentDiscretion => "Let coding agent decide",
        TechnicalDecisionMode.ChooseMyself => "Choose myself",
        _ => "Recommended"
    };

    private static TechnicalDecisionMode ParseDecisionMode(string value) =>
        value.Contains("agent", StringComparison.OrdinalIgnoreCase)
            ? TechnicalDecisionMode.AgentDiscretion
            : value.Contains("myself", StringComparison.OrdinalIgnoreCase)
                ? TechnicalDecisionMode.ChooseMyself
                : TechnicalDecisionMode.Recommended;

    private void SyncDetailLevelLabel()
    {
        SelectedDetailLevelLabel = Answers.DetailLevel.ToString();
        OnPropertyChanged(nameof(ShowStandardDetails));
        OnPropertyChanged(nameof(ShowExpertDetails));
    }

    private void UpdateStepChrome()
    {
        var index = (int)CurrentStep;
        if (CurrentStep != WizardStep.HandOff)
        {
            ProgressValue = (index + 1) / (double)StepList.Count;
            ProgressText = $"Step {index + 1} of {StepList.Count}";
        }

        CanGoBack = CurrentStep > WizardStep.YourIdea;
        CanGoNext = CurrentStep < WizardStep.HandOff;
        IsReadyStep = CurrentStep == WizardStep.HandOff;

        (StepTitle, StepSubtitle) = CurrentStep switch
        {
            WizardStep.YourIdea => ("Tell us about your app",
                "Fill in the blanks — no jargon required. We’ll only ask technical questions later if something is missing."),
            WizardStep.WhatYouNeed => ("What does your app need?",
                "Ordinary questions about accounts, devices, and reminders. “I’m not sure” is always OK."),
            WizardStep.Screens => ("Screens", "Build what people see. Add everyday things — not technical jargon."),
            WizardStep.WhenTheyClick => ("When they click", "Write business steps here; together with your description they drive the real data model."),
            WizardStep.RememberStuff => ("Remember stuff?", "Only turn these on if you really need them."),
            WizardStep.HandOff => ("Project Blueprint",
                "Command center: recommendations with Why?, completeness, detail level, then export for an AI coding agent."),
            _ => ("Core AI Architect", string.Empty)
        };
    }

    private bool ValidateCurrentStep()
    {
        switch (CurrentStep)
        {
            case WizardStep.YourIdea:
                if (SelectedAppKind is null)
                {
                    _dialogs.ShowMessage("Pick what kind of app you want.");
                    return false;
                }

                ComposeAdLibIntoDescription();
                if (string.IsNullOrWhiteSpace(Answers.ProjectName) || string.IsNullOrWhiteSpace(Answers.Description))
                {
                    _dialogs.ShowMessage(
                        "Enter an app name and complete the ad-lib (who it’s for / what it helps them do) " +
                        "or add product rules so we know what to build.");
                    return false;
                }

                return true;

            case WizardStep.WhatYouNeed:
                ComposeAdLibIntoDescription();
                return true;

            case WizardStep.Screens:
                if (Document is null || Document.Ui.Screens.Count == 0)
                {
                    _dialogs.ShowMessage("Add at least one screen.");
                    return false;
                }

                if (Document.Ui.Screens.All(s => s.Controls.Count == 0))
                {
                    _dialogs.ShowMessage("Add at least one thing to a screen (a button, choice list, place to type, or results list).");
                    return false;
                }

                return true;

            case WizardStep.WhenTheyClick:
                SyncClickFlowStepsToDocument();
                if (ClickFlows.Count == 0)
                {
                    if (!_dialogs.Confirm("You have no buttons yet. Continue anyway? You can go back and add a Button on Screens."))
                        return false;
                }

                return true;

            default:
                return true;
        }
    }

    private void SyncReviewSelections()
    {
        if (Document is null) return;
        ReviewScreens.Clear();
        foreach (var screen in Document.Ui.Screens)
            ReviewScreens.Add(screen);
        SelectedScreen = ReviewScreens.FirstOrDefault();
        ReloadReviewControls();
        SelectedControl = ReviewControls.FirstOrDefault();
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
    }

    private void ReloadReviewControls()
    {
        ReviewControls.Clear();
        if (SelectedScreen?.Controls is null) return;
        foreach (var control in SelectedScreen.Controls)
            ReviewControls.Add(control);
    }

    private void SyncClickFlowsFromDocument()
    {
        if (Document is null) return;
        CompileDocumentLightWorkflows();

        var previous = SelectedClickFlow?.Button.Id;
        ClickFlows.Clear();
        var buttons = Document.Ui.Screens.SelectMany(s => s.Controls)
            .Where(c => IsButtonType(c.Type))
            .ToList();

        foreach (var button in buttons)
        {
            var workflow = Document.Workflows.FirstOrDefault(w =>
                w.Trigger.Equals(button.Id, StringComparison.OrdinalIgnoreCase));
            if (workflow is null)
            {
                workflow = new WorkflowInfo
                {
                    Id = button.Id,
                    Name = button.Label,
                    Trigger = button.Id,
                    Steps = { "Read the user's current choices", "Do the action", "Show the outcome on screen" },
                    ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
                };
                Document.Workflows.Add(workflow);
            }

            ClickFlows.Add(new ClickFlowOption(button, workflow));
        }

        SelectedClickFlow = ClickFlows.FirstOrDefault(c => c.Button.Id == previous) ?? ClickFlows.FirstOrDefault();
    }

    private void CompileDocumentLightWorkflows()
    {
        if (Document is null) return;
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        Document = _compiler.Compile(Document, Answers, profile);
        _documents.ReplaceDocument(Document, profile, _documents.FilePath);
    }

    private void SyncClickFlowStepsToDocument()
    {
        foreach (var flow in ClickFlows)
        {
            flow.Workflow.Steps = flow.StepsText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }
    }

    private void CommitAnswersToDocument()
    {
        if (Document is null) return;
        Document.Project.Name = Answers.ProjectName.Trim();
        Document.Project.Description = Answers.Description.Trim();
        Document.Project.Owner = Answers.Owner.Trim();
        Document.Project.Id = Answers.DerivedProjectId;
        Document.Environment.TargetOs = Answers.TargetOs;
        Document.Environment.Runtime = Answers.Runtime;
        Document.Environment.UiFramework = Answers.UiFramework;
        Document.Environment.Language = Answers.Language;
        Document.Ui.Theme = Answers.Theme;
        Document.Ui.NavigationPattern = Answers.NavigationPattern;
    }

    private void HydrateFromDocument(SsotDocument doc)
    {
        _suppressKindChange = true;
        var match = AppKinds.FirstOrDefault(k =>
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

    private void RefreshDescriptionTips()
    {
        DescriptionTips.Clear();
        foreach (var tip in BlueprintTemplates.SuggestFromDescription(Answers.Description))
            DescriptionTips.Add(tip);
    }

    private bool ConfirmDiscard()
    {
        if (!IsDirty) return true;
        return _dialogs.Confirm("Discard unsaved changes?");
    }

    private void MarkDirty()
    {
        IsDirty = true;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        var name = string.IsNullOrWhiteSpace(Answers.ProjectName) ? "Untitled" : Answers.ProjectName;
        var dirty = IsDirty ? "*" : string.Empty;
        WindowTitle = $"Core AI Architect — {name}{dirty}";
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
