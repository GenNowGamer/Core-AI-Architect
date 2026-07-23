using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoreAiArchitect.Core.Wizard;

public enum WizardStep
{
    AppKind = 0,
    NameAndPurpose = 1,
    Platform = 2,
    LookAndFeel = 3,
    Memory = 4,
    Capabilities = 5,
    ReviewScreens = 6,
    ReviewData = 7,
    ReviewWorkflows = 8,
    Ready = 9
}

public enum AppCapability
{
    PickOptions,
    GenerateResults,
    SaveHistory,
    SettingsScreen,
    ReviewPastResults
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

    public HashSet<AppCapability> Capabilities { get; set; } = new();

    public string DerivedProjectId =>
        string.IsNullOrWhiteSpace(ProjectName)
            ? "my-app"
            : slugify(ProjectName);

    public static string ProfileLabel(string profileId) => profileId switch
    {
        "wpf_desktop" => "Desktop window app",
        "winpe_tool" => "Recovery-style tool",
        "web_api" => "Website API",
        "cli_tool" => "Command-line tool",
        "powershell_module" => "PowerShell module",
        "deployment_automation" => "Automation pipeline",
        "custom" => "Something else",
        _ => profileId
    };

    public static string CapabilityLabel(AppCapability capability) => capability switch
    {
        AppCapability.PickOptions => "Users pick from options",
        AppCapability.GenerateResults => "Create or generate results",
        AppCapability.SaveHistory => "Save history for later",
        AppCapability.SettingsScreen => "Include a settings screen",
        AppCapability.ReviewPastResults => "Review past results",
        _ => capability.ToString()
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
