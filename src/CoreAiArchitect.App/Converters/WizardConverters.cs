using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
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

/// <summary>
/// Shows content when DetailLevel is at least the level named in ConverterParameter (Simple|Standard|Expert).
/// </summary>
public sealed class DetailLevelVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DetailLevel current || parameter is null)
            return Visibility.Collapsed;

        if (!Enum.TryParse<DetailLevel>(parameter.ToString(), ignoreCase: true, out var minimum))
            return Visibility.Collapsed;

        return current >= minimum ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class HubStatusBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Ready = BrushFrom("#0F3D3E");
    private static readonly SolidColorBrush Attention = BrushFrom("#E85D4C");
    private static readonly SolidColorBrush Optional = BrushFrom("#888888");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value switch
        {
            BlueprintSectionStatus.Ready => Ready,
            BlueprintSectionStatus.Attention => Attention,
            _ => Optional
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static SolidColorBrush BrushFrom(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        brush.Freeze();
        return brush;
    }
}
