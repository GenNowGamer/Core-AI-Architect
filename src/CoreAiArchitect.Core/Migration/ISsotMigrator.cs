using System.Text.Json.Nodes;

namespace CoreAiArchitect.Core.Migration;

public interface ISsotMigrator
{
    string SourceVersion { get; }
    string TargetVersion { get; }
    void Migrate(JsonObject root);
}

