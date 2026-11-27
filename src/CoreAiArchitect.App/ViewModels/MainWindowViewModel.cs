using System.Collections.ObjectModel;
using System.Reflection;
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

public partial class WizardStepItem : ObservableObject
{
    public WizardStepItem(WizardStep step, int number, string label, bool isRequired, string requirementTag)
    {
        Step = step;
        Number = number;
        Label = label;
        IsRequired = isRequired;
        RequirementTag = requirementTag;
    }

    public WizardStep Step { get; }
    public int Number { get; }
    public bool IsRequired { get; }
    public string RequirementTag { get; }
    [ObservableProperty] private string _label;
    [ObservableProperty] private bool _isCurrent;
    [ObservableProperty] private bool _isCompleted;
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
    private readonly UserSettingsService _settingsService = new();
    private readonly Core.Models.UserSettings _settings;
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
        _settings = _settingsService.Load();
        foreach (var path in _settings.RecentFiles)
            RecentFiles.Add(path);

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
    public ObservableCollection<string> RecentFiles { get; } = new();
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

    public string AppVersion { get; } = ResolveAppVersion();
    public string AppVersionDisplay => $"v{AppVersion}";

    private static string ResolveAppVersion()
    {
        var asm = typeof(MainWindowViewModel).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plus = info.IndexOf('+');
            return plus > 0 ? info[..plus] : info;
        }
        var ver = asm.GetName().Version;
        return ver is not null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.1.0";
    }

    public bool IsWebApiProfile => string.Equals(Answers.ProfileId, "web_api", StringComparison.OrdinalIgnoreCase);

    public string ScreensGroupHeading => IsWebApiProfile ? "Endpoint Groups" : "Screens";
    public string AddScreenButtonLabel => IsWebApiProfile ? "Add Group" : "Add Screen";
    public string ScreenTitleLabel => IsWebApiProfile ? "Resource / Route Group" : "Screen title";
    public string ScreenTypeLabel => IsWebApiProfile ? "Group type" : "Screen type";
    public string AddThingSectionLabel => IsWebApiProfile ? "Add endpoint or parameter" : "Add a thing";
    public string ButtonThingLabel => IsWebApiProfile ? "Endpoint" : "Button";
    public string ClicksListHeading => IsWebApiProfile ? "Endpoints" : "Buttons";
    public string ClicksFlowQuestion => IsWebApiProfile
        ? "Request handling & response steps for this endpoint:"
        : "What happens when someone uses this button?";

    private void NotifyProfileLabelsChanged()
    {
        OnPropertyChanged(nameof(IsWebApiProfile));
        OnPropertyChanged(nameof(ScreensGroupHeading));
        OnPropertyChanged(nameof(AddScreenButtonLabel));
        OnPropertyChanged(nameof(ScreenTitleLabel));
        OnPropertyChanged(nameof(ScreenTypeLabel));
        OnPropertyChanged(nameof(AddThingSectionLabel));
        OnPropertyChanged(nameof(ButtonThingLabel));
        OnPropertyChanged(nameof(ClicksListHeading));
        OnPropertyChanged(nameof(ClicksFlowQuestion));

        if (StepItems.Count >= 4)
        {
            StepItems[2].Label = IsWebApiProfile ? "3. Endpoints" : "3. Screens";
            StepItems[3].Label = IsWebApiProfile ? "4. Flow" : "4. Clicks";
        }
    }

    public string DescriptionGuidance => DomainScaffoldPolicy.UserGuidanceDescription;
    public string ClicksGuidance => DomainScaffoldPolicy.UserGuidanceClicks;
    public string HandOffGuidance => DomainScaffoldPolicy.UserGuidanceHandOff;
    public string DomainGuidanceShort => DomainScaffoldPolicy.UserGuidanceShort;
    public string AccountsHelp =>
        "You probably don't need accounts if one person/device uses the app and nothing private syncs elsewhere. " +
        "You may need accounts if users need their information on multiple devices or online.";

    public IReadOnlyList<(WizardStep Step, string Title)> StepList { get; } = new (WizardStep, string)[]
    {
        (WizardStep.YourIdea, "1. Idea"),
        (WizardStep.WhatYouNeed, "2. Needs"),
        (WizardStep.Screens, "3. Screens"),
        (WizardStep.WhenTheyClick, "4. Clicks"),
        (WizardStep.RememberStuff, "5. Remember"),
        (WizardStep.HandOff, "6. Blueprint")
    };

    public ObservableCollection<WizardStepItem> StepItems { get; } = new()
    {
        new(WizardStep.YourIdea, 1, "1. Idea", true, "Required"),
        new(WizardStep.WhatYouNeed, 2, "2. Needs", false, "Optional"),
        new(WizardStep.Screens, 3, "3. Screens", true, "Required"),
        new(WizardStep.WhenTheyClick, 4, "4. Clicks", false, "Optional"),
        new(WizardStep.RememberStuff, 5, "5. Remember", false, "Optional"),
        new(WizardStep.HandOff, 6, "6. Blueprint", false, "Export")
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
    [ObservableProperty] private int _wireframeRefreshVersion;
    [ObservableProperty] private bool _showAdvancedControlId;
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

        if (value is null || _suppressKindChange)
        {
            NotifyProfileLabelsChanged();
            return;
        }
        ApplyProfileDefaults(value.ProfileId);
        NotifyProfileLabelsChanged();
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
    private void NavigateToStep(WizardStep target)
    {
        if (target == CurrentStep) return;

        // Validate only when advancing forward out of Idea or Needs
        if (target > CurrentStep && !ValidateCurrentStep()) return;

        if (CurrentStep == WizardStep.YourIdea)
            EnsureDocumentFromIdea();
        else if (CurrentStep == WizardStep.WhenTheyClick)
            SyncClickFlowStepsToDocument();
        else if (CurrentStep == WizardStep.RememberStuff)
            ApplyRememberToDocument();

        if (target > WizardStep.YourIdea)
            EnsureDocumentFromIdea();

        CurrentStep = target;

        if (CurrentStep == WizardStep.Screens)
            SyncReviewSelections();
        else if (CurrentStep == WizardStep.WhenTheyClick)
            SyncClickFlowsFromDocument();
        else if (CurrentStep == WizardStep.HandOff)
            RunReadyCheck();
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
    private void ShowAbout()
    {
        var window = new Views.AboutWindow(AppVersion, IsDarkMode)
        {
            Owner = Application.Current?.MainWindow
        };
        window.ShowDialog();
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
        if (app.MainWindow is MainWindow mainWin)
            MainWindow.ApplyImmersiveDarkMode(mainWin, darkMode);

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

        foreach (var item in StepItems)
        {
            item.IsCurrent = item.Step == CurrentStep;
            item.IsCompleted = item.Step < CurrentStep;
        }

        (StepTitle, StepSubtitle) = CurrentStep switch
        {
            WizardStep.YourIdea => ("Tell us about your app",
                "Fill in the blanks — no jargon required. We’ll only ask technical questions later if something is missing."),
            WizardStep.WhatYouNeed => ("What does your app need?",
                "Ordinary questions about accounts, devices, and reminders. “I’m not sure” is always OK."),
            WizardStep.Screens => IsWebApiProfile
                ? ("Endpoints & Routes", "Define API resource groups, parameters, and endpoints.")
                : ("Screens", "Build what people see. Add everyday things — not technical jargon."),
            WizardStep.WhenTheyClick => IsWebApiProfile
                ? ("Request / Response Flow", "Describe validation, data access, and response steps for each endpoint.")
                : ("When they click", "Write business steps here; together with your description they drive the real data model."),
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

    private void RefreshDescriptionTips()
    {
        DescriptionTips.Clear();
        foreach (var tip in BlueprintTemplates.SuggestFromDescription(Answers.Description))
            DescriptionTips.Add(tip);
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
}
