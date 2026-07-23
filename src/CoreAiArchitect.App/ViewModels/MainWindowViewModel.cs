using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreAiArchitect.App.Services;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.Validation;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.App.ViewModels;

public partial class CapabilityOption : ObservableObject
{
    public CapabilityOption(AppCapability capability, string label, string description)
    {
        Capability = capability;
        Label = label;
        Description = description;
    }

    public AppCapability Capability { get; }
    public string Label { get; }
    public string Description { get; }
    [ObservableProperty] private bool _isSelected;
}

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

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ISchemaCatalogService _schemaCatalog;
    private readonly IDialogService _dialogs;
    private readonly ISsotDocumentService _documents;
    private readonly CompatibilityEngine _compatibility = new();
    private readonly SsotSmartBuilder _smartBuilder = new();
    private readonly SsotReadinessEngine _readiness = new();
    private bool _reviewEdited;
    private bool _suppressCompatPrompt;

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
            new("wpf_desktop", "Desktop window app", "A Windows app with windows, buttons, and forms (WPF)."),
            new("web_api", "Website API", "A backend that other apps or websites call."),
            new("cli_tool", "Command-line tool", "An app you run in a terminal with commands."),
            new("powershell_module", "PowerShell module", "Commands you import into PowerShell."),
            new("winpe_tool", "Recovery-style tool", "A guided tool for recovery or setup scenarios."),
            new("deployment_automation", "Automation pipeline", "Stages and tasks that run in order."),
            new("custom", "Something else", "Start with flexible defaults and refine as you go.")
        };

        Capabilities = new ObservableCollection<CapabilityOption>
        {
            new(AppCapability.PickOptions, WizardAnswers.CapabilityLabel(AppCapability.PickOptions),
                "Example: choose a game, mode, or category."),
            new(AppCapability.GenerateResults, WizardAnswers.CapabilityLabel(AppCapability.GenerateResults),
                "Example: create numbers, reports, or recommendations."),
            new(AppCapability.SaveHistory, WizardAnswers.CapabilityLabel(AppCapability.SaveHistory),
                "Keep past runs so they can be opened again."),
            new(AppCapability.ReviewPastResults, WizardAnswers.CapabilityLabel(AppCapability.ReviewPastResults),
                "A place to browse what was saved before."),
            new(AppCapability.SettingsScreen, WizardAnswers.CapabilityLabel(AppCapability.SettingsScreen),
                "A screen for preferences.")
        };

        Answers = new WizardAnswers();
        SelectedAppKind = AppKinds[0];
        AppKinds[0].IsSelected = true;
        ApplyProfileDefaults(SelectedAppKind.ProfileId, quiet: true);
        CurrentStep = WizardStep.AppKind;
        UpdateStepChrome();
    }

    public ObservableCollection<AppKindOption> AppKinds { get; }
    public ObservableCollection<CapabilityOption> Capabilities { get; }
    public ObservableCollection<string> AvailableThemes { get; } = new();
    public ObservableCollection<string> AvailableNavigationPatterns { get; } = new();
    public ObservableCollection<string> AvailableOsOptions { get; } = new()
    {
        "Windows 11", "Windows 10", "Windows Server 2022", "Cross-Platform"
    };
    public ObservableCollection<string> AvailableLanguages { get; } = new() { "C#", "PowerShell", "F#" };
    public ObservableCollection<string> ReadyMessages { get; } = new();
    public ObservableCollection<string> TriggerOptions { get; } = new();
    public ObservableCollection<ScreenInfo> ReviewScreens { get; } = new();
    public ObservableCollection<ControlInfo> ReviewControls { get; } = new();
    public ObservableCollection<string> AvailableControlTypes { get; } = new();

    public WizardAnswers Answers { get; }

    public IReadOnlyList<(WizardStep Step, string Title)> StepList { get; } = new (WizardStep, string)[]
    {
        (WizardStep.AppKind, "App kind"),
        (WizardStep.NameAndPurpose, "Name"),
        (WizardStep.Platform, "Platform"),
        (WizardStep.LookAndFeel, "Look"),
        (WizardStep.Memory, "Memory"),
        (WizardStep.Capabilities, "Features"),
        (WizardStep.ReviewScreens, "Screens"),
        (WizardStep.ReviewData, "Data"),
        (WizardStep.ReviewWorkflows, "Flows"),
        (WizardStep.Ready, "Ready")
    };

    [ObservableProperty] private WizardStep _currentStep;
    [ObservableProperty] private AppKindOption? _selectedAppKind;
    [ObservableProperty] private string _stepTitle = string.Empty;
    [ObservableProperty] private string _stepSubtitle = string.Empty;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusText = "Start by choosing what kind of app you want.";
    [ObservableProperty] private string _windowTitle = "Core AI Architect";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoNext = true;
    [ObservableProperty] private bool _isReadyStep;
    [ObservableProperty] private bool _isReadyToExport;
    [ObservableProperty] private string _readySummary = string.Empty;
    [ObservableProperty] private MemoryChoice _selectedMemory = MemoryChoice.None;
    [ObservableProperty] private SsotDocument? _document;
    [ObservableProperty] private ScreenInfo? _selectedScreen;
    [ObservableProperty] private ControlInfo? _selectedControl;
    [ObservableProperty] private EntityInfo? _selectedEntity;
    [ObservableProperty] private EntityFieldInfo? _selectedField;
    [ObservableProperty] private WorkflowInfo? _selectedWorkflow;

    public bool MemoryNone
    {
        get => SelectedMemory == MemoryChoice.None;
        set { if (value) SelectedMemory = MemoryChoice.None; }
    }

    public bool MemoryFiles
    {
        get => SelectedMemory == MemoryChoice.SimpleFiles;
        set { if (value) SelectedMemory = MemoryChoice.SimpleFiles; }
    }

    public bool MemoryDatabase
    {
        get => SelectedMemory == MemoryChoice.Database;
        set { if (value) SelectedMemory = MemoryChoice.Database; }
    }

    partial void OnSelectedAppKindChanged(AppKindOption? value)
    {
        foreach (var kind in AppKinds)
            kind.IsSelected = kind == value;

        if (value is null || _suppressCompatPrompt) return;
        ApplyProfileDefaults(value.ProfileId, quiet: false);
    }

    partial void OnSelectedMemoryChanged(MemoryChoice value)
    {
        Answers.Memory = value;
        OnPropertyChanged(nameof(MemoryNone));
        OnPropertyChanged(nameof(MemoryFiles));
        OnPropertyChanged(nameof(MemoryDatabase));
        if (!_suppressCompatPrompt)
            EnsureCompatibleOrRevert();
    }

    partial void OnCurrentStepChanged(WizardStep value) => UpdateStepChrome();

    partial void OnSelectedScreenChanged(ScreenInfo? value)
    {
        ReloadReviewControls();
        SelectedControl = ReviewControls.FirstOrDefault();
    }

    [RelayCommand]
    private void Next()
    {
        if (!ValidateCurrentStep()) return;

        if (CurrentStep == WizardStep.Capabilities)
            RebuildDocumentFromAnswers(force: true);

        if (CurrentStep < WizardStep.Ready)
        {
            CurrentStep++;
            if (CurrentStep is WizardStep.ReviewScreens or WizardStep.ReviewData or WizardStep.ReviewWorkflows)
                SyncReviewSelections();
            if (CurrentStep == WizardStep.Ready)
                RunReadyCheck();
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > WizardStep.AppKind)
            CurrentStep--;
    }

    [RelayCommand]
    private void NewDocument()
    {
        if (!ConfirmDiscard()) return;
        _suppressCompatPrompt = true;
        Answers.ProjectName = string.Empty;
        Answers.Description = string.Empty;
        Answers.Owner = string.Empty;
        Answers.Capabilities.Clear();
        foreach (var c in Capabilities) c.IsSelected = false;
        SelectedAppKind = AppKinds[0];
        ApplyProfileDefaults(SelectedAppKind.ProfileId, quiet: true);
        SelectedMemory = MemoryChoice.None;
        Document = null;
        _reviewEdited = false;
        _documents.NewDocument(_schemaCatalog.RequireById(SelectedAppKind.ProfileId));
        CurrentStep = WizardStep.AppKind;
        IsDirty = false;
        _suppressCompatPrompt = false;
        StatusText = "Started a new guided blueprint.";
        UpdateTitle();
    }

    [RelayCommand]
    private void OpenDocument()
    {
        if (!ConfirmDiscard()) return;
        var path = _dialogs.OpenFile("SSOT JSON|*.json|All files|*.*", "Open SSOT");
        if (path is null) return;

        var profile = _schemaCatalog.RequireById(SelectedAppKind?.ProfileId ?? "wpf_desktop");
        var doc = _documents.Open(path, profile);
        HydrateFromDocument(doc);
        Document = doc;
        _reviewEdited = true;
        CurrentStep = WizardStep.ReviewScreens;
        SyncReviewSelections();
        IsDirty = false;
        StatusText = $"Opened {path}. Review and continue to Ready when finished.";
        UpdateTitle();
    }

    [RelayCommand]
    private void SaveDocument()
    {
        EnsureDocument();
        CommitAnswersToDocument();
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
        EnsureDocument();
        CommitAnswersToDocument();
        var suggested = $"{Answers.DerivedProjectId}.ssot.json";
        var path = _dialogs.SaveFile("SSOT JSON|*.json", "Save SSOT", suggested);
        if (path is null) return;
        _documents.Save(path);
        IsDirty = false;
        StatusText = $"Saved {path}";
        UpdateTitle();
    }

    [RelayCommand]
    private void ExportJson()
    {
        EnsureDocument();
        CommitAnswersToDocument();
        var path = _dialogs.SaveFile("SSOT JSON|*.json", "Export JSON", $"{Answers.DerivedProjectId}.ssot.json");
        if (path is null) return;
        _documents.ExportJson(path);
        StatusText = $"Exported JSON to {path}";
    }

    [RelayCommand]
    private void ExportMarkdown()
    {
        EnsureDocument();
        CommitAnswersToDocument();
        var path = _dialogs.SaveFile("Markdown|*.md", "Export Markdown", $"{Answers.DerivedProjectId}.md");
        if (path is null) return;
        _documents.ExportMarkdown(path);
        StatusText = $"Exported Markdown to {path}";
    }

    [RelayCommand]
    private void RunReadyCheck()
    {
        EnsureDocument();
        CommitAnswersToDocument();
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        var result = _readiness.Assess(Document!, profile);
        var compat = _compatibility.EvaluateDocument(Document!);
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
        StatusText = IsReadyToExport
            ? "Ready — you can save/export for an AI coding agent."
            : "Not ready yet — fix the items listed on this page.";
    }

    [RelayCommand]
    private void ShowHelp()
    {
        _dialogs.ShowMessage(
            "Core AI Architect builds a blueprint (SSOT) for an AI coding agent.\n\n" +
            "Answer each plain-language step. The app creates screens, data, and workflows for you.\n\n" +
            "If a choice conflicts with an earlier one, you’ll be stopped and told what to pick instead.");
    }

    [RelayCommand]
    private void MarkReviewEdited()
    {
        _reviewEdited = true;
        IsDirty = true;
        UpdateTitle();
    }

    [RelayCommand]
    private void AddScreen()
    {
        EnsureDocument();
        if (Document is null) return;

        var n = Document.Ui.Screens.Count + 1;
        var screen = new ScreenInfo
        {
            Id = $"screen{n}",
            Title = $"Screen {n}",
            Route = n == 1 ? "/" : $"/screen{n}",
            Controls = new List<ControlInfo>()
        };
        Document.Ui.Screens.Add(screen);
        ReviewScreens.Add(screen);
        SelectedScreen = screen;
        MarkReviewEdited();
        StatusText = $"Added screen “{screen.Title}”. Rename it and add buttons or controls.";
    }

    [RelayCommand]
    private void RemoveScreen()
    {
        if (Document is null || SelectedScreen is null) return;
        Document.Ui.Screens.Remove(SelectedScreen);
        ReviewScreens.Remove(SelectedScreen);
        SelectedScreen = ReviewScreens.FirstOrDefault();
        ReloadReviewControls();
        RefreshTriggerOptions();
        MarkReviewEdited();
    }

    [RelayCommand]
    private void MoveScreenUp()
    {
        MoveSelectedScreen(-1);
    }

    [RelayCommand]
    private void MoveScreenDown()
    {
        MoveSelectedScreen(1);
    }

    private void MoveSelectedScreen(int delta)
    {
        if (Document is null || SelectedScreen is null) return;

        var index = Document.Ui.Screens.IndexOf(SelectedScreen);
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= Document.Ui.Screens.Count) return;

        var screen = SelectedScreen;
        Document.Ui.Screens.RemoveAt(index);
        Document.Ui.Screens.Insert(newIndex, screen);
        ReviewScreens.Move(index, newIndex);
        SelectedScreen = screen;
        MarkReviewEdited();
        StatusText = delta < 0
            ? $"Moved “{screen.Title}” up."
            : $"Moved “{screen.Title}” down.";
    }

    [RelayCommand]
    private void AddButton()
    {
        AddControlOfType(ResolveButtonType(), "Button", "DoAction");
    }

    [RelayCommand]
    private void AddControl()
    {
        var type = ResolveInputControlType();
        AddControlOfType(type, type, "SelectOption");
    }

    [RelayCommand]
    private void RemoveControl()
    {
        if (SelectedScreen is null || SelectedControl is null) return;
        SelectedScreen.Controls.Remove(SelectedControl);
        ReviewControls.Remove(SelectedControl);
        SelectedControl = ReviewControls.FirstOrDefault();
        RefreshTriggerOptions();
        MarkReviewEdited();
    }

    private void AddControlOfType(string type, string labelPrefix, string action)
    {
        EnsureDocument();
        if (Document is null || SelectedScreen is null)
        {
            _dialogs.ShowMessage("Select or add a screen first, then add buttons or controls.");
            return;
        }

        var n = SelectedScreen.Controls.Count + 1;
        var idBase = char.ToLowerInvariant(type[0]) + type[1..];
        var control = new ControlInfo
        {
            Id = $"{idBase}{n}",
            Type = type,
            Label = $"{labelPrefix} {n}",
            Action = action
        };
        SelectedScreen.Controls.Add(control);
        ReviewControls.Add(control);
        SelectedControl = control;
        RefreshTriggerOptions();
        MarkReviewEdited();
        StatusText = $"Added {type} “{control.Label}” on “{SelectedScreen.Title}”.";
    }

    private string ResolveButtonType()
    {
        EnsureControlTypesLoaded();
        return AvailableControlTypes.FirstOrDefault(t =>
                   t.Equals("Button", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Command", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Cmdlet", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
               ?? AvailableControlTypes.FirstOrDefault()
               ?? "Button";
    }

    private string ResolveInputControlType()
    {
        EnsureControlTypesLoaded();
        return AvailableControlTypes.FirstOrDefault(t =>
                   t.Equals("ComboBox", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("TextBox", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("ListView", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Option", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Parameter", StringComparison.OrdinalIgnoreCase))
               ?? AvailableControlTypes.FirstOrDefault(t => !t.Equals("Button", StringComparison.OrdinalIgnoreCase))
               ?? "ComboBox";
    }

    private void EnsureControlTypesLoaded()
    {
        if (AvailableControlTypes.Count > 0) return;
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        var types = profile.Sections
            .SelectMany(s => s.Fields)
            .Where(f => f.Path.Equals("ui.screens", StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => f.ItemSchema ?? new List<CoreAiArchitect.Core.Models.FormSchema.FieldDefinition>())
            .Where(f => f.Path.Equals("controls", StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => f.ItemSchema ?? new List<CoreAiArchitect.Core.Models.FormSchema.FieldDefinition>())
            .FirstOrDefault(f => f.Path.Equals("type", StringComparison.OrdinalIgnoreCase))
            ?.Values;

        AvailableControlTypes.Clear();
        foreach (var t in types ?? new List<string> { "Button", "TextBox", "ComboBox", "ListView", "CheckBox" })
            AvailableControlTypes.Add(t);
    }

    private void ApplyProfileDefaults(string profileId, bool quiet)
    {
        var profile = _schemaCatalog.RequireById(profileId);
        Answers.ProfileId = profileId;
        _compatibility.ApplySafeDefaults(Answers, profile);

        AvailableThemes.Clear();
        foreach (var t in _compatibility.AllowedThemes(profile))
            AvailableThemes.Add(t);

        AvailableNavigationPatterns.Clear();
        foreach (var n in _compatibility.AllowedNavigationPatterns(profileId, Answers.UiFramework, profile))
            AvailableNavigationPatterns.Add(n);

        AvailableControlTypes.Clear();
        var controlTypes = profile.Sections
            .SelectMany(s => s.Fields)
            .Where(f => f.Path.Equals("ui.screens", StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => f.ItemSchema ?? new List<CoreAiArchitect.Core.Models.FormSchema.FieldDefinition>())
            .Where(f => f.Path.Equals("controls", StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => f.ItemSchema ?? new List<CoreAiArchitect.Core.Models.FormSchema.FieldDefinition>())
            .FirstOrDefault(f => f.Path.Equals("type", StringComparison.OrdinalIgnoreCase))
            ?.Values;
        foreach (var t in controlTypes ?? new List<string> { "Button", "TextBox", "ComboBox", "ListView", "CheckBox" })
            AvailableControlTypes.Add(t);

        if (!quiet)
        {
            var compat = _compatibility.Evaluate(Answers, profile);
            if (!compat.IsCompatible)
            {
                _dialogs.ShowMessage(
                    string.Join(Environment.NewLine, compat.Issues.Select(i => $"• {i.Message}\n  {i.SuggestedFix}")),
                    "That combination won’t work",
                    isError: true);
            }
        }

        if (_documents.Document is null || _documents.Profile?.Id != profileId)
            _documents.NewDocument(profile);
    }

    public bool EnsureCompatibleOrShow() => EnsureCompatibleOrRevert();

    private bool EnsureCompatibleOrRevert()
    {
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        var compat = _compatibility.Evaluate(Answers, profile);
        if (compat.IsCompatible) return true;

        _dialogs.ShowMessage(
            string.Join(Environment.NewLine + Environment.NewLine,
                compat.Issues.Select(i => i.Message + Environment.NewLine + i.SuggestedFix)),
            "That choice doesn’t work with your earlier answers",
            isError: true);

        _compatibility.ApplySafeDefaults(Answers, profile);
        OnPropertyChanged(nameof(Answers));
        return false;
    }

    private bool ValidateCurrentStep()
    {
        switch (CurrentStep)
        {
            case WizardStep.AppKind:
                if (SelectedAppKind is null)
                {
                    _dialogs.ShowMessage("Pick what kind of app you want to build.");
                    return false;
                }
                return EnsureCompatibleOrRevert();

            case WizardStep.NameAndPurpose:
                if (string.IsNullOrWhiteSpace(Answers.ProjectName) || string.IsNullOrWhiteSpace(Answers.Description))
                {
                    _dialogs.ShowMessage("Enter an app name and a short description of what it should do.");
                    return false;
                }
                return true;

            case WizardStep.Platform:
            case WizardStep.LookAndFeel:
            case WizardStep.Memory:
                return EnsureCompatibleOrRevert();

            case WizardStep.Capabilities:
                SyncCapabilitiesFromUi();
                if (Answers.Capabilities.Count == 0
                    && !_dialogs.Confirm("You didn’t pick any features. Continue anyway? We’ll still create a simple starter app."))
                    return false;
                return true;

            default:
                return true;
        }
    }

    private void SyncCapabilitiesFromUi()
    {
        Answers.Capabilities.Clear();
        foreach (var option in Capabilities.Where(c => c.IsSelected))
            Answers.Capabilities.Add(option.Capability);
    }

    private void RebuildDocumentFromAnswers(bool force)
    {
        if (!force && _reviewEdited)
        {
            if (!_dialogs.Confirm("You already tweaked the generated screens/data/flows. Rebuild from your answers and replace those tweaks?"))
                return;
        }

        SyncCapabilitiesFromUi();
        Answers.Memory = SelectedMemory;
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        if (!EnsureCompatibleOrRevert()) return;

        Document = _smartBuilder.Build(Answers, profile);
        var path = _documents.FilePath;
        _documents.ReplaceDocument(Document, profile, path);
        _reviewEdited = false;
        IsDirty = true;
        SyncReviewSelections();
        UpdateTitle();
        StatusText = "Starter blueprint created from your answers. You can tweak it on the next pages.";
    }

    private void EnsureDocument()
    {
        if (Document is not null) return;
        RebuildDocumentFromAnswers(force: true);
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

    private void SyncReviewSelections()
    {
        if (Document is null) return;
        ReloadReviewScreens();
        SelectedScreen = ReviewScreens.FirstOrDefault();
        ReloadReviewControls();
        SelectedControl = ReviewControls.FirstOrDefault();
        SelectedEntity = Document.Domain.Entities.FirstOrDefault();
        SelectedField = SelectedEntity?.Fields.FirstOrDefault();
        SelectedWorkflow = Document.Workflows.FirstOrDefault();
        RefreshTriggerOptions();
    }

    private void ReloadReviewScreens()
    {
        ReviewScreens.Clear();
        if (Document is null) return;
        foreach (var screen in Document.Ui.Screens)
            ReviewScreens.Add(screen);
    }

    private void ReloadReviewControls()
    {
        ReviewControls.Clear();
        if (SelectedScreen?.Controls is null) return;
        foreach (var control in SelectedScreen.Controls)
            ReviewControls.Add(control);
    }

    private void RefreshTriggerOptions()
    {
        TriggerOptions.Clear();
        if (Document is null) return;
        foreach (var id in Document.Ui.Screens.SelectMany(s => s.Controls).Select(c => c.Id).Where(id => !string.IsNullOrWhiteSpace(id)))
            TriggerOptions.Add(id);
    }

    private void HydrateFromDocument(SsotDocument doc)
    {
        _suppressCompatPrompt = true;
        Answers.ProjectName = doc.Project.Name;
        Answers.Description = doc.Project.Description;
        Answers.Owner = doc.Project.Owner;
        Answers.TargetOs = doc.Environment.TargetOs;
        Answers.Runtime = doc.Environment.Runtime;
        Answers.UiFramework = doc.Environment.UiFramework;
        Answers.Language = doc.Environment.Language;
        Answers.Theme = doc.Ui.Theme;
        Answers.NavigationPattern = doc.Ui.NavigationPattern;
        SelectedMemory = doc.Data.Storage.Type switch
        {
            "LocalJson" or "Registry" => MemoryChoice.SimpleFiles,
            "SQLite" or "SQLServer" or "PostgreSQL" => MemoryChoice.Database,
            _ => MemoryChoice.None
        };
        Answers.Memory = SelectedMemory;

        var match = AppKinds.FirstOrDefault(k =>
            string.Equals(_schemaCatalog.RequireById(k.ProfileId).EnvironmentDefaults.UiFramework,
                doc.Environment.UiFramework, StringComparison.OrdinalIgnoreCase));
        SelectedAppKind = match ?? AppKinds[0];
        Answers.ProfileId = SelectedAppKind.ProfileId;
        ApplyProfileDefaults(Answers.ProfileId, quiet: true);
        _suppressCompatPrompt = false;
    }

    private void UpdateStepChrome()
    {
        var index = (int)CurrentStep;
        ProgressValue = (index + 1) / (double)StepList.Count;
        ProgressText = $"Step {index + 1} of {StepList.Count}";
        CanGoBack = CurrentStep > WizardStep.AppKind;
        CanGoNext = CurrentStep < WizardStep.Ready;
        IsReadyStep = CurrentStep == WizardStep.Ready;

        (StepTitle, StepSubtitle) = CurrentStep switch
        {
            WizardStep.AppKind => ("What kind of app?", "Pick the closest match. You can still describe details next."),
            WizardStep.NameAndPurpose => ("Name and purpose", "Tell us what to call it and what it should do."),
            WizardStep.Platform => ("Where it runs", "We pre-filled safe defaults for your app kind."),
            WizardStep.LookAndFeel => ("Look and movement", "Only choices that work with your app kind are listed."),
            WizardStep.Memory => ("What should it remember?", "This controls whether the app saves information."),
            WizardStep.Capabilities => ("What should the app do?", "We’ll turn your checks into screens, data, and click-flows."),
            WizardStep.ReviewScreens => ("Screens and buttons", "Add screens, then Add Button (actions) or Add Control (lists/text boxes). Rename to match your app."),
            WizardStep.ReviewData => ("Information it stores", "These are the data objects an AI agent will build."),
            WizardStep.ReviewWorkflows => ("What happens when someone clicks", "Each flow needs steps and a starting button."),
            WizardStep.Ready => ("Ready check", "We’ll verify everything is compatible and complete."),
            _ => ("Core AI Architect", string.Empty)
        };
    }

    private bool ConfirmDiscard()
    {
        if (!IsDirty) return true;
        return _dialogs.Confirm("Discard unsaved changes?");
    }

    private void UpdateTitle()
    {
        var name = string.IsNullOrWhiteSpace(Answers.ProjectName) ? "Untitled" : Answers.ProjectName;
        var dirty = IsDirty ? "*" : string.Empty;
        WindowTitle = $"Core AI Architect — {name}{dirty}";
    }
}
