using System.Windows.Controls;
using CoreAiArchitect.App.ViewModels;

namespace CoreAiArchitect.App.Views;

public partial class WorkflowBuilderView
{
    public WorkflowBuilderView()
    {
        InitializeComponent();
    }

    private void TriggerPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not WorkflowBuilderViewModel vm) return;
        if (sender is not ComboBox combo) return;
        if (combo.SelectedItem is string trigger)
            vm.ApplyTriggerCommand.Execute(trigger);
    }
}
