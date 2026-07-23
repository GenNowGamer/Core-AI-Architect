using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.SchemaEngine;

public sealed class SchemaInterpreter
{
    public IReadOnlyList<SectionDefinition> GetVisibleSections(FormSchemaDefinition schema, SsotDocument document) =>
        schema.Sections
            .Where(s => s.ShowIf is null || PathBinder.EvaluateShowIf(document, s.ShowIf.Path, s.ShowIf.EqualsValue))
            .ToList();

    public IReadOnlyList<FieldDefinition> GetVisibleFields(SectionDefinition section, SsotDocument document) =>
        section.Fields
            .Where(f => f.ShowIf is null || PathBinder.EvaluateShowIf(document, f.ShowIf.Path, f.ShowIf.EqualsValue))
            .ToList();
}
