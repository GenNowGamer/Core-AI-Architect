using System.IO;
using CoreAiArchitect.Core.Models.FormSchema;

namespace CoreAiArchitect.App.Services;

public interface ISchemaCatalogService
{
    string SchemasDirectory { get; }
    IReadOnlyList<FormSchemaDefinition> Profiles { get; }
    void EnsureLoaded();
    FormSchemaDefinition RequireById(string id);
}

public sealed class SchemaCatalogService : ISchemaCatalogService
{
    private readonly CoreAiArchitect.Core.SchemaEngine.SchemaCatalog _catalog = new();
    private bool _loaded;

    public string SchemasDirectory { get; private set; } = string.Empty;
    public IReadOnlyList<FormSchemaDefinition> Profiles => _catalog.Profiles;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        SchemasDirectory = ResolveSchemasDirectory();
        _catalog.LoadFromDirectory(SchemasDirectory);
        _loaded = true;
    }

    public FormSchemaDefinition RequireById(string id)
    {
        EnsureLoaded();
        return _catalog.RequireById(id);
    }

    private static string ResolveSchemasDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "schemas"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "schemas")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "schemas"))
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.json").Any())
                return candidate;
        }

        Directory.CreateDirectory(Path.Combine(baseDir, "schemas"));
        return Path.Combine(baseDir, "schemas");
    }
}
