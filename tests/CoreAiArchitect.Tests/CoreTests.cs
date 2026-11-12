using System.IO;
using Path = System.IO.Path;
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
        Assert.Equal(8, catalog.Profiles.Count);
        Assert.Contains(catalog.Profiles, p => p.Id == "wpf_desktop");
        Assert.Contains(catalog.Profiles, p => p.Id == "android_app");
        Assert.Contains(catalog.Profiles, p => p.Id == "custom");
    }

    [Theory]
    [InlineData("wpf_desktop")]
    [InlineData("android_app")]
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

    [Fact]
    public void NewDocument_AndroidProfile_UsesMobileDefaults()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("android_app");
        var doc = SsotDocumentFactory.CreateNew(profile);

        Assert.Equal("Android", doc.Environment.TargetOs);
        Assert.Equal("Android Runtime", doc.Environment.Runtime);
        Assert.Equal("Jetpack Compose", doc.Environment.UiFramework);
        Assert.Equal("Kotlin", doc.Environment.Language);
        Assert.Equal("Material3", doc.Ui.Theme);
        Assert.Equal("BottomNavigation", doc.Ui.NavigationPattern);
        Assert.Equal("Aab", doc.Build.OutputType);
        Assert.Equal("AAB", doc.Deployment.Packaging);
        Assert.Equal("JUnit", doc.Testing.UnitTestFramework);
        Assert.Equal("ComposeUiTest", doc.Testing.UiTestFramework);
        Assert.Contains(doc.Build.Targets, t => t.RuntimeIdentifier == "android-arm64");
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
        Assert.Equal(SsotDocument.CurrentSchemaVersion, doc.SsotVersion);
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
    [InlineData("android_app")]
    [InlineData("web_api")]
    [InlineData("cli_tool")]
    [InlineData("powershell_module")]
    public void BlueprintCompiler_MinimalAnswers_PassesReadiness(string profileId)
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById(profileId);

        var answers = new WizardAnswers
        {
            ProfileId = profileId,
            ProjectName = "Demo App",
            Description = "A demo application for testing the wizard builder.",
            SavePastResults = true,
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
    public void BlueprintCompiler_AndroidProfile_EmitsMobileControlsAndPackaging()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("android_app");
        var answers = new WizardAnswers
        {
            ProfileId = "android_app",
            ProjectName = "Demo Android App",
            Description = "A simple Android app for testing mobile blueprints.",
            LookStyle = "Modern",
            SavePastResults = true,
            Memory = MemoryChoice.SimpleFiles
        };

        var compat = new CompatibilityEngine();
        compat.ApplySafeDefaults(answers, profile);
        answers.ApplyLookStyleToTheme(compat.AllowedThemes(profile));
        Assert.Equal("Material3", answers.Theme);
        Assert.True(compat.Evaluate(answers, profile).IsCompatible);

        var doc = new BlueprintCompiler().BuildNew(answers, profile, seedMainScreen: true);
        var types = BlueprintCompiler.ResolveControlTypes(profile);
        doc.Ui.Screens.First(s => s.Id == "main").Controls.Add(
            BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1));
        doc = new BlueprintCompiler().Compile(doc, answers, profile);

        Assert.Equal("Android", doc.Environment.TargetOs);
        Assert.Equal("Jetpack Compose", doc.Environment.UiFramework);
        Assert.Equal("Kotlin", doc.Environment.Language);
        Assert.Equal("Aab", doc.Build.OutputType);
        Assert.Equal("AAB", doc.Deployment.Packaging);
        Assert.Equal("JUnit", doc.Testing.UnitTestFramework);
        Assert.Equal("ComposeUiTest", doc.Testing.UiTestFramework);
        Assert.Equal("DataStore", doc.Data.Storage.Type);
        Assert.Contains(doc.Constraints.AllowedLibraries, l => l.Contains("compose", StringComparison.OrdinalIgnoreCase));

        var main = Assert.Single(doc.Ui.Screens.Where(s => s.Id == "main"));
        Assert.Contains(main.Controls, c => c.Type == "Button");

        var ready = new SsotReadinessEngine().Assess(doc, profile);
        Assert.True(ready.IsValid, string.Join("; ", ready.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void BlueprintTemplates_Lottery_HasGenerateAndResults()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var answers = new WizardAnswers
        {
            ProfileId = "wpf_desktop",
            ProjectName = "Core Lottery Generator",
            Description = "Pick a game, enter ticket count, generate numbers."
        };
        var compat = new CompatibilityEngine();
        compat.ApplySafeDefaults(answers, profile);

        var compiler = new BlueprintCompiler();
        var doc = compiler.BuildNew(answers, profile, seedMainScreen: false);
        BlueprintTemplates.Apply("lottery", doc, profile);
        doc = compiler.Compile(doc, answers, profile);

        Assert.Contains(doc.Ui.Screens, s => s.Title.Contains("Lotto", StringComparison.OrdinalIgnoreCase));
        var main = doc.Ui.Screens[0];
        Assert.Contains(main.Controls, c => c.Id == "gamePicker");
        Assert.Contains(main.Controls, c => c.Id == "ticketCount");
        Assert.Contains(main.Controls, c => c.Id == "generateButton");
        Assert.Contains(main.Controls, c => c.Id == "resultsList");
        Assert.DoesNotContain(doc.Ui.Screens, s => s.Id == "history");
        Assert.All(doc.Domain.Entities, e =>
            Assert.Contains(DomainScaffoldPolicy.ScaffoldMarker, e.Description, StringComparison.OrdinalIgnoreCase));

        var ready = new SsotReadinessEngine().Assess(doc, profile);
        Assert.True(ready.IsValid, string.Join("; ", ready.Errors.Select(e => e.Message)));
    }

    [Theory]
    [InlineData("master_detail")]
    [InlineData("dashboard")]
    [InlineData("batch_processor")]
    public void BlueprintTemplates_NewTemplates_CompileAndPassReadiness(string templateId)
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var answers = new WizardAnswers
        {
            ProfileId = "wpf_desktop",
            ProjectName = "Template Test Project",
            Description = "Automated test validating new template compile and readiness."
        };
        var compat = new CompatibilityEngine();
        compat.ApplySafeDefaults(answers, profile);

        var compiler = new BlueprintCompiler();
        var doc = compiler.BuildNew(answers, profile, seedMainScreen: false);
        BlueprintTemplates.Apply(templateId, doc, profile);
        doc = compiler.Compile(doc, answers, profile);

        Assert.NotEmpty(doc.Ui.Screens);
        Assert.NotEmpty(doc.Workflows);
        var ready = new SsotReadinessEngine().Assess(doc, profile);
        Assert.True(ready.IsValid, string.Join("; ", ready.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void DomainScaffoldPolicy_AppStory_IncludesAgentGuidance()
    {
        var doc = new SsotDocument();
        doc.Ui.Screens.Add(new ScreenInfo { Id = "main", Title = "Main" });
        var story = BlueprintCompiler.BuildAppStory(doc, new WizardAnswers
        {
            ProjectName = "Demo",
            ProfileId = "wpf_desktop",
            Description = "Pick a game and generate tickets."
        });
        Assert.Contains(DomainScaffoldPolicy.ScaffoldMarker, story, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("project.description", story, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void WizardAnswers_ComposeDescription_IncludesAdLibAndRules()
    {
        var answers = new WizardAnswers
        {
            ProjectName = "GameVault",
            Audience = "PC gamers",
            HelpsThem = "manage and back up game save files",
            FeatureListText = "automatic backups\nmultiple restore points",
            Connectivity = ConnectivityChoice.Offline,
            LookStyle = "Modern",
            Theme = "Fluent",
            ProductRules = "Support Steam and Epic library folders."
        };

        var description = answers.ComposeDescription();
        Assert.Contains("PC gamers", description);
        Assert.Contains("automatic backups", description);
        Assert.Contains("offline", description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Support Steam and Epic", description);
        Assert.Equal(description, answers.Description);
    }

    [Fact]
    public void BlueprintCompiler_SeedAdLibFeatures_AddsButtonsOnBlankMain()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var answers = new WizardAnswers
        {
            ProfileId = "wpf_desktop",
            ProjectName = "GameVault",
            FeatureListText = "Detect games\nSchedule backups"
        };
        answers.ComposeDescription();
        new CompatibilityEngine().ApplySafeDefaults(answers, profile);

        var compiler = new BlueprintCompiler();
        var doc = compiler.BuildNew(answers, profile, seedMainScreen: false);
        BlueprintTemplates.Apply("blank", doc, profile);
        var seeded = BlueprintCompiler.SeedAdLibFeatures(doc, answers, profile);
        doc = compiler.Compile(doc, answers, profile);

        Assert.Equal(2, seeded);
        var main = Assert.Single(doc.Ui.Screens.Where(s => s.Id == "main"));
        Assert.Contains(main.Controls, c => c.Label.Contains("Detect", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(main.Controls, c => c.Label.Contains("Schedule", StringComparison.OrdinalIgnoreCase));
        Assert.True(new SsotReadinessEngine().Assess(doc, profile).IsValid);
    }

    [Fact]
    public void BlueprintHubAssessor_ReportsAttentionWhenScreensMissing()
    {
        var answers = new WizardAnswers
        {
            ProjectName = "Demo",
            Description = "A short description for testing.",
            ProfileId = "wpf_desktop",
            TargetOs = "Windows 11",
            UiFramework = "WPF",
            Language = "C#"
        };
        var assessment = BlueprintHubAssessor.Assess(null, answers, readiness: null);
        Assert.Contains(assessment.Sections, s => s.Id == "screens" && s.Status == BlueprintSectionStatus.Attention);
        Assert.True(assessment.DecisionsRemaining >= 1);
        Assert.Contains("remaining", assessment.ProgressCaption, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecommendationEngine_OfflinePersist_SuggestsSqlite()
    {
        var answers = new WizardAnswers
        {
            ProfileId = "wpf_desktop",
            ProjectName = "Vault",
            Connectivity = ConnectivityChoice.Offline,
            MultiDevice = MultiDeviceChoice.No,
            SavePastResults = true,
            Memory = MemoryChoice.SimpleFiles,
            LookStyle = "Modern",
            UiFramework = "WPF"
        };
        var doc = new SsotDocument
        {
            Data = { Storage = { Type = "LocalJson" } },
            Ui = { Theme = "Default", Screens = { new ScreenInfo { Id = "main", Title = "Main" } } },
            Architecture = { Pattern = "MVC" }
        };

        var recs = new RecommendationEngine().Evaluate(doc, answers);
        Assert.Contains(recs, r => r.RuleId == "STORAGE-OFFLINE-STRUCTURED-001");
        Assert.Contains(recs, r => r.RuleId == "ARCH-DESKTOP-MVVM-001");
        Assert.Contains(recs, r => r.RuleId.StartsWith("UI-MODERN", StringComparison.Ordinal));
    }

    [Fact]
    public void BlueprintCompiler_WritesRequirementsAndDecisionModes()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var answers = new WizardAnswers
        {
            ProfileId = "wpf_desktop",
            ProjectName = "Needs App",
            Audience = "testers",
            HelpsThem = "try gap questions",
            Accounts = AccountsChoice.Separate,
            MultiDevice = MultiDeviceChoice.Later,
            Notifications = true,
            NotificationNotes = "Remind about due items",
            ArchitectureDecision = TechnicalDecisionMode.AgentDiscretion,
            StorageDecision = TechnicalDecisionMode.Recommended,
            SavePastResults = true,
            Memory = MemoryChoice.SimpleFiles
        };
        answers.ComposeDescription();
        new CompatibilityEngine().ApplySafeDefaults(answers, profile);

        var doc = new BlueprintCompiler().BuildNew(answers, profile);
        Assert.Equal("separate", doc.Requirements.Accounts);
        Assert.Equal("later", doc.Requirements.MultiDevice);
        Assert.True(doc.Requirements.Notifications);
        Assert.Equal("agent_discretion", doc.Architecture.PatternDecision.Mode);
        Assert.Equal("recommended", doc.Data.Storage.Decision.Mode);
        Assert.Contains(doc.Ui.Screens.SelectMany(s => s.Controls), c => c.Id == "remindersHint");
    }

    [Fact]
    public void AgentPackageExporter_WritesExpectedLayout()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");
        var answers = new WizardAnswers
        {
            ProfileId = "wpf_desktop",
            ProjectName = "Package Demo",
            Description = "Demo for agent package export.",
            SavePastResults = true,
            Memory = MemoryChoice.SimpleFiles
        };
        new CompatibilityEngine().ApplySafeDefaults(answers, profile);
        var doc = new BlueprintCompiler().BuildNew(answers, profile);

        var root = Path.Combine(Path.GetTempPath(), "caa-agent-pkg-" + Guid.NewGuid().ToString("N"));
        try
        {
            var written = new AgentPackageExporter().Export(doc, root);
            Assert.Contains(written, p => p.EndsWith("AGENTS.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(written, p => p.EndsWith("project.json", StringComparison.OrdinalIgnoreCase));
            Assert.True(File.Exists(Path.Combine(root, "specifications", "requirements.md")));
            Assert.True(Directory.Exists(Path.Combine(root, "tasks")));
            Assert.Contains("agent_discretion", File.ReadAllText(Path.Combine(root, "AGENTS.md")), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
