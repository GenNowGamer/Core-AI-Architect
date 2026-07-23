using CoreAiArchitect.Core.Documents;
using CoreAiArchitect.Core.Export;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.SchemaEngine;
using CoreAiArchitect.Core.Validation;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.Tests;

public class SchemaCatalogTests
{
    private static string SchemasDir
    {
        get
        {
            var beside = Path.Combine(AppContext.BaseDirectory, "schemas");
            if (Directory.Exists(beside) && Directory.EnumerateFiles(beside, "*.json").Any())
                return beside;

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schemas"));
        }
    }

    [Fact]
    public void LoadAllProfiles_Succeeds()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        Assert.Equal(7, catalog.Profiles.Count);
        Assert.Contains(catalog.Profiles, p => p.Id == "wpf_desktop");
        Assert.Contains(catalog.Profiles, p => p.Id == "custom");
    }

    [Theory]
    [InlineData("wpf_desktop")]
    [InlineData("winpe_tool")]
    [InlineData("web_api")]
    [InlineData("cli_tool")]
    [InlineData("powershell_module")]
    [InlineData("deployment_automation")]
    [InlineData("custom")]
    public void EachProfile_HasRequiredSections(string profileId)
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById(profileId);
        Assert.Contains(profile.Sections, s => s.Id == "project");
        Assert.Contains(profile.Sections, s => s.Id == "workflows");
        Assert.Contains(profile.Sections, s => s.Id == "ui");
        Assert.False(string.IsNullOrWhiteSpace(profile.EnvironmentDefaults.UiFramework));
    }
}

public class DocumentFactoryTests
{
    private static string SchemasDir
    {
        get
        {
            var beside = Path.Combine(AppContext.BaseDirectory, "schemas");
            if (Directory.Exists(beside) && Directory.EnumerateFiles(beside, "*.json").Any())
                return beside;

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schemas"));
        }
    }

    [Fact]
    public void NewDocument_UsesProfileDefaults_WithoutSampleContent()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var doc = SsotDocumentFactory.CreateNew(profile);

        Assert.Equal("1.0.0", doc.SsotVersion);
        Assert.Equal("WPF", doc.Environment.UiFramework);
        Assert.Equal(".NET 8", doc.Environment.Runtime);
        Assert.True(doc.Constraints.LoggingRequired);
        Assert.False(doc.Constraints.TelemetryAllowed);
        Assert.Empty(doc.Project.Name);
        Assert.Empty(doc.Domain.Entities);
        Assert.Empty(doc.Ui.Screens);
        Assert.Empty(doc.Workflows);
        Assert.Empty(doc.Architecture.Modules);
    }
}

public class PathBinderTests
{
    [Fact]
    public void GetSet_NestedPaths()
    {
        var doc = new SsotDocument();
        PathBinder.SetValue(doc, "project.name", "Demo");
        Assert.Equal("Demo", PathBinder.GetValue(doc, "project.name"));

        doc.Architecture.Modules.Add(new ModuleInfo { Name = "Core" });
        Assert.Equal("Core", PathBinder.GetValue(doc, "architecture.modules.0.name"));
    }

    [Fact]
    public void EvaluateShowIf_Works()
    {
        var doc = new SsotDocument { Environment = { UiFramework = "WPF" } };
        Assert.True(PathBinder.EvaluateShowIf(doc, "environment.uiFramework", "WPF"));
        Assert.False(PathBinder.EvaluateShowIf(doc, "environment.uiFramework", "Console"));
    }
}

public class ValidationAndExportTests
{
    private static string SchemasDir
    {
        get
        {
            var beside = Path.Combine(AppContext.BaseDirectory, "schemas");
            if (Directory.Exists(beside) && Directory.EnumerateFiles(beside, "*.json").Any())
                return beside;

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schemas"));
        }
    }

