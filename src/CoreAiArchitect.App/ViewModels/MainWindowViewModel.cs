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
            new("web_api", "Website / API", "A backend that websites or other apps call."),
            new("custom", "Something else", "We’ll use flexible defaults you can refine later.")
        };

        Templates = new ObservableCollection<BlueprintTemplateOption>(
            BlueprintTemplates.All.Select(t => new BlueprintTemplateOption(t.Id, t.Title, t.Description)));

        Answers = new WizardAnswers();
        SelectedAppKind = AppKinds[0];
        AppKinds[0].IsSelected = true;
        SelectedTemplate = Templates[0];
        Templates[0].IsSelected = true;
        ApplyProfileDefaults(SelectedAppKind.ProfileId);
        CurrentStep = WizardStep.YourIdea;
        UpdateStepChrome();
        RefreshDescriptionTips();
    }

    public ObservableCollection<AppKindOption> AppKinds { get; }
    public ObservableCollection<BlueprintTemplateOption> Templates { get; }
    public ObservableCollection<ScreenInfo> ReviewScreens { get; } = new();
    public ObservableCollection<ControlInfo> ReviewControls { get; } = new();
    public ObservableCollection<ClickFlowOption> ClickFlows { get; } = new();
    public ObservableCollection<string> ReadyMessages { get; } = new();
    public ObservableCollection<string> DescriptionTips { get; } = new();

    public WizardAnswers Answers { get; }

    public IReadOnlyList<(WizardStep Step, string Title)> StepList { get; } = new (WizardStep, string)[]
    {
        (WizardStep.YourIdea, "Your idea"),
        (WizardStep.Screens, "Screens"),
        (WizardStep.WhenTheyClick, "Clicks"),
        (WizardStep.RememberStuff, "Remember"),
        (WizardStep.HandOff, "Hand off")
    };

    [ObservableProperty] private WizardStep _currentStep;
    [ObservableProperty] private AppKindOption? _selectedAppKind;
    [ObservableProperty] private BlueprintTemplateOption? _selectedTemplate;
    [ObservableProperty] private string _stepTitle = string.Empty;
    [ObservableProperty] private string _stepSubtitle = string.Empty;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusText = "Start by naming your app and saying what it should do.";
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
        Answers.Description = string.Empty;
        Answers.Owner = string.Empty;
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
        StatusText = "Started a new app blueprint.";
        UpdateTitle();
        RefreshDescriptionTips();
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
    private void RunReadyCheck()
    {
        EnsureDocumentFromIdea();
        CompileDocument();
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
        AppStory = BlueprintCompiler.BuildAppStory(Document!, Answers);
        StatusText = IsReadyToExport
            ? "Ready — you can save and hand this blueprint to an AI coding agent."
            : "Not ready yet — fix the items listed on this page.";
    }

    [RelayCommand]
    private void ShowHelp()
    {
        _dialogs.ShowMessage(
            "Core AI Architect helps you describe an app in plain language.\n\n" +
            "1. Name it and say what it should do.\n" +
            "2. Build each screen with everyday things (choice lists, boxes to type, buttons, results).\n" +
            "3. Write what happens when someone clicks a button.\n" +
            "4. Choose whether it remembers past results.\n" +
            "5. Hand the App Blueprint to an AI coding agent to build it.\n\n" +
            "You do not need to know programming or AI tools — just describe what people see and do.");
    }

    [RelayCommand]
    private void MarkReviewEdited() => MarkDirty();

    [RelayCommand]
    private void RefreshTips() => RefreshDescriptionTips();

    [RelayCommand]
    private void AddScreen()
    {
        EnsureDocumentFromIdea();
        if (Document is null) return;

        var n = Document.Ui.Screens.Count(s => !IsSystemScreen(s)) + 1;
        var screen = new ScreenInfo
        {
            Id = $"screen{n}",
            Title = $"Screen {n}",
            Route = n == 1 ? "/" : $"/screen{n}"
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

        if (IsSystemScreen(SelectedScreen) && SelectedScreen.Id.Equals("settings", StringComparison.OrdinalIgnoreCase))
        {
            // allow editing
        }

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
        if (_documentInitialized && Document is not null) return;

        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        var doc = _compiler.BuildNew(Answers, profile, seedMainScreen: false);
        var templateId = SelectedTemplate?.Id ?? "blank";
        BlueprintTemplates.Apply(templateId, doc, profile);
        if (Answers.SavePastResults || Answers.IncludeSettingsScreen)
            _compiler.Compile(doc, Answers, profile);
        else
            _compiler.Compile(doc, Answers, profile);

        Document = doc;
        _documents.ReplaceDocument(doc, profile, _documents.FilePath);
        _documentInitialized = true;
        MarkDirty();
        StatusText = templateId == "blank"
            ? "Created a blank blueprint. Add screens and things next."
            : $"Started from the “{SelectedTemplate?.Title}” template.";
    }

    private void CompileDocument()
    {
        if (Document is null) return;
        CommitAnswersToDocument();
        SyncClickFlowStepsToDocument();
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        Document = _compiler.Compile(Document, Answers, profile);
        _documents.ReplaceDocument(Document, profile, _documents.FilePath);
        SyncReviewSelections();
        SyncClickFlowsFromDocument();
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
        if (_documents.Document is null || _documents.Profile?.Id != profileId)
            _documents.NewDocument(profile);
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

                if (string.IsNullOrWhiteSpace(Answers.ProjectName) || string.IsNullOrWhiteSpace(Answers.Description))
                {
                    _dialogs.ShowMessage("Enter an app name and a short description of what it should do.");
                    return false;
                }

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
        Answers.Description = doc.Project.Description;
        Answers.Owner = doc.Project.Owner;
        Answers.TargetOs = doc.Environment.TargetOs;
        Answers.Runtime = doc.Environment.Runtime;
        Answers.UiFramework = doc.Environment.UiFramework;
        Answers.Language = doc.Environment.Language;
        Answers.Theme = doc.Ui.Theme;
        Answers.NavigationPattern = doc.Ui.NavigationPattern;

        Answers.SavePastResults = doc.Ui.Screens.Any(s => s.Id.Equals("history", StringComparison.OrdinalIgnoreCase))
                                  || !string.Equals(doc.Data.Storage.Type, "None", StringComparison.OrdinalIgnoreCase);
        Answers.IncludeSettingsScreen = doc.Ui.Screens.Any(s => s.Id.Equals("settings", StringComparison.OrdinalIgnoreCase));
        Answers.Memory = Answers.SavePastResults ? MemoryChoice.SimpleFiles : MemoryChoice.None;

        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == "blank") ?? Templates[0];
        _documents.SetProfile(profile);
        _suppressKindChange = false;
        OnPropertyChanged(nameof(SavePastResults));
        OnPropertyChanged(nameof(IncludeSettingsScreen));
        RefreshDescriptionTips();
    }

    private void RefreshDescriptionTips()
    {
        DescriptionTips.Clear();
        foreach (var tip in BlueprintTemplates.SuggestFromDescription(Answers.Description))
            DescriptionTips.Add(tip);
    }

    private void UpdateStepChrome()
    {
        var index = (int)CurrentStep;
        ProgressValue = (index + 1) / (double)StepList.Count;
        ProgressText = $"Step {index + 1} of {StepList.Count}";
        CanGoBack = CurrentStep > WizardStep.YourIdea;
        CanGoNext = CurrentStep < WizardStep.HandOff;
        IsReadyStep = CurrentStep == WizardStep.HandOff;

        (StepTitle, StepSubtitle) = CurrentStep switch
        {
            WizardStep.YourIdea => ("Your idea", "Name your app, say what it should do, and pick a starting shape."),
            WizardStep.Screens => ("Screens", "Build what people see. Add everyday things — not technical jargon."),
            WizardStep.WhenTheyClick => ("When they click", "For each button, write the steps in plain English."),
            WizardStep.RememberStuff => ("Remember stuff?", "Only turn these on if you really need them."),
            WizardStep.HandOff => ("Hand off to AI", "We’ll check your blueprint and show a plain-English app story."),
            _ => ("Core AI Architect", string.Empty)
        };
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
