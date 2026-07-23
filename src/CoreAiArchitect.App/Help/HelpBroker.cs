using System.Windows;

namespace CoreAiArchitect.App.Help;

public static class HelpBroker
{
    public static void Show(string? key)
    {
        var topic = HelpCatalog.Get(key);
        var owner = Application.Current?.MainWindow;
        var window = new Views.HelpTopicWindow(topic)
        {
            Owner = owner
        };
        window.ShowDialog();
    }
}
