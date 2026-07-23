using System.Windows;
using System.Windows.Threading;

namespace CoreAiArchitect.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowError("Unexpected error", e.Exception);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowError("Fatal error", ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ShowError("Background task error", e.Exception);
        e.SetObserved();
    }

    private static void ShowError(string title, Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"{ex.GetType().Name}: {ex.Message}",
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Last-resort: avoid throwing from the error handler itself.
        }
    }
}
