using CoreAiArchitect.Core.Documents;
using CoreAiArchitect.Core.Export;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.Validation;

namespace CoreAiArchitect.App.Services;

public interface ISsotDocumentService
{
    SsotDocument? Document { get; }
    FormSchemaDefinition? Profile { get; }
    string? FilePath { get; }
    bool IsDirty { get; set; }
    SsotDocument NewDocument(FormSchemaDefinition profile);
    SsotDocument Open(string path, FormSchemaDefinition? profileHint = null);
    void SetProfile(FormSchemaDefinition profile);
    void ReplaceDocument(SsotDocument document, FormSchemaDefinition profile, string? filePath = null);
    void Save(string path);
    ValidationResult Validate();
    ValidationResult Finalize();
    void ExportJson(string path);
    void ExportMarkdown(string path);
    string GetPowerShellExportCommand();
}

public sealed class SsotDocumentService : ISsotDocumentService
{
    private readonly JsonExporter _jsonExporter = new();
    private readonly MarkdownExporter _markdownExporter = new();
    private readonly ValidationEngine _validationEngine = new();
    private readonly SsotReadinessEngine _readinessEngine = new();

    public SsotDocument? Document { get; private set; }
    public FormSchemaDefinition? Profile { get; private set; }
    public string? FilePath { get; private set; }
    public bool IsDirty { get; set; }

    public SsotDocument NewDocument(FormSchemaDefinition profile)
    {
        Profile = profile;
        Document = SsotDocumentNormalizer.Normalize(SsotDocumentFactory.CreateNew(profile));
        FilePath = null;
        IsDirty = true;
        return Document;
    }

    public SsotDocument Open(string path, FormSchemaDefinition? profileHint = null)
    {
        Document = SsotDocumentNormalizer.Normalize(_jsonExporter.LoadFromFile(path));
        FilePath = path;
        Profile = profileHint;
        IsDirty = false;
        return Document;
    }

    public void SetProfile(FormSchemaDefinition profile) => Profile = profile;

    public void ReplaceDocument(SsotDocument document, FormSchemaDefinition profile, string? filePath = null)
    {
        Document = SsotDocumentNormalizer.Normalize(document);
        Profile = profile;
        FilePath = filePath;
        IsDirty = true;
    }

    public void Save(string path)
    {
        if (Document is null) throw new InvalidOperationException("No document loaded.");
        SsotDocumentNormalizer.Normalize(Document);
        _jsonExporter.ExportToFile(Document, path);
        FilePath = path;
        IsDirty = false;
    }

    public ValidationResult Validate()
    {
        if (Document is null || Profile is null)
            throw new InvalidOperationException("Document and profile are required for validation.");
        SsotDocumentNormalizer.Normalize(Document);
        return _validationEngine.Validate(Document, Profile);
    }

    public ValidationResult Finalize()
    {
        if (Document is null || Profile is null)
            throw new InvalidOperationException("Document and profile are required to finalize.");
        SsotDocumentNormalizer.Normalize(Document);
        return new SsotReadinessEngine().Assess(Document, Profile);
    }

    public void ExportJson(string path)
    {
        if (Document is null) throw new InvalidOperationException("No document loaded.");
        SsotDocumentNormalizer.Normalize(Document);
        _jsonExporter.ExportToFile(Document, path);
    }

    public void ExportMarkdown(string path)
    {
        if (Document is null) throw new InvalidOperationException("No document loaded.");
        SsotDocumentNormalizer.Normalize(Document);
        _markdownExporter.ExportToFile(Document, path);
    }

    public string GetPowerShellExportCommand()
    {
        var path = FilePath ?? "<path-to-ssot.json>";
        return $"Import-Module CoreAiArchitect; Test-SsotDocument -Path '{path}'; Export-SsotMarkdown -Path '{path}' -OutFile ([System.IO.Path]::ChangeExtension('{path}', '.md'))";
    }
}
