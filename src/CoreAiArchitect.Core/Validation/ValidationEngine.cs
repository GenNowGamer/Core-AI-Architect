using System.Globalization;
using CoreAiArchitect.Core.Models.FormSchema;
using CoreAiArchitect.Core.Models.Ssot;
using CoreAiArchitect.Core.SchemaEngine;

namespace CoreAiArchitect.Core.Validation;

public sealed class ValidationResult
{
    public List<ValidationIssue> Issues { get; } = new();
    public bool IsValid => !Errors.Any();
    public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == ValidationSeverity.Error);
    public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == ValidationSeverity.Warning);
}

public enum ValidationSeverity
{
    Error,
    Warning
}

public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; init; }
    public string Path { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class ValidationEngine
{
    public ValidationResult Validate(SsotDocument document, FormSchemaDefinition schema)
    {
        var result = new ValidationResult();

        foreach (var section in schema.Sections)
        {
            if (section.ShowIf is not null &&
                !PathBinder.EvaluateShowIf(document, section.ShowIf.Path, section.ShowIf.EqualsValue))
                continue;

            foreach (var field in section.Fields)
            {
                if (field.ShowIf is not null &&
                    !PathBinder.EvaluateShowIf(document, field.ShowIf.Path, field.ShowIf.EqualsValue))
                    continue;

                ValidateField(document, field, result);
            }
        }

        ValidateWorkflowTriggers(document, result);
        return result;
    }

    private static void ValidateField(SsotDocument document, FieldDefinition field, ValidationResult result)
    {
        var value = PathBinder.GetValue(document, field.Path);

        if (field.Required && IsEmpty(value))
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Path = field.Path,
                Message = $"{field.Label} is required."
            });
            return;
        }

        if (IsEmpty(value)) return;

        switch (field.Type.ToLowerInvariant())
        {
            case "enum" when field.Values is { Count: > 0 }:
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (!field.Values.Contains(text, StringComparer.OrdinalIgnoreCase))
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Path = field.Path,
                        Message = $"{field.Label} must be one of: {string.Join(", ", field.Values)}."
                    });
                }
                break;

            case "boolean" when value is not bool:
                result.Issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Path = field.Path,
                    Message = $"{field.Label} must be a boolean."
                });
                break;

            case "number" when value is not double and not int and not float and not decimal:
                if (!double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out _))
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Path = field.Path,
                        Message = $"{field.Label} must be a number."
                    });
                }
                break;

            case "objectlist" when field.ItemSchema is { Count: > 0 } && value is System.Collections.IEnumerable items:
                var index = 0;
                foreach (var item in items)
                {
                    if (item is null)
                    {
                        index++;
                        continue;
                    }

                    foreach (var sub in field.ItemSchema.Where(s => s.Required))
                    {
                        // Item-schema paths are relative to the item (e.g. "name" or "errorHandling.strategy").
                        var subValue = PathBinder.GetValue(item, sub.Path);
                        if (IsEmpty(subValue))
                        {
                            result.Issues.Add(new ValidationIssue
                            {
                                Severity = ValidationSeverity.Error,
                                Path = $"{field.Path}[{index}].{sub.Path}",
                                Message = $"{field.Label} item {index + 1}: {sub.Label} is required."
                            });
                        }
                    }

                    // Validate nested enums on item schema when present.
                    foreach (var sub in field.ItemSchema.Where(s =>
                                 s.Type.Equals("enum", StringComparison.OrdinalIgnoreCase) && s.Values is { Count: > 0 }))
                    {
                        var subValue = PathBinder.GetValue(item, sub.Path);
                        if (IsEmpty(subValue)) continue;
                        var enumText = Convert.ToString(subValue, CultureInfo.InvariantCulture) ?? string.Empty;
                        if (!sub.Values!.Contains(enumText, StringComparer.OrdinalIgnoreCase))
                        {
                            result.Issues.Add(new ValidationIssue
                            {
                                Severity = ValidationSeverity.Error,
                                Path = $"{field.Path}[{index}].{sub.Path}",
                                Message = $"{field.Label} item {index + 1}: {sub.Label} must be one of: {string.Join(", ", sub.Values!)}."
                            });
                        }
                    }

                    index++;
                }
                break;
        }
    }

    private static void ValidateWorkflowTriggers(SsotDocument document, ValidationResult result)
    {
        var controlIds = document.Ui.Screens
            .SelectMany(s => s.Controls)
            .Select(c => c.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (controlIds.Count == 0) return;

        foreach (var workflow in document.Workflows)
        {
            if (string.IsNullOrWhiteSpace(workflow.Trigger)) continue;

            var triggerId = workflow.Trigger;
            if (triggerId.StartsWith("UI:", StringComparison.OrdinalIgnoreCase))
                triggerId = triggerId[3..];

            if (!controlIds.Contains(triggerId))
            {
                result.Issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Path = $"workflows[{document.Workflows.IndexOf(workflow)}].trigger",
                    Message = $"Workflow '{workflow.Name}' trigger '{workflow.Trigger}' does not match a known UI control id."
                });
            }
        }
    }

    private static bool IsEmpty(object? value) =>
        value is null
        || value is string s && string.IsNullOrWhiteSpace(s)
        || value is System.Collections.ICollection c && c.Count == 0;
}
