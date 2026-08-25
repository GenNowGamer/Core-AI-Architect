namespace CoreAiArchitect.Core.Models.Ssot;

public sealed class SsotDocument
{
    public string SsotVersion { get; set; } = "1.0.0";
    public ProjectInfo Project { get; set; } = new();
    public EnvironmentInfo Environment { get; set; } = new();
    public ArchitectureInfo Architecture { get; set; } = new();
    public UiInfo Ui { get; set; } = new();
    public DomainInfo Domain { get; set; } = new();
    public List<WorkflowInfo> Workflows { get; set; } = new();
    public ConstraintsInfo Constraints { get; set; } = new();
    public DataInfo Data { get; set; } = new();
    public RequirementsInfo Requirements { get; set; } = new();
    public BuildInfo Build { get; set; } = new();
    public DeploymentInfo Deployment { get; set; } = new();
    public TestingInfo Testing { get; set; } = new();
    public DocumentationInfo Documentation { get; set; } = new();
}

/// <summary>
/// How a technical choice was made. <c>agent_discretion</c> means the coding agent should choose
/// intentionally — distinct from leaving a field blank by accident.
/// </summary>
public sealed class DecisionInfo
{
    /// <summary>specified | recommended | agent_discretion</summary>
    public string Mode { get; set; } = "specified";
    public List<string> Guidance { get; set; } = new();
    public string RuleId { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
}

/// <summary>Human-language product requirements collected by gap questions (not UI storyboard).</summary>
public sealed class RequirementsInfo
{
    /// <summary>none | separate | unsure</summary>
    public string Accounts { get; set; } = "none";
    /// <summary>no | yes | later</summary>
    public string MultiDevice { get; set; } = "no";
    public bool Notifications { get; set; }
    public string NotificationNotes { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = new();
}

public sealed class ProjectInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

public sealed class EnvironmentInfo
{
    public string TargetOs { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;
    public string UiFramework { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public sealed class ArchitectureInfo
{
    public string Pattern { get; set; } = string.Empty;
    public DecisionInfo PatternDecision { get; set; } = new();
    public List<string> Layers { get; set; } = new();
    public List<ModuleInfo> Modules { get; set; } = new();
}

public sealed class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
}

public sealed class UiInfo
{
    public string Theme { get; set; } = "Default";
    public string NavigationPattern { get; set; } = "Window+Dialogs";
    public List<ScreenInfo> Screens { get; set; } = new();
}

public sealed class ScreenInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public List<ControlInfo> Controls { get; set; } = new();
}

public sealed class ControlInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "Button";
    public string Label { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public sealed class DomainInfo
{
    public List<EntityInfo> Entities { get; set; } = new();
}

public sealed class EntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EntityFieldInfo> Fields { get; set; } = new();
}

public sealed class EntityFieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public bool Required { get; set; }
}

public sealed class WorkflowInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public ErrorHandlingInfo ErrorHandling { get; set; } = new();
}

public sealed class ErrorHandlingInfo
{
    public string Strategy { get; set; } = "ShowMessageBox";
    public string LogLevel { get; set; } = "Warning";
}

public sealed class ConstraintsInfo
{
    public List<string> AllowedLibraries { get; set; } = new();
    public List<string> ForbiddenLibraries { get; set; } = new();
    public bool LoggingRequired { get; set; } = true;
    public bool TelemetryAllowed { get; set; }
    public bool ThirdPartyBootloadersAllowed { get; set; }
}

public sealed class DataInfo
{
    public StorageInfo Storage { get; set; } = new();
}

public sealed class StorageInfo
{
    public string Type { get; set; } = "None";
    public string ConnectionStringMode { get; set; } = "None";
    public bool MigrationsRequired { get; set; }
    public DecisionInfo Decision { get; set; } = new();
}

public sealed class BuildInfo
{
    public string OutputType { get; set; } = "WinExe";
    public List<string> Configuration { get; set; } = new() { "Debug", "Release" };
    public List<BuildTargetInfo> Targets { get; set; } = new();
}

public sealed class BuildTargetInfo
{
    public string Name { get; set; } = string.Empty;
    public string RuntimeIdentifier { get; set; } = string.Empty;
}

public sealed class DeploymentInfo
{
    public string Packaging { get; set; } = "None";
    public bool AutoUpdate { get; set; }
}

public sealed class TestingInfo
{
    public string UnitTestFramework { get; set; } = "xUnit";
    public string UiTestFramework { get; set; } = "None";
    public double CoverageTarget { get; set; }
}

public sealed class DocumentationInfo
{
    public bool GenerateReadme { get; set; } = true;
    public bool GenerateArchitectureDoc { get; set; } = true;
    public List<string> Sections { get; set; } = new() { "Overview", "Architecture", "Workflows", "Entities" };
}