    [Fact]
    public void Validate_EmptyRequiredFields_Fails()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var doc = SsotDocumentFactory.CreateNew(profile);
        var result = new ValidationEngine().Validate(doc, profile);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "project.name");
    }

    [Fact]
    public void Validate_FilledRequiredFields_Passes()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var doc = SsotDocumentFactory.CreateNew(profile);
        doc.Project.Id = "demo";
        doc.Project.Name = "Demo";
        doc.Project.Description = "A project";
        doc.Architecture.Pattern = "MVVM";
        var result = new ValidationEngine().Validate(doc, profile);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void JsonRoundTrip_PreservesData()
    {
        var doc = new SsotDocument
        {
            Project = { Id = "x", Name = "X", Description = "Desc" },
            Architecture = { Pattern = "MVVM" }
        };
        doc.Architecture.Modules.Add(new ModuleInfo { Name = "M1", Description = "Module" });
        var exporter = new JsonExporter();
        var json = exporter.ToJson(doc);
        var loaded = exporter.LoadFromJson(json);
        Assert.Equal("X", loaded.Project.Name);
        Assert.Single(loaded.Architecture.Modules);
        Assert.Equal("M1", loaded.Architecture.Modules[0].Name);
    }

    [Fact]
    public void Markdown_OmitsEmptySections_IncludesFilled()
    {
        var doc = new SsotDocument
        {
            Project = { Name = "Filled", Description = "Has content" },
            Architecture = { Pattern = "MVVM" },
            Documentation =
            {
                GenerateReadme = true,
                GenerateArchitectureDoc = true,
                Sections = new List<string> { "Overview", "Architecture", "Workflows", "Entities" }
            }
        };

        var md = new MarkdownExporter().ToMarkdown(doc);
        Assert.Contains("# Filled", md);
        Assert.Contains("## Architecture", md);
        Assert.DoesNotContain("## Workflows", md);
        Assert.DoesNotContain("## Entities", md);
    }

    [Fact]
    public void WorkflowTrigger_UnknownControl_Warns()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var doc = SsotDocumentFactory.CreateNew(profile);
        doc.Project.Id = "demo";
        doc.Project.Name = "Demo";
        doc.Project.Description = "A project";
        doc.Architecture.Pattern = "MVVM";
        doc.Ui.Screens.Add(new ScreenInfo
        {
            Id = "main",
            Title = "Main",
            Controls = { new ControlInfo { Id = "saveButton", Type = "Button" } }
        });
        doc.Workflows.Add(new WorkflowInfo { Id = "w1", Name = "W1", Trigger = "missingButton" });

        var result = new ValidationEngine().Validate(doc, profile);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Message.Contains("missingButton"));
    }

    [Fact]
    public void Normalize_FillsNullCollectionsAndErrorHandling()
    {
        var doc = new SsotDocument();
        doc.Architecture.Modules = null!;
        doc.Ui.Screens = null!;
        doc.Domain.Entities = null!;
        doc.SsotVersion = "";
        doc.Workflows =
        [
            new WorkflowInfo { Id = "w1", Name = "W1", ErrorHandling = null!, Steps = null! }
        ];

        SsotDocumentNormalizer.Normalize(doc);

        Assert.NotNull(doc.Architecture.Modules);
        Assert.NotNull(doc.Ui.Screens);
        Assert.NotNull(doc.Domain.Entities);
        Assert.NotNull(doc.Workflows[0].Steps);
        Assert.NotNull(doc.Workflows[0].ErrorHandling);
        Assert.Equal("1.0.0", doc.SsotVersion);
    }

    [Fact]
    public void Validate_NestedWorkflowEnumPath_UsesFullRelativePath()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var doc = SsotDocumentFactory.CreateNew(profile);
        doc.Project.Id = "demo";
        doc.Project.Name = "Demo";
        doc.Project.Description = "A project";
        doc.Architecture.Pattern = "MVVM";
        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "w1",
            Name = "W1",
            ErrorHandling = new ErrorHandlingInfo { Strategy = "NotARealStrategy", LogLevel = "Warning" }
        });

        var result = new ValidationEngine().Validate(doc, profile);
        Assert.Contains(result.Errors, e => e.Path.Contains("errorHandling.strategy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Finalize_EmptyGuiDocument_FailsReadiness()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var doc = SsotDocumentFactory.CreateNew(profile);
        doc.Project.Id = "demo";
        doc.Project.Name = "Demo";
        doc.Project.Description = "A project";
        doc.Architecture.Pattern = "MVVM";

        var result = new SsotReadinessEngine().Assess(doc, profile);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Path == "ui.screens");
        Assert.Contains(result.Errors, e => e.Path == "domain.entities");
        Assert.Contains(result.Errors, e => e.Path == "workflows");
        Assert.Contains(result.Errors, e => e.Path == "build.targets");
    }

    [Fact]
    public void Finalize_FilledGuiDocument_Passes()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var doc = SsotDocumentFactory.CreateNew(profile);
        doc.Project.Id = "demo";
        doc.Project.Name = "Demo";
        doc.Project.Description = "A project";
        doc.Architecture.Pattern = "MVVM";
        doc.Architecture.Layers.Add("UI");
        doc.Architecture.Modules.Add(new ModuleInfo { Name = "App", Description = "Shell" });
        doc.Ui.Screens.Add(new ScreenInfo
        {
            Id = "main",
            Title = "Main",
            Controls = { new ControlInfo { Id = "runButton", Type = "Button", Label = "Run", Action = "Run" } }
        });
        doc.Domain.Entities.Add(new EntityInfo
        {
            Name = "Item",
            Fields = { new EntityFieldInfo { Name = "Id", Type = "Guid", Required = true } }
        });
        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "run",
            Name = "Run",
            Trigger = "runButton",
            Steps = { "Do work" }
        });
        doc.Build.Targets.Add(new BuildTargetInfo { Name = "win-x64", RuntimeIdentifier = "win-x64" });

        var result = new SsotReadinessEngine().Assess(doc, profile);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}

