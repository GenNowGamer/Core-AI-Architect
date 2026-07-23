using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.App.ViewModels;

public partial class WorkflowBuilderViewModel : ObservableObject
{
    public WorkflowBuilderViewModel(SsotDocument document, FormSchemaDefinition profile, Action onChanged)
    {
        Document = document;
        OnChanged = onChanged;
        ApplyProfileOptions(profile);
        Reload();
    }

    public SsotDocument Document { get; }
    private Action OnChanged { get; }

    public ObservableCollection<WorkflowInfo> Workflows { get; } = new();
    public ObservableCollection<string> AvailableTriggers { get; } = new();

    [ObservableProperty] private WorkflowInfo? _selectedWorkflow;
    [ObservableProperty] private string _draftStep = string.Empty;
    [ObservableProperty] private string _selectedWorkflowId = string.Empty;
    [ObservableProperty] private string _selectedWorkflowName = string.Empty;
    [ObservableProperty] private string _selectedWorkflowTrigger = string.Empty;
    [ObservableProperty] private string? _selectedErrorStrategy;
    [ObservableProperty] private string? _selectedLogLevel;

    public ObservableCollection<string> Steps { get; } = new();

    public ObservableCollection<string> ErrorStrategies { get; } = new();
    public ObservableCollection<string> LogLevels { get; } = new();

    private void ApplyProfileOptions(FormSchemaDefinition profile)
    {
        ErrorStrategies.Clear();
        foreach (var value in ProfileEnumHelper.GetItemSchemaEnumValues(profile, "workflows", "errorHandling.strategy")
                     ?? new[] { "ShowMessageBox", "Throw", "LogAndContinue", "Retry" })
            ErrorStrategies.Add(value);

        LogLevels.Clear();
        foreach (var value in ProfileEnumHelper.GetItemSchemaEnumValues(profile, "workflows", "errorHandling.logLevel")
                     ?? new[] { "Trace", "Debug", "Information", "Warning", "Error" })
            LogLevels.Add(value);
    }

    public void Reload()
    {
        var previousId = SelectedWorkflow?.Id;

        Workflows.Clear();
        foreach (var workflow in Document.Workflows)
        {
            workflow.ErrorHandling ??= new ErrorHandlingInfo();
            workflow.Steps ??= new List<string>();
            Workflows.Add(workflow);
        }

        AvailableTriggers.Clear();
        foreach (var control in Document.Ui.Screens.SelectMany(s => s.Controls))
        {
            if (!string.IsNullOrWhiteSpace(control.Id))
                AvailableTriggers.Add(control.Id);
        }

        SelectedWorkflow = Workflows.FirstOrDefault(w => w.Id == previousId) ?? Workflows.FirstOrDefault();
        SyncSelectedWorkflowFields();
        LoadSelectedSteps();
    }

    partial void OnSelectedWorkflowChanged(WorkflowInfo? value)
    {
        SyncSelectedWorkflowFields();
        LoadSelectedSteps();
    }

    private void SyncSelectedWorkflowFields()
    {
        if (SelectedWorkflow is null)
        {
            SelectedWorkflowId = string.Empty;
            SelectedWorkflowName = string.Empty;
            SelectedWorkflowTrigger = string.Empty;
            SelectedErrorStrategy = null;
            SelectedLogLevel = null;
            return;
        }

        SelectedWorkflow.ErrorHandling ??= new ErrorHandlingInfo();
        SelectedWorkflowId = SelectedWorkflow.Id;
        SelectedWorkflowName = SelectedWorkflow.Name;
        SelectedWorkflowTrigger = SelectedWorkflow.Trigger;
        SelectedErrorStrategy = SelectedWorkflow.ErrorHandling.Strategy;
        SelectedLogLevel = SelectedWorkflow.ErrorHandling.LogLevel;
    }

    partial void OnSelectedWorkflowIdChanged(string value)
    {
        if (SelectedWorkflow is null || SelectedWorkflow.Id == value) return;
        SelectedWorkflow.Id = value;
        MarkDirty();
        // Refresh list display
        OnPropertyChanged(nameof(Workflows));
    }

    partial void OnSelectedWorkflowNameChanged(string value)
    {
        if (SelectedWorkflow is null || SelectedWorkflow.Name == value) return;
        SelectedWorkflow.Name = value;
        MarkDirty();
        OnPropertyChanged(nameof(Workflows));
    }

    partial void OnSelectedWorkflowTriggerChanged(string value)
    {
        if (SelectedWorkflow is null || SelectedWorkflow.Trigger == value) return;
        SelectedWorkflow.Trigger = value;
        MarkDirty();
    }

