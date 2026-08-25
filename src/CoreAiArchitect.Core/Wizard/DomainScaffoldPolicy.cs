namespace CoreAiArchitect.Core.Wizard;

/// <summary>
/// Product policy: screens are the UI contract; domain entities are generic scaffolds.
/// Rich product rules live in project.description and workflow steps for the coding agent.
/// </summary>
public static class DomainScaffoldPolicy
{
    public const string ScaffoldMarker = "Scaffold only";

    public const string AgentInstruction =
        "Scaffold only. Treat ui.screens as the UI contract. Invent a richer domain model from " +
        "project.description and workflows[].steps (game rules, tickets, numbers, validation, etc.). " +
        "Do not stop at OptionChoice/UserInput/ResultItem if the description implies a fuller model.";

    public const string UserGuidanceShort =
        "Put detailed product rules in your description and in each button’s click steps. " +
        "Screens define what people see; the AI builder invents the real data model from your words.";

    public const string UserGuidanceDescription =
        "Include product rules here (games list, number rules, validation, caching). " +
        "You do not need to design database tables — the AI coding agent builds those from this text and your Generate steps.";

    public const string UserGuidanceClicks =
        "Write the real business steps here (read game + count, apply that game’s rules, show tickets). " +
        "These steps plus your description are what the AI uses to invent ticket/game models — domain entities in the JSON stay generic on purpose.";

    public const string UserGuidanceHandOff =
        "Domain entities in the blueprint are placeholders. Your description and click steps carry the real product rules for the coding agent.";

    public static string AnnotateEntityDescription(string plainDescription) =>
        $"{plainDescription.TrimEnd('.')} — {AgentInstruction}";
}
