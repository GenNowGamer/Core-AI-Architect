namespace CoreAiArchitect.App.Services;

public interface IDialogService
{
    string? OpenFile(string filter, string title = "Open");
    string? SaveFile(string filter, string title = "Save", string? defaultFileName = null);
    string? PickFolder(string title = "Select folder");
    void ShowMessage(string message, string title = "Core AI Architect");
    void ShowMessage(string message, string title, bool isError);
    bool Confirm(string message, string title = "Confirm");
    string? PickProfile(IEnumerable<(string Id, string Label)> profiles);
}

public sealed class DialogService : IDialogService
{
    public string? OpenFile(string filter, string title = "Open")
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            Title = title,
            CheckFileExists = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveFile(string filter, string title = "Save", string? defaultFileName = null)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            Title = title,
            FileName = defaultFileName ?? string.Empty,
            AddExtension = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickFolder(string title = "Select folder")
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public void ShowMessage(string message, string title = "Core AI Architect") =>
        ShowMessage(message, title, false);

    public void ShowMessage(string message, string title, bool isError) =>
        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            isError ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);

    public bool Confirm(string message, string title = "Confirm") =>
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
        == System.Windows.MessageBoxResult.Yes;

    public string? PickProfile(IEnumerable<(string Id, string Label)> profiles)
    {
        var window = new Views.ProfilePickerWindow(profiles.ToList())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        return window.ShowDialog() == true ? window.SelectedProfileId : null;
    }
}
