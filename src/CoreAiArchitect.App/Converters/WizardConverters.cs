using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CoreAiArchitect.Core.Wizard;

namespace CoreAiArchitect.App.Converters;

public sealed class WizardStepVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not WizardStep current || parameter is null)
            return Visibility.Collapsed;

        var targetName = parameter.ToString() ?? string.Empty;
        if (!Enum.TryParse<WizardStep>(targetName, ignoreCase: true, out var target))
            return Visibility.Collapsed;

        return current == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
