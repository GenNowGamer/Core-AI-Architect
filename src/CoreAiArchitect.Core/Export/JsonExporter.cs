using System.Text.Json;
using CoreAiArchitect.Core.Json;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Export;

public sealed class JsonExporter
{
    private readonly JsonSerializerOptions _options = SsotJsonOptions.Create();

    public string ToJson(SsotDocument document) =>
        JsonSerializer.Serialize(document, _options);

    public void ExportToFile(SsotDocument document, string path)
    {
        var json = ToJson(document);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, json);
    }

    public SsotDocument LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SsotDocument>(json, _options)
               ?? throw new InvalidOperationException("SSOT JSON deserialized to null.");
    }

    public SsotDocument LoadFromJson(string json) =>
        JsonSerializer.Deserialize<SsotDocument>(json, _options)
        ?? throw new InvalidOperationException("SSOT JSON deserialized to null.");
}
