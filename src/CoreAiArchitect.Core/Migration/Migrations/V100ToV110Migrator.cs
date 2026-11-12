using System.Text.Json.Nodes;

namespace CoreAiArchitect.Core.Migration.Migrations;

public sealed class V100ToV110Migrator : ISsotMigrator
{
    public string SourceVersion => "1.0.0";
    public string TargetVersion => "1.1.0";

    public void Migrate(JsonObject root)
    {
        // 1. Explicit profileId
        if (!root.ContainsKey("profileId") || root["profileId"] is null)
        {
            var profileId = "wpf_desktop";
            if (root["environment"] is JsonObject env)
            {
                var targetOs = env["targetOs"]?.ToString() ?? "";
                var uiFramework = env["uiFramework"]?.ToString() ?? "";
                if (targetOs.Equals("Android", StringComparison.OrdinalIgnoreCase)
                    || uiFramework.Equals("Jetpack Compose", StringComparison.OrdinalIgnoreCase))
                {
                    profileId = "android_app";
                }
                else if (uiFramework.Equals("ASP.NET", StringComparison.OrdinalIgnoreCase))
                {
                    profileId = "web_api";
                }
            }
            root["profileId"] = profileId;
        }

        // 2. Multi-window / modal screenType
        if (root["ui"] is JsonObject ui && ui["screens"] is JsonArray screens)
        {
            var isAndroid = string.Equals(root["profileId"]?.ToString(), "android_app", StringComparison.OrdinalIgnoreCase);
            for (var i = 0; i < screens.Count; i++)
            {
                if (screens[i] is JsonObject screen)
                {
                    if (!screen.ContainsKey("screenType") || screen["screenType"] is null)
                    {
                        var id = screen["id"]?.ToString() ?? "";
                        var isMain = i == 0 || id.Equals("main", StringComparison.OrdinalIgnoreCase);
                        screen["screenType"] = isMain
                            ? (isAndroid ? "Screen" : "Window")
                            : (isAndroid ? "Screen" : "Dialog");
                    }
                }
            }
        }

        // 3. Update version string
        root["ssotVersion"] = TargetVersion;
    }
}

