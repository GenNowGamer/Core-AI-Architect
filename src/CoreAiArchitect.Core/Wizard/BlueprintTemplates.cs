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
        new TemplateInfo("master_detail", "Master-Detail / Catalog",
            "Browse a list of items with category filters, view details, and edit entries."),
        new TemplateInfo("dashboard", "Dashboard & Analytics",
            "Key metric summaries, time filters, quick actions, and recent activity records."),
        new TemplateInfo("batch_processor", "File / Batch Processor",
            "Queue files or items, select processing presets and destination, and run batch jobs."),
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
            case "master_detail":
                ApplyMasterDetail(doc, types);
                break;
            case "dashboard":
                ApplyDashboard(doc, types);
                break;
            case "batch_processor":
                ApplyBatchProcessor(doc, types);
                break;
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

    private static void ApplyMasterDetail(SsotDocument doc, IReadOnlyList<string> types)
    {
        var main = new ScreenInfo { Id = "main", Title = "Catalog Browser", Route = "/" };

        var searchBox = BlueprintCompiler.CreateThing(BlueprintThingKind.PlaceToType, types, 1);
        searchBox.Id = "searchBox";
        searchBox.Label = "Search catalog";
        main.Controls.Add(searchBox);

        var categoryFilter = BlueprintCompiler.CreateThing(BlueprintThingKind.ChoiceList, types, 1);
        categoryFilter.Id = "categoryFilter";
        categoryFilter.Label = "Filter by category";
        categoryFilter.Action = "FilterList";
        main.Controls.Add(categoryFilter);

        var catalogList = BlueprintCompiler.CreateThing(BlueprintThingKind.ResultsList, types, 1);
        catalogList.Id = "catalogList";
        catalogList.Label = "Catalog items";
        catalogList.Action = "SelectItem";
        main.Controls.Add(catalogList);

        var newButton = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1);
        newButton.Id = "newItemButton";
        newButton.Label = "New item";
        newButton.Action = "OpenEditor";
        main.Controls.Add(newButton);

        doc.Ui.Screens.Add(main);

        var editor = new ScreenInfo { Id = "editor", Title = "Item Editor", Route = "/editor" };
        var titleBox = BlueprintCompiler.CreateThing(BlueprintThingKind.PlaceToType, types, 2);
        titleBox.Id = "itemTitle";
        titleBox.Label = "Item Title";
        editor.Controls.Add(titleBox);

        var descBox = BlueprintCompiler.CreateThing(BlueprintThingKind.PlaceToType, types, 3);
        descBox.Id = "itemDescription";
        descBox.Label = "Description";
        editor.Controls.Add(descBox);

        var saveButton = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 2);
        saveButton.Id = "saveItemButton";
        saveButton.Label = "Save Changes";
        saveButton.Action = "SaveItem";
        editor.Controls.Add(saveButton);

        doc.Ui.Screens.Add(editor);

        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "filterCatalog",
            Name = "Filter Catalog",
            Trigger = "categoryFilter",
            Steps = { "Read selected category", "Query matching catalog items", "Update catalog items list" },
            ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
        });

        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "saveItem",
            Name = "Save Item",
            Trigger = "saveItemButton",
            Steps = { "Read item form fields", "Validate item properties", "Persist item to storage", "Return to catalog" },
            ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
        });
    }

    private static void ApplyDashboard(SsotDocument doc, IReadOnlyList<string> types)
    {
        var main = new ScreenInfo { Id = "main", Title = "Analytics Dashboard", Route = "/" };

        var timeRange = BlueprintCompiler.CreateThing(BlueprintThingKind.ChoiceList, types, 1);
        timeRange.Id = "timeRangeFilter";
        timeRange.Label = "Time range (Today / 7 Days / 30 Days)";
        timeRange.Action = "UpdateTimeRange";
        main.Controls.Add(timeRange);

        var refreshButton = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1);
        refreshButton.Id = "refreshMetricsButton";
        refreshButton.Label = "Refresh Metrics";
        refreshButton.Action = "RefreshDashboard";
        main.Controls.Add(refreshButton);

        var activityList = BlueprintCompiler.CreateThing(BlueprintThingKind.ResultsList, types, 1);
        activityList.Id = "activityFeed";
        activityList.Label = "Recent Activity & Events";
        activityList.Action = "ViewActivityDetail";
        main.Controls.Add(activityList);

        var exportReportButton = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 2);
        exportReportButton.Id = "exportReportButton";
        exportReportButton.Label = "Export Report";
        exportReportButton.Action = "ExportReport";
        main.Controls.Add(exportReportButton);

        doc.Ui.Screens.Add(main);

        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "refreshDashboard",
            Name = "Refresh Dashboard",
            Trigger = "refreshMetricsButton",
            Steps = { "Fetch updated metrics from data source", "Aggregate totals by time range", "Populate recent activity feed" },
            ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
        });
    }

    private static void ApplyBatchProcessor(SsotDocument doc, IReadOnlyList<string> types)
    {
        var main = new ScreenInfo { Id = "main", Title = "Batch Processor", Route = "/" };

        var destinationBox = BlueprintCompiler.CreateThing(BlueprintThingKind.PlaceToType, types, 1);
        destinationBox.Id = "destinationFolder";
        destinationBox.Label = "Destination folder";
        main.Controls.Add(destinationBox);

        var formatPicker = BlueprintCompiler.CreateThing(BlueprintThingKind.ChoiceList, types, 1);
        formatPicker.Id = "formatPreset";
        formatPicker.Label = "Processing preset / format";
        formatPicker.Action = "SelectPreset";
        main.Controls.Add(formatPicker);

        var queueList = BlueprintCompiler.CreateThing(BlueprintThingKind.ResultsList, types, 1);
        queueList.Id = "fileQueue";
        queueList.Label = "File processing queue";
        queueList.Action = "SelectQueueItem";
        main.Controls.Add(queueList);

        var startButton = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 1);
        startButton.Id = "startBatchButton";
        startButton.Label = "Start Processing";
        startButton.Action = "ProcessBatch";
        main.Controls.Add(startButton);

        doc.Ui.Screens.Add(main);

        var logsScreen = new ScreenInfo { Id = "logs", Title = "Batch Summary & Logs", Route = "/logs" };
        var logList = BlueprintCompiler.CreateThing(BlueprintThingKind.ResultsList, types, 2);
        logList.Id = "processLog";
        logList.Label = "Execution logs and results";
        logsScreen.Controls.Add(logList);

        var openDestButton = BlueprintCompiler.CreateThing(BlueprintThingKind.Button, types, 2);
        openDestButton.Id = "openDestinationButton";
        openDestButton.Label = "Open Destination Folder";
        openDestButton.Action = "OpenDestination";
        logsScreen.Controls.Add(openDestButton);

        doc.Ui.Screens.Add(logsScreen);

        doc.Workflows.Add(new WorkflowInfo
        {
            Id = "processBatch",
            Name = "Process Batch",
            Trigger = "startBatchButton",
            Steps = { "Verify destination folder and input queue", "Iterate items and apply transformation preset", "Log status per item", "Display completion summary" },
            ErrorHandling = new ErrorHandlingInfo { Strategy = "ShowMessageBox", LogLevel = "Warning" }
        });
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}
