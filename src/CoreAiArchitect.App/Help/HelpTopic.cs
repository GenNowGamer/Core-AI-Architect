namespace CoreAiArchitect.App.Help;

public sealed class HelpTopic
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    /// <summary>Short text for hover tooltip.</summary>
    public required string Summary { get; init; }
    /// <summary>Full explanation shown when the user clicks the info button.</summary>
    public required string Details { get; init; }
}
