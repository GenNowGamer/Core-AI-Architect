namespace CoreAiArchitect.Core.Models;

/// <summary>Local user preferences and recent blueprint history.</summary>
public sealed class UserSettings
{
    public const int MaxRecentFiles = 8;

    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool IsMaximized { get; set; }
    public List<string> RecentFiles { get; set; } = new();

    public void AddRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        RecentFiles.Insert(0, path);
        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
    }
}