    partial void OnSelectedErrorStrategyChanged(string? value)
    {
        if (SelectedWorkflow?.ErrorHandling is null || value is null) return;
        if (SelectedWorkflow.ErrorHandling.Strategy == value) return;
        SelectedWorkflow.ErrorHandling.Strategy = value;
        MarkDirty();
    }

    partial void OnSelectedLogLevelChanged(string? value)
    {
        if (SelectedWorkflow?.ErrorHandling is null || value is null) return;
        if (SelectedWorkflow.ErrorHandling.LogLevel == value) return;
        SelectedWorkflow.ErrorHandling.LogLevel = value;
        MarkDirty();
    }

    private void LoadSelectedSteps()
    {
        Steps.Clear();
        if (SelectedWorkflow is null) return;
        SelectedWorkflow.Steps ??= new List<string>();
        foreach (var step in SelectedWorkflow.Steps)
            Steps.Add(step);
    }

    [RelayCommand]
    private void AddWorkflow()
    {
        var workflow = new WorkflowInfo
        {
            Id = $"workflow{Document.Workflows.Count + 1}",
            Name = $"Workflow {Document.Workflows.Count + 1}",
            ErrorHandling = new ErrorHandlingInfo
            {
                Strategy = ErrorStrategies.FirstOrDefault() ?? "ShowMessageBox",
                LogLevel = LogLevels.Contains("Warning") ? "Warning" : (LogLevels.FirstOrDefault() ?? "Warning")
            }
        };
        Document.Workflows.Add(workflow);
        Workflows.Add(workflow);
        SelectedWorkflow = workflow;
        MarkDirty();
    }

    [RelayCommand]
    private void RemoveWorkflow()
    {
        if (SelectedWorkflow is null) return;
        Document.Workflows.Remove(SelectedWorkflow);
        Workflows.Remove(SelectedWorkflow);
        SelectedWorkflow = Workflows.FirstOrDefault();
        MarkDirty();
    }

    [RelayCommand]
    private void AddStep()
    {
        if (SelectedWorkflow is null || string.IsNullOrWhiteSpace(DraftStep)) return;
        SelectedWorkflow.Steps.Add(DraftStep.Trim());
        Steps.Add(DraftStep.Trim());
        DraftStep = string.Empty;
        MarkDirty();
    }

    [RelayCommand]
    private void RemoveStep(string? step)
    {
        if (SelectedWorkflow is null || step is null) return;
        SelectedWorkflow.Steps.Remove(step);
        Steps.Remove(step);
        MarkDirty();
    }

    [RelayCommand]
    private void MoveStepUp(string? step)
    {
        if (SelectedWorkflow is null || step is null) return;
        var index = SelectedWorkflow.Steps.IndexOf(step);
        if (index <= 0) return;
        SelectedWorkflow.Steps.RemoveAt(index);
        SelectedWorkflow.Steps.Insert(index - 1, step);
        LoadSelectedSteps();
        MarkDirty();
    }

    [RelayCommand]
    private void MoveStepDown(string? step)
    {
        if (SelectedWorkflow is null || step is null) return;
        var index = SelectedWorkflow.Steps.IndexOf(step);
        if (index < 0 || index >= SelectedWorkflow.Steps.Count - 1) return;
        SelectedWorkflow.Steps.RemoveAt(index);
        SelectedWorkflow.Steps.Insert(index + 1, step);
        LoadSelectedSteps();
        MarkDirty();
    }

    [RelayCommand]
    private void ApplyTrigger(string? trigger)
    {
        if (SelectedWorkflow is null || string.IsNullOrWhiteSpace(trigger)) return;
        SelectedWorkflowTrigger = trigger;
    }

    private void MarkDirty() => OnChanged();
}

public partial class UiLayoutBuilderViewModel : ObservableObject
{
    public UiLayoutBuilderViewModel(SsotDocument document, FormSchemaDefinition profile, Action onChanged)
    {
        Document = document;
        OnChanged = onChanged;
        ApplyProfileOptions(profile);
        Theme = Document.Ui.Theme;
        NavigationPattern = Document.Ui.NavigationPattern;
        Reload();
    }

    public SsotDocument Document { get; }
    private Action OnChanged { get; }

    public ObservableCollection<ScreenInfo> Screens { get; } = new();
    public ObservableCollection<ControlInfo> Controls { get; } = new();
    public ObservableCollection<string> Themes { get; } = new();
    public ObservableCollection<string> NavigationPatterns { get; } = new();
    public ObservableCollection<string> ControlTypes { get; } = new();

