using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Documents;

public static class SsotDocumentFactory
{
    public static SsotDocument CreateNew(FormSchemaDefinition profile)
    {
        var doc = new SsotDocument
        {
            SsotVersion = profile.TargetSsotVersion,
            Environment =
            {
                TargetOs = profile.EnvironmentDefaults.TargetOs,
                Runtime = profile.EnvironmentDefaults.Runtime,
                UiFramework = profile.EnvironmentDefaults.UiFramework,
                Language = profile.EnvironmentDefaults.Language
            },
            Constraints =
            {
                LoggingRequired = true,
                TelemetryAllowed = false,
                ThirdPartyBootloadersAllowed = false,
                AllowedLibraries = new List<string>(),
                ForbiddenLibraries = new List<string>()
            },
            Ui =
            {
                Theme = ResolveDefaultTheme(profile),
                NavigationPattern = ResolveDefaultNavigation(profile)
            },
            Build =
            {
                OutputType = ResolveDefaultOutputType(profile.Id),
                Configuration = new List<string> { "Debug", "Release" },
                Targets = new List<BuildTargetInfo>()
            },
            Deployment =
            {
                Packaging = "None",
                AutoUpdate = false
            },
            Testing =
            {
                UnitTestFramework = "xUnit",
                UiTestFramework = "None",
                CoverageTarget = 0
            },
            Documentation =
            {
                GenerateReadme = true,
                GenerateArchitectureDoc = true,
                Sections = new List<string> { "Overview", "Architecture", "Workflows", "Entities" }
            },
            Data =
            {
                Storage =
                {
                    Type = "None",
                    ConnectionStringMode = "None",
                    MigrationsRequired = false
                }
            }
        };

        return doc;
    }

    private static string ResolveDefaultTheme(FormSchemaDefinition profile)
    {
        var themeField = profile.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("ui.theme", StringComparison.OrdinalIgnoreCase));
        return themeField?.Values?.FirstOrDefault() ?? "Default";
    }

    private static string ResolveDefaultNavigation(FormSchemaDefinition profile)
    {
        var navField = profile.Sections
            .SelectMany(s => s.Fields)
            .FirstOrDefault(f => f.Path.Equals("ui.navigationPattern", StringComparison.OrdinalIgnoreCase));
        if (navField?.Values is { Count: > 0 })
            return navField.Values[0];

        return profile.Id switch
        {
            "web_api" => "HttpRoutes",
            "cli_tool" => "CommandLine",
            "powershell_module" => "CmdletPipeline",
            "winpe_tool" => "Wizard",
            "deployment_automation" => "Pipeline",
            _ => "Window+Dialogs"
        };
    }

    private static string ResolveDefaultOutputType(string profileId) =>
        profileId switch
        {
            "web_api" => "Exe",
            "cli_tool" => "Exe",
            "powershell_module" => "Library",
            "deployment_automation" => "Exe",
            "winpe_tool" => "Exe",
            _ => "WinExe"
        };
}
