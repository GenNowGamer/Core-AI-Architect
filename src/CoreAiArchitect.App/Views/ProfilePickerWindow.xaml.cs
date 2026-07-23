using System.Windows;
using System.Windows.Controls;

namespace CoreAiArchitect.App.Views;

public partial class ProfilePickerWindow : Window
{
    public ProfilePickerWindow(IReadOnlyList<(string Id, string Label)> profiles)
    {
        InitializeComponent();
        ProfileList.ItemsSource = profiles.Select(p => new ProfileItem(p.Id, p.Label)).ToList();
        if (ProfileList.Items.Count > 0)
            ProfileList.SelectedIndex = 0;
    }

    public string? SelectedProfileId { get; private set; }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileList.SelectedItem is ProfileItem item)
        {
            SelectedProfileId = item.Id;
            DialogResult = true;
            Close();
            return;
        }

        MessageBox.Show(this, "Select a profile.", "Core AI Architect");
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ProfileList_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => Ok_Click(sender, e);

    private sealed record ProfileItem(string Id, string Label)
    {
        public override string ToString() => Label;
    }
}
