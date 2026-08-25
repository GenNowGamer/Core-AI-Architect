using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Wizard;

/// <summary>Starter blueprints so beginners are not staring at a blank screen.</summary>
public static class BlueprintTemplates
{
    public sealed record TemplateInfo(string Id, string Title, string Description);

    public static IReadOnlyList<TemplateInfo> All { get; } = new[]
    {
        new TemplateInfo("blank", "Start blank", "One empty main screen — you add everything yourself."),
        new TemplateInfo("lottery", "Number / lottery picker",
            "Pick a game, type how many tickets, press Generate, see results on one screen."),
        new TemplateInfo("simple_form", "Simple form",
            "A few places to type plus a Submit button."),
        new TemplateInfo("list_history", "List + save history",
            "A results list and a History screen for past items.")
    };

    public static void Apply(string templateId, SsotDocument doc, FormSchemaDefinition profile)
    {
        var types = BlueprintCompiler.ResolveControlTypes(profile);
        doc.Ui.Screens.Clear();

        switch (templateId)
        {
            case "lottery":
                ApplyLottery(doc, types);
                break;
            case "simple_form":
                ApplySimpleForm(doc, types);
                break;
            case "list_history":
                ApplyListHistory(doc, types);
                break;
            default:
                doc.Ui.Screens.Add(new ScreenInfo { Id = "main", Title = "Main", Route = "/" });
                break;
        }
    }

    /// <summary>Lightweight hints from the user's description text.</summary>
    public static IReadOnlyList<string> SuggestFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Array.Empty<string>();

        var text = description.ToLowerInvariant();
        var tips = new List<string>();

        if (ContainsAny(text, "lottery", "lotto", "ticket", "powerball", "mega millions", "game"))
            tips.Add("This sounds like a number/lottery picker — try the “Number / lottery picker” template.");

        if (ContainsAny(text, "rule", "ball", "fireball", "range", "cache", "illinois"))
            tips.Add("Good — keep game rules and caching notes in this description; click steps should reference them.");

        if (ContainsAny(text, "generate", "create", "make numbers"))
            tips.Add("Add a Button named Generate and a Results list on the same screen.");

        if (ContainsAny(text, "ticket count", "how many", "quantity", "number of"))
            tips.Add("Add a Place to type labeled Ticket Count.");

        if (ContainsAny(text, "pick", "choose", "select", "dropdown", "combo"))
            tips.Add("Add a Choice list for the options people pick.");

        if (ContainsAny(text, "history", "past", "save", "remember"))
            tips.Add("On the Remember step, turn on “Save past results?”");

        if (ContainsAny(text, "settings", "preferences"))
            tips.Add("On the Remember step, turn on “Include a settings screen?”");

        return tips;
    }

    private static void ApplyLottery(SsotDocument doc, IReadOnlyList<string> types)
    {
                var main = new ScreenInfo
                {
                    Id = "main",
                    Title = "Generate Your Winning Lotto Numbers",
                    Route = "/"
                };

                var choice = BlueprintCompiler.CreateThing(BlueprintThingKind.ChoiceList, types, 1);
        choice.Id = "gamePicker";
        choice.Label = "Illinois Lottery game";
        choice.Action = "SelectOption";
        main.Controls.Add(choice);

        var count = BlueprintCompiler.CreateThing(BlueprintThingKind.PlaceToType, types, 1);
        count.Id = "ticketCount";
        count.Label = "Ticket Count";
        count.Action = "SetTicketCount";
        main.Controls.Add(count);

        var generate = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1);
        generate.Id = "generateButton";
        generate.Label = "Generate";
        generate.Action = "GenerateResults";
        main.Controls.Add(generate);

        var results = BlueprintCompiler.CreateThing(BlueprintThingKind.ResultsList, types, 1);
        results.Id = "resultsList";
        results.Label = "Ticket results";
        results.Action = "ViewResults";
        main.Controls.Add(results);

        doc.Ui.Screens.Add(main);
        doc.Workflows.Clear();
        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "generateResults",
            Name = "Generate",
            Trigger = "generateButton",
            Steps =
            {
                "Read the selected lottery game",
                "Read the ticket count",
                "Create that many tickets using the game’s number rules",
                "Show the tickets in the results list"
            },
            ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
        });
    }

    private static void ApplySimpleForm(SsotDocument doc, IReadOnlyList<string> types)
    {
        var main = new ScreenInfo { Id = "main", Title = "Form", Route = "/" };
        var name = BlueprintCompiler.CreateThing(BlueprintThingKind.PlaceToType, types, 1);
        name.Id = "nameBox";
        name.Label = "Name";
        main.Controls.Add(name);

        var notes = BlueprintCompiler.CreateThing(BlueprintThingKind.PlaceToType, types, 2);
        notes.Id = "notesBox";
        notes.Label = "Notes";
        main.Controls.Add(notes);

        var submit = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1);
        submit.Id = "submitButton";
        submit.Label = "Submit";
        submit.Action = "SubmitForm";
        main.Controls.Add(submit);

        doc.Ui.Screens.Add(main);
        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "submitForm",
            Name = "Submit",
            Trigger = "submitButton",
            Steps = { "Read the form fields", "Validate the values", "Save or show confirmation" },
            ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
        });
    }

    private static void ApplyListHistory(SsotDocument doc, IReadOnlyList<string> types)
    {
        var main = new ScreenInfo { Id = "main", Title = "Items", Route = "/" };
        var results = BlueprintCompiler.CreateThing(BlueprintThingKind.ResultsList, types, 1);
        results.Id = "itemsList";
        results.Label = "Items";
        main.Controls.Add(results);

        var add = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1);
        add.Id = "addButton";
        add.Label = "Add item";
        add.Action = "AddItem";
        main.Controls.Add(add);

        doc.Ui.Screens.Add(main);

        var history = new ScreenInfo { Id = "history", Title = "History", Route = "/history" };
        var list = BlueprintCompiler.CreateThing(BlueprintThingKind.ResultsList, types, 2);
        list.Id = "historyList";
        list.Label = "Past results";
        history.Controls.Add(list);
        var save = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 2);
        save.Id = "saveButton";
        save.Label = "Save";
        save.Action = "SaveHistory";
        history.Controls.Add(save);
        doc.Ui.Screens.Add(history);
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}
