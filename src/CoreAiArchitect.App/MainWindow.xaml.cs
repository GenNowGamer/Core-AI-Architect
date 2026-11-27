using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using CoreAiArchitect.App.Services;
using CoreAiArchitect.App.ViewModels;

namespace CoreAiArchitect.App;

public partial class MainWindow : Window
{
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public MainWindow()
    {
        InitializeComponent();
        var schemas = new SchemaCatalogService();
        var dialogs = new DialogService();
        var documents = new SsotDocumentService();
        var vm = new MainWindowViewModel(schemas, dialogs, documents);
        DataContext = vm;

        SourceInitialized += (_, _) => ApplyImmersiveDarkMode(this, vm.IsDarkMode);
        RestoreWindowBounds(vm);
        Closing += (_, _) => SaveWindowBounds(vm);
    }

    public static void ApplyImmersiveDarkMode(Window window, bool enabled)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            var useDark = enabled ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref useDark, sizeof(int));
        }
        catch
        {
            // Non-fatal on older Windows versions
        }
    }

    private void RestoreWindowBounds(MainWindowViewModel vm)
    {
        var s = vm.GetUserSettings();
        if (s.WindowWidth is >= 900)
            Width = Math.Min(s.WindowWidth.Value, SystemParameters.VirtualScreenWidth);
        if (s.WindowHeight is >= 600)
            Height = Math.Min(s.WindowHeight.Value, SystemParameters.VirtualScreenHeight);
        if (s.WindowLeft.HasValue && s.WindowTop.HasValue)
        {
            var left = s.WindowLeft.Value;
            var top = s.WindowTop.Value;
            if (left >= SystemParameters.VirtualScreenLeft - 50 &&
                left <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100 &&
                top >= SystemParameters.VirtualScreenTop - 50 &&
                top <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = left;
                Top = top;
            }
        }
        if (s.IsMaximized)
            WindowState = WindowState.Maximized;
    }

    private void SaveWindowBounds(MainWindowViewModel vm)
    {
        var s = vm.GetUserSettings();
        s.IsMaximized = WindowState == WindowState.Maximized;
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (bounds.Width >= 900 && bounds.Height >= 600)
        {
            s.WindowWidth = bounds.Width;
            s.WindowHeight = bounds.Height;
            s.WindowLeft = bounds.Left;
            s.WindowTop = bounds.Top;
        }
        vm.SaveUserSettings();
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

    private void WireframeField_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && IsLoaded)
            vm.RefreshWireframe();
    }

    private void WireframeField_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && IsLoaded)
            vm.RefreshWireframe();
    }

    private void HubSection_OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.SelectedHubSection is null)
            return;
        if (vm.JumpToHubSectionCommand.CanExecute(vm.SelectedHubSection))
            vm.JumpToHubSectionCommand.Execute(vm.SelectedHubSection);
    }
}
