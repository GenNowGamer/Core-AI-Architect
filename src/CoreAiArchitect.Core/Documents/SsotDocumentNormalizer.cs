using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Documents;

public static class SsotDocumentNormalizer
{
    /// <summary>
    /// Ensures collections and nested objects are non-null after load/deserialize
    /// so UI bindings and PathBinder never hit null reference crashes.
    /// </summary>
    public static SsotDocument Normalize(SsotDocument document)
    {
        document.Project ??= new ProjectInfo();
        document.Project.Tags ??= new List<string>();

        document.Environment ??= new EnvironmentInfo();
        document.Architecture ??= new ArchitectureInfo();
        document.Architecture.PatternDecision ??= new DecisionInfo();
        document.Architecture.PatternDecision.Guidance ??= new List<string>();
        document.Architecture.Layers ??= new List<string>();
        document.Architecture.Modules ??= new List<ModuleInfo>();
        foreach (var module in document.Architecture.Modules)
        {
            module.Dependencies ??= new List<string>();
        }

        document.Ui ??= new UiInfo();
        document.Ui.Screens ??= new List<ScreenInfo>();
        foreach (var screen in document.Ui.Screens)
        {
            screen.Controls ??= new List<ControlInfo>();
            if (string.IsNullOrWhiteSpace(screen.ScreenType))
                screen.ScreenType = screen.Id.Equals("main", StringComparison.OrdinalIgnoreCase) ? "Window" : "Dialog";
        }

        document.Domain ??= new DomainInfo();
        document.Domain.Entities ??= new List<EntityInfo>();
        foreach (var entity in document.Domain.Entities)
        {
            entity.Fields ??= new List<EntityFieldInfo>();
        }

        document.Workflows ??= new List<WorkflowInfo>();
        foreach (var workflow in document.Workflows)
        {
            workflow.Steps ??= new List<string>();
            workflow.ErrorHandling ??= new ErrorHandlingInfo();
        }

        document.Constraints ??= new ConstraintsInfo();
        document.Constraints.AllowedLibraries ??= new List<string>();
        document.Constraints.ForbiddenLibraries ??= new List<string>();

        document.Data ??= new DataInfo();
        document.Data.Storage ??= new StorageInfo();
        document.Data.Storage.Decision ??= new DecisionInfo();
        document.Data.Storage.Decision.Guidance ??= new List<string>();

        document.Requirements ??= new RequirementsInfo();
        document.Requirements.Notes ??= new List<string>();

        document.Build ??= new BuildInfo();
        document.Build.Configuration ??= new List<string>();
        document.Build.Targets ??= new List<BuildTargetInfo>();

        document.Deployment ??= new DeploymentInfo();
        document.Testing ??= new TestingInfo();
        document.Documentation ??= new DocumentationInfo();
        document.Documentation.Sections ??= new List<string>();

        if (string.IsNullOrWhiteSpace(document.ProfileId))
        {
            if (document.Environment.TargetOs.Equals("Android", StringComparison.OrdinalIgnoreCase)
                || document.Environment.UiFramework.Equals("Jetpack Compose", StringComparison.OrdinalIgnoreCase)
                || document.Project.Tags.Any(t => t.Equals("android-app", StringComparison.OrdinalIgnoreCase)))
            {
                document.ProfileId = "android_app";
            }
            else if (document.Environment.UiFramework.Equals("ASP.NET", StringComparison.OrdinalIgnoreCase)
                     || document.Project.Tags.Any(t => t.Equals("web-api", StringComparison.OrdinalIgnoreCase)))
            {
                document.ProfileId = "web_api";
            }
            else if (document.Environment.Language.Equals("PowerShell", StringComparison.OrdinalIgnoreCase)
                     || document.Project.Tags.Any(t => t.Equals("powershell-module", StringComparison.OrdinalIgnoreCase)))
            {
                document.ProfileId = "powershell_module";
            }
            else
            {
                document.ProfileId = "wpf_desktop";
            }
        }

        if (string.IsNullOrWhiteSpace(document.SsotVersion))
            document.SsotVersion = SsotDocument.CurrentSchemaVersion;

        return document;
    }
}
