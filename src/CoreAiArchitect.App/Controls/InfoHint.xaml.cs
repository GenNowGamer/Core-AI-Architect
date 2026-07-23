using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CoreAiArchitect.App.Help;

namespace CoreAiArchitect.App.Controls;

public partial class InfoHint : UserControl
{
    public static readonly DependencyProperty HelpKeyProperty =
        DependencyProperty.Register(
            nameof(HelpKey),
            typeof(string),
            typeof(InfoHint),
            new PropertyMetadata(null, OnHelpKeyChanged));

    public InfoHint()
    {
        InitializeComponent();
        UpdateTooltip();
    }

    public string? HelpKey
    {
        get => (string?)GetValue(HelpKeyProperty);
        set => SetValue(HelpKeyProperty, value);
    }

    private static void OnHelpKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfoHint hint)
            hint.UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        var topic = HelpCatalog.Get(HelpKey);
        InfoButton.ToolTip = topic.Summary + "\n\nClick for full details.";
    }

    private void InfoButton_OnClick(object sender, RoutedEventArgs e)
    {
        HelpBroker.Show(HelpKey);
        e.Handled = true;
    }

    private void InfoButton_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            HelpBroker.Show(HelpKey);
            e.Handled = true;
        }
    }
}
