using System.IO;
using System.Text.Json;
using CoreAiArchitect.Core.Json;
using CoreAiArchitect.Core.Models;

namespace CoreAiArchitect.App.Services;

public sealed class UserSettingsService
{
    private readonly string _filePath;

    public UserSettingsService(string? customPath = null)
    {
        _filePath = customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CoreAiArchitect",
            "settings.json");
    }

    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new UserSettings();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<UserSettings>(json, SsotJsonOptions.Create()) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, SsotJsonOptions.Create());
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Non-fatal: ignore settings write failures.
        }
    }
}