    [ObservableProperty] private ScreenInfo? _selectedScreen;
    [ObservableProperty] private ControlInfo? _selectedControl;
    [ObservableProperty] private string _theme = "Default";
    [ObservableProperty] private string _navigationPattern = "Window+Dialogs";
    [ObservableProperty] private string _selectedScreenId = string.Empty;
    [ObservableProperty] private string _selectedScreenTitle = string.Empty;
    [ObservableProperty] private string _selectedScreenRoute = string.Empty;
    [ObservableProperty] private string _selectedControlId = string.Empty;
    [ObservableProperty] private string _selectedControlType = string.Empty;
    [ObservableProperty] private string _selectedControlLabel = string.Empty;
    [ObservableProperty] private string _selectedControlAction = string.Empty;

    private void ApplyProfileOptions(FormSchemaDefinition profile)
    {
        Themes.Clear();
        foreach (var value in ProfileEnumHelper.GetFieldEnumValues(profile, "ui", "ui.theme")
                     ?? new[] { "Default", "Fluent", "Custom" })
            Themes.Add(value);

        NavigationPatterns.Clear();
        foreach (var value in ProfileEnumHelper.GetFieldEnumValues(profile, "ui", "ui.navigationPattern")
                     ?? new[] { "Window+Dialogs", "NavigationFrame", "Tabbed", "Custom" })
            NavigationPatterns.Add(value);

        ControlTypes.Clear();
        foreach (var value in ProfileEnumHelper.GetNestedItemSchemaEnumValues(profile, "ui", "ui.screens", "controls", "type")
                     ?? new[] { "Button", "TextBox", "DataGrid", "ListView", "MenuItem", "ComboBox", "CheckBox", "Custom" })
            ControlTypes.Add(value);
    }

    public void Reload()
    {
        var previousScreenId = SelectedScreen?.Id;
        var previousControlId = SelectedControl?.Id;

        Screens.Clear();
        foreach (var screen in Document.Ui.Screens)
        {
            screen.Controls ??= new List<ControlInfo>();
            Screens.Add(screen);
        }

        Theme = Document.Ui.Theme;
        NavigationPattern = Document.Ui.NavigationPattern;

        SelectedScreen = Screens.FirstOrDefault(s => s.Id == previousScreenId) ?? Screens.FirstOrDefault();
        LoadControls();
        SelectedControl = Controls.FirstOrDefault(c => c.Id == previousControlId) ?? Controls.FirstOrDefault();
        SyncSelectedScreenFields();
        SyncSelectedControlFields();
    }

    partial void OnSelectedScreenChanged(ScreenInfo? value)
    {
        LoadControls();
        SelectedControl = Controls.FirstOrDefault();
        SyncSelectedScreenFields();
    }

    partial void OnSelectedControlChanged(ControlInfo? value) => SyncSelectedControlFields();

    private void SyncSelectedScreenFields()
    {
        if (SelectedScreen is null)
        {
            SelectedScreenId = string.Empty;
            SelectedScreenTitle = string.Empty;
            SelectedScreenRoute = string.Empty;
            return;
        }

        SelectedScreenId = SelectedScreen.Id;
        SelectedScreenTitle = SelectedScreen.Title;
        SelectedScreenRoute = SelectedScreen.Route;
    }

    private void SyncSelectedControlFields()
    {
        if (SelectedControl is null)
        {
            SelectedControlId = string.Empty;
            SelectedControlType = string.Empty;
            SelectedControlLabel = string.Empty;
            SelectedControlAction = string.Empty;
            return;
        }

        SelectedControlId = SelectedControl.Id;
        SelectedControlType = SelectedControl.Type;
        SelectedControlLabel = SelectedControl.Label;
        SelectedControlAction = SelectedControl.Action;
    }

    private void LoadControls()
    {
        Controls.Clear();
        if (SelectedScreen is null) return;
        foreach (var control in SelectedScreen.Controls)
            Controls.Add(control);
    }

    partial void OnThemeChanged(string value)
    {
        if (Document.Ui.Theme == value) return;
        Document.Ui.Theme = value;
        MarkDirty();
    }

    partial void OnNavigationPatternChanged(string value)
    {
        if (Document.Ui.NavigationPattern == value) return;
        Document.Ui.NavigationPattern = value;
        MarkDirty();
    }

    partial void OnSelectedScreenIdChanged(string value)
    {
        if (SelectedScreen is null || SelectedScreen.Id == value) return;
        SelectedScreen.Id = value;
        MarkDirty();
    }

    partial void OnSelectedScreenTitleChanged(string value)
    {
        if (SelectedScreen is null || SelectedScreen.Title == value) return;
        SelectedScreen.Title = value;
        MarkDirty();
        OnPropertyChanged(nameof(Screens));
    }

