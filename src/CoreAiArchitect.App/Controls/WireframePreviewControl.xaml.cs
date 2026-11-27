using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            new PropertyMetadata(null, OnVisualPropertyChanged));

    public static readonly DependencyProperty ProfileIdProperty =
        DependencyProperty.Register(
            nameof(ProfileId),
            typeof(string),
            typeof(WireframePreviewControl),
            new PropertyMetadata("wpf_desktop", OnVisualPropertyChanged));

    public static readonly DependencyProperty SelectedControlProperty =
        DependencyProperty.Register(
            nameof(SelectedControl),
            typeof(ControlInfo),
            typeof(WireframePreviewControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisualPropertyChanged));

    public static readonly DependencyProperty RefreshVersionProperty =
        DependencyProperty.Register(
            nameof(RefreshVersion),
            typeof(int),
            typeof(WireframePreviewControl),
            new PropertyMetadata(0, OnVisualPropertyChanged));

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

    public ControlInfo? SelectedControl
    {
        get => (ControlInfo?)GetValue(SelectedControlProperty);
        set => SetValue(SelectedControlProperty, value);
    }

    public int RefreshVersion
    {
        get => (int)GetValue(RefreshVersionProperty);
        set => SetValue(RefreshVersionProperty, value);
    }

    public WireframePreviewControl()
    {
        InitializeComponent();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WireframePreviewControl control)
            control.Refresh();
    }

    public void Refresh()
    {
        var screen = Screen;
        var profile = ProfileId ?? "wpf_desktop";
        var isAndroid = profile.Equals("android_app", StringComparison.OrdinalIgnoreCase);

        var sandDeepBrush = ResolveBrush("SandDeepBrush", Color.FromRgb(0x22, 0x26, 0x2E));
        var cardBrush = ResolveBrush("CardBrush", Color.FromRgb(0x23, 0x27, 0x30));
        var inkBrush = ResolveBrush("InkBrush", Color.FromRgb(0xEE, 0xF1, 0xF5));
        var secondaryBrush = ResolveBrush("TextSecondaryBrush", Color.FromRgb(0xA6, 0xB0, 0xC0));

        // Frame styling
        if (isAndroid)
        {
            WindowGlyph.Text = "📱";
            WindowButtons.Visibility = Visibility.Collapsed;
            TitleBar.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x3B, 0x3E));
            ScreenTitleText.Foreground = Brushes.White;
            ScreenTypeBadge.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x56, 0x5A));
            ScreenTypeText.Foreground = Brushes.White;
        }
        else
        {
            WindowGlyph.Text = "🗖";
            WindowButtons.Visibility = Visibility.Visible;
            TitleBar.Background = sandDeepBrush;
            ScreenTitleText.Foreground = inkBrush;
            ScreenTypeBadge.Background = cardBrush;
            ScreenTypeText.Foreground = secondaryBrush;
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

    private Brush ResolveBrush(string resourceKey, Color fallback) =>
        TryFindResource(resourceKey) as Brush ?? new SolidColorBrush(fallback);

    private FrameworkElement CreateControlWidget(ControlInfo control, bool isAndroid)
    {
        var type = control.Type ?? "Button";
        var label = string.IsNullOrWhiteSpace(control.Label) ? control.Id : control.Label;

        var cardBrush = ResolveBrush("CardBrush", Color.FromRgb(0x23, 0x27, 0x30));
        var cardBorderBrush = ResolveBrush("CardBorderBrush", Color.FromRgb(0x35, 0x3D, 0x4B));
        var inputBgBrush = ResolveBrush("InputBackgroundBrush", Color.FromRgb(0x12, 0x14, 0x19));
        var inputBorderBrush = ResolveBrush("InputBorderBrush", Color.FromRgb(0x3E, 0x47, 0x56));
        var inkBrush = ResolveBrush("InkBrush", Color.FromRgb(0xEE, 0xF1, 0xF5));
        var secondaryBrush = ResolveBrush("TextSecondaryBrush", Color.FromRgb(0xA6, 0xB0, 0xC0));
        var tealBrush = ResolveBrush("TealBrush", Color.FromRgb(0x4E, 0xB8, 0xB5));
        var coralBrush = ResolveBrush("CoralBrush", Color.FromRgb(0xFA, 0x7E, 0x6B));

        var isSelected = ReferenceEquals(control, SelectedControl);
        var container = new Border
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(10),
            Background = cardBrush,
            BorderBrush = isSelected ? tealBrush : cardBorderBrush,
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            CornerRadius = new CornerRadius(6),
            Cursor = Cursors.Hand
        };
        container.MouseLeftButtonUp += (_, e) =>
        {
            SelectedControl = control;
            e.Handled = true;
        };

        var stack = new StackPanel();

        if (IsButtonType(type))
        {
            var btn = new Border
            {
                Background = isAndroid ? tealBrush : coralBrush,
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
                Foreground = inkBrush,
                Margin = new Thickness(0, 0, 0, 4)
            });
            var dropBorder = new Border
            {
                BorderBrush = inputBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Background = inputBgBrush
            };
            var dropGrid = new Grid();
            dropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txt = new TextBlock { Text = "Pick an option...", FontSize = 12, Foreground = secondaryBrush };
            var arrow = new TextBlock { Text = "▾", FontSize = 12, Foreground = secondaryBrush };
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
                Foreground = inkBrush,
                Margin = new Thickness(0, 0, 0, 4)
            });
            var listBorder = new Border
            {
                BorderBrush = inputBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6),
                Background = inputBgBrush,
                MinHeight = 50
            };
            var listStack = new StackPanel();
            listStack.Children.Add(new TextBlock { Text = "• Sample item 1", FontSize = 11, Foreground = secondaryBrush, Margin = new Thickness(0, 2, 0, 2) });
            listStack.Children.Add(new TextBlock { Text = "• Sample item 2", FontSize = 11, Foreground = secondaryBrush, Margin = new Thickness(0, 2, 0, 2) });
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
                Foreground = inkBrush,
                Margin = new Thickness(0, 0, 0, 4)
            });
            var boxBorder = new Border
            {
                BorderBrush = inputBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Background = inputBgBrush
            };
            boxBorder.Child = new TextBlock { Text = "Enter text or number...", FontSize = 12, Foreground = secondaryBrush };
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

