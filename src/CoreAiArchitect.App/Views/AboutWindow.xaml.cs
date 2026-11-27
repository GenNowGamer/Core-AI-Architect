using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow(string appVersion, bool isDarkMode)
    {
        InitializeComponent();
        VersionBadgeText.Text = $"v{appVersion}";
        AppVersionText.Text = appVersion;
        SchemaVersionText.Text = SsotDocument.CurrentSchemaVersion;
        SourceInitialized += (_, _) => MainWindow.ApplyImmersiveDarkMode(this, isDarkMode);
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch
        {
            // Ignore browser launch failures
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
