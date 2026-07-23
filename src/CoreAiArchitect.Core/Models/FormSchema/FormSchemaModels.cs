using System.Text.Json.Serialization;

namespace CoreAiArchitect.Core.Models.FormSchema;

public sealed class FormSchemaDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string TargetSsotVersion { get; set; } = "1.0.0";
    public string Label { get; set; } = string.Empty;
    public EnvironmentDefaults EnvironmentDefaults { get; set; } = new();
    public List<SectionDefinition> Sections { get; set; } = new();
}

public sealed class EnvironmentDefaults
{
    public string TargetOs { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;
    public string UiFramework { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public sealed class SectionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Editor { get; set; } = "default";
    public ShowIfCondition? ShowIf { get; set; }
    public List<FieldDefinition> Fields { get; set; } = new();
}

public sealed class FieldDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public bool Required { get; set; }
    public List<string>? Values { get; set; }
    public object? Default { get; set; }
    public ShowIfCondition? ShowIf { get; set; }
    public List<FieldDefinition>? ItemSchema { get; set; }
}

public sealed class ShowIfCondition
{
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("equals")]
    public string EqualsValue { get; set; } = string.Empty;
}
