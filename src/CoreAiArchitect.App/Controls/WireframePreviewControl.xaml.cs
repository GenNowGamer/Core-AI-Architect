using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CoreAiArchitect.Core.Models.Ssot;

namespace CoreAiArchitect.App.Controls;

public partial class WireframePreviewControl : UserControl
{
    public static readonly DependencyProperty ScreenProperty =
        DependencyProperty.Register(
            nameof(Screen),
            typeof(ScreenInfo),
            typeof(WireframePreviewControl),
            new PropertyMetadata(null, OnScreenChanged));

    public static readonly DependencyProperty ProfileIdProperty =
        DependencyProperty.Register(
            nameof(ProfileId),
            typeof(string),
            typeof(WireframePreviewControl),
            new PropertyMetadata("wpf_desktop", OnProfileChanged));

    public ScreenInfo? Screen
    {
        get => (ScreenInfo?)GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }

    public string ProfileId
    {
        get => (string)GetValue(ProfileIdProperty);
        set => SetValue(ProfileIdProperty, value);
    }

    public WireframePreviewControl()
    {
        InitializeComponent();
    }

    private static void OnScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WireframePreviewControl control)
            control.Refresh();
    }

    private static void OnProfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WireframePreviewControl control)
            control.Refresh();
    }

    public void Refresh()
    {
        var screen = Screen;
        var profile = ProfileId ?? "wpf_desktop";
        var isAndroid = profile.Equals("android_app", StringComparison.OrdinalIgnoreCase);

        // Frame styling
        if (isAndroid)
        {
            WindowGlyph.Text = "📱";
            WindowButtons.Visibility = Visibility.Collapsed;
            TitleBar.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x50, 0x47));
            ScreenTitleText.Foreground = Brushes.White;
            ScreenTypeBadge.Background = new SolidColorBrush(Color.FromRgb(0x42, 0x73, 0x67));
            ScreenTypeText.Foreground = Brushes.White;
        }
        else
        {
            WindowGlyph.Text = "🗖";
            WindowButtons.Visibility = Visibility.Visible;
            TitleBar.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE2, 0xD8));
            ScreenTitleText.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            ScreenTypeBadge.Background = new SolidColorBrush(Color.FromRgb(0xD5, 0xCB, 0xBF));
            ScreenTypeText.Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
        }

        ScreenTitleText.Text = string.IsNullOrWhiteSpace(screen?.Title) ? "Untitled Screen" : screen.Title;
        ScreenTypeText.Text = string.IsNullOrWhiteSpace(screen?.ScreenType)
            ? (isAndroid ? "Screen" : "Window")
            : screen.ScreenType;

        ControlsContainer.Children.Clear();

        if (screen is null || screen.Controls.Count == 0)
        {
            ControlsContainer.Children.Add(EmptyHint);
            return;
        }

        foreach (var c in screen.Controls)
        {
            var widget = CreateControlWidget(c, isAndroid);
            ControlsContainer.Children.Add(widget);
        }
    }

    private FrameworkElement CreateControlWidget(ControlInfo control, bool isAndroid)
    {
        var type = control.Type ?? "Button";
        var label = string.IsNullOrWhiteSpace(control.Label) ? control.Id : control.Label;

        var container = new Border
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(10),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xDF, 0xD8, 0xCE)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6)
        };

        var stack = new StackPanel();

        if (IsButtonType(type))
        {
            var btn = new Border
            {
                Background = isAndroid ? new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x3E)) : new SolidColorBrush(Color.FromRgb(0xE2, 0x6D, 0x5C)),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14, 8, 14, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btn.Child = new TextBlock
            {
                Text = label,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(btn);
        }
        else if (IsChoiceType(type))
        {
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            var dropBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xC4, 0xB8)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF5, 0xF0))
            };
            var dropGrid = new Grid();
            dropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txt = new TextBlock { Text = "Pick an option...", FontSize = 12, Foreground = Brushes.Gray };
            var arrow = new TextBlock { Text = "▾", FontSize = 12, Foreground = Brushes.Gray };
            Grid.SetColumn(arrow, 1);
            dropGrid.Children.Add(txt);
            dropGrid.Children.Add(arrow);
            dropBorder.Child = dropGrid;
            stack.Children.Add(dropBorder);
        }
        else if (IsListType(type))
        {
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            var listBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xC4, 0xB8)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6),
                Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xF8)),
                MinHeight = 50
            };
            var listStack = new StackPanel();
            listStack.Children.Add(new TextBlock { Text = "• Sample item 1", FontSize = 11, Foreground = Brushes.DimGray, Margin = new Thickness(0, 2, 0, 2) });
            listStack.Children.Add(new TextBlock { Text = "• Sample item 2", FontSize = 11, Foreground = Brushes.DimGray, Margin = new Thickness(0, 2, 0, 2) });
            listBorder.Child = listStack;
            stack.Children.Add(listBorder);
        }
        else
        {
            // Text box / place to type
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            var boxBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xC4, 0xB8)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Background = Brushes.White
            };
            boxBorder.Child = new TextBlock { Text = "Enter text or number...", FontSize = 12, Foreground = Brushes.LightGray };
            stack.Children.Add(boxBorder);
        }

        container.Child = stack;
        return container;
    }

    private static bool IsButtonType(string type) =>
        type.Equals("Button", StringComparison.OrdinalIgnoreCase)
        || type.Equals("FloatingActionButton", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Command", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Cmdlet", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Endpoint", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Stage", StringComparison.OrdinalIgnoreCase);

    private static bool IsChoiceType(string type) =>
        type.Equals("ComboBox", StringComparison.OrdinalIgnoreCase)
        || type.Equals("DropdownMenu", StringComparison.OrdinalIgnoreCase)
        || type.Equals("ExposedDropdownMenu", StringComparison.OrdinalIgnoreCase)
        || type.Equals("Option", StringComparison.OrdinalIgnoreCase);

    private static bool IsListType(string type) =>
        type.Equals("ListView", StringComparison.OrdinalIgnoreCase)
        || type.Equals("LazyColumn", StringComparison.OrdinalIgnoreCase)
        || type.Equals("RecyclerView", StringComparison.OrdinalIgnoreCase)
        || type.Equals("DataGrid", StringComparison.OrdinalIgnoreCase)
        || type.Equals("PipelineInput", StringComparison.OrdinalIgnoreCase);
}

