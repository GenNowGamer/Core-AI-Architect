using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Wizard;

/// <summary>
/// Legacy entry point — delegates to <see cref="BlueprintCompiler"/> with a minimal starter screen.
/// Prefer <see cref="BlueprintCompiler"/> for new screen-first flows.
/// </summary>
public sealed class SsotSmartBuilder
{
    private readonly BlueprintCompiler _compiler = new();

    public SsotDocument Build(WizardAnswers answers, FormSchemaDefinition profile)
    {
        var doc = _compiler.BuildNew(answers, profile, seedMainScreen: true);
        var types = BlueprintCompiler.ResolveControlTypes(profile);

        // Seed a simple Generate button so readiness still passes for tests / empty starts.
        if (doc.Ui.Screens[0].Controls.Count == 0)
        {
            var button = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1);
            button.Id = "generateButton";
            button.Label = "Generate";
            button.Action = "GenerateResults";
            doc.Ui.Screens[0].Controls.Add(button);
        }

        if (answers.SavePastResults || answers.Memory != MemoryChoice.None)
            answers.SavePastResults = true;

        return _compiler.Compile(doc, answers, profile);
    }
}
