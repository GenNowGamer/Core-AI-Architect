using System.Text.Json;
using System.Text.Json.Nodes;
using CoreAiArchitect.Core.Migration.Migrations;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.Core.Migration;

public sealed class SsotDocumentMigrator
{
    private static readonly List<ISsotMigrator> RegisteredMigrators = new()
    {
        new V100ToV110Migrator()
    };

    public static string MigrateJson(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            return jsonText;

        var node = JsonNode.Parse(jsonText);
        if (node is not JsonObject root)
            return jsonText;

        var currentVersion = root["ssotVersion"]?.ToString();
        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            currentVersion = "1.0.0";
            root["ssotVersion"] = currentVersion;
        }

        var changed = false;
        while (true)
        {
            var migrator = RegisteredMigrators.FirstOrDefault(m =>
                string.Equals(m.SourceVersion, currentVersion, StringComparison.OrdinalIgnoreCase));
            if (migrator is null)
                break;

            migrator.Migrate(root);
            currentVersion = migrator.TargetVersion;
            changed = true;
        }

        return changed
            ? root.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
            : jsonText;
    }

    public static SsotDocument Migrate(SsotDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.SsotVersion) || document.SsotVersion == "1.0.0")
        {
            if (string.IsNullOrWhiteSpace(document.ProfileId))
            {
                document.ProfileId = document.Environment.TargetOs.Equals("Android", StringComparison.OrdinalIgnoreCase)
                    ? "android_app"
                    : "wpf_desktop";
            }

            foreach (var screen in document.Ui.Screens)
            {
                if (string.IsNullOrWhiteSpace(screen.ScreenType))
                {
                    screen.ScreenType = screen.Id.Equals("main", StringComparison.OrdinalIgnoreCase)
                        ? (document.ProfileId == "android_app" ? "Screen" : "Window")
                        : (document.ProfileId == "android_app" ? "Screen" : "Dialog");
                }
            }

            document.SsotVersion = SsotDocument.CurrentSchemaVersion;
        }

        return document;
    }
}

