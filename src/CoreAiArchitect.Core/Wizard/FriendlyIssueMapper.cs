using CoreAiArchitect.Core.Validation;

namespace CoreAiArchitect.Core.Wizard;

/// <summary>Turns technical validation paths into plain-language messages.</summary>
public static class FriendlyIssueMapper
{
    public static string ToFriendlyMessage(ValidationIssue issue)
    {
        var path = issue.Path ?? string.Empty;
        var message = issue.Message ?? string.Empty;

        if (path.Contains("workflows", StringComparison.OrdinalIgnoreCase)
            && path.Contains("steps", StringComparison.OrdinalIgnoreCase))
        {
            var name = ExtractQuotedName(message) ?? "This workflow";
            return $"{name} needs at least one step describing what should happen.";
        }

        if (path.Contains("workflows", StringComparison.OrdinalIgnoreCase)
            && path.Contains("trigger", StringComparison.OrdinalIgnoreCase))
        {
            return message.Contains("does not match", StringComparison.OrdinalIgnoreCase)
                ? message.Replace("trigger '", "button/control “", StringComparison.OrdinalIgnoreCase)
                    .Replace("' does not match a known UI control id.", "” is not one of your screen buttons. Choose a button from the list.", StringComparison.OrdinalIgnoreCase)
                : "Each workflow needs a starting button/control when your app has a screen.";
        }

        if (path.Contains("domain.entities", StringComparison.OrdinalIgnoreCase)
            && path.Contains("fields", StringComparison.OrdinalIgnoreCase))
        {
            var name = ExtractQuotedName(message) ?? "This data object";
            return $"{name} needs at least one piece of information (a field), like Name or Date.";
        }

        if (path.Equals("domain.entities", StringComparison.OrdinalIgnoreCase))
            return "Add at least one data object that describes information your app stores or uses.";

        if (path.Equals("ui.screens", StringComparison.OrdinalIgnoreCase)
            || path.Contains("ui.screens", StringComparison.OrdinalIgnoreCase) && path.Contains("controls", StringComparison.OrdinalIgnoreCase))
            return "Add at least one screen with a button or input so the app has something to show.";

        if (path.Equals("workflows", StringComparison.OrdinalIgnoreCase))
            return "Add at least one workflow (a sequence of steps) so the agent knows what happens when people use the app.";

        if (path.Equals("build.targets", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("build.targets", StringComparison.OrdinalIgnoreCase))
            return "Add a build target such as win-x64 so the agent knows what kind of computer to build for.";

        if (path.Equals("architecture", StringComparison.OrdinalIgnoreCase))
            return "Add at least one layer or module so the agent knows how to organize the code.";

        if (message.Contains("is required", StringComparison.OrdinalIgnoreCase))
            return $"Please fill in: {HumanizePath(path)}. {message}";

        return string.IsNullOrWhiteSpace(message) ? HumanizePath(path) : message;
    }

    public static string FormatReport(ValidationResult result, bool includeTechnicalDetails = false)
    {
        if (result.IsValid && !result.Warnings.Any())
            return "Everything looks ready. You can save and hand this file to an AI coding agent.";

        var lines = new List<string>();
        if (!result.IsValid)
            lines.Add("Not ready yet. Please fix these items:");
        else
            lines.Add("Ready, with a few notes:");

        foreach (var issue in result.Issues.Take(25))
        {
            var friendly = ToFriendlyMessage(issue);
            lines.Add($"• {friendly}");
            if (includeTechnicalDetails)
                lines.Add($"    (details: {issue.Severity} / {issue.Path})");
        }

        if (result.Issues.Count > 25)
            lines.Add($"• …and {result.Issues.Count - 25} more.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string? ExtractQuotedName(string message)
    {
        var start = message.IndexOf('\'');
        var end = message.LastIndexOf('\'');
        if (start >= 0 && end > start)
            return $"“{message[(start + 1)..end]}”";
        return null;
    }

    private static string HumanizePath(string path) => path switch
    {
        "project.name" => "App name",
        "project.id" => "App id",
        "project.description" => "Description",
        "architecture.pattern" => "Architecture style",
        "environment.uiFramework" => "App type / UI",
        _ => path.Replace('.', ' ')
    };
}
