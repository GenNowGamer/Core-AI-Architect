using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CoreAiArchitect.App.Services;
using CoreAiArchitect.App.ViewModels;

namespace CoreAiArchitect.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var schemas = new SchemaCatalogService();
        var dialogs = new DialogService();
        var documents = new SsotDocumentService();
        DataContext = new MainWindowViewModel(schemas, dialogs, documents);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void AppKindCard_OnClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: AppKindOption option }
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelectedAppKind = option;
        }
    }

    private void TemplateCard_OnClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: BlueprintTemplateOption option }
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelectedTemplate = option;
        }
    }

    private void Description_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.RefreshTipsCommand.Execute(null);
    }

    private void Review_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && IsLoaded)
            vm.MarkReviewEditedCommand.Execute(null);
    }
}
