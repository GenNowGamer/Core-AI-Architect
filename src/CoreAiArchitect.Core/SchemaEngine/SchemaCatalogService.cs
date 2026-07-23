using CoreAiArchitect.Core.Models.FormSchema;

namespace CoreAiArchitect.Core.SchemaEngine;

public sealed class SchemaCatalog
{
    private readonly SchemaLoader _loader = new();
    private readonly List<FormSchemaDefinition> _profiles = new();

    public string? SchemasDirectory { get; private set; }

    public IReadOnlyList<FormSchemaDefinition> Profiles => _profiles;

    public void LoadFromDirectory(string directory)
    {
        SchemasDirectory = directory;
        _profiles.Clear();
        _profiles.AddRange(_loader.LoadAllFromDirectory(directory));
    }

    public FormSchemaDefinition? GetById(string id) =>
        _profiles.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public FormSchemaDefinition RequireById(string id) =>
        GetById(id) ?? throw new InvalidOperationException($"Form profile '{id}' was not found.");
}
