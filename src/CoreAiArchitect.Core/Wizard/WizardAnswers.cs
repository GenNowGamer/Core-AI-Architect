using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CoreAiArchitect.Core.Wizard;

public enum WizardStep
{
    YourIdea = 0,
    WhatYouNeed = 1,
    Screens = 2,
    WhenTheyClick = 3,
    RememberStuff = 4,
    HandOff = 5
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

/// <summary>How much technical detail the Blueprint hub exposes.</summary>
public enum DetailLevel
{
    Simple = 0,
    Standard = 1,
    Expert = 2
}

/// <summary>Connectivity preference collected in the ad-lib (written into description).</summary>
public enum ConnectivityChoice
{
    Offline = 0,
    SometimesOnline = 1,
    Online = 2
}

public enum AccountsChoice
{
    None = 0,
    Separate = 1,
    Unsure = 2
}

public enum MultiDeviceChoice
{
    No = 0,
    Yes = 1,
    Later = 2
}

/// <summary>How the author wants a technical field resolved.</summary>
public enum TechnicalDecisionMode
{
    Recommended = 0,
    AgentDiscretion = 1,
    ChooseMyself = 2
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
    private string _audience = string.Empty;
    private string _helpsThem = string.Empty;
    private string _featureListText = string.Empty;
    private string _productRules = string.Empty;
    private ConnectivityChoice _connectivity = ConnectivityChoice.Offline;
    private string _lookStyle = "Modern";
    private DetailLevel _detailLevel = DetailLevel.Simple;
    private AccountsChoice _accounts = AccountsChoice.None;
    private MultiDeviceChoice _multiDevice = MultiDeviceChoice.No;
    private bool _notifications;
    private string _notificationNotes = string.Empty;
    private TechnicalDecisionMode _architectureDecision = TechnicalDecisionMode.Recommended;
    private TechnicalDecisionMode _storageDecision = TechnicalDecisionMode.Recommended;

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

    /// <summary>Who the app is mainly for (ad-lib).</summary>
    public string Audience { get => _audience; set => Set(ref _audience, value); }

    /// <summary>What the app helps them do (ad-lib).</summary>
    public string HelpsThem { get => _helpsThem; set => Set(ref _helpsThem, value); }

    /// <summary>One capability per line (ad-lib). Seeds blank-template buttons.</summary>
    public string FeatureListText { get => _featureListText; set => Set(ref _featureListText, value); }

    /// <summary>Detailed product rules (games lists, validation, etc.).</summary>
    public string ProductRules { get => _productRules; set => Set(ref _productRules, value); }

    public ConnectivityChoice Connectivity { get => _connectivity; set => Set(ref _connectivity, value); }

    /// <summary>Modern / Professional / Simple — influences theme default and description.</summary>
    public string LookStyle { get => _lookStyle; set => Set(ref _lookStyle, value); }

    public DetailLevel DetailLevel { get => _detailLevel; set => Set(ref _detailLevel, value); }

    public AccountsChoice Accounts { get => _accounts; set => Set(ref _accounts, value); }
    public MultiDeviceChoice MultiDevice { get => _multiDevice; set => Set(ref _multiDevice, value); }
    public bool Notifications { get => _notifications; set => Set(ref _notifications, value); }
    public string NotificationNotes { get => _notificationNotes; set => Set(ref _notificationNotes, value); }
    public TechnicalDecisionMode ArchitectureDecision { get => _architectureDecision; set => Set(ref _architectureDecision, value); }
    public TechnicalDecisionMode StorageDecision { get => _storageDecision; set => Set(ref _storageDecision, value); }

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

    public string ConnectivityLabel => Connectivity switch
    {
        ConnectivityChoice.Online => "Online (needs the internet)",
        ConnectivityChoice.SometimesOnline => "Sometimes online",
        _ => "Offline"
    };

    public IReadOnlyList<string> GetFeatureLines() =>
        FeatureListText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(s => s.Trim().TrimStart('-', '*', '•', ' '))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

