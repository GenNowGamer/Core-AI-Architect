using System.Windows;
using CoreAiArchitect.App.Help;

namespace CoreAiArchitect.App.Views;

public partial class HelpTopicWindow : Window
{
    public HelpTopicWindow(HelpTopic topic)
    {
        InitializeComponent();
        Title = $"Help — {topic.Title}";
        TitleText.Text = topic.Title;
        SummaryText.Text = topic.Summary;
        DetailsText.Text = topic.Details;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
