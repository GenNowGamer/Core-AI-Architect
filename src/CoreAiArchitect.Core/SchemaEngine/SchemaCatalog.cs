using System.Text.Json;
using CoreAiArchitect.Core.Json;
using CoreAiArchitect.Core.Models.FormSchema;

namespace CoreAiArchitect.Core.SchemaEngine;

public sealed class SchemaLoader
{
    private readonly JsonSerializerOptions _options = SsotJsonOptions.Create();

    public FormSchemaDefinition LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Form schema file not found.", path);

        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    public FormSchemaDefinition LoadFromJson(string json)
    {
        var schema = JsonSerializer.Deserialize<FormSchemaDefinition>(json, _options)
                     ?? throw new InvalidOperationException("Form schema JSON deserialized to null.");
        if (string.IsNullOrWhiteSpace(schema.Id))
            throw new InvalidOperationException("Form schema must include an id.");
        return schema;
    }

    public IReadOnlyList<FormSchemaDefinition> LoadAllFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<FormSchemaDefinition>();

        return Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToList();
    }
}
