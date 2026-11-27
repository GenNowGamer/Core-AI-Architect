using CommunityToolkit.Mvvm.Input;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.App.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private void AddScreen()
    {
        EnsureDocumentFromIdea();
        if (Document is null) return;

        RecordUndo("Add Screen");
        var n = Document.Ui.Screens.Count(s => !IsSystemScreen(s)) + 1;
        var screen = new ScreenInfo
        {
            Id = $"screen{n}",
            Title = $"Screen {n}",
            Route = n == 1 ? "/" : $"/screen{n}",
            ScreenType = Answers.ProfileId.Equals("android_app", StringComparison.OrdinalIgnoreCase) ? "Screen" : "Dialog"
        };
        Document.Ui.Screens.Add(screen);
        ReviewScreens.Add(screen);
        SelectedScreen = screen;
        MarkDirty();
        StatusText = $"Added screen “{screen.Title}”.";
    }

    [RelayCommand]
    private void RemoveScreen()
    {
        if (Document is null || SelectedScreen is null) return;
        if (IsSystemScreen(SelectedScreen))
        {
            _dialogs.ShowMessage("History and Settings are controlled on the Remember step. Turn them off there instead.");
            return;
        }

        if (Document.Ui.Screens.Count(s => !IsSystemScreen(s)) <= 1)
        {
            _dialogs.ShowMessage("Keep at least one main screen.");
            return;
        }

        RecordUndo($"Remove Screen '{SelectedScreen.Title}'");
        Document.Ui.Screens.Remove(SelectedScreen);
        ReviewScreens.Remove(SelectedScreen);
        SelectedScreen = ReviewScreens.FirstOrDefault();
        MarkDirty();
    }

    [RelayCommand]
    private void MoveScreenUp() => MoveSelectedScreen(-1);

    [RelayCommand]
    private void MoveScreenDown() => MoveSelectedScreen(1);

    [RelayCommand]
    private void MoveControlUp() => MoveSelectedControl(-1);

    [RelayCommand]
    private void MoveControlDown() => MoveSelectedControl(1);

    private void MoveSelectedControl(int delta)
    {
        if (SelectedScreen is null || SelectedControl is null) return;
        var index = SelectedScreen.Controls.IndexOf(SelectedControl);
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= SelectedScreen.Controls.Count) return;

        RecordUndo("Reorder Control");
        var control = SelectedControl;
        SelectedScreen.Controls.RemoveAt(index);
        SelectedScreen.Controls.Insert(newIndex, control);

        var reviewIndex = ReviewControls.IndexOf(control);
        if (reviewIndex >= 0)
        {
            var reviewNew = Math.Clamp(reviewIndex + delta, 0, ReviewControls.Count - 1);
            ReviewControls.Move(reviewIndex, reviewNew);
        }

        SelectedControl = control;
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
        RefreshWireframe();
        MarkDirty();
    }

    [RelayCommand]
    private void AddChoiceList() => AddThing(BlueprintThingKind.ChoiceList);

    [RelayCommand]
    private void AddPlaceToType() => AddThing(BlueprintThingKind.PlaceToType);

    [RelayCommand]
    private void AddButtonThing() => AddThing(BlueprintThingKind.Button);

    [RelayCommand]
    private void AddResultsList() => AddThing(BlueprintThingKind.ResultsList);

    [RelayCommand]
    private void RemoveThing()
    {
        if (SelectedScreen is null || SelectedControl is null) return;
        RecordUndo($"Remove Thing '{SelectedControl.Label}'");
        SelectedScreen.Controls.Remove(SelectedControl);
        ReviewControls.Remove(SelectedControl);
        SelectedControl = ReviewControls.FirstOrDefault();
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
        RefreshWireframe();
        MarkDirty();
    }

    public void RefreshWireframe() => WireframeRefreshVersion++;

    private void AddThing(BlueprintThingKind kind)
    {
        EnsureDocumentFromIdea();
        if (Document is null || SelectedScreen is null)
        {
            _dialogs.ShowMessage("Select or add a screen first, then add things to it.");
            return;
        }

        RecordUndo($"Add {WizardAnswers.ThingLabel(kind)}");
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        var types = BlueprintCompiler.ResolveControlTypes(profile);
        var control = BlueprintCompiler.CreateThing(kind, types, SelectedScreen.Controls.Count + 1);
        SelectedScreen.Controls.Add(control);
        ReviewControls.Add(control);
        SelectedControl = control;
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
        RefreshWireframe();
        MarkDirty();
        StatusText = $"Added {WizardAnswers.ThingLabel(kind).ToLowerInvariant()} “{control.Label}”.";
    }

    private void MoveSelectedScreen(int delta)
    {
        if (Document is null || SelectedScreen is null) return;
        var index = Document.Ui.Screens.IndexOf(SelectedScreen);
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= Document.Ui.Screens.Count) return;

        RecordUndo("Reorder Screen");
        var screen = SelectedScreen;
        Document.Ui.Screens.RemoveAt(index);
        Document.Ui.Screens.Insert(newIndex, screen);
        var reviewIndex = ReviewScreens.IndexOf(screen);
        if (reviewIndex >= 0)
        {
            var reviewNew = Math.Clamp(reviewIndex + delta, 0, ReviewScreens.Count - 1);
            ReviewScreens.Move(reviewIndex, reviewNew);
        }

        SelectedScreen = screen;
        MarkDirty();
    }

    private void SyncReviewSelections()
    {
        if (Document is null) return;
        ReviewScreens.Clear();
        foreach (var screen in Document.Ui.Screens)
            ReviewScreens.Add(screen);
        SelectedScreen = ReviewScreens.FirstOrDefault();
        ReloadReviewControls();
        SelectedControl = ReviewControls.FirstOrDefault();
        ScreenCaption = BlueprintCompiler.ScreenCaption(SelectedScreen);
    }

    private void ReloadReviewControls()
    {
        ReviewControls.Clear();
        if (SelectedScreen?.Controls is null) return;
        foreach (var control in SelectedScreen.Controls)
            ReviewControls.Add(control);
    }

    private void SyncClickFlowsFromDocument()
    {
        if (Document is null) return;
        CompileDocumentLightWorkflows();

        var previous = SelectedClickFlow?.Button.Id;
        ClickFlows.Clear();
        var buttons = Document.Ui.Screens.SelectMany(s => s.Controls)
            .Where(c => IsButtonType(c.Type))
            .ToList();

        foreach (var button in buttons)
        {
            var workflow = Document.Workflows.FirstOrDefault(w =>
                w.Trigger.Equals(button.Id, StringComparison.OrdinalIgnoreCase));
            if (workflow is null)
            {
                workflow = new WorkflowInfo
                {
                    Id = button.Id,
                    Name = button.Label,
                    Trigger = button.Id,
                    Steps = { "Read the user's current choices", "Do the action", "Show the outcome on screen" },
                    ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
                };
                Document.Workflows.Add(workflow);
            }

            ClickFlows.Add(new ClickFlowOption(button, workflow));
        }

        SelectedClickFlow = ClickFlows.FirstOrDefault(c => c.Button.Id == previous) ?? ClickFlows.FirstOrDefault();
    }

    private void CompileDocumentLightWorkflows()
    {
        if (Document is null) return;
        var profile = _schemaCatalog.RequireById(Answers.ProfileId);
        Document = _compiler.Compile(Document, Answers, profile);
        _documents.ReplaceDocument(Document, profile, _documents.FilePath);
    }

    private void SyncClickFlowStepsToDocument()
    {
        foreach (var flow in ClickFlows)
        {
            flow.Workflow.Steps = flow.StepsText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }
    }

    private static bool IsSystemScreen(ScreenInfo screen) =>
        screen.Id.Equals("history", StringComparison.OrdinalIgnoreCase)
        || screen.Id.Equals("settings", StringComparison.OrdinalIgnoreCase);

    private static bool IsButtonType(string type) =>
        type.Equals("Button", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Command", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Cmdlet", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Endpoint", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Stage", StringComparison.OrdinalIgnoreCase);
}
