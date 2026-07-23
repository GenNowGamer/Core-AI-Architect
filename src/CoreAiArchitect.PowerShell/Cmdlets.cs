using System.Management.Automation;
using CoreAiArchitect.Core.Documents;
using CoreAiArchitect.Core.Export;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.SchemaEngine;
using CoreAiArchitect.Core.Validation;

namespace CoreAiArchitect.PowerShell;

internal static class ModulePaths
{
    public static string ResolveSchemasDirectory()
    {
        var baseDir = Path.GetDirectoryName(typeof(ModulePaths).Assembly.Location) ?? AppContext.BaseDirectory;
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

        return Path.Combine(baseDir, "schemas");
    }

    public static SchemaCatalog LoadCatalog()
    {
        var catalog = new SchemaCatalog();
        catalog.LoadFromDirectory(ResolveSchemasDirectory());
        return catalog;
    }
}

[Cmdlet(VerbsCommon.Get, "SsotFormProfile")]
[OutputType(typeof(FormSchemaDefinition))]
public sealed class GetSsotFormProfileCommand : Cmdlet
{
    [Parameter(Position = 0)]
    public string? Name { get; set; }

    protected override void ProcessRecord()
    {
        var catalog = ModulePaths.LoadCatalog();
        if (string.IsNullOrWhiteSpace(Name))
        {
            WriteObject(catalog.Profiles, true);
            return;
        }

        WriteObject(catalog.RequireById(Name));
    }
}

[Cmdlet(VerbsCommon.New, "SsotDocument")]
[OutputType(typeof(SsotDocument))]
public sealed class NewSsotDocumentCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string Profile { get; set; } = string.Empty;

    [Parameter]
    public string? OutFile { get; set; }

    protected override void ProcessRecord()
    {
        var catalog = ModulePaths.LoadCatalog();
        var profile = catalog.RequireById(Profile);
        var document = SsotDocumentNormalizer.Normalize(SsotDocumentFactory.CreateNew(profile));
        if (!string.IsNullOrWhiteSpace(OutFile))
            new JsonExporter().ExportToFile(document, OutFile);
        WriteObject(document);
    }
}

[Cmdlet(VerbsDiagnostic.Test, "SsotDocument")]
[OutputType(typeof(ValidationResult))]
public sealed class TestSsotDocumentCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; } = string.Empty;

    [Parameter]
    public string Profile { get; set; } = "custom";

    protected override void ProcessRecord()
    {
        var catalog = ModulePaths.LoadCatalog();
        var profile = catalog.RequireById(Profile);
        var document = SsotDocumentNormalizer.Normalize(new JsonExporter().LoadFromFile(Path));
        var result = new ValidationEngine().Validate(document, profile);
        WriteObject(result);
        if (!result.IsValid)
            WriteWarning($"Validation failed with {result.Errors.Count()} error(s).");
    }
}

[Cmdlet(VerbsData.Export, "SsotMarkdown")]
public sealed class ExportSsotMarkdownCommand : Cmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string Path { get; set; } = string.Empty;

    [Parameter(Mandatory = true)]
    public string OutFile { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var document = SsotDocumentNormalizer.Normalize(new JsonExporter().LoadFromFile(Path));
        new MarkdownExporter().ExportToFile(document, OutFile);
        WriteObject(OutFile);
    }
}

[Cmdlet(VerbsData.ConvertTo, "SsotJson")]
[OutputType(typeof(string))]
public sealed class ConvertToSsotJsonCommand : Cmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public SsotDocument Document { get; set; } = new();

    [Parameter]
    public string? OutFile { get; set; }

    protected override void ProcessRecord()
    {
        var exporter = new JsonExporter();
        if (!string.IsNullOrWhiteSpace(OutFile))
        {
            exporter.ExportToFile(Document, OutFile);
            WriteObject(OutFile);
            return;
        }

        WriteObject(exporter.ToJson(Document));
    }
}