public class WizardBuilderTests
{
    private static string SchemasDir
    {
        get
        {
            var beside = Path.Combine(AppContext.BaseDirectory, "schemas");
            if (Directory.Exists(beside) && Directory.EnumerateFiles(beside, "*.json").Any())
                return beside;

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schemas"));
        }
    }

    [Fact]
    public void Compatibility_BlocksWinUiWindowDialogs()
    {
        var engine = new CompatibilityEngine();
        var answers = new WizardAnswers
        {
            ProfileId = "custom",
            UiFramework = "WinUI",
            NavigationPattern = "Window+Dialogs",
            Language = "C#"
        };

        var result = engine.Evaluate(answers);
        Assert.False(result.IsCompatible);
        Assert.Contains(result.Issues, i => i.Message.Contains("WinUI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compatibility_BlocksDesktopPlaywrightOnDocument()
    {
        var doc = new SsotDocument
        {
            Environment = { UiFramework = "WPF" },
            Testing = { UiTestFramework = "Playwright" }
        };
        doc.Ui.Screens.Add(new ScreenInfo
        {
            Id = "main",
            Controls = { new ControlInfo { Id = "ok", Type = "Button" } }
        });
        doc.Workflows.Add(new WorkflowInfo { Name = "Go", Trigger = "missing" });

        var result = new CompatibilityEngine().EvaluateDocument(doc);
        Assert.False(result.IsCompatible);
        Assert.Contains(result.Issues, i => i.Message.Contains("Playwright", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Issues, i => i.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("wpf_desktop")]
    [InlineData("web_api")]
    [InlineData("cli_tool")]
    [InlineData("powershell_module")]
    public void SmartBuilder_MinimalAnswers_PassesReadiness(string profileId)
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById(profileId);

        var answers = new WizardAnswers
        {
            ProfileId = profileId,
            ProjectName = "Demo App",
            Description = "A demo application for testing the wizard builder.",
            Capabilities =
            {
                AppCapability.GenerateResults,
                AppCapability.SaveHistory
            },
            Memory = MemoryChoice.SimpleFiles
        };

        var compat = new CompatibilityEngine();
        compat.ApplySafeDefaults(answers, profile);
        Assert.True(compat.Evaluate(answers, profile).IsCompatible);

        var doc = new SsotSmartBuilder().Build(answers, profile);
        var ready = new SsotReadinessEngine().Assess(doc, profile);
        Assert.True(ready.IsValid, string.Join("; ", ready.Errors.Select(e => e.Message)));
        Assert.NotEmpty(doc.Ui.Screens);
        Assert.NotEmpty(doc.Domain.Entities);
        Assert.NotEmpty(doc.Workflows);
        Assert.NotEmpty(doc.Build.Targets);
    }

    [Fact]
    public void FriendlyIssueMapper_HumanizesWorkflowSteps()
    {
        var issue = new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Path = "workflows[0].steps",
            Message = "Workflow 'Lottery Game Selection' needs at least one step."
        };

        var friendly = FriendlyIssueMapper.ToFriendlyMessage(issue);
        Assert.Contains("Lottery Game Selection", friendly);
        Assert.DoesNotContain("workflows[0]", friendly);
    }
}
