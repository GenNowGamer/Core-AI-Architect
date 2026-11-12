using System.IO;
using Path = System.IO.Path;
using CoreAiArchitect.Core.Documents;
using CoreAiArchitect.Core.Export;
using CoreAiArchitect.Core.Migration;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.SchemaEngine;
using CoreAiArchitect.Core.Validation;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.Tests;

public class NewFeaturesAndIntegrationTests
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
    public void Android_Material3Theme_PassesCompatibilityReadyCheck()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("android_app");

        var doc = SsotDocumentFactory.CreateNew(profile);
        doc.Project.Name = "Android Material3 App";
        doc.Project.Description = "An Android application testing theme compatibility.";
        doc.Ui.Theme = "Material3";
        doc.Ui.Screens.Add(new ScreenInfo
        {
            Id = "main",
            Title = "Main Screen",
            Controls = { new ControlInfo { Id = "btn", Type = "Button", Label = "Go" } }
        });
        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "btn",
            Name = "Go",
            Trigger = "btn",
            Steps = { "Do work" }
        });
        doc.Build.Targets.Add(new BuildTargetInfo { Name = "android-arm64", RuntimeIdentifier = "android-arm64" });

        var compat = new CompatibilityEngine();

        // 1. Evaluate without profile parameter (inferred from document)
        var resultNoProfile = compat.EvaluateDocument(doc);
        Assert.DoesNotContain(resultNoProfile.Issues, i => i.Message.Contains("Material3", StringComparison.OrdinalIgnoreCase));

        // 2. Evaluate with profile parameter
        var resultWithProfile = compat.EvaluateDocument(doc, profile);
        Assert.DoesNotContain(resultWithProfile.Issues, i => i.Message.Contains("Material3", StringComparison.OrdinalIgnoreCase));

        // 3. Overall readiness assessment
        var readiness = new SsotReadinessEngine().Assess(doc, profile);
        Assert.DoesNotContain(readiness.Errors, e => e.Message.Contains("Material3", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExplicitProfileId_PersistsAndNormalizes()
    {
        var doc = new SsotDocument
        {
            ProfileId = "android_app",
            Environment = { TargetOs = "Android", UiFramework = "Jetpack Compose" }
        };

        var exporter = new JsonExporter();
        var json = exporter.ToJson(doc);
        var loaded = exporter.LoadFromJson(json);

        Assert.Equal("android_app", loaded.ProfileId);

        // Blank profileId normalizer test
        var blankDoc = new SsotDocument
        {
            ProfileId = "",
            Environment = { TargetOs = "Android", UiFramework = "Jetpack Compose" }
        };
        SsotDocumentNormalizer.Normalize(blankDoc);
        Assert.Equal("android_app", blankDoc.ProfileId);
    }

    [Fact]
    public void MigrationPipeline_UpgradesLegacyV100Json()
    {
        var legacyJson = """
        {
          "ssotVersion": "1.0.0",
          "project": { "id": "test-app", "name": "Legacy App" },
          "environment": { "targetOs": "Android", "uiFramework": "Jetpack Compose" },
          "ui": {
            "screens": [
              { "id": "main", "title": "Main Screen" },
              { "id": "settings", "title": "Settings Screen" }
            ]
          }
        }
        """;

        var migratedJson = SsotDocumentMigrator.MigrateJson(legacyJson);
        Assert.Contains("\"ssotVersion\": \"1.1.0\"", migratedJson);
        Assert.Contains("\"profileId\": \"android_app\"", migratedJson);
        Assert.Contains("\"screenType\": \"Screen\"", migratedJson);

        var exporter = new JsonExporter();
        var doc = exporter.LoadFromJson(legacyJson);
        Assert.Equal("1.1.0", doc.SsotVersion);
        Assert.Equal("android_app", doc.ProfileId);
        Assert.Equal("Screen", doc.Ui.Screens[0].ScreenType);
    }

    [Fact]
    public void ScreenType_DistinctValues_ExportedInSpecs()
    {
        var doc = new SsotDocument
        {
            Project = { Name = "MultiWindow App" }
        };
        doc.Ui.Screens.Add(new ScreenInfo { Id = "main", Title = "Main Window", ScreenType = "Window" });
        doc.Ui.Screens.Add(new ScreenInfo { Id = "pref", Title = "Preferences", ScreenType = "Dialog" });
        doc.Ui.Screens.Add(new ScreenInfo { Id = "sheet", Title = "Filters", ScreenType = "BottomSheet" });

        var reqs = AgentPackageExporter.BuildRequirements(doc);
        Assert.Contains("Type: Window", reqs);
        Assert.Contains("Type: Dialog", reqs);
        Assert.Contains("Type: BottomSheet", reqs);
    }

    [Fact]
    public void BlueprintCompiler_AssignsPlatformAwareScreenTypes()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(SchemasDir);
        var profile = catalog.RequireById("wpf_desktop");

        var answers = new WizardAnswers
        {
            ProfileId = "wpf_desktop",
            ProjectName = "Desktop App",
            SavePastResults = true,
            IncludeSettingsScreen = true
        };

        var compiler = new BlueprintCompiler();
        var doc = compiler.BuildNew(answers, profile);

        var main = doc.Ui.Screens.First(s => s.Id == "main");
        Assert.Equal("Window", main.ScreenType);

        var history = doc.Ui.Screens.First(s => s.Id == "history");
        Assert.Equal("Dialog", history.ScreenType);

        var settings = doc.Ui.Screens.First(s => s.Id == "settings");
        Assert.Equal("Dialog", settings.ScreenType);
    }

    [Fact]
    public void UndoManager_SnapshotsAndRestores()
    {
        var manager = new UndoManager();
        var answers = new WizardAnswers { ProjectName = "State 1" };
        var doc = new SsotDocument { Project = { Name = "Doc 1" } };

        manager.RecordSnapshot("Initial", WizardStep.YourIdea, answers, doc);
        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);

        answers.ProjectName = "State 2";
        manager.RecordSnapshot("Name Change", WizardStep.YourIdea, answers, doc);
        Assert.Equal(2, manager.UndoCount);

        var undone = manager.Undo(WizardStep.YourIdea, answers, doc);
        Assert.NotNull(undone);
        Assert.True(manager.CanRedo);
        Assert.Contains("State 2", undone.AnswersJson);

        var redone = manager.Redo(WizardStep.YourIdea, answers, doc);
        Assert.NotNull(redone);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void AgentPackageExporter_EmitsAgentContextFiles()
    {
        var doc = new SsotDocument
        {
            Project = { Name = "SmartApp", Description = "An intelligent assistant app." },
            Environment = { TargetOs = "Windows 11", Runtime = ".NET 8", UiFramework = "WPF", Language = "C#" },
            Architecture = { Pattern = "MVVM" },
            Data = { Storage = { Type = "SQLite" } },
            Constraints = { LoggingRequired = true, TelemetryAllowed = false }
        };

        var root = Path.Combine(Path.GetTempPath(), "caa-agent-ctx-" + Guid.NewGuid().ToString("N"));
        try
        {
            var exporter = new AgentPackageExporter();
            var files = exporter.Export(doc, root);

            var cursorRules = Path.Combine(root, ".cursorrules");
            Assert.True(File.Exists(cursorRules));
            var cursorText = File.ReadAllText(cursorRules);
            Assert.Contains("MVVM", cursorText);
            Assert.Contains("SQLite", cursorText);

            var claudeMd = Path.Combine(root, "CLAUDE.md");
            Assert.True(File.Exists(claudeMd));
            var claudeText = File.ReadAllText(claudeMd);
            Assert.Contains("WPF", claudeText);

            var geminiCfg = Path.Combine(root, ".gemini", "config.json");
            Assert.True(File.Exists(geminiCfg));

            var gitIgnore = Path.Combine(root, ".gitignore");
            Assert.True(File.Exists(gitIgnore));
            var gitIgnoreText = File.ReadAllText(gitIgnore);
            Assert.Contains("[Bb]in/", gitIgnoreText);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ControlReordering_MovesItemsAccurately()
    {
        var screen = new ScreenInfo { Id = "main", Title = "Main" };
        var c1 = new ControlInfo { Id = "c1", Label = "Control 1" };
        var c2 = new ControlInfo { Id = "c2", Label = "Control 2" };
        var c3 = new ControlInfo { Id = "c3", Label = "Control 3" };
        screen.Controls.Add(c1);
        screen.Controls.Add(c2);
        screen.Controls.Add(c3);

        // Move c2 up (delta -1)
        var index = screen.Controls.IndexOf(c2);
        screen.Controls.RemoveAt(index);
        screen.Controls.Insert(index - 1, c2);

        Assert.Equal("c2", screen.Controls[0].Id);
        Assert.Equal("c1", screen.Controls[1].Id);
        Assert.Equal("c3", screen.Controls[2].Id);
    }
}
