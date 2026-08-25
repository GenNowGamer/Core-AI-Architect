namespace CoreAiArchitect.App.Help;

/// <summary>
/// Central registry of in-app help. Keys are usually SSOT paths (project.name)
/// or logical ids (section.domain, ui.builder.theme, tools.validate).
/// </summary>
public static class HelpCatalog
{
    private static readonly Dictionary<string, HelpTopic> Topics =
        new(StringComparer.OrdinalIgnoreCase);

    static HelpCatalog()
    {
        foreach (var topic in BuildTopics())
            Topics[topic.Key] = topic;
    }

    public static HelpTopic Get(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Fallback("Help", "No help key was provided for this control.");

        if (Topics.TryGetValue(key, out var topic))
            return topic;

        // Nested list fields often use parent.child keys; try last two segments.
        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var tail = $"{parts[^2]}.{parts[^1]}";
            if (Topics.TryGetValue(tail, out topic))
                return topic;
            if (Topics.TryGetValue(parts[^1], out topic))
                return topic;
        }

        return Fallback(key, $"No detailed help is registered for '{key}' yet. See docs/USER_GUIDE.md for section overviews.");
    }

    public static string GetSummary(string? key) => Get(key).Summary;

    private static HelpTopic Fallback(string title, string details) => new()
    {
        Key = title,
        Title = title,
        Summary = details,
        Details = details
    };

    private static IEnumerable<HelpTopic> BuildTopics()
    {
        // ---- Beginner wizard ----
        yield return T("wizard.yourIdea", "Ad-Lib",
            "Fill in the blanks about your app, then optionally add product rules and a template.",
            """
            This is the product brief an AI coding agent will read first.

            • App type — Windows desktop is the usual choice for home/office tools.
            • Ad-Lib — who it is for, what it helps them do, main capabilities (one per line), online/offline, look.
            • Product rules — game lists, number rules, validation, caching. You do not design database tables here.
            • Templates — Number / lottery picker is a good starting shape for ticket/number apps. On a blank start, ad-lib capabilities become starter buttons.

            Domain entities in the exported JSON stay generic on purpose. Rich rules belong in the ad-lib / product rules and later click steps.
            """);

        yield return T("wizard.screens", "Screens",
            "Build what people see using everyday things.",
            """
            Add screens, then add things:

            • Choice list — pick one option (like a game).
            • Place to type — numbers or text (like ticket count).
            • Button — starts work (like Generate).
            • Results list — shows what was created.

            Screens are the UI contract. Rename labels so they match your app. History and Settings appear only if you turn them on in Remember.
            """);

        yield return T("wizard.clicks", "When they click",
            "Describe what each button does — these steps help the AI invent the real data model.",
            """
            Pick a button on the left. Write one step per line on the right.

            Example for Generate:
            Read the selected lottery game
            Read the ticket count
            Create that many tickets using the game’s rules
            Show the tickets in the results list

            Together with your description, these steps tell the coding agent how to model tickets/games — not the generic OptionChoice/ResultItem placeholders alone.
            """);

        yield return T("wizard.remember", "Remember stuff?",
            "Optional History and Settings — off by default for simple apps.",
            """
            Leave both unchecked unless you need them. Turning them on adds screens automatically; turning them off removes those screens.
            """);

        yield return T("wizard.needs", "What does your app need?",
            "Accounts, multi-device, and notifications in plain language.",
            """
            These gap questions write into requirements and the project description.

            • Accounts — none / separate / not sure (with a short explanation when unsure).
            • Multi-device — whether data should follow the user.
            • Notifications — optional reminder notes; adds a UI contract hint for the agent.

            Conflicts (for example offline + multi-device) show up as Blueprint recommendations.
            """);

        yield return T("wizard.handoff", "Project Blueprint",
            "Command center: completeness, detail level, story, then save or export.",
            """
            The Blueprint hub shows how complete your project is and what decisions remain.

            • Recommendations — deterministic suggestions with Why? and rule IDs (Use to apply).
            • Detail level — Simple / Standard / Expert changes how much technical detail you see (same blueprint underneath).
            • Expert — architecture/storage decision modes: Recommended, Let coding agent decide (agent_discretion), Choose myself.
            • Double-click a section (or Edit selected section) to jump back and fix it.
            • Check again runs readiness + compatibility.
            • Export for AI agent writes the JSON blueprint; Export Agent Package writes a full folder (AGENTS.md, specs, tasks).

            Domain entities are scaffolds; description + click steps carry product rules.
            """);

        // ---- Sections ----
        yield return T("section.project", "Project",
            "Identity and intent for the whole application.",
            """
            Use Project to tell the coding agent what this app is called and why it exists.

            Fill carefully:
            • Project Id — machine-friendly key (kebab-case, no spaces), e.g. core-lottery-generator. Used in filenames and stable references.
            • Project Name — human title shown in windows/README, e.g. Core Lottery Generator.
            • Description — the main narrative of purpose and scope. Agents lean on this heavily when other sections are thin.
            • Owner / Tags — optional metadata for docs.

            Tip: Put “why/scope” here. Put structured rules (screens and click steps) in the Screens and Clicks steps—not only in the description paragraph.
            """);

        yield return T("section.environment", "Environment",
            "Platform stack the generated solution must use.",
            """
            Environment locks the technical stack so the agent does not pick a different OS/runtime/UI by accident.

            • Target OS — where the app runs (Windows 11, Server, Cross-Platform, …).
            • Runtime — e.g. .NET 8 / .NET 9 / PowerShell 7.
            • UI Framework — WPF, WinUI, ASP.NET, Console, PowerShell, or None. This drives project type.
            • Language — C#, PowerShell, or F#.

            Keep this consistent with UI → Navigation Pattern (e.g. WPF + Window+Dialogs, ASP.NET + HttpRoutes).
            """);

        yield return T("section.architecture", "Architecture",
            "How code should be organized: pattern, layers, and modules.",
            """
            Architecture tells the agent how to structure the solution—not what screens look like.

            • Pattern — overall style (MVVM for WPF, CleanArchitecture, Modular, …). Affects folders and dependency direction.
            • Layers — named layers as a list (UI, Domain, Infrastructure). Instructs separation of concerns.
            • Modules — feature areas (e.g. DrawingEngine, Persistence). Each module can list dependencies on other modules.

            Finalize SSOT expects at least one layer or one module for end-to-end readiness.
            """);

        yield return T("section.ui", "UI / UI Layout Builder",
            "Screens, controls, theme, and navigation the agent must build.",
            """
            This section uses the UI Layout Builder. It is the visual contract for GUI/API/CLI surfaces.

            Set Theme and Navigation Pattern at the top, then Add Screen and Add Control for each surface.

            Empty Screens means the agent has no UI contract—only your Description prose. For desktop apps, Finalize requires at least one screen with controls.
            """);

        yield return T("section.domain", "Domain",
            "The data model: entities (types) and their fields (properties).",
            """
            Domain is the structured data model—not free text. Add Entities for each business object the app knows about.

            Per entity: Name (required), Description (optional), then Fields.
            Per field: Name, Type (string/int/bool/decimal/DateTime/Guid), Required.

            Example for a lottery app:
            • LotteryGame — Name (string), BallCount (int), MaxNumber (int)
            • DrawResult — Id (Guid), GameName (string), Numbers (string), DrawnAt (DateTime)

            Finalize requires at least one entity with fields. Do not rely only on Project Description for the data model.
            """);

        yield return T("section.workflows", "Workflows",
            "Named processes: what happens when the user clicks Generate, Save, etc.",
            """
            Use the Workflow Builder. Each workflow is a process the agent should implement.

            • Id / Name — stable key and display name.
            • Trigger — preferably a control Id from the UI section so clicks wire to code.
            • Steps — ordered plain-language checklist the agent follows.
            • Error Strategy / Log Level — how failures are handled and logged.

            Finalize requires at least one workflow with steps; when UI controls exist, Trigger is required too.
            """);

        yield return T("section.constraints", "Constraints",
            "Hard rules for libraries, logging, telemetry, and policy.",
            """
            Constraints are non-negotiable instructions for the coding agent.

            • Allowed Libraries — preferred NuGet/package ids when a need arises (e.g. CommunityToolkit.Mvvm).
            • Forbidden Libraries — packages the agent must never add (e.g. Newtonsoft.Json if you want System.Text.Json only).
            • Logging Required — if true, the agent must include logging.
            • Telemetry Allowed — if false, no analytics/telemetry.
            • Third-Party Bootloaders Allowed — policy flag for bootloader-style third parties (especially WinPE-style tools).
            """);

        yield return T("section.data", "Data",
            "Whether and how the app persists data.",
            """
            • Storage Type — None, LocalJson, SQLite, SQLServer, PostgreSQL, Registry, etc.
            • Connection String Mode — None, ConfigFile, EnvironmentVariable, or UserSecrets (how secrets are supplied).
            • Migrations Required — true when using a real DB whose schema will evolve (EF migrations / SQL scripts). False for None/LocalJson/Registry.

            Match storage to the product: lottery history in LocalJson or SQLite is common; settings-only often LocalJson.
            """);

        yield return T("section.build", "Build",
            "Output type and publish targets (RIDs).",
            """
            • Output Type — WinExe (GUI), Exe (console), or Library.
            • Configuration — Debug/Release list the agent should support.
            • Targets — at least one Name + Runtime Identifier (e.g. win-x64). Finalize requires a build target for E2E readiness.
            """);

        yield return T("section.deployment", "Deployment",
            "How the finished app is packaged and updated.",
            """
            • Packaging — None, MSIX, Zip, Docker, DotnetTool, PSGallery, WinPEMedia, etc.
            • Auto Update — whether the agent should plan for automatic updates.
            """);

        yield return T("section.testing", "Testing",
            "Expected automated test approach for the generated app.",
            """
            • Unit Test Framework — xUnit, NUnit, MSTest, Pester, or None.
            • UI Test Framework — None, FlaUI, WinAppDriver, or Playwright.
            • Coverage Target — number from 0 to 1 (e.g. 0.6 ≈ 60%).

            For WPF/WinUI prefer FlaUI or None—not Playwright (web-oriented)—unless you intentionally have a web UI.
            """);

        yield return T("section.documentation", "Documentation",
            "Which docs the agent/export should produce, plus Finalize SSOT.",
            """
            • Generate README / Architecture Doc — flags for the agent and Markdown export.
            • Sections — named doc topics (Overview, Architecture, Workflows, Entities, …).

            Use Finalize SSOT here when you believe the blueprint is complete. It runs schema validation plus end-to-end readiness (UI, domain, workflows, build targets, etc.) and always shows a clear Ready / Not Ready dialog.
            """);

        // ---- Project fields ----
        yield return T("project.id", "Project Id",
            "Machine-friendly project key (kebab-case, no spaces).",
            """
            A stable identifier for tooling and filenames—not the pretty display name.

            Good: core-lottery-generator, inventory-manager
            Avoid: spaces, punctuation, changing it often after export

            Effect: suggested save names (id.ssot.json), package/identity hints, agent references.
            """);

        yield return T("project.name", "Project Name",
            "Human-readable product title.",
            """
            The name users and README headings should see, e.g. Core Lottery Generator.

            Effect: window titles, branding text, documentation headings.
            """);

        yield return T("project.description", "Description",
            "Plain-language purpose and scope of the app.",
            """
            Write what the app does, who it is for, and what is in/out of scope.

            Agents use this as the primary narrative. Still fill Domain/UI/Workflows for a complete handoff—Description alone is not enough for Finalize.

            Example: “Desktop app that generates lottery number sets for configured games, saves draw history locally, and lets users review past results.”
            """);

        yield return T("project.owner", "Owner",
            "Person, team, or company responsible for the project.",
            """
            Optional metadata for documentation (e.g. Coresynth Studios). Does not change runtime behavior.
            """);

        yield return T("project.tags", "Tags",
            "Short labels for categorization.",
            """
            Add one tag at a time (wpf, lottery, desktop). Used as metadata in docs/export—not as a substitute for Domain or UI.
            """);

        // ---- Environment ----
        yield return T("environment.targetOs", "Target OS",
            "Operating system the app is built for.",
            """
            Choose the OS the agent should assume (APIs, packaging, paths).

            Examples: Windows 11 for a consumer desktop lottery app; Windows Server for services; Cross-Platform only when you truly need it.
            """);

        yield return T("environment.runtime", "Runtime",
            "Execution stack / SDK target.",
            """
            Typically .NET 8 or .NET 9 for C# apps; PowerShell 7 for modules; Native for special cases.

            Effect: TargetFramework and SDK assumptions in the generated solution.
            """);

        yield return T("environment.uiFramework", "UI Framework",
            "Presentation technology for the product.",
            """
            • WPF / WinUI — desktop GUI (use UI Layout Builder for windows/controls).
            • ASP.NET — web API surfaces (screens ≈ endpoints).
            • Console / PowerShell — CLI/cmdlet surfaces.
            • None — no interactive UI.

            This must stay consistent with Navigation Pattern and Testing → UI Test Framework.
            """);

        yield return T("environment.language", "Language",
            "Primary programming language for generated code.",
            """
            Usually C# for WPF/WinUI/API; PowerShell for modules; F# when you explicitly want it.
            """);

        // ---- Architecture ----
        yield return T("architecture.pattern", "Architecture Pattern",
            "Overall design style for the solution.",
            """
            Examples:
            • MVVM — standard for WPF/WinUI (Views + ViewModels).
            • CleanArchitecture — domain-centered rings.
            • Modular — feature modules with clear boundaries.
            • MVC / MinimalApi — common for web.

            The agent uses this to choose folder layout and dependency rules.
            """);

        yield return T("architecture.layers", "Layers",
            "Named architectural layers to keep separate.",
            """
            Add layer names one at a time, e.g. UI, Application, Domain, Infrastructure.

            Effect: agent should not dump all code into one project/folder; keep those layers distinct.
            """);

        yield return T("architecture.modules", "Modules",
            "Feature modules that split the product.",
            """
            Click Add Item for each feature area (DrawingEngine, History, Settings).

            Per module fill Name (required), Description, and Dependencies (other module names this one uses).

            Finalize expects layers and/or modules for a clear solution structure.
            """);

        yield return T("architecture.modules.name", "Module Name",
            "Stable name of a feature module.",
            """
            Use a clear code-friendly name: DrawingEngine, Persistence, UiShell.
            """);

        yield return T("architecture.modules.description", "Module Description",
            "What this module is responsible for.",
            """
            One or two sentences for the agent, e.g. “Generates and validates lottery number sets.”
            """);

        yield return T("architecture.modules.dependencies", "Module Dependencies",
            "Other modules this module depends on.",
            """
            List other module Names this one may reference. Helps the agent order projects and avoid circular references.
            """);

        // ---- UI form fields (also builder) ----
        yield return T("ui.theme", "Theme",
            "Visual styling approach for the target app (not Core AI Architect’s own look).",
            """
            Written to ui.theme for the coding agent:

            • Default — stock/platform look (plain WPF defaults).
            • Fluent — Microsoft Fluent-style cues (modern WinUI/Fluent patterns).
            • Custom — your own branding; do not assume stock or Fluent.

            Changing this does not restyle Core AI Architect; it only changes the SSOT export.
            """);

        yield return T("ui.navigationPattern", "Navigation Pattern",
            "How users move between screens/surfaces.",
            """
            Must fit the UI Framework:

            • Window+Dialogs — classic WPF main window + dialogs.
            • NavigationFrame / Tabbed — frame or tab shell (common for WinUI).
            • HttpRoutes — ASP.NET APIs.
            • CommandLine / CmdletPipeline / Pipeline / Wizard / Menu — CLI, PowerShell, automation, wizards.

            Mismatches (e.g. WinUI + Window+Dialogs) produce Finalize warnings.
            """);

        yield return T("ui.screens", "Screens / Surfaces",
            "Windows, pages, or API/CLI surfaces to build.",
            """
            Each screen is a surface the agent must create (Main window, History page, Settings dialog, or an API/CLI surface depending on profile).

            Fill Id, Title, Route (optional), then add Controls.

            Finalize (for non-None UI frameworks) requires ≥1 screen with ≥1 control.
            """);

        yield return T("ui.screens.id", "Screen Id",
            "Stable key for the screen.",
            """
            Machine id used in code/navigation, e.g. main, history, settings. Prefer camelCase or kebab without spaces.
            """);

        yield return T("ui.screens.title", "Screen Title",
            "User-visible title for the window/page.",
            """
            Shown in window chrome or page headers, e.g. Main, Draw History.
            """);

        yield return T("ui.screens.route", "Screen Route",
            "Optional path/route hint.",
            """
            Useful for frame navigation or APIs (e.g. /, /history). Can be blank for simple Window+Dialogs apps.
            """);

        yield return T("ui.screens.controls", "Controls",
            "Buttons, inputs, grids, endpoints, commands on a screen.",
            """
            Add each interactive element. Id should be unique and is what Workflow → Trigger should match.

            Type depends on profile (Button/TextBox for WPF; Endpoint for API; Command for CLI; Cmdlet for PowerShell).

            Label = visible text; Action = named behavior (e.g. GenerateNumbers) the agent wires to a command/workflow.
            """);

        yield return T("ui.screens.controls.id", "Control Id",
            "Stable id for bindings and workflow triggers.",
            """
            Example: generateButton, gameCombo, saveMenuItem.

            Copy this into Workflow → Trigger so UI actions connect to processes.
            """);

        yield return T("ui.screens.controls.type", "Control Type",
            "What kind of UI/API/CLI element to create.",
            """
            Choose from the profile’s allowed list (Button, TextBox, DataGrid, Endpoint, Command, Cmdlet, …).

            Wrong types for the profile will fail validation.
            """);

        yield return T("ui.screens.controls.label", "Control Label",
            "Visible caption/text for the control.",
            """
            Button text, field label, menu header, etc. Example: Generate, Game, Save Draw.
            """);

        yield return T("ui.screens.controls.action", "Control Action",
            "Named behavior this control should invoke.",
            """
            A short action name the agent maps to a command/workflow, e.g. GenerateNumbers, SaveDraw, OpenHistory.

            Prefer aligning Action with a workflow Id/Name for clarity.
            """);

        // ---- Domain ----
        yield return T("domain.entities", "Entities",
            "Business objects / types in the data model.",
            """
            Click Add Item for each type the app persists or manipulates (LotteryGame, DrawResult, AppSettings).

            Then add Fields for properties. This is what Finalize checks for a real data model.
            """);

        yield return T("domain.entities.name", "Entity Name",
            "Type/class/table name.",
            """
            Use PascalCase type names: LotteryGame, DrawResult. Becomes class or table names in generated code.
            """);

        yield return T("domain.entities.description", "Entity Description",
            "What this entity represents.",
            """
            Optional modeling guidance for the agent, e.g. “A configured lottery game and its number rules.”
            """);

        yield return T("domain.entities.fields", "Entity Fields",
            "Properties of the entity.",
            """
            Add each property: Name, Type, Required.

            Encode product rules here (ball count, max number, timestamps) instead of only in Project Description.
            """);

        yield return T("domain.entities.fields.name", "Field Name",
            "Property name on the entity.",
            """
            Example: BallCount, MaxNumber, DrawnAt. Becomes a code property / column name.
            """);

        yield return T("domain.entities.fields.type", "Field Type",
            "Data type of the property.",
            """
            Allowed: string, int, bool, decimal, DateTime, Guid.

            Pick the closest primitive; complex nested objects should usually be separate entities.
            """);

        yield return T("domain.entities.fields.required", "Field Required",
            "Whether the property must always have a value.",
            """
            When checked, the agent should treat it as non-nullable / validated required input.
            """);

        // ---- Workflows (form schema paths) ----
        yield return T("workflows", "Workflows list",
            "Collection of processes to implement.",
            """
            Prefer the Workflow Builder UI. Each item needs Id, Name, optional Trigger, Steps, and error handling.
            """);

        yield return T("workflows.id", "Workflow Id",
            "Stable workflow key.",
            """
            Example: generateNumbers, saveDraw. Used as a code identifier.
            """);

        yield return T("workflows.name", "Workflow Name",
            "Display name of the process.",
            """
            Human label for docs/UI, e.g. Generate Numbers.
            """);

        yield return T("workflows.trigger", "Workflow Trigger",
            "What starts this workflow—usually a UI control Id.",
            """
            Best practice: set Trigger to a control Id from UI (e.g. generateButton).

            Finalize requires Trigger when interactive UI controls exist. Tools → Validate warns if Trigger does not match a known control Id.
            """);

        yield return T("workflows.steps", "Workflow Steps",
            "Ordered plain-language implementation checklist.",
            """
            Add steps in order, e.g.
            1. Read selected game rules
            2. Generate unique numbers
            3. Display results
            4. Offer save to history

            The agent treats these as the procedure to code.
            """);

        yield return T("workflows.errorHandling.strategy", "Error Strategy",
            "How failures in this workflow should be handled.",
            """
            • ShowMessageBox — user-visible error dialog (common for desktop).
            • Throw — bubble exception to higher handlers.
            • LogAndContinue — log and keep going when safe.
            • Retry — attempt again (agent should implement sensible retry).
            """);

        yield return T("workflows.errorHandling.logLevel", "Log Level",
            "Severity used when logging workflow errors.",
            """
            Trace, Debug, Information, Warning, or Error. Pick Warning or Error for user-facing failures.
            """);

        // ---- Constraints ----
        yield return T("constraints.allowedLibraries", "Allowed Libraries",
            "Preferred packages the agent should use when relevant.",
            """
            List NuGet/package ids (or module names), one per Add.

            Examples: CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, System.Text.Json.

            This is a preference list—not always an exhaustive whitelist—unless you tell the agent otherwise in your prompt.
            Empty means no special preference beyond Forbidden and other constraints.
            """);

        yield return T("constraints.forbiddenLibraries", "Forbidden Libraries",
            "Packages the agent must never add or reference.",
            """
            Hard ban list. Examples:
            • Newtonsoft.Json — force System.Text.Json
            • Prism.Wpf — you standardized on CommunityToolkit
            • EntityFramework — you chose LocalJson only

            Empty means no bans. If a package appears on both lists, treat Forbidden as winning.
            """);

        yield return T("constraints.loggingRequired", "Logging Required",
            "Whether the generated app must include logging.",
            """
            When true, the agent must wire a logging approach (e.g. Microsoft.Extensions.Logging). When false, logging is optional.
            """);

        yield return T("constraints.telemetryAllowed", "Telemetry Allowed",
            "Whether analytics/telemetry may be included.",
            """
            When false, the agent must not add telemetry/analytics SDKs or phone-home behavior.
            """);

        yield return T("constraints.thirdPartyBootloadersAllowed", "Third-Party Bootloaders Allowed",
            "Policy flag for third-party bootloader components.",
            """
            Especially relevant for WinPE/recovery-style tools. When false, the agent must not introduce third-party bootloaders.
            """);

        // ---- Data ----
        yield return T("data.storage.type", "Storage Type",
            "Persistence technology for app data.",
            """
            • None — no persistence.
            • LocalJson — files on disk (simple history/settings).
            • SQLite / SQLServer / PostgreSQL — relational DB.
            • Registry — Windows registry settings.

            Choose based on whether you need queryable history, multi-user DB, or simple files.
            """);

        yield return T("data.storage.connectionStringMode", "Connection String Mode",
            "How database connection secrets are supplied.",
            """
            • None — no connection string (LocalJson/None).
            • ConfigFile — appsettings/config files.
            • EnvironmentVariable — env vars at runtime.
            • UserSecrets — local secret store for development.

            For LocalJson/None, use None.
            """);

        yield return T("data.storage.migrationsRequired", "Migrations Required",
            "Whether DB schema migrations are part of the solution.",
            """
            Use true for SQLite/SQLServer/PostgreSQL when the schema can evolve (EF migrations, SQL scripts).

            Use false for None, LocalJson, Registry, or a fixed schema you will not migrate via tooling.

            This does not run migrations inside Core AI Architect—it tells the coding agent to include migration tooling.
            """);

        // ---- Build ----
        yield return T("build.outputType", "Output Type",
            "Kind of compiled output.",
            """
            • WinExe — Windows GUI executable (typical WPF).
            • Exe — console/executable.
            • Library — class library / module assembly.
            """);

        yield return T("build.configuration", "Build Configuration",
            "Configurations the solution should support.",
            """
            Usually Debug and Release. Add each as a list item. Agents/CI should respect these.
            """);

        yield return T("build.targets", "Build Targets",
            "Runtime identifiers to publish for.",
            """
            Add at least one target for Finalize, e.g.
            • Name: win-x64
            • Runtime Identifier: win-x64

            Other common RIDs: win-x86, win-arm64, portable.
            """);

        yield return T("build.targets.name", "Build Target Name",
            "Friendly name for this publish target.",
            """
            Often matches the RID (win-x64) or a label like Production-x64.
            """);

        yield return T("build.targets.runtimeIdentifier", "Runtime Identifier",
            "dotnet RID for publish.",
            """
            Select from the profile list (win-x64, win-x86, win-arm64, portable). Drives `dotnet publish -r …`.
            """);

        // ---- Deployment ----
        yield return T("deployment.packaging", "Packaging",
            "How the app is distributed.",
            """
            Examples: None, MSIX, Zip, Docker, DotnetTool, PSGallery, WinPEMedia.

            Pick what matches your delivery channel; None if packaging is undecided.
            """);

        yield return T("deployment.autoUpdate", "Auto Update",
            "Whether automatic updates should be planned.",
            """
            When true, the agent should consider an update mechanism appropriate to Packaging (e.g. MSIX auto-update). When false, no auto-update requirement.
            """);

        // ---- Testing ----
        yield return T("testing.unitTestFramework", "Unit Test Framework",
            "Framework for unit tests in the generated solution.",
            """
            xUnit is a common default for .NET; Pester for PowerShell; None if you do not want a test project yet.
            """);

        yield return T("testing.uiTestFramework", "UI Test Framework",
            "UI automation stack for the generated app.",
            """
            • None — no UI automation project.
            • FlaUI / WinAppDriver — desktop (WPF/WinUI).
            • Playwright — web UI (poor fit for pure WPF).

            Finalize warns if Playwright is chosen for WPF/WinUI.
            """);

        yield return T("testing.coverageTarget", "Coverage Target",
            "Desired code coverage as a fraction 0–1.",
            """
            Example: 0.6 means about 60% coverage goal for agents/CI. Use 0 if you do not care yet.
            """);

        // ---- Documentation ----
        yield return T("documentation.generateReadme", "Generate README",
            "Ask the agent/export to produce README-style overview content.",
            """
            When true, documentation output should include an overview README derived from filled SSOT sections.
            """);

        yield return T("documentation.generateArchitectureDoc", "Generate Architecture Doc",
            "Ask for architecture documentation.",
            """
            When true, the agent/export should produce architecture write-ups when Architecture data exists.
            """);

        yield return T("documentation.sections", "Documentation Sections",
            "Named topics to include when data exists.",
            """
            Add topics such as Overview, Architecture, Workflows, Entities. Empty product sections are omitted from Markdown export (no filler).
            """);

        // ---- Builder-specific + actions ----
        yield return T("ui.builder", "UI Layout Builder",
            "Visual editor for theme, navigation, screens, and controls.",
            """
            Top: Theme + Navigation Pattern (exported to ui.*).
            Left: Screens list — Add/Remove.
            Middle: Controls on the selected screen.
            Right: properties for the selected screen/control.

            Fill UI before Workflows so you can set Triggers to control Ids.
            """);

        yield return T("ui.builder.addScreen", "Add Screen",
            "Creates a new window/page/surface.",
            """
            Adds a ScreenInfo with a default id/title. Rename Id/Title, set Route if needed, then Add Control for buttons and inputs.
            """);

        yield return T("ui.builder.addControl", "Add Control",
            "Adds a control to the selected screen.",
            """
            Requires a selected screen. Sets a default Type from the profile’s allowed list. Edit Id, Type, Label, and Action on the right.
            """);

        yield return T("workflow.builder", "Workflow Builder",
            "Editor for processes, triggers, steps, and error handling.",
            """
            Left: list of workflows (Add/Remove).
            Right: Id, Name, Trigger (type or pick from UI control Ids), Error Strategy, Log Level, and ordered Steps.

            Available Triggers refresh from UI controls when you open this section.
            """);

        yield return T("workflow.builder.add", "Add Workflow",
            "Creates a new workflow stub.",
            """
            Adds workflowN with default error handling. Fill Name, Trigger, and Steps before Finalize.
            """);

        yield return T("workflow.builder.addStep", "Add Step",
            "Appends a step to the selected workflow.",
            """
            Type a plain-language step and click Add Step. Use Up/Down to reorder. Steps are the agent’s implementation checklist.
            """);

        yield return T("documentation.finalize", "Finalize SSOT",
            "Validates schema + end-to-end readiness for AI agent handoff.",
            """
            Stronger than Tools → Validate. Finalize checks:

            • All required profile fields / enums
            • Architecture layers or modules
            • UI screens+controls (unless UI Framework is None)
            • Domain entities with fields
            • Workflows with steps (and triggers when UI controls exist)
            • Build targets with RID
            • Consistency warnings (e.g. Playwright on WPF)

            Always shows a Ready / Not Ready dialog. On Ready: Save/Export JSON and give the agent the file plus docs/SSOT_CONTRACT.md.
            """);

        yield return T("tools.validate", "Validate",
            "Checks required fields and basic consistency warnings.",
            """
            Runs schema validation only (required fields, enums, nested required item fields, workflow trigger warnings).

            On success it may only update the status bar (easy to miss). For agent handoff readiness, use Documentation → Finalize SSOT instead.
            """);

        yield return T("tools.copyPowershell", "Copy PowerShell Command",
            "Copies a sample PowerShell 7+ module command to the clipboard.",
            """
            Useful if you use the CoreAiArchitect PowerShell module to test/export SSOT outside the GUI. Requires PowerShell 7+.
            """);

        yield return T("file.new", "New App Blueprint",
            "Start a new App Blueprint from scratch.",
            """
            Clears the wizard and begins at Ad-Lib. Pick a type, fill in the blanks, and optionally start from a template.
            """);

        yield return T("file.open", "Open App Blueprint",
            "Open an existing App Blueprint (.ssot.json / JSON).",
            """
            Loads screens and settings so you can continue editing in the Screens step.
            """);

        yield return T("file.save", "Save App Blueprint",
            "Save the current App Blueprint to disk.",
            """
            Writes the JSON file an AI coding agent can use to build your app.
            """);

        yield return T("file.exportJson", "Export for AI agent",
            "Export the App Blueprint JSON for an AI coding agent.",
            """
            Same blueprint content as Save, offered through an export dialog.
            """);

        yield return T("file.exportAgentPackage", "Export Agent Package",
            "Export a folder with project.json, AGENTS.md, specifications, and tasks.",
            """
            Creates a complete development contract for coding agents (Cursor, Codex, Claude Code, etc.).

            Includes:
            • project.json / .ssot.json — canonical blueprint
            • AGENTS.md — how to interpret decision modes and scaffolds
            • specifications/ — requirements, architecture, UI, data, testing
            • tasks/ — phased build plan
            • assets/ — place for icons and reference images
            """);

        yield return T("file.exportMarkdown", "Export Markdown",
            "Export a Markdown summary of the blueprint.",
            """
            Empty sections are omitted. Useful for reviews; agents should still prefer the JSON blueprint.
            """);

        // Common nested relative keys used as fallbacks
        yield return T("name", "Name",
            "Name of the current item.",
            "Provide a clear, stable name for this item (module, entity, field, target, etc.). See the parent section’s help for context-specific examples.");

        yield return T("description", "Description",
            "Optional description of the current item.",
            "Short guidance for the coding agent about what this item is for.");

        yield return T("type", "Type",
            "Type of the current item.",
            "Choose from the allowed enum values for this field (control type, field type, storage type, etc.).");

        yield return T("id", "Id",
            "Stable identifier.",
            "Machine-friendly key used in code and cross-references. Prefer no spaces.");
    }

    private static HelpTopic T(string key, string title, string summary, string details) => new()
    {
        Key = key,
        Title = title,
        Summary = summary.Trim(),
        Details = details.Replace("\r\n", "\n").Trim()
    };
}
