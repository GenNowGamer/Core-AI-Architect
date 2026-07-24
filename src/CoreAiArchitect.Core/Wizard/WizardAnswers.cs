using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoreAiArchitect.Core.Wizard;

public enum WizardStep
{
    YourIdea = 0,
    Screens = 1,
    WhenTheyClick = 2,
    RememberStuff = 3,
    HandOff = 4
}

/// <summary>Plain-language widget kinds shown in the Screens step.</summary>
public enum BlueprintThingKind
{
    ChoiceList,
    PlaceToType,
    Button,
    ResultsList
}

public enum MemoryChoice
{
    None,
    SimpleFiles,
    Database
}

/// <summary>Plain-language answers collected by the guided wizard.</summary>
public sealed class WizardAnswers : INotifyPropertyChanged
{
    private string _profileId = "wpf_desktop";
    private string _projectName = string.Empty;
    private string _description = string.Empty;
    private string _owner = string.Empty;
    private string _targetOs = "Windows 11";
    private string _runtime = ".NET 8";
    private string _uiFramework = "WPF";
    private string _language = "C#";
    private string _theme = "Default";
    private string _navigationPattern = "Window+Dialogs";
    private MemoryChoice _memory = MemoryChoice.None;
    private bool _savePastResults;
    private bool _includeSettingsScreen;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProfileId { get => _profileId; set => Set(ref _profileId, value); }
    public string ProjectName { get => _projectName; set { if (Set(ref _projectName, value)) OnPropertyChanged(nameof(DerivedProjectId)); } }
    public string Description { get => _description; set => Set(ref _description, value); }
    public string Owner { get => _owner; set => Set(ref _owner, value); }
    public string TargetOs { get => _targetOs; set => Set(ref _targetOs, value); }
    public string Runtime { get => _runtime; set => Set(ref _runtime, value); }
    public string UiFramework { get => _uiFramework; set => Set(ref _uiFramework, value); }
    public string Language { get => _language; set => Set(ref _language, value); }
    public string Theme { get => _theme; set => Set(ref _theme, value); }
    public string NavigationPattern { get => _navigationPattern; set => Set(ref _navigationPattern, value); }
    public MemoryChoice Memory { get => _memory; set => Set(ref _memory, value); }

    public bool SavePastResults
    {
        get => _savePastResults;
        set
        {
            if (!Set(ref _savePastResults, value)) return;
            if (value && Memory == MemoryChoice.None)
                Memory = MemoryChoice.SimpleFiles;
            if (!value && !IncludeSettingsScreen)
                Memory = MemoryChoice.None;
        }
    }

    public bool IncludeSettingsScreen
    {
        get => _includeSettingsScreen;
        set => Set(ref _includeSettingsScreen, value);
    }

    public string DerivedProjectId =>
        string.IsNullOrWhiteSpace(ProjectName)
            ? "my-app"
            : slugify(ProjectName);

    public static string ProfileLabel(string profileId) => profileId switch
    {
        "wpf_desktop" => "Windows desktop app",
        "web_api" => "Website / API",
        "custom" => "Something else",
        "winpe_tool" => "Recovery-style tool",
        "cli_tool" => "Command-line tool",
        "powershell_module" => "PowerShell module",
        "deployment_automation" => "Automation pipeline",
        _ => profileId
    };

    public static string ThingLabel(BlueprintThingKind kind) => kind switch
    {
        BlueprintThingKind.ChoiceList => "Choice list",
        BlueprintThingKind.PlaceToType => "Place to type",
        BlueprintThingKind.Button => "Button",
        BlueprintThingKind.ResultsList => "Results list",
        _ => kind.ToString()
    };

    public static string ThingDescription(BlueprintThingKind kind) => kind switch
    {
        BlueprintThingKind.ChoiceList => "Let people pick one option from a list.",
        BlueprintThingKind.PlaceToType => "A box where people type a number or text.",
        BlueprintThingKind.Button => "Something people click to make the app do work.",
        BlueprintThingKind.ResultsList => "A place to show generated or saved items.",
        _ => string.Empty
    };

    private static string slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return slug.Trim('-');
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