    partial void OnSelectedScreenRouteChanged(string value)
    {
        if (SelectedScreen is null || SelectedScreen.Route == value) return;
        SelectedScreen.Route = value;
        MarkDirty();
    }

    partial void OnSelectedControlIdChanged(string value)
    {
        if (SelectedControl is null || SelectedControl.Id == value) return;
        SelectedControl.Id = value;
        MarkDirty();
        OnPropertyChanged(nameof(Controls));
    }

    partial void OnSelectedControlTypeChanged(string value)
    {
        if (SelectedControl is null || SelectedControl.Type == value) return;
        SelectedControl.Type = value;
        MarkDirty();
    }

    partial void OnSelectedControlLabelChanged(string value)
    {
        if (SelectedControl is null || SelectedControl.Label == value) return;
        SelectedControl.Label = value;
        MarkDirty();
    }

    partial void OnSelectedControlActionChanged(string value)
    {
        if (SelectedControl is null || SelectedControl.Action == value) return;
        SelectedControl.Action = value;
        MarkDirty();
    }

    [RelayCommand]
    private void AddScreen()
    {
        var screen = new ScreenInfo
        {
            Id = $"screen{Document.Ui.Screens.Count + 1}",
            Title = $"Screen {Document.Ui.Screens.Count + 1}",
            Route = "/"
        };
        Document.Ui.Screens.Add(screen);
        Screens.Add(screen);
        SelectedScreen = screen;
        MarkDirty();
    }

    [RelayCommand]
    private void RemoveScreen()
    {
        if (SelectedScreen is null) return;
        Document.Ui.Screens.Remove(SelectedScreen);
        Screens.Remove(SelectedScreen);
        SelectedScreen = Screens.FirstOrDefault();
        MarkDirty();
    }

    [RelayCommand]
    private void AddControl()
    {
        if (SelectedScreen is null) return;
        var defaultType = ControlTypes.FirstOrDefault() ?? "Button";
        var control = new ControlInfo
        {
            Id = $"control{SelectedScreen.Controls.Count + 1}",
            Type = defaultType,
            Label = $"Control {SelectedScreen.Controls.Count + 1}"
        };
        SelectedScreen.Controls.Add(control);
        Controls.Add(control);
        SelectedControl = control;
        MarkDirty();
    }

    [RelayCommand]
    private void RemoveControl()
    {
        if (SelectedScreen is null || SelectedControl is null) return;
        SelectedScreen.Controls.Remove(SelectedControl);
        Controls.Remove(SelectedControl);
        SelectedControl = Controls.FirstOrDefault();
        MarkDirty();
    }

    private void MarkDirty() => OnChanged();
}

internal static class ProfileEnumHelper
{
    public static IReadOnlyList<string>? GetFieldEnumValues(
        FormSchemaDefinition profile,
        string sectionId,
        string fieldPath)
    {
        var section = profile.Sections.FirstOrDefault(s =>
            s.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase));
        var field = section?.Fields.FirstOrDefault(f =>
            f.Path.Equals(fieldPath, StringComparison.OrdinalIgnoreCase)
            || f.Id.Equals(fieldPath, StringComparison.OrdinalIgnoreCase));
        return field?.Values is { Count: > 0 } values ? values : null;
    }

    public static IReadOnlyList<string>? GetItemSchemaEnumValues(
        FormSchemaDefinition profile,
        string sectionId,
        string relativePath)
    {
        var section = profile.Sections.FirstOrDefault(s =>
            s.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase));
        var listField = section?.Fields.FirstOrDefault(f =>
            f.Type.Equals("objectList", StringComparison.OrdinalIgnoreCase));
        var sub = listField?.ItemSchema?.FirstOrDefault(f =>
            f.Path.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
        return sub?.Values is { Count: > 0 } values ? values : null;
    }

    public static IReadOnlyList<string>? GetNestedItemSchemaEnumValues(
        FormSchemaDefinition profile,
        string sectionId,
        string parentListPath,
        string nestedListPath,
        string relativeEnumPath)
    {
        var section = profile.Sections.FirstOrDefault(s =>
            s.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase));
        var parent = section?.Fields.FirstOrDefault(f =>
            f.Path.Equals(parentListPath, StringComparison.OrdinalIgnoreCase));
        var nested = parent?.ItemSchema?.FirstOrDefault(f =>
            f.Path.Equals(nestedListPath, StringComparison.OrdinalIgnoreCase));
        var enumField = nested?.ItemSchema?.FirstOrDefault(f =>
            f.Path.Equals(relativeEnumPath, StringComparison.OrdinalIgnoreCase));
        return enumField?.Values is { Count: > 0 } values ? values : null;
    }
}