    /// <summary>
    /// Builds <see cref="Description"/> from ad-lib blanks + product rules.
    /// Always call before compile / readiness so the SSOT carries a full narrative.
    /// </summary>
    public string ComposeDescription()
    {
        var sb = new StringBuilder();
        var name = string.IsNullOrWhiteSpace(ProjectName) ? "this app" : ProjectName.Trim();
        var hasAdLib = !string.IsNullOrWhiteSpace(Audience)
                       || !string.IsNullOrWhiteSpace(HelpsThem)
                       || GetFeatureLines().Count > 0;

        if (hasAdLib)
        {
            sb.Append("I'm building ").Append(name);
            if (!string.IsNullOrWhiteSpace(Audience))
                sb.Append(" mainly for ").Append(Audience.Trim());
            sb.Append('.');
            if (!string.IsNullOrWhiteSpace(HelpsThem))
                sb.Append(" It helps them ").Append(HelpsThem.Trim().TrimEnd('.')).Append('.');
            sb.AppendLine();
            sb.AppendLine();

            var features = GetFeatureLines();
            if (features.Count > 0)
            {
                sb.AppendLine("Main things users should be able to do:");
                foreach (var feature in features)
                    sb.Append("- ").AppendLine(feature);
                sb.AppendLine();
            }

            sb.Append("The app is mainly used ").Append(ConnectivityLabel.ToLowerInvariant()).AppendLine(".");
            sb.Append("The interface should feel ").Append(LookStyle.Trim())
                .Append(" (theme preference: ").Append(Theme).AppendLine(").");
            AppendGapSummary(sb);
        }

        if (!string.IsNullOrWhiteSpace(ProductRules))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Product rules:");
            }

            sb.Append(ProductRules.Trim());
        }

        // Always include gap-question summary so agent packages stay consistent.
        if (!hasAdLib)
            AppendGapSummary(sb);

        if (sb.Length == 0 && !string.IsNullOrWhiteSpace(Description))
            return Description.Trim();

        var composed = sb.ToString().Trim();
        Description = composed;
        return composed;
    }

    private void AppendGapSummary(StringBuilder sb)
    {
        var lines = new List<string>();
        lines.Add(Accounts switch
        {
            AccountsChoice.Separate => "Users need separate accounts.",
            AccountsChoice.Unsure => "Account requirements are not decided yet — prefer the simplest single-user approach unless product rules require otherwise.",
            _ => "No separate user accounts; anyone using the device can use the app."
        });
        lines.Add(MultiDevice switch
        {
            MultiDeviceChoice.Yes => "Information should follow the user across devices.",
            MultiDeviceChoice.Later => "Multi-device sync may be added later; design so it is not blocked.",
            _ => "Information stays on this device (no multi-device sync required)."
        });
        if (Notifications)
        {
            lines.Add(string.IsNullOrWhiteSpace(NotificationNotes)
                ? "The app should remind or notify users about important events."
                : "Notifications: " + NotificationNotes.Trim());
        }
        else
        {
            lines.Add("No reminder or notification features are required.");
        }

        if (sb.Length > 0)
            sb.AppendLine();
        sb.AppendLine("Needs:");
        foreach (var line in lines)
            sb.Append("- ").AppendLine(line);
    }

    /// <summary>Maps look style onto an allowed theme value when still on the profile default.</summary>
    public void ApplyLookStyleToTheme(IReadOnlyList<string> allowedThemes)
    {
        if (allowedThemes.Count == 0) return;
        string preferred;
        if (LookStyle.Equals("Modern", StringComparison.OrdinalIgnoreCase)
            && allowedThemes.Contains("Material3", StringComparer.OrdinalIgnoreCase))
            preferred = "Material3";
        else if (LookStyle.Equals("Modern", StringComparison.OrdinalIgnoreCase)
                 && allowedThemes.Contains("Fluent", StringComparer.OrdinalIgnoreCase))
            preferred = "Fluent";
        else
            preferred = allowedThemes[0];

        if (string.IsNullOrWhiteSpace(Theme)
            || Theme.Equals("Default", StringComparison.OrdinalIgnoreCase)
            || Theme.Equals("Material", StringComparison.OrdinalIgnoreCase)
            || !allowedThemes.Contains(Theme, StringComparer.OrdinalIgnoreCase))
        {
            Theme = preferred;
        }
    }

    public void ClearAdLib()
    {
        Audience = string.Empty;
        HelpsThem = string.Empty;
        FeatureListText = string.Empty;
        ProductRules = string.Empty;
        Connectivity = ConnectivityChoice.Offline;
        LookStyle = "Modern";
        DetailLevel = DetailLevel.Simple;
        Accounts = AccountsChoice.None;
        MultiDevice = MultiDeviceChoice.No;
        Notifications = false;
        NotificationNotes = string.Empty;
        ArchitectureDecision = TechnicalDecisionMode.Recommended;
        StorageDecision = TechnicalDecisionMode.Recommended;
        Description = string.Empty;
    }

    /// <summary>When opening an existing blueprint, treat the stored description as product rules.</summary>
    public void HydrateAdLibFromDescription(string storedDescription)
    {
        ProductRules = storedDescription?.Trim() ?? string.Empty;
        Description = ProductRules;
        Audience = string.Empty;
        HelpsThem = string.Empty;
        FeatureListText = string.Empty;
    }

    public static string ProfileLabel(string profileId) => profileId switch
    {
        "wpf_desktop" => "Windows desktop app",
        "android_app" => "Android app",
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
